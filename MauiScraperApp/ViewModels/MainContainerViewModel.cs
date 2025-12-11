using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiScraperApp.Views;

namespace MauiScraperApp.ViewModels;

public partial class MainContainerViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty] private object _currentView;
    [ObservableProperty] private string _currentTitle = "Search";
    
    // Track active tab for UI highlights (optional)
    [ObservableProperty] private bool _isSearchActive;
    [ObservableProperty] private bool _isDownloadsActive;
    [ObservableProperty] private bool _isConnectionActive;

    public MainContainerViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        NavigateToSearch(); // Default
    }

    [RelayCommand]
    public void NavigateToSearch()
    {
        CurrentView = _serviceProvider.GetRequiredService<SearchView>();
        CurrentTitle = "Search";
        UpdateState(true, false, false);
    }

    [RelayCommand]
    public void NavigateToDownloads()
    {
        CurrentView = _serviceProvider.GetRequiredService<DownloadsView>();
        CurrentTitle = "Downloads";
        UpdateState(false, true, false);
    }

    [RelayCommand]
    public void NavigateToConnection()
    {
        CurrentView = _serviceProvider.GetRequiredService<ConnectionView>();
        CurrentTitle = "Connection";
        UpdateState(false, false, true);
    }

    private void UpdateState(bool search, bool downloads, bool connection)
    {
        IsSearchActive = search;
        IsDownloadsActive = downloads;
        IsConnectionActive = connection;
    }
}
