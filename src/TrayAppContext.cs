using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using Microsoft.Win32;

namespace ClaudeMeter;

/// <summary>Message-only window receiving WM_HOTKEY.</summary>
sealed class HotkeyWindow : NativeWindow, IDisposable
{
    const int WM_HOTKEY = 0x0312;
    const int HotkeyId = 1;
    const uint MOD_ALT = 0x1, MOD_CONTROL = 0x2;

    public event Action? Pressed;
    public bool Registered { get; private set; }

    public HotkeyWindow()
    {
        CreateHandle(new CreateParams());
        Registered = RegisterHotKey(Handle, HotkeyId, MOD_CONTROL | MOD_ALT, (uint)Keys.U);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY) Pressed?.Invoke();
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (Registered) UnregisterHotKey(Handle, HotkeyId);
        DestroyHandle();
    }

    [DllImport("user32.dll")]
    static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}

sealed class TrayAppContext : ApplicationContext
{
    const int PollIntervalMs = 180_000; // endpoint rate-limits aggressively below ~180 s
    const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunValueName = "ClaudeMeter";

    readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    readonly UsageClient _usage;
    readonly NotifyIcon _trayIcon;
    readonly PopupForm _popup = new();
    readonly System.Windows.Forms.Timer _pollTimer;
    readonly ToolStripMenuItem _autostartItem;
    readonly ToolStripMenuItem _alwaysOnTopItem;
    readonly ToolStripMenuItem _clickThroughItem;
    readonly ToolStripMenuItem _showGraphItem;
    readonly ToolStripMenuItem _showLogoItem;
    readonly Dictionary<string, ToolStripMenuItem> _sizeItems = new();
    readonly Dictionary<int, ToolStripMenuItem> _opacityItems = new();
    readonly Dictionary<int, ToolStripMenuItem> _notifyItems = new();
    readonly Dictionary<int, ToolStripMenuItem> _rangeItems = new();
    readonly Dictionary<int, ToolStripMenuItem> _nowPosItems = new();
    readonly Dictionary<string, ToolStripMenuItem> _themeItems = new();
    readonly ToolStripMenuItem _hotkeyItem;
    readonly ToolStripMenuItem _updateCheckItem;
    readonly ToolStripMenuItem _updateAvailableItem;
    readonly ToolStripMenuItem _fixLoginItem;
    HotkeyWindow? _hotkeyWindow;
    DateTimeOffset _lastUpdateCheck = DateTimeOffset.MinValue;
    string? _updateUrl;
    readonly AppSettings _settings = AppSettings.Load();
    readonly UsageHistory _history = new();
    readonly HashSet<string> _notified = new();

    UsageSnapshot? _lastSnapshot;
    string? _lastError;
    bool _fetching;
    Icon? _currentIcon;

