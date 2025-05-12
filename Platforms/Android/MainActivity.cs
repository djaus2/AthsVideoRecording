using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Camera.Video;
using MauiAndroidCameraViewLib;

namespace MauiCameraViewSample
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState); // Ensure base initialization always occurs

            VideoKapture.RequestPermissionsStatic();
        }
    }
}
