using System.Globalization;
using System.Windows.Data;

namespace ChatApp.Converters;

/// <summary>Converts a username string to its first letter initial (uppercase).</summary>
public class InitialConverter : IValueConverter
{
    public static readonly InitialConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && s.Length > 0)
            return s[0].ToString().ToUpper();
        return "?";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
