using Microsoft.UI.Xaml;

namespace ClashWinUI.Helpers;

public static partial class ThemeHelper
{
    public static ElementTheme ActualTheme
    {
        get
        {
            foreach (var window in WindowHelper.ActiveWindows)
            {
                if (window.Content is FrameworkElement rootElement &&
                    rootElement.RequestedTheme != ElementTheme.Default)
                    return rootElement.RequestedTheme;
            }
            return EnumHelper.GetEnum<ElementTheme>(App.Current.RequestedTheme.ToString());
        }
    }

    public static ElementTheme RootTheme
    {
        get
        {
            foreach (var window in WindowHelper.ActiveWindows)
            {
                if (window.Content is FrameworkElement rootElement)
                    return rootElement.RequestedTheme;
            }
            return ElementTheme.Default;
        }
        set
        {
            foreach (var window in WindowHelper.ActiveWindows)
            {
                if (window.Content is FrameworkElement rootElement)
                    rootElement.RequestedTheme = value;
            }
            AppSettings.SelectedAppTheme = value;
        }
    }

    public static void Initialize()
    {
        RootTheme = AppSettings.SelectedAppTheme;
    }

    public static bool IsDarkTheme()
    {
        if (RootTheme == ElementTheme.Default)
            return Application.Current.RequestedTheme == ApplicationTheme.Dark;
        return RootTheme == ElementTheme.Dark;
    }
}
