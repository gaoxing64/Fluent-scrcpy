using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using FluentScrcpy.WinUI3.Views;
using System;
using System.IO;
using System.Runtime.InteropServices;
using FluentScrcpy.WinUI3.Services;

namespace FluentScrcpy.WinUI3;

public sealed partial class MainWindow : Window
{
    private readonly WindowSettingsService _windowSettingsService = WindowSettingsService.Instance;
    private AppWindow? _appWindow;

    // Public properties for NotificationService
    public Border NotificationBorderElement => NotificationBorder;
    public FontIcon NotificationIconElement => NotificationIcon;
    public TextBlock NotificationMessageTextBlockElement => NotificationMessageTextBlock;

    // Windows API for mouse buttons
    [DllImport("user32.dll")]
    private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTDEVICE
    {
        public ushort usUsagePage;
        public ushort usUsage;
        public uint dwFlags;
        public IntPtr hwndTarget;
    }

    private const ushort HID_USAGE_PAGE_GENERIC = 0x01;
    private const ushort HID_USAGE_GENERIC_MOUSE = 0x02;
    private const uint RIDEV_INPUTSINK = 0x00000100;

    // Windows API for window icon
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr LoadImage(IntPtr hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

    private const uint WM_SETICON = 0x0080;
    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x00000010;
    private const uint LR_DEFAULTSIZE = 0x00000040;
    private const int ICON_SMALL = 0;
    private const int ICON_BIG = 1;



    public MainWindow()
    {
        LogService.WriteLog("MainWindow", "Constructor started");

        try
        {
            InitializeComponent();
            LogService.WriteLog("MainWindow", "InitializeComponent completed");

            // Setup title bar and backdrop
            SetupTitleBar();
            LogService.WriteLog("MainWindow", "Title bar setup completed");

            // Apply saved theme
            ThemeService.InitializeTheme();
            LogService.WriteLog("MainWindow", "Theme initialized");

            // Apply saved backdrop
            BackdropService.InitializeBackdrop();
            LogService.WriteLog("MainWindow", "Backdrop initialized");

            // Apply saved language
            LanguageService.InitializeLanguage();
            LogService.WriteLog("MainWindow", "Language initialized");

            // Apply saved accent color
            AccentColorService.InitializeAccentColor();
            LogService.WriteLog("MainWindow", "Accent color initialized");

            // Setup navigation
            SetupNavigation();
            LogService.WriteLog("MainWindow", "Navigation setup completed");

            // Navigate to home page by default
            LogService.WriteLog("MainWindow", "Navigating to HomePage");
            ContentFrame.Navigate(typeof(HomePage));
            LogService.WriteLog("MainWindow", "HomePage navigation completed");

            // Select the first item (Home)
            if (NavView.MenuItems.Count > 0)
            {
                NavView.SelectedItem = NavView.MenuItems[0];
            }
            LogService.WriteLog("MainWindow", "Home item selected");

            // Store reference for theme changes
            App.MainWindow = this;
            LogService.WriteLog("MainWindow", "MainWindow reference stored");

            // Initialize notification service
            NotificationService.Initialize(this);
            LogService.WriteLog("MainWindow", "Notification service initialized");

            // Set window size and position after window is activated
            Activated += MainWindow_Activated;

            // Save window settings when closing
            Closed += MainWindow_Closed;

            // 订阅语言变化事件
            LanguageService.LanguageChanged += LanguageService_LanguageChanged;

            LogService.WriteLog("MainWindow", "Activated and Closed event handlers attached");
        }
        catch (Exception ex)
        {
            LogService.WriteLog("MainWindow", $"Error in constructor: {ex}");
            throw;
        }

        LogService.WriteLog("MainWindow", "Constructor completed");
    }

    private void LanguageService_LanguageChanged(object sender, EventArgs e)
    {
        // 重新导航到当前页面，以更新 UI 文本
        if (ContentFrame.CurrentSourcePageType != null)
        {
            ContentFrame.Navigate(ContentFrame.CurrentSourcePageType, ContentFrame.Content);
        }
    }

    private void SetupNavigation()
    {
        // Handle frame navigation events
        ContentFrame.Navigated += ContentFrame_Navigated;

        // 启用导航动画
        ContentFrame.NavigationFailed += ContentFrame_NavigationFailed;

        // Handle global key down for back navigation
        RootGrid.KeyDown += RootGrid_KeyDown;
    }

    private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        LogService.WriteLog("MainWindow", $"Navigation failed: {e.Exception.Message}");
    }

