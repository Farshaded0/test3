using MauiScraperApp.ViewModels;

namespace MauiScraperApp.Views;

public partial class DownloadsView : ContentView
{
    private DownloadsViewModel _viewModel;

	public DownloadsView(DownloadsViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
        
        // Use Loaded/Unloaded for custom refresh logic since it's a ContentView
        this.Loaded += (s, e) => _viewModel.StartRefreshing();
        this.Unloaded += (s, e) => _viewModel.StopRefreshing();
	}
}
