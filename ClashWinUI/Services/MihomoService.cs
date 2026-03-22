using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using ClashWinUI.Models;

namespace ClashWinUI.Services;

/// <summary>
/// Manages the mihomo (Clash Meta) core process and its REST/WebSocket API.
/// Pattern: singleton accessed via MihomoService.Instance.
/// </summary>
public sealed class MihomoService
{
    // ── Singleton ────────────────────────────────────────────────────────────

    public static MihomoService Instance { get; } = new();
    private MihomoService() { }

    // ── Public state ─────────────────────────────────────────────────────────

    public bool IsRunning => _process is { HasExited: false };

    /// <summary>The port of the external-controller (REST API).</summary>
    public int ApiPort { get; private set; } = 9090;

    /// <summary>Bearer secret for the API (empty string = no auth).</summary>
    public string ApiSecret { get; private set; } = string.Empty;

    public string BaseUrl => $"http://127.0.0.1:{ApiPort}";

    // ── Events ────────────────────────────────────────────────────────────────

    public event EventHandler? RunningStateChanged;

    // ── Private fields ────────────────────────────────────────────────────────

    private Process? _process;
    private HttpClient? _http;
    private readonly SemaphoreSlim _startLock = new(1, 1);

    // ── Core process management ──────────────────────────────────────────────

    /// <summary>
    /// Locates the mihomo executable bundled with the app under Core/.
    /// </summary>
    private static string CoreExePath()
    {
        // When running as a packaged MSIX app, use the install folder.
        // When running unpackaged (debug), use the process directory.
        try
        {
            var pkgPath = Package.Current.InstalledLocation.Path;
            return Path.Combine(pkgPath, "Core", "mihomo-windows-amd64.exe");
        }
        catch
        {
            var dir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
            return Path.Combine(dir, "Core", "mihomo-windows-amd64.exe");
        }
    }

    /// <summary>
    /// Generates a minimal config file for mihomo that sets up the API port/secret
    /// but uses whatever proxy rules are in the active subscription config.
    /// Returns the path of the temp config file written.
    /// </summary>
    private static string WriteBootstrapConfig(int port, string secret, string? userConfigPath)
    {
        var workDir = Path.Combine(Path.GetTempPath(), "ClashWinUI");
        Directory.CreateDirectory(workDir);

        string configPath;
        if (!string.IsNullOrEmpty(userConfigPath) && File.Exists(userConfigPath))
        {
            // Inject external-controller settings into the user's YAML.
            configPath = Path.Combine(workDir, "active-config.yaml");
            var yaml = File.ReadAllText(userConfigPath);
            // Strip existing controller / secret lines and prepend new ones.
            var lines = new List<string>(yaml.Split('\n'));
            lines.RemoveAll(l =>
                l.TrimStart().StartsWith("external-controller", StringComparison.OrdinalIgnoreCase) ||
                l.TrimStart().StartsWith("secret", StringComparison.OrdinalIgnoreCase));
            lines.Insert(0, $"external-controller: '127.0.0.1:{port}'");
            if (!string.IsNullOrEmpty(secret))
                lines.Insert(1, $"secret: '{secret}'");
            File.WriteAllText(configPath, string.Join('\n', lines));
        }
        else
        {
            // Bare-minimum config so mihomo starts and exposes its API.
            configPath = Path.Combine(workDir, "bootstrap.yaml");
            var yaml = $@"mixed-port: 7890
external-controller: '127.0.0.1:{port}'
{(string.IsNullOrEmpty(secret) ? "" : $"secret: '{secret}'")}
mode: rule
log-level: info
allow-lan: false
";
            File.WriteAllText(configPath, yaml);
        }

        return configPath;
    }

