using MauiScraperApp.ViewModels;

namespace MauiScraperApp.Views;

public partial class MainContainerView : ContentPage
{
	public MainContainerView(MainContainerViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
