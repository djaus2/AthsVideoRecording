using Android;
using Android.Content;
using Android.Content.PM;
using Android.Database;
using Android.OS;
using Android.Provider;
using Java.IO;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Sportronics.VideoEnums;
using SendVideoOverTCPLib.Services;
using SendVideoOverTCPLib.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VideoEnums;
using AndroidUri = Android.Net.Uri;

namespace SendVideoOverTCPLib.Platforms.Android
{
    public class VideoMetadataService : IVideoMetadataService
    {
        // Cache of newest-first filtered videos (only those with meta info)
        private List<VideoListItem>? _cachedFiltered;
        private DateTime _cachedAt = DateTime.MinValue;
        private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(5);

        public async Task<DateTime> GetVideoCreationDateAsync(string filePath)
        {
            // Default to current time in case we can't get the actual creation time
            DateTime creationTime = DateTime.Now;

            try
            {
                var context = Platform.CurrentActivity?.ApplicationContext;
                if (context == null)
                    return creationTime;

                // If it's a content URI (starts with content://)
                if (filePath.StartsWith("content://"))
                {
                    AndroidUri contentUri = AndroidUri.Parse(filePath);
                    creationTime = GetCreationTimeFromContentUri(contentUri, context);
                }
                else
                {
                    // For file paths, we currently return the default creationTime (now)
                    // Optionally, you could try to resolve by filename via MediaStore here.
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting video creation time: {ex.Message}");
            }

            return creationTime;
        }

        // Picks a video using the cached list when available; falls back to system picker
        public async Task<VideoFileInfo> PickVideoAsync()
        {
            var context = Platform.CurrentActivity?.ApplicationContext;
            bool usedCustomList = false;
            try
            {
                // Build list only if cache is null/empty at button press
                if (_cachedFiltered == null || _cachedFiltered.Count == 0)
                {
                    if (context != null)
                    {
                        await EnsureReadMediaPermissionAsync();
                        _cachedFiltered = QueryRecentVideos(context, maxCount: 50);
                    }
                }

                var items = _cachedFiltered ?? new List<VideoListItem>();
                if (items.Any())
                {
                    usedCustomList = true;
                    // Build picker items preserving original index for mapping
                    var pickerItems = items
                        .Select((it, idx) => new SimpleVideoPickerPage.PickerItem
                        {
                            Name = it.DisplayName,
                            When = it.When,
                            Index = idx
                        })
                        .ToList();

                    // Show custom modal picker for exact formatting
                    int? selectedIndex = await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        var page = new SimpleVideoPickerPage("Select a Video (newest first)", pickerItems);
                        await Application.Current.MainPage.Navigation.PushModalAsync(page);
                        return await page.GetSelectionAsync();
                    });

                    // null => Cancel, -1 => Browse, >=0 => index
                    if (selectedIndex == null)
                        return null;

                    if (selectedIndex >= 0)
                    {
                        var sel = items[selectedIndex.Value];

                        // Prefer absolute path if available; otherwise copy the content to cache and use that path
                        string filePath = sel.AbsolutePath;
                        if (string.IsNullOrEmpty(filePath) && context != null)
                        {
                            filePath = await CopyContentToCacheAsync(context, sel.ContentUri);
                        }

                        if (!string.IsNullOrEmpty(filePath))
                        {
                            return new VideoFileInfo
                            {
                                FilePath = filePath,
                                FileName = sel.DisplayName,
                                CreationTime = sel.When
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Custom video picker failed, falling back. {ex.Message}");
            }

            // Fallback to system FilePicker (order is system-defined)
            try
            {
                var customFileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "video/*" } }
                });

                var options = new PickOptions
                {
                    PickerTitle = "Select a Video File",
                    FileTypes = customFileTypes
                };

                var fileResult = await FilePicker.PickAsync(options);
                if (fileResult == null)
                    return null;

                var vfi = new VideoFileInfo
                {
                    FilePath = fileResult.FullPath,
                    FileName = fileResult.FileName,
                    CreationTime = DateTime.Now
                };

                if (context != null)
                {
                    try
                    {
                        if (fileResult.FullPath.StartsWith("content://"))
                        {
                            AndroidUri contentUri = AndroidUri.Parse(fileResult.FullPath);
                            vfi.CreationTime = GetCreationTimeFromContentUri(contentUri, context);
                        }
                        else
                        {
                            DateTime? mediaStoreTime = GetCreationTimeByFileName(fileResult.FileName, context);
                            if (mediaStoreTime.HasValue)
                                vfi.CreationTime = mediaStoreTime.Value;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error getting creation time: {ex.Message}");
                    }
                }

                return vfi;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error picking video: {ex.Message}");
                return null;
            }
        }

