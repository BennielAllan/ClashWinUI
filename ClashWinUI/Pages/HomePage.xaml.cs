using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using ClashWinUI.Helpers;
using ClashWinUI.Services;

namespace ClashWinUI.Pages;

public sealed partial class HomePage : Page, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    // ── Brushes ───────────────────────────────────────────────────────────────

    private readonly SolidColorBrush _greenBrush = new(Windows.UI.Color.FromArgb(0xFF, 0x4C, 0xAF, 0x50));
    private readonly SolidColorBrush _greyBrush  = new(Windows.UI.Color.FromArgb(0xFF, 0x9E, 0x9E, 0x9E));

    // ── Bindable labels ───────────────────────────────────────────────────────

    public string PageTitle             => Strings.Nav_Home;
    public string ActiveSubscriptionLabel => Strings.Home_ActiveSubscription;
    public string NetworkModeLabel      => Strings.Home_NetworkMode;
    public string SystemProxyLabel      => Strings.Home_SystemProxy;
    public string SystemProxyDescLabel  => Strings.Home_SystemProxy_Description;
    public string TunModeLabel          => Strings.Home_TunMode;
    public string TunModeDescLabel      => Strings.Home_TunMode_Description;
    public string ProxyModeLabel        => Strings.Home_ProxyMode;
    public string CurrentNodeLabel      => Strings.Home_CurrentNode;
    public string ModeRuleLabel         => Strings.Proxy_Mode_Rule;
    public string ModeGlobalLabel       => Strings.Proxy_Mode_Global;
    public string ModeDirectLabel       => Strings.Proxy_Mode_Direct;

    // ── Core state ────────────────────────────────────────────────────────────

    public bool IsRunning => MihomoService.Instance.IsRunning;

    public SolidColorBrush CoreDotBrush  => IsRunning ? _greenBrush : _greyBrush;
    public string CoreStatusText         => IsRunning ? Strings.Core_Running : Strings.Core_Stopped;
    public string CoreButtonLabel        => IsRunning ? Strings.Core_Stop : Strings.Core_Start;

    private bool _isBusy;
    public bool IsBusy           => _isBusy;
    public bool IsNotBusy        => !_isBusy;
    public Visibility BusyVisibility => _isBusy ? Visibility.Visible : Visibility.Collapsed;

    // ── Start error ───────────────────────────────────────────────────────────

    private string _startErrorMessage = string.Empty;
    public string StartErrorMessage
    {
        get => _startErrorMessage;
        private set { _startErrorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasStartError)); }
    }
    public bool HasStartError => !string.IsNullOrEmpty(_startErrorMessage);

    // ── Subscription ──────────────────────────────────────────────────────────

    private string _subscriptionName = "—";
    public string SubscriptionName
    {
        get => _subscriptionName;
        private set { _subscriptionName = value; OnPropertyChanged(); }
    }

    // ── Network mode ──────────────────────────────────────────────────────────

    private bool _isSystemProxyEnabled;
    public bool IsSystemProxyEnabled
    {
        get => _isSystemProxyEnabled;
        private set
        {
            if (_isSystemProxyEnabled == value) return;
            _isSystemProxyEnabled = value;
            OnPropertyChanged();
        }
    }

    private bool _isTunEnabled;
    public bool IsTunEnabled
    {
        get => _isTunEnabled;
        private set
        {
            if (_isTunEnabled == value) return;
            _isTunEnabled = value;
            OnPropertyChanged();
        }
    }

    // ── Proxy mode ────────────────────────────────────────────────────────────

    private string _currentMode = "rule";
    public bool IsModeRule   => _currentMode == "rule";
    public bool IsModeGlobal => _currentMode == "global";
    public bool IsModeDirect => _currentMode == "direct";

    private void SetMode(string mode)
    {
        _currentMode = mode;
        OnPropertyChanged(nameof(IsModeRule));
        OnPropertyChanged(nameof(IsModeGlobal));
        OnPropertyChanged(nameof(IsModeDirect));
    }

    // ── Current node ──────────────────────────────────────────────────────────

    private string _currentNodeName = "—";
    public string CurrentNodeName
    {
        get => _currentNodeName;
        private set { _currentNodeName = value; OnPropertyChanged(); }
    }

    // ── Proxy port (cached for system-proxy enable) ───────────────────────────

    private int _proxyPort = 7890;

    // ── Infrastructure ────────────────────────────────────────────────────────

    private readonly DispatcherQueue _dq;

    public HomePage()
    {
        InitializeComponent();
        _dq = DispatcherQueue.GetForCurrentThread();
        Loaded += OnLoaded;
        MihomoService.Instance.RunningStateChanged += OnRunningStateChanged;
    }

    private async void OnLoaded(object _, RoutedEventArgs __)
    {
        await SubscriptionService.Instance.LoadAsync();
        RefreshSubscriptionInfo();
        IsSystemProxyEnabled = await Task.Run(SystemProxyHelper.IsEnabled);
        if (IsRunning)
            await RefreshCoreInfoAsync();
    }

    private void OnRunningStateChanged(object? _, EventArgs __)
    {
        _dq.TryEnqueue(async () =>
        {
            NotifyCoreState();
            if (IsRunning)
                await RefreshCoreInfoAsync();
            else
            {
                IsTunEnabled = false;
                CurrentNodeName = "—";
            }
        });
    }

    private void NotifyCoreState()
    {
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(CoreDotBrush));
        OnPropertyChanged(nameof(CoreStatusText));
        OnPropertyChanged(nameof(CoreButtonLabel));
    }

    // ── Data refresh ──────────────────────────────────────────────────────────

    private void RefreshSubscriptionInfo()
    {
        foreach (var item in SubscriptionService.Instance.Items)
        {
            bool hasConfig =
                (!item.IsRemote && !string.IsNullOrEmpty(item.UrlOrPath) && File.Exists(item.UrlOrPath))
                || (item.IsRemote && !string.IsNullOrEmpty(item.CachedConfigPath) && File.Exists(item.CachedConfigPath));
            if (hasConfig)
            {
                SubscriptionName = item.Name;
                return;
            }
        }
        SubscriptionName = Strings.Home_NoSubscription;
    }

    private async Task RefreshCoreInfoAsync()
    {
        try
        {
            var config = await MihomoService.Instance.GetConfigAsync();
            if (config != null)
            {
                SetMode(config.Mode);
                IsTunEnabled = config.Tun?.Enable ?? false;
                _proxyPort = config.MixedPort > 0 ? config.MixedPort
                           : config.Port > 0 ? config.Port
                           : 7890;
            }

            var (_, node) = await MihomoService.Instance.GetCurrentProxyAsync();
            CurrentNodeName = string.IsNullOrEmpty(node) ? "—" : node;
        }
        catch { }
    }

    // ── Button handlers ───────────────────────────────────────────────────────

    private async void CoreToggle_Click(object _, RoutedEventArgs __)
    {
        if (_isBusy) return;
        SetBusy(true);
        StartErrorMessage = string.Empty;
        try
        {
            if (IsRunning)
            {
                await MihomoService.Instance.StopAsync();
            }
            else
            {
                await SubscriptionService.Instance.LoadAsync();
                string? configPath = null;
                foreach (var item in SubscriptionService.Instance.Items)
                {
                    if (!item.IsRemote && !string.IsNullOrEmpty(item.UrlOrPath) && File.Exists(item.UrlOrPath))
                    { configPath = item.UrlOrPath; break; }
                    if (item.IsRemote && !string.IsNullOrEmpty(item.CachedConfigPath) && File.Exists(item.CachedConfigPath))
                    { configPath = item.CachedConfigPath; break; }
                }
                if (configPath == null)
                {
                    StartErrorMessage = Strings.Subscription_NoCacheHint;
                    return;
                }
                await MihomoService.Instance.StartAsync(9090, string.Empty, configPath);
            }
        }
        catch (Exception ex)
        {
            StartErrorMessage = ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _isBusy = busy;
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(BusyVisibility));
    }

    private async void SystemProxy_Toggled(object sender, RoutedEventArgs _)
    {
        var toggle = (ToggleSwitch)sender;
        bool desired = toggle.IsOn;
        if (desired == _isSystemProxyEnabled) return;
        if (desired)
        {
            await Task.Run(() => SystemProxyHelper.Enable(_proxyPort));
            _isSystemProxyEnabled = true;
        }
        else
        {
            await Task.Run(() => SystemProxyHelper.Disable());
            _isSystemProxyEnabled = false;
        }
    }

    private async void TunMode_Toggled(object sender, RoutedEventArgs _)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn == _isTunEnabled) return;
        if (!IsRunning) { IsTunEnabled = false; return; }
        var desired = toggle.IsOn;
        var (ok, error) = await MihomoService.Instance.SetTunAsync(desired);
        if (ok)
            _isTunEnabled = desired;
        else
        {
            IsTunEnabled = !desired;
            StartErrorMessage = string.IsNullOrEmpty(error) ? "TUN failed" : error;
        }
    }

    private async void ModeRule_Checked(object _, RoutedEventArgs __)
    {
        if (!IsRunning) return;
        await MihomoService.Instance.SetModeAsync("rule");
        _currentMode = "rule";
    }

    private async void ModeGlobal_Checked(object _, RoutedEventArgs __)
    {
        if (!IsRunning) return;
        await MihomoService.Instance.SetModeAsync("global");
        _currentMode = "global";
    }

    private async void ModeDirect_Checked(object _, RoutedEventArgs __)
    {
        if (!IsRunning) return;
        await MihomoService.Instance.SetModeAsync("direct");
        _currentMode = "direct";
    }

    private void SubscriptionCard_Click(object _, RoutedEventArgs __) =>
        NavigateTo("Subscription");

    private void NodeCard_Click(object _, RoutedEventArgs __) =>
        NavigateTo("Proxy");

    private void NavigateTo(string tag)
    {
        var win = WindowHelper.GetWindowForElement(this) as MainWindow;
        if (win == null) return;
        var nav = win.NavigationView;
        foreach (var obj in nav.MenuItems)
        {
            if (obj is NavigationViewItem item && item.Tag as string == tag)
            {
                nav.SelectedItem = item;
                break;
            }
        }
    }

    private void ErrorBar_Closed(InfoBar _, InfoBarClosedEventArgs __) =>
        StartErrorMessage = string.Empty;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