    public TrayAppContext()
    {
        _usage = new UsageClient(_http, new CredentialStore(_http));

        _autostartItem = new ToolStripMenuItem("Start with Windows", null, OnToggleAutostart)
        {
            Checked = IsAutostartEnabled(),
            CheckOnClick = false,
        };

        _alwaysOnTopItem = new ToolStripMenuItem("Always on top", null, OnToggleAlwaysOnTop)
        {
            Checked = _settings.AlwaysOnTop,
        };

        var sizeMenu = new ToolStripMenuItem("Size");
        foreach (var size in new[] { "small", "medium", "big" })
        {
            var item = new ToolStripMenuItem(char.ToUpperInvariant(size[0]) + size[1..], null, (_, _) => SetPopupSize(size))
            {
                Checked = _settings.PopupSize == size,
            };
            _sizeItems[size] = item;
            sizeMenu.DropDownItems.Add(item);
        }

        _clickThroughItem = new ToolStripMenuItem("Pin (Click-through)", null, OnToggleClickThrough)
        {
            Checked = _settings.ClickThrough,
        };

        var opacityMenu = new ToolStripMenuItem("Opacity");
        foreach (var pct in new[] { 100, 85, 70, 55, 40 })
        {
            var item = new ToolStripMenuItem($"{pct}%", null, (_, _) => SetOpacity(pct))
            {
                Checked = _settings.OpacityPercent == pct,
            };
            _opacityItems[pct] = item;
            opacityMenu.DropDownItems.Add(item);
        }

        var notifyMenu = new ToolStripMenuItem("Notify at");
        foreach (var threshold in new[] { 0, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95 })
        {
            int t = threshold;
            var item = new ToolStripMenuItem(t == 0 ? "Off" : $"{t}%", null, (_, _) => SetNotifyThreshold(t))
            {
                Checked = _settings.NotifyThreshold == t,
            };
            _notifyItems[t] = item;
            notifyMenu.DropDownItems.Add(item);
        }

        var themeMenu = new ToolStripMenuItem("Theme");
        foreach (var (key, label) in new[] { ("dark", "Dark"), ("light", "Light") })
        {
            string k = key;
            var item = new ToolStripMenuItem(label, null, (_, _) => SetTheme(k))
            {
                Checked = _settings.Theme == k,
            };
            _themeItems[k] = item;
            themeMenu.DropDownItems.Add(item);
        }

        _hotkeyItem = new ToolStripMenuItem("Hotkey (Ctrl+Alt+U)", null, OnToggleHotkey)
        {
            Checked = _settings.HotkeyEnabled,
        };

        _updateCheckItem = new ToolStripMenuItem("Check for updates", null, OnToggleUpdateCheck)
        {
            Checked = _settings.CheckUpdates,
        };

        _updateAvailableItem = new ToolStripMenuItem("⬆ Update available…", null, (_, _) => OpenUpdatePage())
        {
            Visible = false,
        };

        _fixLoginItem = new ToolStripMenuItem("🔑 Fix Claude login…", null, (_, _) => OpenLoginTerminal())
        {
            Visible = false, // shown only while the login is broken
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add(_updateAvailableItem);
        menu.Items.Add(_fixLoginItem);
        menu.Items.Add(new ToolStripMenuItem("Refresh now", null, async (_, _) => await RefreshAsync()));
        menu.Items.Add(new ToolStripSeparator());
        _showGraphItem = new ToolStripMenuItem("Show Usage Remaining Graph", null, OnToggleShowGraph)
        {
            Checked = _settings.ShowRemainingGraph,
        };

        _showLogoItem = new ToolStripMenuItem("Show logo", null, OnToggleShowLogo)
        {
            Checked = _settings.ShowLogo,
        };

        menu.Items.Add(_alwaysOnTopItem);
        menu.Items.Add(_clickThroughItem);
        var rangeMenu = new ToolStripMenuItem("Graph range");
        foreach (var hours in new[] { 24, 12 })
        {
            int h = hours;
            var item = new ToolStripMenuItem($"{h}h", null, (_, _) => SetGraphRange(h))
            {
                Checked = _settings.GraphRangeHours == h,
            };
            _rangeItems[h] = item;
            rangeMenu.DropDownItems.Add(item);
        }

        var nowPosMenu = new ToolStripMenuItem("Now position");
        foreach (var (pct, label) in new[] { (50, "Center"), (75, "3/4"), (100, "Right (past only)") })
        {
            int p = pct;
            var item = new ToolStripMenuItem(label, null, (_, _) => SetNowPosition(p))
            {
                Checked = _settings.NowPositionPercent == p,
            };
            _nowPosItems[p] = item;
            nowPosMenu.DropDownItems.Add(item);
        }

        menu.Items.Add(_showGraphItem);
        menu.Items.Add(rangeMenu);
        menu.Items.Add(nowPosMenu);
        menu.Items.Add(_showLogoItem);
        menu.Items.Add(sizeMenu);
        menu.Items.Add(opacityMenu);
        menu.Items.Add(themeMenu);
        menu.Items.Add(notifyMenu);
        menu.Items.Add(_hotkeyItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_autostartItem);
        menu.Items.Add(_updateCheckItem);
        menu.Items.Add(new ToolStripMenuItem("About…", null, (_, _) => ShowAbout()));
        menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => ExitThread()));

