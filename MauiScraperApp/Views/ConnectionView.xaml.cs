// Views/ConnectionView.xaml.cs
using MauiScraperApp.ViewModels;

namespace MauiScraperApp.Views;

public partial class ConnectionView : ContentPage
{
    public ConnectionView(ConnectionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}