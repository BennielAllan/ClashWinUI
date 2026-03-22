using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Microsoft.UI.Xaml;

namespace ClashWinUI.Models;

// ── Log models ──────────────────────────────────────────────────────────────

/// <summary>
/// A single log entry from the /logs WebSocket stream.
/// </summary>
public sealed class LogItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;

    /// <summary>Formatted display time — uses current time if core didn't provide one.</summary>
    [JsonIgnore]
    public string TimeDisplay => string.IsNullOrEmpty(Time)
        ? DateTime.Now.ToString("HH:mm:ss")
        : Time;

    [JsonIgnore]
    public string LevelDisplay => Type.ToUpperInvariant() switch
    {
        "ERR" or "ERROR" => "ERR",
        "WARN" or "WARNING" => "WARN",
        "INFO" => "INFO",
        "DEBUG" => "DBG",
        _ => Type.ToUpperInvariant()
    };

    /// <summary>Foreground color (as hex string) per log level.</summary>
    [JsonIgnore]
    public Windows.UI.Color LevelColor => Type.ToLowerInvariant() switch
    {
        "err" or "error" => Windows.UI.Color.FromArgb(0xFF, 0xF4, 0x43, 0x36),    // red
        "warn" or "warning" => Windows.UI.Color.FromArgb(0xFF, 0xFF, 0x98, 0x00), // amber
        "info" => Windows.UI.Color.FromArgb(0xFF, 0x29, 0xB6, 0xF6),              // blue
        "debug" => Windows.UI.Color.FromArgb(0xFF, 0x9E, 0x9E, 0x9E),             // grey
        _ => Windows.UI.Color.FromArgb(0xFF, 0xBD, 0xBD, 0xBD)
    };

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

// ── Connection models ────────────────────────────────────────────────────────

public sealed class ConnectionMetadata
{
    [JsonPropertyName("network")]
    public string Network { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("host")]
    public string Host { get; set; } = string.Empty;

    [JsonPropertyName("sourceIP")]
    public string SourceIP { get; set; } = string.Empty;

    [JsonPropertyName("sourcePort")]
    public string SourcePort { get; set; } = string.Empty;

    [JsonPropertyName("destinationIP")]
    public string DestinationIP { get; set; } = string.Empty;

    [JsonPropertyName("destinationPort")]
    public string DestinationPort { get; set; } = string.Empty;

    [JsonPropertyName("process")]
    public string Process { get; set; } = string.Empty;

    [JsonPropertyName("processPath")]
    public string ProcessPath { get; set; } = string.Empty;

    [JsonPropertyName("remoteDestination")]
    public string RemoteDestination { get; set; } = string.Empty;
}

public sealed class ConnectionItem : INotifyPropertyChanged
{
    private long _upload;
    private long _download;
    private long _uploadRate;
    private long _downloadRate;

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("metadata")]
    public ConnectionMetadata Metadata { get; set; } = new();

    [JsonPropertyName("upload")]
    public long Upload
    {
        get => _upload;
        set { if (_upload == value) return; _upload = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("download")]
    public long Download
    {
        get => _download;
        set { if (_download == value) return; _download = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("start")]
    public string Start { get; set; } = string.Empty;

    [JsonPropertyName("chains")]
    public System.Collections.Generic.List<string> Chains { get; set; } = new();

    [JsonPropertyName("rule")]
    public string Rule { get; set; } = string.Empty;

    [JsonPropertyName("rulePayload")]
    public string RulePayload { get; set; } = string.Empty;

    /// <summary>Per-update upload rate bytes/s — set by ConnectionsService when diffing snapshots.</summary>
    [JsonIgnore]
    public long UploadRate
    {
        get => _uploadRate;
        set { if (_uploadRate == value) return; _uploadRate = value; OnPropertyChanged(); OnPropertyChanged(nameof(UploadRateDisplay)); }
    }

    [JsonIgnore]
    public long DownloadRate
    {
        get => _downloadRate;
        set { if (_downloadRate == value) return; _downloadRate = value; OnPropertyChanged(); OnPropertyChanged(nameof(DownloadRateDisplay)); }
    }

    // ── Display helpers ──────────────────────────────────────────────────────

    [JsonIgnore]
    public string HostDisplay => string.IsNullOrEmpty(Metadata.Host)
        ? Metadata.DestinationIP
        : Metadata.Host;

    [JsonIgnore]
    public string DestDisplay
    {
        get
        {
            var ip = Metadata.DestinationIP;
            var port = Metadata.DestinationPort;
            return string.IsNullOrEmpty(ip) ? port : $"{ip}:{port}";
        }
    }

    [JsonIgnore]
    public string ChainDisplay => Chains.Count > 0 ? string.Join(" → ", Chains) : string.Empty;

    [JsonIgnore]
    public string UploadRateDisplay => Helpers.SubscriptionDisplayHelper.FormatBytes(UploadRate) + "/s";

    [JsonIgnore]
    public string DownloadRateDisplay => Helpers.SubscriptionDisplayHelper.FormatBytes(DownloadRate) + "/s";

    [JsonIgnore]
    public string NetworkLabel => $"{Metadata.Network.ToUpperInvariant()} · {Metadata.Type}";

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Top-level response from GET /connections (and the WS /connections stream).
/// </summary>
public sealed class ConnectionsResponse
{
    [JsonPropertyName("downloadTotal")]
    public long DownloadTotal { get; set; }

    [JsonPropertyName("uploadTotal")]
    public long UploadTotal { get; set; }

    [JsonPropertyName("connections")]
    public System.Collections.Generic.List<ConnectionItem> Connections { get; set; } = new();
}

// ── Mihomo config models ─────────────────────────────────────────────────────

public sealed class MihomoConfig
{
    [JsonPropertyName("port")]
    public int Port { get; set; }

    [JsonPropertyName("socks-port")]
    public int SocksPort { get; set; }

    [JsonPropertyName("mixed-port")]
    public int MixedPort { get; set; }

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "rule";

    [JsonPropertyName("log-level")]
    public string LogLevel { get; set; } = "info";

    [JsonPropertyName("external-controller")]
    public string ExternalController { get; set; } = "127.0.0.1:9090";

    [JsonPropertyName("secret")]
    public string Secret { get; set; } = string.Empty;

    [JsonPropertyName("allow-lan")]
    public bool AllowLan { get; set; }

    [JsonPropertyName("ipv6")]
    public bool Ipv6 { get; set; }
}

// ── Rules models ─────────────────────────────────────────────────────────────

public sealed class RuleItem
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public string Payload { get; set; } = string.Empty;

    [JsonPropertyName("proxy")]
    public string Proxy { get; set; } = string.Empty;

    [JsonIgnore]
    public string Display => string.IsNullOrEmpty(Payload)
        ? $"{Type} → {Proxy}"
        : $"{Type},{Payload} → {Proxy}";
}

public sealed class RulesResponse
{
    [JsonPropertyName("rules")]
    public System.Collections.Generic.List<RuleItem> Rules { get; set; } = new();
}

// ── Traffic models ────────────────────────────────────────────────────────────

public sealed class TrafficItem
{
    [JsonPropertyName("up")]
    public long Up { get; set; }

    [JsonPropertyName("down")]
    public long Down { get; set; }
}
