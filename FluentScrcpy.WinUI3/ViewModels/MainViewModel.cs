using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using System;

namespace FluentScrcpy.WinUI3.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Fluent Scrcpy";

    [ObservableProperty]
    private bool _isDarkTheme;

    public MainViewModel()
    {
        // Check current theme
        var currentTheme = Application.Current.RequestedTheme;
        IsDarkTheme = currentTheme == ApplicationTheme.Dark;
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        
        // Apply theme
        var elementTheme = IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        if (App.MainWindow?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = elementTheme;
        }
    }
}
