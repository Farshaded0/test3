// In Views/SearchView.xaml.cs
using MauiScraperApp.ViewModels;

namespace MauiScraperApp.Views;

public partial class SearchView : ContentPage
{
    public SearchView(SearchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}