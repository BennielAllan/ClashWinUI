using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace ClashWinUI.Models;

/// <summary>
/// A single proxy node (e.g., a VMESS/SOCKS5/Selector entry from /proxies).
/// </summary>
public sealed class ProxyNode : INotifyPropertyChanged
{
    private int? _delay;
    private bool _isTesting;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Udp { get; set; }

    /// <summary>Latest measured round-trip delay in ms. Null = not tested yet.</summary>
    public int? Delay
    {
        get => _delay;
        set
        {
            if (_delay == value) return;
            _delay = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DelayDisplay));
            OnPropertyChanged(nameof(DelayColor));
            OnPropertyChanged(nameof(DelayVisibility));
        }
    }

    /// <summary>True while a speed-test is in progress for this node.</summary>
    [JsonIgnore]
    public bool IsTesting
    {
        get => _isTesting;
        set
        {
            if (_isTesting == value) return;
            _isTesting = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DelayVisibility));
            OnPropertyChanged(nameof(TestingVisibility));
        }
    }

    [JsonIgnore]
    public string DelayDisplay => Delay switch
    {
        null => "—",
        0 => "Timeout",
        var d => $"{d} ms"
    };

    /// <summary>Color coded by latency tier.</summary>
    [JsonIgnore]
    public Windows.UI.Color DelayColor => Delay switch
    {
        null => Windows.UI.Color.FromArgb(0xFF, 0x9E, 0x9E, 0x9E),   // grey
        0 => Windows.UI.Color.FromArgb(0xFF, 0xF4, 0x43, 0x36),      // red (timeout)
        <= 150 => Windows.UI.Color.FromArgb(0xFF, 0x4C, 0xAF, 0x50), // green
        <= 500 => Windows.UI.Color.FromArgb(0xFF, 0xFF, 0x98, 0x00), // amber
        _ => Windows.UI.Color.FromArgb(0xFF, 0xF4, 0x43, 0x36),      // red
    };

    [JsonIgnore]
    public Visibility DelayVisibility => IsTesting ? Visibility.Collapsed : Visibility.Visible;

    [JsonIgnore]
    public Visibility TestingVisibility => IsTesting ? Visibility.Visible : Visibility.Collapsed;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// A proxy group (Selector / URLTest / Fallback / LoadBalance / …).
/// </summary>
public sealed class ProxyGroup : INotifyPropertyChanged
{
    private string _now = string.Empty;
    private bool _isExpanded;
    private bool _isTesting;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;

    /// <summary>Currently selected proxy name within this group.</summary>
    public string Now
    {
        get => _now;
        set
        {
            if (_now == value) return;
            _now = value;
            OnPropertyChanged();
        }
    }

    /// <summary>All proxy names that belong to this group.</summary>
    public List<string> All { get; set; } = new();

    /// <summary>Resolved node objects (populated from the flat proxies map).</summary>
    [JsonIgnore]
    public List<ProxyNode> Nodes { get; set; } = new();

    [JsonIgnore]
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ExpandedVisibility));
            OnPropertyChanged(nameof(ChevronGlyph));
        }
    }

    [JsonIgnore]
    public bool IsTesting
    {
        get => _isTesting;
        set
        {
            if (_isTesting == value) return;
            _isTesting = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TestButtonVisibility));
            OnPropertyChanged(nameof(TestRingVisibility));
        }
    }

    [JsonIgnore]
    public Visibility ExpandedVisibility => IsExpanded ? Visibility.Visible : Visibility.Collapsed;

    [JsonIgnore]
    public string ChevronGlyph => IsExpanded ? "\uE70E" : "\uE76C"; // up / down chevron

    [JsonIgnore]
    public Visibility TestButtonVisibility => IsTesting ? Visibility.Collapsed : Visibility.Visible;

    [JsonIgnore]
    public Visibility TestRingVisibility => IsTesting ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Only Selector groups allow manual node selection.</summary>
    [JsonIgnore]
    public bool IsSelector => Type.Equals("Selector", System.StringComparison.OrdinalIgnoreCase);

    /// <summary>Localized type label.</summary>
    [JsonIgnore]
    public string TypeLabel => Type;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Response shape from GET /proxies.
/// </summary>
public sealed class ProxiesResponse
{
    [JsonPropertyName("proxies")]
    public Dictionary<string, ProxiesEntry> Proxies { get; set; } = new();
}

/// <summary>
/// Individual entry in the /proxies map (may be a group or a leaf node).
/// </summary>
public sealed class ProxiesEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("udp")]
    public bool Udp { get; set; }

    [JsonPropertyName("now")]
    public string? Now { get; set; }

    [JsonPropertyName("all")]
    public List<string>? All { get; set; }

    [JsonPropertyName("history")]
    public List<ProxyHistoryEntry>? History { get; set; }
}

public sealed class ProxyHistoryEntry
{
    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("delay")]
    public int Delay { get; set; }
}

/// <summary>
/// Response from GET /proxies/{name}/delay.
/// </summary>
public sealed class DelayResponse
{
    [JsonPropertyName("delay")]
    public int Delay { get; set; }
}
