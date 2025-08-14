# AthsVideoRecording  
- Note renamed fromMauiMediaRecorderVideoAndroidApp

A test app for the following NuGet package. V3.0.1
[djaus2_MauiMediaRecorderVideoLib](https://www.nuget.org/packages/djaus2_MauiMediaRecorderVideoLib/)

Also uses Nuget Package V1.0.6 [djaus2MauiCountdownToolkit](https://www.nuget.org/packages/djaus2MauiCountdownToolkit/)

## NB 2025-05-29
- See note wrt FPS below

## Updates Summary of late
- Several options for determining the event start time, wrt the video start time

## Popup Icon issue 2025-07-19
- Can use Icon from app but not Toolkit embedded versions
  - 2 Nuget packages tried. Reverting at Version 1.1.6 where app icon works
## Update 2025-07-19
Major bug fix (sorry) in the app that caused the app to crash when trying to run detached from VS.
- ```EnumEqualsConverter``` went missing! Fixed. 
## Update: 2025-07-16
- Numerous updates including with 2 NuGet packages it uses.
  - In TimeFromMode WallClockSelect
    - These features only show in this mode.
    - Can auto start video recording after a period
      - 3 Modes for that:
        - Soft: Just handled by _VideoKapture as UI less delay
          - Can be cancelled by pressing video start (white) button.
          - ***ATM it does this delay in both packages.*** 2Do
        - Red and Rainbow:  A popup with Cancel button
          - Red: Popup border is Red. 2Do make color selectable
          - Rainbow: Rainbow border, as below. _Nice!_
     - There is a start gun icon. Race times are wrt to this.  
![Countdown Popup](https://raw.githubusercontent.com/djaus2/MauiCountdownToolkit/master/Popup1.png)

## Update: 2025-07-06
- Added **TimeFromMode property** to VideoRecorderService to allow for different ways of determining the start time of the event.
  - Appends a text string for **TimeFromMode** to the video filename.
  - **AthStitcher** app uses this to determine the start time of the event and parse it to the video file **Title** property, removing that text from the filename.
  - For **WallClock** mode, the gun time is also appended that AthStitcher parses to gun WallClock time and sets as Video file **Comment** property, _also_ removing that text from the filename.
  - Note: Gun only shows if **TimeFromMode** is set to **WallClock** when ready to record. 
    - Can be pressed before or during video recording.
- _2Do Insert those properties in the capture library so that the video file has those properties set._
- Text appended: (Note: _ is added before and after each text string:
```cs
        TimeFromMode.FromVideoStart => "VIDEOSTART",
        TimeFromMode.FromGunSound => "GUNSOUND",
        TimeFromMode.FromGunFlash => "GUNFLASH",
        TimeFromMode.ManuallySelect => "MANUAL",
        TimeFromMode.WallClockSelect => "WALLCLOCK"
```

## Update: 2025-05-14

- **Found that it works in Release mode if AOT is disabled!  
_Both Bundle AND APK builds_**  
Bundle-No AOT config here for Release config.

## Uses Nuget Package
[NuGet: MauiMediaRecorderVideoLib](https://www.nuget.org/packages/djaus2_MauiMediaRecorderVideoLib)
, a .NET MAUI library for Android video recording using MediaRecorder with Camera Preview and Stabilization features.

---

## About MauiMediaRecorderVideoLib

> Nb: Video is stored /Movies folder. Filename is juxtaposed with date. Can optionally also optionally 
start time to the the video filename which this app can parse and use.

A .NET MAUI library for Android video recording using MediaRecorder with camera preview and stabilization features.

Latest Version: 2.0.1

> Nb This is a work in progress. The library is functional but the test app is not yet fully working in Release mode. _(Debug works)._

## The Test App  _(here)_

Clone this repository, build and deploy to an Android phone.  
_(Was tested on a Pixel 6 phone)_

Change the UI as you wish.

---

### About the library
The library is being developed to target a sporting Photoiming app. See [djaus2/PhotoTimingDjaus](https://github.com/djaus2/PhotoTimingDjaus)

### Features

- Full-screen camera preview
- Video recording with MediaRecorder
  - No audio recording
- Image stabilization options (Standard or Locked)
- Camera rotation support (0, 90, 180, 270 degrees)
- Configurable video FPS (30, 60, or default)
   - Note for 60 FPS with Google Pixel phone need to:
      - Camera Settings
      - Advanced
      - Turn off Store videos efficiently
      - _Probably best to do that for 30 FPS as well otherwise get dynamic FPS_
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

