using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Threading.Tasks;

namespace FluentScrcpy.WinUI3.Services;

public static class NotificationService
{
    private static MainWindow? _mainWindow;
    private static DispatcherTimer? _hideTimer;

    public static void Initialize(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        
        // Initialize hide timer
        _hideTimer = new DispatcherTimer();
        _hideTimer.Tick += (s, e) =>
        {
            Hide();
        };
    }

    public static void Show(string message, string? icon = null, int durationMs = 3000)
    {
        if (_mainWindow == null) return;

        _mainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                // Update content
                _mainWindow.NotificationMessageTextBlockElement.Text = message;

                if (!string.IsNullOrEmpty(icon))
                {
                    _mainWindow.NotificationIconElement.Glyph = icon;
                    _mainWindow.NotificationIconElement.Visibility = Visibility.Visible;
                }
                else
                {
                    _mainWindow.NotificationIconElement.Visibility = Visibility.Collapsed;
                }

                // Show notification
                _mainWindow.NotificationBorderElement.Visibility = Visibility.Visible;

                // Play fade in animation
                var fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = new Duration(TimeSpan.FromMilliseconds(200))
                };

                var storyboard = new Storyboard();
                Storyboard.SetTarget(fadeIn, _mainWindow.NotificationBorderElement);
                Storyboard.SetTargetProperty(fadeIn, "Opacity");
                storyboard.Children.Add(fadeIn);
                storyboard.Begin();

                // Start or restart hide timer
                _hideTimer?.Stop();
                if (durationMs > 0)
                {
                    _hideTimer?.Start();
                }

                LogService.WriteLog("NotificationService", $"Show notification: {message}");
            }
            catch (Exception ex)
            {
                LogService.WriteLog("NotificationService", $"Error showing notification: {ex}");
            }
        });
    }

    public static void ShowSuccess(string message, int durationMs = 3000)
    {
        Show(message, "\uE73E", durationMs); // Checkmark icon
    }

    public static void ShowError(string message, int durationMs = 5000)
    {
        Show(message, "\uE711", durationMs); // Error icon
    }

    public static void ShowInfo(string message, int durationMs = 3000)
    {
        Show(message, "\uE946", durationMs); // Info icon
    }

    public static void ShowLoading(string message)
    {
        Show(message, "\uE895", 0); // Loading icon, no auto hide
    }

    public static void Hide()
    {
        if (_mainWindow == null) return;

        _mainWindow.DispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                // Stop timer
                _hideTimer?.Stop();

                // Play fade out animation
                var fadeOut = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = new Duration(TimeSpan.FromMilliseconds(200))
                };

                var storyboard = new Storyboard();
                Storyboard.SetTarget(fadeOut, _mainWindow.NotificationBorderElement);
                Storyboard.SetTargetProperty(fadeOut, "Opacity");
                storyboard.Children.Add(fadeOut);

                var tcs = new TaskCompletionSource<object?>();
                storyboard.Completed += (s, e) => tcs.SetResult(null);
                storyboard.Begin();

                await tcs.Task;

                _mainWindow.NotificationBorderElement.Visibility = Visibility.Collapsed;
                
                LogService.WriteLog("NotificationService", "Notification hidden");
            }
            catch (Exception ex)
            {
                LogService.WriteLog("NotificationService", $"Error hiding notification: {ex}");
            }
        });
    }
}
