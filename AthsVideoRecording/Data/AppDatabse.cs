using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace AthsVideoRecording.Data
{
    /// <summary>
    /// Lightweight SQLite helper that stores Meet and Event rows.
    /// Maps to the existing domain types in this namespace.
    /// Add package: dotnet add package Microsoft.Data.Sqlite
    /// </summary>
    public sealed class AppDatabase : IDisposable
    {
        private readonly string _dbPath;
        private readonly SqliteConnection _conn;

        public AppDatabase(string? path = null)
        {
            _dbPath = string.IsNullOrWhiteSpace(path)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "athsvideo.db3")
                : path;

            Directory.CreateDirectory(Path.GetDirectoryName(_dbPath) ?? Environment.CurrentDirectory);

            var cs = new SqliteConnectionStringBuilder { DataSource = _dbPath, Mode = SqliteOpenMode.ReadWriteCreate }.ToString();
            _conn = new SqliteConnection(cs);
            _conn.Open();
        }

        public ValueTask InitializeAsync()
        {
            // Create tables if they don't exist
            const string meetSql = @"
CREATE TABLE IF NOT EXISTS Meets (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Description TEXT,
    Round INTEGER,
    Date TEXT,
    Location TEXT,
    MaxLanes INTEGER
);";

            const string eventSql = @"
CREATE TABLE IF NOT EXISTS Events (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MeetId INTEGER NOT NULL,
    EventNumber INTEGER,
    Time TEXT,
    Description TEXT,
    Distance INTEGER,
    HurdleSteepleHeight INTEGER,
    TrackType TEXT,
    Gender TEXT,
    AgeGrouping TEXT,
    UnderAgeGroup TEXT,
    MastersAgeGroup TEXT,
    MaleMastersAgeGroup TEXT,
    FemaleMastersAgeGroup TEXT,
    VideoInfoFile TEXT,
    VideoStartOffsetSeconds REAL,
    MinLane INTEGER,
    MaxLane INTEGER,
    NumHeats INTEGER,
    FOREIGN KEY(MeetId) REFERENCES Meets(Id) ON DELETE CASCADE
);";

            using var tx = _conn.BeginTransaction();
            using var cmd = _conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = meetSql;
            cmd.ExecuteNonQuery();
            cmd.CommandText = eventSql;
            cmd.ExecuteNonQuery();
            tx.Commit();
            return default;
        }

        // ---------- Meet CRUD ----------
        public async Task<int> InsertMeetAsync(Meet meet)
        {
            const string sql = @"
INSERT INTO Meets (Description, Round, Date, Location, MaxLanes)
VALUES ($description, $round, $date, $location, $maxLanes);
SELECT last_insert_rowid();";

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("$description", meet.Description ?? string.Empty);
            cmd.Parameters.AddWithValue("$round", meet.Round);
            cmd.Parameters.AddWithValue("$date", meet.Date?.ToString("o") ?? string.Empty);
            cmd.Parameters.AddWithValue("$location", meet.Location ?? string.Empty);
            cmd.Parameters.AddWithValue("$maxLanes", meet.MaxLanes ?? 8);

            var result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
            return Convert.ToInt32(result);
        }

        public Task<int> UpdateMeetAsync(Meet meet)
        {
            const string sql = @"
UPDATE Meets SET
    Description = $description,
    Round = $round,
    Date = $date,
    Location = $location,
    MaxLanes = $maxLanes
WHERE Id = $id;";

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("$description", meet.Description ?? string.Empty);
            cmd.Parameters.AddWithValue("$round", meet.Round);
            cmd.Parameters.AddWithValue("$date", meet.Date?.ToString("o") ?? string.Empty);
            cmd.Parameters.AddWithValue("$location", meet.Location ?? string.Empty);
            cmd.Parameters.AddWithValue("$maxLanes", meet.MaxLanes ?? 8);
            cmd.Parameters.AddWithValue("$id", meet.Id);

            return Task.FromResult(cmd.ExecuteNonQuery());
        }

        public Task<int> DeleteMeetAsync(int meetId)
        {
            const string sql = "DELETE FROM Meets WHERE Id = $id;";
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("$id", meetId);
            return Task.FromResult(cmd.ExecuteNonQuery());
        }

        public Task<Meet?> GetMeetAsync(int id)
        {
            const string sql = "SELECT Id, Description, Round, Date, Location, MaxLanes FROM Meets WHERE Id = $id;";
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("$id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return Task.FromResult<Meet?>(null);

            var meet = ReadMeetFromReader(reader);
            return Task.FromResult(meet);
        }

        public Task<List<Meet>> GetAllMeetsAsync()
        {
            const string sql = "SELECT Id, Description, Round, Date, Location, MaxLanes FROM Meets ORDER BY Date DESC;";
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();
            var list = new List<Meet>();
            while (reader.Read())
            {
                list.Add(ReadMeetFromReader(reader));
            }
            return Task.FromResult(list);
        }

        // ---------- Event CRUD ----------
        public async Task<int> InsertEventAsync(Event ev)
        {
            const string sql = @"
INSERT INTO Events (MeetId, EventNumber, Time, Description, Distance, HurdleSteepleHeight, TrackType, Gender,
    AgeGrouping, UnderAgeGroup, MastersAgeGroup, MaleMastersAgeGroup, FemaleMastersAgeGroup,
    VideoInfoFile, VideoStartOffsetSeconds, MinLane, MaxLane, NumHeats)
VALUES ($meetId, $eventNumber, $time, $description, $distance, $hurdle, $trackType, $gender,
    $ageGrouping, $underAgeGroup, $mastersAgeGroup, $maleMastersAgeGroup, $femaleMastersAgeGroup,
    $videoInfoFile, $videoStartOffsetSeconds, $minLane, $maxLane, $numHeats);
SELECT last_insert_rowid();";

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("$meetId", ev.MeetId);
            cmd.Parameters.AddWithValue("$eventNumber", ev.EventNumber ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$time", ev.Time?.ToString("o") ?? string.Empty);
            cmd.Parameters.AddWithValue("$description", ev.Description ?? string.Empty);
            cmd.Parameters.AddWithValue("$distance", ev.Distance ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$hurdle", ev.HurdleSteepleHeight ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$trackType", ev.TrackType.ToString());
            cmd.Parameters.AddWithValue("$gender", ev.Gender.ToString());
            cmd.Parameters.AddWithValue("$ageGrouping", ev.AgeGrouping.ToString());
            cmd.Parameters.AddWithValue("$underAgeGroup", ev.UnderAgeGroup?.ToString() ?? string.Empty);
            cmd.Parameters.AddWithValue("$mastersAgeGroup", ev.MastersAgeGroup?.ToString() ?? string.Empty);
            cmd.Parameters.AddWithValue("$maleMastersAgeGroup", ev.MaleMastersAgeGroup?.ToString() ?? string.Empty);
            cmd.Parameters.AddWithValue("$femaleMastersAgeGroup", ev.FemaleMastersAgeGroup?.ToString() ?? string.Empty);
            cmd.Parameters.AddWithValue("$videoInfoFile", ev.VideoInfoFile ?? string.Empty);
            cmd.Parameters.AddWithValue("$videoStartOffsetSeconds", ev.VideoStartOffsetSeconds ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$minLane", ev.MinLane ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$maxLane", ev.MaxLane ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("numHeats", ev.NumHeats );
          
            var result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
            return Convert.ToInt32(result);
        }

        public Task<int> UpdateEventAsync(Event ev)
        {
            const string sql = @"
UPDATE Events SET
    MeetId = $meetId,
    EventNumber = $eventNumber,
    Time = $time,
    Description = $description,
    Distance = $distance,
    HurdleSteepleHeight = $hurdle,
    TrackType = $trackType,
    Gender = $gender,
    AgeGrouping = $ageGrouping,
    UnderAgeGroup = $underAgeGroup,
    MastersAgeGroup = $mastersAgeGroup,
    MaleMastersAgeGroup = $maleMastersAgeGroup,
    FemaleMastersAgeGroup = $femaleMastersAgeGroup,
    VideoInfoFile = $videoInfoFile,
    VideoStartOffsetSeconds = $videoStartOffsetSeconds,
    MinLane = $minLane,
    MaxLane = $maxLane,
    NumHeats = $numHeats
WHERE Id = $id;";

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("$meetId", ev.MeetId);
            cmd.Parameters.AddWithValue("$eventNumber", ev.EventNumber ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$time", ev.Time?.ToString("o") ?? string.Empty);
            cmd.Parameters.AddWithValue("$description", ev.Description ?? string.Empty);
            cmd.Parameters.AddWithValue("$distance", ev.Distance ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$hurdle", ev.HurdleSteepleHeight ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$trackType", ev.TrackType.ToString());
            cmd.Parameters.AddWithValue("$gender", ev.Gender.ToString());
            cmd.Parameters.AddWithValue("$ageGrouping", ev.AgeGrouping.ToString());
            cmd.Parameters.AddWithValue("$underAgeGroup", ev.UnderAgeGroup?.ToString() ?? string.Empty);
            cmd.Parameters.AddWithValue("$mastersAgeGroup", ev.MastersAgeGroup?.ToString() ?? string.Empty);
            cmd.Parameters.AddWithValue("$maleMastersAgeGroup", ev.MaleMastersAgeGroup?.ToString() ?? string.Empty);
            cmd.Parameters.AddWithValue("$femaleMastersAgeGroup", ev.FemaleMastersAgeGroup?.ToString() ?? string.Empty);
            cmd.Parameters.AddWithValue("$videoInfoFile", ev.VideoInfoFile ?? string.Empty);
            cmd.Parameters.AddWithValue("$videoStartOffsetSeconds", ev.VideoStartOffsetSeconds ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$minLane", ev.MinLane ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$maxLane", ev.MaxLane ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("numHeats", ev.NumHeats);
            cmd.Parameters.AddWithValue("$id", ev.Id);

            return Task.FromResult(cmd.ExecuteNonQuery());
        }

        public Task<int> DeleteEventAsync(int eventId)
        {
            const string sql = "DELETE FROM Events WHERE Id = $id;";
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("$id", eventId);
            return Task.FromResult(cmd.ExecuteNonQuery());
        }

        public Task<Event?> GetEventAsync(int id)
        {
            const string sql = "SELECT * FROM Events WHERE Id = $id;";
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("$id", id);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return Task.FromResult<Event?>(null);
            var ev = ReadEventFromReader(reader);
            return Task.FromResult(ev);
        }

        public Task<List<Event>> GetEventsForMeetAsync(int meetId)
        {
            const string sql = "SELECT * FROM Events WHERE MeetId = $meetId ORDER BY EventNumber;";
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("$meetId", meetId);
            using var reader = cmd.ExecuteReader();
            var list = new List<Event>();
            while (reader.Read())
            {
                list.Add(ReadEventFromReader(reader));
            }
            return Task.FromResult(list);
        }

        // ---------- helpers to materialize domain types ----------
        private static Meet ReadMeetFromReader(SqliteDataReader reader)
        {
            var m = new Meet
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Description = reader.GetStringOrDefault("Description"),
                Round = reader.GetInt32OrDefault("Round"),
                Date = reader.GetStringOrDefault("Date") is string ds && !string.IsNullOrEmpty(ds)
                    ? (DateTime?)DateTime.Parse(ds, null, System.Globalization.DateTimeStyles.RoundtripKind)
                    : null,
                Location = reader.GetStringOrDefault("Location"),
                MaxLanes = reader.GetInt32OrDefaultNullable("MaxLanes")
            };
            return m;
        }

        private static Event ReadEventFromReader(SqliteDataReader reader)
        {
            var ev = new Event
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                MeetId = reader.GetInt32(reader.GetOrdinal("MeetId")),
                EventNumber = reader.GetInt32OrNullable("EventNumber"),
                Time = reader.GetStringOrDefault("Time") is string ts && !string.IsNullOrEmpty(ts)
                    ? (DateTime?)DateTime.Parse(ts, null, System.Globalization.DateTimeStyles.RoundtripKind)
                    : null,
                Description = reader.GetStringOrDefault("Description"),
                Distance = reader.GetInt32OrNullable("Distance"),
                HurdleSteepleHeight = reader.GetInt32OrNullable("HurdleSteepleHeight"),
                TrackType = reader.GetStringOrDefault("TrackType").ToEnumOrDefault<TrackType>(),
                Gender = reader.GetStringOrDefault("Gender").ToEnumOrDefault<Gender>(),
                AgeGrouping = reader.GetStringOrDefault("AgeGrouping").ToEnumOrDefault<AgeGrouping>(),
                UnderAgeGroup = reader.GetStringOrDefault("UnderAgeGroup").ToNullableEnum<UnderAgeGroup>(),
                MastersAgeGroup = reader.GetStringOrDefault("MastersAgeGroup").ToNullableEnum<MastersAgeGroup>(),
                MaleMastersAgeGroup = reader.GetStringOrDefault("MaleMastersAgeGroup").ToNullableEnum<MaleMastersAgeGroup>(),
                FemaleMastersAgeGroup = reader.GetStringOrDefault("FemaleMastersAgeGroup").ToNullableEnum<FemaleMastersAgeGroup>(),
                VideoInfoFile = reader.GetStringOrDefault("VideoInfoFile"),
                VideoStartOffsetSeconds = reader.GetDoubleOrNullable("VideoStartOffsetSeconds"),
                MinLane = reader.GetInt32OrNullable("MinLane"),
                MaxLane = reader.GetInt32OrNullable("MaxLane"),
                NumHeats = reader.GetInt32("NumHeats")
            };
            return ev;
        }

        public void Dispose()
        {
            try { _conn?.Close(); } catch { }
            try { _conn?.Dispose(); } catch { }
        }
    }

    // ---------- extension helpers for reader ----------
    internal static class SqliteReaderExtensions
    {
        public static string GetStringOrDefault(this SqliteDataReader r, string name)
        {
            int i = r.GetOrdinal(name);
            if (r.IsDBNull(i)) return string.Empty;
            return r.GetString(i);
        }

        public static int GetInt32OrDefault(this SqliteDataReader r, string name)
        {
            int i = r.GetOrdinal(name);
            if (r.IsDBNull(i)) return default;
            return r.GetInt32(i);
        }

        public static int? GetInt32OrNullable(this SqliteDataReader r, string name)
        {
            int i = r.GetOrdinal(name);
            if (r.IsDBNull(i)) return null;
            return r.GetInt32(i);
        }

        public static int? GetInt32OrDefaultNullable(this SqliteDataReader r, string name)
        {
            int i = r.GetOrdinal(name);
            if (r.IsDBNull(i)) return null;
            return r.GetInt32(i);
        }

        public static double? GetDoubleOrNullable(this SqliteDataReader r, string name)
        {
            int i = r.GetOrdinal(name);
            if (r.IsDBNull(i)) return null;
            return r.GetDouble(i);
        }

        public static T ToEnumOrDefault<T>(this string s) where T : struct
        {
            if (string.IsNullOrEmpty(s)) return default;
            if (Enum.TryParse<T>(s, true, out var v)) return v;
            return default;
        }

        public static T? ToNullableEnum<T>(this string s) where T : struct
        {
            if (string.IsNullOrEmpty(s)) return null;
            if (Enum.TryParse<T>(s, true, out var v)) return v;
            return null;
        }
    }
}