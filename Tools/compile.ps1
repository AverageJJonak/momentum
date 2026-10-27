$ErrorActionPreference = 'Continue'
# MOMENTUM compile check without Unity: Roslyn csc + response files. Lives in Tools so it survives the scratchpad.
Get-ChildItem $env:TEMP -Directory -Filter 'momentum-compile-*' -ErrorAction SilentlyContinue | Where-Object { $_.LastWriteTime -lt (Get-Date).AddHours(-2) } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
$sp = Join-Path $env:TEMP ('momentum-compile-' + $PID)   # per process, so parallel builders never share an rsp
New-Item -ItemType Directory -Force $sp | Out-Null
$csc = 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\Roslyn\csc.exe'
$proj = 'C:\Users\D\MovementShooter'
$data = 'C:\Program Files\Unity\Hub\Editor\6000.5.10f1\Editor\Data'   # the project's own version (three are installed)
if (-not (Test-Path (Join-Path $data 'Managed\UnityEngine'))) { $data = $null }
if ($data -eq $null) {
    foreach ($cand in @((Get-ChildItem 'C:\Program Files\Unity\Hub\Editor' -ErrorAction SilentlyContinue | ForEach-Object { Join-Path $_.FullName 'Editor\Data' }))) {
        if ($data -eq $null -and (Test-Path (Join-Path $cand 'Managed\UnityEngine'))) { $data = $cand }
    }
}
if ($data -eq $null) { Write-Output 'NO UNITY DATA FOLDER'; exit 1 }

$seen = @{}
$rt = @()
function Add-Ref([string]$p) { $n = [IO.Path]::GetFileName($p); if (-not $script:seen.ContainsKey($n)) { $script:seen[$n] = 1; $script:rt += $p } }
Add-Ref (Join-Path $data 'NetStandard\ref\2.1.0\netstandard.dll')
Get-ChildItem (Join-Path $data 'Managed\UnityEngine') -Filter 'UnityEngine*.dll' | ForEach-Object { Add-Ref $_.FullName }
Get-ChildItem (Join-Path $proj 'Library\ScriptAssemblies') -Filter '*.dll' | Where-Object { $_.Name -notlike 'Assembly-CSharp*' -and $_.Name -notmatch 'Editor|Tests' } | ForEach-Object { Add-Ref $_.FullName }
$srcs = Get-ChildItem (Join-Path $proj 'Assets\Scripts') -Recurse -Filter '*.cs' | Where-Object { $_.FullName -notmatch '\\Editor\\' }

$rsp = Join-Path $sp 'runtime.rsp'
$lines = @('-nostdlib+', '-t:library', '-langversion:9.0', '-nowarn:CS0108', '-define:ENABLE_INPUT_SYSTEM;UNITY_6000_0_OR_NEWER;UNITY_2022_1_OR_NEWER', ('-out:"' + (Join-Path $sp 'runtime.dll') + '"'))
foreach ($r in $rt) { $lines += ('-r:"' + $r + '"') }
foreach ($s in $srcs) { $lines += ('"' + $s.FullName + '"') }
$lines | Out-File -Encoding ascii $rsp
& $csc -noconfig "@$rsp"
if ($LASTEXITCODE -ne 0) { Write-Output 'COMPILE FAILED (runtime)'; exit 1 }
Write-Output 'COMPILE OK (runtime)'

$eds = Get-ChildItem (Join-Path $proj 'Assets\Scripts') -Recurse -Filter '*.cs' | Where-Object { $_.FullName -match '\\Editor\\' }
if ($eds.Count -eq 0) { Write-Output 'COMPILE OK (editor)'; exit 0 }
Get-ChildItem (Join-Path $data 'Managed\UnityEngine') -Filter 'UnityEditor*.dll' | ForEach-Object { Add-Ref $_.FullName }
Get-ChildItem (Join-Path $data 'Managed') -Filter 'UnityEditor*.dll' | ForEach-Object { Add-Ref $_.FullName }
Get-ChildItem (Join-Path $proj 'Library\ScriptAssemblies') -Filter '*.dll' | Where-Object { $_.Name -notlike 'Assembly-CSharp*' } | ForEach-Object { Add-Ref $_.FullName }
Add-Ref (Join-Path $sp 'runtime.dll')
$rsp2 = Join-Path $sp 'editor.rsp'
$lines2 = @('-nostdlib+', '-t:library', '-langversion:9.0', '-nowarn:CS0108', '-define:ENABLE_INPUT_SYSTEM;UNITY_EDITOR;UNITY_6000_0_OR_NEWER;UNITY_2022_1_OR_NEWER', ('-out:"' + (Join-Path $sp 'editor.dll') + '"'))
foreach ($r in $rt) { $lines2 += ('-r:"' + $r + '"') }
foreach ($s in $eds) { $lines2 += ('"' + $s.FullName + '"') }
$lines2 | Out-File -Encoding ascii $rsp2
& $csc -noconfig "@$rsp2"
if ($LASTEXITCODE -ne 0) { Write-Output 'COMPILE FAILED (editor)'; exit 1 }
Write-Output 'COMPILE OK (editor)'
