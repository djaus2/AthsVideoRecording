using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AthsVideoRecording.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AthsVideoRecordingDbContext>
    {
        public AthsVideoRecordingDbContext CreateDbContext(string[] args)
        {
            var saveDir = System.IO.Path.Combine(FileSystem.AppDataDirectory, "ReceivedPrograms");
            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir!);
            }
            var dbPath = Path.Combine(saveDir, "athsvideorecording.db");

            var options = new DbContextOptionsBuilder<AthsVideoRecordingDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            return new AthsVideoRecordingDbContext(options);
        }
    }
}
