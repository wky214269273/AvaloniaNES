using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;

namespace AvaloniaNES.Converter;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is byte statusValue && statusValue >= 1)
            return new SolidColorBrush(Colors.Green);
        // 返回主题相关的颜色资源
        return Application.Current!.FindResource("FlagColor")!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null!;
    }
}