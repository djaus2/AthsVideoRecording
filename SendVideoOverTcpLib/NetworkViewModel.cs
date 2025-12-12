using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using CommunityToolkit.Mvvm.ComponentModel;


namespace SendVideoOverTCPLib.ViewModels
{
    public partial class NetworkViewModel : ObservableObject
    {
        [ObservableProperty]
        private string selectedIpAddress = "";

        public bool IsIpSelected => !string.IsNullOrEmpty(SelectedIpAddress);

        partial void OnSelectedIpAddressChanged(string oldValue, string newValue)
        {
            OnPropertyChanged(nameof(IsIpSelected));
        }

        [ObservableProperty]
        private int selectedPort = 5000;

        [ObservableProperty]
        private int selectedProgramUploadPort = 1000;

        [ObservableProperty]
        private int startHostId = 2;

        [ObservableProperty]
        private int endHostId = 20;

        [ObservableProperty]
        private int pingTimeoutInHalfSeconds = 6;

        [ObservableProperty]
        private int pingTimeoutInMs = 500;

        public int PingTimeOut { get { return PingTimeoutInHalfSeconds * PingTimeoutInMs; } }

        [ObservableProperty]
        private ObservableCollection<string> activeIPs = new();

        [ObservableProperty]
        private int downloadTimeoutInSec = 15;

        [ObservableProperty]
        private string videoInfoPathJosnStr = "";

        [ObservableProperty]
        private bool newDatabase = false;

        /*
        public ObservableCollection<string> ActiveIPs
        {
            get => activeIPs;
            set => SetProperty(ref activeIPs, value);
        }


        public string SelectedIP    
        {
            get => selectedIP;
            set => SetProperty(ref selectedIP, value);
        }

        public int SelectedPort
        {
            get => selectedPort;
            set => SetProperty(ref selectedPort, value);
        }

        public int MinIp    
        {
            get => minIp;
            set => SetProperty(ref minIp, value);
        }

        public int MaxIp
        {
            get => maxIp;
            set => SetProperty(ref maxIp, value);
        }*/
    }
}

