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
using Microsoft.UI.Xaml.Input;
using ClashWinUI.Models;
using ClashWinUI.Services;

namespace ClashWinUI.Pages;

public sealed partial class ProxyPage : Page, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    // ── Bindable labels ───────────────────────────────────────────────────────

    public string PageTitle => Strings.Nav_Proxy;
    public string ModeRuleLabel => Strings.Proxy_Mode_Rule;
    public string ModeGlobalLabel => Strings.Proxy_Mode_Global;
    public string ModeDirectLabel => Strings.Proxy_Mode_Direct;
    public string TestAllLabel => Strings.Proxy_TestAll;
    public string RefreshLabel => Strings.Common_Refresh;
    public string NotRunningMessage => Strings.Proxy_NotRunning;
    public string NoGroupsMessage => Strings.Proxy_NoGroups;

    // ── Mode ──────────────────────────────────────────────────────────────────

    private string _currentMode = "rule";
    public string CurrentMode
    {
        get => _currentMode;
        set
        {
            if (_currentMode == value) return;
            _currentMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsModeRule));
            OnPropertyChanged(nameof(IsModeGlobal));
            OnPropertyChanged(nameof(IsModeDirect));
        }
    }

    public bool IsModeRule => _currentMode == "rule";
    public bool IsModeGlobal => _currentMode == "global";
    public bool IsModeDirect => _currentMode == "direct";

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

    public bool IsNotRunning => !MihomoService.Instance.IsRunning;

    public Visibility LoadingVisibility => IsLoading ? Visibility.Visible : Visibility.Collapsed;
    public Visibility GroupsVisibility => !IsLoading && ProxyGroups.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility EmptyVisibility => !IsLoading && ProxyGroups.Count == 0 && MihomoService.Instance.IsRunning ? Visibility.Visible : Visibility.Collapsed;

    // ── Data ─────────────────────────────────────────────────────────────────

    public ObservableCollection<ProxyGroup> ProxyGroups { get; } = new();

    private readonly DispatcherQueue _dq;
    private readonly Dictionary<string, ProxyGroup> _nodeGroupMap = new();

    // ── Constructor ───────────────────────────────────────────────────────────

    public ProxyPage()
    {
        InitializeComponent();
        _dq = DispatcherQueue.GetForCurrentThread();
        Loaded += OnLoaded;
        MihomoService.Instance.RunningStateChanged += OnRunningStateChanged;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (MihomoService.Instance.IsRunning)
            await LoadProxiesAsync();
    }

    private void OnRunningStateChanged(object? sender, EventArgs e)
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
        try
        {
            var groups = await MihomoService.Instance.GetProxyGroupsAsync();
            ProxyGroups.Clear();
            _nodeGroupMap.Clear();
            foreach (var g in groups)
            {
                ProxyGroups.Add(g);
                foreach (var node in g.Nodes)
                    _nodeGroupMap[node.Name] = g;
            }

            var config = await MihomoService.Instance.GetConfigAsync();
            if (config != null) CurrentMode = config.Mode;
        }
        catch { /* core may still be starting */ }
        finally
        {
            IsLoading = false;
        }
    }

    private void RefreshVisibility()
    {
        OnPropertyChanged(nameof(LoadingVisibility));
        OnPropertyChanged(nameof(GroupsVisibility));
        OnPropertyChanged(nameof(EmptyVisibility));
    }

    // ── Button handlers ───────────────────────────────────────────────────────

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (MihomoService.Instance.IsRunning) await LoadProxiesAsync();
    }

    private async void TestAllButton_Click(object sender, RoutedEventArgs e)
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

    private async void GroupTest_Click(object sender, RoutedEventArgs e)
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

    private void GroupExpand_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not ProxyGroup group) return;
        group.IsExpanded = !group.IsExpanded;
    }

    private async void Node_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not ProxyNode node) return;
        if (!_nodeGroupMap.TryGetValue(node.Name, out var group) || !group.IsSelector) return;
        var ok = await MihomoService.Instance.SelectProxyAsync(group.Name, node.Name);
        if (ok) group.Now = node.Name;
    }

    private async void ModeRule_Checked(object sender, RoutedEventArgs e)
    {
        if (!MihomoService.Instance.IsRunning) return;
        await MihomoService.Instance.SetModeAsync("rule");
        _currentMode = "rule";
    }

    private async void ModeGlobal_Checked(object sender, RoutedEventArgs e)
    {
        if (!MihomoService.Instance.IsRunning) return;
        await MihomoService.Instance.SetModeAsync("global");
        _currentMode = "global";
    }

    private async void ModeDirect_Checked(object sender, RoutedEventArgs e)
    {
        if (!MihomoService.Instance.IsRunning) return;
        await MihomoService.Instance.SetModeAsync("direct");
        _currentMode = "direct";
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
