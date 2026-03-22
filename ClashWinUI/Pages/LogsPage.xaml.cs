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

public sealed partial class LogsPage : Page, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    // ── Labels ────────────────────────────────────────────────────────────────

    public string PageTitle => Strings.Nav_Logs;
    public string SearchPlaceholder => Strings.Logs_Search;
    public string ClearLabel => Strings.Logs_Clear;
    public string NotRunningMessage => Strings.Logs_NotRunning;
    public string EmptyMessage => Strings.Logs_Empty;

    // ── State ─────────────────────────────────────────────────────────────────

    private bool _isPaused;
    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            if (_isPaused == value) return;
            _isPaused = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PauseResumeGlyph));
            OnPropertyChanged(nameof(PauseResumeTooltip));
        }
    }

    public string PauseResumeGlyph => IsPaused ? "\uE768" : "\uE769"; // Play / Pause
    public string PauseResumeTooltip => IsPaused ? Strings.Logs_Resume : Strings.Logs_Pause;

    public bool IsNotRunning => !MihomoService.Instance.IsRunning;

    public Visibility ListVisibility => MihomoService.Instance.IsRunning ? Visibility.Visible : Visibility.Collapsed;
    public Visibility EmptyVisibility => (MihomoService.Instance.IsRunning && FilteredLogs.Count == 0) ? Visibility.Visible : Visibility.Collapsed;

    // ── Data ─────────────────────────────────────────────────────────────────

    private readonly ObservableCollection<LogItem> _allLogs = new();
    public ObservableCollection<LogItem> FilteredLogs { get; } = new();

    private string _searchText = string.Empty;
    private string _levelFilter = string.Empty; // empty = all

    private readonly DispatcherQueue _dq;
    private CancellationTokenSource? _cts;
    private readonly object _logLock = new();

    // Max log lines kept in memory.
    private const int MaxLogs = 500;

    // ── Constructor ───────────────────────────────────────────────────────────

    public LogsPage()
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
                ClearLogs();
            }
        });
    }

    // ── Streaming ─────────────────────────────────────────────────────────────

    private void StartStreaming()
    {
        StopStreaming();
        _cts = new CancellationTokenSource();
        var level = string.IsNullOrEmpty(_levelFilter) ? "debug" : _levelFilter;
        var token = _cts.Token;
        _ = Task.Run(() => MihomoService.Instance.StreamLogsAsync(level, OnLogReceived, token));
    }

    private void StopStreaming()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private void OnLogReceived(LogItem item)
    {
        if (IsPaused) return;
        _dq.TryEnqueue(() =>
        {
            lock (_logLock)
            {
                _allLogs.Add(item);
                if (_allLogs.Count > MaxLogs)
                    _allLogs.RemoveAt(0);
            }
            if (MatchesFilter(item))
            {
                FilteredLogs.Add(item);
                if (FilteredLogs.Count > MaxLogs)
                    FilteredLogs.RemoveAt(0);
                // Scroll to end.
                LogListView.ScrollIntoView(item);
            }
            OnPropertyChanged(nameof(EmptyVisibility));
        });
    }

    // ── Filtering ─────────────────────────────────────────────────────────────

    private bool MatchesFilter(LogItem item)
    {
        if (!string.IsNullOrEmpty(_levelFilter) &&
            !item.Type.Equals(_levelFilter, StringComparison.OrdinalIgnoreCase) &&
            !item.Type.Equals(_levelFilter.TrimEnd('r'), StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.IsNullOrEmpty(_searchText) &&
            !item.Payload.Contains(_searchText, StringComparison.OrdinalIgnoreCase) &&
            !item.Type.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }

    private void RebuildFiltered()
    {
        FilteredLogs.Clear();
        lock (_logLock)
        {
            foreach (var item in _allLogs)
                if (MatchesFilter(item))
                    FilteredLogs.Add(item);
        }
        OnPropertyChanged(nameof(EmptyVisibility));
    }

    private void ClearLogs()
    {
        lock (_logLock) _allLogs.Clear();
        FilteredLogs.Clear();
        OnPropertyChanged(nameof(EmptyVisibility));
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        _searchText = sender.Text ?? string.Empty;
        RebuildFiltered();
    }

    private void LevelCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LevelCombo.SelectedItem is not ComboBoxItem item) return;
        var tag = item.Tag as string ?? string.Empty;
        // "All" is index 0 and tag "debug" but we want no filter for all
        _levelFilter = LevelCombo.SelectedIndex == 0 ? string.Empty : tag;

        // Restart stream with correct level if connected.
        if (MihomoService.Instance.IsRunning) StartStreaming();
        RebuildFiltered();
    }

    private void PauseResume_Click(object sender, RoutedEventArgs e)
    {
        IsPaused = !IsPaused;
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        ClearLogs();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
