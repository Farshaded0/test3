using System.Globalization;

namespace MauiScraperApp.Converters;

public class StateToButtonTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string state)
        {
            if (state.Contains("paused", StringComparison.OrdinalIgnoreCase))
                return "▶ Resume";
            else
                return "⏸ Pause";
        }
        return "⏸ Pause";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}