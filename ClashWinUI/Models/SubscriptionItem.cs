using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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

    public event PropertyChangedEventHandler? PropertyChanged;

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
        set => SetField(ref _urlOrPath, value);
    }

    /// <summary>True = remote (URL), false = local file.</summary>
    public bool IsRemote
    {
        get => _isRemote;
        set => SetField(ref _isRemote, value);
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
                OnPropertyChanged(nameof(UsageTotalDisplay));
        }
    }

    /// <summary>Traffic total/cap in bytes (optional; null = unlimited).</summary>
    public long? TotalBytes
    {
        get => _totalBytes;
        set
        {
            if (SetField(ref _totalBytes, value))
                OnPropertyChanged(nameof(UsageTotalDisplay));
        }
    }

    /// <summary>Relative time since last refresh, e.g. "5 minutes ago" / "5 分钟前".</summary>
    public string RefreshedAgoDisplay => ClashWinUI.Helpers.SubscriptionDisplayHelper.GetRefreshedAgo(UpdatedAt);

    /// <summary>Formatted usage/total, e.g. "1.2 GB / 50 GB" or "— / —".</summary>
    public string UsageTotalDisplay => ClashWinUI.Helpers.SubscriptionDisplayHelper.FormatUsageTotal(UsageBytes, TotalBytes);

    /// <summary>Visibility for remote indicator icon.</summary>
    public Visibility RemoteIconVisibility => IsRemote ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Tooltip for refresh button (same for all items).</summary>
    public string RefreshTooltip => ClashWinUI.Strings.Subscription_Refresh;

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
