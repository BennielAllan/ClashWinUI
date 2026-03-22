using System;
using System.IO;
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
        AutoStartCoreAsync();
    }

    private static async void AutoStartCoreAsync()
    {
        if (MihomoService.Instance.IsRunning) return;

        await SubscriptionService.Instance.LoadAsync();

        string? configPath = null;
        foreach (var item in SubscriptionService.Instance.Items)
        {
            if (!item.IsRemote && !string.IsNullOrEmpty(item.UrlOrPath) &&
                File.Exists(item.UrlOrPath))
            {
                configPath = item.UrlOrPath;
                break;
            }
            if (item.IsRemote && !string.IsNullOrEmpty(item.CachedConfigPath) &&
                File.Exists(item.CachedConfigPath))
            {
                configPath = item.CachedConfigPath;
                break;
            }
        }

        if (configPath == null) return;

        try { await MihomoService.Instance.StartAsync(9090, string.Empty, configPath); }
        catch { /* 自动启动失败时静默忽略，用户可手动在订阅页启动 */ }
    }

    private async void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        if (MihomoService.Instance.IsRunning)
            await MihomoService.Instance.StopAsync();
    }
}
