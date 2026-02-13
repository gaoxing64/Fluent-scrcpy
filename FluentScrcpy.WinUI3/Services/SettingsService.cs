using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FluentScrcpy.WinUI3.Services;

public static class SettingsService
{
    private static readonly string SettingsFilePath;
    private static readonly object LockObj = new();
    private static Settings? _cachedSettings;

    static SettingsService()
    {
        var settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FluentScrcpy");
        Directory.CreateDirectory(settingsDir);
        SettingsFilePath = Path.Combine(settingsDir, "settings.json");
    }

    public static string? GetSetting(string key)
    {
        var settings = LoadSettings();
        return settings.Values.TryGetValue(key, out var value) ? value : null;
    }

    public static void SetSetting(string key, string value)
    {
        var settings = LoadSettings();
        settings.Values[key] = value;
        SaveSettings(settings);
    }

    private static Settings LoadSettings()
    {
        if (_cachedSettings != null)
            return _cachedSettings;

        lock (LockObj)
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<Settings>(json);
                    if (settings != null)
                    {
                        _cachedSettings = settings;
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.WriteLog("SettingsService", $"Error loading settings: {ex.Message}");
            }

            _cachedSettings = new Settings();
            return _cachedSettings;
        }
    }

    private static void SaveSettings(Settings settings)
    {
        lock (LockObj)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(SettingsFilePath, json);
                _cachedSettings = settings;
            }
            catch (Exception ex)
            {
                LogService.WriteLog("SettingsService", $"Error saving settings: {ex.Message}");
            }
        }
    }
}

public class Settings
{
    public Dictionary<string, string> Values { get; set; } = new();
}
