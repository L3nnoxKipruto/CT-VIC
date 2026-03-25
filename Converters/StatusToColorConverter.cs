using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DataEntryApp.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            return status switch
            {
                "Complete" or "Success" => Brushes.Teal,
                "Draft" or "Info" => Brushes.Gray,
                "Warning" => Brushes.Orange,
                "Error" => Brushes.Red,
                _ => Brushes.Gray
            };
        }
        if (value is Messages.NotificationType type)
        {
            return type switch
            {
                Messages.NotificationType.Success => Brushes.Teal,
                Messages.NotificationType.Info => Brush.Parse("#3B82F6"),    // Blue 500
                Messages.NotificationType.Warning => Brush.Parse("#F59E0B"), // Amber 500
                Messages.NotificationType.Error => Brush.Parse("#EF4444"),   // Red 500
                _ => Brush.Parse("#64748B")                         // Slate 500
            };
        }
        return Brushes.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
