using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
//using AthStitcher.Data; // Meet model
using AthsVideoRecording.Data; // DbContext that contains Meets DbSet

namespace AthsVideoRecording.Data
{
    /// <summary>
    /// Import / parse helpers for uploading a CSV of Meets into the Meets table.
    /// Expected CSV: header line with column names then one line per meet.
    /// Supported columns (case-insensitive): Id, Description, Round, Date, Location, MaxLanes
    /// Date parsing accepts ISO / round-trip and common date formats.
    /// </summary>
    public static class MeetCsvImporter
    {
        public static List<Meet> ParseMeetsCsv(string csv)
        {
            var result = new List<Meet>();
            if (string.IsNullOrWhiteSpace(csv)) return result;

            var lines = SplitLines(csv).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            if (lines.Count == 0) return result;

            // header
            var headers = ParseCsvLine(lines[0]).Select(h => h.Trim()).ToList();
            // map header name -> index (case-insensitive)
            var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Count; i++)
                index[headers[i]] = i;

            // helper to get field by logical name
            string GetField(string[] fields, string name)
            {
                if (index.TryGetValue(name, out var idx) && idx >= 0 && idx < fields.Length)
                    return fields[idx].Trim();
                // try common alternatives
                if (name.Equals("Description", StringComparison.OrdinalIgnoreCase) && index.TryGetValue("Desc", out idx) && idx < fields.Length)
                    return fields[idx].Trim();
                return string.Empty;
            }

            for (int i = 1; i < lines.Count; i++)
            {
                var fields = ParseCsvLine(lines[i]).ToArray();
                if (fields.Length == 0) continue;

                var meet = new Meet();

                // Id (optional)
                //var idStr = GetField(fields, "Id");
                //if (int.TryParse(idStr, out var idVal)) meet.Id = idVal;

                // ExternalId
                var externalIdStr = GetField(fields, "ExternalId");
                if (Guid.TryParse(externalIdStr, out Guid g)) meet.ExternalId = g.ToString();

                // Description
                var desc = GetField(fields, "Description");
                if (string.IsNullOrEmpty(desc) && fields.Length > 0) desc = fields[0].Trim(); // fallback to first column
                meet.Description = desc;

                // Round
                var roundStr = GetField(fields, "Round");
                if (int.TryParse(roundStr, out var r)) meet.Round = r;

                // Date (nullable)
                var dateStr = GetField(fields, "Date");
                if (!string.IsNullOrWhiteSpace(dateStr))
                {
                    if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt) ||
                        DateTime.TryParse(dateStr, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out dt))
                    {
                        meet.Date = dt;
                    }
                    else
                    {
                        meet.Date = null;
                    }
                }

                // Location
                meet.Location = GetField(fields, "Location");

                // MaxLanes
                var maxLanesStr = GetField(fields, "MaxLanes");
                if (int.TryParse(maxLanesStr, out var ml)) meet.MaxLanes = ml;
                else if (string.IsNullOrWhiteSpace(maxLanesStr))
                {
                    // leave default (Meet class default set)
                }

