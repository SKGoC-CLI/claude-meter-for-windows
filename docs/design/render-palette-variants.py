"""Render the real popup once per candidate light palette, so colour choices get judged
on the actual renderer instead of a mockup.

    python docs/design/render-palette-variants.py <output-dir>

Edit VARIANTS below, run it, look at the PNGs. For each entry it rewrites Theme.cs's
light values, builds, and runs `ClaudeMeter.exe --popup-shot`. Dark values are hardcoded
in the template so dark mode can't drift while we experiment.

WARNING: this OVERWRITES src/Theme.cs and src/PopupForm.cs and restores them with
`git checkout --`, so any uncommitted work in those files would be destroyed. It refuses
to start unless src/ is clean — commit or stash first.
"""
import subprocess, sys, shutil
from pathlib import Path

REPO = Path(r"D:\Onedrive\Desktop\App Claude Meter")
OUT = Path(sys.argv[1])
OUT.mkdir(parents=True, exist_ok=True)

status = subprocess.run(["git", "-C", str(REPO), "status", "--porcelain", "--", "src"],
                        capture_output=True, text=True)
if status.returncode:
    sys.exit(f"git status failed (repo missing or REPO path stale?):\n{status.stderr}")
dirty = status.stdout.strip()
if dirty:
    sys.exit(f"src/ has uncommitted changes; this script would destroy them:\n{dirty}")

TEMPLATE = '''namespace ClaudeMeter;

/// <summary>Dark/Light palette shared by the popup and About dialog.</summary>
static class Theme
{{
    public static bool Light {{ get; set; }}

    public static Color Background => Light ? ColorTranslator.FromHtml("{bg}") : ColorTranslator.FromHtml("#1e1e1e");
    public static Color Track => Light ? ColorTranslator.FromHtml("{track}") : ColorTranslator.FromHtml("#3a3a3a");
    public static Color Label => Light ? ColorTranslator.FromHtml("{label}") : ColorTranslator.FromHtml("#e8e8e8");
    public static Color Muted => Light ? ColorTranslator.FromHtml("{muted}") : ColorTranslator.FromHtml("#8a8a8a");
    public static Color Grid => Light ? Color.FromArgb(24, 0, 0, 0) : Color.FromArgb(38, 255, 255, 255);
    public static Color GridStrong => Light ? Color.FromArgb(60, 0, 0, 0) : Color.FromArgb(80, 255, 255, 255);
    public static Color NowLine => Light ? Color.FromArgb(150, 0, 0, 0) : Color.FromArgb(120, 255, 255, 255);
    public static Color NowText => Light ? Color.FromArgb(200, 0, 0, 0) : Color.FromArgb(170, 255, 255, 255);

    public static Color Accent => Light ? ColorTranslator.FromHtml("{accent}") : IconRenderer.Accent;
    public static Color Warning => Light ? ColorTranslator.FromHtml("{warning}") : IconRenderer.Warning;
    public static Color Danger => Light ? ColorTranslator.FromHtml("{danger}") : IconRenderer.Danger;
    public static Color Graph => Light ? ColorTranslator.FromHtml("{graph}") : IconRenderer.Accent;
    public static Color Success => Light ? ColorTranslator.FromHtml("{success}") : ColorTranslator.FromHtml("#6bcb77");

    public static Color ColorFor(double utilization) =>
        utilization >= 90 ? Danger : utilization >= 70 ? Warning : Accent;

    public static int BorderColorRef =>
        Light ? {border} : unchecked((int)0xFFFFFFFF);
}}
'''