    /// <summary>
    /// Starts the mihomo core process.
    /// </summary>
    /// <param name="port">API port (default 9090).</param>
    /// <param name="secret">API secret (empty = no auth).</param>
    /// <param name="userConfigPath">Optional path to a subscription YAML file to load.</param>
    public async Task StartAsync(int port = 9090, string secret = "", string? userConfigPath = null)
    {
        await _startLock.WaitAsync();
        try
        {
            if (IsRunning) return;

            ApiPort = port;
            ApiSecret = secret;

            var exePath = CoreExePath();
            if (!File.Exists(exePath))
                throw new FileNotFoundException($"mihomo core not found at: {exePath}");

            var configPath = WriteBootstrapConfig(port, secret, userConfigPath);
            var workDir = Path.GetDirectoryName(configPath)!;

            _http = BuildHttpClient();

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"-d \"{workDir}\" -f \"{configPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
            };

            _process = Process.Start(psi);
            if (_process == null)
                throw new InvalidOperationException("Failed to start mihomo process.");

            _process.EnableRaisingEvents = true;
            _process.Exited += (_, _) => RunningStateChanged?.Invoke(this, EventArgs.Empty);

            // Give the core a moment to initialise the HTTP listener.
            await Task.Delay(800);

            RunningStateChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _startLock.Release();
        }
    }

    /// <summary>
    /// Stops the mihomo core process.
    /// </summary>
    public async Task StopAsync()
    {
        await _startLock.WaitAsync();
        try
        {
            if (_process == null || _process.HasExited) return;
            try { _process.Kill(entireProcessTree: true); } catch { /* best-effort */ }
            await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            _process.Dispose();
            _process = null;
            _http?.Dispose();
            _http = null;
            RunningStateChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _startLock.Release();
        }
    }

    // ── HTTP client helper ────────────────────────────────────────────────────

    private HttpClient BuildHttpClient()
    {
        var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "ClashWinUI");
        if (!string.IsNullOrEmpty(ApiSecret))
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ApiSecret);
        return client;
    }

    private HttpClient Http => _http ??= BuildHttpClient();

    // ── REST API: Proxies ─────────────────────────────────────────────────────

    /// <summary>
    /// Fetches all proxies and groups from GET /proxies and returns organised ProxyGroup list.
    /// </summary>
    public async Task<List<ProxyGroup>> GetProxyGroupsAsync(CancellationToken ct = default)
    {
        var resp = await Http.GetFromJsonAsync<ProxiesResponse>("/proxies", ct);
        if (resp == null) return new();

        var allEntries = resp.Proxies;

        // Build a flat map of node name → ProxyNode.
        var nodeMap = new Dictionary<string, ProxyNode>(StringComparer.Ordinal);
        foreach (var (name, entry) in allEntries)
        {
            if (!IsGroupType(entry.Type))
            {
                var node = new ProxyNode
                {
                    Name = name,
                    Type = entry.Type,
                    Udp = entry.Udp,
                };
                // Use last history entry as the current delay.
                if (entry.History is { Count: > 0 })
                    node.Delay = entry.History[^1].Delay;
                nodeMap[name] = node;
            }
        }

        // Build ProxyGroup list (exclude leaf-only types and GLOBAL/DIRECT/REJECT special groups).
        var groups = new List<ProxyGroup>();
        var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "GLOBAL", "DIRECT", "REJECT", "COMPATIBLE", "Pass" };

        foreach (var (name, entry) in allEntries)
        {
            if (!IsGroupType(entry.Type)) continue;
            if (skip.Contains(name)) continue;

            var group = new ProxyGroup
            {
                Name = name,
                Type = entry.Type,
                Now = entry.Now ?? string.Empty,
                All = entry.All ?? new(),
            };

            // Resolve node objects.
            foreach (var nodeName in group.All)
            {
                if (nodeMap.TryGetValue(nodeName, out var node))
                    group.Nodes.Add(node);
                else
                {
                    // Could be a sub-group reference — represent as a pseudo-node.
                    group.Nodes.Add(new ProxyNode { Name = nodeName, Type = "Selector" });
                }
            }

            groups.Add(group);
        }

        return groups;
    }

    private static bool IsGroupType(string type) =>
        type is "Selector" or "URLTest" or "Fallback" or "LoadBalance" or "Relay";

    /// <summary>
    /// Selects a proxy node for a given group (PUT /proxies/{group}).
    /// </summary>
    public async Task<bool> SelectProxyAsync(string groupName, string proxyName, CancellationToken ct = default)
    {
        var body = JsonContent.Create(new { name = proxyName });
        var resp = await Http.PutAsync($"/proxies/{Uri.EscapeDataString(groupName)}", body, ct);
        return resp.IsSuccessStatusCode;
    }

    /// <summary>
    /// Tests the latency of a specific proxy (GET /proxies/{name}/delay).
    /// Returns 0 for timeout, -1 for error.
    /// </summary>
    public async Task<int> TestDelayAsync(string proxyName,
        string testUrl = "http://www.gstatic.com/generate_204",
        int timeout = 5000,
        CancellationToken ct = default)
    {
        try
        {
            var url = $"/proxies/{Uri.EscapeDataString(proxyName)}/delay" +
                      $"?url={Uri.EscapeDataString(testUrl)}&timeout={timeout}";
            var resp = await Http.GetFromJsonAsync<DelayResponse>(url, ct);
            return resp?.Delay ?? 0;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadGateway)
        {
            return 0; // timeout / unreachable → show as timeout
        }
        catch
        {
            return -1;
        }
    }

    // ── REST API: Config ──────────────────────────────────────────────────────

    public async Task<MihomoConfig?> GetConfigAsync(CancellationToken ct = default)
    {
        return await Http.GetFromJsonAsync<MihomoConfig>("/configs", ct);
    }

    /// <summary>
    /// Changes the proxy mode (rule / global / direct).
    /// </summary>
    public async Task<bool> SetModeAsync(string mode, CancellationToken ct = default)
    {
        var body = JsonContent.Create(new { mode });
        var resp = await Http.PatchAsync("/configs", body, ct);
        return resp.IsSuccessStatusCode;
    }

    // ── REST API: Connections ─────────────────────────────────────────────────

    public async Task<ConnectionsResponse?> GetConnectionsAsync(CancellationToken ct = default)
    {
        return await Http.GetFromJsonAsync<ConnectionsResponse>("/connections", ct);
    }

    public async Task<bool> CloseConnectionAsync(string id, CancellationToken ct = default)
    {
        var resp = await Http.DeleteAsync($"/connections/{Uri.EscapeDataString(id)}", ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> CloseAllConnectionsAsync(CancellationToken ct = default)
    {
        var resp = await Http.DeleteAsync("/connections", ct);
        return resp.IsSuccessStatusCode;
    }

    // ── REST API: Rules ───────────────────────────────────────────────────────

    public async Task<List<RuleItem>> GetRulesAsync(CancellationToken ct = default)
    {
        var resp = await Http.GetFromJsonAsync<RulesResponse>("/rules", ct);
        return resp?.Rules ?? new();
    }

    // ── WebSocket streams ─────────────────────────────────────────────────────

    /// <summary>
    /// Streams log entries from WS /logs. Calls <paramref name="onLog"/> for each entry on the
    /// calling thread's synchronisation context (use DispatcherQueue.TryEnqueue in the callback).
    /// </summary>
    public async Task StreamLogsAsync(
        string level,
        Action<LogItem> onLog,
        CancellationToken ct)
    {
        var wsUri = new Uri($"ws://127.0.0.1:{ApiPort}/logs?level={level}");
        await ConnectAndStreamAsync(wsUri, json =>
        {
            var item = JsonSerializer.Deserialize<LogItem>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (item != null) onLog(item);
        }, ct);
    }

    /// <summary>
    /// Streams connection snapshots from WS /connections. Calls <paramref name="onSnapshot"/>
    /// with each update.
    /// </summary>
    public async Task StreamConnectionsAsync(
        Action<ConnectionsResponse> onSnapshot,
        CancellationToken ct)
    {
        var wsUri = new Uri($"ws://127.0.0.1:{ApiPort}/connections");
        await ConnectAndStreamAsync(wsUri, json =>
        {
            var snap = JsonSerializer.Deserialize<ConnectionsResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (snap != null) onSnapshot(snap);
        }, ct);
    }

    /// <summary>
    /// Streams traffic bytes/sec from WS /traffic.
    /// </summary>
    public async Task StreamTrafficAsync(
        Action<TrafficItem> onTraffic,
        CancellationToken ct)
    {
        var wsUri = new Uri($"ws://127.0.0.1:{ApiPort}/traffic");
        await ConnectAndStreamAsync(wsUri, json =>
        {
            var item = JsonSerializer.Deserialize<TrafficItem>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (item != null) onTraffic(item);
        }, ct);
    }

    // ── WebSocket helper ──────────────────────────────────────────────────────

    private async Task ConnectAndStreamAsync(Uri wsUri, Action<string> onMessage, CancellationToken ct)
    {
        using var ws = new ClientWebSocket();
        if (!string.IsNullOrEmpty(ApiSecret))
            ws.Options.SetRequestHeader("Authorization", $"Bearer {ApiSecret}");

        try
        {
            await ws.ConnectAsync(wsUri, ct);
            var buffer = new byte[4096];
            var sb = new StringBuilder();

            while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                sb.Clear();
                WebSocketReceiveResult result;
                do
                {
                    result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close) break;

                var json = sb.ToString();
                if (!string.IsNullOrWhiteSpace(json))
                    onMessage(json);
            }
        }
        catch (OperationCanceledException) { /* expected on shutdown */ }
        catch (WebSocketException) { /* core stopped, caller handles reconnect logic */ }
    }
}
