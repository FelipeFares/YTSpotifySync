using System;
using Microsoft.UI.Xaml.Data;

namespace YTSpotifySync.Helpers;

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolVal)
        {
            return !boolVal;
        }

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolVal)
        {
            return !boolVal;
        }

        return false;
    }
}
