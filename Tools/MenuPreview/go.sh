#!/bin/bash
# MENU CITY PREVIEW. Compiles the menu city's real generator (SheetBaker, CityTextures, MenuCity, MenuSky) against a small
# stand-in for Unity and software-renders it from the menu camera's own orbit, so the backdrop can be looked at without
# opening the editor. Approximate: no rain, nothing that moves, no holograms, a rough bloom.
#   usage:  ./go.sh <width> <tex|notex> <t,t,t...> [camX,camY,camZ,lookX,lookY,lookZ]
#   e.g.    ./go.sh 1280 notex 0,95,215        (t is seconds along MenuBackdrop.Drift's orbit)
# Output: ./out/menu_tNNN.png (and _veil.png, with the menu's 30 % veil over it); `tex` also dumps the textures.
HERE="$(cd "$(dirname "$0")" && pwd)"
S="$HERE/../../Assets/Scripts"
CSC="/c/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Current/Bin/Roslyn/csc.exe"; [ -f "$CSC" ] || CSC="/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/Roslyn/csc.exe"
mkdir -p "$HERE/out"
"$CSC" -nologo -langversion:9.0 -optimize+ -platform:x64 -r:System.Drawing.dll -out:"$(cygpath -w "$HERE/preview.exe")" "$(cygpath -w "$HERE/UnityStub.cs")" "$(cygpath -w "$HERE/KitStub.cs")" "$(cygpath -w "$HERE/Render.cs")" "$(cygpath -w "$S/SheetBaker.cs")" "$(cygpath -w "$S/CityTextures.cs")" "$(cygpath -w "$S/MenuCity.cs")" "$(cygpath -w "$S/MenuCityDense.cs")" "$(cygpath -w "$S/MenuCityOuter.cs")" "$(cygpath -w "$S/MenuCityNight.cs")" "$(cygpath -w "$S/MenuCityDeco.cs")" "$(cygpath -w "$S/MenuCityGotham.cs")" "$(cygpath -w "$S/MenuCityHouse.cs")" "$(cygpath -w "$S/MenuCitySigns.cs")" "$(cygpath -w "$S/MenuCityAir.cs")" "$(cygpath -w "$S/MenuSky.cs")" 2>&1 | grep -v "warning" | head -20
"$HERE/preview.exe" "$(cygpath -w "$HERE/out")" "${1:-1280}" "${2:-notex}" "${3:-0,95,215}" $4 2>&1 | tail -8
