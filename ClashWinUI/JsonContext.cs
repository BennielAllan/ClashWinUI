using System.Collections.Generic;
using System.Text.Json.Serialization;
using ClashWinUI.Models;

namespace ClashWinUI;

/// <summary>
/// Source-generated JSON context for all types used in HTTP/WebSocket API calls
/// and subscription persistence.  Avoids reflection-based serialization which is
/// disabled in Windows App SDK packaged apps.
/// </summary>
[JsonSerializable(typeof(ProxiesResponse))]
[JsonSerializable(typeof(DelayResponse))]
[JsonSerializable(typeof(MihomoConfig))]
[JsonSerializable(typeof(TunConfig))]
[JsonSerializable(typeof(ConnectionsResponse))]
[JsonSerializable(typeof(RulesResponse))]
[JsonSerializable(typeof(LogItem))]
[JsonSerializable(typeof(TrafficItem))]
[JsonSerializable(typeof(List<SubscriptionItem>))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal partial class AppJsonContext : JsonSerializerContext { }
