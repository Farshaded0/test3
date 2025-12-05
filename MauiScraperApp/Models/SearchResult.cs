namespace MauiScraperApp.Models;

public class SearchResult
{
    public string Title { get; set; }
    public string Seeds { get; set; }
    public string Leeches { get; set; }
    public string Size { get; set; }
    public string DetailUrl { get; set; }
    public string Site { get; set; } // Added this property
}

public class Category
{
    public string Name { get; set; }
    public string Value { get; set; }
    public override string ToString() => Name;
}

public class SortOption
{
    public string Name { get; set; }
    public string Value { get; set; }
    public override string ToString() => Name;
}