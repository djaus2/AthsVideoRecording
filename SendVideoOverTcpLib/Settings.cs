using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SendVideoOverTCPLib.ViewModels;

namespace SendVideoOverTCPLib;

public static class Settings
{
    public static void SaveSelectedSettings(NetworkViewModel networkViewModel)
    {
        Preferences.Set("SelectedIpAddress", networkViewModel.SelectedIpAddress);
        Preferences.Set("Port", networkViewModel.SelectedPort);
        Preferences.Set("StartHostId", networkViewModel.StartHostId);
        Preferences.Set("EndHostId", networkViewModel.EndHostId);
        Preferences.Set("TimeoutInHalfSeconds", networkViewModel.PingTimeoutInHalfSeconds);
        Preferences.Set("TimeoutInMs", networkViewModel.PingTimeoutInMs);
        Preferences.Set("DownloadTimeoutInSec", networkViewModel.DownloadTimeoutInSec);
    }

    public static NetworkViewModel GetSettingNetworkViewModel()
    {
        var networkViewModel = new NetworkViewModel
        {
            SelectedIpAddress = GetSettingSelectedIPAddress(),
            SelectedPort = GetSettingSelectedPort() > 0 ? GetSettingSelectedPort() : 5000,
            StartHostId = GetSettingStartHostId(),
            EndHostId = GetSettingEndHostId(),
            PingTimeoutInHalfSeconds = GetSettingTimeoutInHalfSeconds(),
            PingTimeoutInMs = GetSettingTimeoutInMs(),
            DownloadTimeoutInSec = GetSettingDownloadTimeoutInSec()
        };
        return networkViewModel;
    }


    // using System.Net.NetworkInformation;

    public static int GetSettingSelectedPort()
    {
        return Preferences.Get("Port", 5000);
    }

    public static int GetSettingStartHostId()
    {
        return Preferences.Get("StartHostId", 2);
    }

    public static int GetSettingEndHostId()
    {
        return Preferences.Get("EndHostId", 20);
    }

    public static void SaveHostIdRange(int startHostId, int endHostId)
    {
        Preferences.Set("StartHostId", startHostId);
        Preferences.Set("EndHostId", endHostId);
    }

    public static string GetSettingSelectedIPAddress()
    {
        return Preferences.Get("SelectedIpAddress", string.Empty);
    }



    public static int GetSettingTimeoutInHalfSeconds()
    {
        return Preferences.Get("TimeoutInHalfSeconds", 6);
    }

    public static int GetSettingTimeoutInMs()
    {
        return Preferences.Get("GTimeoutInMs", 500);
    }
    public static int GetSettingDownloadTimeoutInSec()
    {
        return Preferences.Get("DownloadTimeoutInSec", 15);
    }

    public static void ClearAllPreferences()
    {
        Preferences.Clear();
    }

    public static void ClearPreferences()
    {
        int startHostId = GetSettingStartHostId();
        int endHostId = GetSettingEndHostId();
        int downloadTimeoutInSec = GetSettingDownloadTimeoutInSec();
        Preferences.Clear();
        Preferences.Set("StartHostId", startHostId);
        Preferences.Set("EndHostId", endHostId);
        Preferences.Set("DownloadTimeoutInSec", downloadTimeoutInSec);
    }
}
