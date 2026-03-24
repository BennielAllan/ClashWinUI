using System;
using Microsoft.UI.Xaml;
using Windows.Storage;

namespace ClashWinUI.Helpers;

/// <summary>
/// Simple persisted app settings (theme, language) using LocalSettings.
/// </summary>
public static class AppSettings
{
    private const string KeyTheme = "SelectedAppTheme";
    private const string KeyLanguage = "Language";

    public static ElementTheme SelectedAppTheme
    {
        get
        {
            try
            {
                var v = ApplicationData.Current.LocalSettings.Values[KeyTheme];
                if (v is int i && i >= 0 && i <= 2)
                    return (ElementTheme)i;
            }
            catch { }
            return ElementTheme.Default;
        }
        set
        {
            try
            {
                ApplicationData.Current.LocalSettings.Values[KeyTheme] = (int)value;
            }
            catch { }
        }
    }

    /// <summary>
    /// Language code: "zh" = 中文, "en" = English.
    /// </summary>
    public static string Language
    {
        get
        {
            try
            {
                if (ApplicationData.Current.LocalSettings.Values[KeyLanguage] is string s)
                    return s;
            }
            catch { }
            return "en";
        }
        set
        {
            try
            {
                var newValue = value ?? "en";
                if (ApplicationData.Current.LocalSettings.Values[KeyLanguage] as string == newValue)
                    return;
                ApplicationData.Current.LocalSettings.Values[KeyLanguage] = newValue;
                LanguageChanged?.Invoke();
            }
            catch { }
        }
    }

    /// <summary>
    /// Raised when the user changes the display language. Subscribe to refresh UI.
    /// </summary>
    public static event Action? LanguageChanged;
}
