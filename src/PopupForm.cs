using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace ClaudeMeter;

/// <summary>
/// Popup anchored above the tray: logo header, one row per usage window
/// (label, percentage, progress bar, reset countdown) and an optional
/// usage-remaining chart. Fully owner-drawn. Supports pinned (always-on-top,
/// draggable) mode, scaling, dark/light theme, opacity with hover restore,
/// and click-through.
/// </summary>
sealed class PopupForm : Form
{
    static Color Background => Theme.Background;
    static Color TrackColor => Theme.Track;
    static Color LabelColor => Theme.Label;
    static Color MutedColor => Theme.Muted;

    float _scale = 1f;
    Font _labelFont = null!;
    Font _valueFont = null!;
    Font _smallFont = null!;
    Font _smallBoldFont = null!;
    Font _tinyFont = null!;
    Font _headerFont = null!;

    UsageSnapshot? _snapshot;
    string? _error;
    bool _stale;
    double _baseOpacity = 1.0;
    bool _hovering;
    bool _clickThrough;
    bool _minimizeHover; // tracked separately so we only Invalidate() on an actual hover change

    readonly System.Windows.Forms.Timer _tick = new() { Interval = 1000 };

    /// <summary>Always-on-top mode: no auto-hide on focus loss, draggable, remembers position.</summary>
    public bool Pinned { get; set; }

    /// <summary>Where the user last dragged the pinned popup; null = default corner.</summary>
    public Point? PinnedLocation { get; set; }

    /// <summary>Raised after the user finishes dragging the pinned popup.</summary>
    public event Action? UserMoved;

    /// <summary>When the next poll will run; drawn as a live countdown in the footer.</summary>
    public DateTimeOffset? NextUpdateAt { get; set; }

    /// <summary>Usage history feeding the remaining chart (rolling 24 h).</summary>
    public UsageHistory? History { get; set; }

    bool _showRemainingGraph;
    bool _showCreditGraph;
    bool _showLogo;
    bool _miniMode;
    bool _showEta;

    /// <summary>Whether to draw the session-remaining chart at the bottom of the popup.</summary>
    public bool ShowRemainingGraph
    {
        get => _showRemainingGraph;
        set { _showRemainingGraph = value; RecomputeLayout(); }
    }

    /// <summary>Whether to draw the month-to-date credit chart below the session chart.</summary>
    public bool ShowCreditGraph
    {
        get => _showCreditGraph;
        set { _showCreditGraph = value; RecomputeLayout(); }
    }

    /// <summary>Whether to draw the logo header at the top of the popup.</summary>
    public bool ShowLogo
    {
        get => _showLogo;
        set { _showLogo = value; RecomputeLayout(); }
    }

    /// <summary>Collapses the popup to a single bar showing just 5h % and weekly %.</summary>
    public bool MiniMode
    {
        get => _miniMode;
        set { _miniMode = value; RecomputeLayout(); }
    }

    /// <summary>Whether to draw a burn-rate ETA line under each row. Changes the row
    /// height (see RowHeight), so this relayouts rather than just repainting.</summary>
    public bool ShowEta
    {
        get => _showEta;
        set { _showEta = value; RecomputeLayout(); }
    }

    IReadOnlyList<UsageWindow> _rawWindows = Array.Empty<UsageWindow>();

    /// <summary>All limit windows from the last snapshot, unfiltered by "Show limits" — mini
    /// mode always shows Session (5h) and Weekly even if the user hid those rows elsewhere.</summary>
    public IReadOnlyList<UsageWindow> RawWindows
    {
        get => _rawWindows;
        // relayout, not just repaint: the bar auto-fits its text, and "5h 100% · W 100%"
        // is wider than "5h 0% · W 3%" — a stale width would run the text under the "−"
        set { _rawWindows = value; if (_miniMode) RecomputeLayout(); }
    }

    void RecomputeLayout()
    {
        Width = ComputeWidth();
        Height = ComputeHeight();
        _fixLoginButton.Visible = _showFixLogin && !_miniMode;
        if (Visible)
        {
            if (!Pinned) Reposition();
            else ClampToScreen();
        }
        Invalidate();
    }

    // the ETA line sits at y+S(38), just under the bar; the extra height is breathing room
    // *below* it, so it reads as part of its own row instead of the next row's label
    int RowHeight => _showEta ? S(66) : S(58);

    // never shorter than the minimize button's hit area, even with the logo off,
    // so "−" never lands on top of the first row's "resets …" text
    int HeaderHeight => _showLogo && LogoStore.Logo is not null ? S(42) : S(22);

    // chart hidden while there is no current data (loading / error state)
    int GraphHeight => _showRemainingGraph && _snapshot is { Windows.Count: > 0 } ? S(162) : 0;

    /// <summary>The merged wallet row (extra_usage/spend), if the account has one and it's not hidden.
    /// Requires both amounts so the chart never reserves space it can't draw.</summary>
    UsageWindow? WalletWindow => _snapshot?.Windows.FirstOrDefault(
        w => w.Key == "extra_usage" && w.UsedDollars is not null && w.LimitDollars is > 0);

    // hidden unless the wallet row is actually present this poll
    int CreditGraphHeight => _showCreditGraph && WalletWindow is not null ? S(140) : 0;

    bool _showFixLogin;
    bool _needsRelogin; // error is a real logout (red + Fix Login) vs a transient blip (plain stale)
    readonly Button _fixLoginButton;

    IReadOnlyList<SessionContext> _sessions = Array.Empty<SessionContext>();

    /// <summary>Context-window info of the active Claude Code sessions (empty hides the section).</summary>
    public IReadOnlyList<SessionContext> Sessions
    {
        get => _sessions;
        set { _sessions = value ?? Array.Empty<SessionContext>(); RecomputeLayout(); }
    }

    int ContextHeaderHeight => S(20);
    int ContextBlockHeight => S(55);
    int ContextSectionHeight => _sessions.Count > 0 ? ContextHeaderHeight + _sessions.Count * ContextBlockHeight : 0;

    static Color ContextColor(double pct) =>
        pct >= 85 ? IconRenderer.Danger : pct >= 60 ? IconRenderer.Warning : IconRenderer.Accent;

    /// <summary>Raised when the user clicks the in-popup "Fix Claude login" button.</summary>
    public event Action? FixLoginRequested;

    int _graphRangeHours = 24;

    /// <summary>Total time-axis width in hours.</summary>
    public int GraphRangeHours
    {
        get => _graphRangeHours;
        set { _graphRangeHours = value; Invalidate(); }
    }

    int _nowPositionPercent = 75;

    /// <summary>Where "now" sits on the time axis: 50 = center, 75 = three-quarters, 100 = right edge.</summary>
    public int NowPositionPercent
    {
        get => _nowPositionPercent;
        set { _nowPositionPercent = value; Invalidate(); }
    }

    int _creditRangeDays = 30;

