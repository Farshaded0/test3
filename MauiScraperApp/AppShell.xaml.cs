// AppShell.xaml.cs
namespace MauiScraperApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        // Register routes for navigation
        Routing.RegisterRoute("Connection", typeof(Views.ConnectionView));
        Routing.RegisterRoute("MainTabs", typeof(AppShell));
    }
}