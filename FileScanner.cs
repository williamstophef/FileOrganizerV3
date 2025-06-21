namespace FileOrganizerV3
{
    public class FileScanner
    {
        public Dictionary<string, List<string>> FileGroups { get; set; } = new();
        public Dictionary<string, string> FileExtension { get; set; } = new();

        public FileScanner()
        {
            FileGroups = new Dictionary<string, List<string>>();
            FileExtension = new(StringComparer.OrdinalIgnoreCase)
            {
                // Images
                {".jpg", "Images" },
                {".jpeg", "Images" },
                {".png", "Images" },
                {".gif", "Images" },
                {".webp", "Images" },
                {".bmp", "Images" },
                {".tiff", "Images" },
                {".tif", "Images" },
                {".svg", "Images" },
                {".ico", "Images" },
                {".raw", "Images" },
                {".heic", "Images" },
                {".heif", "Images" },
                
                // Audio/Music
                {".mp3", "Music" },
                {".wav", "Music" },
                {".flac", "Music" },
                {".aac", "Music" },
                {".ogg", "Music" },
                {".m4a", "Music" },
                {".opus", "Music" },
                {".wma", "Music" },
                
                // Videos
                {".mp4", "Videos" },
                {".mov", "Videos" },
                {".avi", "Videos" },
                {".mkv", "Videos" },
                {".webm", "Videos" },
                {".m4v", "Videos" },
                {".3gp", "Videos" },
                {".3g2", "Videos" },
                {".wmv", "Videos" },
                {".flv", "Videos" },
                
                // Documents
                {".pdf", "Documents" },
                {".txt", "Documents" },
                {".doc", "Documents" },
                {".docx", "Documents" },
                {".xls", "Documents" },
                {".xlsx", "Documents" },
                {".ppt", "Documents" },
                {".pptx", "Documents" },
                {".csv", "Documents" },
                {".rtf", "Documents" },
                {".odt", "Documents" },
                {".ods", "Documents" },
                {".odp", "Documents" },
                
                // Archives
                {".zip", "Archives" },
                {".rar", "Archives" },
                {".7z", "Archives" },
                {".tar", "Archives" },
                {".gz", "Archives" },
                {".apk", "Archives" },
                
                // Other common mobile file types
                {".json", "Documents" },
                {".xml", "Documents" },
                {".html", "Documents" },
                {".epub", "Documents" },
                {".mobi", "Documents" }
            };
        }

        public List<string> GetFiles(string sourceDirectory)
        {
            if (!Directory.Exists(sourceDirectory))
            {
                throw new DirectoryNotFoundException("Source directory does not exist.");
            }
            return new List<string>(Directory.GetFiles(sourceDirectory, "*", SearchOption.TopDirectoryOnly));
        }

        public void FilterByType(string sourceDirectory, List<string> skipPaths = null)
        {
            FileGroups.Clear();
            List<string> allFiles = GetFiles(sourceDirectory);

            HashSet<string> normalizedSkips = skipPaths != null
                ? new HashSet<string>(skipPaths.Select(p => Path.GetFullPath(p).ToLower()))
                : new HashSet<string>();

            foreach (var file in allFiles)
            {
                try
                {
                    string normalized = Path.GetFullPath(file).ToLower();
                    if (normalizedSkips.Contains(normalized))
                    {
                        continue;
                    }
                    
                    string ext = Path.GetExtension(file).ToLower();
                    string category;
                    
                    if (FileExtension.TryGetValue(ext, out category))
                    {
                        if (!FileGroups.ContainsKey(category))
                            FileGroups[category] = new List<string>();

                        FileGroups[category].Add(file);
                    }
                    else
                    {
                        category = "Miscellaneous";
                        if (!FileGroups.ContainsKey(category))
                            FileGroups[category] = new List<string>();
                            
                        FileGroups[category].Add(file);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {file}: {ex.Message}");
                }
            }
        }
    }
}
