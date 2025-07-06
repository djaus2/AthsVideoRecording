using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Java.IO;
using Java.Lang;
using Java.Util.Concurrent;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using static AndroidX.Camera.Core.Internal.CameraUseCaseAdapter;
using AndroidX.Lifecycle;
using System.Diagnostics.Metrics;
using MauiAndroidCameraViewLib;
using Java.Interop;
using static Android.InputMethodServices.Keyboard;
using AndroidX.Camera.Video;


namespace MauiCameraViewSample;


public partial class MainPage : ContentPage, IDisposable
{
    // Service for handling video recording
    //private IVideoRecorderService? _videoRecorderService;
    private MauiAndroidCameraViewLib.VideoKapture _VideoKapture;


    // UI state tracking
    private bool _disposed = false;
    private string? VideoFilePath { get => _VideoKapture?.VideoFilePath; } // File path for the recorded video

    //private AppViewModel appViewModel;

    public MainPage()
    {
        InitializeComponent();
        _VideoKapture = new MauiAndroidCameraViewLib.VideoKapture(this, CameraPreview);
        Task.Delay(1000).Wait();//Let permission/s permeate
        // Register for page lifecycle events
        this.Appearing += _VideoKapture.MainPage_Appearing;
        this.Disappearing += _VideoKapture.MainPage_Disappearing;
        BindingContext = _VideoKapture.ViewModel;
        _VideoKapture.ViewModel.State = MauiAndroidCameraViewLib.MediaRecorderState.Stopped; // Button gets disabled
        _VideoKapture.ViewModel.TimeFromMode = MauiAndroidCameraViewLib.TimeFromMode.FromVideoStart; // Default time from mode
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

                //// Dispose the video recorder service
                //if (_videoRecorderService != null && _videoRecorderService is IDisposable disposable)
                //{
                //    disposable.Dispose();
                //    _videoRecorderService = null;
                //}
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
            // Get the filename from the Entry control
            var entry = sender as Entry;
            string filename = entry?.Text;
            _VideoKapture.OnFilenameCompleted(filename);

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

            // Store the filename for later use in OnButton_GetReady4Recording
            // No need to initialize camera or prepare recorder here
            System.Diagnostics.Debug.WriteLine($"Filename validated: {filename}");
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error validating filename: {ex.Message}");
            ShowMessage($"Error validating filename: {ex.Message}");
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
        }
    }

    private async void OnButton_CancelRecording_Clicked(object? sender, EventArgs e)
    {
        try
        {
            // Clean up resources
            await _VideoKapture.OnButton_CancelRecording_Clicked();
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

    private async void OnButton_StopRecordingClicked(object? sender, EventArgs e)
    {
        try
        {
            // Stop recording
            await _VideoKapture.OnButton_StopRecordingClicked();
            if(gunDateTime != DateTime.MinValue)
            {
                AppendGunTimeToVideoFilename();
                gunDateTime = DateTime.MinValue;
            }
            else
            {
                AppendTimeFromModePrefixToVideoFilename();
            }
            // Update UI state based on the current state of the recorder
            // The state will be updated by the RecordingStopped event handler
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error stopping recording: {ex.Message}");
            ShowMessage($"Error stopping recording: {ex.Message}");
        }
    }

    private void OnFrameRateCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (!e.Value) return; // Only process when a button is checked, not unchecked

        var radioButton = sender as RadioButton;
        if (radioButton == null) return;

        // Get the content of the radio button to identify it
        string content = radioButton.Content?.ToString() ?? "";
        
        if (content.Contains("30"))
        {
            _VideoKapture.SelectedFrameRate = 30; // Set frame rate to 30 FPS
            System.Diagnostics.Debug.WriteLine("Frame rate set to 30 FPS");
        }
        else if (content.Contains("60"))
        {
            _VideoKapture.SelectedFrameRate = 60;
            System.Diagnostics.Debug.WriteLine("Frame rate set to 60 FPS");
        }
        else if (content.Contains("Auto"))
        {
            _VideoKapture.SelectedFrameRate = 0; // Use 0 to indicate auto frame rate
            System.Diagnostics.Debug.WriteLine("Frame rate set to Auto");
        }
    }

    private void OnStabilizationModeChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (!e.Value) return; // Only process when a button is checked, not unchecked

        var radioButton = sender as RadioButton;
        if (radioButton == null) return;

        // Get the content of the radio button to identify it
        string content = radioButton.Content?.ToString() ?? "";
        
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

    private void AppendTimeFromModePrefixToVideoFilename()
    {
        var viewModel = (RecordingViewModel)this.BindingContext;
        var videoPath = _VideoKapture.VideoFilePath;
        if ((_VideoKapture == null) || (string.IsNullOrEmpty(videoPath)))
        {
            ShowMessage("Please start recording first to set gun time.");
            return;
        }
        if (!System.IO.File.Exists(videoPath))
        {
            throw new System.IO.FileNotFoundException($"The specified video file does not exist: {videoPath}");
        }

        string extension = videoPath.Substring(videoPath.Length - 4, 4); // Get the last 4 characters for extension check
        if ((string.IsNullOrEmpty(extension)) ||
            (!extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("The input file must be an MP4 video file.");
        }
        string outputPath = videoPath.Replace(".mp4", $"_{viewModel.TimeFromModePrefix}_.mp4", StringComparison.OrdinalIgnoreCase);

        System.IO.File.Copy(videoPath, outputPath, true);

    }

    DateTime gunDateTime = DateTime.MinValue; // Default gun time
    private void SetGunTime_Click(object sender, EventArgs e)
    {
        gunDateTime = DateTime.Now;
        var viewModel = (RecordingViewModel)this.BindingContext;
        viewModel.GunState = MauiAndroidCameraViewLib.GunStateEnum.Fired;// Update the gun time in the view model
        var xx = viewModel.IsStartGunVisible;
        var yy = viewModel.IsStartGunEnabled;
    }
    
    private void AppendGunTimeToVideoFilename()
    {
        var viewModel = (RecordingViewModel)this.BindingContext;
        var videoPath = _VideoKapture.VideoFilePath;
        if ((_VideoKapture == null) || (string.IsNullOrEmpty(videoPath)))
        {
            ShowMessage("Please start recording first to set gun time.");
            return;
        }
        if (!System.IO.File.Exists(videoPath))
        {
            throw new System.IO.FileNotFoundException($"The specified video file does not exist: {videoPath}");
        }
        string gunTimeString = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        gunTimeString = gunTimeString.Replace(":", "--");
        string extension = videoPath.Substring(videoPath.Length - 4, 4); // Get the last 4 characters for extension check
        if ((string.IsNullOrEmpty(extension)) ||
            (!extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("The input file must be an MP4 video file.");
        }
        string outputPath = videoPath.Replace(".mp4", $"_{viewModel.TimeFromModePrefix}_{gunTimeString}_.mp4", StringComparison.OrdinalIgnoreCase);

        System.IO.File.Copy(videoPath, outputPath, true);

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
                case "Start":
                    viewModel.TimeFromMode = MauiAndroidCameraViewLib.TimeFromMode.FromVideoStart;
                    break;
                case "Bang":
                    viewModel.TimeFromMode = MauiAndroidCameraViewLib.TimeFromMode.FromGunSound;
                    break;
                case "Flash":
                    viewModel.TimeFromMode = MauiAndroidCameraViewLib.TimeFromMode.FromGunFlash;
                    break;
                case "Manual":
                    viewModel.TimeFromMode = MauiAndroidCameraViewLib.TimeFromMode.ManuallySelect;
                    break;
                case "WC":
                    viewModel.TimeFromMode = MauiAndroidCameraViewLib.TimeFromMode.WallClockSelect;
                    break;
            }
        }
    }


}