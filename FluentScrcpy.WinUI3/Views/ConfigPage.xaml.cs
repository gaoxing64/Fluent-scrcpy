using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using Windows.Foundation;
using FluentScrcpy.WinUI3.Models;
using FluentScrcpy.WinUI3.Services;

namespace FluentScrcpy.WinUI3.Views;

public sealed partial class ConfigPage : Page
{
    private readonly DeviceConfigService _configService = DeviceConfigService.Instance;
    private DeviceConfig _config = new();
    private Device? _device;
    private Button? _currentNavButton;
    private Border? _currentSelectionBorder;

    public ConfigPage()
    {
        try
        {
            LogService.WriteLog("ConfigPage", "Constructor started");
            InitializeComponent();
            LogService.WriteLog("ConfigPage", "InitializeComponent completed");
        }
        catch (Exception ex)
        {
            LogService.WriteLog("ConfigPage", $"Error in constructor: {ex}");
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        LogService.WriteLog("ConfigPage", "OnNavigatedTo started");
        try
        {
            base.OnNavigatedTo(e);
            LogService.WriteLog("ConfigPage", "base.OnNavigatedTo completed");

            if (e.Parameter is Device device)
            {
                LogService.WriteLog("ConfigPage", $"Navigated with device: {device.Model} ({device.Serial})");
                _device = device;
                _config = device.Config ?? new DeviceConfig();
                LogService.WriteLog("ConfigPage", "Device config loaded");
                
                if (DeviceModelTextBlock != null)
                {
                    DeviceModelTextBlock.Text = device.Model ?? "Unknown Device";
                }
                if (DeviceSerialTextBlock != null)
                {
                    DeviceSerialTextBlock.Text = device.Serial ?? "Unknown Serial";
                }
            }
            else
            {
                LogService.WriteLog("ConfigPage", "Navigated without device parameter, using global config");
                _device = null;
                try
                {
                    _config = _configService.GlobalConfig;
                    LogService.WriteLog("ConfigPage", "Global config loaded");
                }
                catch (Exception ex)
                {
                    LogService.WriteLog("ConfigPage", $"Error loading global config: {ex}");
                    _config = new DeviceConfig();
                }
                if (DeviceModelTextBlock != null)
                {
                    DeviceModelTextBlock.Text = "全局配置";
                }
                if (DeviceSerialTextBlock != null)
                {
                    DeviceSerialTextBlock.Text = "应用于所有设备";
                }
            }

            LogService.WriteLog("ConfigPage", "Calling LoadConfigToUI");
            LoadConfigToUI();
            LogService.WriteLog("ConfigPage", "LoadConfigToUI completed");
            
            LogService.WriteLog("ConfigPage", "Calling UpdateAllCurrentValues");
            UpdateAllCurrentValues();
            LogService.WriteLog("ConfigPage", "UpdateAllCurrentValues completed");

            // 默认选中第一个导航项（视频设置）
            if (_currentSelectionBorder == null && NavVideoSettingsSelectionBorder != null)
            {
                _currentNavButton = NavVideoSettings;
                _currentSelectionBorder = NavVideoSettingsSelectionBorder;
                _currentSelectionBorder.Opacity = 1;
                LogService.WriteLog("ConfigPage", "Default navigation item selected: Video");
            }
        }
        catch (Exception ex)
        {
            LogService.WriteLog("ConfigPage", $"Error in OnNavigatedTo: {ex}");
            LogService.WriteLog("ConfigPage", $"Exception stack trace: {ex.StackTrace}");
            // 即使出现异常，也确保页面可以显示
            _config = _config ?? new DeviceConfig();
            try
            {
                LoadConfigToUI();
            }
            catch (Exception ex2)
            {
                LogService.WriteLog("ConfigPage", $"Error in LoadConfigToUI after exception: {ex2}");
            }
        }
        LogService.WriteLog("ConfigPage", "OnNavigatedTo completed");
    }

    private void LoadConfigToUI()
    {
        try
        {
            // 确保 _config 不为 null
            if (_config == null)
            {
                _config = new DeviceConfig();
            }

            // Video Settings
            if (BitrateSlider != null)
                BitrateSlider.Value = _config.Bitrate;
            if (ResolutionComboBox != null)
                ResolutionComboBox.SelectedIndex = GetResolutionIndex(_config.MaxSize);
            if (FpsSlider != null)
                FpsSlider.Value = _config.Fps;
            if (VideoCodecComboBox != null)
                VideoCodecComboBox.SelectedIndex = _config.VideoCodec switch
                {
                    "h264" => 0,
                    "h265" => 1,
                    "av1" => 2,
                    _ => 0
                };
            if (VideoSourceComboBox != null)
                VideoSourceComboBox.SelectedIndex = _config.VideoSource == "display" ? 0 : 1;
            if (RotationComboBox != null)
                RotationComboBox.SelectedIndex = _config.Rotation switch
                {
                    0 => 0,
                    90 => 1,
                    180 => 2,
                    270 => 3,
                    _ => 0
                };
            if (DisplayBufferSlider != null)
                DisplayBufferSlider.Value = _config.DisplayBuffer;
            if (CropTextBox != null)
                CropTextBox.Text = _config.Crop ?? "";

            // Audio Settings
            if (EnableAudioToggle != null)
                EnableAudioToggle.IsOn = _config.EnableAudio;
            if (AudioCodecComboBox != null)
                AudioCodecComboBox.SelectedIndex = _config.AudioCodec switch
                {
                    "opus" => 0,
                    "aac" => 1,
                    "flac" => 2,
                    "raw" => 3,
                    _ => 0
                };
            if (AudioSourceComboBox != null)
                AudioSourceComboBox.SelectedIndex = _config.AudioSource == "output" ? 0 : 1;
            if (AudioBufferSlider != null)
                AudioBufferSlider.Value = _config.AudioBuffer;
            // 使用动画控制面板状态
            if (_config.EnableAudio)
            {
                if (AudioOptionsPanel != null && (AudioOptionsPanel.Visibility == Visibility.Collapsed || AudioOptionsPanel.Opacity == 0))
                {
                    ExpandPanel(AudioOptionsPanel, AudioArrowRotate);
                }
            }
            else
            {
                if (AudioOptionsPanel != null && AudioOptionsPanel.Visibility == Visibility.Visible && AudioOptionsPanel.Opacity == 1)
                {
                    CollapsePanel(AudioOptionsPanel, AudioArrowRotate);
                }
            }

            // Control Settings
            if (TurnScreenOffToggle != null)
                TurnScreenOffToggle.IsOn = _config.TurnScreenOff;
            if (StayAwakeToggle != null)
                StayAwakeToggle.IsOn = _config.StayAwake;
            if (ShowTouchesToggle != null)
                ShowTouchesToggle.IsOn = _config.ShowTouches;
            if (DisableControlToggle != null)
                DisableControlToggle.IsOn = _config.DisableControl;
            if (KeyboardModeComboBox != null)
                KeyboardModeComboBox.SelectedIndex = _config.KeyboardMode switch
                {
                    "uhid" => 0,
                    "aoa" => 1,
                    "hid" => 2,
                    _ => 0
                };
            if (MouseModeComboBox != null)
                MouseModeComboBox.SelectedIndex = _config.MouseMode switch
                {
                    "uhid" => 0,
                    "aoa" => 1,
                    "hid" => 2,
                    _ => 0
                };

            // Window Settings
            if (FullscreenToggle != null)
                FullscreenToggle.IsOn = _config.Fullscreen;
            if (AlwaysOnTopToggle != null)
                AlwaysOnTopToggle.IsOn = _config.AlwaysOnTop;
            if (BorderlessToggle != null)
                BorderlessToggle.IsOn = _config.Borderless;
            if (LockAspectRatioToggle != null)
                LockAspectRatioToggle.IsOn = _config.LockAspectRatio;
            if (WindowTitleTextBox != null)
                WindowTitleTextBox.Text = _config.WindowTitle ?? "";
            if (WindowXNumberBox != null)
                WindowXNumberBox.Value = _config.WindowX ?? double.NaN;
            if (WindowYNumberBox != null)
                WindowYNumberBox.Value = _config.WindowY ?? double.NaN;
            if (WindowWidthNumberBox != null)
                WindowWidthNumberBox.Value = _config.WindowWidth ?? double.NaN;
            if (WindowHeightNumberBox != null)
                WindowHeightNumberBox.Value = _config.WindowHeight ?? double.NaN;

            // Recording Settings
            if (EnableRecordingToggle != null)
                EnableRecordingToggle.IsOn = _config.EnableRecording;
            if (RecordPathTextBox != null)
                RecordPathTextBox.Text = _config.RecordPath ?? "";
            if (RecordFormatComboBox != null)
                RecordFormatComboBox.SelectedIndex = _config.RecordFormat == "mp4" ? 0 : 1;
            // 使用动画控制面板状态
            if (_config.EnableRecording)
            {
                if (RecordOptionsPanel != null && (RecordOptionsPanel.Visibility == Visibility.Collapsed || RecordOptionsPanel.Opacity == 0))
                {
                    ExpandPanel(RecordOptionsPanel, RecordArrowRotate);
                }
            }
            else
            {
                if (RecordOptionsPanel != null && RecordOptionsPanel.Visibility == Visibility.Visible && RecordOptionsPanel.Opacity == 1)
                {
                    CollapsePanel(RecordOptionsPanel, RecordArrowRotate);
                }
            }

            // Other Settings
            if (DisableScreensaverToggle != null)
                DisableScreensaverToggle.IsOn = _config.DisableScreensaver;
            if (PowerOffOnCloseToggle != null)
                PowerOffOnCloseToggle.IsOn = _config.PowerOffOnClose;
        }
        catch (Exception ex)
        {
            LogService.WriteLog("ConfigPage", $"Error in LoadConfigToUI: {ex}");
            // 即使出现异常，也确保页面可以显示基本控件
        }
    }

    #region Current Value Updates

    private void UpdateAllCurrentValues()
    {
        try
        {
            UpdateVideoCurrentValue();
            UpdateAudioCurrentValue();
            UpdateControlCurrentValue();
            UpdateWindowCurrentValue();
            UpdateRecordCurrentValue();
        }
        catch (Exception ex)
        {
            LogService.WriteLog("ConfigPage", $"Error in UpdateAllCurrentValues: {ex}");
        }
    }

    private void UpdateVideoCurrentValue()
    {
        try
        {
            // 确保控件和选中项不为 null
            if (VideoCodecComboBox != null && ResolutionComboBox != null && VideoCurrentValue != null)
            {
                string codec = "H.264";
                if (VideoCodecComboBox.SelectedItem is ComboBoxItem codecItem)
                {
                    codec = codecItem.Content?.ToString()?.Split(' ')[0] ?? "H.264";
                }
                
                string resolution = "原生";
                if (ResolutionComboBox.SelectedItem is ComboBoxItem resolutionItem)
                {
                    resolution = resolutionItem.Content?.ToString() ?? "原生";
                }
                
                VideoCurrentValue.Text = $"{codec}, {resolution}";
            }
        }
        catch (Exception ex)
        {
            LogService.WriteLog("ConfigPage", $"Error in UpdateVideoCurrentValue: {ex}");
            if (VideoCurrentValue != null)
            {
                VideoCurrentValue.Text = "默认";
            }
        }
    }

    private void UpdateAudioCurrentValue()
    {
        try
        {
            // 确保控件不为 null
            if (AudioCodecComboBox != null && AudioCurrentValue != null)
            {
                string codec = "Opus";
                if (AudioCodecComboBox.SelectedItem is ComboBoxItem codecItem)
                {
                    codec = codecItem.Content?.ToString()?.Split(' ')[0] ?? "Opus";
                }
                AudioCurrentValue.Text = codec;
            }
        }
        catch (Exception ex)
        {
            LogService.WriteLog("ConfigPage", $"Error in UpdateAudioCurrentValue: {ex}");
            if (AudioCurrentValue != null)
            {
                AudioCurrentValue.Text = "默认";
            }
        }
    }

    private void UpdateControlCurrentValue()
    {
        try
        {
            // 确保控件不为 null
            if (ControlCurrentValue != null)
            {
                var enabledOptions = new[]
                {
                    TurnScreenOffToggle?.IsOn == true ? "关闭屏幕" : null,
                    StayAwakeToggle?.IsOn == true ? "保持唤醒" : null,
                    ShowTouchesToggle?.IsOn == true ? "显示触摸" : null,
                    DisableControlToggle?.IsOn == true ? "仅查看" : null
                }.Where(x => x != null).ToList();

                if (enabledOptions.Count == 0)
                {
                    ControlCurrentValue.Text = "默认";
                }
                else if (enabledOptions.Count <= 2)
                {
                    ControlCurrentValue.Text = string.Join(", ", enabledOptions);
                }
                else
                {
                    ControlCurrentValue.Text = $"{enabledOptions.Count} 项已启用";
                }
            }
        }
        catch (Exception ex)
        {
            LogService.WriteLog("ConfigPage", $"Error in UpdateControlCurrentValue: {ex}");
            if (ControlCurrentValue != null)
            {
                ControlCurrentValue.Text = "默认";
            }
        }
    }

    private void UpdateWindowCurrentValue()
    {
        try
        {
            // 确保控件不为 null
            if (WindowCurrentValue != null)
            {
                var modes = new[]
                {
                    FullscreenToggle?.IsOn == true ? "全屏" : null,
                    AlwaysOnTopToggle?.IsOn == true ? "置顶" : null,
                    BorderlessToggle?.IsOn == true ? "无边框" : null,
                    LockAspectRatioToggle?.IsOn == false ? "自由比例" : null
                }.Where(x => x != null).ToList();

                if (modes.Count == 0)
                {
                    WindowCurrentValue.Text = "默认";
                }
                else
                {
                    WindowCurrentValue.Text = string.Join(", ", modes);
                }
            }
        }
        catch (Exception ex)
        {
            LogService.WriteLog("ConfigPage", $"Error in UpdateWindowCurrentValue: {ex}");
            if (WindowCurrentValue != null)
            {
                WindowCurrentValue.Text = "默认";
            }
        }
    }

    private void UpdateRecordCurrentValue()
    {
        try
        {
            // 确保控件不为 null
            if (RecordFormatComboBox != null && RecordCurrentValue != null)
            {
                string format = "MP4";
                if (RecordFormatComboBox.SelectedItem is ComboBoxItem formatItem)
                {
                    format = formatItem.Content?.ToString() ?? "MP4";
                }
                RecordCurrentValue.Text = format;
            }
        }
        catch (Exception ex)
        {
            LogService.WriteLog("ConfigPage", $"Error in UpdateRecordCurrentValue: {ex}");
            if (RecordCurrentValue != null)
            {
                RecordCurrentValue.Text = "默认";
            }
        }
    }

    #endregion

    #region Selection Changed Handlers

    private void VideoCodecComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateVideoCurrentValue();
    }

