using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FluentScrcpy.WinUI3.Models;
using Microsoft.UI.Dispatching;
using Windows.Foundation;

namespace FluentScrcpy.WinUI3.Services;

public class ScrcpyService
{
    private static readonly Lazy<ScrcpyService> _instance = new(() => new ScrcpyService());
    public static ScrcpyService Instance => _instance.Value;

    private readonly Dictionary<string, Process> _runningProcesses = new();
    private readonly Dictionary<string, IntPtr> _windowHandles = new();
    private readonly Dictionary<string, RECT> _windowRects = new();
    private readonly Dictionary<string, int> _windowStyles = new();
    private string? _scrcpyPath;

    // Windows API for window control
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    // Window style constants
    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;
    private const int WS_CAPTION = 0x00C00000;
    private const int WS_THICKFRAME = 0x00040000;
    private const int WS_BORDER = 0x00800000;
    private const int WS_EX_CLIENTEDGE = 0x00000200;
    private const int WS_EX_WINDOWEDGE = 0x00000100;
    private const int WS_EX_TOPMOST = 0x00000008;

    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint MONITOR_DEFAULTTONEAREST = 2;

    // Additional Windows API
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    private const uint WM_KEYDOWN = 0x0100;
    private const uint WM_KEYUP = 0x0101;
    private const uint WM_SYSKEYDOWN = 0x0104;
    private const uint WM_SYSKEYUP = 0x0105;

    private const int VK_MENU = 0x12; // Alt key
    private const int VK_F = 0x46;
    private const int VK_T = 0x54;
    private const int VK_X = 0x58;
    private const int VK_G = 0x47;
    private const int VK_RETURN = 0x0D;

    private const int SW_SHOW = 5;
    private const int SW_MINIMIZE = 6;
    private const int SW_RESTORE = 9;

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    private const uint SWP_SHOWWINDOW = 0x0040;

    private ScrcpyService()
    {
        FindScrcpyPath();
    }

