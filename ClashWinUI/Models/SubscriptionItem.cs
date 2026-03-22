using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Microsoft.UI.Xaml;

namespace ClashWinUI.Models;

/// <summary>
/// Represents a subscription or profile (remote URL or local file). Reference: clash-verge-rev profile types.
/// </summary>
public sealed class SubscriptionItem : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _name = string.Empty;
    private string _urlOrPath = string.Empty;
    private bool _isRemote = true;
    private DateTimeOffset? _updatedAt;
    private long? _usageBytes;
    private long? _totalBytes;
    private bool _isRefreshing;
    private int _updateIntervalMinutes;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>True while a refresh is in progress; used to show ProgressRing and disable the refresh button. Not persisted (JsonIgnore).</summary>
    [JsonIgnore]
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set
        {
            if (!SetField(ref _isRefreshing, value)) return;
            OnPropertyChanged(nameof(IsRefreshButtonVisible));
            OnPropertyChanged(nameof(RefreshRingVisibility));
            OnPropertyChanged(nameof(RefreshButtonVisibility));
        }
    }

    /// <summary>False when IsRefreshing is true, so the refresh button can be hidden during load.</summary>
    public bool IsRefreshButtonVisible => !IsRefreshing;

    /// <summary>Visibility for the refresh ProgressRing (Visible when refreshing).</summary>
    public Visibility RefreshRingVisibility => IsRefreshing ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Visibility for the refresh button (Collapsed when refreshing).</summary>
    public Visibility RefreshButtonVisibility => IsRefreshing ? Visibility.Collapsed : Visibility.Visible;

    public string Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    /// <summary>Subscription URL for remote, or local file path.</summary>
    public string UrlOrPath
    {
        get => _urlOrPath;
        set
        {
            if (SetField(ref _urlOrPath, value))
                OnPropertyChanged(nameof(HasUrlOrPathVisibility));
        }
    }

    /// <summary>True = remote (URL), false = local file.</summary>
    public bool IsRemote
    {
        get => _isRemote;
        set
        {
            if (SetField(ref _isRemote, value))
            {
                OnPropertyChanged(nameof(TypeLabel));
                OnPropertyChanged(nameof(TypeIcon));
                OnPropertyChanged(nameof(RemoteIconVisibility));
            }
        }
    }

    public DateTimeOffset? UpdatedAt
    {
        get => _updatedAt;
        set
        {
            if (SetField(ref _updatedAt, value))
                OnPropertyChanged(nameof(RefreshedAgoDisplay));
        }
    }

    /// <summary>Traffic usage in bytes (optional, from subscription info).</summary>
    public long? UsageBytes
    {
        get => _usageBytes;
        set
        {
            if (SetField(ref _usageBytes, value))
            {
                OnPropertyChanged(nameof(UsageTotalDisplay));
                OnPropertyChanged(nameof(UsageProgress));
            }
        }
    }

    /// <summary>Traffic total/cap in bytes (optional; null = unlimited).</summary>
    public long? TotalBytes
    {
        get => _totalBytes;
        set
        {
            if (SetField(ref _totalBytes, value))
            {
                OnPropertyChanged(nameof(UsageTotalDisplay));
                OnPropertyChanged(nameof(UsageProgress));
            }
        }
    }

    /// <summary>Auto-refresh interval in minutes; 0 = disabled.</summary>
    public int UpdateIntervalMinutes
    {
        get => _updateIntervalMinutes;
        set => SetField(ref _updateIntervalMinutes, value);
    }

    /// <summary>Progress 0.0–1.0 for usage/total; 0 when no total.</summary>
    public double UsageProgress =>
        TotalBytes.HasValue && TotalBytes.Value > 0 && UsageBytes.HasValue
            ? Math.Clamp((double)UsageBytes.Value / TotalBytes.Value, 0, 1)
            : 0;

    /// <summary>Relative time since last refresh, e.g. "5 minutes ago" / "5 分钟前".</summary>
    public string RefreshedAgoDisplay => ClashWinUI.Helpers.SubscriptionDisplayHelper.GetRefreshedAgo(UpdatedAt);

    /// <summary>Formatted usage/total, e.g. "1.2 GB / 50 GB" or "— / —".</summary>
    public string UsageTotalDisplay => ClashWinUI.Helpers.SubscriptionDisplayHelper.FormatUsageTotal(UsageBytes, TotalBytes);

    /// <summary>Visibility for remote indicator icon.</summary>
    public Visibility RemoteIconVisibility => IsRemote ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Localized type label for the badge (Remote / Local).</summary>
    [JsonIgnore]
    public string TypeLabel => IsRemote ? ClashWinUI.Strings.Subscription_TypeRemote : ClashWinUI.Strings.Subscription_TypeLocal;

    /// <summary>Segoe Fluent icon glyph for the type: globe for remote, document for local.</summary>
    [JsonIgnore]
    public string TypeIcon => IsRemote ? "\uE774" : "\uE8A5";

    /// <summary>Hides the URL/path row when the field is empty.</summary>
    [JsonIgnore]
    public Visibility HasUrlOrPathVisibility => string.IsNullOrEmpty(UrlOrPath) ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>Tooltip for refresh button (same for all items).</summary>
    public string RefreshTooltip => ClashWinUI.Strings.Subscription_Refresh;

    /// <summary>Tooltip for more-actions button.</summary>
    public string MoreTooltip => ClashWinUI.Strings.Subscription_More;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
