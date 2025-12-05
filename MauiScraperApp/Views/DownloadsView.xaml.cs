// Views/DownloadsView.xaml.cs
using MauiScraperApp.ViewModels;

namespace MauiScraperApp.Views;

public partial class DownloadsView : ContentPage
{
    private readonly DownloadsViewModel _viewModel;

    public DownloadsView(DownloadsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
        _viewModel.StartRefreshing();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.StopRefreshing();
    }
}