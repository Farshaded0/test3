namespace MauiScraperApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        // FIX: Removed manual Route Registration. 
        // The Routes are already defined in AppShell.xaml.
        // Registering them here again causes conflicts on iOS.
    }
}