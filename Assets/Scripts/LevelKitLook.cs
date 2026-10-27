using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// READABILITY (2026-10-16, after play: "the floors and walls are too dark to see anything"). The rule and the maths
// behind every number WorldLook, the Momentum/Toon shader and LevelKit's palettes set that day. This is a movement
// game: a floor the player cannot see is a design failure, whatever the painting says. The look stays Batman: The
// Animated Series / 1930s poster — flat bands, one lit face per mass, black paper in the SKY and the DISTANCE — but the
// surfaces the player runs on and into read at all times.
//
// THE RULES (linear luminance, Y = 0.2126 R + 0.7152 G + 0.0722 B; 0.18 linear is 0.46 sRGB: clearly visible)
//   1. a FLOOR in the ramp's shadow band  >= 0.18   a floor in the lit band  0.35–0.50
//      a WALL  in the ramp's shadow band  >= 0.12   a wall  in the lit band  0.28–0.40
//      so the floor is a step lighter than the wall and the eye finds the ground. Only the sky, the far city and
//      deliberate black-lacquer trim (the HARD LINE beads, the ceilings) sit under 0.05.
//   2. the shadow band is NOT black: the key's dark band = its lit band x _ShadowTone (0.45); the ambient is kept at
//      _AmbientCrush 0.8 (was 0.5) over a floor of _AmbientFloor 0.10 (was 0.02) of the albedo.
//   3. one lit face per mass stays: lit : shadow = 1 : 0.45 = 2.2 : 1 (not 10 : 1).
//
// WHAT THE SHADER DOES TO A TEXEL (MomentumToonPass.hlsl), as luminance:
//   Y = A x ( amb x 0.8 x AO + 0.10 )            the paper: A the albedo's luminance x the texture's mean (x 0.9–0.96),
//                                                 amb the trilight ambient for the face: SKY for a floor (normal up),
//                                                 EQUATOR for a wall, GROUND for an underside
//     + A x K x band                              the key: K = intensity x luminance(colour); band = 1 in the lit band
//                                                 (N.L x shadow >= _Band2 0.55), 0.45 in the dark band
//     + A x pool x ring                           the lamps, posterised to rings of 0.22 / 0.56 / 1.0 of the lamp
//     + rim                                       the back light (0.3 under toon) on faces turned from the key; the
//                                                 shader's own silhouette band (0.6) on the edge
//
// WHY THE FLOORS WERE BLACK: the keys RAKE (elevation 12–30°, the paintings' long shadows), so a floor's N.L is
// sin(elevation) = 0.21–0.50, UNDER _Band2 0.55: every floor in the game is in the dark band, always. Inside a roofed
// room the key is shadowed out too. With the dark band at 0 the floor had only the crushed ambient: a charcoal floor
// (A 0.10) x (0.1 x 0.5 + 0.02) = 0.007. Black paper underfoot.
//
// ONE ZONE WORKED THROUGH — UTOPIA (Levels 1–4, the civic streets by day):
//   the key: Noon 1.4 x (1, 0.86, 0.62) -> K = 1.4 x 0.872 = 1.22
//   the ambient: sky (0.34, 0.30, 0.22) Y 0.30 (a floor's), equator (0.22, 0.18, 0.12) Y 0.18 (a wall's)
//   the floor: FloorStone (0.33, 0.30, 0.23) Y 0.30 x the terrazzo's 0.9 -> A 0.27
//     shadow band: 0.27 x (0.30 x 0.8 + 0.10 + 1.22 x 0.45) = 0.27 x 0.89 = 0.24   (>= 0.18: rule 1)
//     lit band:    0.27 x (0.24 + 0.10 + 1.22)              = 0.27 x 1.56 = 0.42   (0.35–0.50)
//   the wall: WallOchre (0.30, 0.255, 0.17) Y 0.26 x the plaster's 0.96 -> A 0.25
//     shadow band: 0.25 x (0.18 x 0.8 + 0.10 + 0.55) = 0.25 x 0.80 = 0.20          (>= 0.12)
//     lit band:    0.25 x (0.15 + 0.10 + 1.22)       = 0.25 x 1.47 = 0.37          (0.28–0.40)
//   lit face : shadow face = 0.37 : 0.20 = 1.85 : 1 (the ambient narrows the shader's 2.2 : 1); floor : wall in the
//   shade = 0.24 : 0.20, the floor the lighter. A roofed hall has no lit band at all: its floor sits at 0.24, its walls
//   at 0.20, the lamps' pools over them. Before: the cream wall (A 0.69) blew out at 1.1 on its lit face and sat at
//   0.04 in the shade (26 : 1); the floor at 0.007.
//
// THE OTHER ZONES, the same sums (floor shadow / wall shadow; the lit band where the zone has one):
//   Underbelly   K 0.55 (the day key through a roof) or 0.56 (the Underground's): concrete A 0.25 -> 0.20; brick A 0.25
//                -> 0.12 (BrickWarm; the bible's brick gave 0.09). Fog 25 -> 90 m, the colour a dark umber, not black.
//   Office       K 1.03 (Level 5's key, roofed: the dark band everywhere): the chequer averages A 0.26 -> 0.19 /
//                lit 0.33; the cobalt wall A 0.21 -> 0.14 / lit 0.26 (the chandeliers' pools do the rest). Fog 30 -> 120.
//   Works        K 0.56 from 45°: the ONE zone whose floors are in the lit band (N.L 0.71): steel terrazzo A 0.29 ->
//                0.27 lit (the globes' pools reach 0.4); the tile A 0.51 -> 0.25 shade / 0.41 lit. Fog 30 -> 110.
//   Surface      Dawn K 1.04 at 14°, Sunset 0.88 at 12°, Night 0.59 at 25°: the roofs' gravel A 0.33 -> 0.27 / 0.23 /
//                0.19 in the shade (night ambient sky Y 0.27: violet, high for a night, the masses still black in the
//                haze); the parapets A 0.33 -> 0.24 / 0.20 / 0.16, lit 0.43 by day.
//
// THE RIM: the directional back light at 0.3 under toon (0.6 x 0.5), not the full 0.6: with the key's dark band at
// 0.45 a full 0.6 cool back light at night (0.45 x 0.59 + 0.6 x 0.6 = 0.63) would sit OVER the lit face (0.59) and
// the masses would light from the wrong side; the shader's silhouette rim carries 0.6.
//
// THE SIGNALS stay brighter than any lit face (0.5 at most): the HARD LINE floor lines (Unlit, Y 0.72–1.26 by zone), the
// lamp cream (1.9, 1.7, 1.3), the cut's cyan (0.3, 2, 2.4), the danger red (2.5, 0.15, 0.1), the mustard hazard
// (1.25, 0.95, 0.32). The brightness knob (WorldLook.Exposure, Settings "BRIGHTNESS") moves everything together.

