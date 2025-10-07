# Sportronics.SendVideoOverTcpLib

A MAUI library for transferring video over TCP from phone to Windows WPF complementary app.

## Overview

SendVideoOverTcpLib is an Android library that enables MAUI applications to send video files over TCP connections. It's primarily designed for Android devices to transfer video files to matching Windows desktop applications.  See repository for the project **GetVideoWPFLibSample**. Also used in the project **AthsVideoRecording** in the the repository [djaus2/AthsVideoRecording](https://github.com/djaus2/AthsVideoRecording).

## Features

- Send video files from mobile devices to desktop applications
- Reliable TCP-based file transfer
- Checksum verification for data integrity
- Configurable timeout settings
- Cross-platform support (Android, iOS, Windows, MacCatalyst, Tizen)

## Installation

```
dotnet add package Sportronics.SendVideoOverTcpLib
```

## Usage

```csharp
using SendVideoOverTcpLib;

// Create an instance of the SendVideo class
var sender = new SendVideo();

// Configure settings
sender.Port = 5000;
sender.TimeoutSeconds = 15;

// Send a video file
await sender.SendVideoAsync(filePath, ipAddress);
```

## Requirements

- .NET 9.0 or later
- Supported platforms:
  - Android 21.0 or later
  - iOS 14.2 or later
  - MacCatalyst 14.0 or later
  - Windows 10.0.17763.0 or later
  - Tizen 6.5 or later

## Dependencies

- CommunityToolkit.Mvvm
- Microsoft.Maui.Controls

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Related Projects

This library is part of the TransferVideoOverTcp solution, which includes:
- SendVideo - MAUI Android app that uses this library
- DownloadVideoOverTcpLib - Library for receiving video files
- GetVideoConsoleApp - Console app for receiving videos
- GetVideoInAppWPF - WPF app for receiving videos
- GetVideoService - Windows service for receiving videos
