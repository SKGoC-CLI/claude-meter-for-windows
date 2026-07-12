using System.Reflection;

namespace ClaudeMeter;

/// <summary>Dark-themed About dialog matching the popup style.</summary>
sealed class AboutForm : Form
{
    readonly List<Font> _fonts = new(); // labels don't own their fonts; dispose them with the form

    public AboutForm()
    {
        Text = "About";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Background;
        ClientSize = new Size(340, 252);
        ShowIcon = false;

        var logo = new PictureBox
        {
            Image = LogoStore.Logo,
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(96, 96),
            Location = new Point((ClientSize.Width - 96) / 2, 16),
            BackColor = Color.Transparent,
        };

        var title = MakeLabel("Claude Usage Meter for Windows", 11f, FontStyle.Bold, Theme.Label, 120);
        var version = MakeLabel($"Version {Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)}", 9f, FontStyle.Regular, Theme.Muted, 146);
        var dev = MakeLabel("Developer: SKGoC", 9.5f, FontStyle.Regular, Theme.Label, 172);
        var license = MakeLabel("License: MIT", 9.5f, FontStyle.Regular, Theme.Label, 194);
        var disclaimer = MakeLabel("Unofficial tool — not affiliated with Anthropic", 8.5f, FontStyle.Italic, Theme.Muted, 220);

        Controls.AddRange(new Control[] { logo, title, version, dev, license, disclaimer });
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            Close();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    Label MakeLabel(string text, float size, FontStyle style, Color color, int y)
    {
        var font = new Font("Segoe UI", size, style);
        _fonts.Add(font);
        return new()
        {
            Text = text,
            Font = font,
            ForeColor = color,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(ClientSize.Width, 24),
            Location = new Point(0, y),
            BackColor = Color.Transparent,
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            foreach (var font in _fonts) font.Dispose();
        base.Dispose(disposing);
    }
}

/// <summary>Loads the embedded logo once and shares it app-wide.</summary>
static class LogoStore
{
    public static readonly Image? Logo = Load();

    static Image? Load()
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("ClaudeMeter.assets.logo.png");
            return stream is null ? null : Image.FromStream(stream);
        }
        catch
        {
            return null;
        }
    }
}
