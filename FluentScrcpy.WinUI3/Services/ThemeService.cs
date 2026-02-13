using Microsoft.UI.Xaml;
using System;

namespace FluentScrcpy.WinUI3.Services;

public static class ThemeService
{
    private const string ThemeSettingsKey = "AppTheme";

    public static ElementTheme GetCurrentTheme()
    {
        var theme = SettingsService.GetSetting(ThemeSettingsKey);
        return theme switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
    }

    public static void SetTheme(ElementTheme theme)
    {
        var themeString = theme switch
        {
            ElementTheme.Light => "Light",
            ElementTheme.Dark => "Dark",
            _ => "Default"
        };

        SettingsService.SetSetting(ThemeSettingsKey, themeString);

        // Apply theme to main window
        ApplyThemeToWindow(theme);
    }

    public static void ApplyThemeToWindow(ElementTheme theme)
    {
        try
        {
            // Apply to main window
            if (App.MainWindow?.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme;
                LogService.WriteLog("ThemeService", $"Theme applied to MainWindow: {theme}");
            }
        }
        catch (Exception ex)
        {
            LogService.WriteLog("ThemeService", $"Error applying theme: {ex}");
        }
    }

    public static void InitializeTheme()
    {
        var theme = GetCurrentTheme();
        ApplyThemeToWindow(theme);
    }
}
