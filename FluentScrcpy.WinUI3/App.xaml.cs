using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using FluentScrcpy.WinUI3.Services;

namespace FluentScrcpy.WinUI3;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
        
        // Setup global exception handling
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        
        LogService.WriteLog("App", "Application starting...");
    }

    private void OnUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogService.WriteLog("App", $"FATAL EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            LogService.WriteLog("App", $"Stack Trace: {ex.StackTrace}");
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        LogService.WriteLog("App", "OnLaunched called");
        
        try
        {
            _window = new MainWindow();
            LogService.WriteLog("App", "MainWindow created");
            
            _window.Activate();
            LogService.WriteLog("App", "MainWindow activated");
            
            // Store reference for theme changes
            MainWindow = _window;
            LogService.WriteLog("App", "MainWindow reference stored");
        }
        catch (Exception ex)
        {
            LogService.WriteLog("App", $"Error in OnLaunched: {ex}");
            throw;
        }
    }

    public static Window MainWindow { get; set; } = null!;
}
