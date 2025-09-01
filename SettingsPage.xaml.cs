using SendVideoOverTCPLib;
using SendVideoOverTCPLib.ViewModels;
using System.ComponentModel;

namespace MauiCameraViewSample
{
    public partial class SettingsPage : ContentPage
    {

        public SettingsPage()
        {
            InitializeComponent();
        }

        protected override async  void OnAppearing()
        {
            base.OnAppearing();;
            this.BindingContext = SendVideoOverTCPLib.SendVideo.NetworkViewModel;
        }


        private async void OnReturnToHomeClicked(object sender, EventArgs e)
        {
            if(this.BindingContext is NetworkViewModel )
            {
                var vm = (NetworkViewModel)this.BindingContext;
                var dl = vm.DownloadTimeoutInSec;
                var pt = vm.SelectedPort;
                SendVideoOverTCPLib.Settings.SaveSelectedSettings(vm);
            }
            await Navigation.PopModalAsync();

        }
    }
}
