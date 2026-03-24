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
using ClashWinUI.Helpers;
using ClashWinUI.Models;
using ClashWinUI.Services;

namespace ClashWinUI.Pages;

public sealed partial class ConnectionsPage : Page, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    // ── Labels ────────────────────────────────────────────────────────────────

    public string PageTitle => Strings.Nav_Connections;
    public string TotalDownLabel => Strings.Connections_Total_Down;
    public string TotalUpLabel => Strings.Connections_Total_Up;
    public string CloseAllLabel => Strings.Connections_CloseAll;
    public string SearchPlaceholder => Strings.Connections_Search;
    public string NotRunningMessage => Strings.Connections_NotRunning;
    public string EmptyMessage => Strings.Connections_Empty;

    // ── State ─────────────────────────────────────────────────────────────────

    private long _totalDown;
    private long _totalUp;

    public string TotalDownDisplay => SubscriptionDisplayHelper.FormatBytes(_totalDown);
    public string TotalUpDisplay => SubscriptionDisplayHelper.FormatBytes(_totalUp);

    public bool IsNotRunning => !MihomoService.Instance.IsRunning;
    public bool HasConnections => FilteredConnections.Count > 0;
    public Visibility ListVisibility => MihomoService.Instance.IsRunning ? Visibility.Visible : Visibility.Collapsed;
    public Visibility EmptyVisibility => (MihomoService.Instance.IsRunning && FilteredConnections.Count == 0) ? Visibility.Visible : Visibility.Collapsed;

    // ── Data ─────────────────────────────────────────────────────────────────

    // All active connections by id.
    private readonly Dictionary<string, ConnectionItem> _allById = new();
    // Previous download/upload totals per connection for rate calc.
    private readonly Dictionary<string, (long up, long down)> _prevTotals = new();

    public ObservableCollection<ConnectionItem> FilteredConnections { get; } = new();

    private string _searchText = string.Empty;
    private readonly DispatcherQueue _dq;
    private CancellationTokenSource? _cts;

    // ── Constructor ───────────────────────────────────────────────────────────

    public ConnectionsPage()
    {
        InitializeComponent();
        _dq = DispatcherQueue.GetForCurrentThread();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        MihomoService.Instance.RunningStateChanged += OnRunningStateChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (MihomoService.Instance.IsRunning)
            StartStreaming();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        StopStreaming();
        MihomoService.Instance.RunningStateChanged -= OnRunningStateChanged;
    }

    private void OnRunningStateChanged(object? sender, EventArgs e)
    {
        _dq.TryEnqueue(() =>
        {
            OnPropertyChanged(nameof(IsNotRunning));
            OnPropertyChanged(nameof(ListVisibility));
            if (MihomoService.Instance.IsRunning)
                StartStreaming();
            else
            {
                StopStreaming();
                ClearConnections();
            }
        });
    }

    // ── Streaming ─────────────────────────────────────────────────────────────

    private void StartStreaming()
    {
        StopStreaming();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _ = Task.Run(() => MihomoService.Instance.StreamConnectionsAsync(OnSnapshot, token));
    }

    private void StopStreaming()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private void OnSnapshot(ConnectionsResponse snapshot)
    {
        _dq.TryEnqueue(() =>
        {
            _totalDown = snapshot.DownloadTotal;
            _totalUp = snapshot.UploadTotal;
            OnPropertyChanged(nameof(TotalDownDisplay));
            OnPropertyChanged(nameof(TotalUpDisplay));

            // Compute rates & update or add items.
            var incoming = snapshot.Connections ?? new();
            var newIds = new HashSet<string>();
            foreach (var conn in incoming)
            {
                newIds.Add(conn.Id);
                if (_prevTotals.TryGetValue(conn.Id, out var prev))
                {
                    conn.UploadRate = Math.Max(0, conn.Upload - prev.up);
                    conn.DownloadRate = Math.Max(0, conn.Download - prev.down);
                }
                _prevTotals[conn.Id] = (conn.Upload, conn.Download);

                if (_allById.ContainsKey(conn.Id))
                {
                    // Update existing — swap in place in FilteredConnections.
                    _allById[conn.Id] = conn;
                }
                else
                {
                    _allById[conn.Id] = conn;
                }
            }

            // Remove connections that are no longer active.
            var removed = new List<string>();
            foreach (var id in _allById.Keys)
                if (!newIds.Contains(id)) removed.Add(id);
            foreach (var id in removed)
            {
                _allById.Remove(id);
                _prevTotals.Remove(id);
            }

            // Rebuild the filtered view.
            RebuildFiltered();
        });
    }

    private void RebuildFiltered()
    {
        FilteredConnections.Clear();
        foreach (var conn in _allById.Values)
            if (MatchesFilter(conn))
                FilteredConnections.Add(conn);
        OnPropertyChanged(nameof(HasConnections));
        OnPropertyChanged(nameof(EmptyVisibility));
    }

    private bool MatchesFilter(ConnectionItem conn)
    {
        if (string.IsNullOrEmpty(_searchText)) return true;
        return conn.Metadata.Host.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
               conn.Metadata.DestinationIP.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
               conn.Rule.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
               conn.ChainDisplay.Contains(_searchText, StringComparison.OrdinalIgnoreCase);
    }

    private void ClearConnections()
    {
        _allById.Clear();
        _prevTotals.Clear();
        FilteredConnections.Clear();
        _totalDown = 0;
        _totalUp = 0;
        OnPropertyChanged(nameof(TotalDownDisplay));
        OnPropertyChanged(nameof(TotalUpDisplay));
        OnPropertyChanged(nameof(HasConnections));
        OnPropertyChanged(nameof(EmptyVisibility));
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        _searchText = sender.Text ?? string.Empty;
        RebuildFiltered();
    }

    private async void CloseConnection_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not ConnectionItem conn) return;
        await MihomoService.Instance.CloseConnectionAsync(conn.Id);
    }

    private async void CloseAll_Click(object sender, RoutedEventArgs e)
    {
        var confirm = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = Strings.Connections_CloseAll,
            Content = Strings.Connections_CloseAllConfirm,
            PrimaryButtonText = Strings.Common_Ok,
            CloseButtonText = Strings.Common_Cancel
        };
        if (await confirm.ShowAsync() == ContentDialogResult.Primary)
            await MihomoService.Instance.CloseAllConnectionsAsync();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
