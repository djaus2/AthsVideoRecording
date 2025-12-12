using Android.Views;
using AthsVideoRecording.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using SendVideoOverTCPLib.Receiver;
using SendVideoOverTCPLib.ViewModels;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Xamarin.Google.Crypto.Tink;
using Xamarin.JSpecify.Annotations;

namespace AthsVideoRecording.Views
{

    public partial class Meets : ObservableObject
    {
        // toolkit generated property (backing field is private)
        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display)), NotifyPropertyChangedFor(nameof(EventsCollection)), NotifyPropertyChangedFor(nameof(GetEventsIsEnabled))]
        private Meet? selectedMeet;

        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display)),
            NotifyPropertyChangedFor(nameof(NextPrevHeatViz)),
            NotifyPropertyChangedFor(nameof(EventsCollection)), 
            NotifyPropertyChangedFor(nameof(GetEventsIsEnabled))]
        private Event? selectedEvent;

        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display)), NotifyPropertyChangedFor(nameof(EventsCollection)), NotifyPropertyChangedFor(nameof(GetEventsIsEnabled))]
        private int selectedHeat = 1;

        public bool NextPrevHeatViz => (SelectedEvent != null) && (SelectedEvent.NumHeats > 1);

        [ObservableProperty, NotifyPropertyChangedFor(nameof(GetEventsIsEnabled))]
        private System.Collections.ObjectModel.ObservableCollection<Meet> meetsCollection
            = new System.Collections.ObjectModel.ObservableCollection<Meet>();

        [ObservableProperty, NotifyPropertyChangedFor(nameof(GetEventsIsEnabled))]
        private System.Collections.ObjectModel.ObservableCollection<Event> eventsCollection
            = new System.Collections.ObjectModel.ObservableCollection<Event>();

        [ObservableProperty, NotifyPropertyChangedFor(nameof(GetEventsIsEnabled))]
        private System.Collections.ObjectModel.ObservableCollection<Event> eventsForMeetCollection
        = new System.Collections.ObjectModel.ObservableCollection<Event>();

        public string FileName => "{SelectedEvent.MeetId}_{HeatNumber}}";
        public string EventStr => SelectedEvent != null ? $"{SelectedEvent.Description}:{SelectedHeat}" : "No Event Selected";  

        public bool GetEventsIsEnabled => (MeetsCollection.Count() > 0);

        [JsonIgnore]
        [NotMapped]
        public string Display => ToString();

        public override string ToString() => ""; // SelectedMeet != null ? $"{SelectedMeet.Display}" : "No Meet Selected";

        // Event raised whenever SelectedMeet changes
        public event EventHandler<SelectedMeetChangedEventArgs>? SelectedMeetChanged;

        // EventArgs type carrying old and new selection
        public sealed class SelectedMeetChangedEventArgs : EventArgs
        {
            public Meet? OldSelected { get; }
            public Meet? NewSelected { get; }

            public SelectedMeetChangedEventArgs(Meet? oldSelected, Meet? newSelected)
            {
                OldSelected = oldSelected;
                NewSelected = newSelected;
            }
        }

        public void NextHeat()
        {
            int numHeats = SelectedEvent.NumHeats;
            if(SelectedHeat < numHeats)
            {
                SelectedHeat++;
                return;
            }
            else 
            {
                //Could increment to next event here
            }
        }

        public void PrevHeat()
        {
            int numHeats = SelectedEvent.NumHeats;
            if (SelectedHeat > 1)
            {
                SelectedHeat--;
                return;
            }
            else
            {
                {
                    //Could decrement to previous event here
                }
            }
        }

            // Called by the source-generated setter when SelectedMeet changes.
            partial void OnSelectedMeetChanged(Meet? oldValue, Meet? newValue)
            {
                if (newValue != null)
                {
                    SelectedMeet = newValue;
                    string newMeetId = SelectedMeet.ExternalId;
                    EventsForMeetCollection = new System.Collections.ObjectModel.ObservableCollection<Event>();

                    if (!string.IsNullOrEmpty(newMeetId))
                    {
                        // Guard against Event.Meet being null before accessing Meet.ExternalId
                        var eventsForMeetList = EventsCollection
                            .Where(ev => ev != null && ev.Meet != null &&(!string.IsNullOrEmpty(ev.Meet.ExternalId)) && ev.Meet.ExternalId == newMeetId)
                            .OrderBy(ev => ev.EventNumber);

                        //var llst = eventsForMeetList.ToList<Event>();
                            foreach (var ev in eventsForMeetList)
                                EventsForMeetCollection.Add(ev);
                    }
                }
                SelectedHeat = 1;
                // raise event for subscribers
                SelectedMeetChanged?.Invoke(this, new SelectedMeetChangedEventArgs(oldValue, newValue));
            }
        }

    public partial class ProgramPage : ContentPage
    {
        public ProgramPage()
        {
            InitializeComponent();
            
        }

        // Can get selections back on MainPage via ProgramPage._Meets.SelectedMeet etc.
        public static Meets _Meets { get; set; }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            ;
            grid.IsVisible = false;
            BusyIndicatorLabel.Text = $"Getting saved Target Host ID or Local active Ids to select from if no setting (slower).";
            BusyIndicatorLabel.IsVisible = true;
            BusyIndicator.IsVisible = true;
            BusyIndicator.IsRunning = true;
            if (SendVideoOverTCPLib.SendVideo.NetworkViewModel == null) //Returning to this page
            {
                var ipaddress = await SendVideoOverTCPLib.SendVideo.GetSettings();
            }
            _Meets = new Meets();
            LoadMeets();
            LoadEvents();

            this.BindingContext = _Meets;
            BusyIndicator.IsRunning = false;
            BusyIndicator.IsVisible = false;
            BusyIndicatorLabel.IsVisible = false;
            grid.IsVisible = true;
        }

        private void LoadMeets()
        {
            using (var ctx = new AthsVideoRecording.Data.AthsVideoRecordingDbContext())
            {
                var meets = ctx.Meets
                               .AsNoTracking()
                               .OrderByDescending(m => m.Date)
                               .ToList();

                _Meets.MeetsCollection.Clear();
                foreach (var m in meets)
                    _Meets.MeetsCollection.Add(m);
            }
        }

        private void LoadEvents()
        {
            using (var ctx = new AthsVideoRecording.Data.AthsVideoRecordingDbContext())
            {
                // Eagerly load the Meet navigation so Event.Meet is populated (avoid lazy-loading / disposed context issues)
                var events = ctx.Events
                                .Include(e => e.Meet)
                                .AsNoTracking()
                                .OrderByDescending(e => e.Meet.Date)
                                .ThenByDescending(e => e.Time)
                                .ToList();

                _Meets.EventsCollection.Clear();
                foreach (var e in events)
                    _Meets.EventsCollection.Add(e);
            }
        }


        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Do not clear the cached list here so it persists across page views.
        }

        // Call this method if you must force the Picker to re-evaluate its ItemsSource binding.
        public void ForceRefreshMeetPicker()
        {
            if (MeetPicker == null) return;
            var src = MeetPicker.ItemsSource;
            MeetPicker.ItemsSource = null;
            MeetPicker.ItemsSource = src;
        }




        private async void Done_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private async void Settings_Clicked(object sender, EventArgs e)
        {
            var serviceProvider = IPlatformApplication.Current.Services;
            var modalPage = serviceProvider.GetRequiredService<SettingsPage>();
            await Navigation.PushModalAsync(modalPage);
        }

        private async void GetProgram_Clicked(object sender, EventArgs e)
        {
            string caption = "";
            if (sender is Button)
            {
                caption = ((Button)sender).Text;
            }
            if(string.IsNullOrEmpty(caption))
            {
                return;
            }
            bool found = false;
            if(caption=="Get Meets")
            {
                found = true;
            }
            else if (caption == "Get Events")
            {
                found = true;
            }
            if (!found)
            {
                return;
            }
            var nwvm= SendVideoOverTCPLib.SendVideo.NetworkViewModel;

            if (nwvm is NetworkViewModel nw)
            {
                grid.IsVisible = false;
                BusyIndicatorLabel.IsVisible = true;
                BusyIndicator.IsVisible = true;
                BusyIndicator.IsRunning = true;
                BusyIndicatorLabel.Text = "Waiting for Program to be sent…";
                int port = nw.SelectedProgramUploadPort;
                try
                {
                    // Choose a user-visible, app-specific external folder on Android (no runtime permission required),
                    // otherwise use the user's Documents folder on desktop platforms.
                    string saveDir;

#if ANDROID
                    // App-specific external files directory (e.g. /storage/emulated/0/Android/data/<pkg>/files/Downloads)
                    // extDir = Android.App.Application.Context.GetExternalFilesDir(Android.OS.Environment.DirectoryDownloads)?.AbsolutePath;
                    saveDir = System.IO.Path.Combine( FileSystem.AppDataDirectory, "ReceivedPrograms");
#else
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            saveDir = System.IO.Path.Combine(docs, "ReceivedPrograms");
#endif

                    if (!Directory.Exists(saveDir))
                    {
                        Directory.CreateDirectory(saveDir!);
                    }

                    // Await the listener task so exceptions propagate here
                    await ProgramReceiver.StartListeningAsync(IPAddress.Any, port, saveDir);
                    string contents = ProgramReceiver.ReadFile(ProgramReceiver.FilePath);
                    //var meets = MeetCsvImporter.ParseMeetsCsv(contents);
                    if(caption == "Get Meets")
                    {
                        string msg = await MeetCsvImporter.ImportMeetsIntoDatabaseAsync(contents);
                        LoadMeets();
                        await DisplayAlert("Import Meets", msg, "OK");
                    }
                    else if(caption == "Get Events")
                    {
                        string msg = await MeetCsvImporter.ImportEventsIntoDatabaseAsync(contents);
                        LoadEvents();
                        await DisplayAlert("Import Events", msg, "OK");
                    }
                }
                catch (OperationCanceledException)
                {
                    // listener was cancelled — no action
                }
                catch (Exception ex)
                {
                    // Surface error to user (UI thread)
                    await DisplayAlert("Listener error", ex.Message, "OK");
                }
                finally
                {
                    BusyIndicator.IsRunning = false;
                    BusyIndicator.IsVisible = false;
                    BusyIndicatorLabel.IsVisible = false;
                    grid.IsVisible = true;
                }

            }
        }

        private void GetEvents4Meet(object sender, EventArgs e)
        {

        }

        private void Heat_Clicked(object sender, EventArgs e)
        {
            if (sender is not Button btn) return;

            var caption = btn.Text ?? string.Empty;

            switch (caption)
            {
                case "Next":
                    _Meets?.NextHeat();
                    break;
                case "Prev":
                    _Meets?.PrevHeat();
                    break;
                default:
                    // no-op
                    break;
            }
        }
    }

}
