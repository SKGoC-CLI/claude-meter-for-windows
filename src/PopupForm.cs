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
    Font _tinyFont = null!;
    Font _headerFont = null!;

    UsageSnapshot? _snapshot;
    string? _error;
    bool _stale;
    double _baseOpacity = 1.0;
    bool _hovering;
    bool _clickThrough;

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
    bool _showLogo;

    /// <summary>Whether to draw the session-remaining chart at the bottom of the popup.</summary>
    public bool ShowRemainingGraph
    {
        get => _showRemainingGraph;
        set { _showRemainingGraph = value; RecomputeLayout(); }
    }

    /// <summary>Whether to draw the logo header at the top of the popup.</summary>
    public bool ShowLogo
    {
        get => _showLogo;
        set { _showLogo = value; RecomputeLayout(); }
    }

    void RecomputeLayout()
    {
        Width = ComputeWidth();
        Height = ComputeHeight();
        if (Visible)
        {
            if (!Pinned) Reposition();
            else ClampToScreen();
        }
        Invalidate();
    }

    int RowHeight => S(58);

    int HeaderHeight => _showLogo && LogoStore.Logo is not null ? S(42) : 0;

    int GraphHeight => _showRemainingGraph ? S(162) : 0;

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
        ApplyScale(1f);
    }

    int S(float v) => (int)Math.Round(v * _scale);

    public void ApplyScale(float scale)
    {
        _scale = scale;
        _labelFont?.Dispose();
        _valueFont?.Dispose();
        _smallFont?.Dispose();
        _tinyFont?.Dispose();
        _headerFont?.Dispose();
        _labelFont = new Font("Segoe UI", 10f * scale);
        _valueFont = new Font("Segoe UI", 10f * scale, FontStyle.Bold);
        _smallFont = new Font("Segoe UI", 8f * scale);
        _tinyFont = new Font("Segoe UI", 7f * scale);
        _headerFont = new Font("Segoe UI", 10f * scale, FontStyle.Bold);
        Width = ComputeWidth();
        Height = ComputeHeight();
        if (Visible) Reposition();
        Invalidate();
    }

    /// <summary>Auto-fit: wide enough that label + % + reset text never collide.</summary>
    int ComputeWidth()
    {
        int min = S(320);
        if (_snapshot is null || _snapshot.Windows.Count == 0) return min;

        using var g = CreateGraphics();
        float widest = 0;
        foreach (var w in _snapshot.Windows)
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
            widest = Math.Max(widest, labelW + S(2) + pctW + S(16) + resetW);
        }

        // footer must also fit: "Updated HH:mm · stale" + countdown, right-aligned
        float footerLeftW = g.MeasureString("Updated 88:88  ·  ⚠ stale", _smallFont).Width;
        float footerRightW = g.MeasureString("Usage will update in next 8:88", _smallFont).Width;
        widest = Math.Max(widest, footerLeftW + S(16) + footerRightW);

        int width = (int)Math.Ceiling(widest) + S(16) * 2;
        return Math.Clamp(width, min, S(560));
    }

    static string ResetText(DateTimeOffset resets, TimeSpan remaining) =>
        remaining.TotalHours >= 24
            ? $"resets in {(int)remaining.TotalDays}d {remaining.Hours}h ({resets:ddd HH:mm})"
            : $"resets in {(int)remaining.TotalHours}h {remaining.Minutes}m ({resets:HH:mm})";

    public void ApplyTheme()
    {
        BackColor = Theme.Background;
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
        if (Pinned && e.Button == MouseButtons.Left)
        {
            // hand the drag to Windows: the whole form acts as a title bar
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
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

    public void UpdateData(UsageSnapshot? snapshot, string? error, bool stale)
    {
        _snapshot = snapshot;
        _error = error;
        _stale = stale;
        Width = ComputeWidth();
        Height = ComputeHeight();
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
        int rows = _snapshot?.Windows.Count ?? 0;
        int body = rows > 0 ? rows * RowHeight : S(64); // space for error/loading text
        return S(16) + HeaderHeight + body + GraphHeight + S(26) + S(8);
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

        int pad = S(16);
        int y = pad;
        int contentWidth = Width - pad * 2;

        if (HeaderHeight > 0)
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
            y += HeaderHeight;
        }

        if (_snapshot is null || _snapshot.Windows.Count == 0)
        {
            string msg = _error ?? "Loading…";
            using var brush = new SolidBrush(_error is null ? MutedColor : IconRenderer.Danger);
            g.DrawString(msg, _labelFont, brush,
                new RectangleF(pad, y, contentWidth, S(64)));
        }
        else
        {
            foreach (var w in _snapshot.Windows)
            {
                DrawRow(g, w, y, pad, contentWidth);
                y += RowHeight;
            }
        }

        if (GraphHeight > 0) DrawRemainingChart(g, pad, y, contentWidth);

        DrawFooter(g, pad, contentWidth);
    }

    void DrawFooter(Graphics g, int pad, int contentWidth)
    {
        float footerY = Height - S(26) - S(4);

        string footer = _snapshot is null
            ? ""
            : $"Updated {_snapshot.FetchedAt:HH:mm}" + (_stale ? "  ·  ⚠ stale" : "");
        if (_error is not null && _snapshot is not null)
            footer = "⚠ " + Truncate(_error.Replace('\n', ' '), 32);
        using var footerBrush = new SolidBrush(_stale || (_error is not null && _snapshot is not null) ? IconRenderer.Warning : MutedColor);
        g.DrawString(footer, _smallFont, footerBrush, pad, footerY);

        // live countdown to the next poll, right-aligned
        if (NextUpdateAt is { } next)
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
    }

    /// <summary>Session-remaining line chart with hourly time axis, "now" marker and reset markers.</summary>
    void DrawRemainingChart(Graphics g, int pad, int top, int contentWidth)
    {
        using var mutedBrush = new SolidBrush(MutedColor);
        g.DrawString($"Usage Graph ({_graphRangeHours}h)", _smallFont, mutedBrush, pad, top + S(2));

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
            var ns = g.MeasureString("Now", _tinyFont);
            float nx = nowX + ns.Width + S(2) > plot.Right ? nowX - ns.Width - S(2) : nowX + S(2);
            g.DrawString("Now", _tinyFont, nowBrush, nx, plot.Top);
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
            var pts = visible
                .Select(p => new PointF(
                    plot.Left + (float)((p[0] - startSec) / rangeSec) * plot.Width,
                    plot.Bottom - (float)(Math.Clamp(100 - p[1], 0, 100) / 100.0) * plot.Height))
                .ToArray();

            // past resets: remaining jumped up sharply between consecutive samples
            for (int i = 1; i < visible.Count; i++)
            {
                if (visible[i][1] <= visible[i - 1][1] - 25)
                    DrawResetMark(DateTimeOffset.FromUnixTimeSeconds((long)visible[i][0]).ToLocalTime());
            }

            if (pts.Length >= 2)
            {
                using var area = new GraphicsPath();
                area.AddLines(pts);
                area.AddLine(pts[^1].X, plot.Bottom, pts[0].X, plot.Bottom);
                area.CloseFigure();
                using var fillBrush = new SolidBrush(Color.FromArgb(42, IconRenderer.Accent));
                g.FillPath(fillBrush, area);

                using var linePen = new Pen(IconRenderer.Accent, Math.Max(1.5f, 2f * _scale)) { LineJoin = LineJoin.Round };
                g.DrawLines(linePen, pts);

                using var curBrush = new SolidBrush(IconRenderer.Accent);
                g.FillEllipse(curBrush, pts[^1].X - S(3), pts[^1].Y - S(3), S(6), S(6));

                double lastRemaining = Math.Clamp(100 - samples[^1][1], 0, 100);
                string cur = $"{Math.Round(lastRemaining)}% remaining";
                var cs = g.MeasureString(cur, _smallFont);
                g.DrawString(cur, _smallFont, curBrush, pad + contentWidth - cs.Width, top + S(2));
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
