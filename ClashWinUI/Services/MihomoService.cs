using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

    /// <summary>Last stderr output from the core process (for diagnostics).</summary>
    public string LastStartupLog { get; private set; } = string.Empty;

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

            // Use a persistent work dir under LocalAppData so geo databases survive reboots.
            var workDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ClashWinUI");
            Directory.CreateDirectory(workDir);

            // Ensure GeoIP database exists before launching (mihomo needs it to start).
            await EnsureGeodataAsync(workDir);

            // Write an active config that is guaranteed to have external-controller.
            var configPath = PrepareConfig(port, secret, userConfigPath, workDir);

            _http = BuildHttpClient();

            // No -ext-ctl needed — external-controller is already injected into the config.
            var args = $"-d \"{workDir}\" -f \"{configPath}\"";
            if (!string.IsNullOrEmpty(secret))
                args += $" -secret \"{secret}\"";

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = args,
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            LastStartupLog = string.Empty;
            try { _process = Process.Start(psi); }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                // User cancelled the UAC prompt.
                throw new InvalidOperationException("需要管理员权限以启动核心（TUN 模式需要）。");
            }
            if (_process == null)
                throw new InvalidOperationException("Failed to start mihomo process.");

            _process.EnableRaisingEvents = true;
            _process.Exited += (_, _) =>
            {
                RunningStateChanged?.Invoke(this, EventArgs.Empty);
            };

            // Poll until the API is responsive (up to 8 s).
            var ready = await WaitForApiAsync(TimeSpan.FromSeconds(8));

            if (!ready)
                throw new InvalidOperationException("mihomo did not respond on port " + port);

            RunningStateChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _startLock.Release();
        }
    }

    /// <summary>Polls GET / until the core API responds or the timeout elapses. Returns true if ready.</summary>
    private async Task<bool> WaitForApiAsync(TimeSpan timeout)
    {
        using var probe = BuildHttpClient();
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (_process?.HasExited == true) return false;
            try
            {
                var r = await probe.GetAsync("/");
                if (r.IsSuccessStatusCode || (int)r.StatusCode < 500) return true;
            }
            catch { /* not ready yet */ }
            await Task.Delay(200);
        }
        return false;
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

    // ── Geodata bootstrap ─────────────────────────────────────────────────────

    private static readonly (string File, string Url)[] GeodataFiles =
    [
        ("Country.mmdb",  "https://github.com/MetaCubeX/meta-rules-dat/releases/download/latest/country.mmdb"),
        ("GeoSite.dat",   "https://github.com/MetaCubeX/meta-rules-dat/releases/download/latest/geosite.dat"),
        ("GeoIP.dat",     "https://github.com/MetaCubeX/meta-rules-dat/releases/download/latest/geoip.dat"),
    ];

    /// <summary>
    /// Downloads missing GeoIP/GeoSite data files into <paramref name="workDir"/>.
    /// Only downloads if the file is absent (cached on disk indefinitely).
    /// </summary>
    private static async Task EnsureGeodataAsync(string workDir)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "ClashWinUI");
        // Follow redirects (GitHub releases redirect to CDN).
        http.Timeout = TimeSpan.FromSeconds(60);

        foreach (var (file, url) in GeodataFiles)
        {
            var dest = Path.Combine(workDir, file);
            if (File.Exists(dest)) continue;

            var tmpDest = dest + ".tmp";
            try
            {
                var bytes = await http.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(tmpDest, bytes);
                File.Move(tmpDest, dest, overwrite: true);
            }
            catch
            {
                try { File.Delete(tmpDest); } catch { }
                // Non-fatal: mihomo may still start without GeoSite/GeoIP.dat.
                // Country.mmdb failure will cause mihomo to fail — rethrow for that.
                if (file == "Country.mmdb") throw;
            }
        }
    }

    // ── Config preparation ────────────────────────────────────────────────────

    /// <summary>
    /// Reads the user config (if any), injects/replaces the external-controller line,
    /// writes the result to workDir/active-config.yaml, and returns that path.
    /// </summary>
    private static string PrepareConfig(int port, string secret, string? userConfigPath, string workDir)
    {
        string yaml;
        if (!string.IsNullOrEmpty(userConfigPath) && File.Exists(userConfigPath))
        {
            yaml = File.ReadAllText(userConfigPath);
            var extCtlLine = $"external-controller: '127.0.0.1:{port}'";
            var pattern = @"^external-controller\s*:.*";
            var opts = System.Text.RegularExpressions.RegexOptions.Multiline;
            if (System.Text.RegularExpressions.Regex.IsMatch(yaml, pattern, opts))
                yaml = System.Text.RegularExpressions.Regex.Replace(yaml, pattern, extCtlLine, opts);
            else
                yaml = extCtlLine + "\n" + yaml;

            if (!string.IsNullOrEmpty(secret))
            {
                var secretLine = $"secret: '{secret}'";
                var secretPattern = @"^secret\s*:.*";
                if (System.Text.RegularExpressions.Regex.IsMatch(yaml, secretPattern, opts))
                    yaml = System.Text.RegularExpressions.Regex.Replace(yaml, secretPattern, secretLine, opts);
                else
                    yaml = secretLine + "\n" + yaml;
            }
        }
        else
        {
            yaml = $"mixed-port: 7890\nexternal-controller: '127.0.0.1:{port}'\nmode: rule\nlog-level: info\nallow-lan: false\n";
            if (!string.IsNullOrEmpty(secret))
                yaml = $"secret: '{secret}'\n" + yaml;
        }

        var configPath = Path.Combine(workDir, "active-config.yaml");
        File.WriteAllText(configPath, yaml);
        return configPath;
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
        var resp = await Http.GetFromJsonAsync("/proxies", AppJsonContext.Default.ProxiesResponse, ct);
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
                    // Sub-group reference — pseudo-node with the actual group type.
                    var subType = allEntries.TryGetValue(nodeName, out var subEntry) ? subEntry.Type : "Selector";
                    group.Nodes.Add(new ProxyNode { Name = nodeName, Type = subType });
                }
            }

            // Sort: sub-group nodes first, then leaf proxy nodes.
            group.Nodes.Sort((a, b) =>
            {
                bool aIsGroup = IsGroupType(a.Type);
                bool bIsGroup = IsGroupType(b.Type);
                if (aIsGroup == bIsGroup) return 0;
                return aIsGroup ? -1 : 1;
            });

            // Build per-group view wrappers — IsNow is scoped to this group.
            foreach (var n in group.Nodes)
                group.NodeViews.Add(new ProxyNodeView(group, n));

            groups.Add(group);
        }

        // Sort groups using the GLOBAL group's All list, which reflects the config-file definition order.
        if (allEntries.TryGetValue("GLOBAL", out var globalEntry) && globalEntry.All is { Count: > 0 })
        {
            var order = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < globalEntry.All.Count; i++)
                order.TryAdd(globalEntry.All[i], i);
            groups.Sort((a, b) =>
            {
                int ai = order.TryGetValue(a.Name, out var av) ? av : int.MaxValue;
                int bi = order.TryGetValue(b.Name, out var bv) ? bv : int.MaxValue;
                return ai.CompareTo(bi);
            });
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
        var body = new StringContent(
            $"{{\"name\":{JsonStr(proxyName)}}}",
            Encoding.UTF8, "application/json");
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
            var resp = await Http.GetFromJsonAsync(url, AppJsonContext.Default.DelayResponse, ct);
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
        return await Http.GetFromJsonAsync("/configs", AppJsonContext.Default.MihomoConfig, ct);
    }

    /// <summary>
    /// Changes the proxy mode (rule / global / direct).
    /// </summary>
    public async Task<bool> SetModeAsync(string mode, CancellationToken ct = default)
    {
        var body = new StringContent(
            $"{{\"mode\":{JsonStr(mode)}}}",
            Encoding.UTF8, "application/json");
        var resp = await Http.PatchAsync("/configs", body, ct);
        return resp.IsSuccessStatusCode;
    }

    /// <summary>
    /// Enables or disables TUN mode via PATCH /configs.
    /// Returns (success, errorMessage).
    /// </summary>
    public async Task<(bool ok, string error)> SetTunAsync(bool enable, CancellationToken ct = default)
    {
        var enableStr = enable ? "true" : "false";
        var body = new StringContent(
            $"{{\"tun\":{{\"enable\":{enableStr},\"stack\":\"system\"}}}}",
            Encoding.UTF8, "application/json");
        var resp = await Http.PatchAsync("/configs", body, ct);
        if (resp.IsSuccessStatusCode) return (true, string.Empty);
        var msg = await resp.Content.ReadAsStringAsync(ct);
        return (false, $"HTTP {(int)resp.StatusCode}: {msg}");
    }

    // Built-in leaf proxy names that are never real groups.
    private static readonly HashSet<string> _builtinProxies =
        new(StringComparer.OrdinalIgnoreCase) { "DIRECT", "REJECT", "REJECT-DROP", "PASS", "COMPATIBLE" };

    // Keywords used to identify the primary selector group in rule mode.
    private static readonly string[] _primaryKeywords =
        ["节点选择", "select", "proxy", "auto"];

    /// <summary>
    /// Returns the active (group, node) pair.
    /// In rule mode: finds the primary selector group (by name keywords) and returns its Now.
    /// In other modes: follows GLOBAL → Now → group.Now.
    /// Returns ("", "") when the info is unavailable.
    /// </summary>
    public async Task<(string group, string node)> GetCurrentProxyAsync(CancellationToken ct = default)
    {
        try
        {
            var resp = await Http.GetFromJsonAsync("/proxies", AppJsonContext.Default.ProxiesResponse, ct);
            if (resp == null) return ("", "");

            var config = await GetConfigAsync(ct);
            var mode = config?.Mode?.ToLowerInvariant() ?? "rule";

            if (mode == "rule")
            {
                // In rule mode, look for the primary selector group by keyword priority.
                var groups = resp.Proxies.Values
                    .Where(p => p.All != null && !_builtinProxies.Contains(p.Name))
                    .ToList();

                var primary = _primaryKeywords
                    .SelectMany(kw => groups.Where(g =>
                        g.Name.Contains(kw, StringComparison.OrdinalIgnoreCase)))
                    .FirstOrDefault()
                    ?? groups.FirstOrDefault();

                if (primary == null) return ("", "");
                var nodeName = primary.Now ?? "";
                return (primary.Name, string.IsNullOrEmpty(nodeName) ? primary.Name : nodeName);
            }
            else
            {
                // Global / direct mode: follow GLOBAL → Now.
                if (!resp.Proxies.TryGetValue("GLOBAL", out var global)) return ("", "");
                var topGroupName = global.Now ?? "";
                if (string.IsNullOrEmpty(topGroupName)) return ("", "");
                if (_builtinProxies.Contains(topGroupName)) return ("GLOBAL", topGroupName);
                if (!resp.Proxies.TryGetValue(topGroupName, out var topGroup)) return (topGroupName, topGroupName);
                var nodeName = topGroup.Now ?? topGroupName;
                return (topGroupName, nodeName);
            }
        }
        catch { return ("", ""); }
    }

    // ── REST API: Connections ─────────────────────────────────────────────────

    public async Task<ConnectionsResponse?> GetConnectionsAsync(CancellationToken ct = default)
    {
        return await Http.GetFromJsonAsync("/connections", AppJsonContext.Default.ConnectionsResponse, ct);
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
        var resp = await Http.GetFromJsonAsync("/rules", AppJsonContext.Default.RulesResponse, ct);
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
            var item = JsonSerializer.Deserialize(json, AppJsonContext.Default.LogItem);
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
            var snap = JsonSerializer.Deserialize(json, AppJsonContext.Default.ConnectionsResponse);
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
            var item = JsonSerializer.Deserialize(json, AppJsonContext.Default.TrafficItem);
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



    /// <summary>Encodes a string as a JSON string literal (with surrounding quotes).</summary>
    private static string JsonStr(string s) =>
        "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
