using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiAndroidVideoCaptureApp
{
    // Have similar enum in two libs so want to convert between them
    public static class CountDownModeTranslator
    {
        public static MauiCountdownToolkit.CountDownMode ToToolkit(MauiAndroidCameraViewLib.CountDownMode source)
        {
            return source switch
            {
                MauiAndroidCameraViewLib.CountDownMode.None => MauiCountdownToolkit.CountDownMode.None,
                MauiAndroidCameraViewLib.CountDownMode.Soft => MauiCountdownToolkit.CountDownMode.Soft,
                MauiAndroidCameraViewLib.CountDownMode.PopupRed => MauiCountdownToolkit.CountDownMode.PopupRed,
                MauiAndroidCameraViewLib.CountDownMode.PopupRainbow => MauiCountdownToolkit.CountDownMode.PopupRainbow,
                _ => throw new ArgumentOutOfRangeException(nameof(source), $"Unsupported value: {source}")
            };
        }

        public static MauiAndroidCameraViewLib.CountDownMode ToCameraView(MauiCountdownToolkit.CountDownMode source)
        {
            return source switch
            {
                MauiCountdownToolkit.CountDownMode.None => MauiAndroidCameraViewLib.CountDownMode.None,
                MauiCountdownToolkit.CountDownMode.Soft => MauiAndroidCameraViewLib.CountDownMode.Soft,
                MauiCountdownToolkit.CountDownMode.PopupRed => MauiAndroidCameraViewLib.CountDownMode.PopupRed,
                MauiCountdownToolkit.CountDownMode.PopupRainbow => MauiAndroidCameraViewLib.CountDownMode.PopupRainbow,
                _ => throw new ArgumentOutOfRangeException(nameof(source), $"Unsupported value: {source}")
            };
        }

        public static MauiCountdownToolkit.CountDownMode ParseToolkit(string text)
        {
            return text.ToLowerInvariant() switch
            {
                "none" => MauiCountdownToolkit.CountDownMode.None,
                "soft" => MauiCountdownToolkit.CountDownMode.Soft,
                "red" => MauiCountdownToolkit.CountDownMode.PopupRed,
                "rainbow" => MauiCountdownToolkit.CountDownMode.PopupRainbow,
                _ => throw new ArgumentException($"Invalid countdown mode string: '{text}'", nameof(text))
            };
        }

        public static MauiAndroidCameraViewLib.CountDownMode ParseCameraView(string text)
        {
            return text.ToLowerInvariant() switch
            {
                "none" => MauiAndroidCameraViewLib.CountDownMode.None,
                "soft" => MauiAndroidCameraViewLib.CountDownMode.Soft,
                "red" => MauiAndroidCameraViewLib.CountDownMode.PopupRed,
                "rainbow" => MauiAndroidCameraViewLib.CountDownMode.PopupRainbow,
                _ => throw new ArgumentException($"Invalid countdown mode string: '{text}'", nameof(text))
            };
        }
    }

}
