using Microsoft.Maui.Storage;
using SendVideoOverTCPLib;
using SendVideoOverTCPLib.Receiver;
using SendVideoOverTCPLib.ViewModels;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace AthsVideoRecording
{
    public partial class SendVideoPage : ContentPage
    {
        public SendVideoPage()
        {
            InitializeComponent();
            this.BindingContext = SendVideoOverTCPLib.SendVideo.NetworkViewModel;
        }

        private async void OnRescanVideos_Clicked(object sender, EventArgs e)
        {
            grid.IsVisible = false;
            BusyIndicatorLabel.IsVisible = true;
            BusyIndicator.IsVisible = true;
            BusyIndicator.IsRunning = true;
            BusyIndicatorLabel.Text = "Rescanning videos (newest first)…";
            try
            {
                SendVideoOverTCPLib.SendVideo.ClearVideoListCache();
                await SendVideoOverTCPLib.SendVideo.EnsureVideoListBuiltAsync();
                // Reset NewVideo after rescan
                SendVideoOverTCPLib.SendVideo.NewVideo = string.Empty;
            }
            catch { }
            finally
            {
                BusyIndicator.IsRunning = false;
                BusyIndicator.IsVisible = false;
                BusyIndicatorLabel.IsVisible = false;
                grid.IsVisible = true;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
           ;
            grid.IsVisible = false;
            BusyIndicatorLabel.Text = $"Getting saved Target Host ID or Local active Ids to select from if no setting (slower).";
            BusyIndicatorLabel.IsVisible = true;
            BusyIndicator.IsVisible = true;
            BusyIndicator.IsRunning = true;
            if (SendVideoOverTCPLib.SendVideo.NetworkViewModel == null) //Returning to this page
            {
                var ipaddress = await SendVideoOverTCPLib.SendVideo.GetSettings();
            }
            this.BindingContext = SendVideoOverTCPLib.SendVideo.NetworkViewModel;
            // Always call EnsureVideoListBuiltAsync so NewVideo (if set) can be inserted at the top.
            // Show spinner only when building from scratch (no cache yet).
            if (!SendVideoOverTCPLib.SendVideo.HasVideoListCache())
            {
                BusyIndicatorLabel.Text = "Scanning videos (newest first)…";
                await SendVideoOverTCPLib.SendVideo.EnsureVideoListBuiltAsync();
            }
            else
            {
                // Quick path: ensure insert without spinner
                await SendVideoOverTCPLib.SendVideo.EnsureVideoListBuiltAsync();
            }
            // Reset NewVideo as requested
            SendVideoOverTCPLib.SendVideo.NewVideo = string.Empty;
            BusyIndicator.IsRunning = false;
            BusyIndicator.IsVisible = false;
            BusyIndicatorLabel.IsVisible = false;
            grid.IsVisible = true;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Do not clear the cached list here so it persists across page views.
        }





        private async void OnCounterClicked(object sender, EventArgs e)
        {

            // Load your file as bytes
            byte[] fileBytes = File.ReadAllBytes(@"c:\temp\AAA\fghfggN.mp4");

            // Create TCP client and connect
            using var client = new TcpClient();
            await client.ConnectAsync("192.168.x.x", 5000); // Use desktop's LAN IP

            using var stream = client.GetStream();
            await stream.WriteAsync(fileBytes, 0, fileBytes.Length);

            await stream.FlushAsync();

        }
        private async void OnSendMovieFile_Clicked(object sender, EventArgs e)
        {
            NetworkViewModel networkViewModel = (NetworkViewModel)BindingContext;
            grid.IsVisible = false;
            BusyIndicatorLabel.IsVisible = true;
            BusyIndicator.IsVisible = true;
            BusyIndicator.IsRunning = true;

            // Show spinner while building the list if needed
            BusyIndicatorLabel.Text = "Scanning videos (newest first)…";
            await SendVideoOverTCPLib.SendVideo.EnsureVideoListBuiltAsync();

            // Hide spinner BEFORE opening the dialog so it is visible to user
            BusyIndicator.IsRunning = false;
            BusyIndicator.IsVisible = false;
            BusyIndicatorLabel.IsVisible = false;
            grid.IsVisible = true;

            // Now run pick+send flow which uses the cached list
            await SendVideoOverTCPLib.SendVideo.OnSendMovieFileClicked(networkViewModel);

            /*NetworkViewModel networkViewModel = (NetworkViewModel)BindingContext;
            grid.IsVisible = false;
            BusyIndicatorLabel.Text = $"Selecting video file then downloading it. Make sure Recvr is listening. Download timeout is {Math.Round((decimal)networkViewModel.DownloadTimeoutInSec)} sec";
            BusyIndicatorLabel.IsVisible = true;
            BusyIndicator.IsVisible = true;
            BusyIndicator.IsRunning = true;
            string json = networkViewModel..VideoInfoPathJosnStr;
            
            await SendVideoOverTCPLib.SendVideo.OnSendMovieFileClicked(networkViewModel);
            BusyIndicator.IsRunning = false;
            BusyIndicator.IsVisible = false;
            BusyIndicatorLabel.IsVisible = false;
            grid.IsVisible = true;
            */

            //var file = await PickMovieFileAsync();
            //if (file is null)
            //return;
            //var ipAddress = networkViewModel.SelectedIP;
            //await SendFileWithChecksumAsync(file.FullPath, ipAddress, 5000); // Use desktop's LAN IP
            /*var fileBytes = File.ReadAllBytes(file.FullPath);

            using var client = new TcpClient();
            await client.ConnectAsync("192.168.0.9", 5000); // desktop IP

            using var stream = client.GetStream();
            await stream.WriteAsync(fileBytes, 0, fileBytes.Length);

            await stream.FlushAsync();*/
        }



        private async Task<FileResult?> PickMovieFileAsync()
        {
            var customFileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "video/*" } }, // targets all video types
            });

            var options = new PickOptions
            {
                PickerTitle = "Select a Movie File",
                FileTypes = customFileTypes,

            };

            var pick =  await FilePicker.PickAsync(options);
            if (pick is null)
            {
                // User canceled the file picker
                return null;
            }
            // Ensure the file is a video type
            var fileExtension = Path.GetExtension(pick.FullPath).ToLowerInvariant();
            var validVideoExtensions = new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv" };
            if (!validVideoExtensions.Contains(fileExtension))
            {
                await DisplayAlert("Invalid File Type", "Please select a valid video file.", "OK");
                return null;
            }
            Uri uri = new Uri(pick.FullPath);
            Preferences.Set("LastVideoUri", uri.ToString());
            return pick;
        }

        public async Task SendFileAsync(string filePath, string ipAddress, int port)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Parse(ipAddress), port);

            using var stream = client.GetStream();
            string fileName = Path.GetFileName(filePath);
            byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
            byte[] fileNameLength = BitConverter.GetBytes(fileNameBytes.Length);

            // 1️⃣ Send filename length (Int32 - 4 bytes)
            await stream.WriteAsync(fileNameLength, 0, fileNameLength.Length);

            // 2️⃣ Send filename (UTF-8 bytes)
            await stream.WriteAsync(fileNameBytes, 0, fileNameBytes.Length);

            // 3️⃣ Send file contents
            using var fileStream = File.OpenRead(filePath);
            await fileStream.CopyToAsync(stream);

            // Optional: flush and close
            //await stream.FlushAsync();
        }

        const int ChunkSize = 1024 * 1024; // 1MB

        public async Task SendFileInChunksAsync(string filePath, string ipAddress, int port)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Parse(ipAddress), port);

            using var stream = client.GetStream();

            // 1️⃣ Send filename header
            string fileName = Path.GetFileName(filePath);
            byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);
            byte[] nameLength = BitConverter.GetBytes(nameBytes.Length);
            await stream.WriteAsync(nameLength);
            await stream.WriteAsync(nameBytes);

            // 2️⃣ Send file contents in chunks
            using var fileStream = File.OpenRead(filePath);
            byte[] buffer = new byte[ChunkSize];
            int bytesRead;
            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await stream.WriteAsync(buffer, 0, bytesRead);
            }

            // Optional: stream.FlushAsync() isn’t strictly needed here
        }

       // using Microsoft.Maui.Storage;



    public async Task SendFileWithChecksumAsync(string filePath, string ipAddress, int port)
    {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Parse(ipAddress), port);
            using var stream = client.GetStream();

            // Send filename
            string fileName = Path.GetFileName(filePath);
            byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);
            byte[] nameLength = BitConverter.GetBytes(nameBytes.Length);
            await stream.WriteAsync(nameLength);
            await stream.WriteAsync(nameBytes);

            // Calculate checksum
            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
            using var sha256 = SHA256.Create();
            byte[] checksum = sha256.ComputeHash(fileBytes);
            await stream.WriteAsync(checksum); // 32 bytes for SHA256

            // Send file in chunks
            using var fileStream = File.OpenRead(filePath);
            byte[] buffer = new byte[1024 * 1024]; // 1MB
            int bytesRead;
            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await stream.WriteAsync(buffer, 0, bytesRead);
            }
        }

        private void ClearAllSettings(object sender, EventArgs e)
        {
            SendVideoOverTCPLib.Settings.ClearAllPreferences();
            OnAppearing();
        }

        private void ClearSettings(object sender, EventArgs e)
        {
            SendVideoOverTCPLib.Settings.ClearPreferences();
            OnAppearing();
        }


        private void OnIpPickerSelectionChanged(object sender, EventArgs e)
        {
            SendVideoOverTCPLib.Settings.SaveSelectedSettings(((NetworkViewModel)this.BindingContext));
        }

        private void SelectedPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            SendVideoOverTCPLib.Settings.SaveSelectedSettings(((NetworkViewModel)this.BindingContext));
        }

        private async void RescanIps_Clicked(object sender, EventArgs e)
        {
            var nw = (NetworkViewModel)this.BindingContext;
 
            SendVideoOverTCPLib.Settings.SaveHostIdRange(nw.StartHostId, nw.EndHostId);
            grid.IsVisible = false;
            BusyIndicatorLabel.Text = $"Getting saved Target Host ID or Local active Ids to select from if no setting (slower).";
            BusyIndicatorLabel.IsVisible = true;
            BusyIndicator.IsVisible = true;
            BusyIndicator.IsRunning = true;
            await SendVideoOverTCPLib.SendVideo.GetSettings(false);
            this.BindingContext = SendVideoOverTCPLib.SendVideo.NetworkViewModel;
            BusyIndicator.IsRunning = false;
            BusyIndicator.IsVisible = false;
            BusyIndicatorLabel.IsVisible = false;
            grid.IsVisible = true;
        }

        private async void OnSetTimeoutClicked(object sender, EventArgs e)
        {
            // Get the current timeout in seconds
            NetworkViewModel networkViewModel = (NetworkViewModel)BindingContext;
            int currentTimeoutSeconds = networkViewModel.DownloadTimeoutInSec / 1000;
            
            // Show prompt for new timeout value
            string result = await DisplayPromptAsync(
                "Set Connection Timeout", 
                "Enter timeout in seconds:",
                initialValue: currentTimeoutSeconds.ToString(),
                maxLength: 5,
                keyboard: Keyboard.Numeric);
            
            // Process the result
            if (result != null && int.TryParse(result, out int newTimeoutSeconds) && newTimeoutSeconds > 0)
            {
                // Update the timeout value (convert seconds to milliseconds)
                networkViewModel.DownloadTimeoutInSec = newTimeoutSeconds * 1000;
                
                // Show confirmation
                await DisplayAlert(
                    "Timeout Updated", 
                    $"Connection timeout set to {newTimeoutSeconds} seconds.", 
                    "OK");
            }
            else if (result != null)
            {
                // Show error for invalid input
                await DisplayAlert(
                    "Invalid Input", 
                    "Please enter a positive number for the timeout in seconds.", 
                    "OK");
            }
        }

        private async void Done_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private async void Settings_Clicked(object sender, EventArgs e)
        {
            var serviceProvider = IPlatformApplication.Current.Services;
            var modalPage = serviceProvider.GetRequiredService<SettingsPage>();
            await Navigation.PushModalAsync(modalPage);
        }

        private async void GetProgram_Clicked(object sender, EventArgs e)
        {
            this.BindingContext = SendVideoOverTCPLib.SendVideo.NetworkViewModel;

            if (this.BindingContext is NetworkViewModel nw)
            {
                int port = nw.SelectedProgramUploadPort;
                try
                {
                    // Choose a user-visible, app-specific external folder on Android (no runtime permission required),
                    // otherwise use the user's Documents folder on desktop platforms.
                    string saveDir;

#if ANDROID
                    // App-specific external files directory (e.g. /storage/emulated/0/Android/data/<pkg>/files/Downloads)
                    // extDir = Android.App.Application.Context.GetExternalFilesDir(Android.OS.Environment.DirectoryDownloads)?.AbsolutePath;
                    saveDir = System.IO.Path.Combine( FileSystem.AppDataDirectory, "ReceivedPrograms");
#else
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            saveDir = System.IO.Path.Combine(docs, "ReceivedPrograms");
#endif

                    if (!Directory.Exists(saveDir))
                    {
                        Directory.CreateDirectory(saveDir!);
                    }

                    // Await the listener task so exceptions propagate here
                    await ProgramReceiver.StartListeningAsync(IPAddress.Any, port, saveDir);
                    string contents = ProgramReceiver.ReadFile(ProgramReceiver.FilePath);
                }
                catch (OperationCanceledException)
                {
                    // listener was cancelled — no action
                }
                catch (Exception ex)
                {
                    // Surface error to user (UI thread)
                    await DisplayAlert("Listener error", ex.Message, "OK");
                }
            }
        }
    }

}