        // Preload newest-first filtered list (called from page OnAppearing)
        public async Task PreloadRecentVideosAsync()
        {
            // Try custom newest-to-oldest list first
            var context = Platform.CurrentActivity?.ApplicationContext;
            if (context != null)
            {
                try
                {
                    // Ensure we have permission to read media before querying
                    await EnsureReadMediaPermissionAsync();
                    // Use cache if fresh, otherwise query
                    _cachedFiltered = (_cachedFiltered != null && DateTime.UtcNow - _cachedAt < _cacheTtl)
                        ? _cachedFiltered
                        : QueryRecentVideos(context, maxCount: 50);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Preload videos failed: {ex.Message}");
                }
            }
        }

        // Clear the preloaded cache (called from page OnDisappearing)
        public void ClearCache()
        {
            _cachedFiltered = null;
            _cachedAt = DateTime.MinValue;
        }

        // Build the list if needed
        public async Task EnsureListBuiltAsync()
        {
            var context = Platform.CurrentActivity?.ApplicationContext;
            if (context == null)
                return;

            bool hasCache = _cachedFiltered != null && _cachedFiltered.Count > 0;
            if (!hasCache)
            {
                await EnsureReadMediaPermissionAsync();
                await Task.Yield();
                _cachedFiltered = await Task.Run(() => QueryRecentVideos(context, maxCount: 50));
                _cachedAt = DateTime.UtcNow;
            }
        }

        // Returns true if a cached list is available
        
        public bool HasCachedList()
        {
            return _cachedFiltered != null && _cachedFiltered.Count > 0;
        }

        // Insert or update a video in the cached list and sort newest-first
        public void AddVideoToCache(string newVideoPath, DateTime? whenOverride = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newVideoPath) || !System.IO.File.Exists(newVideoPath))
                    return;

                if (_cachedFiltered == null)
                    return; // only operate when cache already exists

                string name = System.IO.Path.GetFileName(newVideoPath);
                DateTime when = whenOverride ?? DateTime.Now;
                try
                {
                    var ct = System.IO.File.GetCreationTime(newVideoPath);
                    if (ct != DateTime.MinValue) when = ct;
                }
                catch { }

                // Remove any existing same absolute path
                _cachedFiltered.RemoveAll(v => string.Equals(v?.AbsolutePath, newVideoPath, StringComparison.OrdinalIgnoreCase));

                var item = new VideoListItem
                {
                    DisplayName = name,
                    When = when,
                    AbsolutePath = newVideoPath,
                    ContentUri = AndroidUri.FromFile(new Java.IO.File(newVideoPath))
                };

