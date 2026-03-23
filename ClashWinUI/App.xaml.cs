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
        MainWindow.Closed += OnMainWindowClosed;
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private static void BringToForeground(Window window)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        SetForegroundWindow(hwnd);
    }

    private async void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        if (MihomoService.Instance.IsRunning)
            await MihomoService.Instance.StopAsync();
    }
}