    private void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        // Alt + Left or Escape to go back
        if ((e.Key == Windows.System.VirtualKey.Left && IsAltKeyPressed()) ||
            e.Key == Windows.System.VirtualKey.Escape)
        {
            if (ContentFrame.CanGoBack)
            {
                ContentFrame.GoBack();
                e.Handled = true;
            }
        }
    }

    private bool IsAltKeyPressed()
    {
        var altState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu);
        return (altState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
    }

    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        // Update NavigationView selection based on current page
        UpdateNavigationViewSelection(e.SourcePageType);
    }

    private void UpdateNavigationViewSelection(Type pageType)
    {
        // Find the corresponding NavigationViewItem
        foreach (NavigationViewItem item in NavView.MenuItems)
        {
            var tag = item.Tag?.ToString();
            if (tag == "Home" && pageType == typeof(HomePage))
            {
                NavView.SelectedItem = item;
                return;
            }
            if (tag == "Config" && pageType == typeof(ConfigPage))
            {
                NavView.SelectedItem = item;
                return;
            }
        }

        foreach (NavigationViewItem item in NavView.FooterMenuItems)
        {
            var tag = item.Tag?.ToString();
            if (tag == "Settings" && pageType == typeof(SettingsPage))
            {
                NavView.SelectedItem = item;
                return;
            }
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem selectedItem) return;

        var tag = selectedItem.Tag?.ToString();
        LogService.WriteLog("MainWindow", $"NavigationView selection changed: {tag}");

        // 创建导航动画
        var navigationTransitionInfo = new EntranceNavigationTransitionInfo();

        // Navigate to corresponding page
        switch (tag)
        {
            case "Home":
                if (ContentFrame.CurrentSourcePageType != typeof(HomePage))
                {
                    ContentFrame.Navigate(typeof(HomePage), null, navigationTransitionInfo);
                }
                break;
            case "Config":
                if (ContentFrame.CurrentSourcePageType != typeof(ConfigPage))
                {
                    ContentFrame.Navigate(typeof(ConfigPage), null, navigationTransitionInfo);
                }
                break;
            case "Settings":
                if (ContentFrame.CurrentSourcePageType != typeof(SettingsPage))
                {
                    ContentFrame.Navigate(typeof(SettingsPage), null, navigationTransitionInfo);
                }
                break;
        }
    }

    private void SetupTitleBar()
    {
        try
        {
            // Get AppWindow
            _appWindow = GetAppWindow();
            if (_appWindow == null)
            {
                LogService.WriteLog("MainWindow", "GetAppWindow returned null");
                return;
            }

            // Set Acrylic backdrop (better performance when resizing than Mica)
            if (Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported())
            {
                var acrylicBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
                this.SystemBackdrop = acrylicBackdrop;
                LogService.WriteLog("MainWindow", "Acrylic backdrop applied");
            }
            else if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                var micaBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
                this.SystemBackdrop = micaBackdrop;
                LogService.WriteLog("MainWindow", "Mica backdrop applied");
            }
            else
            {
                LogService.WriteLog("MainWindow", "No backdrop supported, using default");
            }

            // Extend content into title bar
            _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;

            // Set title bar button colors to match theme
            _appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            _appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            _appWindow.TitleBar.ButtonForegroundColor = Colors.Transparent;
            _appWindow.TitleBar.ButtonInactiveForegroundColor = Colors.Transparent;
            _appWindow.TitleBar.ButtonHoverBackgroundColor = Colors.Transparent;
            _appWindow.TitleBar.ButtonHoverForegroundColor = Colors.Transparent;
            _appWindow.TitleBar.ButtonPressedBackgroundColor = Colors.Transparent;
            _appWindow.TitleBar.ButtonPressedForegroundColor = Colors.Transparent;

            // Set the XAML element as the title bar - this enables automatic drag handling
            this.SetTitleBar(AppTitleBar);

            LogService.WriteLog("MainWindow", "Title bar setup completed");
        }
        catch (Exception ex)
        {
            LogService.WriteLog("MainWindow", $"Error setting up title bar: {ex}");
        }
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        LogService.WriteLog("MainWindow", $"Activated event fired, state: {args.WindowActivationState}");

        // Only run once
        Activated -= MainWindow_Activated;

        // Set window icon
        SetWindowIcon();

        // Set window size and position
        SetWindowSizeAndPosition();
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        LogService.WriteLog("MainWindow", "Window closing, saving settings");

        if (_appWindow != null)
        {
            _windowSettingsService.SaveWindowSettings(_appWindow);
        }
    }

    private void SetWindowSizeAndPosition()
    {
        LogService.WriteLog("MainWindow", "Setting window size and position");

        try
        {
            if (_appWindow == null)
            {
                LogService.WriteLog("MainWindow", "AppWindow is null");
                return;
            }

            // Check if we have saved settings
            if (_windowSettingsService.HasSavedSettings)
            {
                LogService.WriteLog("MainWindow", "Applying saved window settings");
                _windowSettingsService.ApplyWindowSettings(_appWindow);
            }
            else
            {
                LogService.WriteLog("MainWindow", "No saved settings, using default 70% of screen");
                SetDefaultWindowSize();
            }
        }
        catch (Exception ex)
        {
            LogService.WriteLog("MainWindow", $"Error setting window position: {ex}");
        }
    }

    private void SetDefaultWindowSize()
    {
        try
        {
            if (_appWindow == null) return;

            // Get screen dimensions
            var displayArea = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary);
            var screenWidth = displayArea.WorkArea.Width;
            var screenHeight = displayArea.WorkArea.Height;

            LogService.WriteLog("MainWindow", $"Screen size: {screenWidth}x{screenHeight}");

            // First time: 70% of screen size
            int windowWidth = (int)(screenWidth * 0.7);
            int windowHeight = (int)(screenHeight * 0.7);

            // Ensure minimum size (800x600 is the window minimum)
            windowWidth = windowWidth < 1000 ? 1000 : windowWidth;
            windowHeight = windowHeight < 700 ? 700 : windowHeight;

            // Calculate centered position
            int x = (screenWidth - windowWidth) / 2;
            int y = (screenHeight - windowHeight) / 2;

            LogService.WriteLog("MainWindow", $"Setting default window size: {windowWidth}x{windowHeight}, position: ({x}, {y})");

            // Set window size and position
            _appWindow.Resize(new SizeInt32(windowWidth, windowHeight));
            _appWindow.Move(new PointInt32(x, y));

            LogService.WriteLog("MainWindow", "Default window size and position set successfully");
        }
        catch (Exception ex)
        {
            LogService.WriteLog("MainWindow", $"Error setting default window size: {ex}");
        }
    }

    private AppWindow? GetAppWindow()
    {
        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            return AppWindow.GetFromWindowId(windowId);
        }
        catch (Exception ex)
        {
            LogService.WriteLog("MainWindow", $"Error getting AppWindow: {ex}");
            return null;
        }
    }

    private void SetWindowIcon()
    {
        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            if (hwnd == IntPtr.Zero)
            {
                LogService.WriteLog("MainWindow", "Window handle is null");
                return;
            }

            // Get the path to the icon file
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "logo.ico");
            if (!File.Exists(iconPath))
            {
                LogService.WriteLog("MainWindow", $"Icon file not found: {iconPath}");
                return;
            }

            // Load the icon
            var hIcon = LoadImage(
                IntPtr.Zero,
                iconPath,
                IMAGE_ICON,
                0,
                0,
                LR_LOADFROMFILE | LR_DEFAULTSIZE);

            if (hIcon == IntPtr.Zero)
            {
                LogService.WriteLog("MainWindow", "Failed to load icon");
                return;
            }

            // Set the icon for the window (both small and big icons)
            SendMessage(hwnd, WM_SETICON, new IntPtr(ICON_SMALL), hIcon);
            SendMessage(hwnd, WM_SETICON, new IntPtr(ICON_BIG), hIcon);

            LogService.WriteLog("MainWindow", "Window icon set successfully");
        }
        catch (Exception ex)
        {
            LogService.WriteLog("MainWindow", $"Error setting window icon: {ex}");
        }
    }
}
