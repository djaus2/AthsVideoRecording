using CommunityToolkit.Maui;
//using CommunityToolkit.Maui.Camera;
using Microsoft.Extensions.Logging;
using MauiAndroidCameraViewLib;
using SendVideoOverTCPLib.Services;
using AthsVideoRecording.Data;
using AthsVideoRecording.Views;



#if ANDROID
using SendVideoOverTCPLib.Platforms.Android;
#endif

namespace AthsVideoRecording
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            //string dbPath = AppDatabase.GetDefaultDbPath();
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

            builder.Services.AddTransient<SendVideoOverTCPLib.ViewModels.NetworkViewModel>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<SendVideoPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<ProgramPage>();

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
            // Register Android-specific newest-first video metadata/picker service
            builder.Services.AddSingleton<IVideoMetadataService, VideoMetadataService>();
            //builder.Services.AddSingleton(new AppDatabase(dbPath));
#endif


            return builder.Build();
        }
    }
}
