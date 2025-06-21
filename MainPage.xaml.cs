namespace FileOrganizerV3;

public partial class MainPage : ContentPage
{
    private readonly FileScanner _fileScanner;
    private readonly FileMover _fileMover;
    private readonly DuplicateDetector _duplicateDetector;
    private readonly PermissionsService _permissionsService;
    private List<string> _selectedDuplicates = new();
    private readonly List<Border> _animatedCards = new();

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
                               "🎨 STUNNING FEATURES:\n" +
                               "• Beautiful animations and effects\n" +
                               "• Scroll-based card transformations\n" +
                               "• Shimmer button effects\n" +
                               "• Smart file organization\n" +
                               "• Advanced duplicate detection\n\n" +
                               "Scroll and interact to see the magic! ✨";

        await DisplayAlert("Welcome", welcomeMessage, "Let's Go! 🚀");
    }

    private async void OnSortFilesClicked(object sender, EventArgs e)
    {
        try
        {
            await AnimateButtonWithShimmer(SortFilesButton, SortButtonShimmer);
            SetUIBusy(true, "🔄 Organizing files with style...");

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
                        await _fileMover.MoveFileToCategory(file, category.Key);
                        
                        LogActivity($"✅ Moved: {fileName} → {category.Key}");
                        
                        processedFiles++;
                        await UpdateProgressAsync((double)processedFiles / totalFiles);
                    }
                    catch (Exception ex)
                    {
                        string fileName = Path.GetFileName(file);
                        LogActivity($"❌ Failed to move {fileName}: {ex.Message}");
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
            SetUIBusy(false);
        }
    }

    private async void OnFindDuplicatesClicked(object sender, EventArgs e)
    {
        try
        {
            await AnimateButtonWithShimmer(FindDuplicatesButton, DuplicateButtonShimmer);
            SetUIBusy(true, "🔍 Finding duplicates with precision...");

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

            LogActivity($"🔍 Scanning for duplicates in: {downloadsPath}");

            var progress = new Progress<string>(message =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ProgressLabel.Text = message;
                    LogActivity(message);
                });
            });

            var duplicates = await _duplicateDetector.FindDuplicatesAsync(downloadsPath, progress);

            if (duplicates.Count == 0)
            {
                await AnimateSpectacularSuccess();
                LogActivity("✨ No duplicates found!");
                await DisplayAlert("Great News! ✨", "No duplicate files found!", "Excellent! ��");
                return;
            }

            long wastedSpace = await _duplicateDetector.GetTotalWastedSpaceAsync();
            string wastedSpaceFormatted = FormatFileSize(wastedSpace);

            UpdateQuickStats($"🔍 Found {duplicates.Count} duplicate groups");
            LogActivity($"🔍 Found {duplicates.Count} duplicate groups, wasting {wastedSpaceFormatted}");
            
            await DisplayAlert("Duplicates Found! 🔍", 
                $"Found {duplicates.Count} groups of duplicate files\nWasted space: {wastedSpaceFormatted}", 
                "OK");
        }
        catch (Exception ex)
        {
            LogActivity($"💥 Error during duplicate detection: {ex.Message}");
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
        finally
        {
            SetUIBusy(false);
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
        // Button press animation
        var pressTask = Task.WhenAll(
            button.ScaleTo(0.95, 100, Easing.CubicOut),
            button.FadeTo(0.8, 100, Easing.CubicOut)
        );
        
        // Shimmer effect
        var shimmerTask = Task.WhenAll(
            shimmer.FadeTo(0.6, 50, Easing.CubicOut),
            shimmer.TranslateTo(400, 0, 400, Easing.CubicOut)
        );
        
        await Task.WhenAll(pressTask, shimmerTask);
        
        // Reset animations
        await Task.WhenAll(
            button.ScaleTo(1, 100, Easing.CubicOut),
            button.FadeTo(1, 100, Easing.CubicOut),
            shimmer.FadeTo(0, 50, Easing.CubicOut),
            shimmer.TranslateTo(-400, 0, 50, Easing.CubicOut)
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

    private async void SetUIBusy(bool isBusy, string operation = "")
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            SortFilesButton.IsEnabled = !isBusy;
            FindDuplicatesButton.IsEnabled = !isBusy;
            
            if (isBusy)
            {
                ProgressSection.IsVisible = true;
                ProgressLabel.Text = operation;
                
                // Spectacular progress bar animation
                await Task.WhenAll(
                    ProgressBar.ScaleTo(1, 400, Easing.SpringOut),
                    ProgressLabel.FadeTo(1, 400, Easing.CubicOut)
                );
            }
            else
            {
                await Task.Delay(800); // Show complete state
                await Task.WhenAll(
                    ProgressBar.ScaleTo(0, 400, Easing.CubicIn),
                    ProgressLabel.FadeTo(0, 400, Easing.CubicIn)
                );
                ProgressSection.IsVisible = false;
                ProgressBar.Progress = 0;
            }
        });
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
}
