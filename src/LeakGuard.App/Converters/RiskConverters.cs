using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using LeakGuard.Core.Models;

namespace LeakGuard.App.Converters;

/// <summary>
/// Конвертер RiskLevel → цвет (SolidColorBrush).
/// </summary>
public class RiskLevelToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is RiskLevel risk)
        {
            return risk switch
            {
                RiskLevel.Info => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7aa2f7")),
                RiskLevel.Low => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9ece6a")),
                RiskLevel.Medium => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e0af68")),
                RiskLevel.High => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f7768e")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#414868"))
            };
        }
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#414868"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Конвертер RiskLevel → строка цвета (для XAML-биндинга).
/// </summary>
public class RiskLevelToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is RiskLevel risk)
        {
            return risk switch
            {
                RiskLevel.Info => "#7aa2f7",
                RiskLevel.Low => "#9ece6a",
                RiskLevel.Medium => "#e0af68",
                RiskLevel.High => "#f7768e",
                _ => "#414868"
            };
        }
        return "#414868";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
