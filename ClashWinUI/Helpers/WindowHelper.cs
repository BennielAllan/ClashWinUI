using Microsoft.UI.Xaml;
using System.Collections.Generic;

namespace ClashWinUI.Helpers;

public static partial class WindowHelper
{
    private static readonly List<Window> _activeWindows = new();

    public static void TrackWindow(Window window)
    {
        window.Closed += (_, _) => _activeWindows.Remove(window);
        _activeWindows.Add(window);
    }

    public static Window? GetWindowForElement(UIElement element)
    {
        if (element.XamlRoot == null) return null;
        foreach (var window in _activeWindows)
        {
            if (window.Content?.XamlRoot == element.XamlRoot)
                return window;
        }
        return null;
    }

    public static void SetWindowMinSize(Window window, double width, double height)
    {
        if (window.Content is not FrameworkElement windowContent || windowContent.XamlRoot is null)
            return;
        if (window.AppWindow.Presenter is not Microsoft.UI.Windowing.OverlappedPresenter presenter)
            return;
        var scale = windowContent.XamlRoot.RasterizationScale;
        presenter.PreferredMinimumWidth = (int)(width * scale);
        presenter.PreferredMinimumHeight = (int)(height * scale);
    }

    public static IReadOnlyList<Window> ActiveWindows => _activeWindows;
}
