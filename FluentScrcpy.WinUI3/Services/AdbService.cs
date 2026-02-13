using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentScrcpy.WinUI3.Models;

namespace FluentScrcpy.WinUI3.Services;

public class AdbService
{
    private static readonly Lazy<AdbService> _instance = new(() => new AdbService());
    public static AdbService Instance => _instance.Value;

    private string? _adbPath;

    private AdbService()
    {
        FindAdbPath();
    }

    private void FindAdbPath()
    {
        // Try to find adb in PATH
        _adbPath = "adb";
        
        // Check if adb is available
        try
        {
            var result = ExecuteAdbCommand("version");
            if (!result.Success)
            {
                // Try common locations
                var possiblePaths = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk", "platform-tools", "adb.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Android", "Sdk", "platform-tools", "adb.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Android", "Sdk", "platform-tools", "adb.exe"),
                };

                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        _adbPath = path;
                        break;
                    }
                }
            }
        }
        catch
        {
            // adb not found
        }
    }

    public bool IsAdbAvailable => !string.IsNullOrEmpty(_adbPath);

    public async Task<List<Device>> GetDevicesAsync()
    {
        var devices = new List<Device>();
        
        var result = await ExecuteAdbCommandAsync("devices -l");
        if (!result.Success) return devices;

        var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.StartsWith("List of devices") || string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 1) continue;

            var serial = parts[0];
            var model = "";
            var isWireless = serial.Contains(":");

            // Parse model from device info
            foreach (var part in parts)
            {
                if (part.StartsWith("model:"))
                {
                    model = part.Substring(6);
                    break;
                }
            }

            // Get IP address for USB devices
            string ipAddress = "";
            if (!isWireless)
            {
                ipAddress = await GetDeviceIpAsync(serial);
            }
            else
            {
                ipAddress = serial.Split(':')[0];
            }

            devices.Add(new Device
            {
                Serial = serial,
                Model = string.IsNullOrEmpty(model) ? serial : model,
                IpAddress = ipAddress,
                IsWireless = isWireless
            });
        }

        return devices;
    }

    public async Task<string> GetDeviceIpAsync(string serial)
    {
        var result = await ExecuteAdbCommandAsync($"-s {serial} shell ip route");
        if (!result.Success) return "";

        // Parse IP from route output
        var match = Regex.Match(result.Output, @"src\s+(\d+\.\d+\.\d+\.\d+)");
        return match.Success ? match.Groups[1].Value : "";
    }

    public async Task<bool> EnableTcpipAsync(string serial, int port = 5555)
    {
        var result = await ExecuteAdbCommandAsync($"-s {serial} tcpip {port}");
        return result.Success;
    }

    public async Task<bool> ConnectWirelessAsync(string ip, int port = 5555)
    {
        var result = await ExecuteAdbCommandAsync($"connect {ip}:{port}");
        return result.Success && result.Output.Contains("connected");
    }

    public async Task<bool> DisconnectWirelessAsync(string ip)
    {
        var result = await ExecuteAdbCommandAsync($"disconnect {ip}");
        return result.Success;
    }

    public async Task<bool> SendKeyEventAsync(string serial, string keyCode)
    {
        var result = await ExecuteAdbCommandAsync($"-s {serial} shell input keyevent {keyCode}");
        return result.Success;
    }

    private (bool Success, string Output) ExecuteAdbCommand(string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _adbPath ?? "adb",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return (false, "");

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit(5000);

            return (process.ExitCode == 0, output + error);
        }
        catch
        {
            return (false, "");
        }
    }

    private async Task<(bool Success, string Output)> ExecuteAdbCommandAsync(string arguments)
    {
        return await Task.Run(() => ExecuteAdbCommand(arguments));
    }
}
