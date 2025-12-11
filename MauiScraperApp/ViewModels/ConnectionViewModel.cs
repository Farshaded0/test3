using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiScraperApp.Services;

namespace MauiScraperApp.ViewModels;

public partial class ConnectionViewModel : ObservableObject
{
    private readonly RemoteClientService _remoteClient;

    [ObservableProperty] private string _serverIp = "";
    [ObservableProperty] private string _serverPort = "5000";
    [ObservableProperty] private bool _isConnecting;
    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private string _statusMessage = "";

    public ObservableCollection<string> DiscoveredServers { get; } = new();

    public ConnectionViewModel(RemoteClientService remoteClient)
    {
        _remoteClient = remoteClient;
        var (savedIp, savedPort) = _remoteClient.GetSavedConnectionInfo();
        
        if (!string.IsNullOrEmpty(savedIp))
        {
            ServerIp = savedIp;
            ServerPort = savedPort.ToString();
            StatusMessage = "Last connected: " + savedIp;
        }
        else
        {
            StatusMessage = "Enter PC IP address";
        }
        IsConnected = _remoteClient.IsConnected;
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (string.IsNullOrWhiteSpace(ServerIp) || !int.TryParse(ServerPort, out int port))
        {
            await Shell.Current.DisplayAlert("Error", "Invalid IP or Port", "OK");
            return;
        }

        try
        {
            IsConnecting = true;
            StatusMessage = "Connecting...";

            bool success = await _remoteClient.ConnectAsync(ServerIp, port);

            // Ensure Main Thread update for reliability
            MainThread.BeginInvokeOnMainThread(async () => 
            {
                if (success)
                {
                    IsConnected = true;
                    StatusMessage = $"Connected to {ServerIp}:{port}";
                    await Shell.Current.DisplayAlert("Connected", "You can now use the Search and Downloads tabs.", "OK");
                }
                else
                {
                    IsConnected = false;
                    StatusMessage = "Connection failed";
                    await Shell.Current.DisplayAlert("Failed", "Could not connect to Bridge.\nCheck IP/Port and Firewall.", "OK");
                }
            });
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() => StatusMessage = $"Error: {ex.Message}");
        }
        finally
        {
            IsConnecting = false;
        }
    }

    [RelayCommand]
    private async Task ScanNetworkAsync()
    {
        try
        {
            IsScanning = true;
            StatusMessage = "Scanning network...";
            DiscoveredServers.Clear();

            List<string> servers = await _remoteClient.DiscoverServersAsync();

            MainThread.BeginInvokeOnMainThread(() => 
            {
                foreach (var server in servers)
                {
                    DiscoveredServers.Add(server);
                }

                if (servers.Count > 0)
                    StatusMessage = $"Found {servers.Count} server(s)";
                else
                    StatusMessage = "No servers found";
            });
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() => StatusMessage = $"Error: {ex.Message}");
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private void SelectServer(string ip)
    {
        ServerIp = ip;
        StatusMessage = $"Selected {ip}";
    }

    [RelayCommand]
    private void Disconnect()
    {
        _remoteClient.Disconnect();
        IsConnected = false;
        StatusMessage = "Disconnected";
    }
}
