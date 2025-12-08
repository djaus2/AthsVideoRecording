using System;
using Microsoft.Maui.Controls;

namespace AthsVideoRecording.Views
{
    public partial class ChangePasswordDialog : ContentPage
    {
        
        public string Username { get; set; } = "admin";
        public string CurrentPassword { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        private TaskCompletionSource<bool> _closeTcs;

        public ChangePasswordDialog()
        {
            InitializeComponent();
            _closeTcs = new TaskCompletionSource<bool>();
        }

        public Task<bool> WaitForCloseAsync()
        {
            return _closeTcs.Task;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // If you need a binding context set it here:
            // this.BindingContext = ...;
        }

        private async void Ok_Click(object sender, EventArgs e)
        {
            string currentPassword = CurrentPasswordEntry?.Text ?? string.Empty;
            string newPwd = NewPasswordEntry?.Text ?? string.Empty;
            string confirm = ConfirmPasswordEntry?.Text ?? string.Empty;
            Password = "";
            if (string.IsNullOrWhiteSpace(newPwd) || newPwd.Length < 12)
            {
                await DisplayAlert("Validation", "Password must be at least 12 characters.", "OK");
                return;
            }

            if (!string.Equals(newPwd, confirm, StringComparison.Ordinal))
            {
                await DisplayAlert("Change Password", "New password and confirmation do not match.", "OK");
                return;
            }

            // TODO: persist password
            Password = newPwd;
            CurrentPassword = currentPassword;
            _closeTcs.TrySetResult(true);
            await Navigation.PopModalAsync(); // close modal
        }

        private async void Cancel_Click(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}