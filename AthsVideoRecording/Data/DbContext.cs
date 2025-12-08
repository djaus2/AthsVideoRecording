using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Proxies;
using System;
using System.IO;

namespace AthsVideoRecording.Data
{
    public class AthsVideoRecordingDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Meet> Meets { get; set; }
        public DbSet<Event> Events { get; set; }
        //public DbSet<Heat> Heats { get; set; }
        //public DbSet<LaneResult> Results { get; set; }

        // Add a constructor that accepts DbContextOptions so EF tools and DI can construct the context.
        // Keep the parameterless behavior (OnConfiguring) for simple scenarios.
        public AthsVideoRecordingDbContext()
        {
        }

        public AthsVideoRecordingDbContext(DbContextOptions<AthsVideoRecordingDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var saveDir = System.IO.Path.Combine(FileSystem.AppDataDirectory, "ReceivedPrograms");
            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir!);
            }
            var dbPath = Path.Combine(saveDir, "athsvideorecording.db");
            //var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AthsVideoRecording", "athsvideorecording.db");
            //var dir = Path.GetDirectoryName(dbPath);

            optionsBuilder
                .UseSqlite($"Data Source={dbPath}")
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
                .UseLazyLoadingProxies(); // This now works because of the added using directive
                //.UseChangeTrackingProxies();
        }

    }
}
