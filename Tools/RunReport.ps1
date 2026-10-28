# RUN REPORT (Phase 2, 2026-10-27): reads a RunRecorder CSV (Docs/Runs/<level>-<date>.csv) and prints what a level
# designer wants to know about the run: the time per course segment with its average and top speed, every STOP (under
# 1.5 m/s for over 0.3 s), every big LOSS (8 m/s or more gone within 0.3 s on the ground), every wall hit, the landings
# (how many, the big falls, how much speed each kept a quarter second later), the techniques used, the markers pressed.
#   powershell -File Tools/RunReport.ps1 -Csv Docs/Runs/Level1-20261027-162819.csv
param([Parameter(Mandatory = $true)][string]$Csv)
$rows = Import-Csv $Csv
$n = $rows.Count
if ($n -lt 10) { "too few rows"; exit }
$t = New-Object double[] $n; $h = New-Object double[] $n; $v = New-Object double[] $n; $gr = New-Object bool[] $n
for ($i = 0; $i -lt $n; $i++) { $t[$i] = [double]$rows[$i].t; $h[$i] = [double]$rows[$i].hspeed; $v[$i] = [double]$rows[$i].vspeed; $gr[$i] = $rows[$i].grounded -eq "1" }
$dt = ($t[$n - 1] - $t[0]) / ($n - 1)
$back = [math]::Max(1, [math]::Round(0.3 / $dt)); $fwd = [math]::Max(1, [math]::Round(0.25 / $dt))
function P($i) { return ("({0:0}, {1:0}, {2:0})" -f [double]$rows[$i].x, [double]$rows[$i].y, [double]$rows[$i].z) }

"RUN: " + [System.IO.Path]::GetFileName($Csv) + "  " + ("{0:0.0}" -f $t[$n - 1]) + " s, " + $n + " rows (" + ("{0:0}" -f (1 / $dt)) + " Hz), top speed " + ("{0:0.0}" -f ($h | Measure-Object -Maximum).Maximum) + " m/s, final rank " + $rows[$n - 1].rank
""
"SEGMENTS (time in each, average and top speed while in it):"
$seg = ""; $start = 0
$segRows = @()
for ($i = 0; $i -lt $n; $i++) {
  if ($rows[$i].segment -ne $seg) {
    if ($seg -ne "") { $segRows += ,@($seg, $start, ($i - 1)) }
    $seg = $rows[$i].segment; $start = $i
  }
}
$segRows += ,@($seg, $start, ($n - 1))
foreach ($s in $segRows) {
  $a = $s[1]; $b = $s[2]; $sum = 0; $max = 0; $stopped = 0
  for ($i = $a; $i -le $b; $i++) { $sum += $h[$i]; if ($h[$i] -gt $max) { $max = $h[$i] }; if ($h[$i] -lt 1.5) { $stopped += $dt } }
  "  {0,-42} {1,6:0.0} s   avg {2,5:0.0}   top {3,5:0.0}   stopped {4,4:0.0} s" -f $s[0], ($t[$b] - $t[$a]), ($sum / ($b - $a + 1)), $max, $stopped
}
""
"STOPS (under 1.5 m/s for over 0.3 s):"
$i = 0; $count = 0
while ($i -lt $n) {
  if ($h[$i] -lt 1.5) { $j = $i; while ($j -lt $n -and $h[$j] -lt 1.5) { $j++ }; $d = $t[[math]::Min($j, $n - 1)] - $t[$i]; if ($d -gt 0.3) { "  t {0,6:0.0} s  {1,5:0.0} s long  at {2}  in {3}" -f $t[$i], $d, (P $i), $rows[$i].segment; $count++ }; $i = $j } else { $i++ }
}
if ($count -eq 0) { "  none" }
""
"BIG LOSSES (8 m/s or more gone within 0.3 s on the ground):"
$last = -10; $count = 0
for ($i = $back; $i -lt $n; $i++) {
  if ($gr[$i] -and ($h[$i - $back] - $h[$i]) -ge 8 -and ($t[$i] - $last) -gt 0.5) {
    $ev = ""; for ($k = $i - $back; $k -le $i; $k++) { if ($rows[$k].events -ne "") { $ev += $rows[$k].events + " " } }
    "  t {0,6:0.0} s  {1,5:0.0} -> {2,4:0.0} m/s  at {3}  in {4}  events: {5}" -f $t[$i], $h[$i - $back], $h[$i], (P $i), $rows[$i].segment, $ev.Trim(); $last = $t[$i]; $count++
  }
}
if ($count -eq 0) { "  none" }
""
"WALL HITS:"
$count = 0
for ($i = 0; $i -lt $n; $i++) { if ($rows[$i].events -match "WallHit") { "  t {0,6:0.0} s  at {1}  speed {2:0.0}  in {3}" -f $t[$i], (P $i), $h[$i], $rows[$i].segment; $count++ } }
if ($count -eq 0) { "  none" }
""
"LANDINGS (fall speed from the event; speed kept a quarter second later):"
$lands = 0; $big = 0; $keptSum = 0
for ($i = 0; $i -lt $n; $i++) {
  if ($rows[$i].events -match "(^|\|)Land:([0-9.]+)") {
    $fall = [double]$Matches[2]; $lands++
    $after = $h[[math]::Min($n - 1, $i + $fwd)]; $before = $h[[math]::Max(0, $i - 2)]
    $keptSum += $(if ($before -gt 0.1) { $after / $before } else { 1 })
    if ($fall -ge 14) { $big++; "  big fall  t {0,6:0.0} s  {1:0.0} m/s down  {2:0.0} -> {3:0.0} m/s  at {4}" -f $t[$i], $fall, $before, $after, (P $i) }
  }
}
"  " + $lands + " landings, " + $big + " over the roll threshold (14 m/s), speed kept on average " + ("{0:0}" -f (100 * $keptSum / [math]::Max(1, $lands))) + "%"
""
"TECHNIQUES:"
$counts = @{}
foreach ($r in $rows) { if ($r.events -ne "") { foreach ($e in $r.events -split "\|") { $k = ($e -split ":")[0]; if ($k -ne "") { $counts[$k] = 1 + $(if ($counts.ContainsKey($k)) { $counts[$k] } else { 0 }) } } } }
($counts.GetEnumerator() | Sort-Object Value -Descending | ForEach-Object { "  {0,-18} {1}" -f $_.Key, $_.Value }) -join "`n"
""
"MARKERS:"
$count = 0
for ($i = 0; $i -lt $n; $i++) { if ($rows[$i].marker -ne "") { "  {0,-5} t {1,6:0.0} s  at {2}  speed {3:0.0}  in {4}" -f $rows[$i].marker, $t[$i], (P $i), $h[$i], $rows[$i].segment; $count++ } }
if ($count -eq 0) { "  none pressed" }
