# AthsVideoRecording  
- Note: Renamed from MauiMediaRecorderVideoAndroidApp

## BIG CHANGES HERE NOW!

An Android Maui App for video recording Athletcs (or similar sport) finish of a race and for sending it locally over TCP to a complementary Windows WPF app for **Photo Finish** processing.

>Still work in progress
Can get Meets-Events-Heats and select, including Event Guid  
Also now sent back (Event Guid and Heat Number).
---

## The Complementary WPF App/s

This receives the video file transmitted from this app:

- [AthStitcher](https://github.com/djaus2/PhotoTimingDjaus/tree/master/AthStitcher)  project from the repository [djaus2/PhotoTimingDjaus](https://github.com/djaus2/PhotoTimingDjaus) that does Photo Finish processing of video.
- Also simpler WPF app [TransferVideoOverTcp](https://github.com/djaus2/TransferVideoOverTcp)that only handles transfer from an Android MAUI app and reception by WPF app in repository: [djaus2/TransferVideoOverTcp](https://github.com/djaus2/TransferVideoOverTcp)
  - This app is used for testing the transfer protocol.
  - SendVideo  _(The Maui-Android phone app)_
    - Uses SendVideoOverTcpLib
    - An **updated** version of the lib is included here.
  - GetVideo _(The 2nd Complementary WPF app)_
    - Uses ReceiveVideoOverTcpLib
    - As used in the [AthStitcher](https://github.com/djaus2/PhotoTimingDjaus/tree/master/AthStitcher) App.

--- 

## This App Features

> ***Note In most the recent version of this app, meta-information is no longer passed from recording to transmission
as appendages to the filename but is added as a json string as a video file comment at the end of recording. 
This is then recovered prior to transmission and used to recreate the VideoInfo for transmission prior to the video's transmission.***
> Note: This has been reversed for the moment to get Event Guid and Heat Number from filename when sending video back. 2Do.

### Buttons at Bottom
- Now fully functional. Have captions.
- Note when a Mee-Event-Heat is selected the video filename is set automatically and can't be changed manually until app is restarted.
  - Can select another Meet-Event-Heat on ProgramPage though which changes the video filename.
  - Meet-Event-Heat now shows and manual filename entry is hidden.


### Meet-Event-Heat Selection  
- Now done on the ProgramPage _(Currently from Crosshairs icon center bottom)_
- From where programs can also be uploaded from AppStitcher.
- When so selected the video filename is automatically set and can't be 
manually changed, until app is rebooted.
- Can select another Meet-Event-Heat on ProgramPage though which changes the video filename.

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
- At transmission the video's metadata is extracted and used to create a VideoInfo object which is then transmitted.
  - Filename, Filepath, Filesize, Checksum
  - TimeFromMode used for the recording
  - DateTime of recording
  - Start time of event (based on TimeFromMode setting)
  - Gun time if TimeFromMode is WallClockSelect

- The WPF then uses the Filename to store the transmitted data an dthe checksum to verify it. 
 Other meta-info is then subsequently used as required.

### Transmission

- "Paper Airplane" button to send the recorded video file over TCP locally to a Windows WPF app.
- Can pick a video for transmission from sorted (latest first) list of videos from the Movies folder.
- Configurable IP address and port for the receiving app.
- Status messages to indicate connection status and transmission progress.
- Error handling for connection issues and transmission failures.
- Checksum and filename automatically transmitted as part of the protocol.
- No auto trasmit after recording. 2Do?

## Custom NuGet packages used
- [Sportronics.MauiMediaRecorderVideoLib](https://www.nuget.org/packages/Sportronics.MauiMediaRecorderVideoLib/)
  - A .NET MAUI library for Android video recording using MediaRecorder with Camera Preview and Stabilization features
- [djaus2MauiCountdownToolkit](https://www.nuget.org/packages/djaus2MauiCountdownToolkit/)
  - A .NET MAUI library for countdown popup with customizable appearance and behavior
- ~~[Sportronics.SendVideoOverTcpLib](https://www.nuget.org/packages/Sportronics.SendVideoOverTcpLib)~~ 
  - ~~Handles the TCP Video transmission to the WPF app~~ ...Now directly included in this repo as updated version.
- [Sportronics.Sportronics.VideoEnums](https://www.nuget.org/packages/Sportronics.VideoEnums) as required by SendVideoOverTcpLib to handle VideoInfo processing and provides app enums.
## Usage

Clone and build the repository targeting an Android phone. Deploy and run.  Need to accept permissions (2).
First time might need to restart after accepting permissions.
Need the e WPF app for receiving the video.


## TimeFromMode Setting

- **TimeFromMode Property** to VideoRecorderService allows for different ways of determining the start time of the event.
  - As above included in VideoInfo and transmitted to the WPF app.
  - The WPF app app uses this to determine how to determine the start time of the event.
  - For **WallClock** mode, the gun time is also included in VideoInfo
    - Note: Gun only shows if **TimeFromMode** is set to **WallClock** when ready to record. 
    - Can be pressed before or during video recording.
- TimeFromModes: 
```csharp
        TimeFromMode.FromVideoStart ,
        TimeFromMode.FromGunSound ,
        TimeFromMode.FromGunFlash ,
        TimeFromMode.ManuallySelect ,
        TimeFromMode.WallClockSelect 
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

