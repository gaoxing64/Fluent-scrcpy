using Microsoft.UI.Xaml;
using System;
using System.Globalization;

namespace FluentScrcpy.WinUI3.Services;

public static class LanguageService
{
    private const string LanguageSettingsKey = "AppLanguage";

    // 语言变化事件
    public static event EventHandler LanguageChanged;

    public static string GetCurrentLanguage()
    {
        var language = SettingsService.GetSetting(LanguageSettingsKey);
        return string.IsNullOrEmpty(language) ? "System" : language;
    }

    public static void SetLanguage(string language)
    {
        SettingsService.SetSetting(LanguageSettingsKey, language);
        ApplyLanguage(language);
        // 触发语言变化事件
        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void ApplyLanguage(string language)
    {
        try
        {
            if (language == "System")
            {
                // Use system default
                var systemLanguage = CultureInfo.CurrentUICulture.Name;
                ApplyCulture(systemLanguage);
            }
            else
            {
                ApplyCulture(language);
            }

            LogService.WriteLog("LanguageService", $"Language applied: {language}");
        }
        catch (Exception ex)
        {
            LogService.WriteLog("LanguageService", $"Error applying language: {ex}");
        }
    }

    private static void ApplyCulture(string cultureName)
    {
        try
        {
            var culture = new CultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }
        catch (Exception ex)
        {
            LogService.WriteLog("LanguageService", $"Error setting culture {cultureName}: {ex}");
        }
    }

    public static void InitializeLanguage()
    {
        var language = GetCurrentLanguage();
        ApplyLanguage(language);
    }

    public static string GetLanguageDisplayName(string languageCode)
    {
        return languageCode switch
        {
            "System" => "跟随系统",
            "zh-CN" => "简体中文",
            "en-US" => "English",
            _ => languageCode
        };
    }
}
