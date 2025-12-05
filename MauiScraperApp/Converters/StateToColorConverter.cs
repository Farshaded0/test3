using System.Globalization;

namespace MauiScraperApp.Converters;

public class StateToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string state)
        {
            return state.ToLower() switch
            {
                "downloading" => Colors.Green,
                "uploading" => Colors.Blue,
                "pausedDL" => Colors.Orange,
                "pausedUP" => Colors.Orange,
                "stalledDL" => Colors.Gray,
                "stalledUP" => Colors.Gray,
                "queuedDL" => Colors.LightBlue,
                "queuedUP" => Colors.LightBlue,
                "checkingDL" => Colors.Purple,
                "checkingUP" => Colors.Purple,
                "error" => Colors.Red,
                _ => Colors.Gray
            };
        }
        return Colors.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}