    private void ResolutionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateVideoCurrentValue();
    }

    private void AudioCodecComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateAudioCurrentValue();
    }

    private void ControlComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateControlCurrentValue();
    }

    private void ControlToggle_Toggled(object sender, RoutedEventArgs e)
    {
        UpdateControlCurrentValue();
    }

    private void WindowToggle_Toggled(object sender, RoutedEventArgs e)
    {
        UpdateWindowCurrentValue();
    }

    private void RecordFormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateRecordCurrentValue();
    }

    #endregion

    #region Header Button Click Handlers

    private void VideoHeaderButton_Click(object sender, RoutedEventArgs e)
    {
        TogglePanel(VideoOptionsPanel, VideoArrowRotate);
    }

    private void ControlHeaderButton_Click(object sender, RoutedEventArgs e)
    {
        TogglePanel(ControlOptionsPanel, ControlArrowRotate);
    }

    private void WindowHeaderButton_Click(object sender, RoutedEventArgs e)
    {
        TogglePanel(WindowOptionsPanel, WindowArrowRotate);
    }

    private void OtherHeaderButton_Click(object sender, RoutedEventArgs e)
    {
        TogglePanel(OtherOptionsPanel, OtherArrowRotate);
    }

    private void TogglePanel(StackPanel panel, RotateTransform arrowRotate)
    {
        if (panel.Visibility == Visibility.Collapsed || panel.Opacity == 0)
        {
            ExpandPanel(panel, arrowRotate);
        }
        else
        {
            CollapsePanel(panel, arrowRotate);
        }
    }

    private void ExpandPanel(StackPanel panel, RotateTransform arrowRotate)
    {
        // 先设置可见
        panel.Visibility = Visibility.Visible;
        
        // 创建一个包含所有动画的 Storyboard
        var storyboard = new Storyboard();
        
        // 内容淡入动画 - 从 Opacity=0 到 Opacity=1
        var opacityAnimation = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = new Duration(TimeSpan.FromMilliseconds(250)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(opacityAnimation, panel);
        Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
        storyboard.Children.Add(opacityAnimation);

        // 箭头旋转动画
        var rotateAnimation = new DoubleAnimation
        {
            From = 0,
            To = 180,
            Duration = new Duration(TimeSpan.FromMilliseconds(250)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(rotateAnimation, arrowRotate);
        Storyboard.SetTargetProperty(rotateAnimation, "Angle");
        storyboard.Children.Add(rotateAnimation);

        // 开始动画
        storyboard.Begin();
    }

    private void CollapsePanel(StackPanel panel, RotateTransform arrowRotate)
    {
        // 创建一个包含所有动画的 Storyboard
        var storyboard = new Storyboard();
        
        // 内容淡出动画 - 从 Opacity=1 到 Opacity=0
        var opacityAnimation = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(opacityAnimation, panel);
        Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
        storyboard.Children.Add(opacityAnimation);

        // 箭头旋转动画
        var rotateAnimation = new DoubleAnimation
        {
            From = 180,
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(rotateAnimation, arrowRotate);
        Storyboard.SetTargetProperty(rotateAnimation, "Angle");
        storyboard.Children.Add(rotateAnimation);

        // 动画完成后设置 Visibility.Collapsed
        storyboard.Completed += (s, e) =>
        {
            panel.Visibility = Visibility.Collapsed;
        };

        // 开始动画
        storyboard.Begin();
    }

    #endregion

    private void EnableAudioToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (EnableAudioToggle.IsOn)
        {
            if (AudioOptionsPanel.Visibility == Visibility.Collapsed || AudioOptionsPanel.Opacity == 0)
            {
                ExpandPanel(AudioOptionsPanel, AudioArrowRotate);
            }
        }
        else
        {
            if (AudioOptionsPanel.Visibility == Visibility.Visible && AudioOptionsPanel.Opacity == 1)
            {
                CollapsePanel(AudioOptionsPanel, AudioArrowRotate);
            }
        }
    }

    private void EnableRecordingToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (EnableRecordingToggle.IsOn)
        {
            if (RecordOptionsPanel.Visibility == Visibility.Collapsed || RecordOptionsPanel.Opacity == 0)
            {
                ExpandPanel(RecordOptionsPanel, RecordArrowRotate);
            }
        }
        else
        {
            if (RecordOptionsPanel.Visibility == Visibility.Visible && RecordOptionsPanel.Opacity == 1)
            {
                CollapsePanel(RecordOptionsPanel, RecordArrowRotate);
            }
        }
    }

    private void AudioHeaderGrid_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        // 只有当音频启用时才允许展开/折叠
        if (EnableAudioToggle.IsOn)
        {
            TogglePanel(AudioOptionsPanel, AudioArrowRotate);
        }
    }

    private void RecordHeaderGrid_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        // 只有当录制启用时才允许展开/折叠
        if (EnableRecordingToggle.IsOn)
        {
            TogglePanel(RecordOptionsPanel, RecordArrowRotate);
        }
    }

    private void NavigationItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button navButton && navButton.Tag is string tag)
            {
                // 移除之前选中项的选中状态
                if (_currentSelectionBorder != null)
                {
                    _currentSelectionBorder.Opacity = 0;
                }

                // 设置新的选中项
                _currentNavButton = navButton;
                _currentSelectionBorder = tag switch
                {
                    "Video" => NavVideoSettingsSelectionBorder,
                    "Audio" => NavAudioSettingsSelectionBorder,
                    "Control" => NavControlSettingsSelectionBorder,
                    "Window" => NavWindowSettingsSelectionBorder,
                    "Record" => NavRecordSettingsSelectionBorder,
                    "Other" => NavOtherSettingsSelectionBorder,
                    _ => null
                };

                // 显示新的选中边框
                if (_currentSelectionBorder != null)
                {
                    _currentSelectionBorder.Opacity = 1;
                }

                // 先隐藏所有设置卡片
                VideoSettingsSection.Visibility = Visibility.Collapsed;
                AudioSettingsSection.Visibility = Visibility.Collapsed;
                ControlSettingsSection.Visibility = Visibility.Collapsed;
                WindowSettingsSection.Visibility = Visibility.Collapsed;
                RecordSettingsSection.Visibility = Visibility.Collapsed;
                OtherSettingsSection.Visibility = Visibility.Collapsed;

                // 根据标签显示对应的设置卡片
                switch (tag)
                {
                    case "Video":
                        VideoSettingsSection.Visibility = Visibility.Visible;
                        break;
                    case "Audio":
                        AudioSettingsSection.Visibility = Visibility.Visible;
                        break;
                    case "Control":
                        ControlSettingsSection.Visibility = Visibility.Visible;
                        break;
                    case "Window":
                        WindowSettingsSection.Visibility = Visibility.Visible;
                        break;
                    case "Record":
                        RecordSettingsSection.Visibility = Visibility.Visible;
                        break;
                    case "Other":
                        OtherSettingsSection.Visibility = Visibility.Visible;
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            LogService.WriteLog("ConfigPage", $"Error in NavigationItem_Click: {ex}");
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 确保 _config 不为 null
            if (_config == null)
            {
                _config = new DeviceConfig();
            }

            // Video Settings
            _config.Bitrate = (int)BitrateSlider.Value;
            _config.MaxSize = GetResolutionValue(ResolutionComboBox.SelectedIndex);
            _config.Fps = (int)FpsSlider.Value;
            _config.VideoCodec = VideoCodecComboBox.SelectedIndex switch
            {
                0 => "h264",
                1 => "h265",
                2 => "av1",
                _ => "h264"
            };
            _config.VideoSource = VideoSourceComboBox.SelectedIndex == 0 ? "display" : "camera";
            _config.Rotation = RotationComboBox.SelectedIndex switch
            {
                0 => 0,
                1 => 90,
                2 => 180,
                3 => 270,
                _ => 0
            };
            _config.DisplayBuffer = (int)DisplayBufferSlider.Value;
            _config.Crop = string.IsNullOrWhiteSpace(CropTextBox.Text) ? null : CropTextBox.Text.Trim();

            // Audio Settings
            _config.EnableAudio = EnableAudioToggle.IsOn;
            _config.AudioCodec = AudioCodecComboBox.SelectedIndex switch
            {
                0 => "opus",
                1 => "aac",
                2 => "flac",
                3 => "raw",
                _ => "opus"
            };
            _config.AudioSource = AudioSourceComboBox.SelectedIndex == 0 ? "output" : "playback";
            _config.AudioBuffer = (int)AudioBufferSlider.Value;

            // Control Settings
            _config.TurnScreenOff = TurnScreenOffToggle.IsOn;
            _config.StayAwake = StayAwakeToggle.IsOn;
            _config.ShowTouches = ShowTouchesToggle.IsOn;
            _config.DisableControl = DisableControlToggle.IsOn;
            _config.KeyboardMode = KeyboardModeComboBox.SelectedIndex switch
            {
                0 => "uhid",
                1 => "aoa",
                2 => "hid",
                _ => "uhid"
            };
            _config.MouseMode = MouseModeComboBox.SelectedIndex switch
            {
                0 => "uhid",
                1 => "aoa",
                2 => "hid",
                _ => "uhid"
            };

            // Window Settings
            _config.Fullscreen = FullscreenToggle.IsOn;
            _config.AlwaysOnTop = AlwaysOnTopToggle.IsOn;
            _config.Borderless = BorderlessToggle.IsOn;
            _config.LockAspectRatio = LockAspectRatioToggle.IsOn;
            _config.WindowTitle = string.IsNullOrWhiteSpace(WindowTitleTextBox.Text) ? null : WindowTitleTextBox.Text.Trim();
            _config.WindowX = double.IsNaN(WindowXNumberBox.Value) ? null : (int)WindowXNumberBox.Value;
            _config.WindowY = double.IsNaN(WindowYNumberBox.Value) ? null : (int)WindowYNumberBox.Value;
            _config.WindowWidth = double.IsNaN(WindowWidthNumberBox.Value) ? null : (int)WindowWidthNumberBox.Value;
            _config.WindowHeight = double.IsNaN(WindowHeightNumberBox.Value) ? null : (int)WindowHeightNumberBox.Value;

            // Recording Settings
            _config.EnableRecording = EnableRecordingToggle.IsOn;
            _config.RecordPath = string.IsNullOrWhiteSpace(RecordPathTextBox.Text) ? null : RecordPathTextBox.Text.Trim();
            _config.RecordFormat = RecordFormatComboBox.SelectedIndex == 0 ? "mp4" : "mkv";

            // Other Settings
            _config.DisableScreensaver = DisableScreensaverToggle.IsOn;
            _config.PowerOffOnClose = PowerOffOnCloseToggle.IsOn;

            // Save config
            if (_device != null && !string.IsNullOrEmpty(_device.Serial))
            {
                _configService.UpdateDeviceConfig(_device.Serial, _config);
            }
            else
            {
                _configService.SaveGlobalConfig(_config);
            }

            _ = ShowSaveSuccess();
        }
        catch (Exception ex)
        {
            LogService.WriteLog("ConfigPage", $"Error in SaveButton_Click: {ex}");
            _ = ShowSaveError();
        }
    }

    private int GetResolutionIndex(int maxSize)
    {
        return maxSize switch
        {
            0 => 0,
            2560 => 1,
            1920 => 2,
            1600 => 3,
            1280 => 4,
            854 => 5,
            640 => 6,
            _ => 0
        };
    }

    private int GetResolutionValue(int index)
    {
        return index switch
        {
            0 => 0,
            1 => 2560,
            2 => 1920,
            3 => 1600,
            4 => 1280,
            5 => 854,
            6 => 640,
            _ => 0
        };
    }

    private IAsyncOperation<ContentDialogResult> ShowSaveSuccess()
    {
        var dialog = new ContentDialog
        {
            Title = "保存成功",
            Content = "配置已保存。",
            CloseButtonText = "确定",
            XamlRoot = XamlRoot
        };

        return dialog.ShowAsync();
    }

    private IAsyncOperation<ContentDialogResult> ShowSaveError()
    {
        var dialog = new ContentDialog
        {
            Title = "保存失败",
            Content = "保存配置时出现错误，请稍后重试。",
            CloseButtonText = "确定",
            XamlRoot = XamlRoot
        };

        return dialog.ShowAsync();
    }
}
