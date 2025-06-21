namespace FileOrganizerV3
{
    public class FileMover
    {
        public Task<string> GetDownloadsPath()
        {
            try
            {
#if ANDROID
                var downloadsPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads)?.AbsolutePath;
                if (!string.IsNullOrEmpty(downloadsPath) && Directory.Exists(downloadsPath))
                {
                    return Task.FromResult(downloadsPath);
                }
#endif
                return Task.FromResult(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
            }
            catch (Exception)
            {
                return Task.FromResult(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
            }
        }

        public async Task<string> GetCategoryPath(string category)
        {
            try
            {
#if ANDROID
                return category.ToLower() switch
                {
                    "images" => Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures)?.AbsolutePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Pictures"),
                    "videos" => Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies)?.AbsolutePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Videos"),
                    "music" => Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic)?.AbsolutePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Music"),
                    "documents" => Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments)?.AbsolutePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents"),
                    "archives" => Path.Combine(await GetDownloadsPath(), "Archives"),
                    _ => Path.Combine(await GetDownloadsPath(), "Miscellaneous")
                };
#else
                string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return category.ToLower() switch
                {
                    "images" => Path.Combine(homeDirectory, "Pictures"),
                    "videos" => Path.Combine(homeDirectory, "Videos"),
                    "music" => Path.Combine(homeDirectory, "Music"),
                    "documents" => Path.Combine(homeDirectory, "Documents"),
                    "archives" => Path.Combine(homeDirectory, "Downloads", "Archives"),
                    _ => Path.Combine(homeDirectory, "Downloads", "Miscellaneous")
                };
#endif
            }
            catch (Exception)
            {
                string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(homeDirectory, "Downloads", "Miscellaneous");
            }
        }

        public async Task<string> MoveFileToCategory(string sourcePath, string category)
        {
            try
            {
                string destinationFolder = await GetCategoryPath(category);
                
                if (!Directory.Exists(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                }

                string fileName = Path.GetFileName(sourcePath);
                string destinationPath = Path.Combine(destinationFolder, fileName);

                if (File.Exists(destinationPath))
                {
                    string filenameWithoutExtension = Path.GetFileNameWithoutExtension(destinationPath);
                    string extension = Path.GetExtension(destinationPath);
                    int counter = 1;

                    do
                    {
                        destinationPath = Path.Combine(destinationFolder, $"{filenameWithoutExtension}({counter}){extension}");
                        counter++;
                    } while (File.Exists(destinationPath));
                }

                File.Move(sourcePath, destinationPath);
                return destinationPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to move file from {sourcePath} to {category}: {ex.Message}");
            }
        }
    }
}
