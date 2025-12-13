using AthsVideoRecording.Data;
using AthsVideoRecording.Views;
using CommunityToolkit.Maui.Views;
using Java.Lang;
using MauiAndroidCameraViewLib;
using MauiAndroidVideoCaptureApp;
using MauiCountdownToolkit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using SendVideoOverTCPLib.ViewModels;
using Sportronics.VideoEnums;
using System;
// Ensure that the necessary namespaces are included at the top of the file.  
using System;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks;

namespace AthsVideoRecording;


public partial class MainPage : ContentPage, IDisposable
{
    // Service for handling video recording
    //private IVideoRecorderService? _videoRecorderService;
    private MauiAndroidCameraViewLib.VideoKapture _VideoKapture;



    // UI state tracking
    private bool _disposed = false;
    private string? VideoFilePath { get => _VideoKapture?.VideoFilePath; } // File path for the recorded video

    //private AppViewModel appViewModel;
    private bool NewDatabase = false;

    public MainPage()
    {
        InitializeComponent();
        _VideoKapture = new MauiAndroidCameraViewLib.VideoKapture(this, CameraPreview);
        Task.Delay(1000).Wait();//Let permission/s permeate
        // Register for page lifecycle events
        this.Appearing += _VideoKapture.MainPage_Appearing;
        this.Disappearing += _VideoKapture.MainPage_Disappearing;
        BindingContext = _VideoKapture.ViewModel;
        NewDatabase = SendVideoOverTCPLib.Settings.GetNewDatabaseSetting();
        Resources.Add("TimeModeToVisible", new TimeFromModeToVisibilityConverter());
        _VideoKapture.ViewModel.State = MediaRecorderState.Stopped; // Button gets disabled
        _VideoKapture.ViewModel.TimeFromMode = TimeFromMode.FromVideoStart; // Default time from mode
        _VideoKapture.ViewModel.CountdownMode = CountDownModeTranslator.ParseCameraView("red");// MauiAndroidCameraViewLib.CountDownMode.PopupRed;
        
        try {
            // Defer EF migration to after window loads, off UI thread to avoid startup hang
            this.Loaded += async (_, __) =>
            {
                try
                {
                    await Task.Run(() =>
                    {

                        using var ctx = new AthsVideoRecording.Data.AthsVideoRecordingDbContext();
                        if (NewDatabase)
                        {
                            ctx.Database.EnsureDeleted();
                            NewDatabase = false;
                            SendVideoOverTCPLib.Settings.SetNewDatabaseSetting(NewDatabase);
                        
                        }
                        ctx.Database.EnsureCreated();
                        //ctx.Database.Migrate();
                    });
                    // Seeding uses short operations; OK on UI thread post-migrate
                    await SeedAdminIfMissing();
                    await EnforceForcePasswordChangeIfNeeded();
                    await DisplayAlert("Database", $"All good!", "OK");
                }
                catch (System.Exception ex2)
                {
                    await DisplayAlert("Database", $"Database initialization error: {ex2.Message}", "OK");
                }
            };
        }
        catch (System.Exception ex)
        {
        }

    }



