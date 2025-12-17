using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using Microsoft.Maui.Controls;

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



namespace AthsVideoRecording.Converters
{
    public class VisibilityToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Microsoft.Maui.Visibility v)
            {
                return v == Microsoft.Maui.Visibility.Visible;
            }
            // fallback: if already bool
            if (value is bool b) return b;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool b)
                return b ? Microsoft.Maui.Visibility.Visible : Microsoft.Maui.Visibility.Collapsed;
            return Microsoft.Maui.Visibility.Collapsed;
        }
    }
}
