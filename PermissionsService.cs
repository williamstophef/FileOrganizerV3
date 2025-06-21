#if ANDROID
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Android;
#endif

namespace FileOrganizerV3
{
    public class PermissionsService
    {
        public async Task<bool> RequestStoragePermissionsAsync()
        {
#if ANDROID
            try
            {
                // Check Android version
                var status = await Permissions.RequestAsync<Permissions.StorageRead>();
                if (status != PermissionStatus.Granted)
                {
                    return false;
                }

                status = await Permissions.RequestAsync<Permissions.StorageWrite>();
                if (status != PermissionStatus.Granted)
                {
                    return false;
                }

                // For Android 11+ (API 30+), we might need MANAGE_EXTERNAL_STORAGE
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.R)
                {
                    if (!Android.OS.Environment.IsExternalStorageManager)
                    {
                        // Guide user to settings for MANAGE_EXTERNAL_STORAGE
                        var intent = new Android.Content.Intent();
                        intent.SetAction(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission);
                        intent.SetData(Android.Net.Uri.Parse($"package:{Platform.CurrentActivity?.PackageName}"));
                        Platform.CurrentActivity?.StartActivity(intent);
                        
                        return false; // User needs to manually grant in settings
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Permission request error: {ex.Message}");
                return false;
            }
#else
            return true; // Non-Android platforms
#endif
        }

        public bool HasStoragePermissions()
        {
#if ANDROID
            try
            {
                // Check if we have basic storage permissions
                var readPermission = ContextCompat.CheckSelfPermission(Platform.CurrentActivity, 
                    Manifest.Permission.ReadExternalStorage) == Android.Content.PM.Permission.Granted;
                
                var writePermission = ContextCompat.CheckSelfPermission(Platform.CurrentActivity, 
                    Manifest.Permission.WriteExternalStorage) == Android.Content.PM.Permission.Granted;

                // For Android 11+, also check MANAGE_EXTERNAL_STORAGE
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.R)
                {
                    return readPermission && writePermission && Android.OS.Environment.IsExternalStorageManager;
                }

                return readPermission && writePermission;
            }
            catch
            {
                return false;
            }
#else
            return true;
#endif
        }

        public async Task<string> GetDownloadsPathWithPermissionCheck()
        {
#if ANDROID
            if (!HasStoragePermissions())
            {
                bool granted = await RequestStoragePermissionsAsync();
                if (!granted)
                {
                    throw new UnauthorizedAccessException("Storage permissions not granted");
                }
            }

            // Get the actual Downloads path
            var downloadsPath = Android.OS.Environment.GetExternalStoragePublicDirectory(
                Android.OS.Environment.DirectoryDownloads)?.AbsolutePath;
            
            if (!string.IsNullOrEmpty(downloadsPath) && Directory.Exists(downloadsPath))
            {
                return downloadsPath;
            }
            
            throw new DirectoryNotFoundException("Downloads folder not accessible");
#else
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
#endif
        }
    }
}
