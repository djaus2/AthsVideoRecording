using Android.Content.PM;
using Android.Telephony.Mbms;
using Microsoft.Extensions.DependencyInjection;
using Sportronics.VideoEnums;
using SendVideoOverTCPLib.Services;
using SendVideoOverTCPLib.ViewModels;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using static System.Net.Mime.MediaTypeNames;


namespace SendVideoOverTCPLib
{
    // All the code in this file is included in all platforms.
    public static class SendVideo
    {
       
        /*public static int MaxIPAddress { get; set; } = 20;
        public static int MinIPAddress { get; set; } = 2;  //Router is often XX.YY.ZZ.1
        public static int TimeoutInHalfSeconds { get; set; } = 6;*/
        public static NetworkViewModel? NetworkViewModel { 
            get; 
            set; 
        } 

        public static string NewVideo { get; set; } = string.Empty;

        private static IVideoMetadataService VideoMetadataService =>
            Microsoft.Maui.Controls.Application.Current.Handler.MauiContext.Services.GetService<IVideoMetadataService>() ?? 
            new DefaultVideoMetadataService();



        public static async Task OnSendMovieFileClicked(NetworkViewModel _networkViewModel)
        {
            // Use the VideoMetadataService to pick a video file and get its original metadata
            var videoFileInfo = await VideoMetadataService.PickVideoAsync();
            if (videoFileInfo is null)
                return;
            /*string jsonPath = videoFileInfo.FileName.Replace(".mp4", ".json", StringComparison.OrdinalIgnoreCase);
            string appDataPath = FileSystem.Current.AppDataDirectory;
            string filePath = Path.Combine(appDataPath, jsonPath);
            string videoInfojson = await File.ReadAllTextAsync(filePath);
            ShowMessage(videoInfojson, "Video Info JSON");*/
            //string json = NetworkViewModel?.SelectedIpAddress ?? "";
            //VideoInfo videoInfo = new VideoInfo
            //{
            //    Filename = videoFileInfo.FileName,
            //    VideoStart = videoFileInfo.CreationTime,
            //    GunTime = videoFileInfo.CreationTime,
            //    DetectMode = VideoDetectMode.FromFlash,
            //    TimeFrom = TimeFromMode.FromVideoStart
            //};

            //videoInfo.GetInfo();

            try
            {
                // Set busy state to show the indicator
                await SendFileWithChecksumAsync(videoFileInfo, _networkViewModel.SelectedIpAddress, _networkViewModel.SelectedPort);
            }
            catch (Exception ex)
            {
                // Show error popup
                await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert("Connection Error",
                    $"Could not connect to receiver. Please ensure the receiver app is running and listening on port {_networkViewModel.SelectedPort}.",
                    "OK");
            }
            finally
            {
                // Always clear busy state when done
            }
        }

        /// <summary>
        /// Preload newest-first, meta-filtered recent videos into cache (Android implementation).
        /// Call this from the UI when the Send page is shown to avoid delay on button press.
        /// No-ops on platforms without a custom implementation.
        /// </summary>
        public static async Task PreloadRecentVideosAsync()
        {
            try
            {
                // Only Android implementation has this method; others safely ignore
                if (VideoMetadataService is Platforms.Android.VideoMetadataService androidSvc)
                {
                    await androidSvc.PreloadRecentVideosAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PreloadRecentVideosAsync failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Ensure the newest-first filtered list exists (Android only). Does nothing if already built.
        /// </summary>
        public static async Task EnsureVideoListBuiltAsync()
        {
            if (VideoMetadataService is Platforms.Android.VideoMetadataService androidSvc)
            {
                await androidSvc.EnsureListBuiltAsync();
            }
        }

        /// <summary>
        /// Clear the prebuilt list cache (Android only).
        /// </summary>
        public static void ClearVideoListCache()
        {
            if (VideoMetadataService is Platforms.Android.VideoMetadataService androidSvc)
            {
                androidSvc.ClearCache();
            }
        }

        /// <summary>
        /// Returns true if the Android video list cache has items.
        /// </summary>
        public static bool HasVideoListCache()
        {
            if (VideoMetadataService is Platforms.Android.VideoMetadataService androidSvc)
            {
                return androidSvc.HasCachedList();
            }
            return false;
        }

        // This method is replaced by VideoMetadataService.PickVideoAsync
        private static async Task<FileResult?> PickMovieFileAsync()
        {
            var customFileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "video/*" } }, // targets all video types
            });

            var options = new PickOptions
            {
                PickerTitle = "Select a Movie File",
                FileTypes = customFileTypes
            };

            return await FilePicker.PickAsync(options);
        }

