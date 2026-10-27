# MAKE SMOKE (2026-10-23, user: "lets find some real smoke to use for the smog"): composes Kenney's CC0 smoke puffs
# (Incoming\kenney\particle-pack\PNG (Transparent)\smoke_01..10.png, 512 px) into the game's smoke textures under
# Assets\Resources\Smoke\:
#   SmogTile.png  a 1024 TILING sheet of overlapping puffs (every puff is drawn at the eight wrapped offsets too, so the
#                 seams are continuous): LevelKitSmog's smog sheets drift it over the city and through the rooms
#   Puff.png      one puff at 256, for the chimney and exhaust particles
# RGB is white in both: the colour comes from the material, only the alpha (the density) is the photo's.
#   powershell -NoProfile -ExecutionPolicy Bypass -File Tools\MakeSmoke.ps1
param([int]$Seed = 4100, [int]$Puffs = 30, [int]$Size = 1024)

$ErrorActionPreference = 'Stop'
$proj = Split-Path -Parent $PSScriptRoot
$src = Join-Path $proj 'Incoming\kenney\particle-pack\PNG (Transparent)'
$dst = Join-Path $proj 'Assets\Resources\Smoke'
New-Item -ItemType Directory -Force $dst | Out-Null

Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
public static class Smoke
{
    /// Draws `puff` centred at (cx, cy) turned `deg` at `scale`, and again at the eight wrapped offsets
    static void Stamp(Graphics g, Image puff, float cx, float cy, float deg, float scale, int size)
    {
        float w = puff.Width * scale, h = puff.Height * scale;
        for (int ox = -1; ox <= 1; ox++)
            for (int oy = -1; oy <= 1; oy++)
            {
                g.ResetTransform();
                g.TranslateTransform(cx + ox * size, cy + oy * size);
                g.RotateTransform(deg);
                g.DrawImage(puff, -w * 0.5f, -h * 0.5f, w, h);
            }
    }
    /// White RGB, the alpha lifted by `gain` and clamped
    static void Whiten(Bitmap b, double gain)
    {
        var r = new Rectangle(0, 0, b.Width, b.Height);
        var d = b.LockBits(r, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        var px = new byte[d.Stride * b.Height];
        Marshal.Copy(d.Scan0, px, 0, px.Length);
        for (int i = 0; i < px.Length; i += 4)
        {
            px[i] = 255; px[i + 1] = 255; px[i + 2] = 255;
            px[i + 3] = (byte)Math.Min(255, Math.Round(px[i + 3] * gain));
        }
        Marshal.Copy(px, 0, d.Scan0, px.Length);
        b.UnlockBits(d);
    }
    public static void Tile(string[] puffs, string dst, int size, int count, int seed)
    {
        var rnd = new Random(seed);
        var images = new Image[puffs.Length];
        for (int i = 0; i < puffs.Length; i++) images[i] = Image.FromFile(puffs[i]);
        using (var b = new Bitmap(size, size, PixelFormat.Format32bppArgb))
        {
            using (var g = Graphics.FromImage(b))
            {
                g.Clear(Color.FromArgb(0, 255, 255, 255));
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.CompositingMode = CompositingMode.SourceOver;
                g.CompositingQuality = CompositingQuality.HighQuality;
                for (int i = 0; i < count; i++)
                {
                    var puff = images[rnd.Next(images.Length)];
                    float scale = (float)(0.55 + rnd.NextDouble() * 0.7) * size / 1024f;
                    Stamp(g, puff, (float)(rnd.NextDouble() * size), (float)(rnd.NextDouble() * size), (float)(rnd.NextDouble() * 360.0), scale, size);
                }
            }
            Whiten(b, 1.15);
            b.Save(dst, ImageFormat.Png);
        }
        foreach (var im in images) im.Dispose();
    }
    public static void One(string puff, string dst, int size)
    {
        using (var img = Image.FromFile(puff))
        using (var b = new Bitmap(size, size, PixelFormat.Format32bppArgb))
        {
            using (var g = Graphics.FromImage(b))
            {
                g.Clear(Color.FromArgb(0, 255, 255, 255));
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(img, 0, 0, size, size);
            }
            Whiten(b, 1.0);
            b.Save(dst, ImageFormat.Png);
        }
    }
}
'@

$files = @()
foreach ($i in 1..10) { $p = Join-Path $src ("smoke_{0:00}.png" -f $i); if (Test-Path $p) { $files += $p } }
if ($files.Count -eq 0) { throw "no smoke puffs under $src" }
[Smoke]::Tile([string[]]$files, (Join-Path $dst 'SmogTile.png'), $Size, $Puffs, $Seed)
[Smoke]::One((Join-Path $src 'smoke_05.png'), (Join-Path $dst 'Puff.png'), 256)
Write-Output ("SmogTile.png ({0} px, {1} puffs of {2}) and Puff.png written to {3}" -f $Size, $Puffs, $files.Count, $dst)