    /// <summary>Credit graph time-axis width in days (1 = 24 h, 3, 7, 15, 30); independent of the session graph.</summary>
    public int CreditRangeDays
    {
        get => _creditRangeDays;
        set { _creditRangeDays = value; Invalidate(); }
    }

    int _creditNowPositionPercent = 100;

    /// <summary>Where "now" sits on the credit graph's time axis; independent of the session graph.</summary>
    public int CreditNowPositionPercent
    {
        get => _creditNowPositionPercent;
        set { _creditNowPositionPercent = value; Invalidate(); }
    }

    /// <summary>Mouse clicks pass through the popup to windows behind it.</summary>
    public bool ClickThrough
    {
        get => _clickThrough;
        set
        {
            _clickThrough = value;
            if (IsHandleCreated) ApplyClickThrough();
        }
    }

    public PopupForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Background;
        DoubleBuffered = true;
        _tick.Tick += (_, _) => Invalidate(); // live countdown + "resets in" refresh

        _fixLoginButton = new Button
        {
            Text = "🔑 Fix Claude login",
            FlatStyle = FlatStyle.Flat,
            Visible = false,
        };
        _fixLoginButton.FlatAppearance.BorderSize = 0;
        _fixLoginButton.Click += (_, _) => FixLoginRequested?.Invoke();
        Controls.Add(_fixLoginButton);

