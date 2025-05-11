# MauiMediaRecorderVideoAndroidApp

A test app for the following NuGet package.

## Uses Nuget Package
[NuGet: MauiMediaRecorderVideoLib](https://www.nuget.org/packages/djaus2_MauiMediaRecorderVideoLib)
, a .NET MAUI library for Android video recording using MediaRecorder with Camera Preview and Stabilization features.

> This package is a work in progress. The library is functional but the test app is not yet fully working in Release mode. _(Debug works.)_
---

## About MauiMediaRecorderVideoLib

A .NET MAUI library for Android video recording using MediaRecorder with camera preview and stabilization features.


> Nb This is a work in progress. The library is functional but the test app is not yet fully working in Release mode. _(Debug works)._


> **Update:** Have resolved issue to do with permssions. Now waits for the user to accept Camera before starting the camera preview. 
***~~Should now work in Release version of host app.~~***  
Only debug version of test app works.  
Note also: Audio permissions are not requested as not captured. Video only.  

> Nb: (Private Repository) The solution of test app plus this lib as one solution DOES work in Release mode.

### Test App Repository _(this)_
[djaus2/MauiMediaRecorderVideoAndroidApp](https://github.com/djaus2/MauiMediaRecorderVideoAndroidApp)

### About the library
This library is being developed to target a sporting Photoiming app. See [djaus2/PhotoTimingDjaus](https://github.com/djaus2/PhotoTimingDjaus)

### Features

- Full-screen camera preview
- Video recording with MediaRecorder
  - No audio recording
- Image stabilization options (Standard or Locked)
- Camera rotation support (0, 90, 180, 270 degrees)
- Configurable video FPS (30, 60, or default)
- Support for pausing and resuming recording
- Proper handling of Android permissions
- Screen dimensions detection for optimal preview

### Usage with a MAUI Android Phone App

#### Installation

Start by creating a new .NET MAUI project or using an existing one.

Install the package via NuGet:

```shell
dotnet add package MauiMediaRecorderVideoLib
```

Also need to install the following NuGet package:

```shell
dotnet add package CommunityToolkit.Maui.Camera
```

> **Nb:** CommunityToolkit.Maui.Camera is in the library but needs a reference in 
the App as it uses the Toolkit Preview.


#### Basic Setup

 
 1. Add the required permissions to your Android Manifest:  


```xml
<uses-permission android:name="android.permission.CAMERA" />
```

> **Note:** In app Android//MainActivity calls permissions setup via static 
method in library that iterates through in-app Android/Manifest permissions.

### The following needs updating (2Do).

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

### Requirements

- .NET MAUI project targeting Android
- Android API level 21 or higher
- Requires Android device with camera

