using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using FluentScrcpy.WinUI3.Services;
using System;
using System.Threading.Tasks;

namespace FluentScrcpy.WinUI3.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        LogService.WriteLog("SettingsPage", "Constructor started");
        try
        {
            InitializeComponent();
            LogService.WriteLog("SettingsPage", "InitializeComponent completed");
            NavigationCacheMode = NavigationCacheMode.Enabled;
            LogService.WriteLog("SettingsPage", "NavigationCacheMode set");
            
            // 订阅语言变化事件
            LanguageService.LanguageChanged += LanguageService_LanguageChanged;
        }
        catch (Exception ex)
        {
            LogService.WriteLog("SettingsPage", $"Error in constructor: {ex}");
            throw;
        }
        LogService.WriteLog("SettingsPage", "Constructor completed");
    }

    private void LanguageService_LanguageChanged(object sender, EventArgs e)
    {
        // 重新加载语言设置显示
        LoadCurrentSettings();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        LogService.WriteLog("SettingsPage", "OnNavigatedTo started");
        base.OnNavigatedTo(e);
        LoadCurrentSettings();
        LogService.WriteLog("SettingsPage", "OnNavigatedTo completed");
    }

    private void LoadCurrentSettings()
    {
        LogService.WriteLog("SettingsPage", "LoadCurrentSettings started");
        try
        {
            // Load Theme
            var currentTheme = ThemeService.GetCurrentTheme();
            ThemeSystemRadio.IsChecked = currentTheme == ElementTheme.Default;
            ThemeLightRadio.IsChecked = currentTheme == ElementTheme.Light;
            ThemeDarkRadio.IsChecked = currentTheme == ElementTheme.Dark;
            UpdateThemeDisplayText();
            LogService.WriteLog("SettingsPage", "Theme settings loaded");

            // Load Backdrop
            var currentBackdrop = BackdropService.GetCurrentBackdrop();
            BackdropMicaRadio.IsChecked = currentBackdrop == BackdropService.BackdropType.Mica;
            BackdropMicaAltRadio.IsChecked = currentBackdrop == BackdropService.BackdropType.MicaAlt;
            BackdropAcrylicRadio.IsChecked = currentBackdrop == BackdropService.BackdropType.Acrylic;
            UpdateBackdropDisplayText();
            LogService.WriteLog("SettingsPage", "Backdrop settings loaded");

            // Load Language
            var currentLanguage = LanguageService.GetCurrentLanguage();
            LangSystemRadio.IsChecked = currentLanguage == "System";
            LangChineseRadio.IsChecked = currentLanguage == "zh-CN";
            LangEnglishRadio.IsChecked = currentLanguage == "en-US";
            UpdateLanguageDisplayText();
            LogService.WriteLog("SettingsPage", "Language settings loaded");

            // Load Accent Color
            UseSystemAccentToggle.IsOn = AccentColorService.UseSystemAccent();
            PickAccentColorButton.IsEnabled = !AccentColorService.UseSystemAccent();
            UpdateAccentColorDisplayText();
            LogService.WriteLog("SettingsPage", "Accent color settings loaded");
        }
        catch (Exception ex)
        {
            LogService.WriteLog("SettingsPage", $"Error in LoadCurrentSettings: {ex}");
            throw;
        }
        LogService.WriteLog("SettingsPage", "LoadCurrentSettings completed successfully");
    }

    #region Expand/Collapse with Animation
    private void LanguageHeader_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        TogglePanelWithAnimation(LanguageOptionsPanel, LanguageArrowRotate);
    }

    private void ThemeHeader_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        TogglePanelWithAnimation(ThemeOptionsPanel, ThemeArrowRotate);
    }

    private void BackdropHeader_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        TogglePanelWithAnimation(BackdropOptionsPanel, BackdropArrowRotate);
    }

    private void AccentHeader_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        TogglePanelWithAnimation(AccentOptionsPanel, AccentArrowRotate);
    }

    private void TogglePanelWithAnimation(StackPanel panel, RotateTransform arrowRotate)
    {
        if (panel.Visibility == Visibility.Collapsed)
        {
            // Expand
            panel.Visibility = Visibility.Visible;
            panel.Opacity = 0;

            // Animate arrow rotation (0 to 180)
            AnimateArrowRotation(arrowRotate, 0, 180);

            // Animate opacity
            AnimateOpacity(panel, 0, 1);
        }
        else
        {
            // Collapse
            // Animate arrow rotation (180 to 0)
            AnimateArrowRotation(arrowRotate, 180, 0);

            // Animate opacity
            AnimateOpacity(panel, 1, 0, () => {
                panel.Visibility = Visibility.Collapsed;
            });
        }
    }

    private void AnimateArrowRotation(RotateTransform rotateTransform, double from, double to)
    {
        var rotateAnimation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };

        var rotateStoryboard = new Storyboard();
        Storyboard.SetTarget(rotateAnimation, rotateTransform);
        Storyboard.SetTargetProperty(rotateAnimation, "Angle");
        rotateStoryboard.Children.Add(rotateAnimation);
        rotateStoryboard.Begin();
    }

    private void AnimateOpacity(UIElement element, double from, double to, Action? completedAction = null)
    {
        var opacityAnimation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };

        if (completedAction != null)
        {
            opacityAnimation.Completed += (sender, e) => completedAction();
        }

        var opacityStoryboard = new Storyboard();
        Storyboard.SetTarget(opacityAnimation, element);
        Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
        opacityStoryboard.Children.Add(opacityAnimation);
        opacityStoryboard.Begin();
    }
    #endregion

    #region Theme
    private void ThemeRadio_Checked(object sender, RoutedEventArgs e)
    {
        LogService.WriteLog("SettingsPage", "ThemeRadio_Checked called");
        try
        {
            if (ThemeSystemRadio.IsChecked == true)
            {
                ThemeService.SetTheme(ElementTheme.Default);
            }
            else if (ThemeLightRadio.IsChecked == true)
            {
                ThemeService.SetTheme(ElementTheme.Light);
            }
            else if (ThemeDarkRadio.IsChecked == true)
            {
                ThemeService.SetTheme(ElementTheme.Dark);
            }
            UpdateThemeDisplayText();
            LogService.WriteLog("SettingsPage", "Theme changed");
        }
        catch (Exception ex)
        {
            LogService.WriteLog("SettingsPage", $"Error in ThemeRadio_Checked: {ex}");
        }
    }

    private void UpdateThemeDisplayText()
    {
        if (ThemeSystemRadio.IsChecked == true)
            ThemeValueText.Text = "使用系统设置";
        else if (ThemeLightRadio.IsChecked == true)
            ThemeValueText.Text = "浅色";
        else if (ThemeDarkRadio.IsChecked == true)
            ThemeValueText.Text = "深色";
    }
    #endregion

    #region Backdrop
    private void BackdropRadio_Checked(object sender, RoutedEventArgs e)
    {
        LogService.WriteLog("SettingsPage", "BackdropRadio_Checked called");
        try
        {
            if (BackdropMicaRadio.IsChecked == true)
            {
                BackdropService.SetBackdrop(BackdropService.BackdropType.Mica);
            }
            else if (BackdropMicaAltRadio.IsChecked == true)
            {
                BackdropService.SetBackdrop(BackdropService.BackdropType.MicaAlt);
            }
            else if (BackdropAcrylicRadio.IsChecked == true)
            {
                BackdropService.SetBackdrop(BackdropService.BackdropType.Acrylic);
            }
            UpdateBackdropDisplayText();
            LogService.WriteLog("SettingsPage", "Backdrop changed");
        }
        catch (Exception ex)
        {
            LogService.WriteLog("SettingsPage", $"Error in BackdropRadio_Checked: {ex}");
        }
    }

    private void UpdateBackdropDisplayText()
    {
        if (BackdropMicaRadio.IsChecked == true)
            BackdropValueText.Text = "云母 (Mica)";
        else if (BackdropMicaAltRadio.IsChecked == true)
            BackdropValueText.Text = "云母变体 (Mica Alt)";
        else if (BackdropAcrylicRadio.IsChecked == true)
            BackdropValueText.Text = "亚克力 (Acrylic)";
    }
    #endregion

    #region Language
    private void LanguageRadio_Checked(object sender, RoutedEventArgs e)
    {
        LogService.WriteLog("SettingsPage", "LanguageRadio_Checked called");
        try
        {
            if (LangSystemRadio.IsChecked == true)
            {
                LanguageService.SetLanguage("System");
            }
            else if (LangChineseRadio.IsChecked == true)
            {
                LanguageService.SetLanguage("zh-CN");
            }
            else if (LangEnglishRadio.IsChecked == true)
            {
                LanguageService.SetLanguage("en-US");
            }
            UpdateLanguageDisplayText();
            LogService.WriteLog("SettingsPage", "Language changed");
        }
        catch (Exception ex)
        {
            LogService.WriteLog("SettingsPage", $"Error in LanguageRadio_Checked: {ex}");
        }
    }

    private void UpdateLanguageDisplayText()
    {
        if (LangSystemRadio.IsChecked == true)
            LanguageValueText.Text = "使用系统设置";
        else if (LangChineseRadio.IsChecked == true)
            LanguageValueText.Text = "简体中文";
        else if (LangEnglishRadio.IsChecked == true)
            LanguageValueText.Text = "English";
    }
    #endregion

    #region Accent Color
    private void UseSystemAccentToggle_Toggled(object sender, RoutedEventArgs e)
    {
        LogService.WriteLog("SettingsPage", "UseSystemAccentToggle_Toggled called");
        try
        {
            AccentColorService.SetUseSystemAccent(UseSystemAccentToggle.IsOn);
            PickAccentColorButton.IsEnabled = !UseSystemAccentToggle.IsOn;
            UpdateAccentColorDisplayText();
            LogService.WriteLog("SettingsPage", $"UseSystemAccent changed to: {UseSystemAccentToggle.IsOn}");
        }
        catch (Exception ex)
        {
            LogService.WriteLog("SettingsPage", $"Error in UseSystemAccentToggle_Toggled: {ex}");
        }
    }

    private async void PickAccentColorButton_Click(object sender, RoutedEventArgs e)
    {
        LogService.WriteLog("SettingsPage", "PickAccentColorButton_Click called");
        try
        {
            var colorPicker = new ColorPicker
            {
                Color = AccentColorService.GetAccentColor()
            };

            var dialog = new ContentDialog
            {
                Title = "选择强调色",
                Content = colorPicker,
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                AccentColorService.SetAccentColor(colorPicker.Color);
                LogService.WriteLog("SettingsPage", $"Accent color changed to: {colorPicker.Color}");
            }
        }
        catch (Exception ex)
        {
            LogService.WriteLog("SettingsPage", $"Error in PickAccentColorButton_Click: {ex}");
        }
    }

    private void UpdateAccentColorDisplayText()
    {
        if (UseSystemAccentToggle.IsOn)
            AccentColorValueText.Text = "使用系统强调色";
        else
            AccentColorValueText.Text = "自定义颜色";
    }
    #endregion

    #region GitHub 链接处理
    private void GitHubLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is HyperlinkButton linkButton && linkButton.Tag is string url)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
                LogService.WriteLog("SettingsPage", $"Opened GitHub link: {url}");
            }
            catch (Exception ex)
            {
                LogService.WriteLog("SettingsPage", $"Error opening GitHub link: {ex.Message}");
                // 显示错误提示
                var dialog = new ContentDialog
                {
                    Title = "打开链接失败",
                    Content = "无法打开 GitHub 链接，请检查网络连接后重试。",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };
                _ = dialog.ShowAsync();
            }
        }
    }
    #endregion
}
