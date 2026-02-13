using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Timers;
using FluentScrcpy.WinUI3.Models;
using FluentScrcpy.WinUI3.Services;

namespace FluentScrcpy.WinUI3.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly AdbService _adbService = AdbService.Instance;
    private readonly ScrcpyService _scrcpyService = ScrcpyService.Instance;
    private readonly DeviceConfigService _configService = DeviceConfigService.Instance;
    private DispatcherQueue? _dispatcherQueue;
    private System.Timers.Timer? _refreshTimer;

    [ObservableProperty]
    private ObservableCollection<Device> _devices = new();

    [ObservableProperty]
    private string _connectIpAddress = "";

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private string _statusMessage = "";

    public HomeViewModel()
    {
        // Get dispatcher queue on UI thread
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        
        // Initial refresh
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await RefreshDevicesAsync();
        
        // Setup timer after initialization
        _refreshTimer = new System.Timers.Timer(3000);
        _refreshTimer.Elapsed += async (s, e) => await RefreshDevicesAsync();
        _refreshTimer.AutoReset = true;
        _refreshTimer.Start();
    }

    [RelayCommand]
    private async Task RefreshDevicesAsync()
    {
        if (IsRefreshing) return;
        
        IsRefreshing = true;
        StatusMessage = "Refreshing devices...";

        try
        {
            var devices = await _adbService.GetDevicesAsync();
            
            // Use dispatcher to update UI
            if (_dispatcherQueue != null)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    Devices.Clear();
                    foreach (var device in devices)
                    {
                        // Load device config
                        device.Config = _configService.GetDeviceConfig(device.Serial, device.Model);
                        Devices.Add(device);
                    }
                    StatusMessage = $"Found {devices.Count} device(s)";
                });
            }
            else
            {
                Devices.Clear();
                foreach (var device in devices)
                {
                    device.Config = _configService.GetDeviceConfig(device.Serial, device.Model);
                    Devices.Add(device);
                }
                StatusMessage = $"Found {devices.Count} device(s)";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private void StartMirroring(Device device)
    {
        if (device == null) return;

        var success = _scrcpyService.StartMirroring(device, _configService.GlobalConfig);
        StatusMessage = success ? $"Started mirroring {device.Model}" : $"Failed to start mirroring {device.Model}";
    }

    [RelayCommand]
    private async Task SwitchToWirelessAsync(Device device)
    {
        if (device == null) return;

        StatusMessage = "Enabling TCP/IP mode...";
        
        var success = await _adbService.EnableTcpipAsync(device.Serial);
        if (success)
        {
            await Task.Delay(2000); // Wait for device to restart in TCP/IP mode
            
            success = await _adbService.ConnectWirelessAsync(device.IpAddress);
            if (success)
            {
                StatusMessage = $"Wireless connection established for {device.Model}";
                await RefreshDevicesAsync();
            }
            else
            {
                StatusMessage = $"Failed to connect wirelessly to {device.Model}";
            }
        }
        else
        {
            StatusMessage = $"Failed to enable TCP/IP mode for {device.Model}";
        }
    }

    [RelayCommand]
    private async Task ConnectWirelessAsync()
    {
        if (string.IsNullOrWhiteSpace(ConnectIpAddress)) return;

        StatusMessage = $"Connecting to {ConnectIpAddress}...";

        var parts = ConnectIpAddress.Split(':');
        var ip = parts[0];
        var port = 5555;
        
        if (parts.Length > 1 && int.TryParse(parts[1], out var parsedPort))
        {
            port = parsedPort;
        }

        var success = await _adbService.ConnectWirelessAsync(ip, port);
        StatusMessage = success ? $"Connected to {ConnectIpAddress}" : $"Failed to connect to {ConnectIpAddress}";
        
        if (success)
        {
            ConnectIpAddress = "";
            await RefreshDevicesAsync();
        }
    }

    [RelayCommand]
    private void OpenDeviceConfig(Device device)
    {
        // Navigate to config page with device
        // This will be handled by the MainWindow
    }

    public void StopRefreshTimer()
    {
        _refreshTimer?.Stop();
        _refreshTimer?.Dispose();
        _refreshTimer = null;
    }
}