    private async Task DeleteAndRecreateDatabase_Menu_Click(object sender, EventArgs e)
    {
        var confirm = await DisplayAlert(
            "Delete Database",
            "This will delete the local AthStitcher database file and recreate it. Continue?",
            "Yes",
            "No"
            );
        if (!confirm)
            return;

        try
        {
            using var ctx = new AthsVideoRecording.Data.AthsVideoRecordingDbContext();
            if (!ctx.Database.EnsureDeleted())
            {
                // If ensure delete returns false or fails due to locks, try to drop all tables then continue
                try { ctx.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF;"); }
                catch { }
                try
                {
                    ctx.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS Meets;");
                    ctx.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS Events;");
                    ctx.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS Users;");
                }
                catch { }
                try { ctx.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;"); }
                catch { }
            }
            ctx.Database.Migrate();
            await SeedAdminIfMissing();
            await DisplayAlert("Database", "Database recreated successfully.", "OK");
        }
        catch (System.Exception ex)
        {
            await DisplayAlert("Database Error", $"Failed to delete/recreate database: {ex.Message}", "OK");
        }
    }

    private async Task SeedAdminIfMissing()
    {
        using var ctx = new AthsVideoRecording.Data.AthsVideoRecordingDbContext();
        var admin = ctx.Users.SingleOrDefault(u => u.Username == "admin");
        if (admin == null)
        {
            string tempPwd = AthsVideoRecording.Security.PasswordHasher.GenerateRandomPassword(24);
            var (hash, salt) = AthsVideoRecording.Security.PasswordHasher.HashPassword(tempPwd);
            admin = new AthsVideoRecording.Data.User
            {
                Username = "admin",
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = "Admin",
                CreatedAt = DateTime.UtcNow,
                ForcePasswordChange = true
            };
            ctx.Users.Add(admin);
            ctx.SaveChanges();
            try { await Clipboard.SetTextAsync(tempPwd); } catch { }
            await DisplayAlert("Admin Created", $"Admin user created.\n\nUsername: admin\nTemporary Password (copied to clipboard):\n{tempPwd}\n\nYou will be asked to change it on first login.",
                 "OK");
        }
    }

    private async Task EnforceForcePasswordChangeIfNeeded()
    {
        using var ctx = new AthsVideoRecording.Data.AthsVideoRecordingDbContext();
        var admin = ctx.Users.SingleOrDefault(u => u.Username == "admin");
        if (admin != null && admin.ForcePasswordChange)
        {
            if (this.IsLoaded)
            {
                await ChangePasswordForUser("admin", requireCurrent: false);
            }
            else
            {
                this.Loaded += async (_, __) => await ChangePasswordForUser("admin", requireCurrent: false);
            }
        }
    }

    private async Task ChangePasswordForUser(string username, bool requireCurrent)
    {
        //var dlg = new AthsVideoRecording.Views.ChangePasswordDialog { Username = username };
        //if (this.IsLoaded && this.IsVisible) dlg.Owner = this;
        //if (this.ShowPopup< ChangePasswordDialog>(dlg) != true) return;


        var modal = new ChangePasswordDialog();
        await Navigation.PushModalAsync(modal);
        bool result = await modal.WaitForCloseAsync();

        using var ctx = new AthsVideoRecording.Data.AthsVideoRecordingDbContext();
        var user = ctx.Users.SingleOrDefault(u => u.Username == username);
        if (user == null)
        {
            await DisplayAlert("Change Password",$"User '{username}' not found.","OK" );
            return;
        }

        if (requireCurrent)
        {
            string current = modal.CurrentPassword;
            if (!AthsVideoRecording.Security.PasswordHasher.Verify(current, user.PasswordHash, user.PasswordSalt))
            {
                await DisplayAlert("Change Password", "Current password is incorrect.",  "OK"); 
                return;
            }
        }

        var (hash, salt) = AthsVideoRecording.Security.PasswordHasher.HashPassword(modal.Password);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.ForcePasswordChange = false;
        ctx.SaveChanges();
        await DisplayAlert("Change Password", "Password changed successfully.", "OK");

    }


    // Menu handler to invoke change password (wire from XAML MenuItem)
    private async Task ChangePassword_Menu_Click(object sender, EventArgs e)
    {
        await ChangePasswordForUser("admin", requireCurrent: true);
    }

    // Menu handler to reset admin password (one-time recovery)
    private async Task ResetAdminPassword_Menu_Click(object sender, EventArgs e)
    {
        try
        {
            using var conn = Db.CreateConnection();
            var repo = new UserRepository();
            var user = repo.GetByUsername(conn, "admin");
            if (user == null)
            {
                await DisplayAlert("Reset Password", "Admin user not found.", "OK");
                return;
            }

            string tempPwd = AthsVideoRecording.Security.PasswordHasher.GenerateRandomPassword(24);
            if (repo.ResetPassword(conn, user.Id, tempPwd, forceChange: true))
            {
                try { await Clipboard.SetTextAsync(tempPwd); } catch { }
                await DisplayAlert("Reset Password", $"Admin password reset.\n\nTemporary Password (copied to clipboard):\n{tempPwd}\n\nYou'll be asked to change it on next login.", "OK");
            }
            else
            {
                await DisplayAlert("Reset Password", "Failed to reset admin password.", "OK");
            }
        }
        catch (System.Exception ex)
        {
            await DisplayAlert("Reset Password", $"Error resetting password: {ex.Message}", "OK");
        }
    }

    ~MainPage()
    {
        // Finalizer
        Dispose(false);
    }   

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                //// Dispose managed resources

                this.Appearing -= _VideoKapture.MainPage_Appearing;
                this.Disappearing -= _VideoKapture.MainPage_Disappearing;
            }

            _disposed = true;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _VideoKapture.MainPage_Appearing(this, EventArgs.Empty);
    }


    
    private void OnFilenameCompleted(object? sender, EventArgs e)
    {
        try
        {
            if (_VideoKapture.ViewModel is RecordingViewModel)
            {
                // Get the filename from the Entry control
                var entry = sender as Entry;
                string filename = entry?.Text;

                if (string.IsNullOrWhiteSpace(filename))
                {
                    ShowMessage("Please enter a valid filename");
                    return;
                }

                // Validate the filename
                if (filename.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
                {
                    ShowMessage("Invalid filename. Please avoid using special characters.");
                    return;
                }
                

                _VideoKapture.OnFilenameCompleted(filename);
                System.Diagnostics.Debug.WriteLine($"Filename validated and set: {filename}");
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error validating filename: {ex.Message}");
            ShowMessage($"Error validating filename: {ex.Message}");
        }
    }

    private void OnAutoStartSecsCompleted(object? sender, EventArgs e)
    {
        try
        {
            if (_VideoKapture.ViewModel is RecordingViewModel)
            {
                // Get the filename from the Entry control
                var entry = sender as Entry;
                string autostartsecsStr = entry?.Text;

                if (!string.IsNullOrWhiteSpace(autostartsecsStr))
                {
                    _VideoKapture.ViewModel.AutoStartSecs = int.TryParse(autostartsecsStr, out int autostartSecs) ? autostartSecs : 0;
                }
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error validating AuoStartSecs: {ex.Message}");
            ShowMessage($"Error validating AutoStartSecs: {ex.Message}");
        }
    }

    private async void OnButton_GetReady4Recording(object? sender, EventArgs e)
    {
        try
        {
            // Get screen dimensions for preview
            var activity = Platform.CurrentActivity;
            await _VideoKapture.OnButton_GetReady4Recording(activity);
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"App:Error getting ready for recording: {ex.Message}");
            ShowMessage($"Error getting ready for recording: {ex.Message}");
        }
    }

    private async void OnButton_CancelRecording_Clicked(object? sender, EventArgs e)
    {
        try
        {
            // Clean up resources
            await _VideoKapture.OnButton_CancelRecording_Clicked();
            _VideoKapture.ViewModel.NotifyStateChange();
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"App: Error canceling recording: {ex.Message}");;
        }
    }

    private async void OnButton_StartRecording_Clicked(object? sender, EventArgs e)
    {

        try
        {
            if (countdown != null)
            {
                // If a countdown popup is active, cancel it
                countdown.Cancel();
            }
            // Start recording
            await _VideoKapture.OnButton_StartRecording_Clicked();

            // Update UI state based on the current state of the recorder
            // The state will be updated by the RecordingStarted event handler
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error starting recording: {ex.Message}");
            ShowMessage($"Error starting recording: {ex.Message}");
        }
    }


    MauiCountdownToolkit.Countdown? countdown = null;
   
    private async void OnButton_AutoStartRecordingafterAutoStartSecs_Clicked(object? sender, EventArgs e)
    {
        try

        {
            if (_VideoKapture.ViewModel is RecordingViewModel)
            {
                if ((_VideoKapture.ViewModel.AutoStart))
                {
                    var mode = _VideoKapture.ViewModel.CountdownMode;
                    //Need to translate between types. 
                    MauiCountdownToolkit.CountDownMode mmode = CountDownModeTranslator.ToToolkit(mode);
                    if (mmode == MauiCountdownToolkit.CountDownMode.None)
                    {
                        return;
                    }
                    int delay = _VideoKapture.ViewModel.AutoStartSecs; 
                    if (delay > 0)
                    {

                        // If doing delay here then signal to VideoKapture to not use soft auto start
                        countdown = Countdown.Create(this, mmode, "videogreenx.svg");// "dotnet_athletics.png");
                        if (countdown != null)
                        {
                            bool response = await countdown.Wait(delay);
                            if (response)
                            {
                                await _VideoKapture.OnButton_AutoStartRecordingafterAutoStartSecs_Clicked();
                            }
                        }
                        countdown = null;
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error starting recording: {ex.Message}");
            ShowMessage($"Error starting recording: {ex.Message}");
        }
    }

    private async void OnButton_PauseRecording_Clicked(object? sender, EventArgs e)
    {
        try
        {
            // Pause recording
            await _VideoKapture.OnButton_PauseRecording_Clicked();
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error pausing recording: {ex.Message}");
            ShowMessage($"Error pausing recording: {ex.Message}");
        }
    }

    private async void OnButton_ContinueRecording_Clicked(object? sender, EventArgs e)
    {
        try
        {
            // Resume recording
            await _VideoKapture.OnButton_ContinueRecording_Clicked();
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error resuming recording: {ex.Message}");
            ShowMessage($"Error resuming recording: {ex.Message}");
        }
    }

    private async void OnButton_StopRecording_Clicked(object? sender, EventArgs e)
    {
        try
        {
            // Stop recording
            DateTime videoStart = await _VideoKapture.OnButton_StopRecordingClicked();
            if (videoStart == DateTime.MinValue)
            {
                // Error occurred during stopping
                return;
            }

            // If the Send page list is already cached, insert and sort it now; otherwise do nothing
            var svc = IPlatformApplication.Current.Services.GetService<SendVideoOverTCPLib.Services.IVideoMetadataService>();
            if (svc != null && svc.HasCachedList())
            {
                svc.AddVideoToCache(_VideoKapture.VideoFilePath, videoStart);
                SendVideoOverTCPLib.SendVideo.NewVideo = string.Empty;
            }

            // Save JSON metadata alongside the video
            string path = await GetJson();
            ShowMessage($"VideoInfo: {path}");
            System.Diagnostics.Debug.WriteLine($"Video Info JSON saved: {path}");

            // Apply file naming based on timing mode or gun time
            // Nb: GunTime is now in Json
            if (_VideoKapture.ViewModel.GunDateTime != DateTime.MinValue)
            {
                _VideoKapture.ViewModel.GunDateTime = DateTime.MinValue;
            }
            else
            {
            }

            _VideoKapture.ViewModel.NotifyStateChange();
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error stopping recording: {ex.Message}");
            ShowMessage($"Error stopping recording: {ex.Message}");
        }
    }

    private void OnButton_NextPage(object? sender, EventArgs e)
    {
        var window = Application.Current.Windows[0];
        window.Page = new MainPage();
    }

    private void ShowMessage(string message)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await DisplayAlert("Alert", message, "OK");
        });
    }

    private void OnButton_CrossHairs(object sender, EventArgs e)
    {
        _VideoKapture.ViewModel.EnableCrossHairs = !_VideoKapture.ViewModel.EnableCrossHairs;
    }

    private void SetGunTime_Click(object sender, EventArgs e)
    {
        _VideoKapture.ViewModel.GunDateTime = DateTime.Now;
        var viewModel = (RecordingViewModel)this.BindingContext;
        viewModel.GunState = MauiAndroidCameraViewLib.GunStateEnum.Fired;// Update the gun time in the view model
        var yy = viewModel.IsStartGunButtonEnabled;
    }


    private void OnTimeFromModeChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value)
        {
            if (this.BindingContext is RecordingViewModel viewModel)
            {
                var radioButton = sender as RadioButton;
                if (radioButton == null) return;
                // Get the content of the radio button to identify it
                string mode = radioButton.Content?.ToString() ?? "";
                if (string.IsNullOrEmpty(mode))
                {
                    System.Diagnostics.Debug.WriteLine("RadioButton content is null or empty.");
                    return;
                }
                System.Diagnostics.Debug.WriteLine($"Selected: {mode}");
                SetModeFromButton(mode);
            }
        }
    }

    private void OnFrameRateChanged(object? sender, CheckedChangedEventArgs e)
    {
        // Only process when a button is checked, not unchecked
        if (!e.Value) return;

        var radioButton = sender as RadioButton;
        if (radioButton == null) return;

        // Identify which radio button by its content
        string content = radioButton.Content?.ToString() ?? "";
        if (string.IsNullOrEmpty(content)) return;

        if (content.Contains("30"))
        {
            _VideoKapture.SelectedFrameRate = 30; // 30 FPS
            System.Diagnostics.Debug.WriteLine("Frame rate set to 30 FPS");
        }
        else if (content.Contains("60"))
        {
            _VideoKapture.SelectedFrameRate = 60; // 60 FPS
            System.Diagnostics.Debug.WriteLine("Frame rate set to 60 FPS");
        }
        else if (content.Contains("Auto"))
        {
            _VideoKapture.SelectedFrameRate = 0; // 0 indicates auto
            System.Diagnostics.Debug.WriteLine("Frame rate set to Auto");
        }
    }

    private void OnStabilizationModeChanged(object? sender, CheckedChangedEventArgs e)
    {
        // Only process when a button is checked, not unchecked
        if (!e.Value) return;

        var radioButton = sender as RadioButton;
        if (radioButton == null) return;

        // Identify which radio button by its content
        string content = radioButton.Content?.ToString() ?? "";
        if (string.IsNullOrEmpty(content)) return;

        if (content.Contains("Standard"))
        {
            _VideoKapture.UseLockedStabilization = false;
            System.Diagnostics.Debug.WriteLine("Selected stabilization mode: Standard");
        }
        else if (content.Contains("Locked"))
        {
            _VideoKapture.UseLockedStabilization = true;
            System.Diagnostics.Debug.WriteLine("Selected stabilization mode: Locked");
        }
    }

    private void OnButton_TimeFromMode_Clicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("OnButton_TimeFromMode_Clicked");
        if (sender is ImageButton button && button.CommandParameter is string mode)
        {
            System.Diagnostics.Debug.WriteLine(mode);
            mode = mode.Replace("Button", "").Trim(); // Remove "Button" from the mode string
            System.Diagnostics.Debug.WriteLine(mode);
            switch (mode)
            {
                case "Start":
                    FromVideoStart.IsChecked = true; // Set the corresponding radio button to checked
                    break;
                case "Bang":
                    FromGunSound.IsChecked = true; // Set the corresponding radio button to checked
                    break;
                case "Flash":
                    FromGunFlash.IsChecked = true; // Set the corresponding radio button to checked
                    break;
                case "Manual":
                    ManuallySelect.IsChecked = true; // Set the corresponding radio button to checked
                    break;
                case "WC":
                    WallClockSelect.IsChecked = true; // Set the corresponding radio button to checked
                    break;
            }            
        }
    }

    private void SetModeFromButton(string mode)
    {
        if (this.BindingContext is RecordingViewModel viewModel)
        {
            switch (mode)
            {
                case "Vid Start":
                    viewModel.TimeFromMode = TimeFromMode.FromVideoStart;
                    break;
                case "Gun Bang":
                    viewModel.TimeFromMode = TimeFromMode.FromGunSound;
                    break;
                case "Gun Flash":
                    viewModel.TimeFromMode = TimeFromMode.FromGunFlash;
                    break;
                case "Manual":
                    viewModel.TimeFromMode = TimeFromMode.ManuallySelect;
                    break;
                case "Wall Clck":
                    viewModel.TimeFromMode = TimeFromMode.WallClockSelect;
                    break;
            }
        }
    }

    public class TimeFromModeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //return mode != null && mode == TimeFromMode.WallClockSelect;
            if (value == null)
            {
                return false; // If value is null, return false
            }
            return value is TimeFromMode mode && mode == TimeFromMode.WallClockSelect;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    private void OnCountDownModeChange(object sender, CheckedChangedEventArgs e)
    {
        if (!e.Value) return; // Only process when a button is checked, not unchecked

        var radioButton = sender as RadioButton;
        if (radioButton == null) return;

        // Get the content of the radio button to identify it
        string content = radioButton.Content?.ToString() ?? "";
        if (string.IsNullOrEmpty(content))
            return;
        if (content.Contains("none", StringComparison.OrdinalIgnoreCase))
        {
            _VideoKapture.ViewModel.CountdownMode = MauiAndroidCameraViewLib.CountDownMode.None;
        }
        else if (content.Contains("soft",StringComparison.OrdinalIgnoreCase))
        {
            _VideoKapture.ViewModel.CountdownMode = MauiAndroidCameraViewLib.CountDownMode.Soft;
        }
        else if (content.Contains("red", StringComparison.OrdinalIgnoreCase))
        {
            _VideoKapture.ViewModel.CountdownMode = MauiAndroidCameraViewLib.CountDownMode.PopupRed;
        }
        else if (content.Contains("rainbow", StringComparison.OrdinalIgnoreCase))
        {
            _VideoKapture.ViewModel.CountdownMode = MauiAndroidCameraViewLib.CountDownMode.PopupRainbow;
        }
    }
    private async void DownloadVideo()
    {
        var serviceProvider = IPlatformApplication.Current.Services;
        var modalPage = serviceProvider.GetRequiredService<SendVideoPage>();
        await Navigation.PushModalAsync(modalPage);

    }

    private void OnButton_SendVideo(object sender, EventArgs e)
    {
        DownloadVideo();
    }

    private async Task<string> GetJson()
    {
        try
        {
            //// Calculate checksum
            //byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(_VideoKapture.VideoFilePath);
            //using var sha256 = System.Security.Cryptography.SHA256.Create();
            //byte[] checksum = sha256.ComputeHash(fileBytes);

            VideoInfo videoInfo = new VideoInfo
            {
                VideoPath = _VideoKapture.VideoFilePath,
                Filename = System.IO.Path.GetFileName(_VideoKapture.VideoFilePath),
                VideoStart = _VideoKapture.ViewModel.RecordingStartTime,
                TimeFrom = _VideoKapture.ViewModel.TimeFromMode,
            };

            videoInfo.GetCheckSum();

            switch (videoInfo.TimeFrom)
            {
                case TimeFromMode.FromGunFlash:
                    videoInfo.DetectMode = VideoDetectMode.FromFlash;
                    break;
                case TimeFromMode.WallClockSelect:
                    videoInfo.GunTime = _VideoKapture.ViewModel.GunDateTime;
                    break;
            }

            string videoInfoStr = videoInfo.ToJson();

            string jsonPath = _VideoKapture.VideoFilePath.Replace(".mp4", ".json", StringComparison.OrdinalIgnoreCase);
            string jsonFileName = System.IO.Path.GetFileName(jsonPath);
            string appDataPath = Microsoft.Maui.Storage.FileSystem.AppDataDirectory;
            string privateJsonPath = System.IO.Path.Combine(appDataPath, jsonFileName);

            await System.IO.File.WriteAllTextAsync(privateJsonPath, videoInfoStr);
            System.Diagnostics.Debug.WriteLine($"JSON saved in app data: {privateJsonPath}");

            _VideoKapture.VideoInfoFilePath = privateJsonPath;
            _VideoKapture.VideoInfoStr = videoInfoStr;

            return privateJsonPath;
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving JSON: {ex.Message}");
            ShowMessage($"Error saving metadata: {ex.Message}");
            return string.Empty;
        }
    }

    //private async void OnButton_Programm(object sender, EventArgs e)
    //{
    //    var dial = "AthsVideoRecording.Views.ProgramPage";

    //    var serviceProvider = IPlatformApplication.Current.Services;
    //    var modalPage = serviceProvider.GetRequiredService<AthsVideoRecording.Views.ProgramPage>();
    //    await Navigation.PushModalAsync(modalPage);
    //}

    private async void OnButton_Programm(object sender, EventArgs e)
    {
        var serviceProvider = IPlatformApplication.Current.Services;
        var modalPage = serviceProvider.GetRequiredService<AthsVideoRecording.Views.ProgramPage>();

        // subscribe to the ProgramPage.Close event and run logic when it closes
        EventHandler? handler = null;
        handler = async (s, args) =>
        {
            // unsubscribe to avoid memory leak / duplicate calls
            modalPage.Closed -= handler;

            // do UI work on the main thread (this handler may already be on UI thread)
            await OnProgramPageReturnedAsync();
        };
        modalPage.Closed += handler;

        await Navigation.PushModalAsync(modalPage);
    }

    // action to run when ProgramPage finishes and returns to MainPage
    private async Task OnProgramPageReturnedAsync()
    {
        // example: refresh UI, reload data, show a message, etc.
        // call methods that update state or reload DB content here
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            // Replace with your actual refresh logic
            await DisplayAlert("Returned", "Returned from ProgramPage", "OK");
            // e.g. RefreshMeetList();
            var _Meets = ProgramPage._Meets;
            this.Filename.Text = _Meets.EventHeatInfo;
        });
    }
}