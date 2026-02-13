using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;

namespace FluentScrcpy.WinUI3.Services;

public static class BackdropService
{
    private const string BackdropSettingsKey = "AppBackdrop";

    public enum BackdropType
    {
        Mica,
        MicaAlt,
        Acrylic
    }

    public static BackdropType GetCurrentBackdrop()
    {
        var backdrop = SettingsService.GetSetting(BackdropSettingsKey);
        return backdrop switch
        {
            "Mica" => BackdropType.Mica,
            "MicaAlt" => BackdropType.MicaAlt,
            "Acrylic" => BackdropType.Acrylic,
            _ => BackdropType.Mica
        };
    }

    public static void SetBackdrop(BackdropType backdropType)
    {
        var backdropString = backdropType switch
        {
            BackdropType.Mica => "Mica",
            BackdropType.MicaAlt => "MicaAlt",
            BackdropType.Acrylic => "Acrylic",
            _ => "Mica"
        };

        SettingsService.SetSetting(BackdropSettingsKey, backdropString);
        ApplyBackdrop(backdropType);
    }

    public static void ApplyBackdrop(BackdropType backdropType)
    {
        try
        {
            var window = App.MainWindow;
            if (window == null) return;

            // 保存当前主题设置
            ElementTheme currentTheme = ElementTheme.Default;
            if (window.Content is FrameworkElement rootElement)
            {
                currentTheme = rootElement.RequestedTheme;
            }

            SystemBackdrop? backdrop = backdropType switch
            {
                BackdropType.Mica => new MicaBackdrop(),
                BackdropType.MicaAlt => new MicaBackdrop { Kind = MicaKind.BaseAlt },
                BackdropType.Acrylic => new DesktopAcrylicBackdrop(),
                _ => new MicaBackdrop()
            };

            window.SystemBackdrop = backdrop;

            // 恢复主题设置
            if (window.Content is FrameworkElement rootElement2)
            {
                rootElement2.RequestedTheme = currentTheme;
            }

            LogService.WriteLog("BackdropService", $"Backdrop applied: {backdropType}");
        }
        catch (Exception ex)
        {
            LogService.WriteLog("BackdropService", $"Error applying backdrop: {ex}");
        }
    }

    public static void InitializeBackdrop()
    {
        var backdrop = GetCurrentBackdrop();
        ApplyBackdrop(backdrop);
    }

    public static string GetBackdropDisplayName(BackdropType backdropType)
    {
        return backdropType switch
        {
            BackdropType.Mica => "云母 (Mica)",
            BackdropType.MicaAlt => "云母变体 (Mica Alt)",
            BackdropType.Acrylic => "亚克力 (Acrylic)",
            _ => "云母 (Mica)"
        };
    }
}
