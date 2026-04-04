using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using ClashWinUI.Helpers;
using ClashWinUI.Services;

namespace ClashWinUI;

public partial class App : Application
{
    internal static MainWindow? MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        ThemeHelper.Initialize();
        MainWindow = new MainWindow();
        Helpers.WindowHelper.TrackWindow(MainWindow);
        MainWindow.Activate();
        BringToForeground(MainWindow);

        // Initialize system tray
        TrayIconService.Instance.Initialize();

        // Intercept close → minimize to tray
        MainWindow.AppWindow.Closing += OnAppWindowClosing;
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private static void BringToForeground(Window window)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        SetForegroundWindow(hwnd);
    }

    private void OnAppWindowClosing(
        Microsoft.UI.Windowing.AppWindow sender,
        Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        args.Cancel = true;
        TrayIconService.Instance.HideMainWindow();
    }
}
