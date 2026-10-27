using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// WHAT LIVES IN THE MENU'S CITY. The other half of MenuCity: everything that
/// moves, blinks or sweeps, built from the sites the still half recorded and
/// handed to CityLife (one Update for the lot). GOTHAM, 1939, after dark:
///   THE ELEVATED   trains nose to tail on both rail rings, amber windows, a
///                  cream lamp on the nose.
///   THE AIR        ZEPPELINS on closed lanes (closed, so nothing ever pops in
///                  or out at the end of a lane): one inside the Court over the
///                  spires' finials, running against the camera's orbit so they
///                  cross the House's lit body often; one outside the camera's
///                  corridor over the six-hundred ring, the other way; charcoal
///                  and cream hulls by turns, the company's name on their
///                  flanks, a cream searchlight walking the roofs under each.
///                  A lane of MAIL PLANES further out and higher, fast, a red
///                  port lamp and a cream starboard one. Two more zeppelins and
///                  two more planes crossing the whole city under the cloud.
///   THE POLICE     ONE police blimp, fat and dark, on a slow round over the
///                  whole city with a searchlight cone sweeping the roofs, and
///                  two small patrol blimps about the House's windows: the dark
///                  things in the air, as the House is the one dark thing on
///                  the ground.
///   THE BEAMS      the three searchlights from the ground in the far city
///                  sweep (thin cones on the drums the still half built; the
///                  still beams are hidden); the crowns' fans stay still.
///   THE SIGNS      the company's name in LIT CREAM BLOCK LETTERS on charcoal
///                  plates in brass frames: standing on the Court's first
///                  setbacks (every other tall spire, facing the orbit), and a
///                  sign bridge on lattice pylons over the south approach.
///   THE FLOORS     a floor on each short spire that fills with light and
///                  empties again; sky-tracker searchlights on three finials;
///                  a red light on everything tall.
/// THE TELL: on every sign one letter dies for a moment now and then, or (on
/// every other sign) goes RED for that moment. The House's own mark never does.
/// No lights are made here: every lamp and beam is an unlit box.
/// </summary>
public static partial class MenuCity
{
    static Mesh beamMesh;

