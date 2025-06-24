namespace FileOrganizerV3;

public partial class MainPage : ContentPage
{
    private readonly FileScanner _fileScanner;
    private readonly FileMover _fileMover;
    private readonly DuplicateDetector _duplicateDetector;
    private readonly PermissionsService _permissionsService;
    private List<string> _selectedDuplicates = new();
    private readonly List<Border> _animatedCards = new();
    private bool _isOperationInProgress = false;
    private Button _activeButton = null;

    public MainPage()
    {
        InitializeComponent();
        _fileScanner = new FileScanner();
        _fileMover = new FileMover();
        _duplicateDetector = new DuplicateDetector();
        _permissionsService = new PermissionsService();
        _permissionsService = new PermissionsService();
        
        // Store cards for scroll animations
        _animatedCards.AddRange(new[] { OrganizationCard, DuplicateCard, StatsCard, LogCard });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Start entrance animations
        _ = Task.Run(async () => await AnimateEntranceAsync());
        
        try
        {
            await Task.Delay(1500); // Wait for animations to complete
            await ShowWelcomeDialog();
        }
        catch (Exception ex)
        {
            LogActivity($"Welcome dialog error: {ex.Message}");
        }
    }

    private async Task AnimateEntranceAsync()
    {
        // Enhanced header animation with bounce
        await Task.Delay(300);
        await ((VisualElement)HeaderSection.Children[0]).FadeTo(1, 1000, Easing.CubicOut);
        await ((VisualElement)HeaderSection.Children[0]).ScaleTo(1.1, 200, Easing.CubicOut);
        await ((VisualElement)HeaderSection.Children[0]).ScaleTo(1, 200, Easing.CubicOut);
        
        await Task.Delay(200);
        await ((VisualElement)HeaderSection.Children[1]).FadeTo(1, 800, Easing.CubicOut);

        // Spectacular card entrance with stagger
        for (int i = 0; i < _animatedCards.Count; i++)
        {
            var card = _animatedCards[i];
            _ = Task.Run(async () =>
            {
                await Task.Delay(i * 200);
                // Slide in from different directions
                var startX = i % 2 == 0 ? -400 : 400;
                card.TranslationX = startX;
                
                var animateIn = Task.WhenAll(
                    card.TranslateTo(0, 0, 800, Easing.CubicOut),
                    card.ScaleTo(1, 800, Easing.SpringOut),
                    card.FadeTo(1, 600, Easing.CubicOut)
                );
                await animateIn;
                
                // Add a little bounce at the end
                await card.ScaleTo(1.05, 150, Easing.CubicOut);
                await card.ScaleTo(1, 150, Easing.CubicOut);
            });
        }
    }

    private async void OnScrolled(object sender, ScrolledEventArgs e)
    {
        // Cool scroll-based animations
        var scrollView = sender as ScrollView;
        var scrollY = e.ScrollY;
        var viewportHeight = scrollView.Height;

        foreach (var card in _animatedCards)
        {
            // Get card position relative to viewport
            var cardY = card.Y - scrollY;
            var cardCenterY = cardY + (card.Height / 2);
            var viewportCenterY = viewportHeight / 2;
            
            // Calculate distance from viewport center (0 to 1)
            var distanceFromCenter = Math.Abs(cardCenterY - viewportCenterY) / viewportCenterY;
            distanceFromCenter = Math.Min(distanceFromCenter, 1.0);
            
            // Calculate scale and opacity based on distance
            var scale = Math.Max(0.85, 1.0 - (distanceFromCenter * 0.15));
            var opacity = Math.Max(0.6, 1.0 - (distanceFromCenter * 0.4));
            
            // Apply smooth transformations
            await Task.WhenAll(
                card.ScaleTo(scale, 100, Easing.CubicOut),
                card.FadeTo(opacity, 100, Easing.CubicOut)
            );
        }
    }

    private async Task ShowWelcomeDialog()
    {
        string welcomeMessage = "Welcome to File Organizer V3! 🚀\n\n" +
                               "• Smart file organization\n" +
                               "• NEW: Multi-folder duplicate detection (Downloads/Documents/Pictures/DCIM)\n" +
                               "• NEW: Enhanced duplicate deletion with options\n\n" +
                               "Scroll and interact to see the magic! ✨";

        await DisplayAlert("Welcome", welcomeMessage, "Let's Go! 🚀");
    }