        ApplyScale(1f); // also styles the fix-login button
    }

    void StyleFixLoginButton()
    {
        _fixLoginButton.BackColor = Theme.Track;
        _fixLoginButton.ForeColor = Theme.Label;
        _fixLoginButton.Font = _labelFont;
        _fixLoginButton.Size = new Size(S(190), S(34));
    }

    int S(float v) => (int)Math.Round(v * _scale);

    public void ApplyScale(float scale)
    {
        _scale = scale;
        _labelFont?.Dispose();
        _valueFont?.Dispose();
        _smallFont?.Dispose();
        _smallBoldFont?.Dispose();
        _tinyFont?.Dispose();
        _headerFont?.Dispose();
        _labelFont = new Font("Segoe UI", 10f * scale);
        _valueFont = new Font("Segoe UI", 10f * scale, FontStyle.Bold);
        _smallFont = new Font("Segoe UI", 8f * scale);
        _smallBoldFont = new Font("Segoe UI", 8f * scale, FontStyle.Bold);
        _tinyFont = new Font("Segoe UI", 7f * scale);
        _headerFont = new Font("Segoe UI", 10f * scale, FontStyle.Bold);
        StyleFixLoginButton(); // the button held the old (now disposed) label font
        Width = ComputeWidth();
        Height = ComputeHeight();
        if (Visible) Reposition();
        Invalidate();
    }

    /// <summary>Auto-fit: wide enough that label + % + reset text never collide.</summary>
    int ComputeWidth()
    {
        if (_miniMode)
        {
            using var mg = CreateGraphics();
            float textW = MiniTextRuns().Sum(r => mg.MeasureString(r.Text, r.Font).Width);
            int miniWidth = (int)Math.Ceiling(textW) + S(16) * 2 + S(22) + S(8);
            return Math.Max(miniWidth, S(150));
        }

        int min = S(320);
        if ((_snapshot is null || _snapshot.Windows.Count == 0) && _sessions.Count == 0) return min;

        using var g = CreateGraphics();
        // worst-case ETA string, used below so the ETA line's width is always budgeted for
        float etaW = ShowEta ? g.MeasureString("full in ~23h 59m", _smallFont).Width : 0;
        float widest = 0;
        foreach (var w in _snapshot?.Windows ?? Array.Empty<UsageWindow>())
        {
            float labelW = g.MeasureString(w.Label + ":", _labelFont).Width;
            float pctW = g.MeasureString("100%", _valueFont).Width;
            float resetW = 0;
            if (w.ResetsAt is { } resets)
            {
                var remaining = resets - DateTimeOffset.Now;
                if (remaining > TimeSpan.Zero)
                    resetW = g.MeasureString(ResetText(resets, remaining), _smallFont).Width;
            }
            else if (w.Key == "five_hour")
                resetW = g.MeasureString(NextUseHint, _smallFont).Width;
            else if (w.UsedDollars is { } used && w.LimitDollars is { } limit)
                resetW = g.MeasureString(MoneyText(used, limit), _smallFont).Width;
            widest = Math.Max(widest, labelW + S(2) + pctW + S(16) + resetW);
            // the ETA line sits below (same row as the collision-fallback reset text)
            widest = Math.Max(widest, etaW + S(16) + resetW);
        }

        // session-context line 1 must fit too: bold "project · model" + % on the
        // left, "current / capacity" tokens right-aligned (same layout as DrawContextBlock)
        foreach (var s in _sessions)
        {
            float nameW = g.MeasureString($"{s.Project} · {s.Model}", _smallBoldFont).Width;
            float pctW = g.MeasureString("100%", _smallBoldFont).Width;
            float tokW = g.MeasureString($"{FmtTokens(s.Tokens)} / {FmtTokens(s.WindowSize)}", _smallFont).Width;
            widest = Math.Max(widest, nameW + S(6) + pctW + S(16) + tokW);
        }

        // footer must also fit: "Updated HH:mm · stale" + countdown, right-aligned
        float footerLeftW = g.MeasureString("Updated 88:88  ·  ⚠ stale", _smallFont).Width;
        float footerRightW = g.MeasureString("Usage will update in next 8:88", _smallFont).Width;
        widest = Math.Max(widest, footerLeftW + S(16) + footerRightW);

        int width = (int)Math.Ceiling(widest) + S(16) * 2;
        return Math.Clamp(width, min, S(560));
    }

    const string NextUseHint = "resets 5h after next use";

    static string MoneyText(double used, double limit) => $"${used:0.00} / ${limit:0.00}";

    static string ResetText(DateTimeOffset resets, TimeSpan remaining) =>
        remaining.TotalHours >= 24
            ? $"resets in {(int)remaining.TotalDays}d {remaining.Hours}h ({resets:ddd HH:mm})"
            : $"resets in {(int)remaining.TotalHours}h {remaining.Minutes}m ({resets:HH:mm})";

    public void ApplyTheme()
    {
        BackColor = Theme.Background;
        StyleFixLoginButton();
        Invalidate();
    }

    public void SetBaseOpacity(double opacity)
    {
        _baseOpacity = opacity;
        if (!_hovering) Opacity = opacity;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hovering = true;
        Opacity = 1.0;
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hovering = false;
        Opacity = _baseOpacity;
        if (_minimizeHover) { _minimizeHover = false; Invalidate(); } // no MouseMove fires once the cursor is gone
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (Visible) _tick.Start();
        else _tick.Stop();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // Win11 rounded corners; harmless no-op on Win10
        int preference = 2; // DWMWCP_ROUND
        DwmSetWindowAttribute(Handle, 33 /*DWMWA_WINDOW_CORNER_PREFERENCE*/, ref preference, sizeof(int));
        ApplyClickThrough();
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        if (!Pinned) Hide();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            Hide();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // -- click-through ------------------------------------------------------

    const int GWL_EXSTYLE = -20;
    const int WS_EX_TRANSPARENT = 0x20;
    const int WS_EX_LAYERED = 0x80000;

    void ApplyClickThrough()
    {
        int ex = GetWindowLong(Handle, GWL_EXSTYLE);
        ex = _clickThrough ? ex | WS_EX_TRANSPARENT | WS_EX_LAYERED : ex & ~WS_EX_TRANSPARENT;
        SetWindowLong(Handle, GWL_EXSTYLE, ex);
    }

    // -- pinned-mode dragging ----------------------------------------------

    const int WM_NCLBUTTONDOWN = 0xA1;
    const int HTCAPTION = 0x2;
    const int WM_EXITSIZEMOVE = 0x232;

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;

        // check the minimize hit area FIRST — otherwise a pinned popup would hand the
        // click to Windows as a title-bar drag before we ever see it was "−"
        if (MinimizeRect().Contains(e.Location))
        {
            Hide();
            return;
        }

        if (Pinned)
        {
            // hand the drag to Windows: the whole form acts as a title bar
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        bool hover = MinimizeRect().Contains(e.Location);
        if (hover != _minimizeHover)
        {
            _minimizeHover = hover;
            Invalidate(); // only repaint on an actual hover-state change
        }
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg == WM_EXITSIZEMOVE && Pinned)
        {
            PinnedLocation = Location;
            UserMoved?.Invoke();
        }
    }

    // -- data & layout ------------------------------------------------------

    public void UpdateData(UsageSnapshot? snapshot, string? error, bool stale, bool needsRelogin = false)
    {
        _snapshot = snapshot;
        _error = error;
        _stale = stale;
        _needsRelogin = needsRelogin;
        _showFixLogin = needsRelogin && snapshot is null; // full-error view only
        Width = ComputeWidth();
        Height = ComputeHeight();

        _fixLoginButton.Visible = _showFixLogin && !_miniMode;
        if (_showFixLogin)
            _fixLoginButton.Location = new Point(
                (Width - _fixLoginButton.Width) / 2,
                S(16) + HeaderHeight + S(58));

        if (Visible)
        {
            if (!Pinned) Reposition();      // unpinned popup re-anchors above the tray
            else ClampToScreen();           // pinned popup stays put, but must not grow off-screen
        }
        Invalidate();
    }

    void ClampToScreen()
    {
        var area = Screen.FromPoint(Location).WorkingArea;
        Location = new Point(
            Math.Max(area.Left, Math.Min(Location.X, area.Right - Width)),
            Math.Max(area.Top, Math.Min(Location.Y, area.Bottom - Height)));
    }

    int ComputeHeight()
    {
        if (_miniMode) return S(34);

        int rows = _snapshot?.Windows.Count ?? 0;
        // zero rows with data and no error means every limit is hidden by choice
        int body = rows > 0 ? rows * RowHeight
            : (_snapshot is not null && _error is null) ? S(4)
            : S(64); // space for error/loading text
        if (_showFixLogin) body += S(48);               // room for the fix-login button
        return S(16) + HeaderHeight + body + ContextSectionHeight + GraphHeight + CreditGraphHeight + S(26) + S(8);
    }

    public void ShowNearTray()
    {
        Reposition();
        Show();
        Activate();
    }

    public void ToggleNearTray()
    {
        if (Visible) Hide();
        else ShowNearTray();
    }

    void Reposition()
    {
        if (Pinned && PinnedLocation is { } p && IsVisibleOnAnyScreen(p))
        {
            Location = p;
            return;
        }
        var area = Screen.PrimaryScreen!.WorkingArea;
        Location = new Point(area.Right - Width - 12, area.Bottom - Height - 12);
    }

    bool IsVisibleOnAnyScreen(Point p)
    {
        var rect = new Rectangle(p, Size);
        // require a reasonable grab area so the popup can't get stranded off-screen
        return Screen.AllScreens.Any(s =>
        {
            var overlap = Rectangle.Intersect(s.WorkingArea, rect);
            return overlap.Width >= 60 && overlap.Height >= 30;
        });
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Background);

        if (_miniMode)
        {
            DrawMiniBar(g);
            DrawMinimizeButton(g);
            return;
        }

        int pad = S(16);
        int y = pad;
        int contentWidth = Width - pad * 2;

        // the header band always reserves at least S(22) (see HeaderHeight) so the
        // minimize button never lands on the first row — the logo itself is still
        // gated on _showLogo, so an empty band draws nothing here
        if (_showLogo && LogoStore.Logo is not null)
        {
            var logo = LogoStore.Logo!;
            int logoSize = S(30);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(logo, pad, y, logoSize, logoSize);
            g.InterpolationMode = InterpolationMode.Default;
            g.PixelOffsetMode = PixelOffsetMode.Default;

            using var titleBrush = new SolidBrush(LabelColor);
            var titleSize = g.MeasureString("Claude Usage Meter", _headerFont);
            g.DrawString("Claude Usage Meter", _headerFont, titleBrush,
                pad + logoSize + S(8), y + (logoSize - titleSize.Height) / 2f);
        }
        y += HeaderHeight;

        if (_snapshot is not null && _error is null && _snapshot.Windows.Count == 0)
        {
            y += S(4); // all limits hidden by user choice — just a small gap
        }
        else if (_snapshot is null || _snapshot.Windows.Count == 0)
        {
            string msg = _error ?? "Loading…";
            Color msgColor = _error is null ? MutedColor
                : _needsRelogin ? IconRenderer.Danger : IconRenderer.Warning;
            using var brush = new SolidBrush(msgColor);
            g.DrawString(msg, _labelFont, brush,
                new RectangleF(pad, y, contentWidth, S(28)));

            // when the last successful contact is known, show it under the error
            if (_error is not null && History?.LastSampleTime() is { } lastOk)
            {
                string lastText = "Last session at " +
                    (lastOk.Date == DateTimeOffset.Now.Date ? lastOk.ToString("HH:mm") : lastOk.ToString("d MMM HH:mm"));
                using var lastBrush = new SolidBrush(MutedColor);
                g.DrawString(lastText, _smallFont, lastBrush, pad, y + S(30));
            }

            y += S(64);
            if (_showFixLogin) y += S(48);
        }
        else
        {
            foreach (var w in _snapshot.Windows)
            {
                DrawRow(g, w, y, pad, contentWidth);
                y += RowHeight;
            }
        }

        if (ContextSectionHeight > 0)
        {
            DrawContextSection(g, y, pad, contentWidth);
            y += ContextSectionHeight;
        }

        if (GraphHeight > 0) DrawRemainingChart(g, pad, y, contentWidth);
        y += GraphHeight;

        if (CreditGraphHeight > 0) DrawCreditGraph(g, pad, y, contentWidth);

        DrawFooter(g, pad, contentWidth);

        // drawn last so it stays on top even over the fix-login error view
        DrawMinimizeButton(g);
    }

    /// <summary>Hit area for the "−" button, shared by paint and hit-testing so they can't drift.
    /// Same x formula in every state; y is fixed near the top normally, or vertically
    /// centred in the collapsed mini bar.</summary>
    Rectangle MinimizeRect()
    {
        int size = S(22);
        int x = Width - S(16) - size;
        int y = _miniMode ? (S(34) - size) / 2 : S(8);
        return new Rectangle(x, y, size, size);
    }

    void DrawMinimizeButton(Graphics g)
    {
        var rect = MinimizeRect();
        if (_minimizeHover)
            using (var trackBrush = new SolidBrush(TrackColor))
                FillRounded(g, trackBrush, rect, S(4));

        using var dashBrush = new SolidBrush(_minimizeHover ? LabelColor : MutedColor);
        int dashW = S(10), dashH = Math.Max(2, S(2));
        g.FillRectangle(dashBrush, rect.X + (rect.Width - dashW) / 2f, rect.Y + (rect.Height - dashH) / 2f, dashW, dashH);
    }

    /// <summary>Mini-bar text as an ordered run list, so paint and width-measurement read
    /// the exact same source and can never disagree on what fits.</summary>
    List<(string Text, Font Font, Color Color)> MiniTextRuns()
    {
        if (_rawWindows.Count == 0)
            return new() { ("Claude Meter", _labelFont, MutedColor) };

        double? five = _rawWindows.FirstOrDefault(w => w.Key == "five_hour")?.Utilization;
        // "highest weekly" mirrors TrayAppContext.TrayTwoRowValues() — keep the two in sync
        var weeklyWindows = _rawWindows.Where(w => w.Key.StartsWith("seven_day", StringComparison.Ordinal)).ToList();
        double? weekly = weeklyWindows.Count > 0 ? weeklyWindows.Max(w => w.Utilization) : null;

        (string, Font, Color) ValueRun(double? v) =>
            v is { } n ? ($"{Math.Round(n)}%", _labelFont, IconRenderer.ColorFor(n)) : ("—", _labelFont, MutedColor);

        return new()
        {
            ("5h ", _smallFont, MutedColor),
            ValueRun(five),
            (" · ", _smallFont, MutedColor),
            ("W ", _smallFont, MutedColor),
            ValueRun(weekly),
        };
    }

    void DrawMiniBar(Graphics g)
    {
        float x = S(16);
        float centerY = Height / 2f;
        foreach (var (text, font, color) in MiniTextRuns())
        {
            var size = g.MeasureString(text, font);
            using var brush = new SolidBrush(color);
            g.DrawString(text, font, brush, x, centerY - size.Height / 2f);
            x += size.Width;
        }
    }

    void DrawFooter(Graphics g, int pad, int contentWidth)
    {
        float footerY = Height - S(26) - S(4);

        // A real logout replaces the footer with the error; a transient blip keeps the
        // "Updated HH:mm · ⚠ stale" line so saved usage still reads as usable, just old.
        bool showErrorFooter = _error is not null && _snapshot is not null && _needsRelogin;
        string footer = _snapshot is null
            ? ""
            : $"Updated {_snapshot.FetchedAt:HH:mm}" + (_stale ? "  ·  ⚠ stale" : "");
        if (showErrorFooter)
            footer = "⚠ " + Truncate(_error!.Replace('\n', ' '), 32);
        // red for a real logout that needs the user; amber for merely-stale saved data
        using var footerBrush = new SolidBrush(
            showErrorFooter ? IconRenderer.Danger : _stale ? IconRenderer.Warning : MutedColor);
        g.DrawString(footer, _smallFont, footerBrush, pad, footerY);

        // live countdown to the next poll, right-aligned (hidden during any
        // error state so it never collides with error text or the fix button)
        if (_error is null && NextUpdateAt is { } next)
        {
            var left = next - DateTimeOffset.Now;
            if (left < TimeSpan.Zero) left = TimeSpan.Zero;
            string cd = $"Usage will update in next {(int)left.TotalMinutes}:{left.Seconds:D2}";
            using var cdBrush = new SolidBrush(MutedColor);
            var size = g.MeasureString(cd, _smallFont);
            g.DrawString(cd, _smallFont, cdBrush, pad + contentWidth - size.Width, footerY);
        }
    }

    void DrawRow(Graphics g, UsageWindow w, int y, int pad, int contentWidth)
    {
        using var labelBrush = new SolidBrush(LabelColor);
        g.DrawString(w.Label + ":", _labelFont, labelBrush, pad, y);

        var barColor = IconRenderer.ColorFor(w.Utilization);
        string pct = $"{Math.Round(w.Utilization)}%";
        using var pctBrush = new SolidBrush(barColor);
        var labelSize = g.MeasureString(w.Label + ":", _labelFont);
        float pctX = pad + labelSize.Width + S(2); // ~1 character gap
        g.DrawString(pct, _valueFont, pctBrush, pctX, y);

        // reset countdown + actual clock time, right-aligned
        if (w.ResetsAt is { } resets)
        {
            var remaining = resets - DateTimeOffset.Now;
            if (remaining > TimeSpan.Zero)
            {
                string resetText = ResetText(resets, remaining);
                using var mutedBrush = new SolidBrush(MutedColor);
                var size = g.MeasureString(resetText, _smallFont);
                float resetX = pad + contentWidth - size.Width;
                float pctRight = pctX + g.MeasureString(pct, _valueFont).Width;
                // collision fallback: drop below the progress bar
                float resetY = resetX < pctRight + S(10) ? y + S(38) : y + S(3);
                g.DrawString(resetText, _smallFont, mutedBrush, resetX, resetY);
            }
        }
        else if (w.Key == "five_hour")
        {
            // window reset & idle: no active 5h window yet, so the server has no reset time —
            // the next window starts (and the 5h clock begins) on your next use
            using var mutedBrush = new SolidBrush(MutedColor);
            var size = g.MeasureString(NextUseHint, _smallFont);
            g.DrawString(NextUseHint, _smallFont, mutedBrush, pad + contentWidth - size.Width, y + S(3));
        }
        else if (w.UsedDollars is { } used && w.LimitDollars is { } limit)
        {
            // wallet row has no reset time, so its slot is free for the dollar amounts
            using var mutedBrush = new SolidBrush(MutedColor);
            string moneyText = MoneyText(used, limit);
            var size = g.MeasureString(moneyText, _smallFont);
            g.DrawString(moneyText, _smallFont, mutedBrush, pad + contentWidth - size.Width, y + S(3));
        }

        // progress bar
        int barY = y + S(28);
        int barH = Math.Max(4, S(6));
        var track = new Rectangle(pad, barY, contentWidth, barH);
        using (var trackBrush = new SolidBrush(TrackColor))
            FillRounded(g, trackBrush, track, barH / 2);

        int fillW = (int)Math.Round(contentWidth * Math.Clamp(w.Utilization, 0, 100) / 100.0);
        if (fillW >= barH) // too narrow to round below bar height
        {
            using var fillBrush = new SolidBrush(barColor);
            FillRounded(g, fillBrush, new Rectangle(pad, barY, fillW, barH), barH / 2);
        }

        // burn-rate ETA, left-aligned under the bar — same line the reset text falls
        // back to on collision, but that one is right-aligned so they never overlap
        if (ShowEta && EtaText(w) is { } eta)
        {
            using var etaBrush = new SolidBrush(MutedColor);
            g.DrawString(eta, _smallFont, etaBrush, pad, y + S(38));
        }
    }

    /// <summary>Burn-rate ETA for a row ("full in ~2h 30m"), or null when it can't be
    /// computed or would be noise (idle, just reset, or resetting before it fills).</summary>
    string? EtaText(UsageWindow w)
    {
        if (History is null) return null;
        var now = DateTimeOffset.Now;

        double remaining;
        double? rate;
        DateTimeOffset? boundary;

        if (w.Key == "extra_usage" && w.UsedDollars is { } used && w.LimitDollars is { } limit && limit > 0)
        {
            remaining = limit - used;
            if (remaining <= 0) return null;
            rate = History.RatePerSecond("credit_spend", TimeSpan.FromHours(24));
            // no reset time from the API for the wallet — the equivalent boundary is
            // the start of next month, when spend resets to $0
            boundary = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset).AddMonths(1);
        }
        else
        {
            if (w.Utilization >= 100) return null;
            remaining = 100 - w.Utilization;
            // a 1h slope on a weekly window reads a burst of work as "weekly full in 4h",
            // which is wrong — weekly windows need the longer, steadier lookback
            var lookback = w.Key.StartsWith("seven_day", StringComparison.Ordinal) ? TimeSpan.FromHours(24) : TimeSpan.FromHours(1);
            rate = History.RatePerSecond(w.Key, lookback);
            boundary = w.ResetsAt;
        }

        if (rate is not { } r) return null;
        double seconds = remaining / r;
        if (!double.IsFinite(seconds) || seconds <= 0) return null;
        // a month out is noise, not a forecast — and it keeps absurd rates (a hairline
        // drift over a 24 h lookback) from overflowing the TimeSpan below
        if (seconds > TimeSpan.FromDays(30).TotalSeconds) return null;

        // a limit that resets before it fills is not news
        if (boundary is { } b && now.AddSeconds(seconds) >= b) return null;

        var t = TimeSpan.FromSeconds(seconds);
        string label = t.TotalHours >= 24 ? $"{(int)t.TotalDays}d {t.Hours}h"
            : t.TotalHours >= 1 ? $"{(int)t.TotalHours}h {t.Minutes}m"
            : $"{Math.Max(1, (int)t.TotalMinutes)}m";
        return "full in ~" + label;
    }

    /// <summary>
    /// Context section, visually separated from the usage limits: one divider + tiny
    /// caps header, then one compact block per active session (current/capacity tokens,
    /// distance to auto-compact, age, thin bar).
    /// </summary>
    void DrawContextSection(Graphics g, int y, int pad, int contentWidth)
    {
        using var mutedBrush = new SolidBrush(MutedColor);

        using (var sepPen = new Pen(Theme.Grid, 1))
            g.DrawLine(sepPen, pad, y + S(3), pad + contentWidth, y + S(3));
        string header = _sessions.Count > 1 ? $"SESSION CONTEXT ({_sessions.Count})" : "SESSION CONTEXT";
        g.DrawString(header, _tinyFont, mutedBrush, pad, y + S(8));

        int by = y + ContextHeaderHeight;
        foreach (var ctx in _sessions)
        {
            DrawContextBlock(g, ctx, by, pad, contentWidth);
            by += ContextBlockHeight;
        }
    }

    void DrawContextBlock(Graphics g, SessionContext ctx, int y, int pad, int contentWidth)
    {
        var color = ContextColor(ctx.Percent);
        using var mutedBrush = new SolidBrush(MutedColor);
        using var pctBrush = new SolidBrush(color);

        // breathing room above each block
        y += S(5);

        // line 1: bold session name, then percent (left); current/capacity tokens (right)
        using var labelBrush = new SolidBrush(LabelColor);
        string name = $"{ctx.Project} · {ctx.Model}";
        g.DrawString(name, _smallBoldFont, labelBrush, pad, y);
        var nameSize = g.MeasureString(name, _smallBoldFont);
        string pct = $"{Math.Round(ctx.Percent)}%";
        g.DrawString(pct, _smallBoldFont, pctBrush, pad + nameSize.Width + S(6), y);

        string tokens = $"{FmtTokens(ctx.Tokens)} / {FmtTokens(ctx.WindowSize)}";
        var tokSize = g.MeasureString(tokens, _smallFont);
        g.DrawString(tokens, _smallFont, mutedBrush, pad + contentWidth - tokSize.Width, y);

        // line 2: distance to auto-compact + age
        long compactAt = (long)(ctx.WindowSize * 0.8);
        string compactText = ctx.Tokens < compactAt
            ? $"~{(compactAt - ctx.Tokens) / 1000}k to auto-compact"
            : "past auto-compact";
        var age = DateTimeOffset.Now - ctx.StartedAt;
        string ageText = age.TotalDays >= 1 ? $"{(int)age.TotalDays}d old"
            : age.TotalHours >= 1 ? $"{(int)age.TotalHours}h old"
            : $"{Math.Max(1, (int)age.TotalMinutes)}m old";
        g.DrawString($"{compactText}  ·  {ageText}", _tinyFont, mutedBrush, pad, y + S(17));

        // thin bar
        int barY = y + S(37);
        int barH = Math.Max(3, S(4));
        using (var trackBrush = new SolidBrush(TrackColor))
            FillRounded(g, trackBrush, new Rectangle(pad, barY, contentWidth, barH), barH / 2);
        int fillW = (int)Math.Round(contentWidth * ctx.Percent / 100.0);
        if (fillW >= barH)
        {
            using var fillBrush = new SolidBrush(color);
            FillRounded(g, fillBrush, new Rectangle(pad, barY, fillW, barH), barH / 2);
        }
    }

    /// <summary>Compact token count: 169k, 1.0M, or the raw number below 1000.</summary>
    static string FmtTokens(long n) =>
        n >= 1_000_000 ? $"{n / 1_000_000.0:0.0}M"
        : n >= 1_000 ? $"{Math.Round(n / 1000.0)}k"
        : n.ToString();

    /// <summary>Session-remaining line chart with hourly time axis, "now" marker and reset markers.</summary>
    void DrawRemainingChart(Graphics g, int pad, int top, int contentWidth)
    {
        using var mutedBrush = new SolidBrush(MutedColor);

        // divider + tiny caps header, matching the SESSION CONTEXT section style
        using (var sepPen = new Pen(Theme.Grid, 1))
            g.DrawLine(sepPen, pad, top + S(3), pad + contentWidth, top + S(3));
        g.DrawString($"SESSION GRAPH ({_graphRangeHours}H)", _tinyFont, mutedBrush, pad, top + S(8));

        // plot starts below two text rows: title/remaining row, then reset-time labels row
        int labelGutter = S(24);
        var plot = new Rectangle(
            pad + labelGutter,
            top + S(40),
            contentWidth - labelGutter,
            GraphHeight - S(40) - S(18) - S(6));

        using var gridPen = new Pen(Theme.Grid, 1);
        using var tickPen = new Pen(Theme.GridStrong, 1);

        // Y axis: remaining % gridlines
        foreach (int v in new[] { 0, 50, 100 })
        {
            int yy = plot.Bottom - (int)(v / 100.0 * plot.Height);
            g.DrawLine(gridPen, plot.Left, yy, plot.Right, yy);
            var s = g.MeasureString(v.ToString(), _tinyFont);
            g.DrawString(v.ToString(), _tinyFont, mutedBrush, plot.Left - s.Width - S(3), yy - s.Height / 2);
        }

        // time axis: "now" sits at NowPositionPercent — history left of it, future right
        double rangeSec = _graphRangeHours * 3600.0;
        double pastSec = rangeSec * _nowPositionPercent / 100.0;
        var now = DateTimeOffset.Now;
        var start = now.AddSeconds(-pastSec);
        var end = now.AddSeconds(rangeSec - pastSec);
        int labelStep = _graphRangeHours >= 24 ? 3 : 2;

        // X axis: tick every hour, labels every labelStep h (date shown at midnight)
        var tick = new DateTimeOffset(start.Year, start.Month, start.Day, start.Hour, 0, 0, start.Offset).AddHours(1);
        for (; tick <= end; tick = tick.AddHours(1))
        {
            float x = plot.Left + (float)((tick - start).TotalSeconds / rangeSec) * plot.Width;
            bool major = tick.Hour % labelStep == 0;
            if (major)
            {
                g.DrawLine(gridPen, x, plot.Top, x, plot.Bottom);
                string label = tick.Hour == 0 ? tick.ToString("d MMM") : tick.ToString("HH:mm");
                var s = g.MeasureString(label, _tinyFont);
                if (x - s.Width / 2 > plot.Left - S(6) && x + s.Width / 2 < plot.Right + S(6))
                    g.DrawString(label, _tinyFont, mutedBrush, x - s.Width / 2, plot.Bottom + S(3));
            }
            else
            {
                g.DrawLine(tickPen, x, plot.Bottom - S(3), x, plot.Bottom);
            }
        }

        // "now" marker
        float nowX = plot.Left + plot.Width * _nowPositionPercent / 100f;
        using (var nowPen = new Pen(Theme.NowLine, 1))
            g.DrawLine(nowPen, nowX, plot.Top, nowX, plot.Bottom);
        using (var nowBrush = new SolidBrush(Theme.NowText))
        {
            // two lines: "Now" with a live hh:mm:ss clock under it
            string clock = DateTimeOffset.Now.ToString("HH:mm:ss");
            var ns = g.MeasureString("Now", _tinyFont);
            var cs = g.MeasureString(clock, _tinyFont);
            float widest = Math.Max(ns.Width, cs.Width);
            bool flip = nowX + widest + S(2) > plot.Right;
            g.DrawString("Now", _tinyFont, nowBrush, flip ? nowX - ns.Width - S(2) : nowX + S(2), plot.Top);
            g.DrawString(clock, _tinyFont, nowBrush, flip ? nowX - cs.Width - S(2) : nowX + S(2), plot.Top + ns.Height);
        }

        // reset markers: green dashed line + time — future reset from the API,
        // past resets detected as big upward jumps in remaining
        var resetColor = ColorTranslator.FromHtml("#6bcb77");
        using var resetPen = new Pen(Color.FromArgb(190, resetColor), 1) { DashStyle = DashStyle.Dash };
        using var resetBrush = new SolidBrush(resetColor);

        void DrawResetMark(DateTimeOffset t)
        {
            float x = plot.Left + (float)((t - start).TotalSeconds / rangeSec) * plot.Width;
            if (x < plot.Left || x > plot.Right) return;
            g.DrawLine(resetPen, x, plot.Top, x, plot.Bottom);
            string label = t.ToString("HH:mm");
            var s = g.MeasureString(label, _tinyFont);
            float lx = Math.Min(x - s.Width / 2, plot.Right - s.Width);
            g.DrawString(label, _tinyFont, resetBrush, lx, plot.Top - s.Height - S(1));
        }

        var sessionReset = _snapshot?.Windows.FirstOrDefault(w => w.Key == "five_hour")?.ResetsAt;
        if (sessionReset is { } future && future > now && future <= end)
            DrawResetMark(future);

        var samples = History?.Samples("five_hour");
        if (samples is { Count: >= 2 })
        {
            double startSec = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - pastSec;
            var visible = samples.Where(p => p[0] >= startSec).ToList();

            // split at sampling gaps (>15 min ≈ 5 missed polls): the app wasn't running
            // there, so a connecting line would fabricate data — and the reset detector
            // below would pin a reset marker at the wrong time
            var segments = new List<List<double[]>>();
            foreach (var p in visible)
            {
                if (segments.Count == 0 || p[0] - segments[^1][^1][0] > 900)
                    segments.Add(new List<double[]>());
                segments[^1].Add(p);
            }

            PointF Pt(double[] p) => new(
                plot.Left + (float)((p[0] - startSec) / rangeSec) * plot.Width,
                plot.Bottom - (float)(Math.Clamp(100 - p[1], 0, 100) / 100.0) * plot.Height);

            using var fillBrush = new SolidBrush(Color.FromArgb(42, IconRenderer.Accent));
            using var linePen = new Pen(IconRenderer.Accent, Math.Max(1.5f, 2f * _scale)) { LineJoin = LineJoin.Round };

            // gaps: faint band + dashed connector + "no data", so the hole reads as
            // "meter was off" instead of the line just vanishing
            using var gapPen = new Pen(Color.FromArgb(90, IconRenderer.Accent), Math.Max(1f, 1.4f * _scale)) { DashStyle = DashStyle.Dash };
            using var gapBand = new SolidBrush(Color.FromArgb(12, Theme.Light ? Color.Black : Color.White));
            void DrawGapBand(float x0, float x1)
            {
                g.FillRectangle(gapBand, x0, plot.Top, x1 - x0, plot.Height);
                var ns = g.MeasureString("no data", _smallFont);
                if (x1 - x0 > ns.Width + S(8)) // skip the label on gaps too narrow to fit it
                    g.DrawString("no data", _smallFont, mutedBrush,
                        (x0 + x1 - ns.Width) / 2, plot.Bottom - ns.Height - S(2));
            }

            for (int s = 1; s < segments.Count; s++)
            {
                var a = Pt(segments[s - 1][^1]);
                var b = Pt(segments[s][0]);
                DrawGapBand(a.X, b.X);
                g.DrawLine(gapPen, a, b);
            }

            // the window can also reach past the recorded data at either edge —
            // e.g. a 12h view whose left half predates the oldest sample in range
            if (visible.Count > 0)
            {
                if (visible[0][0] - startSec > 900)
                    DrawGapBand(plot.Left, Pt(visible[0]).X);
                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - visible[^1][0] > 900)
                    DrawGapBand(Pt(visible[^1]).X, nowX);
            }
            else
            {
                // history exists but all of it predates the window (app was off ~18-24h)
                DrawGapBand(plot.Left, nowX);
            }

            foreach (var seg in segments)
            {
                // past resets: remaining jumped up sharply between consecutive samples
                for (int i = 1; i < seg.Count; i++)
                {
                    if (seg[i][1] <= seg[i - 1][1] - 25)
                        DrawResetMark(DateTimeOffset.FromUnixTimeSeconds((long)seg[i][0]).ToLocalTime());
                }

                var pts = seg.Select(Pt).ToArray();
                if (pts.Length < 2) continue;

                using var area = new GraphicsPath();
                area.AddLines(pts);
                area.AddLine(pts[^1].X, plot.Bottom, pts[0].X, plot.Bottom);
                area.CloseFigure();
                g.FillPath(fillBrush, area);
                g.DrawLines(linePen, pts);
            }

            if (visible.Count > 0)
            {
                var last = Pt(visible[^1]);
                using var curBrush = new SolidBrush(IconRenderer.Accent);
                g.FillEllipse(curBrush, last.X - S(3), last.Y - S(3), S(6), S(6));

                double lastRemaining = Math.Clamp(100 - samples[^1][1], 0, 100);
                string cur = $"{Math.Round(lastRemaining)}% remaining";
                var cs = g.MeasureString(cur, _smallFont);
                g.DrawString(cur, _smallFont, curBrush, pad + contentWidth - cs.Width, top + S(5));
            }
        }
        else
        {
            const string msg = "Collecting data…";
            var s = g.MeasureString(msg, _smallFont);
            g.DrawString(msg, _smallFont, mutedBrush,
                plot.Left + (plot.Width - s.Width) / 2, plot.Top + (plot.Height - s.Height) / 2);
        }
    }

    /// <summary>Cumulative usage-credit spend vs the monthly cap over a rolling day window.
    /// Spend resets to $0 on the 1st, so a window spanning a month boundary shows the line
    /// dive to the floor there — left as-is, no reset marker.</summary>
    void DrawCreditGraph(Graphics g, int pad, int top, int contentWidth)
    {
        var wallet = WalletWindow;
        if (wallet?.LimitDollars is not { } limit || limit <= 0) return;
        double used = wallet.UsedDollars ?? 0;

        using var mutedBrush = new SolidBrush(MutedColor);

        // divider + tiny caps header, matching the SESSION GRAPH section style
        using (var sepPen = new Pen(Theme.Grid, 1))
            g.DrawLine(sepPen, pad, top + S(3), pad + contentWidth, top + S(3));
        g.DrawString(_creditRangeDays == 1 ? "CREDIT (24H)" : $"CREDIT ({_creditRangeDays}D)",
            _tinyFont, mutedBrush, pad, top + S(8));

        // plot starts below the header row; dollar labels run wider than the % gutter
        int labelGutter = S(28);
        var plot = new Rectangle(
            pad + labelGutter,
            top + S(40),
            contentWidth - labelGutter,
            CreditGraphHeight - S(40) - S(18) - S(6));

        using var gridPen = new Pen(Theme.Grid, 1);

        // Y axis: $0 at bottom, the monthly limit at top
        foreach (double frac in new[] { 0.0, 0.5, 1.0 })
        {
            int yy = plot.Bottom - (int)(frac * plot.Height);
            g.DrawLine(gridPen, plot.Left, yy, plot.Right, yy);
            string label = $"${Math.Round(limit * frac):0}";
            var s = g.MeasureString(label, _tinyFont);
            g.DrawString(label, _tinyFont, mutedBrush, plot.Left - s.Width - S(3), yy - s.Height / 2);
        }

        // limit line: dashed red at the cap — coincides with the top gridline, but
        // labeled separately so the cap reads as a hard ceiling, not just an axis tick
        using (var limitPen = new Pen(IconRenderer.Danger, 1) { DashStyle = DashStyle.Dash })
            g.DrawLine(limitPen, plot.Left, plot.Top, plot.Right, plot.Top);
        using (var limitBrush = new SolidBrush(IconRenderer.Danger))
        {
            string limitLabel = $"${limit:0.00} limit";
            var ls = g.MeasureString(limitLabel, _tinyFont);
            g.DrawString(limitLabel, _tinyFont, limitBrush, plot.Right - ls.Width, plot.Top - ls.Height - S(1));
        }

        // X axis: rolling window of _creditRangeDays, now sitting at _creditNowPositionPercent
        // (past to its left, empty future to its right) — same scheme as the session graph
        double rangeSec = _creditRangeDays * 86400.0;
        double pastSec = rangeSec * _creditNowPositionPercent / 100.0;
        var now = DateTimeOffset.Now;
        var winStart = now.AddSeconds(-pastSec);
        var winEnd = now.AddSeconds(rangeSec - pastSec);
        double winStartSec = now.ToUnixTimeSeconds() - pastSec;

        float TickX(DateTimeOffset t) =>
            plot.Left + (float)((t.ToUnixTimeSeconds() - winStartSec) / rangeSec) * plot.Width;

        void DrawTickLabel(float x, string label)
        {
            var s = g.MeasureString(label, _tinyFont);
            if (x - s.Width / 2 > plot.Left - S(6) && x + s.Width / 2 < plot.Right + S(6))
                g.DrawString(label, _tinyFont, mutedBrush, x - s.Width / 2, plot.Bottom + S(3));
        }

        if (_creditRangeDays < 7)
        {
            // short ranges get hourly ticks like the session graph — a day-scale axis
            // would leave the 24 h view with a single gridline at midnight
            int hourStep = _creditRangeDays <= 1 ? 1 : 6;    // minor tick spacing
            int labelStep = _creditRangeDays <= 1 ? 3 : 12;  // gridline + label spacing
            using var tickPen = new Pen(Theme.GridStrong, 1);
            var tick = new DateTimeOffset(winStart.Year, winStart.Month, winStart.Day, 0, 0, 0, winStart.Offset);
            for (; tick <= winEnd; tick = tick.AddHours(hourStep))
            {
                if (tick < winStart) continue;
                float x = TickX(tick);
                if (tick.Hour % labelStep == 0)
                {
                    g.DrawLine(gridPen, x, plot.Top, x, plot.Bottom);
                    DrawTickLabel(x, tick.Hour == 0 ? tick.ToString("d MMM") : tick.ToString("HH:mm"));
                }
                else
                {
                    g.DrawLine(tickPen, x, plot.Bottom - S(3), x, plot.Bottom);
                }
            }
        }
        else
        {
            // day ticks: every 1/3/5 days for the 7/15/30-day ranges, date at each
            int dayStep = _creditRangeDays <= 7 ? 1 : _creditRangeDays <= 15 ? 3 : 5;
            var tick = new DateTimeOffset(winStart.Year, winStart.Month, winStart.Day, 0, 0, 0, winStart.Offset);
            if (tick < winStart) tick = tick.AddDays(1);
            for (; tick <= winEnd; tick = tick.AddDays(dayStep))
            {
                float x = TickX(tick);
                g.DrawLine(gridPen, x, plot.Top, x, plot.Bottom);
                DrawTickLabel(x, tick.ToString("d MMM"));
            }
        }

        // month reset: spend is cumulative *within a month*, so on the 1st the series
        // cliffs from last month's total down to $0. Mark the boundary rather than
        // leaving an unexplained drop — applies to every range, not just the short ones.
        var monthStart = new DateTimeOffset(winStart.Year, winStart.Month, 1, 0, 0, 0, winStart.Offset);
        if (monthStart < winStart) monthStart = monthStart.AddMonths(1);
        using (var resetPen = new Pen(IconRenderer.Danger, 1) { DashStyle = DashStyle.Dash })
        using (var resetBrush = new SolidBrush(IconRenderer.Danger))
            for (; monthStart <= winEnd; monthStart = monthStart.AddMonths(1))
            {
                float x = TickX(monthStart);
                g.DrawLine(resetPen, x, plot.Top, x, plot.Bottom);
                var s = g.MeasureString("reset", _tinyFont);
                g.DrawString("reset", _tinyFont, resetBrush,
                    Math.Min(x + S(2), plot.Right - s.Width), plot.Bottom - s.Height);
            }

        // "now" marker line; its "Now" label rides the latest dot (drawn below) instead
        // of sitting at the top, so the reading travels with the point
        float nowX = plot.Left + plot.Width * _creditNowPositionPercent / 100f;
        using (var nowPen = new Pen(Theme.NowLine, 1))
            g.DrawLine(nowPen, nowX, plot.Top, nowX, plot.Bottom);

        // data: credit_spend samples are server-truth cumulative values. The stretch
        // before our first in-window sample is drawn as a flat dashed lead-in held at that
        // sample's level, stretched to the left edge — not literally true (we don't know
        // the early curve), but it fills the chart, and with no history yet it becomes a
        // full-width line at the current value. Solid takes over wherever we did record.
        var recorded = History?.Samples("credit_spend").Where(p => p[0] >= winStartSec).ToList()
            ?? new List<double[]>();
        // the snapshot's own month-to-date value is one more server-truth point — it lets
        // the solid line reach "Now" and draw something from the very first poll
        recorded.Add(new[] { (double)now.ToUnixTimeSeconds(), used });
        recorded.Sort((a, b) => a[0].CompareTo(b[0]));

        PointF Pt(double t, double v) => new(
            plot.Left + (float)((t - winStartSec) / rangeSec) * plot.Width,
            plot.Bottom - (float)(Math.Clamp(v, 0, limit) / limit) * plot.Height);

        var recPts = recorded.Select(p => Pt(p[0], p[1])).ToArray();
        var baseline = new PointF(plot.Left, recPts[0].Y); // flat, at the first known level

        // one continuous soft fill under the whole line (lead-in + recorded)
        using (var fillBrush = new SolidBrush(Color.FromArgb(42, IconRenderer.Accent)))
        using (var area = new GraphicsPath())
        {
            var outline = new[] { baseline }.Concat(recPts).ToArray();
            area.AddLines(outline);
            area.AddLine(outline[^1].X, plot.Bottom, outline[0].X, plot.Bottom);
            area.CloseFigure();
            g.FillPath(fillBrush, area);
        }

        // dashed lead-in over the un-recorded stretch, solid over what we logged
        using (var leadPen = new Pen(Color.FromArgb(140, IconRenderer.Accent), Math.Max(1.5f, 2f * _scale))
            { DashStyle = DashStyle.Dash, LineJoin = LineJoin.Round })
            g.DrawLine(leadPen, baseline, recPts[0]);
        if (recPts.Length >= 2)
            using (var linePen = new Pen(IconRenderer.Accent, Math.Max(1.5f, 2f * _scale)) { LineJoin = LineJoin.Round })
                g.DrawLines(linePen, recPts);

        var dot = recPts[^1];
        using (var curBrush = new SolidBrush(IconRenderer.Accent))
            g.FillEllipse(curBrush, dot.X - S(3), dot.Y - S(3), S(6), S(6));

        // two-line "Now / $X used" label clinging to the dot: upper-right by default, in
        // the session graph's tiny soft-white style. Flips below on a top collision and to
        // the dot's left if it would run past the right edge, so it never leaves the plot.
        string usedText = $"${used:0.00} used";
        var nowSize = g.MeasureString("Now", _tinyFont);
        var usedSize = g.MeasureString(usedText, _tinyFont);
        float labelW = Math.Max(nowSize.Width, usedSize.Width);
        float labelLeft = dot.X + S(6);
        if (labelLeft + labelW > plot.Right) labelLeft = dot.X - S(6) - labelW;  // flip left
        float labelTop = dot.Y - nowSize.Height - usedSize.Height - S(4);
        if (labelTop < plot.Top) labelTop = dot.Y + S(4);                        // flip below
        using var labelBrush = new SolidBrush(Theme.NowText);
        g.DrawString("Now", _tinyFont, labelBrush, labelLeft, labelTop);
        g.DrawString(usedText, _tinyFont, labelBrush, labelLeft, labelTop + nowSize.Height);
    }

    static void FillRounded(Graphics g, Brush brush, Rectangle rect, int radius)
    {
        using var path = new GraphicsPath();
        int d = Math.Max(2, radius * 2);
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }

    static string Truncate(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tick.Dispose();
            _labelFont.Dispose();
            _valueFont.Dispose();
            _smallFont.Dispose();
            _smallBoldFont.Dispose();
            _tinyFont.Dispose();
            _headerFont.Dispose();
        }
        base.Dispose(disposing);
    }

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("user32.dll")]
    static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

    [DllImport("user32.dll")]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
