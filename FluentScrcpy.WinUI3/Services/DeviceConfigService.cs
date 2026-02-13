using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentScrcpy.WinUI3.Models;
using Windows.Storage;

namespace FluentScrcpy.WinUI3.Services;

public class DeviceConfigService
{
    private static readonly Lazy<DeviceConfigService> _instance = new(() => new DeviceConfigService());
    public static DeviceConfigService Instance => _instance.Value;

    private readonly string _configFolder;
    private readonly string _devicesFile;
    private readonly string _globalConfigFile;

    private DeviceConfig? _globalConfig;
    private Dictionary<string, DeviceConfig> _deviceConfigs = new();

    private DeviceConfigService()
    {
        _configFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FluentScrcpy");
        
        _devicesFile = Path.Combine(_configFolder, "devices.json");
        _globalConfigFile = Path.Combine(_configFolder, "config.json");

        Directory.CreateDirectory(_configFolder);
        
        LoadConfigs();
    }

    public DeviceConfig GlobalConfig
    {
        get
        {
            if (_globalConfig == null)
            {
                _globalConfig = LoadGlobalConfig();
            }
            return _globalConfig;
        }
    }

    public DeviceConfig GetDeviceConfig(string serial, string model)
    {
        if (!_deviceConfigs.TryGetValue(serial, out var config))
        {
            config = new DeviceConfig();
            _deviceConfigs[serial] = config;
        }
        return config;
    }

    public void UpdateDeviceConfig(string serial, DeviceConfig config)
    {
        _deviceConfigs[serial] = config;
        SaveDeviceConfigs();
    }

    public void UpdateGlobalConfig(DeviceConfig config)
    {
        _globalConfig = config;
        SaveGlobalConfig();
    }

    public void SaveGlobalConfig(DeviceConfig config)
    {
        _globalConfig = config;
        SaveGlobalConfig();
    }

    private void LoadConfigs()
    {
        _globalConfig = LoadGlobalConfig();
        _deviceConfigs = LoadDeviceConfigs();
    }

    private DeviceConfig LoadGlobalConfig()
    {
        try
        {
            if (File.Exists(_globalConfigFile))
            {
                var json = File.ReadAllText(_globalConfigFile);
                var config = JsonSerializer.Deserialize<DeviceConfig>(json);
                if (config != null)
                {
                    return config;
                }
            }
        }
        catch { }

        return new DeviceConfig();
    }

    private Dictionary<string, DeviceConfig> LoadDeviceConfigs()
    {
        try
        {
            if (File.Exists(_devicesFile))
            {
                var json = File.ReadAllText(_devicesFile);
                var configs = JsonSerializer.Deserialize<Dictionary<string, DeviceConfig>>(json);
                if (configs != null)
                {
                    return configs;
                }
            }
        }
        catch { }

        return new Dictionary<string, DeviceConfig>();
    }

    private void SaveGlobalConfig()
    {
        try
        {
            var json = JsonSerializer.Serialize(_globalConfig, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_globalConfigFile, json);
        }
        catch { }
    }

    private void SaveDeviceConfigs()
    {
        try
        {
            var json = JsonSerializer.Serialize(_deviceConfigs, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_devicesFile, json);
        }
        catch { }
    }
}
