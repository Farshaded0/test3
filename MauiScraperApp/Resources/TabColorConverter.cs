using System.Globalization;

namespace MauiScraperApp.Resources;

public class TabColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isActive && isActive)
        {
            return Application.Current.RequestedTheme == AppTheme.Dark ? Colors.White : Colors.Black; // Active
        }
        return Colors.Gray; // Inactive
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
