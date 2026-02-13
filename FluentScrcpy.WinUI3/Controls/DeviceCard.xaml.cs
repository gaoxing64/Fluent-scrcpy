using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Windows.Input;
using FluentScrcpy.WinUI3.Models;
using FluentScrcpy.WinUI3.Services;

namespace FluentScrcpy.WinUI3.Controls;

public sealed partial class DeviceCard : UserControl
{
    public static readonly DependencyProperty DeviceProperty =
        DependencyProperty.Register(nameof(Device), typeof(Device), typeof(DeviceCard), new PropertyMetadata(null, OnDeviceChanged));

    public static readonly DependencyProperty StartMirroringCommandProperty =
        DependencyProperty.Register(nameof(StartMirroringCommand), typeof(ICommand), typeof(DeviceCard), new PropertyMetadata(null));

    public static readonly DependencyProperty SwitchToWirelessCommandProperty =
        DependencyProperty.Register(nameof(SwitchToWirelessCommand), typeof(ICommand), typeof(DeviceCard), new PropertyMetadata(null));

    public static readonly DependencyProperty OpenConfigCommandProperty =
        DependencyProperty.Register(nameof(OpenConfigCommand), typeof(ICommand), typeof(DeviceCard), new PropertyMetadata(null));

    public DeviceCard()
    {
        InitializeComponent();
        this.DataContextChanged += DeviceCard_DataContextChanged;
        
        // Subscribe to window state changes
        ScrcpyService.Instance.OnWindowStateChanged += OnWindowStateChanged;
    }
    
    private void OnWindowStateChanged(string serial, ScrcpyService.WindowStateType stateType, bool isActive)
    {
        if (Device?.Serial == serial)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                switch (stateType)
                {
                    case ScrcpyService.WindowStateType.Fullscreen:
                        Device.IsFullscreen = isActive;
                        break;
                    case ScrcpyService.WindowStateType.AlwaysOnTop:
                        Device.IsAlwaysOnTop = isActive;
                        break;
                    case ScrcpyService.WindowStateType.Borderless:
                        Device.IsBorderless = isActive;
                        break;
                }
            });
        }
    }

    private void DeviceCard_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        // 当 DataContext 改变时，更新 UI 状态
        UpdateUIState();
    }

    private static void OnDeviceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DeviceCard card)
        {
            // 解绑旧设备的事件
            if (e.OldValue is Device oldDevice)
            {
                oldDevice.PropertyChanged -= card.Device_PropertyChanged;
            }

            // 绑定新设备的事件
            if (e.NewValue is Device newDevice)
            {
                newDevice.PropertyChanged += card.Device_PropertyChanged;
            }

            card.UpdateUIState();
        }
    }

    private void Device_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Device.IsMirroring))
        {
            DispatcherQueue.TryEnqueue(() => UpdateUIState());
        }
    }

    private void UpdateUIState()
    {
        if (Device == null) return;

        // 根据 IsMirroring 状态切换按钮可见性
        if (Device.IsMirroring)
        {
            StartButton.Visibility = Visibility.Collapsed;
            StopButton.Visibility = Visibility.Visible;
            QuickControlsGrid.Visibility = Visibility.Visible;
        }
        else
        {
            StartButton.Visibility = Visibility.Visible;
            StopButton.Visibility = Visibility.Collapsed;
            QuickControlsGrid.Visibility = Visibility.Collapsed;
        }
    }

    public Device Device
    {
        get => (Device)GetValue(DeviceProperty);
        set => SetValue(DeviceProperty, value);
    }

    public ICommand StartMirroringCommand
    {
        get => (ICommand)GetValue(StartMirroringCommandProperty);
        set => SetValue(StartMirroringCommandProperty, value);
    }

    public ICommand SwitchToWirelessCommand
    {
        get => (ICommand)GetValue(SwitchToWirelessCommandProperty);
        set => SetValue(SwitchToWirelessCommandProperty, value);
    }

    public ICommand OpenConfigCommand
    {
        get => (ICommand)GetValue(OpenConfigCommandProperty);
        set => SetValue(OpenConfigCommandProperty, value);
    }

    // ========== Main Actions ==========

    private void StopMirroringButton_Click(object sender, RoutedEventArgs e)
    {
        if (Device != null)
        {
            ScrcpyService.Instance.StopMirroring(Device.Serial);
        }
    }

    // ========== Quick Controls ==========

    private void FullscreenToggle_Checked(object sender, RoutedEventArgs e)
    {
        if (Device != null)
        {
            ScrcpyService.Instance.ToggleFullscreen(Device.Serial);
        }
    }

    private void FullscreenToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        if (Device != null)
        {
            ScrcpyService.Instance.ToggleFullscreen(Device.Serial);
        }
    }

    private void AlwaysOnTopToggle_Checked(object sender, RoutedEventArgs e)
    {
        if (Device != null)
        {
            ScrcpyService.Instance.ToggleAlwaysOnTop(Device.Serial);
        }
    }

    private void AlwaysOnTopToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        if (Device != null)
        {
            ScrcpyService.Instance.ToggleAlwaysOnTop(Device.Serial);
        }
    }

    private void BorderlessToggle_Checked(object sender, RoutedEventArgs e)
    {
        if (Device != null)
        {
            ScrcpyService.Instance.ToggleBorderless(Device.Serial);
        }
    }

    private void BorderlessToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        if (Device != null)
        {
            ScrcpyService.Instance.ToggleBorderless(Device.Serial);
        }
    }

    private void FocusWindowButton_Click(object sender, RoutedEventArgs e)
    {
        if (Device != null)
        {
            ScrcpyService.Instance.FocusWindow(Device.Serial);
        }
    }

    // ========== Device Control Buttons ==========

    private async void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        if (Device != null)
        {
            await AdbService.Instance.SendKeyEventAsync(Device.Serial, "3");
        }
    }

    private async void RecentsButton_Click(object sender, RoutedEventArgs e)
    {
        if (Device != null)
        {
            await AdbService.Instance.SendKeyEventAsync(Device.Serial, "187");
        }
    }

    private async void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Device != null)
        {
            await AdbService.Instance.SendKeyEventAsync(Device.Serial, "4");
        }
    }

    private async void MuteButton_Click(object sender, RoutedEventArgs e)
    {
        if (Device != null)
        {
            await AdbService.Instance.SendKeyEventAsync(Device.Serial, "164");
        }
    }

    private async void PowerButton_Click(object sender, RoutedEventArgs e)
    {
        if (Device != null)
        {
            await AdbService.Instance.SendKeyEventAsync(Device.Serial, "26");
        }
    }
}
