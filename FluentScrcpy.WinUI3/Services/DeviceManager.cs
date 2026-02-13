using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FluentScrcpy.WinUI3.Models;

namespace FluentScrcpy.WinUI3.Services;

public class DeviceManager
{
    private static readonly Lazy<DeviceManager> _instance = new(() => new DeviceManager());
    public static DeviceManager Instance => _instance.Value;

    private readonly ObservableCollection<Device> _devices = new();
    private readonly Dictionary<string, DeviceState> _deviceStates = new();

    public ObservableCollection<Device> Devices => _devices;

    private DeviceManager()
    {
    }

    public void UpdateDevices(IEnumerable<Device> newDevices)
    {
        // Save current states
        foreach (var device in _devices)
        {
            _deviceStates[device.Serial] = new DeviceState
            {
                IsMirroring = device.IsMirroring,
                IsFullscreen = device.IsFullscreen,
                IsAlwaysOnTop = device.IsAlwaysOnTop,
                IsBorderless = device.IsBorderless
            };
        }

        // Clear and repopulate
        _devices.Clear();
        foreach (var device in newDevices)
        {
            // Restore state if exists
            if (_deviceStates.TryGetValue(device.Serial, out var state))
            {
                device.IsMirroring = state.IsMirroring;
                device.IsFullscreen = state.IsFullscreen;
                device.IsAlwaysOnTop = state.IsAlwaysOnTop;
                device.IsBorderless = state.IsBorderless;
            }
            _devices.Add(device);
        }
    }

    public void ClearDevices()
    {
        _devices.Clear();
        _deviceStates.Clear();
    }

    public Device? GetDevice(string serial)
    {
        return _devices.FirstOrDefault(d => d.Serial == serial);
    }

    public void UpdateDeviceState(string serial, bool isMirroring)
    {
        var device = GetDevice(serial);
        if (device != null)
        {
            device.IsMirroring = isMirroring;
        }

        // Also update saved state
        if (_deviceStates.TryGetValue(serial, out var state))
        {
            state.IsMirroring = isMirroring;
        }
        else
        {
            _deviceStates[serial] = new DeviceState { IsMirroring = isMirroring };
        }
    }

    private class DeviceState
    {
        public bool IsMirroring { get; set; }
        public bool IsFullscreen { get; set; }
        public bool IsAlwaysOnTop { get; set; }
        public bool IsBorderless { get; set; }
    }
}
