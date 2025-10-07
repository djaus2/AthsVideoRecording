using CommunityToolkit.Maui.Views;
using AthsVideoRecording;

namespace AthsVideoRecording
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                System.Diagnostics.Debug.WriteLine($"[AppDomain] Crash: {ex?.Message}");
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"[TaskScheduler] Unobserved: {e.Exception.Message}");
                e.SetObserved(); // Prevent crashing the app
            };

        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Start with splash screen
            var splash = new SplashPage();
            var window = new Window(splash);

            // Use ConfigureAwait(false) to avoid deadlocks
            Task.Run(async () => 
            {
                try 
                {
                    // Give splash screen time to render
                    await Task.Delay(1000).ConfigureAwait(false);
                    
                    // Switch to main UI thread for UI operations
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        try
                        {
                            // Create AppShell instance first to ensure it's fully initialized
                            var appShell = new AppShell();
                            
                            // Then set it as the page
                            window.Page = appShell;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error setting main page: {ex.Message}");
                            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                        }
                    }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Fatal error during app initialization: {ex}");
                }
            });

            return window;
        }
    }
}
