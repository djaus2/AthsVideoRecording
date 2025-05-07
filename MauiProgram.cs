using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Camera;
//using MauiCameraViewSample.Services;
using Microsoft.Extensions.Logging;
//using MauiCameraViewSample.Platforms.Android;
//using MauiCameraViewSample.Platforms.Android;
using MauiAndroidCameraViewLib;

//[assembly: Dependency(typeof(MauiCameraViewSample.Platforms.Android.VideoRecorderService))];
namespace MauiAndroidVideoCaptureApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkitCamera()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddTransient<MainPage>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

#if ANDROID
            
builder.ConfigureMauiHandlers(handlers =>
{
    handlers.AddHandler(typeof(CameraPreviewView), typeof(MauiAndroidCameraViewLib.Platforms.Android.CameraPreviewHandler));
});/*
            // Register the AndroidVideoRecorderService
            builder.Services.AddSingleton<IVideoRecorderService, AndroidVideoRecorderService>();
            */
            MauiAndroidCameraViewLib.MauiCameraServicesSetup.ConfigureCameraServices(builder);
#endif


            return builder.Build();
        }
    }
}