                _cachedFiltered.Add(item);
                _cachedFiltered = _cachedFiltered.OrderByDescending(v => v.When).ToList();
                _cachedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddVideoToCache error: {ex.Message}");
            }
        }

        // Ensures the app has permission to read media before querying MediaStore
        private async Task EnsureReadMediaPermissionAsync()
        {
            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
                {
                    var activity = Platform.CurrentActivity;
                    if (activity == null)
                        return;

                    if (activity.CheckSelfPermission(Manifest.Permission.ReadMediaVideo) != Permission.Granted)
                    {
                        // Request runtime permission for Android 13+
                        activity.RequestPermissions(new[] { Manifest.Permission.ReadMediaVideo }, 1001);
                        // Brief delay; ideally hook into permission callback
                        await Task.Delay(300);
                    }
                }
                else
                {
                    // Older APIs can use StorageRead permission helper
                    try { await Permissions.RequestAsync<Permissions.StorageRead>(); } catch { }
                }
            }
            catch { }
        }

        private sealed class VideoListItem
        {
            public string DisplayName { get; set; }
            public DateTime When { get; set; }
            public AndroidUri ContentUri { get; set; }
            public string AbsolutePath { get; set; } // May be null on newer Android
        }

        private List<VideoListItem> QueryRecentVideos(Context context, int maxCount)
        {
            var results = new List<VideoListItem>();
            string[] projection = {
                MediaStore.MediaColumns.Id,
                MediaStore.MediaColumns.DisplayName,
                MediaStore.Video.Media.InterfaceConsts.DateTaken,
                MediaStore.Video.Media.InterfaceConsts.DateAdded,
                MediaStore.MediaColumns.Data // deprecated but useful when available
            };


            string sortOrder = MediaStore.Video.Media.InterfaceConsts.DateAdded + " DESC";

            using var cursor = context.ContentResolver.Query(
                MediaStore.Video.Media.ExternalContentUri,
                projection,
                null,
                null,
                sortOrder);

            if (cursor == null)
                return results;

            int idCol = cursor.GetColumnIndex(MediaStore.MediaColumns.Id);
            int nameCol = cursor.GetColumnIndex(MediaStore.MediaColumns.DisplayName);
            int takenCol = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.DateTaken);
            int addedCol = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.DateAdded);
            int dataCol = cursor.GetColumnIndex(MediaStore.MediaColumns.Data);

            int count = 0;
            while (cursor.MoveToNext() && count < maxCount)
            {
                long id = idCol != -1 ? cursor.GetLong(idCol) : -1;
                string name = nameCol != -1 ? cursor.GetString(nameCol) : "(unknown)";

                DateTime when = DateTime.Now;
                if (takenCol != -1 && !cursor.IsNull(takenCol))
                {
                    long ms = cursor.GetLong(takenCol);
                    when = DateTimeOffset.FromUnixTimeMilliseconds(ms).LocalDateTime;
                }
                else if (addedCol != -1 && !cursor.IsNull(addedCol))
                {
                    long sec = cursor.GetLong(addedCol);
                    when = DateTimeOffset.FromUnixTimeSeconds(sec).LocalDateTime;
                }

                string abs = dataCol != -1 && !cursor.IsNull(dataCol) ? cursor.GetString(dataCol) : null;
                var contentUri = AndroidUri.WithAppendedPath(MediaStore.Video.Media.ExternalContentUri, id.ToString());

                // Filter: Check4MetaInfo Include only items that have required meta info in JSON comment for the TimeFrom mode
                bool hasMeta = VideoInfo.Check4MetaInfo(abs); // MinimalTest(abs);//that have meta info in JSON comment with Filename property

                if (hasMeta)
                {
                    results.Add(new VideoListItem
                    {
                        DisplayName = name,
                        When = when,
                        ContentUri = contentUri,
                        AbsolutePath = abs
                    });
                    count++;
                }
            }

            return results;
        }

        private async Task<string> CopyContentToCacheAsync(Context context, AndroidUri uri)
        {
            try
            {
                var fileName = QueryDisplayName(context, uri) ?? ($"video_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");
                var cacheDir = FileSystem.Current.CacheDirectory;
                var destPath = System.IO.Path.Combine(cacheDir, fileName);

                using var input = context.ContentResolver.OpenInputStream(uri);
                using var output = System.IO.File.Open(destPath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None);
                await input.CopyToAsync(output);
                return destPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to copy content to cache: {ex.Message}");
                return null;
            }
        }

        private string QueryDisplayName(Context context, AndroidUri uri)
        {
            try
            {
                string[] projection = { MediaStore.MediaColumns.DisplayName };
                using var cursor = context.ContentResolver.Query(uri, projection, null, null, null);
                if (cursor != null && cursor.MoveToFirst())
                {
                    int idx = cursor.GetColumnIndex(MediaStore.MediaColumns.DisplayName);
                    if (idx != -1)
                        return cursor.GetString(idx);
                }
            }
            catch { }
            return null;
        }

        private DateTime? GetCreationTimeByFileName(string fileName, Context context)
        {
            try
            {
                string[] projection = { 
                    MediaStore.Video.Media.InterfaceConsts.DateAdded,
                    MediaStore.Video.Media.InterfaceConsts.DateModified,
                    MediaStore.Video.Media.InterfaceConsts.DateTaken
                };

                string selection = $"{MediaStore.Video.Media.InterfaceConsts.DisplayName} = ?";
                string[] selectionArgs = { fileName };

                using var cursor = context.ContentResolver.Query(
                    MediaStore.Video.Media.ExternalContentUri,
                    projection,
                    selection,
                    selectionArgs,
                    null);

                if (cursor != null && cursor.MoveToFirst())
                {
                    // Try to get the date taken first (most accurate for videos)
                    int dateTakenColumnIndex = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.DateTaken);
                    if (dateTakenColumnIndex != -1 && !cursor.IsNull(dateTakenColumnIndex))
                    {
                        long dateTakenMs = cursor.GetLong(dateTakenColumnIndex);
                        return DateTimeOffset.FromUnixTimeMilliseconds(dateTakenMs).LocalDateTime;
                    }

                    // Fall back to date added
                    int dateAddedColumnIndex = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.DateAdded);
                    if (dateAddedColumnIndex != -1 && !cursor.IsNull(dateAddedColumnIndex))
                    {
                        long dateAddedSeconds = cursor.GetLong(dateAddedColumnIndex);
                        return DateTimeOffset.FromUnixTimeSeconds(dateAddedSeconds).LocalDateTime;
                    }

                    // Last resort: date modified
                    int dateModifiedColumnIndex = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.DateModified);
                    if (dateModifiedColumnIndex != -1 && !cursor.IsNull(dateModifiedColumnIndex))
                    {
                        long dateModifiedSeconds = cursor.GetLong(dateModifiedColumnIndex);
                        return DateTimeOffset.FromUnixTimeSeconds(dateModifiedSeconds).LocalDateTime;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting creation time by file name: {ex.Message}");
            }

            return null;
        }

        private DateTime GetCreationTimeFromContentUri(AndroidUri contentUri, Context context)
        {
            DateTime creationTime = DateTime.Now;

            try
            {
                string[] projection = { 
                    MediaStore.Video.Media.InterfaceConsts.DateAdded,
                    MediaStore.Video.Media.InterfaceConsts.DateModified,
                    MediaStore.Video.Media.InterfaceConsts.DateTaken
                };

                using var cursor = context.ContentResolver.Query(contentUri, projection, null, null, null);
                if (cursor != null && cursor.MoveToFirst())
                {
                    // Try to get the date taken first (most accurate for videos)
                    int dateTakenColumnIndex = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.DateTaken);
                    if (dateTakenColumnIndex != -1 && !cursor.IsNull(dateTakenColumnIndex))
                    {
                        long dateTakenMs = cursor.GetLong(dateTakenColumnIndex);
                        return DateTimeOffset.FromUnixTimeMilliseconds(dateTakenMs).LocalDateTime;
                    }

                    // Fall back to date added
                    int dateAddedColumnIndex = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.DateAdded);
                    if (dateAddedColumnIndex != -1 && !cursor.IsNull(dateAddedColumnIndex))
                    {
                        long dateAddedSeconds = cursor.GetLong(dateAddedColumnIndex);
                        return DateTimeOffset.FromUnixTimeSeconds(dateAddedSeconds).LocalDateTime;
                    }

                    // Last resort: date modified
                    int dateModifiedColumnIndex = cursor.GetColumnIndex(MediaStore.Video.Media.InterfaceConsts.DateModified);
                    if (dateModifiedColumnIndex != -1 && !cursor.IsNull(dateModifiedColumnIndex))
                    {
                        long dateModifiedSeconds = cursor.GetLong(dateModifiedColumnIndex);
                        return DateTimeOffset.FromUnixTimeSeconds(dateModifiedSeconds).LocalDateTime;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting creation time from content URI: {ex.Message}");
            }

            return creationTime;
        }
    }
}
