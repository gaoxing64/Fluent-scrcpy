using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using FluentScrcpy.WinUI3.Models;
using FluentScrcpy.WinUI3.Services;

namespace FluentScrcpy.WinUI3.Views;

public sealed partial class HomePage : Page
{
    private readonly AdbService _adbService = AdbService.Instance;
    private readonly ScrcpyService _scrcpyService = ScrcpyService.Instance;
    private readonly DeviceConfigService _configService = DeviceConfigService.Instance;
    private readonly DeviceManager _deviceManager = DeviceManager.Instance;
    private System.Timers.Timer? _refreshTimer;

    // Commands for DeviceCard binding
    public ICommand StartMirroringCommand { get; }
    public ICommand OpenConfigCommand { get; }
    public ICommand SwitchToWirelessCommand { get; }

    public HomePage()
    {
        InitializeComponent();
        DevicesItemsControl.ItemsSource = _deviceManager.Devices;

        // Initialize commands
        StartMirroringCommand = new RelayCommand<Device>(StartMirroring);
        OpenConfigCommand = new RelayCommand<Device>(OpenDeviceConfig);
        SwitchToWirelessCommand = new RelayCommand<Device>(SwitchToWireless);
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = LoadDevicesAsync();

        // Setup refresh timer
        _refreshTimer = new System.Timers.Timer(3000);
        _refreshTimer.Elapsed += async (s, args) => await LoadDevicesAsync();
        _refreshTimer.AutoReset = true;
        _refreshTimer.Start();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _refreshTimer?.Stop();
        _refreshTimer?.Dispose();
        _refreshTimer = null;
    }

    private async Task LoadDevicesAsync()
    {
        try
        {
            await DispatcherQueue.TryEnqueueAsync(async () =>
            {
                StatusTextBlock.Text = "Refreshing devices...";
            });

            var devices = await _adbService.GetDevicesAsync();

            await DispatcherQueue.TryEnqueueAsync(() =>
            {
                // Load configs for devices
                foreach (var device in devices)
                {
                    device.Config = _configService.GetDeviceConfig(device.Serial, device.Model);
                }

                // Update device manager (preserves states)
                _deviceManager.UpdateDevices(devices);

                StatusTextBlock.Text = $"Found {devices.Count} device(s)";
            });
        }
        catch
        {
            await DispatcherQueue.TryEnqueueAsync(() =>
            {
                StatusTextBlock.Text = "Error loading devices";
            });
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadDevicesAsync();
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        var ipAddress = IpTextBox.Text.Trim();
        if (string.IsNullOrEmpty(ipAddress)) return;

        StatusTextBlock.Text = $"Connecting to {ipAddress}...";

        var parts = ipAddress.Split(':');
        var ip = parts[0];
        var port = 5555;

        if (parts.Length > 1 && int.TryParse(parts[1], out var parsedPort))
        {
            port = parsedPort;
        }

        var success = await _adbService.ConnectWirelessAsync(ip, port);
        StatusTextBlock.Text = success ? $"Connected to {ipAddress}" : $"Failed to connect to {ipAddress}";

        if (success)
        {
            IpTextBox.Text = "";
            await LoadDevicesAsync();
        }
    }

    // Command implementations
    private void StartMirroring(Device? device)
    {
        if (device == null) return;

        var success = _scrcpyService.StartMirroring(device, _configService.GlobalConfig);
        StatusTextBlock.Text = success ? $"Started mirroring {device.Model}" : $"Failed to start mirroring";
    }

    private void OpenDeviceConfig(Device? device)
    {
        if (device == null) return;

        // Navigate to config page with device parameter
        StatusTextBlock.Text = $"Opening config for {device.Model}...";

        // Navigate to ConfigPage with device
        Frame.Navigate(typeof(ConfigPage), device);
    }

    private async void ClearDevicesButton_Click(object sender, RoutedEventArgs e)
    {
        _deviceManager.ClearDevices();
        StatusTextBlock.Text = "已清除所有设备";

        // Show success dialog
        var dialog = new ContentDialog
        {
            Title = "清除成功",
            Content = "已清除所有已连接设备。",
            CloseButtonText = "确定",
            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
    }

    private async void SwitchToWireless(Device? device)
    {
        if (device == null) return;

        StatusTextBlock.Text = "Enabling TCP/IP mode...";

        var success = await _adbService.EnableTcpipAsync(device.Serial);
        if (success)
        {
            await Task.Delay(2000);
            success = await _adbService.ConnectWirelessAsync(device.IpAddress);
            StatusTextBlock.Text = success ? "Wireless connection established" : "Failed to connect wirelessly";
            if (success) await LoadDevicesAsync();
        }
        else
        {
            StatusTextBlock.Text = "Failed to enable TCP/IP mode";
        }
    }
}

public static class DispatcherQueueExtensions
{
    public static Task TryEnqueueAsync(this Microsoft.UI.Dispatching.DispatcherQueue dispatcher, Action action)
    {
        var tcs = new TaskCompletionSource();
        dispatcher.TryEnqueue(() =>
        {
            try
            {
                action();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }
}

// Simple RelayCommand implementation
public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke((T?)parameter) ?? true;
    }

    public void Execute(object? parameter)
    {
        _execute((T?)parameter);
    }

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
