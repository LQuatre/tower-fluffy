using System;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;
using TowerFluffy.Application.Game.Dtos.Combat;

namespace TowerFluffy.UI.Desktop.Converters;

public class ReadyToColorConverter : IValueConverter
{
    public static readonly ReadyToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool ready = value is bool b && b;
        
        if (parameter is string p && p == "StatusText")
        {
            return ready ? "PRÊT" : "ATTENTE";
        }

        if (ready)
        {
            return Brush.Parse("#10B981"); // Emerald
        }
        return Brush.Parse("#374151"); // Muted Gray
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class TowerTypeToBrushConverter : IValueConverter
{
    public static readonly TowerTypeToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TowerTypeDto currentType && parameter is string targetTypeStr)
        {
            if (currentType.ToString() == targetTypeStr)
            {
                return Brush.Parse("#00A3FF"); // Blue selection
            }
        }
        return Brush.Parse("#1F242E"); // Dark Slate
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
