using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ClashWinUI.Models;
using ClashWinUI.Services;

namespace ClashWinUI.Pages;

public sealed partial class ProxyPage : Page, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    // ── Bindable labels ───────────────────────────────────────────────────────

    public string PageTitle => Strings.Nav_Proxy;
    public string TestAllLabel => Strings.Proxy_TestAll;
    public string RefreshLabel => Strings.Common_Refresh;
    public string NotRunningMessage => Strings.Proxy_NotRunning;
    public string NoGroupsMessage => Strings.Proxy_NoGroups;

    // ── Mode ──────────────────────────────────────────────────────────────────

    // ── Loading / testing state ────────────────────────────────────────────────

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { if (_isLoading == value) return; _isLoading = value; OnPropertyChanged(); RefreshVisibility(); }
    }

    private bool _isTesting;
    public bool IsTesting
    {
        get => _isTesting;
        set { if (_isTesting == value) return; _isTesting = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotTesting)); }
    }
    public bool IsNotTesting => !_isTesting;

    private bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set
        {
            if (_isRefreshing == value) return;
            _isRefreshing = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotRefreshing));
        }
    }
    public bool IsNotRefreshing => !_isRefreshing;

    public bool IsNotRunning => !MihomoService.Instance.IsRunning;

    private string? _loadError;
    public string? LoadError
    {
        get => _loadError;
        private set { _loadError = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasLoadError)); }
    }
    public bool HasLoadError => !string.IsNullOrEmpty(_loadError);

    public Visibility LoadingVisibility => IsLoading ? Visibility.Visible : Visibility.Collapsed;
    public Visibility GroupsVisibility => !IsLoading && ProxyGroups.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility EmptyVisibility => !IsLoading && ProxyGroups.Count == 0 && MihomoService.Instance.IsRunning ? Visibility.Visible : Visibility.Collapsed;

    // ── Data ─────────────────────────────────────────────────────────────────

    public ObservableCollection<ProxyGroup> ProxyGroups { get; } = new();

    private readonly DispatcherQueue _dq;

    // ── Constructor ───────────────────────────────────────────────────────────

    public ProxyPage()
    {
        InitializeComponent();
        _dq = DispatcherQueue.GetForCurrentThread();
        Loaded += OnLoaded;
        MihomoService.Instance.RunningStateChanged += OnRunningStateChanged;
    }

    private async void OnLoaded(object _, RoutedEventArgs __)
    {
        if (MihomoService.Instance.IsRunning)
            await LoadProxiesAsync();
    }

    private void OnRunningStateChanged(object? _, EventArgs __)
    {
        _dq.TryEnqueue(async () =>
        {
            OnPropertyChanged(nameof(IsNotRunning));
            if (MihomoService.Instance.IsRunning)
                await LoadProxiesAsync();
            else
            {
                ProxyGroups.Clear();
                RefreshVisibility();
            }
        });
    }

    // ── Data loading ──────────────────────────────────────────────────────────

    private async Task LoadProxiesAsync()
    {
        IsLoading = true;
        IsRefreshing = true;
        LoadError = null;
        try
        {
            // Retry up to 3 times — the core may still be loading its config on first start.
            List<ProxyGroup> groups = new();
            Exception? lastEx = null;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                if (attempt > 0) await Task.Delay(1500);
                try
                {
                    groups = await MihomoService.Instance.GetProxyGroupsAsync();
                    if (groups.Count > 0) break;
                }
                catch (Exception ex) { lastEx = ex; }
            }

            if (groups.Count == 0 && lastEx != null)
                throw lastEx;

            if (groups.Count == 0)
                LoadError = Strings.Proxy_NoGroups + " — " + Strings.Subscription_NoCacheHint;

            ProxyGroups.Clear();
            foreach (var g in groups)
                ProxyGroups.Add(g);
        }
        catch (Exception ex)
        {
            LoadError = ex.Message;
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
        }
    }

    private void RefreshVisibility()
    {
        OnPropertyChanged(nameof(LoadingVisibility));
        OnPropertyChanged(nameof(GroupsVisibility));
        OnPropertyChanged(nameof(EmptyVisibility));
    }

    // ── Button handlers ───────────────────────────────────────────────────────

    private async void RefreshButton_Click(object _, RoutedEventArgs __)
    {
        if (MihomoService.Instance.IsRunning) await LoadProxiesAsync();
    }

    private async void TestAllButton_Click(object _, RoutedEventArgs __)
    {
        if (!MihomoService.Instance.IsRunning || IsTesting) return;
        IsTesting = true;
        try
        {
            var tasks = new List<Task>();
            foreach (var group in ProxyGroups) tasks.Add(TestGroupAsync(group));
            await Task.WhenAll(tasks);
        }
        finally { IsTesting = false; }
    }

    private async void GroupTest_Click(object sender, RoutedEventArgs _)
    {
        if ((sender as FrameworkElement)?.Tag is not ProxyGroup group) return;
        await TestGroupAsync(group);
    }

    private async Task TestGroupAsync(ProxyGroup group)
    {
        group.IsTesting = true;
        try
        {
            var tasks = new List<Task>();
            foreach (var node in group.Nodes)
            {
                var n = node;
                tasks.Add(Task.Run(async () =>
                {
                    _dq.TryEnqueue(() => n.IsTesting = true);
                    var d = await MihomoService.Instance.TestDelayAsync(n.Name);
                    _dq.TryEnqueue(() =>
                    {
                        n.Delay = d < 0 ? null : d;
                        n.IsTesting = false;
                    });
                }));
            }
            await Task.WhenAll(tasks);
        }
        finally { group.IsTesting = false; }
    }

    private async void Node_Click(object sender, RoutedEventArgs _)
    {
        if ((sender as FrameworkElement)?.Tag is not ProxyNodeView nv) return;
        if (!nv.Group.IsSelector) return;
        var ok = await MihomoService.Instance.SelectProxyAsync(nv.Group.Name, nv.Node.Name);
        if (ok) nv.Group.Now = nv.Node.Name;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
