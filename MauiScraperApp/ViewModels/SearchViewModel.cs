using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiScraperApp.Models;
using MauiScraperApp.Services;

namespace MauiScraperApp.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly WebScrapingService _scraper;
    private readonly RemoteClientService _remote;

    [ObservableProperty] private string _searchQuery;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isSendingToRemote;
    [ObservableProperty] private bool _isConnected;

    // Site Selection
    [ObservableProperty] private string _selectedSite = "1337x";
    public List<string> Sites { get; } = new() { "1337x", "ThePirateBay", "Nyaa" };

    // Categories
    [ObservableProperty] private Category _selectedCategory;
    public ObservableCollection<Category> Categories { get; } = new();

    // Sort Options
    [ObservableProperty] private SortOption _selectedSortOption;
    public ObservableCollection<SortOption> SortOptions { get; } = new()
    {
        new SortOption { Name = "Time (Newest)", Value = "time" },
        new SortOption { Name = "Size (Largest)", Value = "size" },
        new SortOption { Name = "Seeders (Most)", Value = "seeders" }
    };

    // Wizard Properties
    [ObservableProperty] private bool _isDownloadWizardVisible;
    [ObservableProperty] private bool _isManualPathEnabled;
    [ObservableProperty] private string _manualPathText;
    [ObservableProperty] private SearchResult _selectedItem;
    [ObservableProperty] private DriveInfoModel _selectedDrive;
    [ObservableProperty] private string _selectedPathCategory;

    public ObservableCollection<SearchResult> Results { get; } = new();
    public ObservableCollection<DriveInfoModel> Drives { get; } = new();
    public ObservableCollection<string> PathCategories { get; } = new() {
        "Animated Movies", "Animated shows", "Documentary", "Horror", "Mom Films", "Mom Tv Shows", "Movies", "Tv shows", "Misc"
    };

    public SearchViewModel(WebScrapingService s, RemoteClientService r)
    {
        _scraper = s; _remote = r;
        IsConnected = _remote.IsConnected;
        SelectedSortOption = SortOptions[0]; // Default to Time
        UpdateCategories(); 
    }

    partial void OnSelectedSiteChanged(string value) => UpdateCategories();

    private void UpdateCategories()
    {
        Categories.Clear();
        
        // DEFAULT VALUES FOR "ALL"
        string allValue = null;
        if (SelectedSite == "ThePirateBay") allValue = "0";
        if (SelectedSite == "Nyaa") allValue = "0_0";

        Categories.Add(new Category { Name = "All", Value = allValue });

        if (SelectedSite == "1337x")
        {
            Categories.Add(new Category { Name = "Movies", Value = "Movies" });
            Categories.Add(new Category { Name = "TV", Value = "TV" });
            Categories.Add(new Category { Name = "Games", Value = "Games" });
            Categories.Add(new Category { Name = "Music", Value = "Music" });
            Categories.Add(new Category { Name = "Anime", Value = "Anime" });
            Categories.Add(new Category { Name = "XXX", Value = "XXX" });
        }
        else if (SelectedSite == "Nyaa")
        {
            Categories.Add(new Category { Name = "Anime", Value = "1_2" });
            Categories.Add(new Category { Name = "Manga", Value = "3_1" });
            Categories.Add(new Category { Name = "Audio", Value = "2_0" });
        }
        else if (SelectedSite == "ThePirateBay")
        {
            Categories.Add(new Category { Name = "Audio", Value = "100" });
            Categories.Add(new Category { Name = "Video", Value = "200" });
            Categories.Add(new Category { Name = "Applications", Value = "300" });
            Categories.Add(new Category { Name = "Games", Value = "400" });
            Categories.Add(new Category { Name = "Porn", Value = "500" });
            Categories.Add(new Category { Name = "Other", Value = "600" });
        }
        
        SelectedCategory = Categories[0];
    }

    [RelayCommand]
    private void CheckConnection() => IsConnected = _remote.IsConnected;

    [RelayCommand]
    private async Task PerformSearchAsync()
    {
        if (IsLoading) return;
        IsConnected = _remote.IsConnected;

        IsLoading = true;
        Results.Clear();
        try {
            var data = await _scraper.PerformSearchAsync(
                SearchQuery, 
                SelectedSite, 
                SelectedCategory?.Value, 
                SelectedSortOption?.Value ?? "time", 
                1
            );
            
            foreach(var i in data) Results.Add(i);

            if (Results.Count == 0)
                await Shell.Current.DisplayAlert("Info", "No results found for this query.", "OK");
        } 
        catch (Exception ex) 
        { 
            await Shell.Current.DisplayAlert("Search Error", ex.Message, "OK"); 
        } 
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task OpenWizard(SearchResult item)
    {
        if (!_remote.IsConnected) { await Shell.Current.DisplayAlert("Error", "Connect to PC first", "OK"); return; }
        
        SelectedItem = item;
        IsDownloadWizardVisible = true;
        IsManualPathEnabled = false;
        ManualPathText = "";
        SelectedPathCategory = "Movies";

        try {
            var d = await _remote.GetDrives();
            Drives.Clear();
            foreach(var x in d) Drives.Add(x);
            if(Drives.Any()) SelectedDrive = Drives[0];
        } catch { }
    }

    [RelayCommand]
    private async Task ConfirmDownload()
    {
        if (SelectedItem == null) return;
        
        string finalPath;
        if (IsManualPathEnabled)
        {
            if (string.IsNullOrWhiteSpace(ManualPathText)) { await Shell.Current.DisplayAlert("Error", "Enter a path", "OK"); return; }
            finalPath = ManualPathText;
        }
        else
        {
            if (SelectedDrive == null) return;
            finalPath = PathResolver.GetPath(SelectedDrive.Name, SelectedPathCategory);
        }

        IsDownloadWizardVisible = false;
        IsSendingToRemote = true;

        try {
            string magnet = await _scraper.GetMagnetLinkAsync(SelectedItem);
            if(string.IsNullOrEmpty(magnet)) throw new Exception("Magnet not found");

            if(await _remote.AddTorrentAsync(magnet, finalPath))
                await Shell.Current.DisplayAlert("Success", $"Added to:\n{finalPath}", "OK");
            else
                throw new Exception("Failed to send to qBittorrent");
        } 
        catch (Exception ex) { await Shell.Current.DisplayAlert("Error", ex.Message, "OK"); }
        finally { IsSendingToRemote = false; SelectedItem = null; }
    }

    [RelayCommand] void CancelWizard() { IsDownloadWizardVisible = false; SelectedItem = null; }
}