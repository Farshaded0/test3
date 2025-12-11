using MauiScraperApp.ViewModels;

namespace MauiScraperApp.Views;

public partial class DownloadsView : ContentPage
{
    private DownloadsViewModel _viewModel;

	public DownloadsView(DownloadsViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.StartRefreshing();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.StopRefreshing();
    }
}
