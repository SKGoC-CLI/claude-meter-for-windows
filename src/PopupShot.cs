namespace ClaudeMeter;

/// <summary>
/// Renders the popup to PNG in both themes, invoked via `--popup-shot [outDir]`.
/// Same spirit as IconSelfTest: not a test project, just enough to eyeball a palette
/// or layout change without hunting the real popup on screen. Uses the real saved
/// history so the charts draw actual curves.
/// </summary>
static class PopupShot
{
    public static void Run(string outDir)
    {
        Directory.CreateDirectory(outDir);

        var now = DateTimeOffset.Now;
        // one row per severity band, plus the wallet row, so every status colour shows up
        var snapshot = new UsageSnapshot(new[]
        {
            new UsageWindow("five_hour", "Session (5h)", 42, now.AddHours(2).AddMinutes(18)),
            new UsageWindow("seven_day", "Weekly (all models)", 76, now.AddDays(3).AddHours(5)),
            new UsageWindow("seven_day_opus", "Weekly (Opus)", 93, now.AddDays(3).AddHours(5)),
            new UsageWindow("extra_usage", "Extra usage", 24.8, null, UsedDollars: 12.40, LimitDollars: 50.00),
            new UsageWindow("cloud_credit", "Cloud credit", 1.49, now.AddDays(34), UsedDollars: 1.49, LimitDollars: 100),
        }, now);

        var sessions = new[]
        {
            new SessionContext("App Claude Meter", "opus-5", 169_000, 200_000, now.AddHours(-3), now, SessionState.Working),
            new SessionContext("abbott-archive", "sonnet-5", 48_000, 200_000, now.AddMinutes(-40), now, SessionState.Waiting),
            new SessionContext("CORSAIR XENEON EDGE Claude App", "opus-5", 135_000, 1_000_000, now.AddHours(-3), now, SessionState.Idle),
        };

        foreach (bool light in new[] { true, false })
        {
            Theme.Light = light;
            using var popup = new PopupForm
            {
                Pinned = true,
                ShowLogo = true,
                ShowEta = true,
                ShowRemainingGraph = true,
                ShowCreditGraph = true,
                History = new UsageHistory(),
                NextUpdateAt = now.AddMinutes(2).AddSeconds(14),
                Sessions = sessions,
                RawWindows = snapshot.Windows,
            };
            popup.ApplyScale(1.3f); // Khun Somgok's saved Scale
            popup.ApplyTheme();
            popup.UpdateData(snapshot, error: null, stale: false);

            popup.Location = new Point(-4000, -4000); // off-screen: paint it without flashing it
            popup.Show();
            Application.DoEvents();

            using var bmp = new Bitmap(popup.Width, popup.Height);
            popup.DrawToBitmap(bmp, new Rectangle(0, 0, popup.Width, popup.Height));

            // DrawToBitmap only captures what the app paints; the window border is
            // drawn by DWM via DWMWA_BORDER_COLOR (see ApplyBorderColor), composited
            // outside the bitmap, so paint it in by hand to match the real window.
            int borderRef = Theme.BorderColorRef;
            if (borderRef != unchecked((int)0xFFFFFFFF))
            {
                var borderColor = Color.FromArgb(borderRef & 0xFF, (borderRef >> 8) & 0xFF, (borderRef >> 16) & 0xFF);
                using var g = Graphics.FromImage(bmp);
                using var pen = new Pen(borderColor);
                g.DrawRectangle(pen, 0, 0, bmp.Width - 1, bmp.Height - 1);
            }

            string path = Path.Combine(outDir, light ? "popup-light.png" : "popup-dark.png");
            bmp.Save(path);
            popup.Hide();
            Console.WriteLine($"{path}  {bmp.Width}x{bmp.Height}");
        }
    }
}
