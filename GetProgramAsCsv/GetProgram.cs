using CommunityToolkit.Mvvm.ComponentModel;
using SendVideoOverTCPLib.Receiver;
using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.Views;
using static Java.Util.Jar.Attributes;




#if ANDROID
using Android.Content;
using Android.Provider;
using Android.Net;
using Android.App;
#endif

namespace SendVideoOverTCPLib.Receiver
{
    public partial class NetworkSettings : ObservableObject, INotifyPropertyChanged
    {
        [ObservableProperty]
        public string targetHostOrIp = "";
        [ObservableProperty]
        public int targetPort = 1000;
        [ObservableProperty]
        public int connectTimeoutMs = 2000;
    }

    public class MetaInfo
    {
        // Return nullable to reflect that deserialization can return null
        public static MetaInfo? CreateFromJson(string json)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<MetaInfo?>(json);
            }
            catch
            {
                return null;
            }
        }

        // Make these nullable so the compiler knows they may be missing in JSON
        public string? Filename { get; set; }
        public string? Checksum { get; set; }
        public string? ChecksumAlgorithm { get; set; }
        public long FileLength { get; set; }
    }

    public static class ProgramReceiver
    {

        public static string FilePath { get; set; } = "";

        public static async Task StartListeningAsync(IPAddress bindAddress, int port, string saveDirectory, CancellationToken ct = default)
        {
            Directory.CreateDirectory(saveDirectory);
            var listener = new TcpListener(bindAddress, port);
            listener.Start();
            try
            {
                //while (!ct.IsCancellationRequested)
                {
                    var client = await listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
#pragma warning disable CS4014
                    // handle without awaiting to keep accepting other clients
                    HandleClientAsync(client, saveDirectory, ct);
#pragma warning restore CS4014
                    //ct.l;
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        private static async Task HandleClientAsync(TcpClient client, string saveDirectory, CancellationToken ct)
        {
            FilePath = "";
            using (client)
            {
                using NetworkStream stream = client.GetStream();
                try
                {
                    // 1) Read 4 bytes (int) length prefix (sender uses BitConverter.GetBytes(int) -> 4 bytes, little-endian)
                    byte[] lenBuf = new byte[4];
                    await ReadExactlyAsync(stream, lenBuf, 0, 4, ct).ConfigureAwait(false);
                    int jsonLength = BitConverter.ToInt32(lenBuf, 0);

                    if (jsonLength <= 0 || jsonLength > 10_000_000) // safety check (10 MB)
                        throw new InvalidDataException($"Invalid JSON length: {jsonLength}");

                    // 2) Read JSON metadata
                    byte[] jsonBuf = new byte[jsonLength];
                    await ReadExactlyAsync(stream, jsonBuf, 0, jsonLength, ct).ConfigureAwait(false);
                    string json = Encoding.UTF8.GetString(jsonBuf);

                    // 3) Attempt to obtain a filename from metadata (optional)
                    string? fileName = null;
                    MetaInfo? info = null;
                    try
                    {
                        info = MetaInfo.CreateFromJson(json);
                        if (info != null && !string.IsNullOrWhiteSpace(info.Filename))
                            fileName = SanitizeFileName(info.Filename!);
                    }
                    catch
                    {
                        // ignore parse errors; we'll use fallback filename
                    }

                    if (string.IsNullOrEmpty(fileName))
                    {
                        fileName = $"received_{DateTime.Now:yyyyMMdd_HHmmss}.bin";
                    }

                    string destPath = Path.Combine(saveDirectory, fileName);

                    // 4) Read remaining bytes (the sender writes file bytes then closes) and write to disk
                    const int bufferSize = 1024 * 1024;
                    byte[] buffer = new byte[bufferSize];
                    long totalBytesRead = 0;
                    using (var outStream = File.Open(destPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        int bytesRead = 0;
                        while (!ct.IsCancellationRequested && (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
                        {
                            await outStream.WriteAsync(buffer, 0, bytesRead, ct).ConfigureAwait(false);
                            totalBytesRead += bytesRead;
                        }
                    }

                    if (info != null && info.FileLength > 0 && totalBytesRead != info.FileLength)
                    {
                        throw new InvalidDataException($"File length mismatch: expected {info.FileLength}, received {totalBytesRead}");
                    }

                    // Compute checksum of saved file and compare to info.Checksum (if provided)
                    if (info != null && !string.IsNullOrWhiteSpace(info.Checksum))
                    {
                        string algorithm = string.IsNullOrWhiteSpace(info.ChecksumAlgorithm) ? "SHA256" : info.ChecksumAlgorithm!;
                        string actualHex = ComputeFileChecksumHex(destPath, algorithm);
                        string expectedHex = info.Checksum!.Trim().ToLowerInvariant();

                        if (!string.Equals(actualHex, expectedHex, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidDataException($"Checksum mismatch for '{fileName}': expected {expectedHex}, actual {actualHex}");
                        }
                    }
                    // Save for later use
                    FilePath = destPath;
                }
                catch (OperationCanceledException) { /* canceled */ }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"VideoReceiver error: {ex.Message}");
                    throw;
                }
            }
        }

        // Helper: ensure we read exactly count bytes or throw if stream ends early
        private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken ct)
        {
            int read = 0;
            while (read < count)
            {
                int n = await stream.ReadAsync(buffer, offset + read, count - read, ct).ConfigureAwait(false);
                if (n == 0)
                    throw new EndOfStreamException("Stream ended before expected number of bytes were read.");
                read += n;
            }
        }

        private static string ComputeFileChecksumHex(string path, string algorithm)
        {
            algorithm = string.IsNullOrWhiteSpace(algorithm) ? "SHA256" : algorithm.Trim().ToUpperInvariant();
            byte[] hash;
            // Open the file once and compute chosen algorithm
            using (var stream = File.OpenRead(path))
            {
                switch (algorithm)
                {
                    case "MD5":
                        using (var md5 = MD5.Create())
                        {
                            hash = md5.ComputeHash(stream);
                        }
                        break;
                    case "SHA1":
                        using (var sha1 = SHA1.Create())
                        {
                            hash = sha1.ComputeHash(stream);
                        }
                        break;
                    case "SHA256":
                    default:
                        using (var sha256 = SHA256.Create())
                        {
                            hash = sha256.ComputeHash(stream);
                        }
                        break;
                }
            }
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public static string ReadFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
                {
                    return "";
                }
                else
                {
                    path = FilePath;
                }
            }

            string contents = "";
            using (var stream = File.OpenRead(path))
            {
                var reader = new StreamReader(stream);
                contents = reader.ReadToEnd();
            }
            return contents;
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        private static string GetMimeTypeByName(string fileName)
        {
            var ext = Path.GetExtension(fileName)?.ToLowerInvariant() ?? "";
            return ext switch
            {
                ".txt" => "text/plain",
                ".json" => "application/json",
                ".csv" => "text/csv",
                ".mp4" => "video/mp4",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream",
            };
        }

    }
}