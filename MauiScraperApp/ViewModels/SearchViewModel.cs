using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiScraperApp.Models;
using MauiScraperApp.Services;

namespace MauiScraperApp.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly WebScrapingService _scraper;

    [ObservableProperty] private string _searchQuery;
    [ObservableProperty] private bool _isLoading;

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

    public ObservableCollection<SearchResult> Results { get; } = new();

    public SearchViewModel(WebScrapingService s)
    {
        _scraper = s;
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
    private async Task PerformSearchAsync()
    {
        if (IsLoading) return;

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
    private async Task CopyMagnetLink(SearchResult item)
    {
        if (item == null) return;
        
        try
        {
            IsLoading = true;
            string magnet = await _scraper.GetMagnetLinkAsync(item);
            
            if (!string.IsNullOrEmpty(magnet))
            {
                await Clipboard.SetTextAsync(magnet);
                await Shell.Current.DisplayAlert("Success", "Magnet link copied to clipboard!", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Could not retrieve magnet link.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }


}