        private static void ShowMessage(string message, string title = "Alert")
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert(title, message, "OK");
            });
        }

        public static async Task SendFileWithChecksumAsync(VideoFileInfo fileInfo, string ipAddress, int port)
        {

            using var client = new TcpClient();
            try
            {
                // Get the configurable timeout from NetworkViewModel - access directly from the static instance
                int timeoutMs = SendVideo.NetworkViewModel.DownloadTimeoutInSec*1000;

                // Set a connection timeout based on the configurable setting
                var connectTask = client.ConnectAsync(IPAddress.Parse(ipAddress), port);

                // Wait for connection with configurable timeout
                if (await Task.WhenAny(connectTask, Task.Delay(timeoutMs)) != connectTask)
                {
                    // Connection timed out
                    throw new TimeoutException($"Connection to {ipAddress}:{port} timed out after {timeoutMs/1000} seconds. Please ensure the receiver is listening.");
                }

                // Make sure the connection task completed successfully
                await connectTask;
            }
            catch (SocketException ex)
            {
                throw new Exception("Failed to connect to receiver", ex);
            }
            using var stream = client.GetStream();

            VideoInfo? videoInfo = null;

            var res =MetadataManager.ReadJsonComment(fileInfo.FilePath); // Verify save

            videoInfo = VideoInfo.CreateFromJson(res ?? "");

            // If no valid metadata, create new with basic info
            if (videoInfo is null)
            {
                videoInfo = new VideoInfo
                {
                    Filename = fileInfo.FileName,
                    VideoStart = fileInfo.CreationTime,
                    GunTime = fileInfo.CreationTime,
                    DetectMode = VideoDetectMode.FromFlash,
                    TimeFrom = TimeFromMode.FromVideoStart
                };
            }

            videoInfo.VideoPath = fileInfo.FilePath;

            videoInfo.GetCheckSum();
            //ShowMessage(videoInfo.ToJson());

            byte[] jsonBytes = Encoding.UTF8.GetBytes(videoInfo.ToJson());
            byte[] jsonLength = BitConverter.GetBytes(jsonBytes.Length);
            await stream.WriteAsync(jsonLength);
            await stream.WriteAsync(jsonBytes);

            // Send file in chunks
            using var fileStream = File.OpenRead(fileInfo.FilePath);
            byte[] buffer = new byte[1024 * 1024]; // 1MB
            int bytesRead;
            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await stream.WriteAsync(buffer, 0, bytesRead);
            }
        }

        /// <summary>
        /// Get list of local active IpAddresses 
        /// Uses this phones's subnet
        /// Excluding this phone's
        /// </summary>
        /// <returns>List of IpAddresses.</returns>
        public static async Task<List<string>> GetLocalActiveDevices()
        {
            string localIP = GetLocalPhoneIPAddress(); // e.g., 192.168.1.42
            string subnet = localIP.Substring(0, localIP.LastIndexOf('.') + 1); // e.g., 192.168.1.
            List<string> activeIPs = new List<string>();
            for (int i = NetworkViewModel.StartHostId; i <= NetworkViewModel.EndHostId; i++)
            {
                string ip = $"{subnet}{i}";
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ip, NetworkViewModel.PingTimeoutInMs);
                if (reply.Status == IPStatus.Success)
                {
                    if(ip != localIP) // Exclude the local IP
                        activeIPs.Add(ip);
                }
            }

            return activeIPs;

        }

        static string GetLocalPhoneIPAddress()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return ip.Address.ToString();
                        }
                    }
                }
            }
            return "No active IPv4 network adapters found.";
        }

        /// <summary>
        /// Get Observable Collection of local active IpAddresses.
        /// Excluding this phone's
        /// And excluding Subnet.1, oftehn the router.
        /// </summary>
        /// <returns>IPAddress if only one.</returns>
        public static async Task<string> GetIps()
        {;
            var ips = await GetLocalActiveDevices();
            NetworkViewModel.ActiveIPs = new ObservableCollection<string>(ips);
            if (NetworkViewModel.ActiveIPs.Count == 1)
            {
                NetworkViewModel.SelectedIpAddress = NetworkViewModel.ActiveIPs[0];
                return NetworkViewModel.SelectedIpAddress;
            }
            else
            {
                NetworkViewModel.SelectedIpAddress = "";
            }
            return "";
        }



        public static async Task<string> GetSettings(bool checkSettings = true)
        {
            NetworkViewModel = Settings.GetSettingNetworkViewModel();
            NetworkViewModel.ActiveIPs = new();
            string ip = "";
            if(checkSettings)
                ip = await TryRestoreSelectedIpAsync();
            if (string.IsNullOrEmpty(ip))
            { 
                try
                {
                    ip = await GetIps();
                    Settings.SaveSelectedSettings(NetworkViewModel);
                }
                catch (Exception ex)
                {
                    // Handle exceptions, e.g., log them or show an alert
                    Console.WriteLine($"Error retrieving IPs: {ex.Message}");
                    return ""; // Return an empty on error
                }   
            }
            return ip;
        }

        public static async Task<string> TryRestoreSelectedIpAsync()
        {

            string savedIp = NetworkViewModel.SelectedIpAddress;
            if (!string.IsNullOrEmpty(savedIp))
            {
                using var ping = new Ping();
                try
                {
                    var reply = await ping.SendPingAsync(savedIp, NetworkViewModel.PingTimeoutInHalfSeconds * 500);
                    if (reply.Status == IPStatus.Success)
                    {
                        return savedIp;
                    }
                }
                catch
                {
                    // Optionally log or ignore
                }
            }
            return "";
        }

    }
}
