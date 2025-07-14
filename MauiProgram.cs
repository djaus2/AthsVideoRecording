using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Camera;
using Microsoft.Extensions.Logging;
using MauiAndroidCameraViewLib;

namespace MauiCameraViewSample
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
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