                result.Add(meet);
            }

            return result;
        }

        public static List<Event> ParseEventsCsv(string csv, AthsVideoRecording.Data.AthsVideoRecordingDbContext ctx)
        {
            var result = new List<Event>();
            if (string.IsNullOrWhiteSpace(csv)) return result;

            var lines = SplitLines(csv).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            if (lines.Count == 0) return result;

            // header
            var headers = ParseCsvLine(lines[0]).Select(h => h.Trim()).ToList();
            // map header name -> index (case-insensitive)
            var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Count; i++)
                index[headers[i]] = i;

            // CSV Header reference from AthStitcher
            // sb.AppendLine("Id,EventExternalId,EventNumber,Time,Description,Distance,TrackType,Gender,AgeGrouping,UnderAgeGroup,MastersAgeGroup,MinLane,MaxLane,VideoInfoFile,VideoStartOffsetSeconds,MeetId,MeetExternalId,MeetDescription,MeetDate,MeetLocation,MeetRound,NumHeats");
            //
            // helper to get field by logical name
            string GetField(string[] fields, string name)
            {
                if (index.TryGetValue(name, out var idx) && idx >= 0 && idx < fields.Length)
                    return fields[idx].Trim();
                // try common alternatives
                if (name.Equals("Description", StringComparison.OrdinalIgnoreCase) && index.TryGetValue("Desc", out idx) && idx < fields.Length)
                    return fields[idx].Trim();
                return string.Empty;
            }

            for (int i = 1; i < lines.Count; i++)
            {
                var fields = ParseCsvLine(lines[i]).ToArray();
                if (fields.Length == 0) continue;

                var ev = new Event();



                // Id (optional)
                var numHeatsStr = GetField(fields, "NumHeats");
                if (int.TryParse(numHeatsStr, out var numHeats)) ev.NumHeats = numHeats;

                // ExternalId
                var externalIdStr = GetField(fields, "EventExternalId");
                if (Guid.TryParse(externalIdStr, out Guid g)) ev.ExternalId = g.ToString();

                // Description
                var desc = GetField(fields, "Description");
                if (string.IsNullOrEmpty(desc) && fields.Length > 0) desc = fields[0].Trim(); // fallback to first column
                ev.Description = desc;

                // Get MeetId
                // Use the Event MeetExternalId field to lookup the Meet
                // If found use that Meet's Id
                // Otherwise try to use the Meet Id (as sent) field directly
                var meetExternalIdStr = GetField(fields, "MeetExternalId");
                if (Guid.TryParse(meetExternalIdStr, out Guid meetExternalId))
                {
                    ev.MeetExternalId = meetExternalId.ToString();
                }


                result.Add(ev);
                //break;
            }

            return result;
        }

        /// <summary>
        /// Parse CSV and insert/update rows into the provided DbContext.
        /// If a parsed Meet has Id > 0 and an existing row is found, it will be updated.
        /// Otherwise a new row will be inserted.
        /// </summary>
        public static async Task<string> ImportMeetsIntoDatabaseAsync(string csv)
        {
            using var ctx = new AthsVideoRecording.Data.AthsVideoRecordingDbContext();
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            var meets = ParseMeetsCsv(csv);
            if (meets.Count == 0) return "No meets";

            int numUpdates = 0;
            int numNew = 0;
            bool newTable = (ctx.Meets.Count() == 0);
            foreach (var m in meets)
            {
                if (!newTable)
                {
                    if (!string.IsNullOrEmpty(m.ExternalId))
                    {
                        var existing = await ctx.Meets.FirstOrDefaultAsync(mm => mm.ExternalId == m.ExternalId);
                        if (existing != null)
                        {
                            // update fields (do not overwrite Events navigation)
                            existing.Description = m.Description;
                            existing.Round = m.Round;
                            existing.Date = m.Date;
                            existing.Location = m.Location;
                            existing.MaxLanes = m.MaxLanes;
                            ctx.Meets.Update(existing);
                            numUpdates++;
                            continue;
                        }
                    }
                }

                // insert new
                // ensure EF will generate Id
                m.Id = 0;
                await ctx.Meets.AddAsync(m).ConfigureAwait(false);
                numNew++;
            }

            int num = await ctx.SaveChangesAsync().ConfigureAwait(false);
            string msg = $"Num changes: {num} = New:{numNew} + Updates:{numUpdates}";
            return msg; 
        }


        /// <summary>
        /// Parse CSV and insert/update rows into the provided DbContext.
        /// If a parsed Meet has Id > 0 and an existing row is found, it will be updated.
        /// Otherwise a new row will be inserted.
        /// </summary>
        public static async Task<string> ImportEventsIntoDatabaseAsync(string csv)
        {
            using var ctx = new AthsVideoRecording.Data.AthsVideoRecordingDbContext();
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            var events = ParseEventsCsv(csv, ctx);
            Meet meet = ctx.Meets.FirstOrDefault();
            //var eventf = events.FirstOrDefault();
            foreach (var ev in events)
            {
                ev.Id = 0;
                var meet2 = ctx.Meets
                    .FirstOrDefault(m => !string.IsNullOrEmpty(m.ExternalId)
                                         && (m.ExternalId == ev.MeetExternalId));// .Equals//, StringComparison.OrdinalIgnoreCase));
                if (meet2 != null)
                {
                    ev.Meet = meet2;
                }
            }
            //var events = new List<Event>() { new Event {Meet= meet } };
            if (events.Count == 0) return "No events";

            //await ctx.Events.AddRangeAsync(events).ConfigureAwait(false);

            int numUpdates = 0;
            int numNew = 0;
            foreach (var ev in events)
            {
                if (!string.IsNullOrEmpty(ev.ExternalId))
                {
                    var existing = await ctx.Events.FirstOrDefaultAsync(eev => eev.ExternalId == ev.ExternalId);
                    if (existing != null)
                    {
                        // update fields (do not overwrite Events navigation)
                        existing.Description = ev.Description;
                        existing.NumHeats = ev.NumHeats;
                        existing.EventNumber = ev.EventNumber;
                        ctx.Events.Update(existing);
                        numUpdates++;
                        continue;
                    }
                }

                // insert new
                // ensure EF will generate Id
                
                ev.Id = 0;
                ctx.Events.Add(ev);
                numNew++;
            }

            int num = ctx.SaveChanges();
            var evs = ctx.Events;
            string msg = $"Num changes: {num} = New:{numNew} + Updates:{numUpdates}";
            return msg;
        }

        // --- CSV helpers ------------------------------------------------

        // Split into lines supporting various line endings
        private static IEnumerable<string> SplitLines(string s)
        {
            using var reader = new StringReader(s);
            string? line;
            while ((line = reader.ReadLine()) != null)
                yield return line;
        }

        // Basic CSV line parser handling quoted fields and doubled quotes.
        // Not a full RFC4180 implementation but robust for typical exported CSV.
        private static List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            if (line == null)
                return fields;

            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        // possible escaped quote
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++; // skip second quote
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (c == ',')
                    {
                        fields.Add(sb.ToString());
                        sb.Clear();
                    }
                    else if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }

            fields.Add(sb.ToString());
            return fields;
        }
    }
}