    private async void OnSortFilesClicked(object sender, EventArgs e)
    {
        try
        {
            await AnimateButtonWithShimmer(SortFilesButton, SortButtonShimmer);
            await SetUIBusy(true, "🔄 Organizing files...", SortFilesButton);

            string downloadsPath;
            try
            {
                downloadsPath = await _permissionsService.GetDownloadsPathWithPermissionCheck();
            }
            catch (UnauthorizedAccessException)
            {
                await DisplayAlert("Permissions Required", 
                    "Storage permissions are required to access files. Please grant permissions in your device settings.", 
                    "OK");
                return;
            }
            catch (DirectoryNotFoundException)
            {
                await DisplayAlert("Error", "Could not access Downloads folder", "OK");
                return;
            }
            
            if (string.IsNullOrEmpty(downloadsPath) || !Directory.Exists(downloadsPath))
            {
                await DisplayAlert("Error", "Could not access Downloads folder", "OK");
                return;
            }

            LogActivity($"📂 Scanning: {downloadsPath}");

            await Task.Run(() => _fileScanner.FilterByType(downloadsPath));

            int totalFiles = _fileScanner.FileGroups.Sum(group => group.Value.Count);
            
            if (totalFiles == 0)
            {
                LogActivity("ℹ️ No files found to organize");
                await DisplayAlert("Info", "No files found to organize in Downloads folder", "OK");
                return;
            }

            LogActivity($"📊 Found {totalFiles} files to organize");

            int processedFiles = 0;

            foreach (var category in _fileScanner.FileGroups)
            {
                foreach (string file in category.Value)
                {
                    try
                    {
                        string fileName = Path.GetFileName(file);
                        
                        // Update progress with real-time info
                        var progress = (double)processedFiles / totalFiles;
                        var percentage = (int)(progress * 100);
                        
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            ProgressLabel.Text = $"📁 Moving {fileName} → {category.Key} ({processedFiles + 1}/{totalFiles} - {percentage}%)";
                            UpdateQuickStats($"📂 Processing: {processedFiles + 1}/{totalFiles} files ({percentage}%)");
                        });
                        
                        await _fileMover.MoveFileToCategory(file, category.Key);
                        
                        LogActivity($"✅ Moved: {fileName} → {category.Key}");
                        
                        processedFiles++;
                        await UpdateProgressAsync((double)processedFiles / totalFiles);
                        
                        // Small delay to show the progress visually
                        await Task.Delay(50);
                    }
                    catch (Exception ex)
                    {
                        string fileName = Path.GetFileName(file);
                        LogActivity($"❌ Failed to move {fileName}: {ex.Message}");
                        processedFiles++; // Still count as processed to maintain progress accuracy
                    }
                }
            }

