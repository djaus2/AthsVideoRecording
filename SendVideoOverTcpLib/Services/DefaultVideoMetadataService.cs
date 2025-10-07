using System;
using System.Threading.Tasks;

namespace SendVideoOverTCPLib.Services
{
    public class DefaultVideoMetadataService : IVideoMetadataService
    {
        public async Task<DateTime> GetVideoCreationDateAsync(string filePath)
        {
            // For non-Android platforms, we'll use the standard File API
            // This won't work correctly on Android due to the FilePicker issue
            if (File.Exists(filePath))
            {
                return File.GetCreationTime(filePath);
            }
            
            return DateTime.Now;
        }

        public async Task<VideoFileInfo> PickVideoAsync()
        {
            var customFileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "video/*" } },
                { DevicePlatform.iOS, new[] { "public.movie" } },
                { DevicePlatform.macOS, new[] { "public.movie" } },
                { DevicePlatform.WinUI, new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv" } }
            });

            var options = new PickOptions
            {
                PickerTitle = "Select a Video File",
                FileTypes = customFileTypes
            };

            var fileResult = await FilePicker.PickAsync(options);
            if (fileResult == null)
                return null;

            // For non-Android platforms, we can use File.GetCreationTime
            DateTime creationTime = File.Exists(fileResult.FullPath) 
                ? File.GetCreationTime(fileResult.FullPath) 
                : DateTime.Now;

            return new VideoFileInfo
            {
                FilePath = fileResult.FullPath,
                FileName = fileResult.FileName,
                CreationTime = creationTime
            };
        }

        public bool HasCachedList()
        {
            // Non-Android default: no cached list concept
            return false;
        }

        public void AddVideoToCache(string newVideoPath, DateTime? whenOverride = null)
        {
            // Non-Android default: no-op
        }
    }
}