        // restore persisted popup state
        _popup.History = _history;
        _popup.ShowRemainingGraph = _settings.ShowRemainingGraph;
        _popup.GraphRangeHours = _settings.GraphRangeHours;
        _popup.NowPositionPercent = _settings.NowPositionPercent;
        _popup.ShowLogo = _settings.ShowLogo;
        Theme.Light = _settings.Theme == "light";
        _popup.ApplyTheme();
        _popup.ApplyScale(_settings.Scale);
        _popup.SetBaseOpacity(_settings.OpacityPercent / 100.0);
        _popup.Pinned = _settings.AlwaysOnTop;
        _popup.ClickThrough = _settings.AlwaysOnTop && _settings.ClickThrough;
        if (_settings.PinnedX is { } px && _settings.PinnedY is { } py)
            _popup.PinnedLocation = new Point(px, py);
        _popup.UserMoved += () =>
        {
            _settings.PinnedX = _popup.PinnedLocation?.X;
            _settings.PinnedY = _popup.PinnedLocation?.Y;
            _settings.Save();
        };

        _trayIcon = new NotifyIcon
        {
            ContextMenuStrip = menu,
            Visible = true,
            Text = "Claude Meter — loading…",
        };
        SetIcon(null);
        _trayIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) TogglePopup();
        };

        // these can show balloon tips, so they must come after _trayIcon exists
        ApplyHotkey();
        if (_settings.CheckUpdates) _ = CheckUpdatesAsync(notifyBalloon: true);

        _pollTimer = new System.Windows.Forms.Timer { Interval = PollIntervalMs };
        _pollTimer.Tick += async (_, _) => await RefreshAsync();
        _pollTimer.Start();

        if (!IsAutostartEnabled()) EnableAutostart(); // default on first run, per plan
        _autostartItem.Checked = IsAutostartEnabled();

        _ = RefreshAsync();

        if (_settings.AlwaysOnTop) _popup.ShowNearTray();
    }

    void OnToggleAlwaysOnTop(object? sender, EventArgs e)
    {
        _settings.AlwaysOnTop = !_settings.AlwaysOnTop;
        _alwaysOnTopItem.Checked = _settings.AlwaysOnTop;
        _popup.Pinned = _settings.AlwaysOnTop;
        _popup.ClickThrough = _settings.AlwaysOnTop && _settings.ClickThrough;
        _settings.Save();

        if (_settings.AlwaysOnTop && !_popup.Visible) _popup.ShowNearTray();
        else if (!_settings.AlwaysOnTop && _popup.Visible) _popup.Hide();
    }

    void OnToggleClickThrough(object? sender, EventArgs e)
    {
        _settings.ClickThrough = !_settings.ClickThrough;
        _clickThroughItem.Checked = _settings.ClickThrough;
        _popup.ClickThrough = _settings.AlwaysOnTop && _settings.ClickThrough;
        _settings.Save();
    }

    void OnToggleShowGraph(object? sender, EventArgs e)
    {
        _settings.ShowRemainingGraph = !_settings.ShowRemainingGraph;
        _showGraphItem.Checked = _settings.ShowRemainingGraph;
        _popup.ShowRemainingGraph = _settings.ShowRemainingGraph;
        _settings.Save();
    }

    void OnToggleShowLogo(object? sender, EventArgs e)
    {
        _settings.ShowLogo = !_settings.ShowLogo;
        _showLogoItem.Checked = _settings.ShowLogo;
        _popup.ShowLogo = _settings.ShowLogo;
        _settings.Save();
    }

    AboutForm? _aboutForm;

    void ShowAbout()
    {
        if (_aboutForm is { IsDisposed: false })
        {
            _aboutForm.Activate();
            return;
        }
        _aboutForm = new AboutForm();
        _aboutForm.Show();
    }

    void SetGraphRange(int hours)
    {
        _settings.GraphRangeHours = hours;
        foreach (var (key, item) in _rangeItems) item.Checked = key == hours;
        _popup.GraphRangeHours = hours;
        _settings.Save();
    }

    void SetTheme(string theme)
    {
        _settings.Theme = theme;
        Theme.Light = theme == "light";
        foreach (var (key, item) in _themeItems) item.Checked = key == theme;
        _popup.ApplyTheme();
        _aboutForm?.Close(); // reopens with the new palette
        _settings.Save();
    }

    void OnToggleHotkey(object? sender, EventArgs e)
    {
        _settings.HotkeyEnabled = !_settings.HotkeyEnabled;
        _hotkeyItem.Checked = _settings.HotkeyEnabled;
        ApplyHotkey();
        _settings.Save();
    }

    void ApplyHotkey()
    {
        if (_settings.HotkeyEnabled && _hotkeyWindow is null)
        {
            _hotkeyWindow = new HotkeyWindow();
            _hotkeyWindow.Pressed += TogglePopup;
            if (!_hotkeyWindow.Registered)
                _trayIcon.ShowBalloonTip(4000, "Claude Meter",
                    "Ctrl+Alt+U is already in use by another app.", ToolTipIcon.Warning);
        }
        else if (!_settings.HotkeyEnabled && _hotkeyWindow is not null)
        {
            _hotkeyWindow.Dispose();
            _hotkeyWindow = null;
        }
    }

    void OnToggleUpdateCheck(object? sender, EventArgs e)
    {
        _settings.CheckUpdates = !_settings.CheckUpdates;
        _updateCheckItem.Checked = _settings.CheckUpdates;
        _settings.Save();
        if (_settings.CheckUpdates)
        {
            _lastUpdateCheck = DateTimeOffset.MinValue; // force an immediate re-check
            _ = CheckUpdatesAsync(notifyBalloon: true);
        }
    }

    const string LatestReleaseApi = "https://api.github.com/repos/SKGoC-CLI/claude-meter-for-windows/releases/latest";

    async Task CheckUpdatesAsync(bool notifyBalloon)
    {
        if (DateTimeOffset.Now - _lastUpdateCheck < TimeSpan.FromHours(20)) return;
        _lastUpdateCheck = DateTimeOffset.Now;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseApi);
            request.Headers.TryAddWithoutValidation("User-Agent", "ClaudeMeter");
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return; // repo or release may not exist yet

            var root = JsonNode.Parse(await response.Content.ReadAsStringAsync());
            string? tag = root?["tag_name"]?.GetValue<string>();
            string? url = root?["html_url"]?.GetValue<string>();
            if (tag is null || url is null) return;
            if (!Version.TryParse(tag.TrimStart('v', 'V'), out var remote)) return;

            var current = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0);
            if (remote <= current) return;

            _updateUrl = url;
            _updateAvailableItem.Text = $"⬆ Update available: {tag}";
            _updateAvailableItem.Visible = true;
            if (notifyBalloon)
                _trayIcon.ShowBalloonTip(5000, "Claude Meter", $"New version {tag} is available.", ToolTipIcon.Info);
        }
        catch
        {
            // update check is best-effort; never disturb the app
        }
    }

    void OpenUpdatePage()
    {
        if (_updateUrl is null) return;
        try
        {
            Process.Start(new ProcessStartInfo(_updateUrl) { UseShellExecute = true });
        }
        catch
        {
        }
    }

    void SetNowPosition(int percent)
    {
        _settings.NowPositionPercent = percent;
        foreach (var (key, item) in _nowPosItems) item.Checked = key == percent;
        _popup.NowPositionPercent = percent;
        _settings.Save();
    }

    void SetNotifyThreshold(int threshold)
    {
        _settings.NotifyThreshold = threshold;
        foreach (var (key, item) in _notifyItems) item.Checked = key == threshold;
        _notified.Clear(); // re-arm against the new threshold
        _settings.Save();
    }

    void SetOpacity(int pct)
    {
        _settings.OpacityPercent = pct;
        foreach (var (key, item) in _opacityItems) item.Checked = key == pct;
        _popup.SetBaseOpacity(pct / 100.0);
        _settings.Save();
    }

    void SetPopupSize(string size)
    {
        _settings.PopupSize = size;
        foreach (var (key, item) in _sizeItems) item.Checked = key == size;
        _popup.ApplyScale(_settings.Scale);
        _settings.Save();
    }

    void TogglePopup()
    {
        // refresh on open when data is older than 60 s, but never hammer the endpoint
        if (!_popup.Visible && !_fetching &&
            (_lastSnapshot is null || DateTimeOffset.Now - _lastSnapshot.FetchedAt > TimeSpan.FromSeconds(60)))
        {
            _ = RefreshAsync();
        }
        _popup.ToggleNearTray();
    }

    async Task RefreshAsync()
    {
        if (_fetching) return;
        _fetching = true;
        try
        {
            var snapshot = await _usage.FetchAsync();
            _lastSnapshot = snapshot;
            _lastError = null;
            _history.Add(snapshot);
            NotifyIfNearLimit(snapshot);
        }
        catch (UsageException ex)
        {
            _lastError = ex.Message;
            if (ex.RetryAfter is { } retry && retry > TimeSpan.FromMilliseconds(PollIntervalMs))
            {
                // back off: skip polls until the endpoint says we may retry
                _pollTimer.Interval = (int)Math.Min(retry.TotalMilliseconds, int.MaxValue);
            }
        }
        catch (Exception ex)
        {
            _lastError = "Network error: " + ex.Message;
        }
        finally
        {
            _fetching = false;
            if (_pollTimer.Interval != PollIntervalMs && _lastError is null)
                _pollTimer.Interval = PollIntervalMs;
            _popup.NextUpdateAt = DateTimeOffset.Now + TimeSpan.FromMilliseconds(_pollTimer.Interval);
            UpdateUi();
            if (_settings.CheckUpdates) _ = CheckUpdatesAsync(notifyBalloon: true); // throttled to ~daily internally
        }
    }

    void NotifyIfNearLimit(UsageSnapshot snapshot)
    {
        int threshold = _settings.NotifyThreshold;
        if (threshold <= 0) return;

        foreach (var w in snapshot.Windows)
        {
            if (w.Utilization >= threshold)
            {
                if (_notified.Add(w.Key))
                    _trayIcon.ShowBalloonTip(5000, "Claude Meter",
                        $"{w.Label} usage at {Math.Round(w.Utilization)}%", ToolTipIcon.Warning);
            }
            else if (w.Utilization < threshold - 5)
            {
                _notified.Remove(w.Key); // re-arm after the window resets
            }
        }
    }

    static bool IsLoginError(string? error) =>
        error is not null &&
        (error.Contains("login", StringComparison.OrdinalIgnoreCase) ||
         error.Contains("Token rejected", StringComparison.OrdinalIgnoreCase));

    /// <summary>Opens a terminal running the Claude CLI so the user can /login.</summary>
    void OpenLoginTerminal()
    {
        try
        {
            Process.Start(new ProcessStartInfo("cmd.exe", "/k claude")
            {
                UseShellExecute = true,
            });
            _trayIcon.ShowBalloonTip(6000, "Claude Meter",
                "Type /login in the terminal, sign in, then click Refresh now.", ToolTipIcon.Info);
        }
        catch
        {
            _trayIcon.ShowBalloonTip(6000, "Claude Meter",
                "Could not open a terminal. Run 'claude' manually and /login.", ToolTipIcon.Warning);
        }
    }

    void UpdateUi()
    {
        bool stale = _lastError is not null && _lastSnapshot is not null;
        _popup.UpdateData(_lastSnapshot, _lastError, stale);
        _fixLoginItem.Visible = IsLoginError(_lastError);

        double? max = _lastSnapshot?.Windows.Max(w => w.Utilization);
        SetIcon(max);

        string tooltip = _lastSnapshot is null
            ? "Claude Meter — " + (_lastError is null ? "loading…" : "error")
            : string.Join("\n", _lastSnapshot.Windows.Select(w => $"{w.Label}: {Math.Round(w.Utilization)}%"));
        // NotifyIcon.Text caps at 127 chars
        _trayIcon.Text = tooltip.Length > 127 ? tooltip[..127] : tooltip;
    }

    void SetIcon(double? maxUtilization)
    {
        var old = _currentIcon;
        _currentIcon = IconRenderer.Render(maxUtilization);
        _trayIcon.Icon = _currentIcon;
        old?.Dispose();
    }

    // -- autostart ---------------------------------------------------------

    static string ExePath => Application.ExecutablePath;

    static bool IsAutostartEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return string.Equals(key?.GetValue(RunValueName) as string, $"\"{ExePath}\"", StringComparison.OrdinalIgnoreCase);
    }

    static void EnableAutostart()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        key.SetValue(RunValueName, $"\"{ExePath}\"");
    }

    static void DisableAutostart()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(RunValueName, throwOnMissingValue: false);
    }

    void OnToggleAutostart(object? sender, EventArgs e)
    {
        if (IsAutostartEnabled()) DisableAutostart();
        else EnableAutostart();
        _autostartItem.Checked = IsAutostartEnabled();
    }

    protected override void ExitThreadCore()
    {
        _hotkeyWindow?.Dispose();
        _pollTimer.Stop();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _popup.Dispose();
        _currentIcon?.Dispose();
        _http.Dispose();
        base.ExitThreadCore();
    }
}
