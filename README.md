# AthsVideoRecording  
- Note: Renamed from MauiMediaRecorderVideoAndroidApp

An Android Maui App for video recoding Athletcs (or similar sport) finish of a race and for sending it locally over TCP to a complementary Windows WPF app for **Photo Finish** processing.

## The Complementary WPF App

- [AthStitcher](https://github.com/djaus2/PhotoTimingDjaus/tree/master/AthStitcher)  project from the repository [djaus2/PhotoTimingDjaus](https://github.com/djaus2/PhotoTimingDjaus)
- Also simpler WPF app that only handles reception: [TransferVideoOverTcp](https://github.com/djaus2/TransferVideoOverTcp/tree/master/GetVideoWPFLibSample) in repository [djaus2/TransferVideoOverTcp](https://github.com/djaus2/TransferVideoOverTcp)

## App Features

### Recording

- Full-screen camera preview
- Video recording with MediaRecorder
  - Audio can be used for GunFire race start detection.
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
- Screen dimensions detection for optimal preview.

### Transmission

- "Paper Airplane" button to send the recorded video file over TCP locally to a Windows WPF app.
- Configurable IP address and port for the receiving app.
- Status messages to indicate connection status and transmission progress.
- Error handling for connection issues and transmission failures.
- Checksum and filename automatically transmitted as part of the protocol.

## Custom NuGet packages used
- [djaus2_MauiMediaRecorderVideoLib](https://www.nuget.org/packages/djaus2_MauiMediaRecorderVideoLib/)
  - A .NET MAUI library for Android video recording using MediaRecorder with Camera Preview and Stabilization features
- [djaus2MauiCountdownToolkit](https://www.nuget.org/packages/djaus2MauiCountdownToolkit/)
  - A .NET MAUI library for countdown popup with customizable appearance and behavior
- [Sportronics.SendVideoOverTcpLib](https://www.nuget.org/packages/Sportronics.SendVideoOverTcpLib)
  - Handles the TCP Video transmission to the WPF app

## Usage

Clone and build the repository targeting an Android phone. Deploy and run.  Need to accept permissions (2).
First time might need to restart after accepting permissions.
Need the e WPF app for receiving the video.


## TimeFromMode Setting

- **TimeFromMode Property** to VideoRecorderService allows for different ways of determining the start time of the event.
  - Appends a text string for **TimeFromMode** to the video filename.
  - The WPF app app uses this to determine the start time of the event and parse it to the video file **Title** property, removing that text from the filename.
  - For **WallClock** mode, the gun time is also appended that WPF app parses to gun WallClock time and sets as Video file **Comment** property, _also_ removing that text from the filename.
  - Note: Gun only shows if **TimeFromMode** is set to **WallClock** when ready to record. 
    - Can be pressed before or during video recording.
  - Would like to insert those properties into the video file before transmission, but adding info to the filename works. 
   _The Windows app does parse that info in the filename and add it as file properties, removing it from the filename._
- Text appended: _(Note: an underscore is added before and after each text string when prepended to the filename._
```cs
        TimeFromMode.FromVideoStart => "VIDEOSTART",
        TimeFromMode.FromGunSound => "GUNSOUND",
        TimeFromMode.FromGunFlash => "GUNFLASH",
        TimeFromMode.ManuallySelect => "MANUAL",
        TimeFromMode.WallClockSelect => "WALLCLOCK"
```

## Other
- In TimeFromMode WallClockSelect
_(These features only show in this mode.)_
  - Can auto start video recording after a period
    - 3 Modes for that:
    - Soft: Just handled by automatically by VideoKapture as UI-less delay
        - Can be cancelled by pressing video start (white) button.
        - ***ATM it does this delay in both packages.*** 2Do
    - Red and Rainbow:  A popup with Cancel button
        - Red: Popup border is Red. 2Do make color selectable
        - Rainbow: Rainbow border, as below. _Nice!_
    - There is a start gun icon. Race times are wrt to this.  
![Countdown Popup](https://raw.githubusercontent.com/djaus2/MauiCountdownToolkit/master/Popup1.png)


 ---

 ## Notes

### Popup Icon issue 2025-07-19
- Can use Icon from app but not Toolkit embedded versions
  - 2 Nuget packages tried. Reverting at Version 1.1.6 where app icon works

## Update: 2025-05-14

- **Found that it works in Release mode if AOT is disabled!  
_Both Bundle AND APK builds_**  
Bundle-No AOT config here for Release config.

## The MauiMediaRecorderVideoLib

> Nb: Video is stored /Movies folder. Filename is juxtaposed with date. Can optionally also optionally 
start time to the the video filename which this app can parse and use.

### About the library
The library has being developed to target a this app.



#### Library Installation

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