    public static void BuildLife(Transform living, CityLife life,
        List<Renderer> flickers, List<float> fPeriod, List<float> fPhase, List<float> fDuty,
        List<Renderer> beacons, List<float> bPhase)
    {
        // A red light on everything tall. Big: they are read from a kilometre away, through rain
        foreach (var p in Beacons)
        {
            float size = p.y > 500f ? 5f : p.y > 300f ? 3.6f : 2.6f;
            var go = LevelKit.Prim(living, p, Vector3.one * size, CityKit.BeaconRed, "Beacon", PrimitiveType.Sphere);
            beacons.Add(go.GetComponent<Renderer>());
            bPhase.Add(Random.value);
        }

        // The elevated's trains: two on each ring, half a lap apart, the outer ring running the other way
        var inner = RailPath(RailRadius, RailY + 0.2f);
        var outer = RailPath(OuterRailRadius, OuterRailY + 0.2f);
        System.Array.Reverse(outer);
        Train(living, life, inner, 7, 0.0085f, 0f);
        Train(living, life, inner, 7, 0.0085f, 0.5f);
        Train(living, life, outer, 9, 0.0056f, 0.2f);
        Train(living, life, outer, 9, 0.0056f, 0.7f);

        // THE AIR. The inner lane sits over the Court's finials (204 m, and the trackers' lamps on them), high enough
        // that its searchlights end above them; the outer lane and the mail planes stand outside the camera's corridor
        // (470 m, up to 260 m high) and above it. A positive speed runs north-east-south, against the camera's orbit
        AirshipLane(living, life, 232f, 300f, 2, 0.0062f, false);
        AirshipLane(living, life, 560f, 340f, 3, -0.0028f, true);
        MailLane(living, life, 620f, 385f, 2, -0.0108f);

        // THE COURT, dressed. Outward faces only: those are the ones the orbit looks at from two hundred metres. Every
        // other tall spire carries a sign on its first setback, the short ones the floors that fill and empty; three
        // of the tall ones track the cloud with a searchlight from their finials
        int signs = 0, n = 0;
        foreach (var site in Heroes)
        {
            bool tall = n++ % 2 == 0;
            float span = site.y1 - site.y0, half = site.d * 0.5f;
            if (tall && signs < 3 && n % 4 != 3)   // three of the company's signs on the Court: the sign budget (MenuCityGotham)
            {
                string line = signs % 3 == 2 ? ShortTagline(signs) : LevelKit.Company;
                RoofSign(living, site.foot + Vector3.up * (site.y1 + 1.3f) + site.outward * (half - 0.9f), site.outward, site.w * 0.92f, line, signs, 3f,
                    flickers, fPeriod, fPhase, fDuty);
                signs++;
            }
            if (!tall)
            {
                // A floor that fills with light and empties again: its own little mesh, so CityLife can switch it
                var sb = new SheetBaker();
                float y = site.y0 + span * Random.Range(0.2f, 0.9f);
                sb.Wall(lightRibbons, new Vector3(site.foot.x, y, site.foot.z) + site.outward * (half + 0.5f), site.outward, site.w * 0.96f, 3.2f,
                    new Vector2(0f, 0.2f), new Vector2(site.w / 3.4f / CityTextures.AtlasCells, 0.23f));
                foreach (var r in sb.Flush(living, "LivingFloor"))
                {
                    flickers.Add(r);
                    fPeriod.Add(Random.Range(11f, 30f));
                    fPhase.Add(Random.value);
                    fDuty.Add(Random.Range(0.35f, 0.7f));
                }
            }
        }

        // THE THREE SEARCHLIGHTS FROM THE GROUND sweep: a thin additive cone on each far drum the still half built
        // (MenuCityAir), slow, crossing over the plaza; the still beams they stand in for are hidden
        if (GroundLights.Count > 0)
        {
            var root = living.parent;
            if (root != null)
                foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
                    if (r.sharedMaterial == StillBeam) r.enabled = false;
            for (int i = 0; i < GroundLights.Count; i++)
            {
                var g = GroundLights[i];
                CityKit.GroundSearchlight(living, g.at, g.az, g.pitch, g.length, 9f + 5f * i, 0.055f + 0.012f * i, i * 2.1f);
            }
        }

        // The company's name over the south approach: a sign bridge on lattice pylons where the avenue meets the
        // plaza, between the plaza's ring of lamp standards and the first pair down the approach
        RoofSign(living, Origin + new Vector3(0f, 0f, -(Plaza + 13f)), Vector3.back, 31.2f, LevelKit.Company, 7, 22f, flickers, fPeriod, fPhase, fDuty);

        // THE POLICE BLIMP: one, fat and dark, on a slow round outside the camera's corridor and over everything,
        // its searchlight cone sweeping the roofs; and two small patrol blimps close in about the House's windows
        CityKit.PoliceBlimp(living, life, Origin, 760f, 380f);
        CityKit.Patrol(living, life, Origin, 150f, 190f, 2);

        // The far air: two zeppelins crossing the whole city under the cloud, two mail planes over them
        CityKit.Airship(living, life, Origin + new Vector3(-1500f, 330f, 380f), Origin + new Vector3(1500f, 350f, -120f), 0.0035f, 0.45f, true);
        CityKit.Airship(living, life, Origin + new Vector3(900f, 420f, -1400f), Origin + new Vector3(-700f, 400f, 1400f), 0.003f, 0.1f);
        CityKit.Aircraft(living, life, Origin + new Vector3(-1800f, 560f, 600f), Origin + new Vector3(1800f, 600f, -300f), 0.006f, 0.2f);
        CityKit.Aircraft(living, life, Origin + new Vector3(1600f, 640f, 900f), Origin + new Vector3(-1600f, 620f, -700f), 0.005f, 0.7f);
    }

    // ---------------------------------------------------------------- the elevated

    /// The elevated's train: iron cars nose to tail on a closed line, a ribbon of amber windows down each side, a
    /// cream lamp on the nose
    static void Train(Transform living, CityLife life, Vector3[] line, int cars, float speed, float phase)
    {
        float total = 0f;
        for (int i = 1; i < line.Length; i++) total += Vector3.Distance(line[i - 1], line[i]);
        if (total < 60f) return;
        float gap = 17f / total;
        for (int i = 0; i < cars; i++)
        {
            var car = new GameObject("TramCar");
            car.transform.SetParent(living);
            car.transform.position = line[0];
            LevelKit.Prim(car.transform, line[0], new Vector3(3f, 3f, 16f), deck, "Body").transform.localPosition = new Vector3(0f, 1.7f, 0f);
            LevelKit.Prim(car.transform, line[0], new Vector3(3.12f, 0.8f, 14f), glowWarm, "Windows").transform.localPosition = new Vector3(0f, 2.1f, 0f);
            if (i == 0) LevelKit.Prim(car.transform, line[0], new Vector3(2f, 1f, 0.4f), glowWhite, "Nose").transform.localPosition = new Vector3(0f, 1.4f, 8.1f);
            life.AddPath(car.transform, line, speed, phase - i * gap);
        }
    }

    // ---------------------------------------------------------------- the air

    /// A closed ring round the House at radius `r` and height `y`; `reverse` runs it the other way round
    static Vector3[] Ring(float r, float y, bool reverse)
    {
        const int N = 48;
        var pts = new Vector3[N + 1];
        for (int i = 0; i <= N; i++) pts[i] = Polar(r, (reverse ? -i : i) * 360f / N, y);
        return pts;
    }

