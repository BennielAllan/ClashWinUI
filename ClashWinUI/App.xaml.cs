using System;
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
        MainWindow.Closed += OnMainWindowClosed;
    }

    private async void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        // Ensure the mihomo core process is stopped when the app exits.
        if (MihomoService.Instance.IsRunning)
            await MihomoService.Instance.StopAsync();
    }
}
