// In Views/SearchView.xaml.cs
using MauiScraperApp.ViewModels;

namespace MauiScraperApp.Views;

public partial class SearchView : ContentView
{
    public SearchView(SearchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
    private void OnSearchButtonPressed(object sender, EventArgs e)
    {
        // Force unfocus 
        TheSearchBar.Unfocus();
        // Dispatch to ensure it stays down after command execution
        Dispatcher.Dispatch(() => {
            TheSearchBar.Unfocus();
        });
    }
}