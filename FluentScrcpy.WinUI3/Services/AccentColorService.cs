using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace FluentScrcpy.WinUI3.Services;

public static class AccentColorService
{
    private const string AccentColorSettingsKey = "AppAccentColor";
    private const string UseSystemAccentKey = "UseSystemAccent";

    public static bool UseSystemAccent()
    {
        var useSystem = SettingsService.GetSetting(UseSystemAccentKey);
        return string.IsNullOrEmpty(useSystem) || useSystem == "true";
    }

    public static Color GetAccentColor()
    {
        if (UseSystemAccent())
        {
            return GetSystemAccentColor();
        }

        var colorString = SettingsService.GetSetting(AccentColorSettingsKey);
        if (string.IsNullOrEmpty(colorString))
        {
            return GetSystemAccentColor();
        }

        try
        {
            var parts = colorString.Split(',');
            if (parts.Length == 3 &&
                byte.TryParse(parts[0], out byte r) &&
                byte.TryParse(parts[1], out byte g) &&
                byte.TryParse(parts[2], out byte b))
            {
                return Color.FromArgb(255, r, g, b);
            }
        }
        catch (Exception ex)
        {
            LogService.WriteLog("AccentColorService", $"Error parsing accent color: {ex}");
        }

        return GetSystemAccentColor();
    }

