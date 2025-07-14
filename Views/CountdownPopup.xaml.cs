using CommunityToolkit.Maui.Views;

namespace MauiAndroidVideoCaptureApp.Views;

public partial class CountdownPopup : Popup
{
    private int _secondsRemaining;
    private IDispatcherTimer _timer;
    private TaskCompletionSource<bool> _tcs;

    public Task<bool> Result => _tcs.Task;

    public CountdownPopup(int seconds)
    {
        InitializeComponent();
        _secondsRemaining = seconds;
        _tcs = new TaskCompletionSource<bool>();

        _timer = Application.Current.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private void OnTick(object sender, EventArgs e)
    {
        if (_secondsRemaining > 0)
        {
            CountdownLabel.Text = $"{_secondsRemaining--} seconds";
        }
        else
        {
            _timer.Stop();
            _tcs.TrySetResult(true); // Countdown completed
            Close();
        }
    }

    public void Cancel()
    {
        _timer.Stop();
        _tcs.TrySetResult(false); // User cancelled
        Close();
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        _timer?.Stop();
        _tcs.TrySetResult(false); // User cancelled
        Close();
    }

}

