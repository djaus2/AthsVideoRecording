using Android.App;
using Android.Content.PM;
using Android.OS;

namespace MauiCameraViewSample
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
            protected override void OnCreate(Bundle? savedInstanceState)
            {
                base.OnCreate(savedInstanceState); // Ensure base initialization always occurs

                // Handle permissions asynchronously
                Task.Run(async () =>
                {
                    await Permissions.RequestAsync<Permissions.Camera>();
                    await Permissions.RequestAsync<Permissions.StorageWrite>();
                });
            }
    }
}
