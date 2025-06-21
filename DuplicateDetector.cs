using System.Security.Cryptography;

namespace FileOrganizerV3
{
    public class DuplicateDetector
    {
        public Dictionary<string, List<string>> DuplicateGroups { get; set; } = new();

        public async Task<Dictionary<string, List<string>>> FindDuplicatesAsync(string directoryPath, IProgress<string> progress = null)
        {
            DuplicateGroups.Clear();
            var fileHashes = new Dictionary<string, List<string>>();

            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    throw new DirectoryNotFoundException($"Directory does not exist: {directoryPath}");
                }

                var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
                progress?.Report($"Found {files.Length} files to analyze");

                int processedFiles = 0;
                foreach (var file in files)
                {
                    try
                    {
                        processedFiles++;
                        progress?.Report($"Processing {processedFiles}/{files.Length}: {Path.GetFileName(file)}");

                        string hash = await CalculateFileHashAsync(file);
                        
                        if (!fileHashes.ContainsKey(hash))
                        {
                            fileHashes[hash] = new List<string>();
                        }
                        
                        fileHashes[hash].Add(file);
                    }
                    catch (Exception ex)
                    {
                        progress?.Report($"Error processing {file}: {ex.Message}");
                        continue;
                    }
                }

                foreach (var kvp in fileHashes.Where(h => h.Value.Count > 1))
                {
                    string duplicateKey = $"Group_{DuplicateGroups.Count + 1}_{Path.GetFileName(kvp.Value.First())}";
                    DuplicateGroups[duplicateKey] = kvp.Value;
                }

                progress?.Report($"Analysis complete. Found {DuplicateGroups.Count} duplicate groups");
                return DuplicateGroups;
            }
            catch (Exception ex)
            {
                progress?.Report($"Error during duplicate detection: {ex.Message}");
                throw;
            }
        }

        private async Task<string> CalculateFileHashAsync(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var sha256 = SHA256.Create();
            
            var hashBytes = await Task.Run(() => sha256.ComputeHash(stream));
            return Convert.ToBase64String(hashBytes);
        }

        public Task<long> GetTotalWastedSpaceAsync()
        {
            long totalWasted = 0;

            foreach (var group in DuplicateGroups.Values)
            {
                if (group.Count > 1)
                {
                    for (int i = 1; i < group.Count; i++)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(group[i]);
                            totalWasted += fileInfo.Length;
                        }
                        catch
                        {
                        }
                    }
                }
            }

            return Task.FromResult(totalWasted);
        }

        public Task<int> DeleteDuplicatesAsync(List<string> filesToDelete, IProgress<string> progress = null)
        {
            int deletedCount = 0;

            foreach (var file in filesToDelete)
            {
                try
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                        deletedCount++;
                        progress?.Report($"Deleted: {Path.GetFileName(file)}");
                    }
                }
                catch (Exception ex)
                {
                    progress?.Report($"Failed to delete {Path.GetFileName(file)}: {ex.Message}");
                }
            }

            progress?.Report($"Deleted {deletedCount} duplicate files");
            return Task.FromResult(deletedCount);
        }

        public string GetFileSize(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                return FormatFileSize(fileInfo.Length);
            }
            catch
            {
                return "Unknown";
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