    public static Color GetSystemAccentColor()
    {
        try
        {
            var uiSettings = new Windows.UI.ViewManagement.UISettings();
            return uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Accent);
        }
        catch (Exception ex)
        {
            LogService.WriteLog("AccentColorService", $"Error getting system accent color: {ex}");
            return Color.FromArgb(255, 0, 120, 212);
        }
    }

    public static void SetUseSystemAccent(bool useSystem)
    {
        SettingsService.SetSetting(UseSystemAccentKey, useSystem ? "true" : "false");
        ApplyAccentColor();
    }

    public static void SetAccentColor(Color color)
    {
        var colorString = $"{color.R},{color.G},{color.B}";
        SettingsService.SetSetting(AccentColorSettingsKey, colorString);
        SettingsService.SetSetting(UseSystemAccentKey, "false");
        ApplyAccentColor();
    }

    public static void ApplyAccentColor()
    {
        try
        {
            var color = GetAccentColor();

            ApplyAccentColorToResources(color);

            LogService.WriteLog("AccentColorService", $"Accent color applied: {color}");
        }
        catch (Exception ex)
        {
            LogService.WriteLog("AccentColorService", $"Error applying accent color: {ex}");
        }
    }

    private static void ApplyAccentColorToResources(Color accentColor)
    {
        var resources = Application.Current.Resources;

        var accentBrush = new SolidColorBrush(accentColor);
        var accentLight1 = CreateLightColor(accentColor, 0.2);
        var accentLight2 = CreateLightColor(accentColor, 0.4);
        var accentDark1 = CreateDarkColor(accentColor, 0.2);
        var accentDark2 = CreateDarkColor(accentColor, 0.4);

        var accentLight1Brush = new SolidColorBrush(accentLight1);
        var accentLight2Brush = new SolidColorBrush(accentLight2);
        var accentDark1Brush = new SolidColorBrush(accentDark1);
        var accentDark2Brush = new SolidColorBrush(accentDark2);

        var textOnAccentBrush = GetContrastColor(accentColor) == Colors.Black
            ? new SolidColorBrush(Colors.Black)
            : new SolidColorBrush(Colors.White);

        resources["SystemAccentColor"] = accentColor;
        resources["SystemAccentColorLight1"] = accentLight1;
        resources["SystemAccentColorLight2"] = accentLight2;
        resources["SystemAccentColorDark1"] = accentDark1;
        resources["SystemAccentColorDark2"] = accentDark2;

        resources["AccentFillColorDefaultBrush"] = accentBrush;
        resources["AccentFillColorSecondaryBrush"] = accentLight1Brush;
        resources["AccentFillColorTertiaryBrush"] = accentLight2Brush;
        resources["AccentFillColorDisabledBrush"] = new SolidColorBrush(CreateLightColor(accentColor, 0.6));

        resources["AccentStrokeColorDefaultBrush"] = accentBrush;
        resources["AccentStrokeColorSecondaryBrush"] = accentLight1Brush;

        resources["TextOnAccentFillColorPrimaryBrush"] = textOnAccentBrush;
        resources["TextOnAccentFillColorSecondaryBrush"] = textOnAccentBrush;
        resources["TextOnAccentFillColorDisabledBrush"] = new SolidColorBrush(Colors.Gray);

        resources["AccentButtonBackground"] = accentBrush;
        resources["AccentButtonBackgroundPointerOver"] = accentLight1Brush;
        resources["AccentButtonBackgroundPressed"] = accentDark1Brush;
        resources["AccentButtonBackgroundDisabled"] = new SolidColorBrush(CreateLightColor(accentColor, 0.5));

        resources["AccentButtonForeground"] = textOnAccentBrush;
        resources["AccentButtonForegroundPointerOver"] = textOnAccentBrush;
        resources["AccentButtonForegroundPressed"] = textOnAccentBrush;
        resources["AccentButtonForegroundDisabled"] = new SolidColorBrush(Colors.Gray);

        resources["AccentButtonBorderBrush"] = accentBrush;
        resources["AccentButtonBorderBrushPointerOver"] = accentLight1Brush;
        resources["AccentButtonBorderBrushPressed"] = accentDark1Brush;

        resources["ToggleButtonBackgroundChecked"] = accentBrush;
        resources["ToggleButtonBackgroundCheckedPointerOver"] = accentLight1Brush;
        resources["ToggleButtonBackgroundCheckedPressed"] = accentDark1Brush;

        resources["ToggleButtonForegroundChecked"] = textOnAccentBrush;
        resources["ToggleButtonForegroundCheckedPointerOver"] = textOnAccentBrush;
        resources["ToggleButtonForegroundCheckedPressed"] = textOnAccentBrush;

        resources["ToggleButtonBorderBrushChecked"] = accentBrush;
        resources["ToggleButtonBorderBrushCheckedPointerOver"] = accentLight1Brush;
        resources["ToggleButtonBorderBrushCheckedPressed"] = accentDark1Brush;

        resources["HyperlinkButtonForeground"] = accentBrush;
        resources["HyperlinkButtonForegroundPointerOver"] = accentLight1Brush;
        resources["HyperlinkButtonForegroundPressed"] = accentDark1Brush;

        resources["ContentDialogBorderThemeBrush"] = accentBrush;
        resources["ContentDialogTopOverlay"] = accentLight2Brush;

        NotifyResourcesChanged();
    }

    private static Color CreateLightColor(Color color, double factor)
    {
        return Color.FromArgb(
            color.A,
            (byte)Math.Min(255, color.R + (255 - color.R) * factor),
            (byte)Math.Min(255, color.G + (255 - color.G) * factor),
            (byte)Math.Min(255, color.B + (255 - color.B) * factor));
    }

    private static Color CreateDarkColor(Color color, double factor)
    {
        return Color.FromArgb(
            color.A,
            (byte)(color.R * (1 - factor)),
            (byte)(color.G * (1 - factor)),
            (byte)(color.B * (1 - factor)));
    }

    private static Color GetContrastColor(Color backgroundColor)
    {
        double luminance = (0.299 * backgroundColor.R + 0.587 * backgroundColor.G + 0.114 * backgroundColor.B) / 255;
        return luminance > 0.5 ? Colors.Black : Colors.White;
    }

    private static void NotifyResourcesChanged()
    {
        try
        {
            if (App.MainWindow?.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = ElementTheme.Light;
                rootElement.RequestedTheme = ElementTheme.Default;
            }
        }
        catch (Exception ex)
        {
            LogService.WriteLog("AccentColorService", $"Error notifying resources changed: {ex}");
        }
    }

    public static void InitializeAccentColor()
    {
        ApplyAccentColor();
    }

    public static string ColorToString(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
