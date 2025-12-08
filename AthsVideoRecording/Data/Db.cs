using Microsoft.Data.Sqlite;
using System;
using System.Data.Common;
using System.IO;

namespace AthsVideoRecording.Data
{
    public static class Db
    {
        // SQLite DB path: %LocalAppData%/AthStitcher/athstitcher.db
        /// e.g., %LocalAppData%\AthStitcher\athstitcher.db
        public static string GetConnectionString()
        {
            var saveDir = System.IO.Path.Combine(FileSystem.AppDataDirectory, "ReceivedPrograms");
            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir!);
            }
            var path = Path.Combine(saveDir, "athsvideorecording.db");
            var cs = new SqliteConnectionStringBuilder { DataSource = path }.ToString();
            System.Diagnostics.Debug.WriteLine(cs);
            return cs;
        }

        public static DbConnection CreateConnection(bool open = true)
        {
            var conn = new SqliteConnection(GetConnectionString());
            if (open)
            {
                conn.Open();
                // Ensure foreign keys are enforced
                using var pragma = conn.CreateCommand();
                pragma.CommandText = "PRAGMA foreign_keys = ON;";
                pragma.ExecuteNonQuery();
            }
            return conn;
        }
    }
}
