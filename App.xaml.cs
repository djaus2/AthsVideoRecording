using MauiCameraViewSample;

namespace MauiCameraViewSample
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell()); // Your default main page

            // Example: Changing the main page dynamically
            window.Page = new MainPage();

            return window;
        }
    }
}