VARIANTS = {
    # A — what is on main now: Windows 11 Fluent semantic colours on a cold near-white
    "A-fluent-blue": dict(
        bg="#fbfbfb", track="#e3e3e3", label="#1f1f1f", muted="#5d5d5d",
        accent="#005FB8", warning="#9D5D00", danger="#C42B1C", success="#0F7B0F",
        graph="#005FB8", border="0xE0E0E0"),
    # B — same blue ramp, warm "paper" surface. Isolates the background as the variable.
    "B-paper-blue": dict(
        bg="#faf9f5", track="#e7e3d9", label="#1f1e1d", muted="#6b6760",
        accent="#005FB8", warning="#9D5D00", danger="#C42B1C", success="#0F7B0F",
        graph="#005FB8", border="0xD8DCE2"),
    # C — full brand: warm paper + the logo's coral as the normal state, gold caution,
    #     deep red danger. The risk is coral and red reading alike at a glance.
    "C-paper-coral": dict(
        bg="#faf9f5", track="#e7e3d9", label="#1f1e1d", muted="#6b6760",
        accent="#B0512C", warning="#8A6100", danger="#A82015", success="#3F6B21",
        graph="#B0512C", border="0xD8DCE2"),
    # D — colour as exception: normal state is warm graphite ink, so the popup is calm
    #     until something actually needs attention.
    "D-ink-first": dict(
        bg="#faf9f5", track="#e7e3d9", label="#1f1e1d", muted="#6b6760",
        accent="#3B3A36", warning="#9D5D00", danger="#C42B1C", success="#3F6B21",
        graph="#3B3A36", border="0xD8DCE2"),
    # E - D's calm severity ramp, but the charts keep the brand coral: a data series is
    #     not a severity signal, so it does not have to obey the same palette.
    "E-ink-rows-coral-graph": dict(
        bg="#faf9f5", track="#e7e3d9", label="#1f1e1d", muted="#6b6760",
        accent="#3B3A36", warning="#9D5D00", danger="#C42B1C", success="#3F6B21",
        graph="#B0512C", border="0xD8DCE2"),
}

theme = REPO / "src" / "Theme.cs"
try:
    for name, p in VARIANTS.items():
        theme.write_text(TEMPLATE.format(**p), encoding="utf-8")
        # only the chart drawing moves to Theme.Graph, anchored on the two method
        # markers below (not line numbers, which drift as the file changes);
        # ContextColor, well above the start marker, is a severity signal and must
        # stay on Accent. The substitution is idempotent, so re-running it each
        # loop iteration without restoring the file first is fine.
        popup = REPO / "src" / "PopupForm.cs"
        lines = popup.read_text(encoding="utf-8").splitlines(keepends=True)
        start = next((i for i, l in enumerate(lines) if "void DrawRemainingChart(" in l), None)
        end = next((i for i, l in enumerate(lines) if "static void FillRounded(" in l), None)
        if start is None or end is None:
            sys.exit("could not find DrawRemainingChart/FillRounded markers in PopupForm.cs")
        if start >= end:
            sys.exit(f"DrawRemainingChart marker (line {start}) is not before FillRounded marker (line {end})")
        for i in range(start, end):
            lines[i] = lines[i].replace("Theme.Accent", "Theme.Graph")
        popup.write_text("".join(lines), encoding="utf-8")
        build = subprocess.run(
            ["dotnet", "build", str(REPO / "ClaudeMeter.csproj"), "-c", "Debug", "-v", "quiet"],
            capture_output=True, text=True)
        if build.returncode:
            print(f"{name}: BUILD FAILED\n{build.stdout[-1500:]}")
            continue
        tmp = OUT / name
        subprocess.run([str(REPO / "bin" / "Debug" / "net8.0-windows" / "ClaudeMeter.exe"),
                        "--popup-shot", str(tmp)], capture_output=True)
        shot = tmp / "popup-light.png"
        if shot.exists():
            shutil.move(str(shot), str(OUT / f"{name}.png"))
            shutil.rmtree(tmp, ignore_errors=True)
            print(f"{name}: ok")
        else:
            print(f"{name}: NO SHOT")
finally:
    subprocess.run(["git", "-C", str(REPO), "checkout", "--", "src/Theme.cs", "src/PopupForm.cs"])
    print("Theme.cs + PopupForm.cs restored from git")
    # the Debug binary still holds the last variant's palette until it's rebuilt
    # from the restored source, or a stale palette silently ships via portable/
    rebuild = subprocess.run(
        ["dotnet", "build", str(REPO / "ClaudeMeter.csproj"), "-c", "Debug", "-v", "quiet"],
        capture_output=True, text=True)
    if rebuild.returncode:
        print(f"WARNING: rebuild after restore FAILED; Debug binary still holds the last variant's palette\n{rebuild.stdout[-1500:]}")
    else:
        print("Debug binary rebuilt from restored source")
