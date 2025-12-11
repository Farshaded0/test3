using MauiScraperApp.Services;
using MauiScraperApp.ViewModels;
using MauiScraperApp.Views;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace MauiScraperApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddHttpClient("Scraper", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Connection", "keep-alive");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://www.google.com/");
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                UseCookies = true,
                AllowAutoRedirect = true,
                ConnectCallback = async (context, cancellationToken) =>
                {
                    string host = context.DnsEndPoint.Host.ToLower();

                    // CRITICAL FIX: Added "tpb" to catch tpb.party
                    if (host.Contains("1337") || host.Contains("pirate") || host.Contains("tpb") || host.Contains("nyaa"))
                    {
                        var ipAddress = await ResolveViaCloudflareDoH(host, cancellationToken);
                        if (ipAddress != null)
                        {
                            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                            await socket.ConnectAsync(ipAddress, context.DnsEndPoint.Port, cancellationToken);
                            return new NetworkStream(socket, ownsSocket: true);
                        }
                    }

                    var defaultSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    await defaultSocket.ConnectAsync(context.DnsEndPoint, cancellationToken);
                    return new NetworkStream(defaultSocket, ownsSocket: true);
                }
            };
            return handler;
        });

        builder.Services.AddSingleton<WebScrapingService>(sp => 
            new WebScrapingService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("Scraper")));
        builder.Services.AddSingleton<RemoteClientService>();

        // ViewModels - Singleton to keep state when switching "Tabs"
        builder.Services.AddSingleton<SearchViewModel>();
        builder.Services.AddSingleton<DownloadsViewModel>();
        builder.Services.AddSingleton<ConnectionViewModel>();
        builder.Services.AddSingleton<MainContainerViewModel>();

        // Views - Singleton usually okay for ContentView custom nav
        builder.Services.AddSingleton<SearchView>();
        builder.Services.AddSingleton<DownloadsView>();
        builder.Services.AddSingleton<ConnectionView>();
        builder.Services.AddSingleton<MainContainerView>();

        return builder.Build();
    }

    private static async Task<IPAddress?> ResolveViaCloudflareDoH(string hostname, CancellationToken token)
    {
        try
        {
            using var dnsClient = new HttpClient();
            var url = $"https://1.1.1.1/dns-query?name={hostname}&type=A";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Accept", "application/dns-json");

            var response = await dnsClient.SendAsync(request, token);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(token);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("Answer", out var answers))
            {
                foreach (var answer in answers.EnumerateArray())
                {
                    if (answer.TryGetProperty("type", out var type) && type.GetInt32() == 1 && answer.TryGetProperty("data", out var data))
                    {
                        if (IPAddress.TryParse(data.GetString(), out var ip)) return ip;
                    }
                }
            }
        }
        catch { }
        return null;
    }
}