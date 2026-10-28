# RUN OVERLAY (Phase 2, 2026-10-27): draws a recorded run (Docs/Runs/<level>-<date>.csv, from RunRecorder) onto that
# level's plan (Docs/Design/validation/<level>-plan.png) using the frame line the validator writes into the level's
# report. The path is coloured by speed (blue = stopped, white = run speed, yellow = fast, red = very fast), wall
# hits are red crosses, landings small dots, stops (under 1 m/s for over 0.3 s) orange circles, markers labelled.
#   powershell -File Tools/RunOverlay.ps1 -Csv Docs/Runs/Level1-20261027-162819.csv
param([Parameter(Mandatory = $true)][string]$Csv, [string]$Project = (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)))
Add-Type -AssemblyName System.Drawing
$name = [System.IO.Path]::GetFileNameWithoutExtension($Csv)
$level = ($name -split "-")[0]
$report = Join-Path $Project "Docs\Design\validation\$level.md"
$plan = Join-Path $Project "Docs\Design\validation\$level-plan.png"
if (-not (Test-Path $report) -or -not (Test-Path $plan)) { throw "no validation report or plan for $level (run Momentum/Level Validator first)" }
$frameLine = (Select-String -Path $report -Pattern "Plan frame: centre \(([-0-9.]+), ([-0-9.]+)\), ([-0-9.]+) m across").Matches[0]
if ($null -eq $frameLine) { throw "the report has no plan frame line: regenerate it with the current validator" }
$cx = [double]$frameLine.Groups[1].Value; $cz = [double]$frameLine.Groups[2].Value; $size = [double]$frameLine.Groups[3].Value
$px = 2048
function ToPixel($x, $z) { return @((($x - $cx + $size / 2) / $size * $px), (($cz + $size / 2 - $z) / $size * $px)) }

$rows = Import-Csv $Csv
$bmp = [System.Drawing.Bitmap]::FromFile($plan)
$img = New-Object System.Drawing.Bitmap $bmp.Width, $bmp.Height
$g = [System.Drawing.Graphics]::FromImage($img); $g.DrawImage($bmp, 0, 0, $bmp.Width, $bmp.Height); $bmp.Dispose()
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
function SpeedColour($h) {
  if ($h -lt 2) { return [System.Drawing.Color]::FromArgb(230, 70, 110, 255) }
  if ($h -lt 9) { $t = ($h - 2) / 7; return [System.Drawing.Color]::FromArgb(230, [int](70 + 185 * $t), [int](110 + 145 * $t), 255) }
  if ($h -lt 18) { $t = ($h - 9) / 9; return [System.Drawing.Color]::FromArgb(230, 255, 255, [int](255 - 200 * $t)) }
  $t = [math]::Min(1, ($h - 18) / 14); return [System.Drawing.Color]::FromArgb(230, 255, [int](255 - 200 * $t), 55)
}
$prev = $null; $stopStart = -1; $stops = @(); $hits = @(); $lands = @(); $marks = @()
foreach ($r in $rows) {
  $x = [double]$r.x; $z = [double]$r.z; $h = [double]$r.hspeed; $t = [double]$r.t
  $p = ToPixel $x $z
  if ($null -ne $prev) { $pen = New-Object System.Drawing.Pen (SpeedColour $h), 4; $g.DrawLine($pen, [single]$prev[0], [single]$prev[1], [single]$p[0], [single]$p[1]); $pen.Dispose() }
  $prev = $p
  if ($h -lt 1) { if ($stopStart -lt 0) { $stopStart = $t; $stopAt = $p } } elseif ($stopStart -ge 0) { if ($t - $stopStart -gt 0.3) { $stops += ,@($stopAt, ($t - $stopStart)) }; $stopStart = -1 }
  if ($r.events -match "WallHit") { $hits += ,$p }
  if ($r.events -match "(^|\|)Land") { $lands += ,$p }
  if ($r.marker -ne "") { $marks += ,@($p, $r.marker, $t) }
}
foreach ($l in $lands) { $g.FillEllipse([System.Drawing.Brushes]::White, [single]($l[0] - 3), [single]($l[1] - 3), 6, 6) }
$red = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 255, 60, 60)), 3
foreach ($hp in $hits) { $g.DrawLine($red, [single]($hp[0] - 8), [single]($hp[1] - 8), [single]($hp[0] + 8), [single]($hp[1] + 8)); $g.DrawLine($red, [single]($hp[0] - 8), [single]($hp[1] + 8), [single]($hp[0] + 8), [single]($hp[1] - 8)) }
$orange = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 255, 150, 40)), 4
$font = New-Object System.Drawing.Font "Arial", 14, ([System.Drawing.FontStyle]::Bold)
foreach ($s in $stops) { $g.DrawEllipse($orange, [single]($s[0][0] - 16), [single]($s[0][1] - 16), 32, 32); $g.DrawString(("{0:0.0}s" -f $s[1]), $font, [System.Drawing.Brushes]::Orange, [single]($s[0][0] + 18), [single]($s[0][1] - 10)) }
foreach ($m in $marks) { $g.FillEllipse([System.Drawing.Brushes]::Magenta, [single]($m[0][0] - 9), [single]($m[0][1] - 9), 18, 18); $g.DrawString($m[1] + (" {0:0}s" -f $m[2]), $font, [System.Drawing.Brushes]::Magenta, [single]($m[0][0] + 12), [single]($m[0][1] - 24)) }
$out = [System.IO.Path]::ChangeExtension($Csv, $null).TrimEnd('.') + "-on-plan.png"
$img.Save($out, [System.Drawing.Imaging.ImageFormat]::Png); $g.Dispose(); $img.Dispose()
"wrote $out  (rows " + $rows.Count + ", stops " + $stops.Count + ", wall hits " + $hits.Count + ", landings " + $lands.Count + ", markers " + $marks.Count + ")"
