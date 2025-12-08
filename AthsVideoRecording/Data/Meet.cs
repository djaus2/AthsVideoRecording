using AthsVideoRecording.Data;
using System;
using System.Globalization;
using System.ComponentModel;
using System.Xml.Linq;

using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AthsVideoRecording.Data
{

    public partial class Meet :ObservableObject
    {
        public Meet()
        {
            // Ensure a stable ExternalId exists for each new Meet
            if (string.IsNullOrWhiteSpace(ExternalId))
                ExternalId = Guid.NewGuid().ToString();

            PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(IsDirty) or nameof(Id) or nameof(Display)) return;
                IsDirty = true;
            };
        }

        [Key]
        public int Id { get; set; }

        [JsonIgnore]
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();


        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        private string description;

        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        public int round;

        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        public DateTime? date;

        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        public string? location = "";

        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        public int? maxLanes = 8;

        // New stable identifier to help cross-device reconciliation
        [ObservableProperty]
        private string externalId = Guid.NewGuid().ToString();


        [JsonIgnore]
        // Convenience: date-only string for UI bindings
        public string DateStr => Date?.ToString("yyyy-MM-dd") ?? string.Empty;

        [JsonIgnore]
        [NotMapped]
        public string Display => ToString();

        public override string ToString()
        {
            string result = $"{Description} Round:{Round}.  {DateStr} at {Location}";
            return result;
        }

        [property: JsonIgnore]
        [property: NotMapped]
        [ObservableProperty] private bool isDirty = false;

        public bool IsMeetDirty()
        {
            if (IsDirty) return true;
            foreach (var r in Events)
            {
                if (r.IsDirty) return true;
            }
            return false;
        }
    }

}
