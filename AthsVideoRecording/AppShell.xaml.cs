namespace AthsVideoRecording
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            try
            {
                InitializeComponent();
                // ... your code ...
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AppShell error: {ex}");
            }
        }
    }
}