    /// A lane of zeppelins (CityKit's: lit gondola, the name on the flanks, a searchlight under each): `count` of
    /// them spaced round a closed ring, charcoal and cream hulls by turns, `creamFirst` saying which leads. A
    /// negative speed runs the lane the other way round
    static void AirshipLane(Transform living, CityLife life, float r, float y, int count, float speed, bool creamFirst)
    {
        var lane = Ring(r, y, speed < 0f);
        for (int i = 0; i < count; i++)
            CityKit.Airship(living, life, lane, Mathf.Abs(speed) * Random.Range(0.92f, 1.08f), i / (float)count, (i % 2 == 0) == creamFirst);
    }

    /// A lane of mail planes round a closed ring, fast
    static void MailLane(Transform living, CityLife life, float r, float y, int count, float speed)
    {
        var lane = Ring(r, y, speed < 0f);
        for (int i = 0; i < count; i++)
            CityKit.Aircraft(living, life, lane, Mathf.Abs(speed) * Random.Range(0.95f, 1.05f), i / (float)count + Random.Range(-0.03f, 0.03f));
    }

    // ---------------------------------------------------------------- the signs

    /// A LIT SIGN in the company's manner, 1939: a charcoal plate in a brass frame on iron posts, the words in CREAM
    /// BLOCK LETTERS (five-by-seven cells, every run of cells one box, the whole sign welded into one mesh) facing
    /// `outward`. `at` is the posts' feet, `width` the plate's, `legH` the posts' height under it: three metres on a
    /// setback ledge; a sign bridge over a street stands on tall lattice pylons with stone footings. A long line is
    /// broken in two. THE TELL: one letter is held out of the weld so CityLife can kill it for a moment now and then,
    /// or, on every other sign, turn it RED for that moment (red is danger; the company does not mean it)
    static void RoofSign(Transform living, Vector3 at, Vector3 outward, float width, string line, int seed, float legH,
        List<Renderer> flickers, List<float> fPeriod, List<float> fPhase, List<float> fDuty)
    {
        if (Cut(at, width * 0.5f)) return;
        Vector3 right = Vector3.Cross(outward, Vector3.up);   // the viewer's right, standing in front of it
        Quaternion turn = Quaternion.LookRotation(outward, Vector3.up);
        string[] lines = SplitLine(line);
        int cellsWide = 1;
        foreach (var l in lines) cellsWide = Mathf.Max(cellsWide, Cells(l));
        float c = (width - 1.6f) / cellsWide;                                   // a cell, in metres
        float plateH = lines.Length * 7f * c + (lines.Length + 1) * 1.6f * c;
        Vector3 plateC = at + Vector3.up * (legH + plateH * 0.5f);
        var sb = new MeshBaker();
        sb.Add(signBack, plateC, new Vector3(width, plateH, 0.3f), turn);
        // The brass frame, proud of the plate
        Vector3 proud = outward * 0.1f;
        sb.Add(brass, plateC + proud + Vector3.up * (plateH * 0.5f), new Vector3(width + 0.4f, 0.4f, 0.5f), turn);
        sb.Add(brass, plateC + proud - Vector3.up * (plateH * 0.5f), new Vector3(width + 0.4f, 0.4f, 0.5f), turn);
        sb.Add(brass, plateC + proud + right * (width * 0.5f), new Vector3(0.4f, plateH + 0.4f, 0.5f), turn);
        sb.Add(brass, plateC + proud - right * (width * 0.5f), new Vector3(0.4f, plateH + 0.4f, 0.5f), turn);
        // The posts, iron, a brace between them. Tall ones are lattice pylons: two crossed plates a post, which from any
        // distance is a lattice, on a stone footing
        float postH = legH + plateH;
        bool pylons = legH > 8f;
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 foot = at + right * (s * (width * 0.5f - 1f));
            if (pylons)
            {
                sb.Add(iron, foot + Vector3.up * (postH * 0.5f), new Vector3(1.2f, postH, 0.3f), turn);
                sb.Add(iron, foot + Vector3.up * (postH * 0.5f), new Vector3(0.3f, postH, 1.2f), turn);
                sb.Add(stoneChar, foot + Vector3.up * 0.6f, new Vector3(2.2f, 1.2f, 2.2f), turn);
            }
            else sb.Add(iron, foot + Vector3.up * (postH * 0.5f), new Vector3(0.5f, postH, 0.5f), turn);
        }
        sb.Add(iron, at + Vector3.up * (legH * 0.5f), new Vector3(width - 2f, 0.3f, 0.3f), turn);
        if (pylons) sb.Add(iron, at + Vector3.up * (legH * 0.82f), new Vector3(width - 2f, 0.3f, 0.3f), turn);
        // The letters, in front of the plate, line under line from the top; the tell held out of the weld. The company's
        // own name is never broken (like its mark: it never glitches); a slogan's tell goes RED for its beat, never
        // dark, so no sign ever reads as a missing letter (user 2026-10-20: "this sign is missing a letter")
        var held = new MeshBaker();
        int tell = line == LevelKit.Company ? -1 : TellIndex(line, seed), index = 0;
        var heldRed = tell >= 0 ? new MeshBaker() : null;
        float y = plateC.y + plateH * 0.5f;
        foreach (var l in lines)
        {
            y -= 1.6f * c;
            Vector3 origin = new Vector3(plateC.x, y, plateC.z) - right * (Cells(l) * c * 0.5f) + outward * 0.33f;
            index = BlockLetters(sb, held, heldRed, l, origin, right, c, turn, tell, index);
            y -= 7f * c;
        }
        sb.Flush(living, "Sign");
        float period = Random.Range(9f, 20f), phase = Random.value, dark = Random.Range(0.035f, 0.07f);
        foreach (var r in held.Flush(living, "SignLetter"))
        {
            // Lit for all of the period but `dark` of it (CityLife: lit while the clock is past the duty)
            flickers.Add(r); fPeriod.Add(period); fPhase.Add(phase); fDuty.Add(dark);
        }
        if (heldRed != null)
            foreach (var r in heldRed.Flush(living, "SignLetterRed"))
            {
                r.enabled = false;   // out until its beat: outside Play (the Level Editor) the word reads cream and whole
                // Lit exactly when the cream letter is not: the same clock with its window turned inside out
                flickers.Add(r); fPeriod.Add(period); fPhase.Add(phase + 1f - dark); fDuty.Add(1f - dark);
            }
    }

    /// The company's shorter lines, the ones a sign can carry in letters a storey tall
    static readonly int[] ShortLines = { 7, 3, 1, 4, 0 };

    static string ShortTagline(int k)
    {
        var all = LevelKit.Taglines;
        if (all == null || all.Length == 0) return LevelKit.Company;
        return all[ShortLines[k % ShortLines.Length] % all.Length];
    }

    // ---------------------------------------------------------------- the searchlights

    /// A searchlight that tracks the SKY: a lamp on a finial and a beam up into the cloud, turning. The beam is two
    /// crossed sheets with soft edges that fade along their length, so it reads as a shaft of light from any side
    public static Transform SkyTracker(Transform living, CityLife life, Vector3 at, float length, float pitch, float degPerSec, float phase)
    {
        var pivot = new GameObject("SkyTracker");
        pivot.transform.SetParent(living);
        pivot.transform.position = at;
        var shaft = Beam(pivot.transform, length, length * 0.05f, new Color(0.85f, 0.92f, 1f, 0.26f));   // wide and soft: a shaft in smog
        shaft.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        LevelKit.Prim(pivot.transform, at, Vector3.one * 3f, glowWhite, "Lamp", PrimitiveType.Sphere).transform.localPosition = Vector3.zero;
        if (life != null) life.AddSpinner(pivot.transform, degPerSec, Vector3.up, phase);
        return pivot.transform;
    }

    /// A beam of light down +z from `parent`'s origin: two crossed sheets, `width` at the far end
    public static Transform Beam(Transform parent, float length, float width, Color tint)
        => Beam(parent, length, width, Sheet(CityTextures.Beam(), tint, (int)RenderQueue.Transparent));

    /// The same on a material of the caller's (shared between beams: one material, however many of them)
    public static Transform Beam(Transform parent, float length, float width, Material mat)
    {
        if (beamMesh == null)
        {
            // Two quads crossed on the z axis, a quarter as wide at the lamp as at the far end; u across, v along
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int q = 0; q < 2; q++)
            {
                Vector3 side = q == 0 ? Vector3.right : Vector3.up;
                int i = verts.Count;
                verts.Add(-side * 0.125f); verts.Add(-side * 0.5f + Vector3.forward); verts.Add(side * 0.5f + Vector3.forward); verts.Add(side * 0.125f);
                uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(0f, 1f)); uvs.Add(new Vector2(1f, 1f)); uvs.Add(new Vector2(1f, 0f));
                tris.Add(i); tris.Add(i + 1); tris.Add(i + 2); tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
            }
            beamMesh = new Mesh { name = "BeamMesh" };
            beamMesh.SetVertices(verts);
            beamMesh.SetUVs(0, uvs);
            beamMesh.SetTriangles(tris, 0);
            beamMesh.RecalculateBounds();
        }
        var go = new GameObject("Beam");
        go.transform.SetParent(parent, false);
        go.transform.localScale = new Vector3(width, width, length);
        go.AddComponent<MeshFilter>().sharedMesh = beamMesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go.transform;
    }
}