            await AnimateSpectacularSuccess();
            UpdateQuickStats($"✨ Organized {processedFiles} files into categories");
            LogActivity($"�� Organization complete! Processed {processedFiles} files");
            await DisplayAlert("Success! 🎉", $"Successfully organized {processedFiles} files!", "Awesome! 🚀");
        }
        catch (Exception ex)
        {
            LogActivity($"�� Error during organization: {ex.Message}");
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
        finally
        {
            await SetUIBusy(false);
        }
    }

    private async void OnFindDuplicatesClicked(object sender, EventArgs e)
    {
        try
        {
            await AnimateButtonWithShimmer(FindDuplicatesButton, DuplicateButtonShimmer);
            await SetUIBusy(true, "🔍 Finding duplicates across Downloads, Documents, Pictures...", FindDuplicatesButton);

            // Get multiple folder paths to search
            var foldersToSearch = new List<string>();
            var folderNames = new List<string>();
            
            try
            {
                // Downloads folder
                var downloadsPath = await _permissionsService.GetDownloadsPathWithPermissionCheck();
                if (!string.IsNullOrEmpty(downloadsPath) && Directory.Exists(downloadsPath))
                {
                    foldersToSearch.Add(downloadsPath);
                    folderNames.Add("Downloads");
                }
                
                // Documents folder
#if ANDROID
                var documentsPath = Android.OS.Environment.GetExternalStoragePublicDirectory(
                    Android.OS.Environment.DirectoryDocuments)?.AbsolutePath;
                if (!string.IsNullOrEmpty(documentsPath) && Directory.Exists(documentsPath))
                {
                    foldersToSearch.Add(documentsPath);
                    folderNames.Add("Documents");
                }
                
                // Pictures folder
                var picturesPath = Android.OS.Environment.GetExternalStoragePublicDirectory(
                    Android.OS.Environment.DirectoryPictures)?.AbsolutePath;
                if (!string.IsNullOrEmpty(picturesPath) && Directory.Exists(picturesPath))
                {
                    foldersToSearch.Add(picturesPath);
                    folderNames.Add("Pictures");
                }
                
                // DCIM folder (Camera photos)
                var dcimPath = Android.OS.Environment.GetExternalStoragePublicDirectory(
                    Android.OS.Environment.DirectoryDcim)?.AbsolutePath;
                if (!string.IsNullOrEmpty(dcimPath) && Directory.Exists(dcimPath))
                {
                    foldersToSearch.Add(dcimPath);
                    folderNames.Add("DCIM");
                }
#else
                // For other platforms, add standard paths
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var documentsPath = Path.Combine(userProfile, "Documents");
                var picturesPath = Path.Combine(userProfile, "Pictures");
                
                if (Directory.Exists(documentsPath))
                {
                    foldersToSearch.Add(documentsPath);
                    folderNames.Add("Documents");
                }
                
                if (Directory.Exists(picturesPath))
                {
                    foldersToSearch.Add(picturesPath);
                    folderNames.Add("Pictures");
                }
#endif
            }
            catch (UnauthorizedAccessException)
            {
                await DisplayAlert("Permissions Required", 
                    "Storage permissions are required to access files. Please grant permissions in your device settings.", 
                    "OK");
                return;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Could not access folders: {ex.Message}", "OK");
                return;
            }
            
            if (foldersToSearch.Count == 0)
            {
                await DisplayAlert("Error", "Could not access any folders for scanning", "OK");
                return;
            }

            LogActivity($"🔍 Scanning for duplicates in: {string.Join(", ", folderNames)}");

            // Debug: Check files in all directories
            var allFiles = new List<string>();
            foreach (var folder in foldersToSearch)
            {
                try
                {
                    var folderFiles = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
                    allFiles.AddRange(folderFiles);
                    LogActivity($"📂 Debug: {Path.GetFileName(folder)} - {folderFiles.Length} files");
                }
                catch (Exception ex)
                {
                    LogActivity($"❌ Debug: Error accessing {Path.GetFileName(folder)}: {ex.Message}");
                }
            }
            
            LogActivity($"📊 Debug: Found {allFiles.Count} total files across all folders");
            
            // Log first few files for debugging
            for (int i = 0; i < Math.Min(5, allFiles.Count); i++)
            {
                LogActivity($"📄 Debug: File {i + 1}: {Path.GetFileName(allFiles[i])}");
            }

            var progress = new Progress<string>(message =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ProgressLabel.Text = message;
                    LogActivity(message);
                });
            });

            // Find duplicates across all folders by scanning all files together
            var duplicates = await _duplicateDetector.FindDuplicatesInMultipleFoldersAsync(foldersToSearch, progress);
            
            // Debug: Log the actual result
            LogActivity($"🔍 Debug: Duplicate detection returned {duplicates.Count} groups across all folders");

            if (duplicates.Count == 0)
            {
                await AnimateSpectacularSuccess();
                LogActivity("✨ No duplicates found!");
                await DisplayAlert("Great News! ✨", "No duplicate files found!", "Excellent! 🎉");
                return;
            }

            long wastedSpace = await _duplicateDetector.GetTotalWastedSpaceAsync();
            string wastedSpaceFormatted = FormatFileSize(wastedSpace);

            UpdateQuickStats($"🔍 Found {duplicates.Count} duplicate groups");
            LogActivity($"🔍 Found {duplicates.Count} duplicate groups, wasting {wastedSpaceFormatted}");
            
            // Debug: Log that we're about to show action sheet
            LogActivity($"🔍 Debug: About to show action sheet for {duplicates.Count} groups");
            
            // Ask user what to do with duplicates
            string action = await DisplayActionSheet(
                $"🔍 Found {duplicates.Count} groups of duplicate files!\nWasted space: {wastedSpaceFormatted}\n\nWhat would you like to do?",
                "Cancel",
                null,
                "🗑️ Delete Oldest Files",
                "🗑️ Delete Newest Files",
                "📋 View Details Only"
            );
            
            // Debug: Log user's choice
            LogActivity($"🔍 Debug: User selected: {action ?? "NULL"}");

            if (action == "🗑️ Delete Oldest Files")
            {
                await DeleteDuplicates(true); // true = delete oldest
            }
            else if (action == "🗑️ Delete Newest Files")
            {
                await DeleteDuplicates(false); // false = delete newest
            }
            else if (action == "📋 View Details Only")
            {
                await ShowDuplicateDetails();
            }
        }
        catch (Exception ex)
        {
            LogActivity($"💥 Error during duplicate detection: {ex.Message}");
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
        finally
        {
            await SetUIBusy(false);
        }
    }

    private async void OnClearLogClicked(object sender, EventArgs e)
    {
        await AnimateButtonWithShimmer(ClearLogButton, ClearButtonShimmer);
        ActivityLog.Text = "🧹 Log cleared...";
        UpdateQuickStats("🧹 Log cleared");
    }

    private async void OnExportLogClicked(object sender, EventArgs e)
    {
        try
        {
            await AnimateButtonWithShimmer(ExportLogButton, ExportButtonShimmer);
            string logContent = ActivityLog.Text;
            string fileName = $"FileOrganizerLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            
            await DisplayAlert("Export Log 📤", $"Log exported as {fileName}\n\n{logContent}", "Great!");
            LogActivity($"📤 Log exported as {fileName}");
        }
        catch (Exception ex)
        {
            LogActivity($"❌ Export failed: {ex.Message}");
        }
    }

    private async Task AnimateButtonWithShimmer(Button button, Border shimmer)
    {
        try
        {
            // Haptic feedback for touch responsiveness
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch
        {
            // Haptic feedback not available on this device
        }
        
        // Enhanced button press animation with immediate feedback
        var pressTask = Task.WhenAll(
            button.ScaleTo(0.92, 80, Easing.CubicOut),
            button.FadeTo(0.7, 80, Easing.CubicOut)
        );
        
        // Enhanced shimmer effect
        var shimmerTask = Task.WhenAll(
            shimmer.FadeTo(0.8, 50, Easing.CubicOut),
            shimmer.TranslateTo(450, 0, 350, Easing.CubicOut)
        );
        
        await Task.WhenAll(pressTask, shimmerTask);
        
        // Bounce back animation
        await Task.WhenAll(
            button.ScaleTo(1.02, 60, Easing.SpringOut),
            button.FadeTo(1, 60, Easing.CubicOut)
        );
        
        await button.ScaleTo(1, 40, Easing.CubicOut);
        
        // Reset shimmer
        await Task.WhenAll(
            shimmer.FadeTo(0, 50, Easing.CubicOut),
            shimmer.TranslateTo(-450, 0, 50, Easing.CubicOut)
        );
    }

    private async Task AnimateSpectacularSuccess()
    {
        // Multi-stage success animation
        var tasks = new List<Task>();
        
        // Pulse all cards
        foreach (var card in _animatedCards)
        {
            tasks.Add(Task.Run(async () =>
            {
                for (int i = 0; i < 3; i++)
                {
                    await card.ScaleTo(1.08, 200, Easing.CubicOut);
                    await card.ScaleTo(1, 200, Easing.CubicOut);
                    await Task.Delay(100);
                }
            }));
        }
        
        // Add sparkle effect to stats card
        tasks.Add(Task.Run(async () =>
        {
            for (int i = 0; i < 5; i++)
            {
                await StatsCard.FadeTo(0.7, 100, Easing.CubicOut);
                await StatsCard.FadeTo(1, 100, Easing.CubicOut);
                await Task.Delay(80);
            }
        }));
        
        await Task.WhenAll(tasks);
    }

    private void LogActivity(string message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string logEntry = $"[{timestamp}] {message}";
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ActivityLog.Text += Environment.NewLine + logEntry;
        });
    }

    private void UpdateQuickStats(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            QuickStatsLabel.Text = $"⚡ {message}";
        });
    }

    private async Task SetUIBusy(bool isBusy, string operation = "", Button activeButton = null)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            _isOperationInProgress = isBusy;
            
            // Disable all buttons and show loading states
            SortFilesButton.IsEnabled = !isBusy;
            FindDuplicatesButton.IsEnabled = !isBusy;
            ClearLogButton.IsEnabled = !isBusy;
            ExportLogButton.IsEnabled = !isBusy;
            
            if (isBusy)
            {
                _activeButton = activeButton;
                
                // Show immediate feedback - button is working
                if (_activeButton != null)
                {
                    _activeButton.Text = "⏳ Working...";
                    _activeButton.BackgroundColor = Color.FromArgb("#4338CA"); // Darker shade
                    
                    // Start pulse animation for active button
                    _ = StartButtonPulseAnimation(_activeButton);
                }
                
                // Show progress section immediately
                ProgressSection.IsVisible = true;
                ProgressLabel.Text = operation;
                ProgressBar.Progress = 0;
                
                // Immediate visibility for instant feedback
                ProgressBar.Opacity = 1;
                ProgressLabel.Opacity = 1;
                
                // Spectacular progress bar animation
                await Task.WhenAll(
                    ProgressBar.ScaleTo(1, 400, Easing.SpringOut),
                    ProgressLabel.FadeTo(1, 200, Easing.CubicOut)
                );
                
                // Start progress bar pulsing animation
                _ = StartProgressBarPulseAnimation();
            }
            else
            {
                // Stop all animations first
                _isOperationInProgress = false;
                
                // Restore button states
                if (_activeButton != null)
                {
                    // Stop any ongoing scale animation
                    _activeButton.AbortAnimation("ScaleAnimation");
                    
                    // Reset scale immediately
                    _activeButton.Scale = 1.0;
                    
                    // Restore original button appearance
                    if (_activeButton == SortFilesButton)
                        _activeButton.Text = "✨ Sort Downloads Folder";
                    else if (_activeButton == FindDuplicatesButton)
                        _activeButton.Text = "🔎 Find Duplicates";
                    else if (_activeButton == ClearLogButton)
                        _activeButton.Text = "🧹 Clear";
                    else if (_activeButton == ExportLogButton)
                        _activeButton.Text = "📤 Export";
                        
                    _activeButton.BackgroundColor = null; // Reset to original
                    _activeButton = null;
                }
                
                // Stop progress bar animation
                ProgressBar.AbortAnimation("FadeAnimation");
                ProgressBar.Opacity = 1.0;
                
                // Show completion state briefly before hiding
                ProgressLabel.Text = "✅ Complete!";
                await Task.Delay(1000);
                
                await Task.WhenAll(
                    ProgressBar.ScaleTo(0, 400, Easing.CubicIn),
                    ProgressLabel.FadeTo(0, 400, Easing.CubicIn)
                );
                ProgressSection.IsVisible = false;
                ProgressBar.Progress = 0;
            }
        });
    }

    private async Task StartButtonPulseAnimation(Button button)
    {
        try
        {
            while (_isOperationInProgress && button != null)
            {
                if (!_isOperationInProgress) break;
                
                await button.ScaleTo(1.05, 600, Easing.SinInOut);
                
                if (!_isOperationInProgress) break;
                
                await button.ScaleTo(1.0, 600, Easing.SinInOut);
            }
        }
        catch
        {
            // Animation was cancelled, reset scale
            if (button != null)
            {
                button.Scale = 1.0;
            }
        }
    }

    private async Task StartProgressBarPulseAnimation()
    {
        try
        {
            while (_isOperationInProgress)
            {
                if (!_isOperationInProgress) break;
                
                await ProgressBar.FadeTo(0.7, 800, Easing.SinInOut);
                
                if (!_isOperationInProgress) break;
                
                await ProgressBar.FadeTo(1.0, 800, Easing.SinInOut);
            }
        }
        catch
        {
            // Animation was cancelled, reset opacity
            ProgressBar.Opacity = 1.0;
        }
    }

    private async Task UpdateProgressAsync(double progress)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await ProgressBar.ProgressTo(progress, 300, Easing.CubicOut);
        });
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

    private async Task DeleteDuplicates(bool deleteOldest)
    {
        try
        {
            await SetUIBusy(true, $"🗑️ Deleting {(deleteOldest ? "oldest" : "newest")} duplicate files...", null);
            
            var filesToDelete = new List<string>();
            
            // Build list of files to delete based on user choice
            foreach (var group in _duplicateDetector.DuplicateGroups.Values)
            {
                if (group.Count > 1)
                {
                    // Sort files by creation time
                    var sortedFiles = group.Select(file => new FileInfo(file))
                                           .Where(fi => fi.Exists)
                                           .OrderBy(fi => fi.CreationTime)
                                           .ToList();
                    
                    if (sortedFiles.Count > 1)
                    {
                        if (deleteOldest)
                        {
                            // Delete all but the newest (last in sorted list)
                            filesToDelete.AddRange(sortedFiles.Take(sortedFiles.Count - 1).Select(fi => fi.FullName));
                        }
                        else
                        {
                            // Delete all but the oldest (first in sorted list)
                            filesToDelete.AddRange(sortedFiles.Skip(1).Select(fi => fi.FullName));
                        }
                    }
                }
            }

            if (filesToDelete.Count == 0)
            {
                LogActivity("ℹ️ No files to delete");
                await DisplayAlert("Info", "No duplicate files to delete", "OK");
                return;
            }

            // Confirm deletion
            bool confirm = await DisplayAlert("Confirm Deletion ⚠️", 
                $"Are you sure you want to delete {filesToDelete.Count} duplicate files?\n\nThis action cannot be undone!", 
                "Yes, Delete", "Cancel");

            if (!confirm)
            {
                LogActivity("❌ Deletion cancelled by user");
                return;
            }

            LogActivity($"🗑️ Starting deletion of {filesToDelete.Count} duplicate files...");

            // Calculate space that will be freed BEFORE deletion
            long spaceToFree = 0;
            foreach (var file in filesToDelete)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.Exists)
                    {
                        spaceToFree += fileInfo.Length;
                    }
                }
                catch { }
            }

            var progress = new Progress<string>(message =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ProgressLabel.Text = message;
                    LogActivity(message);
                });
            });

            int deletedCount = await _duplicateDetector.DeleteDuplicatesAsync(filesToDelete, progress);
            
            await AnimateSpectacularSuccess();
            UpdateQuickStats($"🗑️ Deleted {deletedCount} duplicate files");
            LogActivity($"✅ Successfully deleted {deletedCount} duplicate files!");
            
            // Calculate actual space freed based on deletion success rate
            long spaceFreed = (long)(spaceToFree * ((double)deletedCount / filesToDelete.Count));
            
            string spaceFreedFormatted = FormatFileSize(spaceFreed);
            await DisplayAlert("Success! 🎉", 
                $"Successfully deleted {deletedCount} duplicate files!\nSpace freed: {spaceFreedFormatted}", 
                "Awesome! 🚀");
        }
        catch (Exception ex)
        {
            LogActivity($"❌ Error during deletion: {ex.Message}");
            await DisplayAlert("Error", $"An error occurred during deletion: {ex.Message}", "OK");
        }
        finally
        {
            await SetUIBusy(false);
        }
    }

    private async Task ShowDuplicateDetails()
    {
        try
        {
            string details = "📋 DUPLICATE FILES DETAILS:\n\n";
            int groupNumber = 1;
            
            foreach (var group in _duplicateDetector.DuplicateGroups.Values.Take(5)) // Show first 5 groups
            {
                details += $"Group {groupNumber}:\n";
                
                foreach (var file in group)
                {
                    string fileName = Path.GetFileName(file);
                    string fileSize = _duplicateDetector.GetFileSize(file);
                    string fileDate = "";
                    
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        fileDate = fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm");
                    }
                    catch { }
                    
                    details += $"  📄 {fileName} ({fileSize}) - {fileDate}\n";
                }
                details += "\n";
                groupNumber++;
            }
            
            if (_duplicateDetector.DuplicateGroups.Count > 5)
            {
                details += $"... and {_duplicateDetector.DuplicateGroups.Count - 5} more groups\n";
            }
            
            long wastedSpace = await _duplicateDetector.GetTotalWastedSpaceAsync();
            details += $"\n💾 Total wasted space: {FormatFileSize(wastedSpace)}";
            
            await DisplayAlert("Duplicate Details 📋", details, "OK");
            LogActivity("📋 Showed duplicate file details");
        }
        catch (Exception ex)
        {
            LogActivity($"❌ Error showing details: {ex.Message}");
            await DisplayAlert("Error", $"Error showing details: {ex.Message}", "OK");
        }
    }
}
