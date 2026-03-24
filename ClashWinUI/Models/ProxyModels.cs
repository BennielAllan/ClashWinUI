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
/// Shared across all groups that contain this proxy — delay/testing state is global.
/// </summary>
public sealed class ProxyNode : INotifyPropertyChanged
{
    private int? _delay;
    private bool _isTesting;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Udp { get; set; }

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

    [JsonIgnore]
    public Windows.UI.Color DelayColor => Delay switch
    {
        null => Windows.UI.Color.FromArgb(0xFF, 0x9E, 0x9E, 0x9E),
        0 => Windows.UI.Color.FromArgb(0xFF, 0xF4, 0x43, 0x36),
        <= 150 => Windows.UI.Color.FromArgb(0xFF, 0x4C, 0xAF, 0x50),
        <= 500 => Windows.UI.Color.FromArgb(0xFF, 0xFF, 0x98, 0x00),
        _ => Windows.UI.Color.FromArgb(0xFF, 0xF4, 0x43, 0x36),
    };

    [JsonIgnore]
    public Visibility DelayVisibility => IsTesting ? Visibility.Collapsed : Visibility.Visible;

    [JsonIgnore]
    public Visibility TestingVisibility => IsTesting ? Visibility.Visible : Visibility.Collapsed;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Group-scoped view of a proxy node. Computes IsNow relative to its owning group,
/// so the same ProxyNode can show different selection states in different groups.
/// Also used as the Button Tag so Node_Click knows exactly which group was clicked.
/// </summary>
public sealed class ProxyNodeView : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public ProxyNodeView(ProxyGroup group, ProxyNode node)
    {
        Group = group;
        Node = node;
        // Propagate node property changes (delay, testing) to the view.
        node.PropertyChanged += (_, e) => PropertyChanged?.Invoke(this, e);
        // When the group's selection changes, recompute IsNow.
        group.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ProxyGroup.Now))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsNow)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedDotVisibility)));
            }
        };
    }

    public ProxyGroup Group { get; }
    public ProxyNode Node { get; }

    // Forwarded for direct XAML binding.
    public string Name => Node.Name;
    public string Type => Node.Type;
    public bool IsTesting => Node.IsTesting;
    public string DelayDisplay => Node.DelayDisplay;
    public Visibility DelayVisibility => Node.DelayVisibility;
    public Visibility TestingVisibility => Node.TestingVisibility;

    public bool IsNow => Group.Now == Node.Name;
    public Visibility SelectedDotVisibility => IsNow ? Visibility.Visible : Visibility.Collapsed;
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

    public List<string> All { get; set; } = new();

    /// <summary>Resolved node objects. Shared across groups; used for testing.</summary>
    [JsonIgnore]
    public List<ProxyNode> Nodes { get; set; } = new();

    /// <summary>Per-group view wrappers used for display (IsNow is group-scoped).</summary>
    [JsonIgnore]
    public List<ProxyNodeView> NodeViews { get; set; } = new();

    [JsonIgnore]
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            OnPropertyChanged();
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
    public Visibility TestButtonVisibility => IsTesting ? Visibility.Collapsed : Visibility.Visible;

    [JsonIgnore]
    public Visibility TestRingVisibility => IsTesting ? Visibility.Visible : Visibility.Collapsed;

    [JsonIgnore]
    public bool IsSelector => Type.Equals("Selector", System.StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public string TypeLabel => Type;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>Response shape from GET /proxies.</summary>
public sealed class ProxiesResponse
{
    [JsonPropertyName("proxies")]
    public Dictionary<string, ProxiesEntry> Proxies { get; set; } = new();
}

/// <summary>Individual entry in the /proxies map.</summary>
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

/// <summary>Response from GET /proxies/{name}/delay.</summary>
public sealed class DelayResponse
{
    [JsonPropertyName("delay")]
    public int Delay { get; set; }
}
