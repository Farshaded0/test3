namespace MauiScraperApp.Services;

public static class PathResolver
{
    public static string GetPath(string driveLetter, string category)
    {
        // Normalize input (remove ":\" if present, though logic expects just "C", "D", "E")
        driveLetter = driveLetter.Replace(":", "").Replace("\\", "").ToUpper().Trim();
        
        return (category, driveLetter) switch
        {
            // Animated Movies
            ("Animated Movies", "E") => @"E:\HDD 2 Jellyfin\animated movies",
            ("Animated Movies", "C") => @"C:\SSD Jellyfin\animated movies",
            ("Animated Movies", "D") => @"D:\HDD 1 Jellyfin\animated movies",

            // Animated shows
            ("Animated shows", "C") => @"C:\SSD Jellyfin\animated ssd",
            ("Animated shows", "E") => @"E:\HDD 2 Jellyfin\animated shows",
            ("Animated shows", "D") => @"D:\HDD 1 Jellyfin\animated shows 2",

            // Documentary
            ("Documentary", "D") => @"D:\HDD 1 Jellyfin\Documentary",
            ("Documentary", "E") => @"E:\HDD 2 Jellyfin\Documentary",
            ("Documentary", "C") => @"C:\SSD Jellyfin\Documentary",

            // Horror
            ("Horror", "E") => @"E:\HDD 2 Jellyfin\horror",
            ("Horror", "C") => @"C:\SSD Jellyfin\horror ssd",
            ("Horror", "D") => @"D:\HDD 1 Jellyfin\horror 2",

            // Mom Films
            ("Mom Films", "D") => @"D:\HDD 1 Jellyfin\mom films",
            ("Mom Films", "E") => @"E:\HDD 2 Jellyfin\mom films 2",
            ("Mom Films", "C") => @"C:\SSD Jellyfin\mom films ssd",

            // Mom Tv Shows
            ("Mom Tv Shows", "C") => @"C:\SSD Jellyfin\mom tv shows ssd",
            ("Mom Tv Shows", "D") => @"D:\HDD 1 Jellyfin\mom tv shows",
            ("Mom Tv Shows", "E") => @"E:\HDD 2 Jellyfin\mom tv shows 2",

            // Movies
            ("Movies", "C") => @"C:\SSD Jellyfin\movies ssd",
            ("Movies", "D") => @"D:\HDD 1 Jellyfin\movies",
            ("Movies", "E") => @"E:\HDD 2 Jellyfin\movies 2",

            // Tv shows
            ("Tv shows", "D") => @"D:\HDD 1 Jellyfin\tv shows",
            ("Tv shows", "E") => @"E:\HDD 2 Jellyfin\tv shows 2",
            ("Tv shows", "C") => @"C:\SSD Jellyfin\tv shows ssd",

            // Misc
            ("Misc", "D") => @"D:\HDD 1 Jellyfin\New folder (2)",
            ("Misc", "C") => @"C:\SSD Jellyfin\Misc",
            ("Misc", "E") => @"E:\HDD 2 Jellyfin\Misc",

            // Fallback default
            _ => $@"C:\Downloads\{category}" 
        };
    }
}