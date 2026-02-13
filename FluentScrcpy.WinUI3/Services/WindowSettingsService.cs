using System;
using System.IO;
using System.Text.Json;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace FluentScrcpy.WinUI3.Services;

public class WindowSettingsService
{
    private static readonly Lazy<WindowSettingsService> _instance = new(() => new WindowSettingsService());
    public static WindowSettingsService Instance => _instance.Value;

    private readonly string _settingsFile;
    private WindowSettings _settings;

    private WindowSettingsService()
    {
        var configFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FluentScrcpy");
        
        _settingsFile = Path.Combine(configFolder, "window_settings.json");
        
        Directory.CreateDirectory(configFolder);
        
        _settings = LoadSettings();
    }

    public WindowSettings Settings => _settings;

    public bool HasSavedSettings => _settings.Width > 0 && _settings.Height > 0;

    public void SaveWindowSettings(AppWindow appWindow)
    {
        try
        {
            var size = appWindow.Size;
            var position = appWindow.Position;

            _settings = new WindowSettings
            {
                Width = size.Width,
                Height = size.Height,
                X = position.X,
                Y = position.Y,
                IsMaximized = appWindow.Presenter.Kind == AppWindowPresenterKind.Overlapped && 
                             ((OverlappedPresenter)appWindow.Presenter).State == OverlappedPresenterState.Maximized
            };

            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            File.WriteAllText(_settingsFile, json);
            
            LogService.WriteLog("WindowSettingsService", $"Saved window settings: {size.Width}x{size.Height} at ({position.X}, {position.Y})");
        }
        catch (Exception ex)
        {
            LogService.WriteLog("WindowSettingsService", $"Error saving settings: {ex}");
        }
    }

    private WindowSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFile))
            {
                var json = File.ReadAllText(_settingsFile);
                var settings = JsonSerializer.Deserialize<WindowSettings>(json);
                if (settings != null)
                {
                    LogService.WriteLog("WindowSettingsService", $"Loaded window settings: {settings.Width}x{settings.Height} at ({settings.X}, {settings.Y})");
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            LogService.WriteLog("WindowSettingsService", $"Error loading settings: {ex}");
        }

        return new WindowSettings();
    }

    public void ApplyWindowSettings(AppWindow appWindow)
    {
        try
        {
            if (!HasSavedSettings)
            {
                LogService.WriteLog("WindowSettingsService", "No saved settings, using default");
                return;
            }

            var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
            var screenWidth = displayArea.WorkArea.Width;
            var screenHeight = displayArea.WorkArea.Height;

            // Ensure window is not larger than screen
            var width = Math.Min(_settings.Width, screenWidth);
            var height = Math.Min(_settings.Height, screenHeight);

            // Ensure window is on screen
            var x = Math.Max(0, Math.Min(_settings.X, screenWidth - width));
            var y = Math.Max(0, Math.Min(_settings.Y, screenHeight - height));

            appWindow.Resize(new SizeInt32(width, height));
            appWindow.Move(new PointInt32(x, y));

            if (_settings.IsMaximized && appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }

            LogService.WriteLog("WindowSettingsService", $"Applied window settings: {width}x{height} at ({x}, {y})");
        }
        catch (Exception ex)
        {
            LogService.WriteLog("WindowSettingsService", $"Error applying settings: {ex}");
        }
    }
}

public class WindowSettings
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsMaximized { get; set; }
}
