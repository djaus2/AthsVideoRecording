csharp AthStitcher/Network/SendTextFileClient.cs
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AthStitcher.Network
{
    public static class SendTextFileClient
    {
        /// <summary>
        /// Sends metadata (JSON) then a file over TCP using the receiver protocol:
        /// [4-byte little-endian JSON length][JSON bytes][file bytes...]
        /// Connection is closed by sender when done.
        /// </summary>
        public static async Task SendFileAsync(string hostOrIp, int port, string filePath, string? filenameOverride = null, int connectTimeoutMs = 10000, CancellationToken ct = default)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(hostOrIp, port);
            if (await Task.WhenAny(connectTask, Task.Delay(connectTimeoutMs, ct)) != connectTask)
                throw new TimeoutException($"Connecting to {hostOrIp}:{port} timed out.");

            using var stream = client.GetStream();

            // Build minimal metadata JSON (adjust shape to match receiver expectations)
            var metadata = new { Filename = filenameOverride ?? Path.GetFileName(filePath) };
            string json = JsonSerializer.Serialize(metadata);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            byte[] jsonLen = BitConverter.GetBytes(jsonBytes.Length); // little-endian on Windows

            // Send JSON length prefix
            await stream.WriteAsync(jsonLen.AsMemory(0, jsonLen.Length), ct).ConfigureAwait(false);
            // Send JSON bytes
            await stream.WriteAsync(jsonBytes.AsMemory(0, jsonBytes.Length), ct).ConfigureAwait(false);

            // Send file contents
            const int bufferSize = 1024 * 1024;
            byte[] buffer = new byte[bufferSize];
            using var fileStream = File.OpenRead(filePath);
            int bytesRead;
            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
            {
                await stream.WriteAsync(buffer.AsMemory(0, bytesRead), ct).ConfigureAwait(false);
            }

            // Optionally flush and close by disposing client/stream
            await stream.FlushAsync(ct).ConfigureAwait(false);
        }
    }
}