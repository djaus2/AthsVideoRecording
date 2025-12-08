using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace AthsVideoRecording.Data
{
    //public enum TrackType {  run, walk, steeple, hurdles, relay, none=100 }
    //public enum Gender { male, female, mixed, none=100 }
    //public enum AgeGrouping { junior, open, masters, none=100 }
    //public enum MastersAgeGroup { M30, M35, M40, M45, M50, M55, M60, M65, M70, M75, M80, M85, M90, M95, W30, W35, W40, W45, W50, W55, W60, W65, W70, W75, W80, W85, W90, W95, other=100 }

    //public enum MaleMastersAgeGroup { M30, M35, M40, M45, M50, M55, M60, M65, M70, M75, M80, M85, M90, M95, other = 100 }

    //public enum FemaleMastersAgeGroup {W30, W35, W40, W45, W50, W55, W60, W65, W70, W75, W80, W85, W90, W95, other = 100 }

    //public enum UnderAgeGroup { U13, U14, U15, U16, U17, U18, U19, U20, other=100 }
    //public enum LittleAthleticsAgeGroup { U6, U7, U8, U9, U10, U11, U12, other=100 }


    public static class DbInitializer
    {
        public static void Initialize()
        {
            // Ensure database exists, then ensure tables exist
            EnsureDatabase();
            EnsureTables();
        }

        public static void EnsureDatabase()
        {
            // For SQLite, opening the connection ensures the file exists.
            using var conn = Db.CreateConnection();
            // no-op: file is created when connection opens; foreign keys pragma is set in Db.CreateConnection
        }

        private static void EnsureTables()
        {
            using var conn = Db.CreateConnection();
            using var cmd = conn.CreateCommand();

            // Users (SQLite)
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Users (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Username TEXT NOT NULL UNIQUE,
  PasswordHash BLOB NOT NULL,
  PasswordSalt BLOB NOT NULL,
  Role TEXT NOT NULL,
  CreatedAt TEXT NOT NULL,
  ForcePasswordChange INTEGER NOT NULL DEFAULT 0
);";
            cmd.ExecuteNonQuery();

            // Meets (SQLite)
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Meets (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Description TEXT NOT NULL,
  Date TEXT NULL,
  Location TEXT NULL
);";
            cmd.ExecuteNonQuery();

            // Events (SQLite)
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Events (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  MeetId INTEGER NOT NULL,
  Time TEXT NULL,
  EventNumber INTEGER NULL,
  Description TEXT NULL,
  Distance INTEGER NULL,
  HurdleSteepleHeight INTEGER NULL,
  Gender INTEGER NOT NULL DEFAULT 100 CHECK (Gender IN (0,1,2,100)),
  AgeGrouping INTEGER NOT NULL DEFAULT 100 CHECK (AgeGrouping IN (0,1,2,100)),
  -- Applies only when AgeGrouping is junior (0) or open (1)
  UnderAgeGroup INTEGER NULL CHECK (
    (AgeGrouping IN (0,1) AND UnderAgeGroup IN (0,1,2,3,4,5,6,7,100))
    OR (AgeGrouping NOT IN (0,1) AND (UnderAgeGroup IS NULL OR UnderAgeGroup = 100))
  ),
  -- Applies only when AgeGrouping is masters (2)
  MastersAgeGroup INTEGER NULL CHECK (
    (AgeGrouping = 2 AND MastersAgeGroup IN (0,1,2,3,4,5,6,7,8,9,10,11,12,13,
                                             14,15,16,17,18,19,20,21,22,23,24,25,26,27,100))
    OR (AgeGrouping <> 2 AND (MastersAgeGroup IS NULL OR MastersAgeGroup = 100))
  ),
  TrackType INTEGER NOT NULL DEFAULT 100 CHECK (TrackType IN (0,1,2,3,4,100)),
  VideoInfoFile TEXT NULL,
  VideoStartOffsetSeconds REAL NULL,
  FOREIGN KEY (MeetId) REFERENCES Meets(Id) ON DELETE CASCADE
);";
            cmd.ExecuteNonQuery();



        }

        // Drops all known tables in dependency-safe order then re-enables FK checks
        public static void DropAllTables()
        {
            using var conn = Db.CreateConnection();
            using var cmd = conn.CreateCommand();
            // Disable FK checks to allow dropping in any order
            cmd.CommandText = "PRAGMA foreign_keys = OFF;";
            cmd.ExecuteNonQuery();

            // Drop child-first order
            string[] tables = new[] {  "Events", "Meets" };
            foreach (var t in tables)
            {
                cmd.CommandText = $"DROP TABLE IF EXISTS {t};";
                cmd.ExecuteNonQuery();
            }

            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();
        }

        // Fully recreates schema inside the existing file
        public static void RecreateSchema()
        {
            DropAllTables();
            EnsureTables();
        }
    }
}
