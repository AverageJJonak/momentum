# IMPORT TEXTURES (2026-10-23): turns ambientCG 2K JPG sets in Incoming\ambientcg into the game's surface sets under
# Assets\Resources\Surfaces\<Set>\ — Color.png, Normal.png (the GL normal), MetalGloss.png (metallic 0 in RGB, smoothness
# = 1 - roughness in alpha, as URP Lit and Momentum/Toon read it), Occlusion.png (white when the set has none) — all at
# 1024, plus Surfaces\sets.json with each colour map's mean linear luminance (LevelKit normalises the tint by it, so a
# surface's average colour is the palette's colour whatever the photo's brightness; the readability maths hold).
#   powershell -NoProfile -ExecutionPolicy Bypass -File Tools\ImportTextures.ps1            (all sets listed below)
#   ... -File Tools\ImportTextures.ps1 Plaster001 Marble021                               (just those)
# Editor\SurfaceImportSettings.cs sets the Unity import flags (normal map type, linear data maps) on first import.
param([string[]]$Only)

$ErrorActionPreference = 'Stop'
$proj = Split-Path -Parent $PSScriptRoot
$src = Join-Path $proj 'Incoming\ambientcg'
$dst = Join-Path $proj 'Assets\Resources\Surfaces'
$size = 1024
# The sets the game wires (LevelKitSurfaceSets.cs maps each Surf kind to one of these)
$sets = @('Plaster001', 'Concrete034', 'Marble021', 'Marble016', 'Bricks106A', 'PavingStones046', 'Tiles036', 'Tiles012', 'WoodFloor051', 'Metal034', 'DiamondPlate009', 'Carpet013', 'Leather026',
          'Concrete046', 'Concrete030', 'Concrete028', 'Concrete036', 'Concrete045', 'Concrete031', 'Gravel043', 'Metal041B', 'Rust009', 'Terrazzo013', 'Asphalt033', 'Concrete048', 'PaintedPlaster017')
if ($Only -and $Only.Count -gt 0) { $sets = $Only }

Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
public static class TexConv
{
    public static Bitmap Load(string path, int size)
    {
        using (var img = Image.FromFile(path))
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(img, 0, 0, size, size);
            }
            return bmp;
        }
    }
    static byte[] Bytes(Bitmap b, out int stride)
    {
        var r = new Rectangle(0, 0, b.Width, b.Height);
        var d = b.LockBits(r, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        stride = d.Stride;
        var bytes = new byte[stride * b.Height];
        Marshal.Copy(d.Scan0, bytes, 0, bytes.Length);
        b.UnlockBits(d);
        return bytes;
    }
    static void Write(Bitmap b, byte[] bytes)
    {
        var r = new Rectangle(0, 0, b.Width, b.Height);
        var d = b.LockBits(r, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        Marshal.Copy(bytes, 0, d.Scan0, bytes.Length);
        b.UnlockBits(d);
    }
    /// Saves the colour map and returns its mean LINEAR luminance
    public static double SaveColor(string src, string dst, int size)
    {
        using (var b = Load(src, size))
        {
            int stride; var px = Bytes(b, out stride);
            double sum = 0; int n = 0;
            for (int i = 0; i < px.Length; i += 4)
            {
                double bl = Lin(px[i]), gr = Lin(px[i + 1]), rd = Lin(px[i + 2]);
                sum += 0.2126 * rd + 0.7152 * gr + 0.0722 * bl; n++;
            }
            b.Save(dst, ImageFormat.Png);
            return n > 0 ? sum / n : 0.5;
        }
    }
    static double Lin(byte v) { double c = v / 255.0; return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4); }
    public static void SaveAsIs(string src, string dst, int size)
    {
        using (var b = Load(src, size)) b.Save(dst, ImageFormat.Png);
    }
    /// Metallic 0 in RGB, smoothness in alpha from the roughness map
    public static void SaveMetalGloss(string roughness, string dst, int size)
    {
        using (var r = Load(roughness, size))
        {
            int stride; var px = Bytes(r, out stride);
            var outp = new byte[px.Length];
            for (int i = 0; i < px.Length; i += 4)
            {
                outp[i] = 0; outp[i + 1] = 0; outp[i + 2] = 0;
                outp[i + 3] = (byte)(255 - px[i + 1]);   // green of the grey roughness
            }
            using (var o = new Bitmap(size, size, PixelFormat.Format32bppArgb)) { Write(o, outp); o.Save(dst, ImageFormat.Png); }
        }
    }
    public static void SaveWhite(string dst, int size)
    {
        using (var o = new Bitmap(size, size, PixelFormat.Format32bppArgb)) { using (var g = Graphics.FromImage(o)) g.Clear(Color.White); o.Save(dst, ImageFormat.Png); }
    }
}
'@

New-Item -ItemType Directory -Force $dst | Out-Null
$index = @()
$existing = Join-Path $dst 'sets.json'
if (Test-Path $existing) {
    $old = (Get-Content $existing -Raw | ConvertFrom-Json).sets
    foreach ($o in $old) { if ($sets -notcontains $o.name) { $index += @{ name = $o.name; mean = [double]$o.mean } } }
}
foreach ($s in $sets) {
    $folder = Join-Path $src $s
    if (-not (Test-Path $folder)) { Write-Output "SKIP $s (not in Incoming\ambientcg)"; continue }
    $color = Get-ChildItem $folder -Filter '*_Color.jpg' | Select-Object -First 1
    $normal = Get-ChildItem $folder -Filter '*_NormalGL.jpg' | Select-Object -First 1
    $rough = Get-ChildItem $folder -Filter '*_Roughness.jpg' | Select-Object -First 1
    $ao = Get-ChildItem $folder -Filter '*_AmbientOcclusion.jpg' | Select-Object -First 1
    if (-not $color -or -not $normal -or -not $rough) { Write-Output "SKIP $s (maps missing)"; continue }
    $out = Join-Path $dst $s
    New-Item -ItemType Directory -Force $out | Out-Null
    $mean = [TexConv]::SaveColor($color.FullName, (Join-Path $out 'Color.png'), $size)
    [TexConv]::SaveAsIs($normal.FullName, (Join-Path $out 'Normal.png'), $size)
    [TexConv]::SaveMetalGloss($rough.FullName, (Join-Path $out 'MetalGloss.png'), $size)
    if ($ao) { [TexConv]::SaveAsIs($ao.FullName, (Join-Path $out 'Occlusion.png'), $size) } else { [TexConv]::SaveWhite((Join-Path $out 'Occlusion.png'), $size) }
    $index += @{ name = $s; mean = [math]::Round($mean, 4) }
    Write-Output ("{0,-18} mean luminance {1:0.000}{2}" -f $s, $mean, $(if ($ao) { '' } else { '  (no AO: white)' }))
}
$json = @{ sets = $index } | ConvertTo-Json -Depth 3
[IO.File]::WriteAllText((Join-Path $dst 'sets.json'), $json, (New-Object System.Text.UTF8Encoding($false)))   # no byte-order mark: Unity's JSON reader wants none
Write-Output ("sets.json written with {0} set(s)" -f $index.Count)