    private void FindScrcpyPath()
    {
        _scrcpyPath = "scrcpy";

        var possiblePaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scrcpy", "scrcpy.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "scrcpy", "scrcpy.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "scrcpy", "scrcpy.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet", "Packages", "Genymobile.scrcpy_Microsoft.Winget.Source_8wekyb3d8bbwe", "scrcpy-win64-v3.1", "scrcpy.exe"),
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                _scrcpyPath = path;
                break;
            }
        }
    }

    public bool IsScrcpyAvailable => !string.IsNullOrEmpty(_scrcpyPath);

    public bool StartMirroring(Device device, DeviceConfig? globalConfig = null)
    {
        var config = device.Config.UseGlobalConfig && globalConfig != null
            ? globalConfig
            : device.Config;

        var args = BuildArguments(device, config);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _scrcpyPath ?? "scrcpy",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = Process.Start(psi);
            if (process != null)
            {
                _runningProcesses[device.Serial] = process;
                device.IsMirroring = true;

                process.EnableRaisingEvents = true;
                process.Exited += (s, e) =>
                {
                    _runningProcesses.Remove(device.Serial);
                    _windowHandles.Remove(device.Serial);

                    // 在 UI 线程上更新属性
                    var dispatcherQueue = App.MainWindow?.DispatcherQueue;
                    if (dispatcherQueue != null)
                    {
                        dispatcherQueue.TryEnqueue(() =>
                        {
                            device.IsMirroring = false;
                        });
                    }
                    else
                    {
                        // 如果无法获取 DispatcherQueue，直接设置（可能会导致崩溃，但总比不设置好）
                        device.IsMirroring = false;
                    }
                };

                // Wait a moment for window to appear, then find it
                Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    FindScrcpyWindow(device);
                });

                process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        LogService.WriteLog("Scrcpy", $"[stdout] {e.Data}");
                };
                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        LogService.WriteLog("Scrcpy", $"[stderr] {e.Data}");
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                LogService.WriteLog("ScrcpyService", $"Started mirroring for device {device.Serial}");
                return true;
            }
        }
        catch (Exception ex)
        {
            LogService.WriteLog("ScrcpyService", $"Failed to start scrcpy: {ex.Message}");
        }

        return false;
    }

    public void StopMirroring(string serial)
    {
        if (_runningProcesses.TryGetValue(serial, out var process))
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    LogService.WriteLog("ScrcpyService", $"Stopped mirroring for device {serial}");
                }
            }
            catch (Exception ex)
            {
                LogService.WriteLog("ScrcpyService", $"Error stopping mirroring: {ex.Message}");
            }
            _runningProcesses.Remove(serial);
            _windowHandles.Remove(serial);
        }
    }

    public bool IsMirroring(string serial)
    {
        if (_runningProcesses.TryGetValue(serial, out var process))
        {
            return !process.HasExited;
        }
        return false;
    }

    public async Task<bool> RestartMirroring(Device device, DeviceConfig? globalConfig = null)
    {
        var serial = device.Serial;

        // Stop current mirroring
        StopMirroring(serial);

        // Wait for process to fully exit
        await Task.Delay(500);

        // Start new mirroring with updated config
        return StartMirroring(device, globalConfig);
    }

    private void FindScrcpyWindow(Device device)
    {
        try
        {
            // Get the process ID of the scrcpy process
            if (!_runningProcesses.TryGetValue(device.Serial, out var process))
            {
                LogService.WriteLog("ScrcpyService", $"No running process found for {device.Serial}");
                return;
            }

            var processId = (uint)process.Id;
            IntPtr foundHwnd = IntPtr.Zero;

            // Enumerate all windows to find the one belonging to our process
            EnumWindows((hwnd, lParam) =>
            {
                if (!IsWindowVisible(hwnd))
                    return true; // Continue enumerating

                GetWindowThreadProcessId(hwnd, out uint windowProcessId);

                if (windowProcessId == processId)
                {
                    // Get window title
                    int length = GetWindowTextLength(hwnd);
                    if (length > 0)
                    {
                        var sb = new StringBuilder(length + 1);
                        GetWindowText(hwnd, sb, sb.Capacity);
                        var title = sb.ToString();

                        LogService.WriteLog("ScrcpyService", $"Found window: '{title}' (PID: {windowProcessId})");

                        // Check if this is the main scrcpy window (not console window)
                        if (!string.IsNullOrEmpty(title) && 
                            !title.Contains("cmd", StringComparison.OrdinalIgnoreCase) &&
                            !title.Contains("powershell", StringComparison.OrdinalIgnoreCase) &&
                            !title.Contains("console", StringComparison.OrdinalIgnoreCase))
                        {
                            foundHwnd = hwnd;
                            return false; // Stop enumerating
                        }
                    }
                }

                return true; // Continue enumerating
            }, IntPtr.Zero);

            if (foundHwnd != IntPtr.Zero)
            {
                _windowHandles[device.Serial] = foundHwnd;
                LogService.WriteLog("ScrcpyService", $"Found scrcpy window for {device.Serial}: {foundHwnd}");

                // Apply initial window settings
                ApplyWindowSettings(device, foundHwnd);
            }
            else
            {
                LogService.WriteLog("ScrcpyService", $"Could not find scrcpy window for {device.Serial}");
            }
        }
        catch (Exception ex)
        {
            LogService.WriteLog("ScrcpyService", $"Error finding window: {ex.Message}");
        }
    }

    private void ApplyWindowSettings(Device device, IntPtr hwnd)
    {
        var config = device.Config;

        // Apply fullscreen
        if (config.Fullscreen && !device.IsFullscreen)
        {
            ToggleFullscreen(device.Serial);
        }

        // Apply always on top
        if (config.AlwaysOnTop && !device.IsAlwaysOnTop)
        {
            ToggleAlwaysOnTop(device.Serial);
        }

        // Apply borderless
        if (config.Borderless && !device.IsBorderless)
        {
            ToggleBorderless(device.Serial);
        }
    }

    // ========== Window Control Methods ==========

    private bool ValidateWindowHandle(string serial)
    {
        if (!_windowHandles.TryGetValue(serial, out var hwnd))
            return false;

        // Check if window is still valid
        if (!IsWindow(hwnd))
        {
            LogService.WriteLog("ScrcpyService", $"Window handle for {serial} is no longer valid, trying to find again");
            _windowHandles.Remove(serial);
            return false;
        }

        return true;
    }

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    public bool ToggleFullscreen(string serial)
    {
        if (!ValidateWindowHandle(serial))
        {
            LogService.WriteLog("ScrcpyService", $"No valid window handle for {serial}");
            return false;
        }

        var hwnd = _windowHandles[serial];

        try
        {
            // Get current window style
            var style = GetWindowLong(hwnd, GWL_STYLE);
            var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

            // Get window rect for restore
            GetWindowRect(hwnd, out RECT rect);

            // Check if currently fullscreen
            bool isFullscreen = (style & WS_CAPTION) == 0;

            if (!isFullscreen)
            {
                // Save current window rect for restore
                _windowRects[serial] = rect;

                // Remove caption and border to make fullscreen
                SetWindowLong(hwnd, GWL_STYLE, style & ~(WS_CAPTION | WS_THICKFRAME | WS_BORDER));
                SetWindowLong(hwnd, GWL_EXSTYLE, exStyle & ~(WS_EX_CLIENTEDGE | WS_EX_WINDOWEDGE));

                // Get monitor size
                var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                MONITORINFO monitorInfo = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
                GetMonitorInfo(monitor, ref monitorInfo);

                // Set window to cover entire monitor
                SetWindowPos(hwnd, IntPtr.Zero,
                    monitorInfo.rcMonitor.Left,
                    monitorInfo.rcMonitor.Top,
                    monitorInfo.rcMonitor.Right - monitorInfo.rcMonitor.Left,
                    monitorInfo.rcMonitor.Bottom - monitorInfo.rcMonitor.Top,
                    SWP_FRAMECHANGED | SWP_NOZORDER);

                ShowWindow(hwnd, SW_SHOW);
            }
            else
            {
                // Restore window style
                SetWindowLong(hwnd, GWL_STYLE, style | WS_CAPTION | WS_THICKFRAME | WS_BORDER);
                SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_CLIENTEDGE | WS_EX_WINDOWEDGE);

                // Restore window size and position
                if (_windowRects.TryGetValue(serial, out var savedRect))
                {
                    SetWindowPos(hwnd, IntPtr.Zero,
                        savedRect.Left,
                        savedRect.Top,
                        savedRect.Right - savedRect.Left,
                        savedRect.Bottom - savedRect.Top,
                        SWP_FRAMECHANGED | SWP_NOZORDER);
                }

                ShowWindow(hwnd, SW_SHOW);
            }

            // Update device state via callback
            OnWindowStateChanged?.Invoke(serial, WindowStateType.Fullscreen, !isFullscreen);
            LogService.WriteLog("ScrcpyService", $"Fullscreen toggled: {!isFullscreen}");
            return true;
        }
        catch (Exception ex)
        {
            LogService.WriteLog("ScrcpyService", $"Error toggling fullscreen: {ex.Message}");
        }

        return false;
    }

    public bool ToggleAlwaysOnTop(string serial)
    {
        if (!ValidateWindowHandle(serial))
        {
            LogService.WriteLog("ScrcpyService", $"No valid window handle for {serial}");
            return false;
        }

        var hwnd = _windowHandles[serial];

        try
        {
            // Get current window rect
            GetWindowRect(hwnd, out RECT rect);
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            // Check current state using extended style
            var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            bool isTopmost = (exStyle & WS_EX_TOPMOST) != 0;

            var hwndInsertAfter = !isTopmost ? HWND_TOPMOST : HWND_NOTOPMOST;

            SetWindowPos(hwnd, hwndInsertAfter, rect.Left, rect.Top, width, height,
                SWP_SHOWWINDOW);

            // Update device state via callback
            OnWindowStateChanged?.Invoke(serial, WindowStateType.AlwaysOnTop, !isTopmost);
            LogService.WriteLog("ScrcpyService", $"Always on top: {!isTopmost}");
            return true;
        }
        catch (Exception ex)
        {
            LogService.WriteLog("ScrcpyService", $"Error toggling always on top: {ex.Message}");
        }

        return false;
    }

    public bool ToggleBorderless(string serial)
    {
        if (!ValidateWindowHandle(serial))
        {
            LogService.WriteLog("ScrcpyService", $"No valid window handle for {serial}");
            return false;
        }

        var hwnd = _windowHandles[serial];

        try
        {
            // Get current window style
            var style = GetWindowLong(hwnd, GWL_STYLE);

            // Check if currently borderless
            bool isBorderless = (style & WS_CAPTION) == 0;

            if (!isBorderless)
            {
                // Save current style for restore
                _windowStyles[serial] = style;

                // Remove caption and border
                SetWindowLong(hwnd, GWL_STYLE, style & ~(WS_CAPTION | WS_THICKFRAME));
            }
            else
            {
                // Restore original style
                if (_windowStyles.TryGetValue(serial, out var savedStyle))
                {
                    SetWindowLong(hwnd, GWL_STYLE, savedStyle);
                }
                else
                {
                    SetWindowLong(hwnd, GWL_STYLE, style | WS_CAPTION | WS_THICKFRAME);
                }
            }

            // Apply changes
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);

            // Update device state via callback
            OnWindowStateChanged?.Invoke(serial, WindowStateType.Borderless, !isBorderless);
            LogService.WriteLog("ScrcpyService", $"Borderless toggled: {!isBorderless}");
            return true;
        }
        catch (Exception ex)
        {
            LogService.WriteLog("ScrcpyService", $"Error toggling borderless: {ex.Message}");
        }

        return false;
    }

    public bool ToggleLockAspectRatio(string serial)
    {
        return SendShortcut(serial, VK_G, true);
    }

    // Window state changed event
    public event Action<string, WindowStateType, bool>? OnWindowStateChanged;

    public enum WindowStateType
    {
        Fullscreen,
        AlwaysOnTop,
        Borderless
    }

    public bool SendShortcut(string serial, int keyCode, bool useAlt = false)
    {
        if (_windowHandles.TryGetValue(serial, out var hwnd))
        {
            try
            {
                SetForegroundWindow(hwnd);

                if (useAlt)
                {
                    // Alt + Key
                    PostMessage(hwnd, WM_SYSKEYDOWN, (IntPtr)VK_MENU, IntPtr.Zero);
                    PostMessage(hwnd, WM_SYSKEYDOWN, (IntPtr)keyCode, IntPtr.Zero);
                    PostMessage(hwnd, WM_SYSKEYUP, (IntPtr)keyCode, IntPtr.Zero);
                    PostMessage(hwnd, WM_SYSKEYUP, (IntPtr)VK_MENU, IntPtr.Zero);
                }
                else
                {
                    // Just Key
                    PostMessage(hwnd, WM_KEYDOWN, (IntPtr)keyCode, IntPtr.Zero);
                    PostMessage(hwnd, WM_KEYUP, (IntPtr)keyCode, IntPtr.Zero);
                }

                return true;
            }
            catch (Exception ex)
            {
                LogService.WriteLog("ScrcpyService", $"Error sending shortcut: {ex.Message}");
            }
        }
        return false;
    }

    public bool FocusWindow(string serial)
    {
        if (!ValidateWindowHandle(serial))
        {
            LogService.WriteLog("ScrcpyService", $"No valid window handle for {serial}");
            return false;
        }

        var hwnd = _windowHandles[serial];
        var result = SetForegroundWindow(hwnd);
        LogService.WriteLog("ScrcpyService", $"FocusWindow for {serial}: {result}");
        return result;
    }

    public bool MinimizeWindow(string serial)
    {
        if (!ValidateWindowHandle(serial))
        {
            LogService.WriteLog("ScrcpyService", $"No valid window handle for {serial}");
            return false;
        }

        var hwnd = _windowHandles[serial];
        return ShowWindow(hwnd, SW_MINIMIZE);
    }

    public bool RestoreWindow(string serial)
    {
        if (!ValidateWindowHandle(serial))
        {
            LogService.WriteLog("ScrcpyService", $"No valid window handle for {serial}");
            return false;
        }

        var hwnd = _windowHandles[serial];
        return ShowWindow(hwnd, SW_RESTORE);
    }

    private Device? GetDeviceBySerial(string serial)
    {
        // This would need to be passed in or stored elsewhere
        // For now, return null
        return null;
    }

    // ========== Argument Building ==========

    private string BuildArguments(Device device, DeviceConfig config)
    {
        var sb = new StringBuilder();

        // Device serial
        sb.Append($"-s {device.Serial} ");

        // ========== Video Settings ==========
        if (config.MaxSize > 0)
        {
            sb.Append($"--max-size={config.MaxSize} ");
        }

        sb.Append($"--video-bit-rate={config.Bitrate}M ");

        if (config.Fps > 0)
        {
            sb.Append($"--max-fps={config.Fps} ");
        }

        sb.Append($"--video-codec={config.VideoCodec} ");

        if (config.VideoSource != "display")
        {
            sb.Append($"--video-source={config.VideoSource} ");
        }

        if (config.Rotation != 0)
        {
            sb.Append($"--rotation={config.Rotation} ");
        }

        if (!string.IsNullOrEmpty(config.Crop))
        {
            sb.Append($"--crop={config.Crop} ");
        }

        if (config.DisplayBuffer > 0)
        {
            sb.Append($"--display-buffer={config.DisplayBuffer} ");
        }

        // ========== Audio Settings ==========
        if (!config.EnableAudio)
        {
            sb.Append("--no-audio ");
        }
        else
        {
            sb.Append($"--audio-codec={config.AudioCodec} ");

            if (config.AudioSource != "output")
            {
                sb.Append($"--audio-source={config.AudioSource} ");
            }

            if (config.AudioBuffer != 50)
            {
                sb.Append($"--audio-buffer={config.AudioBuffer} ");
            }
        }

        // ========== Control Settings ==========
        if (config.TurnScreenOff)
        {
            sb.Append("--turn-screen-off ");
        }

        if (config.StayAwake)
        {
            sb.Append("--stay-awake ");
        }

        if (config.ShowTouches)
        {
            sb.Append("--show-touches ");
        }

        if (config.DisableControl)
        {
            sb.Append("--no-control ");
        }

        if (config.KeyboardMode != "uhid")
        {
            sb.Append($"--keyboard={config.KeyboardMode} ");
        }

        if (config.MouseMode != "uhid")
        {
            sb.Append($"--mouse={config.MouseMode} ");
        }

        // ========== Window Settings ==========
        if (config.Borderless)
        {
            sb.Append("--window-borderless ");
        }

        if (config.AlwaysOnTop)
        {
            sb.Append("--always-on-top ");
        }

        if (config.Fullscreen)
        {
            sb.Append("--fullscreen ");
        }

        if (!config.LockAspectRatio)
        {
            sb.Append("--no-window-clip ");
        }

        if (!string.IsNullOrEmpty(config.WindowTitle))
        {
            sb.Append($"--window-title=\"{config.WindowTitle}\" ");
        }

        if (config.WindowX.HasValue)
        {
            sb.Append($"--window-x={config.WindowX} ");
        }

        if (config.WindowY.HasValue)
        {
            sb.Append($"--window-y={config.WindowY} ");
        }

        if (config.WindowWidth.HasValue)
        {
            sb.Append($"--window-width={config.WindowWidth} ");
        }

        if (config.WindowHeight.HasValue)
        {
            sb.Append($"--window-height={config.WindowHeight} ");
        }

        // ========== Recording Settings ==========
        if (config.EnableRecording && !string.IsNullOrEmpty(config.RecordPath))
        {
            // 移除路径中已有的引号和空白字符，避免重复
            var cleanPath = config.RecordPath?.Trim('"', ' ', '\t', '\n', '\r') ?? "";
            
            // 如果路径是文件夹，则添加默认文件名
            if (Directory.Exists(cleanPath))
            {
                var fileName = $"scrcpy_record_{DateTime.Now:yyyyMMdd_HHmmss}.{config.RecordFormat}";
                cleanPath = Path.Combine(cleanPath, fileName);
            }
            
            // 如果路径没有扩展名，添加默认扩展名
            if (string.IsNullOrEmpty(Path.GetExtension(cleanPath)))
            {
                cleanPath = $"{cleanPath}.{config.RecordFormat}";
            }
            
            if (!string.IsNullOrEmpty(cleanPath))
            {
                sb.Append($"--record=\"{cleanPath}\" ");

                if (config.RecordFormat != "mp4")
                {
                    sb.Append($"--record-format={config.RecordFormat} ");
                }
            }
        }

        // ========== Other Settings ==========
        if (!config.DisableScreensaver)
        {
            sb.Append("--no-disable-screensaver ");
        }

        if (config.PowerOffOnClose)
        {
            sb.Append("--power-off-on-close ");
        }

        return sb.ToString().Trim();
    }
}
