using CommunityToolkit.Mvvm.ComponentModel;

namespace FluentScrcpy.WinUI3.Models;

public partial class Device : ObservableObject
{
    [ObservableProperty]
    private string _serial = string.Empty;

    [ObservableProperty]
    private string _model = string.Empty;

    [ObservableProperty]
    private string _ipAddress = string.Empty;

    [ObservableProperty]
    private bool _isWireless;

    [ObservableProperty]
    private DeviceConfig _config = new();

    // ========== Runtime State (Not saved) ==========
    [ObservableProperty]
    private bool _isMirroring;

    [ObservableProperty]
    private bool _isFullscreen;

    [ObservableProperty]
    private bool _isAlwaysOnTop;

    [ObservableProperty]
    private bool _isBorderless;
}

public partial class DeviceConfig : ObservableObject
{
    [ObservableProperty]
    private bool _useGlobalConfig = true;

    // ========== Video Settings ==========
    [ObservableProperty]
    private int _maxSize = 0; // 0 = native

    [ObservableProperty]
    private int _bitrate = 8; // Mbps

    [ObservableProperty]
    private int _fps = 0; // 0 = auto

    [ObservableProperty]
    private string _videoCodec = "h264"; // h264, h265, av1

    [ObservableProperty]
    private string _videoSource = "display"; // display, camera

    [ObservableProperty]
    private int _rotation = 0; // 0, 90, 180, 270

    [ObservableProperty]
    private string? _crop; // wx:h:x:y format

    [ObservableProperty]
    private int _displayBuffer = 0; // ms

    // ========== Audio Settings ==========
    [ObservableProperty]
    private bool _enableAudio = true;

    [ObservableProperty]
    private string _audioCodec = "opus"; // opus, aac, flac, raw

    [ObservableProperty]
    private string _audioSource = "output"; // output, playback

    [ObservableProperty]
    private int _audioBuffer = 50; // ms

    // ========== Control Settings ==========
    [ObservableProperty]
    private bool _turnScreenOff;

    [ObservableProperty]
    private bool _stayAwake;

    [ObservableProperty]
    private bool _showTouches;

    [ObservableProperty]
    private bool _disableControl;

    [ObservableProperty]
    private string _keyboardMode = "uhid"; // aoa, hid, uhid

    [ObservableProperty]
    private string _mouseMode = "uhid"; // aoa, hid, uhid

    // ========== Window Settings ==========
    [ObservableProperty]
    private bool _borderless;

    [ObservableProperty]
    private bool _alwaysOnTop;

    [ObservableProperty]
    private bool _fullscreen;

    [ObservableProperty]
    private bool _lockAspectRatio = true;

    [ObservableProperty]
    private string? _windowTitle; // null = device model

    [ObservableProperty]
    private int? _windowX; // null = auto

    [ObservableProperty]
    private int? _windowY; // null = auto

    [ObservableProperty]
    private int? _windowWidth; // null = auto

    [ObservableProperty]
    private int? _windowHeight; // null = auto

    // ========== Recording Settings ==========
    [ObservableProperty]
    private bool _enableRecording;

    [ObservableProperty]
    private string? _recordPath;

    [ObservableProperty]
    private string _recordFormat = "mp4"; // mp4, mkv

    // ========== Other Settings ==========
    [ObservableProperty]
    private bool _disableScreensaver = true;

    [ObservableProperty]
    private bool _powerOffOnClose;
}