/// <summary>
/// WHAT THE ROOM REFLECTS. Every roofed room gets a box-projected reflection
/// probe the size of itself, rendered once when the level stands and its light
/// is set: the steel plate mirrors the room it is in, the glossy tiles carry
/// the lamp above them, the damp concrete shows the floodlight, and none of it
/// costs a frame after the build. Open-sky rooms keep the sky's own reflection.
/// Forward+ blends the probes per pixel, so a doorway between two rooms is a
/// crossfade, not a seam. Room() queues one; GameBootstrap renders them all
/// once WorldLook has set the sky, sun and fog they will show.
/// </summary>
public static partial class LevelKit
{
    static readonly List<ReflectionProbe> probes = new List<ReflectionProbe>();

    /// How many probes the current build has queued and not yet rendered
    public static int ProbeCount => probes.Count;

    /// A probe for the room `min`..`max` (world). Rooms under 4 m in any axis (a duct, a closet) share their neighbour's.
    public static void Probe(Transform parent, Vector3 min, Vector3 max)
    {
        Vector3 ext = max - min;
        if (ext.x < 4f || ext.y < 2.2f || ext.z < 4f) return;
        var go = new GameObject("Probe");
        go.transform.SetParent(parent, false);
        go.transform.position = (min + max) * 0.5f;
        go.transform.rotation = Quaternion.identity;
        var p = go.AddComponent<ReflectionProbe>();
        p.mode = ReflectionProbeMode.Realtime;
        p.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
        p.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
        p.boxProjection = true;
        p.size = ext + Vector3.one * 2f;   // a metre past each wall: the blend runs into the doorway, not short of it
        p.center = Vector3.zero;
        p.blendDistance = 1.5f;
        p.resolution = 128;
        p.hdr = true;
        p.shadowDistance = 40f;
        p.nearClipPlane = 0.2f;
        p.farClipPlane = 900f;
        p.clearFlags = ReflectionProbeClearFlags.Skybox;
        p.cullingMask = ProbeMask;
        p.intensity = 1f;
        p.importance = 1;
        probes.Add(p);
    }

    /// What a probe sees: the world, not the player and the units (Ignore Raycast, they move), not the mines, not the hands (the Viewmodel layer)
    static int ProbeMask => ~((1 << 2) | (1 << HoverMine.Layer) | Viewmodel.Mask);

    /// Render every probe queued since the build began: called once the sky, sun and fog are set
    public static void RenderProbes()
    {
        if (probes.Count == 0) return;
        QualitySettings.realtimeReflectionProbes = true;
        foreach (var p in probes) if (p != null) p.RenderProbe();
        probes.Clear();
    }

    /// A new build: the previous level's probes went with it
    public static void ClearProbes() => probes.Clear();
}
