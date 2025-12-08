using AthsVideoRecording.Data;
using SendVideoOverTCPLib.Receiver;
using SendVideoOverTCPLib.ViewModels;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace AthsVideoRecording.Views
{
    public partial class ProgramPage : ContentPage
    {
        public ProgramPage()
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
            //// Always call EnsureVideoListBuiltAsync so NewVideo (if set) can be inserted at the top.
            //// Show spinner only when building from scratch (no cache yet).
            //if (!SendVideoOverTCPLib.SendVideo.HasVideoListCache())
            //{
            //    BusyIndicatorLabel.Text = "Scanning videos (newest first)…";
            //    await SendVideoOverTCPLib.SendVideo.EnsureVideoListBuiltAsync();
            //}
            //else
            //{
            //    // Quick path: ensure insert without spinner
            //    await SendVideoOverTCPLib.SendVideo.EnsureVideoListBuiltAsync();
            //}
            //// Reset NewVideo as requested
            //SendVideoOverTCPLib.SendVideo.NewVideo = string.Empty;
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
            string caption = "";
            if (sender is Button)
            {
                caption = ((Button)sender).Text;
            }
            if(string.IsNullOrEmpty(caption))
            {
                return;
            }
            bool found = false;
            if(caption=="Get All Meets")
            {
                found = true;
            }
            else if (caption == "Get All Events")
            {
                found = true;
            }
            if (!found)
            {
                return;
            }
            this.BindingContext = SendVideoOverTCPLib.SendVideo.NetworkViewModel;

            if (this.BindingContext is NetworkViewModel nw)
            {
                grid.IsVisible = false;
                BusyIndicatorLabel.IsVisible = true;
                BusyIndicator.IsVisible = true;
                BusyIndicator.IsRunning = true;
                BusyIndicatorLabel.Text = "Waiting for Program to be sent…";
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
                    //var meets = MeetCsvImporter.ParseMeetsCsv(contents);
                    if(caption == "Get All Meets")
                    {
                        string msg = await MeetCsvImporter.ImportMeetsIntoDatabaseAsync(contents);
                        await DisplayAlert("Import Meets", msg, "OK");
                    }
                    else if(caption == "Get All Events")
                    {
                        string msg = await MeetCsvImporter.ImportEventsIntoDatabaseAsync(contents);
                        await DisplayAlert("Import Events", msg, "OK");
                    }
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
                finally
                {
                    BusyIndicator.IsRunning = false;
                    BusyIndicator.IsVisible = false;
                    BusyIndicatorLabel.IsVisible = false;
                    grid.IsVisible = true;
                }

            }
        }

        private void GetEvents4Meet(object sender, EventArgs e)
        {

        }
    }

}
