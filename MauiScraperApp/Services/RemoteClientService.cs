using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MauiScraperApp.Services;

public class RemoteClientService
{
    private readonly HttpClient _httpClient;
    private string _serverUrl;
    public bool IsConnected { get; private set; }

    public RemoteClientService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    }

    public async Task<bool> ConnectAsync(string ip, int port = 5000)
    {
        var url = $"http://{ip}:{port}";
        try {
            var response = await _httpClient.GetAsync($"{url}/api/torrent/ping");
            if (response.IsSuccessStatusCode) {
                _serverUrl = url; 
                IsConnected = true;
                Preferences.Set("last_ip", ip); 
                Preferences.Set("last_port", port);
                return true;
            }
        } catch { }
        IsConnected = false; 
        return false;
    }

    // Renamed back to match ConnectionViewModel
    public (string ip, int port) GetSavedConnectionInfo() 
    {
        return (Preferences.Get("last_ip", ""), Preferences.Get("last_port", 5000));
    }

    public void Disconnect() { _serverUrl = null; IsConnected = false; }

    // Restored Discovery Logic
    public async Task<List<string>> DiscoverServersAsync()
    {
        var discovered = new List<string>();
        var localIp = GetLocalIPAddress();
        if (string.IsNullOrEmpty(localIp)) return discovered;

        // Get network prefix (e.g., 192.168.1.)
        var prefix = localIp.Substring(0, localIp.LastIndexOf('.') + 1);
        var tasks = new List<Task>();

        // Scan range (Scanning 1-255 is slow, you might want to limit this or use UDP broadcast in future)
        // For now, we scan quickly with short timeout
        for (int i = 1; i < 255; i++)
        {
            var ip = prefix + i;
            tasks.Add(Task.Run(async () => 
            {
                try 
                {
                    using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(200) };
                    var resp = await client.GetAsync($"http://{ip}:5000/api/torrent/ping");
                    if (resp.IsSuccessStatusCode)
                    {
                        lock (discovered) discovered.Add(ip);
                    }
                } 
                catch { }
            }));
        }

        await Task.WhenAll(tasks);
        return discovered;
    }

    private string GetLocalIPAddress()
    {
        try {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList) {
                if (ip.AddressFamily == AddressFamily.InterNetwork) return ip.ToString();
            }
        } catch { }
        return "";
    }

    // --- API CALLS ---
    public async Task<List<RemoteTorrentInfo>> GetTorrentsAsync()
    {
        if (!IsConnected) return new();
        try {
            var s = await _httpClient.GetStringAsync($"{_serverUrl}/api/torrent/list");
            return JsonConvert.DeserializeObject<List<RemoteTorrentInfo>>(s) ?? new();
        } catch { return new(); }
    }

    public async Task<bool> AddTorrentAsync(string magnet, string path)
    {
        if (!IsConnected) return false;
        try {
            var json = JsonConvert.SerializeObject(new { magnetLink = magnet, savePath = path });
            var c = new StringContent(json, Encoding.UTF8, "application/json");
            return (await _httpClient.PostAsync($"{_serverUrl}/api/torrent/add", c)).IsSuccessStatusCode;
        } catch { return false; }
    }

    public async Task<bool> PauseTorrentAsync(string hash) => await Post($"/api/torrent/pause/{hash}");
    public async Task<bool> ResumeTorrentAsync(string hash) => await Post($"/api/torrent/resume/{hash}");
    public async Task<bool> DeleteTorrentAsync(string hash, bool files) => 
        (await _httpClient.DeleteAsync($"{_serverUrl}/api/torrent/delete/{hash}?deleteFiles={files}")).IsSuccessStatusCode;
    
    private async Task<bool> Post(string end) {
        if (!IsConnected) return false;
        try { return (await _httpClient.PostAsync(_serverUrl + end, null)).IsSuccessStatusCode; } catch { return false; }
    }

    public async Task<List<DriveInfoModel>> GetDrives()
    {
        if (!IsConnected) return new();
        try {
            var s = await _httpClient.GetStringAsync($"{_serverUrl}/api/system/drives");
            return JsonConvert.DeserializeObject<List<DriveInfoModel>>(s) ?? new();
        } catch { return new(); }
    }
}

// --- MODELS ---

public class DriveInfoModel
{
    public string Name { get; set; }
    public long TotalBytes { get; set; }
    public long FreeBytes { get; set; }
    public long UsedBytes { get; set; }
    public string DisplayName => $"Drive {Name}";
    public double UsageProgress => TotalBytes == 0 ? 0 : (double)UsedBytes / TotalBytes;
    public string UsageText => $"{FormatBytes(UsedBytes)} Used / {FormatBytes(FreeBytes)} Free";
    private string FormatBytes(long b) => RemoteTorrentInfo.FormatBytesStatic(b);
}

public partial class RemoteTorrentInfo : ObservableObject
{
    public string Hash { get; set; }
    
    [ObservableProperty] private string _name;
    [ObservableProperty] private long _size;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private long _downloadSpeed;
    [ObservableProperty] private long _uploadSpeed;
    [ObservableProperty] private long _eta;
    [ObservableProperty] private string _state;
    [ObservableProperty] private string _savePath;
    [ObservableProperty] private long _downloaded;

    public string ProgressPercent => $"{(Progress * 100):F1}%";
    public string DownloadSpeedFormatted => FormatBytesStatic(DownloadSpeed) + "/s";
    public string UploadSpeedFormatted => FormatBytesStatic(UploadSpeed) + "/s";
    public string SizeFormatted => FormatBytesStatic(Size);
    public string DownloadedFormatted => FormatBytesStatic(Downloaded);
    public string EtaFormatted => FormatEta(Eta);

    public void UpdateFrom(RemoteTorrentInfo fresh)
    {
        if (Name != fresh.Name) Name = fresh.Name;
        if (State != fresh.State) State = fresh.State;
        if (SavePath != fresh.SavePath) SavePath = fresh.SavePath;

        if (Size != fresh.Size) { Size = fresh.Size; OnPropertyChanged(nameof(SizeFormatted)); }
        if (Progress != fresh.Progress) { Progress = fresh.Progress; OnPropertyChanged(nameof(ProgressPercent)); }
        if (DownloadSpeed != fresh.DownloadSpeed) { DownloadSpeed = fresh.DownloadSpeed; OnPropertyChanged(nameof(DownloadSpeedFormatted)); }
        if (UploadSpeed != fresh.UploadSpeed) { UploadSpeed = fresh.UploadSpeed; OnPropertyChanged(nameof(UploadSpeedFormatted)); }
        if (Downloaded != fresh.Downloaded) { Downloaded = fresh.Downloaded; OnPropertyChanged(nameof(DownloadedFormatted)); }
        if (Eta != fresh.Eta) { Eta = fresh.Eta; OnPropertyChanged(nameof(EtaFormatted)); }
    }

    public static string FormatBytesStatic(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1) { order++; len /= 1024; }
        return $"{len:0.##} {sizes[order]}";
    }

    private string FormatEta(long s)
    {
        if (s < 0 || s >= 8640000) return "∞";
        var t = TimeSpan.FromSeconds(s);
        if (t.TotalHours >= 24) return $"{(int)t.TotalDays}d {t.Hours}h";
        return t.TotalHours >= 1 ? $"{t.Hours}h {t.Minutes}m" : $"{t.Minutes}m {t.Seconds}s";
    }
}