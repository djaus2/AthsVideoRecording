
//SendVideoOverTCPLib / Receiver / VideoReceiver.cs
using System;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sportronics.VideoEnums; // optional: for parsing VideoInfo

namespace SendVideoOverTCPLib.Receiver
{
    public static class VideoReceiver
    {
        // Start a listener that accepts one connection at a time (can be modified for concurrency)
        public static async Task StartListeningAsync(IPAddress bindAddress, int port, string saveDirectory, CancellationToken ct = default)
        {
            Directory.CreateDirectory(saveDirectory);
            var listener = new TcpListener(bindAddress, port);
            listener.Start();
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var client = await listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
#pragma warning disable CS4014
                    // handle without awaiting to keep accepting other clients
                    HandleClientAsync(client, saveDirectory, ct);
#pragma warning restore CS4014
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        private static async Task HandleClientAsync(TcpClient client, string saveDirectory, CancellationToken ct)
        {
            using (client)
            {
                using var stream = client.GetStream();
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
                    string fileName = null;
                    try
                    {
                        var info = VideoInfo.CreateFromJson(json);
                        if (info != null && !string.IsNullOrWhiteSpace(info.Filename))
                            fileName = SanitizeFileName(info.Filename);
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
                    using var outStream = File.Open(destPath, FileMode.Create, FileAccess.Write, FileShare.None);

                    int bytesRead;
                    while (!ct.IsCancellationRequested && (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
                    {
                        await outStream.WriteAsync(buffer, 0, bytesRead, ct).ConfigureAwait(false);
                    }

                    // Optionally: process the received JSON or compute checksum here
                }
                catch (OperationCanceledException) { /* canceled */ }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"VideoReceiver error: {ex.Message}");
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

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
