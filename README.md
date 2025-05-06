# MauiMediaRecorderVideoLib

## Uses Nuget Package
[NuGet: MauiMediaRecorderVideoLib](https://www.nuget.org/packages/djaus2_MauiMediaRecorderVideoLib)
, a .NET MAUI library for Android video recording using MediaRecorder with Camera Preview and Stabilization features.

## Features

- Full-screen camera preview
- Video recording with MediaRecorder
- Image stabilization options (Standard or Locked)
- Camera rotation support (0, 90, 180, 270 degrees)
- Configurable video FPS (30, 60, or default)
- Support for pausing and resuming recording
- Proper handling of Android permissions
- Screen dimensions detection for optimal preview

## Installation

You can install the package via NuGet:

```shell
dotnet add package MauiMediaRecorderVideoLib
```

## Usage

This app implements the following.
> **Nb:** The app only works in Debug mode when run from Visual Studio.

### Basic Setup

 1. Add the required permissions to your Android Manifest:

```xml
<uses-permission android:name="android.permission.CAMERA" />
<uses-permission android:name="android.permission.RECORD_AUDIO" />
<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
```

  2. Initialize the video recorder service:

  ```csharp
  using MauiMediaRecorderVideoLib;

// Initialize the service
var videoRecorderService = new AndroidVideoRecorderService();
await videoRecorderService.InitializeAsync();
```

 3. Start the camera preview:
```csharp
// Assuming you have a GraphicsView named 'cameraPreview' in your XAML
videoRecorderService.SetPreviewSurface(cameraPreview);
```

 4. Start recording:

```csharp
// Configure recording options
videoRecorderService.SetVideoFPS(30); // Optional: Set FPS
videoRecorderService.SetImageStabilization(StabilizationMode.Locked); // Optional: Set stabilization

// Start recording

string outputFilePath = Path.Combine(FileSystem.CacheDirectory, "myVideo.mp4");
await videoRecorderService.StartRecordingAsync(outputFilePath);
```
 5. Control recording:
```csharp
// Pause recording
await videoRecorderService.PauseRecordingAsync();

// Resume recording
await videoRecorderService.ResumeRecordingAsync();

// Stop recording
await videoRecorderService.StopRecordingAsync();
```
 6. Clean up resources:
 ```csharp
 // Release resources when done
await videoRecorderService.CleanupAsync();
```

## Requirements

- .NET MAUI project targeting Android
- Android API level 21 or higher
- Requires Android device with camera (and ??microphone?? ,, not used.)

