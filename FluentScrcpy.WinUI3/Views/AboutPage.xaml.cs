using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Diagnostics;
using FluentScrcpy.WinUI3.Services;

namespace FluentScrcpy.WinUI3.Views;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        LogService.WriteLog("AboutPage", "Navigated to AboutPage");
    }

    #region 超链接点击事件处理

    private void ScrcpyLink_Click(object sender, RoutedEventArgs e)
    {
        OpenGitHubLink("https://github.com/Genymobile/scrcpy");
    }

    private void WinUILink_Click(object sender, RoutedEventArgs e)
    {
        OpenGitHubLink("https://github.com/microsoft/microsoft-ui-xaml");
    }

    private void CommunityToolkitLink_Click(object sender, RoutedEventArgs e)
    {
        OpenGitHubLink("https://github.com/CommunityToolkit/dotnet");
    }

    private void ColorCodeLink_Click(object sender, RoutedEventArgs e)
    {
        OpenGitHubLink("https://github.com/ChristianFindlay/ColorCode");
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 打开 GitHub 链接
    /// </summary>
    /// <param name="url">GitHub 仓库 URL</param>
    private void OpenGitHubLink(string url)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(psi);
            LogService.WriteLog("AboutPage", $"Opened GitHub link: {url}");
        }
        catch (Exception ex)
        {
            LogService.WriteLog("AboutPage", $"Error opening GitHub link: {ex.Message}");
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

    #endregion
}