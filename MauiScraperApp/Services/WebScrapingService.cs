using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using MauiScraperApp.Models;

namespace MauiScraperApp.Services;

public class WebScrapingService
{
    private readonly HttpClient _httpClient;
    
    // Mirrors
    private const string Base1337x = "https://1337xx.to"; 
    private const string BaseTPB = "https://tpb.party";

    public WebScrapingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<SearchResult>> PerformSearchAsync(string query, string site, string category, string sortBy, int page = 1)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<SearchResult>();

        try 
        {
            return site switch
            {
                "1337x" => await Search1337x(query, category, sortBy, page),
                "ThePirateBay" => await SearchTPB(query, category, sortBy, page),
                "Nyaa" => await SearchNyaa(query, category, sortBy, page),
                _ => new List<SearchResult>()
            };
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Forbidden)
                throw new Exception($"[{site}] 403 Forbidden. Bot protection blocked us.");
            throw new Exception($"[{site}] Network Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            throw new Exception($"{ex.Message}");
        }
    }

    // --- ThePirateBay (Strict Column Logic) ---
    private async Task<List<SearchResult>> SearchTPB(string query, string category, string sortBy, int page)
    {
        // 3=Date, 5=Size, 7=Seeds, 99=Relevance
        int orderBy = sortBy switch { "size" => 5, "seeders" => 7, "time" => 3, _ => 99 };
        string cat = string.IsNullOrEmpty(category) ? "0" : category;
        string url = $"{BaseTPB}/search/{Uri.EscapeDataString(query)}/{page}/{orderBy}/{cat}";

        string html;
        try 
        {
            html = await GetHtmlAsync(url, BaseTPB);
        }
        catch (Exception ex)
        {
             throw new Exception($"TPB Connection Failed: {ex.Message}");
        }

        var doc = new HtmlDocument(); doc.LoadHtml(html);
        
        // Strategy: Find magnet links to identify rows
        var magnetNodes = doc.DocumentNode.SelectNodes("//a[starts-with(@href, 'magnet:')]");

        if (magnetNodes == null || magnetNodes.Count == 0) 
        {
            var pageTitle = doc.DocumentNode.SelectSingleNode("//title")?.InnerText.Trim() ?? "No Title";
            if (pageTitle.Contains("Just a moment") || pageTitle.Contains("Cloudflare"))
                throw new Exception("TPB Error: Blocked by Cloudflare.");
            
            if (html.Contains("searchResult")) return new List<SearchResult>(); // 0 results
            throw new Exception($"TPB Parse Error. Title: {pageTitle}");
        }

        var results = new List<SearchResult>();
        
        foreach (var magnet in magnetNodes)
        {
            try 
            {
                // 1. Get the Row (TR)
                var row = magnet.Ancestors("tr").FirstOrDefault();
                if (row == null) continue;

                // 2. Get All Cells (TDs)
                var cells = row.SelectNodes(".//td");
                
                // We need at least 7 columns based on your feedback
                // Layout: [Type] [Name] [Uploaded] [Icons] [Size] [SE] [LE]
                if (cells == null || cells.Count < 7) continue;

                // --- EXTRACT TITLE (Column 2 / Index 1) ---
                // User info: td:nth-child(2) > a
                var titleNode = cells[1].SelectSingleNode(".//a");
                string title = (titleNode != null) 
                    ? WebUtility.HtmlDecode(titleNode.InnerText).Trim() 
                    : "Unknown Name";

                // --- EXTRACT SIZE (Column 5 / Index 4) ---
                // User info: td:nth-child(5) -> "550.99 MiB"
                string size = WebUtility.HtmlDecode(cells[4].InnerText)
                    .Replace("&nbsp;", " ")
                    .Trim();

                // --- EXTRACT SEEDS (Column 6 / Index 5) ---
                string seeds = cells[5].InnerText.Trim();

                // --- EXTRACT LEECHES (Column 7 / Index 6) ---
                string leeches = cells[6].InnerText.Trim();

                results.Add(new SearchResult
                {
                    Title = title,
                    DetailUrl = magnet.GetAttributeValue("href", ""),
                    Seeds = seeds,
                    Leeches = leeches,
                    Size = size,
                    Site = "ThePirateBay"
                });
            }
            catch 
            {
                continue;
            }
        }
        return results;
    }

    // --- 1337x (Mirror) ---
    private async Task<List<SearchResult>> Search1337x(string query, string category, string sortBy, int page)
    {
        string sortTerm = sortBy switch { "size" => "size", "seeders" => "seeders", _ => "time" };
        string url;
        
        if (!string.IsNullOrEmpty(category) && category != "All")
            url = $"{Base1337x}/sort-category-search/{Uri.EscapeDataString(query)}/{category}/{sortTerm}/desc/{page}/";
        else
            url = $"{Base1337x}/sort-search/{Uri.EscapeDataString(query)}/{sortTerm}/desc/{page}/";

        var html = await GetHtmlAsync(url, Base1337x + "/");
        var doc = new HtmlDocument(); doc.LoadHtml(html);
        
        if (doc.DocumentNode.InnerText.Contains("Access denied") || doc.DocumentNode.InnerText.Contains("Cloudflare"))
            throw new Exception("1337x Error: Cloudflare Blocked.");

        var nodes = doc.DocumentNode.SelectNodes("//table[contains(@class, 'table-list')]/tbody/tr");

        if (nodes == null) throw new Exception("1337x Error: No table found.");

        var results = new List<SearchResult>();
        foreach (var node in nodes)
        {
            var titleNode = node.SelectSingleNode(".//td[contains(@class, 'name')]/a[2]");
            if (titleNode == null) continue;

            results.Add(new SearchResult
            {
                Title = WebUtility.HtmlDecode(titleNode.InnerText),
                DetailUrl = Base1337x + titleNode.GetAttributeValue("href", ""),
                Seeds = node.SelectSingleNode(".//td[contains(@class, 'seeds')]")?.InnerText ?? "0",
                Leeches = node.SelectSingleNode(".//td[contains(@class, 'leeches')]")?.InnerText ?? "0",
                Size = node.SelectSingleNode(".//td[contains(@class, 'size')]")?.InnerText.Split('B')[0] + "B",
                Site = "1337x"
            });
        }
        return results;
    }

    // --- NYAA ---
    private async Task<List<SearchResult>> SearchNyaa(string query, string category, string sortBy, int page)
    {
        string sortTerm = sortBy switch { "size" => "size", "seeders" => "seeders", _ => "id" };
        string catCode = category ?? "0_0";
        string url = $"https://nyaa.si/?f=0&c={catCode}&q={Uri.EscapeDataString(query)}&s={sortTerm}&o=desc&p={page}";

        var html = await GetHtmlAsync(url, "https://nyaa.si/");
        var doc = new HtmlDocument(); doc.LoadHtml(html);
        
        var rows = doc.DocumentNode.SelectNodes("//table/tbody/tr");
        if (rows == null) throw new Exception("Nyaa Error: No results found.");

        var results = new List<SearchResult>();
        foreach (var row in rows)
        {
            var titleNode = row.SelectSingleNode(".//td[2]/a[not(contains(@class, 'comments'))]");
            if (titleNode == null) continue;

            var magnetNode = row.SelectSingleNode(".//td[3]/a[starts-with(@href, 'magnet:')]");
            var sizeNode = row.SelectSingleNode(".//td[4]");
            var seedNode = row.SelectSingleNode(".//td[6]");
            var leechNode = row.SelectSingleNode(".//td[7]");

            results.Add(new SearchResult
            {
                Title = WebUtility.HtmlDecode(titleNode.InnerText),
                DetailUrl = magnetNode?.GetAttributeValue("href", "") ?? "",
                Seeds = seedNode?.InnerText ?? "0",
                Leeches = leechNode?.InnerText ?? "0",
                Size = sizeNode?.InnerText ?? "?",
                Site = "Nyaa"
            });
        }
        return results;
    }

    public async Task<string> GetMagnetLinkAsync(SearchResult result)
    {
        if (result.Site == "ThePirateBay" || result.Site == "Nyaa") return result.DetailUrl;
        return await Get1337xMagnet(result.DetailUrl);
    }

    private async Task<string> Get1337xMagnet(string url)
    {
        var html = await GetHtmlAsync(url, Base1337x + "/");
        if (html == null) return null;
        var doc = new HtmlDocument(); doc.LoadHtml(html);
        var node = doc.DocumentNode.SelectSingleNode("//a[starts-with(@href, 'magnet:')]") 
                ?? doc.DocumentNode.SelectSingleNode("//li/a[contains(@href, 'magnet:')]");
        return node?.GetAttributeValue("href", "");
    }

    private async Task<string> GetHtmlAsync(string url, string referrer)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Referrer = new Uri(referrer);
        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException(null, null, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }
}