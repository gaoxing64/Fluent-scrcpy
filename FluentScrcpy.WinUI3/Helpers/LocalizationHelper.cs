using System;
using Windows.ApplicationModel.Resources;

namespace FluentScrcpy.WinUI3.Helpers;

public static class LocalizationHelper
{
    private static readonly ResourceLoader _resourceLoader = ResourceLoader.GetForViewIndependentUse();

    public static string GetString(string key)
    {
        return _resourceLoader.GetString(key);
    }
}
