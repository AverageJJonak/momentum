using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE FABRIC OF GOTHAM (2026-10-07, after the first Deco renders read as a
/// recoloured cyberpunk city; the Court redrawn to the user's reference
/// paintings 2026-10-16): the sea of low masonry every ring stands in,
/// the Court's eighteen shafts no two of which are alike, the landmarks
/// placed by hand, and the street. All of it period, 1939: BROWNSTONE ROWS of
/// three to five joined houses with stoops, CORNICE BLOCKS (lofts and
/// warehouses with arched ground floors and a painted wall sign), HOTELS with
/// a porte-cochère, flags and a lit rooftop name in cream bulbs on a brass
/// frame, DEPARTMENT STORES with a corner clock and awnings, THEATRES with a
/// lit marquee and a blade sign up the face, GENTLEMEN'S CLUBS behind a
/// porch of columns; a domed CITY HALL, a railway TERMINUS with a great arched
/// window, GASOMETERS in iron frames; lamp standards, parked limousines,
/// newsstands, statues, fountains, flagpoles. Nothing here glows but lamps,
/// windows, signs and marquees. Partial of MenuCity, compiled by the menu
/// harness too: plain UnityEngine, no LINQ, no Mathf.Abs(int).
/// </summary>
public static partial class MenuCity
{
    static readonly string[] HotelNames = { "HOTEL REGENT", "THE ASTOR", "GRAND HOTEL", "HOTEL CARLYLE", "THE EMPIRE", "HOTEL SAVOY" };
    static readonly string[] TheatreNames = { "RIALTO", "ROXY", "ORPHEUM", "APOLLO", "MAJESTIC", "PALACE" };
    static readonly string[] StoreNames = { "DINER", "FINCH'S", "ARCADE", "HALE BROS" };

    static float H01(int seed, int k) => CityTextures.Hash01(seed, k, 1939);

    /// THE SIGN BUDGET: neon and bulb signs are five in the whole city (the House's mark is the living half's): one
    /// hotel's rooftop name, one theatre's blade sign and marquee, one store's name (the diner), one red neon word,
    /// and the clocks. The first of each kind the plan builds gets it; the rest go dark. Reset by Init
    static int hotelSigns, theatreSigns, storeSigns, neonWords;

    /// Where the Court's crowns throw their SEARCHLIGHT FANS from (MenuCityAir.CrownFans): the finial, and the way
    /// the fan spreads (the tangent of the ring, so the orbit sees it fan left and right)
    struct FanSite { public Vector3 at, across; public float height; }
    static readonly List<FanSite> FanSites = new List<FanSite>();

    static void GothamInit()
    {
        hotelSigns = theatreSigns = storeSigns = neonWords = 0;
        FanSites.Clear();
    }

    // ------------------------------------------------------------ the low fabric

    /// A BROWNSTONE ROW: three to five narrow houses joined shoulder to shoulder, each its own height and stone, a
    /// stone cornice on each, a stoop and a lit door to the street (outward), a chimney or a tank on the roof.
    /// `w` is the row's length along the street, `d` its depth. Returns the tallest roof
    static float BrownstoneRow(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, float az, int seed, bool lit = true)
    {
        if (Cut(foot, Mathf.Max(w, d) * 0.5f + 2f)) return h;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        int houses = Mathf.Clamp(Mathf.RoundToInt(w / 12f), 3, 5);
        float hw = w / houses, top = 0f;
        d = Mathf.Min(d, 18f);
        for (int k = 0; k < houses; k++)
        {
            float x = -w * 0.5f + hw * (k + 0.5f);
            float hh = h * (0.8f + 0.4f * H01(seed, k));
            float pick = H01(seed, k + 10);
            Skin skin = pick < 0.65f ? brownstone : pick < 0.85f ? brick : cream;
            Material coping = skin == cream ? stoneLime : brownFlat;
            Vector3 hf = foot + turn * new Vector3(x, 0f, 0f);
            Tier(sheets, hf, hw, d, hh, skin, lit, az, BlindSides);
            Box(boxes, coping, hf + Vector3.up * (hh + 0.3f), new Vector3(hw + 0.5f, 0.6f, d + 0.6f), turn);
            if (k > 0) Box(boxes, coping, hf + turn * new Vector3(-hw * 0.5f, hh * 0.5f, d * 0.5f + 0.12f), new Vector3(0.5f, hh, 0.3f), turn);   // the party rib
            Box(boxes, stoneLime, hf + turn * new Vector3(hw * 0.2f, 0.7f, d * 0.5f + 1.1f), new Vector3(2.2f, 1.4f, 2.2f), turn);           // the stoop
            if (lit) Box(boxes, glowWarm, hf + turn * new Vector3(hw * 0.2f, 2.6f, d * 0.5f + 0.15f), new Vector3(1.2f, 2.4f, 0.2f), turn);  // the door, lit
            if (k % 2 == 0) Chimney(boxes, hf + Vector3.up * (hh + 0.5f) + turn * new Vector3(-hw * 0.3f, 0f, -d * 0.3f), 2.6f);
            else RoofFurniture(boxes, hf + Vector3.up * hh, hw, d, az, seed + k * 7, 0);
            if (hh > top) top = hh;
        }
        return top;
    }

    /// A CORNICE BLOCK: a loft or a warehouse, stone or brick, an arched ground floor under a shaft of punched
    /// windows, an attic under a deep stepped cornice, quoins on the stone ones, a water tower and a flagpole on the
    /// roof, a painted sign on the blind flank
    static float CorniceBlock(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, float az, int seed, bool lit = true)
    {
        if (Cut(foot, Mathf.Max(w, d) * 0.5f + 2f)) return h;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        float pick = H01(seed, 1);
        Skin skin = pick < 0.4f ? brick : pick < 0.7f ? limestone : pick < 0.85f ? cream : charcoal;
        Material stone = skin == brick ? brickFlat : skin == charcoal ? stoneChar : stoneLime;
        Tier(sheets, foot, w, d, h, skin, lit, az, TowerBlind(w, d, seed), true, true);
        Cornice(boxes, foot + Vector3.up * h, w, d, az, stone, null, 3);
        if (skin == limestone || skin == cream) Quoins(boxes, foot, w, d, h, az, stoneCream);
        RoofFurniture(boxes, foot + Vector3.up * h, w, d, az, seed, 1);
        WaterTower(boxes, foot + Vector3.up * (h + 1.3f) + turn * new Vector3(w * 0.3f, 0f, d * 0.2f), 3f, 5f, iron);
        if (H01(seed, 2) > 0.5f) Flagpole(boxes, foot + Vector3.up * (h + 1.3f) + turn * new Vector3(-w * 0.35f, 0f, d * 0.35f), 8f);
        return h;
    }

    /// A HOTEL: cream or limestone, a porte-cochère on posts over the entrance with a lamp lip, three flags on the
    /// parapet, the name lit in cream bulbs on a brass frame on the roof, a water tower behind it
    static float Hotel(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, float az, int seed, bool lit = true)
    {
        if (Cut(foot, Mathf.Max(w, d) * 0.5f + 6f)) return h;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        Vector3 fwd = turn * Vector3.forward, right = turn * Vector3.right;
        Skin skin = H01(seed, 1) < 0.5f ? cream : limestone;
        Tier(sheets, foot, w, d, h, skin, lit, az, H01(seed, 3) < 0.5f ? BlindLeft : BlindRight, true, true);
        Cornice(boxes, foot + Vector3.up * h, w, d, az, stoneCream, lit ? glowWarm : null);
        // The porte-cochère: a bronze canopy on four posts out from the door, a lamp lip along its edge
        Vector3 door = foot + fwd * (d * 0.5f);
        float cw = Mathf.Min(w * 0.45f, 14f);
        Box(boxes, bronze, door + fwd * 3.2f + Vector3.up * 5.2f, new Vector3(cw, 0.35f, 6.4f), turn);
        for (int s = -1; s <= 1; s += 2)
        {
            Box(boxes, bronze, door + fwd * 5.8f + right * (s * (cw * 0.5f - 0.5f)) + Vector3.up * 2.5f, new Vector3(0.4f, 5f, 0.4f), turn);
            if (lit) Box(boxes, glowWhite, door + fwd * 6.3f + right * (s * (cw * 0.5f - 0.5f)) + Vector3.up * 4.9f, Vector3.one * 0.7f, null, PrimitiveType.Sphere);
        }
        if (lit) Box(boxes, glowWhite, door + fwd * 6.4f + Vector3.up * 5.1f, new Vector3(cw, 0.14f, 0.14f), turn);
        if (lit) Box(boxes, glowWhite, door + fwd * 0.15f + Vector3.up * 2.6f, new Vector3(4f, 4.4f, 0.2f), turn);   // the doors, lit
        for (int k = -1; k <= 1; k += 2) Flagpole(boxes, foot + Vector3.up * (h + 1.3f) + turn * new Vector3(k * (w * 0.5f - 1.2f), 0f, d * 0.5f - 1.2f), 7f);   // the flags at the parapet's corners, clear of the name
        if (lit && hotelSigns++ == 0) StillSign(boxes, foot + Vector3.up * (h + 1.3f) + fwd * (d * 0.5f - 4.5f), fwd, Mathf.Min(w * 0.76f, 30f), HotelNames[((seed % HotelNames.Length) + HotelNames.Length) % HotelNames.Length], 2.5f);
        WaterTower(boxes, foot + Vector3.up * (h + 1.3f) + turn * new Vector3(-w * 0.3f, 0f, -d * 0.3f), 3.2f, 5.5f, iron);
        RoofFurniture(boxes, foot + Vector3.up * h, w, d, az, seed, 1);
        return h;
    }

    /// A DEPARTMENT STORE: a wide cream block with shop windows lit round every street face, awnings over them,
    /// a clock on the corner, flags and a water tower on the flat roof, the store's name on it
    static float DepartmentStore(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, float az, int seed, bool lit = true)
    {
        if (Cut(foot, Mathf.Max(w, d) * 0.5f + 3f)) return h;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        Vector3 fwd = turn * Vector3.forward, right = turn * Vector3.right;
        Tier(sheets, foot, w, d, h, H01(seed, 1) < 0.6f ? cream : limestone, lit, az, BlindBack, true, true);
        Cornice(boxes, foot + Vector3.up * h, w, d, az, stoneCream, null, 3);
        // The awnings, oxblood, one a bay along the front, leaning out over the pavement
        int bays = Mathf.Max(2, Mathf.RoundToInt(w / Bay));
        for (int k = 0; k < bays; k++)
        {
            float x = -w * 0.5f + w * (k + 0.5f) / bays;
            Box(boxes, oxblood, foot + turn * new Vector3(x, 4.2f, d * 0.5f + 0.9f), new Vector3(Bay - 0.8f, 0.12f, 2f), turn * Quaternion.Euler(18f, 0f, 0f));
        }
        // The corner clock, on a bracket at the second floor, facing the street
        Vector3 corner = foot + turn * new Vector3(w * 0.5f - 2f, 9f, d * 0.5f + 0.6f);
        Box(boxes, bronze, corner - fwd * 0.4f, new Vector3(0.5f, 2.2f, 0.9f), turn);
        ClockFace(boxes, corner + fwd * 0.3f, fwd, 2.6f);
        RoofFurniture(boxes, foot + Vector3.up * h, w, d, az, seed, 1);
        Flagpole(boxes, foot + Vector3.up * (h + 1.3f) + turn * new Vector3(0f, 0f, d * 0.5f - 1.5f), 9f);
        WaterTower(boxes, foot + Vector3.up * (h + 1.3f) + turn * new Vector3(-w * 0.3f, 0f, -d * 0.25f), 3.4f, 5.5f, iron);
        if (lit && w > 24f && storeSigns++ == 0) StillSign(boxes, foot + Vector3.up * (h + 1.3f) + fwd * (d * 0.5f - 4f) + right * (w * 0.2f), fwd, Mathf.Min(w * 0.5f, 18f), StoreNames[0], 1.6f);   // the diner's: the one store sign
        return h;
    }

    /// A THEATRE: a low front of house in cream under a lit MARQUEE on brass chains, the name up a blade sign on the
    /// face, a tall blind brick fly tower behind, a water tank on it
    static float Theatre(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, float az, int seed, bool lit = true)
    {
        if (Cut(foot, Mathf.Max(w, d) * 0.5f + 4f)) return h;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        Vector3 fwd = turn * Vector3.forward, right = turn * Vector3.right;
        float frontD = d * 0.45f, frontH = h * 0.6f, flyH = h;
        Vector3 front = foot + fwd * (d * 0.5f - frontD * 0.5f), fly = foot - fwd * (d * 0.5f - d * 0.55f * 0.5f);
        Tier(sheets, front, w, frontD, frontH, cream, lit, az, BlindSides);
        Cornice(boxes, front + Vector3.up * frontH, w, frontD, az, stoneCream, null, 2);
        Tier(sheets, fly, w * 0.7f, d * 0.55f, flyH, brick, false, az, BlindAll);
        Box(boxes, brickFlat, fly + Vector3.up * (flyH + 0.25f), new Vector3(w * 0.7f + 0.4f, 0.5f, d * 0.55f + 0.4f), turn);
        WaterTower(boxes, fly + Vector3.up * (flyH + 0.5f) + turn * new Vector3(w * 0.2f, 0f, 0f), 3f, 5f, iron);
        // The marquee: a canopy out over the pavement, its fascia lit cream, bulb lips along its edges, chains up to the face
        Vector3 face = front + fwd * (frontD * 0.5f);
        float mw = Mathf.Min(w * 0.7f, 16f);
        Box(boxes, stoneChar, face + fwd * 2.8f + Vector3.up * 5.2f, new Vector3(mw, 0.3f, 5.6f), turn);
        if (lit && theatreSigns++ == 0)   // the one theatre with its marquee and blade sign lit
        {
            Box(boxes, glowWhite, face + fwd * 5.6f + Vector3.up * 4.6f, new Vector3(mw, 1f, 0.2f), turn);            // the fascia
            Box(boxes, glowWhite, face + fwd * 5.6f + Vector3.up * 5.3f, new Vector3(mw + 0.2f, 0.14f, 0.14f), turn);  // the bulb lips
            Box(boxes, glowWhite, face + fwd * 5.6f + Vector3.up * 4f, new Vector3(mw + 0.2f, 0.14f, 0.14f), turn);
            Box(boxes, glowWarm, face + fwd * 0.15f + Vector3.up * 2.4f, new Vector3(mw * 0.6f, 4f, 0.2f), turn);     // the doors, lit
            BladeSign(boxes, face + Vector3.up * (frontH - 1f) + right * (w * 0.3f), fwd, 1.8f, TheatreNames[((seed % TheatreNames.Length) + TheatreNames.Length) % TheatreNames.Length]);
        }
        for (int s = -1; s <= 1; s += 2) Box(boxes, brass, face + fwd * 2.8f + right * (s * (mw * 0.5f - 0.4f)) + Vector3.up * 7.2f, new Vector3(0.12f, 4f, 0.12f), turn * Quaternion.Euler(-35f, 0f, 0f));
        return flyH;
    }

    /// A GENTLEMEN'S CLUB: limestone with quoins, few windows lit, a porch of four columns under an entablature at
    /// the door, a balustrade along the parapet, a flag
    static float Club(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, float az, int seed, bool lit = true)
    {
        if (Cut(foot, Mathf.Max(w, d) * 0.5f + 4f)) return h;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        Vector3 fwd = turn * Vector3.forward, right = turn * Vector3.right;
        Tier(sheets, foot, w, d, h, limestone, lit, az, BlindSides);
        Quoins(boxes, foot, w, d, h, az, stoneCream);
        Cornice(boxes, foot + Vector3.up * h, w, d, az, stoneLime, null, 3);
        Vector3 door = foot + fwd * (d * 0.5f);
        Colonnade(boxes, door + fwd * 2.4f, right, fwd, Mathf.Min(w * 0.5f, 12f), 6.5f, 4);
        if (lit) Box(boxes, glowWhite, door + fwd * 0.15f + Vector3.up * 2.2f, new Vector3(2.4f, 3.6f, 0.2f), turn);
        // The balustrade: a stone rail along the parapet's three street sides on short posts
        float pw = w - 0.6f;
        Box(boxes, stoneCream, foot + Vector3.up * (h + 1.3f + 0.9f) + fwd * (d * 0.5f - 0.3f), new Vector3(pw, 0.25f, 0.4f), turn);
        for (int k = 0; k < 6; k++) Box(boxes, stoneCream, foot + Vector3.up * (h + 1.3f + 0.4f) + fwd * (d * 0.5f - 0.3f) + right * (-pw * 0.5f + pw * (k + 0.5f) / 6f), new Vector3(0.3f, 0.8f, 0.3f), turn);
        Flagpole(boxes, foot + Vector3.up * (h + 1.3f) + turn * new Vector3(0f, 0f, d * 0.5f - 2.4f), 8f);
        RoofFurniture(boxes, foot + Vector3.up * h, w, d, az, seed, 0);
        return h;
    }

    /// A COLONNADE: `cols` limestone columns `colH` tall in a row across `width` centred on `at`, their capitals,
    /// an entablature over them, a stylobate of steps under. `right` runs along it, `outward` is the way it faces
    static void Colonnade(MeshBaker boxes, Vector3 at, Vector3 right, Vector3 outward, float width, float colH, int cols)
    {
        if (Cut(at, width * 0.5f + 2f)) return;
        Quaternion turn = Quaternion.LookRotation(outward, Vector3.up);
        float dia = Mathf.Clamp(colH * 0.11f, 0.7f, 2.4f);
        for (int k = 0; k < cols; k++)
        {
            float x = cols == 1 ? 0f : -width * 0.5f + width * k / (cols - 1);
            Box(boxes, stoneLime, at + right * x + Vector3.up * (colH * 0.5f + 0.6f), new Vector3(dia, colH, dia), null, PrimitiveType.Cylinder);
            Box(boxes, stoneLime, at + right * x + Vector3.up * (colH + 0.9f), new Vector3(dia * 1.4f, 0.6f, dia * 1.4f), turn);   // the capital
        }
        Box(boxes, stoneLime, at + Vector3.up * (colH + 1.2f + dia * 0.4f), new Vector3(width + dia * 1.6f, dia * 0.8f + 0.4f, dia * 2f), turn);   // the entablature
        Box(boxes, stoneLime, at + Vector3.up * 0.3f, new Vector3(width + dia * 2f, 0.6f, dia * 2.4f), turn);                                  // the stylobate
    }

    // ------------------------------------------------------------ the landmarks

    /// The CITY HALL: a limestone block with a colonnade on the face that looks at the House, corner pavilions,
    /// bronze figures along the parapet, a drum of arched windows under a bronze DOME with a lit lantern, flags
    static float CityHall(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, float az, int seed, bool lit = true)
    {
        if (Cut(foot, Mathf.Max(w, d) * 0.5f + 4f)) return h;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        Vector3 fwd = turn * Vector3.forward, right = turn * Vector3.right;
        float baseH = h * 0.4f, drumH = h * 0.22f, dome = Mathf.Min(w, d) * 0.5f;
        Tier(sheets, foot, w, d, baseH, arcade, lit, az, 0);
        Cornice(boxes, foot + Vector3.up * baseH, w, d, az, stoneLime, lit ? glowWarm : null);
        Colonnade(boxes, foot - fwd * (d * 0.5f + 2.6f), right, -fwd, w * 0.6f, baseH - 4f, Mathf.Clamp(Mathf.RoundToInt(w / 7f), 6, 14));
        if (lit) Box(boxes, glowWhite, foot - fwd * (d * 0.5f + 0.3f) + Vector3.up * 5f, new Vector3(7f, 10f, 0.3f), turn);   // the great door
        for (int c = 0; c < 4; c++)
        {
            Vector3 pav = foot + turn * new Vector3((c % 2 == 0 ? -1f : 1f) * (w * 0.5f - w * 0.1f), 0f, (c < 2 ? -1f : 1f) * (d * 0.5f - w * 0.1f));
            Tier(sheets, pav + Vector3.up * baseH, w * 0.2f, w * 0.2f, baseH * 0.35f, limestone, lit, az, 0);
            Cornice(boxes, pav + Vector3.up * (baseH * 1.35f), w * 0.2f, w * 0.2f, az, stoneLime, null, 2);
            Cone(boxes, roofSlate, pav + Vector3.up * (baseH * 1.35f + 1.2f), w * 0.09f, w * 0.07f, 3);
            Statue(boxes, pav + Vector3.up * (baseH + 1.3f) + turn * new Vector3((c % 2 == 0 ? 1f : -1f) * w * 0.14f, 0f, 0f), 6f, az);
        }
        RoundTier(boxes, sheets, foot + Vector3.up * baseH, dome * 1.1f, drumH, arcade, lit);
        Box(boxes, stoneLime, foot + Vector3.up * (baseH + drumH + 0.3f), new Vector3(dome * 1.16f, 0.6f, dome * 1.16f), null, PrimitiveType.Cylinder);
        float domeFoot = baseH + drumH + 0.6f;
        Box(boxes, bronze, foot + Vector3.up * domeFoot, Vector3.one * dome, null, PrimitiveType.Sphere);
        float lantern = domeFoot + dome * 0.5f;
        for (int k = 0; k < 8; k++) Box(boxes, stoneLime, foot + Radial(2.2f, k * 45f) + Vector3.up * (lantern + 1.6f), new Vector3(0.5f, 3.2f, 0.5f));
        if (lit) Box(boxes, glowWhite, foot + Vector3.up * (lantern + 1.6f), new Vector3(3f, 3f, 3f), null, PrimitiveType.Cylinder);
        Cone(boxes, bronze, foot + Vector3.up * (lantern + 3.4f), 2.6f, 3.6f, 3);
        Box(boxes, glowGold, foot + Vector3.up * (lantern + 7.6f), Vector3.one * 1.4f, null, PrimitiveType.Sphere);
        Flagpole(boxes, foot + Vector3.up * (baseH + 1.3f) + fwd * (d * 0.5f - 3f), 12f);
        if (lantern + 8f > 230f) Beacons.Add(foot + Vector3.up * (lantern + 8.5f));
        WashUp(sheets, foot, -fwd, d * 0.5f, w * 0.9f, baseH * 0.7f, washCream);
        return baseH;
    }

    /// The railway TERMINUS: a long hall under a pitched train-shed roof, a stone front with a GREAT ARCHED WINDOW
    /// lit amber and a clock in the pediment over it, the doors lit along the foot under a bronze canopy, a cupola
    /// tower at each front corner. The front looks at the House
    static float Terminus(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, float az, int seed, bool lit = true)
    {
        if (Cut(foot, Mathf.Max(w, d) * 0.5f + 4f)) return h;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        Vector3 fwd = turn * Vector3.forward, right = turn * Vector3.right;
        float hallH = h * 0.6f, tw = w * 0.14f, towerH = h;
        Tier(sheets, foot, w, d, hallH, arcade, lit, az, BlindBack);
        Cornice(boxes, foot + Vector3.up * hallH, w, d, az, stoneLime, null, 2);
        Pitched(boxes, foot + Vector3.up * (hallH + 0.9f), w, d, az, d * 0.14f);
        Vector3 face = foot - fwd * (d * 0.5f);
        // The pediment, the arch in it, the clock over the arch
        float pedH = h * 0.28f;
        Box(boxes, stoneLime, face + fwd * 1.5f + Vector3.up * (hallH + pedH * 0.5f), new Vector3(w * 0.64f, pedH, 3f), turn);
        Cornice(boxes, face + fwd * 1.5f + Vector3.up * (hallH + pedH), w * 0.64f, 3f, az, stoneLime, lit ? glowWarm : null, 2);
        Quaternion lie = turn * Quaternion.Euler(90f, 0f, 0f);
        float arch = w * 0.34f;
        Vector3 ac = face - fwd * 0.2f + Vector3.up * (hallH * 0.55f);
        Box(boxes, stoneChar, ac, new Vector3(arch + 2f, 0.5f, arch + 2f), lie, PrimitiveType.Cylinder);                                 // the bezel
        if (lit) Box(boxes, glowWarm, ac - fwd * 0.35f, new Vector3(arch, 0.2f, arch), lie, PrimitiveType.Cylinder);                      // the window, amber
        Box(boxes, stoneChar, ac - fwd * 0.5f, new Vector3(arch, 0.5f, 0.2f), turn);                                                       // the tracery
        Box(boxes, stoneChar, ac - fwd * 0.5f, new Vector3(0.5f, arch, 0.2f), turn);
        Box(boxes, stoneLime, ac - fwd * 0.4f - Vector3.up * (arch * 0.5f + 0.8f), new Vector3(arch + 2f, 1.6f, 1.2f), turn);              // the sill
        Box(boxes, stoneLime, face - fwd * 0.2f + Vector3.up * (hallH * 0.12f), new Vector3(w * 0.64f, hallH * 0.24f, 0.4f), turn);        // the wall under the window
        ClockFace(boxes, face + fwd * 0.1f + Vector3.up * (hallH + pedH * 0.55f), -fwd, Mathf.Min(pedH * 0.7f, 8f));
        for (int k = -1; k <= 1; k++) if (lit) Box(boxes, glowWhite, face - fwd * 0.3f + right * (k * w * 0.14f) + Vector3.up * 3f, new Vector3(4f, 6f, 0.3f), turn);   // the doors
        Box(boxes, bronze, face - fwd * 3f + Vector3.up * 7.5f, new Vector3(w * 0.6f, 0.3f, 6f), turn);                                       // the canopy
        for (int k = 0; k < 5; k++) Box(boxes, bronze, face - fwd * 5.8f + right * (-w * 0.28f + w * 0.56f * k / 4f) + Vector3.up * 3.7f, new Vector3(0.35f, 7.4f, 0.35f), turn);
        if (lit) Box(boxes, glowWhite, face - fwd * 6f + Vector3.up * 7.3f, new Vector3(w * 0.6f, 0.14f, 0.14f), turn);
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 tf = face + fwd * (tw * 0.5f) + right * (s * (w * 0.5f - tw * 0.5f));
            Tier(sheets, tf, tw, tw, towerH, pier, lit, az, 0);
            Cornice(boxes, tf + Vector3.up * towerH, tw, tw, az, stoneLime, lit ? glowWarm : null);
            Cone(boxes, roofSlate, tf + Vector3.up * (towerH + 1.3f), tw * 0.5f, tw * 0.6f, 4);
            Box(boxes, glowGold, tf + Vector3.up * (towerH + 1.3f + tw * 0.6f + 0.8f), Vector3.one * 1.2f, null, PrimitiveType.Sphere);
        }
        Flagpole(boxes, face + fwd * 1.5f + Vector3.up * (hallH + pedH + 1.3f), 8f);
        WashUp(sheets, foot, -fwd, d * 0.5f, w * 0.9f, hallH * 0.7f, washCream);
        return hallH;
    }

    /// A GASOMETER: a drum of iron inside a frame of iron columns tied by three ring girders, a red lamp on the frame
    static float Gasometer(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float dia, float h, int seed, bool lit = true)
    {
        if (Cut(foot, dia * 0.6f)) return h;
        const int Posts = 12;
        Box(boxes, stoneLime, foot + Vector3.up * 0.6f, new Vector3(dia * 1.1f, 1.2f, dia * 1.1f), null, PrimitiveType.Cylinder);
        Box(boxes, tankMat, foot + Vector3.up * (h * 0.3f + 1.2f), new Vector3(dia * 0.9f, h * 0.6f, dia * 0.9f), null, PrimitiveType.Cylinder);
        Box(boxes, roofSlate, foot + Vector3.up * (h * 0.6f + 1.6f), new Vector3(dia * 0.92f, 0.8f, dia * 0.92f), null, PrimitiveType.Cylinder);
        for (int k = 0; k < Posts; k++)
        {
            float a = k * 360f / Posts;
            Box(boxes, iron, foot + Radial(dia * 0.5f, a) + Vector3.up * (h * 0.5f + 1.2f), new Vector3(0.8f, h, 0.8f), Quaternion.Euler(0f, a, 0f));
            for (int g = 1; g <= 3; g++) Bar(boxes, iron, foot + Radial(dia * 0.5f, a) + Vector3.up * (h * g / 3f + 1f), foot + Radial(dia * 0.5f, a + 360f / Posts) + Vector3.up * (h * g / 3f + 1f), 0.5f);
        }
        if (lit) Box(boxes, glowRed, foot + Radial(dia * 0.5f, 0f) + Vector3.up * (h + 2f), Vector3.one * 0.8f);
        return h * 0.6f;
    }

    // ------------------------------------------------------------ the Court

    /// A PYRAMID ROOF over a `w` by `d` top, `rise` high: four slate slabs leaning together, a lantern on the point
    static void Pyramid(MeshBaker boxes, Vector3 top, float w, float d, float az, float rise)
    {
        if (Cut(top, Mathf.Max(w, d) * 0.5f)) return;
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        float hd = d * 0.5f, hw = w * 0.5f;
        float slantD = Mathf.Sqrt(hd * hd + rise * rise), pitchD = Mathf.Atan2(rise, hd) * Mathf.Rad2Deg;
        float slantW = Mathf.Sqrt(hw * hw + rise * rise), pitchW = Mathf.Atan2(rise, hw) * Mathf.Rad2Deg;
        for (int s = -1; s <= 1; s += 2)
        {
            Box(boxes, roofSlate, top + turn * new Vector3(0f, rise * 0.5f, s * hd * 0.5f), new Vector3(w * 1.02f, 0.4f, slantD + 0.3f), turn * Quaternion.Euler(-s * pitchD, 0f, 0f));
            Box(boxes, roofSlate, top + turn * new Vector3(s * hw * 0.5f, rise * 0.5f, 0f), new Vector3(slantW + 0.3f, 0.4f, d * 1.02f), turn * Quaternion.Euler(0f, 0f, s * pitchW));
        }
    }

    /// The TOP MASS of a tower: the tall thin last step. On the tallest towers (`taper`) its top fifteen per cent
    /// narrows by a tenth, so the shaft reads drawn from below
    static void TopMass(SheetBaker sheets, Vector3 foot, float w, float d, float th, Skin skin, float az, int blind, bool taper)
    {
        if (!taper || th < 20f) { Tier(sheets, foot, w, d, th, skin, true, az, blind, false, true); return; }
        float tap = th * 0.3f;
        Tier(sheets, foot, w, d, th - tap, skin, true, az, blind, false, false);
        Tier(sheets, foot + Vector3.up * (th - tap), w, d, tap, skin, true, az, blind, false, false, 0.1f);
    }

    /// The Court: eighteen shafts on the plaza's ring, no two alike, seventy to two hundred and thirty metres, the
    /// pair flanking the south approach the tallest. Every one LEANS outward two degrees (the forced perspective),
    /// wears FLUTED PIERS the height of its first mass, and the tall ones stand behind finned BUTTRESSES and throw
    /// BRIDGES to their neighbours at height. Every one is a hero: its street face is recorded for the living half
    /// (a sign on its first ledge, or a floor that fills and empties); the tallest crowns are fan sites
    static void Court(MeshBaker boxes, SheetBaker sheets)
    {
        const int N = 18;
        float[] hs = { 130f, 90f, 150f, 70f, 120f, 100f, 170f, 80f, 230f, 230f, 95f, 160f, 140f, 75f, 110f, 185f, 85f, 150f };
        int[] types = { 10, 0, 4, 7, 2, 9, 5, 1, 10, 11, 3, 6, 8, 0, 2, 5, 9, 4 };
        var feet = new Vector3[N];
        var widths = new float[N];
        var built = new bool[N];
        for (int i = 0; i < N; i++)
        {
            float az = (i + 0.5f) * 360f / N;
            Vector3 foot = Polar(262f, az);
            float w = 30f + 10f * H01(i, 1), d = 24f + 8f * H01(i, 2), h = hs[i];
            feet[i] = foot; widths[i] = w;
            int t = types[i];
            Skin skin = i % 4 == 0 ? pier : i % 4 == 1 ? pierDark : i % 4 == 2 ? limestone : charcoal;
            Material stone = skin == pierDark || skin == charcoal ? stoneChar : stoneLime;
            if (Cut(foot, Mathf.Max(w, d) * 0.5f + 4f)) continue;
            built[i] = true;
            Quaternion turn = Quaternion.Euler(0f, az, 0f);
            LeanOn(Radial(1f, az), 2.2f, foot.y);
            float y1, top;
            switch (t)
            {
                case 0: y1 = Ziggurat(boxes, sheets, foot, w, d, h, skin, stone, az, i, out top); break;
                case 1: y1 = PyramidTower(boxes, sheets, foot, w, d, h, skin, stone, az, i, out top); break;
                case 2: y1 = BrassCrown(boxes, sheets, foot, w, d, h, skin, stone, az, i, out top); break;
                case 3: y1 = MastTower(boxes, sheets, foot, w, d, h, skin, stone, az, i, out top); break;
                case 4: y1 = DomeTower(boxes, sheets, foot, w, d, h, skin, stone, az, i, out top); break;
                case 5: y1 = GothicTower(boxes, sheets, foot, w, d, h, skin, stone, az, i, out top); break;
                case 6: y1 = TwinTowers(boxes, sheets, foot, w, d, h, skin, stone, az, i, out top); break;
                case 7: y1 = CivicBlock(boxes, sheets, foot, w + 6f, d + 4f, Mathf.Min(h, 90f), az, i, out top); break;
                case 9: y1 = StackTower(boxes, sheets, foot, w, d, h, skin, stone, az, i, out top); break;
                case 10: y1 = SlabTower(boxes, sheets, foot, w, d, h, skin, stone, az, i, out top); break;
                case 11: y1 = SunburstTower(boxes, sheets, foot, w, d, h, skin, stone, az, i, out top); break;
                default: Spire(boxes, sheets, foot, w, d, h, skin, az, glowGold, true); y1 = h * 0.38f; top = h * 1.16f; break;
            }
            // The fluted piers up the first mass, and the buttresses the tall ones stand behind
            if (t != 7) FlutedPiers(boxes, foot, w, d, y1, az, stone == stoneChar ? stoneGranite : stoneCream);
            if (h >= 150f)
            {
                float bw = w * 0.26f, bd = d * 0.55f, bh = Mathf.Min(h * 0.32f, 62f);
                for (int s = -1; s <= 1; s += 2)
                    Buttress(boxes, foot + turn * new Vector3(s * (w * 0.5f - bw * 0.5f), 0f, d * 0.5f + bd * 0.5f - 0.3f), bw, bd, bh, az, stone, 3);
            }
            if (h >= 180f) FanSites.Add(new FanSite { at = foot + Vector3.up * top + Radial(top * leanTan, az), across = Radial(1f, az + 90f), height = h });
            LeanOff();
            if (t != 8) Heroes.Add(new Site { foot = foot, outward = Radial(1f, az), w = w, d = d, y0 = 0f, y1 = y1, h = top });
        }
        // The bridges: thrown between neighbours at height, every third pair where both stand tall enough
        for (int i = 0; i < N; i++)
        {
            int j = (i + 1) % N;
            if (i % 3 != 1 || !built[i] || !built[j] || hs[i] < 90f || hs[j] < 90f) continue;
            float y = Mathf.Min(hs[i], hs[j]) * 0.4f;
            Vector3 dir = (feet[j] - feet[i]).normalized;
            SkyBridge(boxes, feet[i] + dir * (widths[i] * 0.42f) + Vector3.up * y, feet[j] - dir * (widths[j] * 0.42f) + Vector3.up * y, 5f, stoneLime);
        }
        // Tiny crowds on the terraces of the civic blocks and the twin towers' podium
        for (int i = 0; i < N; i++)
            if (built[i] && (types[i] == 7 || types[i] == 6))
                Crowd(boxes, feet[i] + Vector3.up * (types[i] == 7 ? Mathf.Min(hs[i], 90f) * 0.6f + 1.3f : hs[i] * 0.3f + 1.3f) + Radial(10f, (i + 0.5f) * 360f / N + (i % 2 == 0 ? 60f : -60f)), 6f, 14, i);
    }

    /// A ZIGGURAT: four steps, each a fifth narrower, the last tall and thin, a cornice on each, gargoyles on the
    /// first, a lantern and a flag on the flat top
    static float Ziggurat(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, Skin skin, Material stone, float az, int seed, out float top)
    {
        float[] part = { 0.38f, 0.22f, 0.14f, 0.26f };
        float y = 0f, cw = w, cd = d, y1 = h * part[0];
        for (int i = 0; i < part.Length; i++)
        {
            float th = h * part[i];
            if (i == part.Length - 1) TopMass(sheets, foot + Vector3.up * y, cw, cd, th, skin, az, 0, h >= 150f);
            else Tier(sheets, foot + Vector3.up * y, cw, cd, th, skin, true, az, 0, i == 0, false);
            y += th;
            Cornice(boxes, foot + Vector3.up * y, cw, cd, az, stone, i >= part.Length - 2 ? glowWarm : null, 2);
            if (i == 0) Gargoyles(boxes, foot + Vector3.up * y, cw, cd, az, stone);
            cw *= 0.78f; cd *= 0.78f;
        }
        float tw = cw / 0.78f, td = cd / 0.78f;
        Box(boxes, glowWhite, foot + Vector3.up * (y + 1.3f + 1.4f), new Vector3(3f, 2.8f, 3f), Quaternion.Euler(0f, az, 0f));   // the lantern
        Box(boxes, brass, foot + Vector3.up * (y + 1.3f + 3.1f), new Vector3(3.6f, 0.6f, 3.6f), Quaternion.Euler(0f, az, 0f));
        Flagpole(boxes, foot + Vector3.up * (y + 1.3f) + Quaternion.Euler(0f, az, 0f) * new Vector3(tw * 0.3f, 0f, td * 0.3f), 8f);
        RoofFurniture(boxes, foot + Vector3.up * y, tw, td, az, seed, 0);
        top = y + 5f;
        return y1;
    }

    /// A shaft under a stone PYRAMID roof with a lit lantern on the point
    static float PyramidTower(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, Skin skin, Material stone, float az, int seed, out float top)
    {
        float y1 = h * 0.46f, y2 = h * 0.78f;
        Tier(sheets, foot, w, d, y1, skin, true, az, 0, true, false);
        Cornice(boxes, foot + Vector3.up * y1, w, d, az, stone, null);
        Gargoyles(boxes, foot + Vector3.up * y1, w, d, az, stone);
        float w2 = w * 0.8f, d2 = d * 0.8f;
        Tier(sheets, foot + Vector3.up * y1, w2, d2, y2 - y1, skin, true, az, 0, false, true);
        Cornice(boxes, foot + Vector3.up * y2, w2, d2, az, stone, glowWarm);
        float rise = h - y2;
        Pyramid(boxes, foot + Vector3.up * (y2 + 1.3f), w2 + 1.2f, d2 + 1.2f, az, rise - 1.3f);
        Box(boxes, glowWhite, foot + Vector3.up * (h + 1.4f), new Vector3(2.4f, 2.8f, 2.4f), null, PrimitiveType.Cylinder);   // the lantern on the point
        Box(boxes, brass, foot + Vector3.up * (h + 3.4f), new Vector3(0.5f, 2f, 0.5f));
        Box(boxes, glowGold, foot + Vector3.up * (h + 4.6f), Vector3.one * 1.2f, null, PrimitiveType.Sphere);
        top = h + 5f;
        return y1;
    }

    /// A shaft under a CROWN OF STACKED BRASS TIERS, each with lit slits in it, a brass spike over them
    static float BrassCrown(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, Skin skin, Material stone, float az, int seed, out float top)
    {
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        float y1 = h * 0.46f, y2 = h * 0.7f, y3 = h * 0.84f;
        Tier(sheets, foot, w, d, y1, skin, true, az, 0, true, false);
        Cornice(boxes, foot + Vector3.up * y1, w, d, az, stone, null);
        Gargoyles(boxes, foot + Vector3.up * y1, w, d, az, stone);
        float w2 = w * 0.8f, d2 = d * 0.8f, w3 = w * 0.62f, d3 = d * 0.62f;
        Tier(sheets, foot + Vector3.up * y1, w2, d2, y2 - y1, skin, true, az, 0);
        Cornice(boxes, foot + Vector3.up * y2, w2, d2, az, stone, null);
        Tier(sheets, foot + Vector3.up * y2, w3, d3, y3 - y2, skin, true, az, 0, false, true);
        Cornice(boxes, foot + Vector3.up * y3, w3, d3, az, stone, glowGold);
        const int Tiers = 4;
        float cw = w3 * 0.9f, cd = d3 * 0.9f, y = y3 + 1.3f, th = (h - y3 - 1.3f) / Tiers;
        for (int i = 0; i < Tiers; i++)
        {
            Box(boxes, brass, foot + Vector3.up * (y + th * 0.5f), new Vector3(cw, th, cd), turn);
            for (int f = 0; f < 4; f++)   // the slits, cream, three a face
            {
                Vector3 n = turn * (f == 0 ? Vector3.back : f == 1 ? Vector3.forward : f == 2 ? Vector3.left : Vector3.right);
                Vector3 along = turn * (f < 2 ? Vector3.right : Vector3.forward);
                float width = f < 2 ? cw : cd, half = (f < 2 ? cd : cw) * 0.5f;
                for (int k = -1; k <= 1; k++)
                    Box(boxes, glowWhite, foot + Vector3.up * (y + th * 0.5f) + n * (half + 0.08f) + along * (k * width * 0.28f), new Vector3(f < 2 ? 0.7f : 0.12f, th * 0.55f, f < 2 ? 0.12f : 0.7f), turn);
            }
            y += th;
            cw *= 0.74f; cd *= 0.74f;
        }
        float spike = h * 0.08f;
        Box(boxes, brass, foot + Vector3.up * (h + spike * 0.5f), new Vector3(1f, spike, 1f));
        Box(boxes, glowGold, foot + Vector3.up * (h + spike), Vector3.one * 1.8f, null, PrimitiveType.Sphere);
        top = h + spike;
        return y1;
    }

    /// A shaft under a FLAT TOP: a water tower on stilts and a tall MAST with the red lamp
    static float MastTower(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, Skin skin, Material stone, float az, int seed, out float top)
    {
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        float y1 = h * 0.52f, y2 = h * 0.8f;
        Tier(sheets, foot, w, d, y1, skin, true, az, 0, true, false);
        Cornice(boxes, foot + Vector3.up * y1, w, d, az, stone, null);
        Gargoyles(boxes, foot + Vector3.up * y1, w, d, az, stone);
        float w2 = w * 0.8f, d2 = d * 0.8f, w3 = w * 0.62f, d3 = d * 0.62f;
        Tier(sheets, foot + Vector3.up * y1, w2, d2, y2 - y1, skin, true, az, 0, false, false);
        Cornice(boxes, foot + Vector3.up * y2, w2, d2, az, stone, glowWarm);
        Tier(sheets, foot + Vector3.up * y2, w3, d3, h - y2, skin, true, az, 0, false, true);
        Cornice(boxes, foot + Vector3.up * h, w3, d3, az, stone, null);
        WaterTower(boxes, foot + Vector3.up * (h + 1.3f) + turn * new Vector3(-w3 * 0.22f, 0f, d3 * 0.1f), 4.2f, 9f, iron);   // on tall stilts
        float mast = Mathf.Min(h * 0.22f, 34f);
        RadioMast(boxes, foot + Vector3.up * (h + 1.3f) + turn * new Vector3(w3 * 0.3f, 0f, -d3 * 0.2f), mast, true);
        RoofFurniture(boxes, foot + Vector3.up * h, w3, d3, az, seed, 0);
        top = h + 1.3f + mast;
        return y1;
    }

    /// A square shaft under a round drum of arched windows and a bronze DOME with a gilt finial: the drum with a lantern
    static float DomeTower(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, Skin skin, Material stone, float az, int seed, out float top)
    {
        float y1 = h * 0.45f, y2 = h * 0.7f, drumH = h * 0.14f, dome = Mathf.Min(w, d) * 0.66f;
        Tier(sheets, foot, w, d, y1, skin, true, az, 0, true, false);
        Cornice(boxes, foot + Vector3.up * y1, w, d, az, stone, null);
        Gargoyles(boxes, foot + Vector3.up * y1, w, d, az, stone);
        float w2 = w * 0.78f, d2 = d * 0.78f;
        Tier(sheets, foot + Vector3.up * y1, w2, d2, y2 - y1, skin, true, az, 0, false, true);
        Cornice(boxes, foot + Vector3.up * y2, w2, d2, az, stone, glowWarm);
        RoundTier(boxes, sheets, foot + Vector3.up * (y2 + 1.3f), dome * 1.05f, drumH, arcade, true);
        float domeFoot = y2 + 1.3f + drumH;
        Box(boxes, brass, foot + Vector3.up * (domeFoot + 0.2f), new Vector3(dome * 1.1f, 0.4f, dome * 1.1f), null, PrimitiveType.Cylinder);
        Box(boxes, bronze, foot + Vector3.up * (domeFoot + 0.3f), Vector3.one * dome, null, PrimitiveType.Sphere);
        float finial = domeFoot + 0.3f + dome * 0.5f;
        Box(boxes, glowWhite, foot + Vector3.up * (finial + 1.2f), new Vector3(2.4f, 2.4f, 2.4f), null, PrimitiveType.Cylinder);   // the lantern on the dome
        Box(boxes, brass, foot + Vector3.up * (finial + 3.2f), new Vector3(0.8f, 1.6f, 0.8f), null, PrimitiveType.Cylinder);
        Box(boxes, glowGold, foot + Vector3.up * (finial + 4.6f), Vector3.one * 1.8f, null, PrimitiveType.Sphere);
        for (int c = 0; c < 4; c++)   // urns on the drum's corners
            Box(boxes, stoneLime, foot + Vector3.up * (y2 + 1.3f + 1.5f) + Quaternion.Euler(0f, az, 0f) * new Vector3((c % 2 == 0 ? -1f : 1f) * (w2 * 0.5f - 1.5f), 0f, (c < 2 ? -1f : 1f) * (d2 * 0.5f - 1.5f)), new Vector3(1.6f, 3f, 1.6f), Quaternion.Euler(0f, az, 0f));
        top = finial + 5.5f;
        return y1;
    }

    /// A shaft in two masses, the top one tall and thin, under a GOTHIC CROWN: winged gargoyles on the corners,
    /// pinnacles behind them, a stepped finial between
    static float GothicTower(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, Skin skin, Material stone, float az, int seed, out float top)
    {
        float y1 = h * 0.52f, crown = h * 0.1f;
        Tier(sheets, foot, w, d, y1, skin, true, az, 0, true, false);
        Cornice(boxes, foot + Vector3.up * y1, w, d, az, stone, null);
        Gargoyles(boxes, foot + Vector3.up * y1, w, d, az, stone);
        float w2 = w * 0.78f, d2 = d * 0.78f;
        TopMass(sheets, foot + Vector3.up * y1, w2, d2, h - y1, skin, az, 0, h >= 150f);
        Cornice(boxes, foot + Vector3.up * h, w2 * (h >= 150f ? 0.9f : 1f), d2 * (h >= 150f ? 0.9f : 1f), az, stone, glowWarm, 3);
        GothicCrown(boxes, foot + Vector3.up * h, w2 * 0.9f, d2 * 0.9f, az, crown, stone);
        top = h + 1.3f + crown + 3f;
        return y1;
    }

    /// TWIN TOWERS on one podium: a wide base, two shafts rising from it a well apart, a small spire on each
    static float TwinTowers(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, Skin skin, Material stone, float az, int seed, out float top)
    {
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        float y1 = h * 0.3f;
        Tier(sheets, foot, w, d, y1, skin, true, az, 0, true, false);
        Cornice(boxes, foot + Vector3.up * y1, w, d, az, stone, null);
        Gargoyles(boxes, foot + Vector3.up * y1, w, d, az, stone);
        float tw = w * 0.36f, td = d * 0.8f;
        for (int s = -1; s <= 1; s += 2)
        {
            Vector3 tf = foot + turn * new Vector3(s * (w * 0.5f - tw * 0.5f), 0f, 0f);
            float th = h * (s < 0 ? 1f : 0.93f);
            Tier(sheets, tf + Vector3.up * y1, tw, td, th - y1, skin, true, az, 0, false, true);
            Cornice(boxes, tf + Vector3.up * th, tw, td, az, stone, glowWarm);
            CrownSpire(boxes, tf + Vector3.up * (th + 1.3f), Mathf.Min(tw, td) * 0.3f, h * 0.08f, stone, glowGold);
        }
        SkyBridge(boxes, foot + turn * new Vector3(-(w * 0.5f - tw), 0f, 0f) + Vector3.up * (h * 0.62f), foot + turn * new Vector3(w * 0.5f - tw, 0f, 0f) + Vector3.up * (h * 0.62f), 4f, stone);
        RoofFurniture(boxes, foot + Vector3.up * y1, w * 0.26f, td, az, seed, 0);
        top = h + 1.3f + h * 0.08f + 3f;
        return y1;
    }

    /// A CIVIC BLOCK, lower than the rest: limestone behind a colonnade on its outward face, a stepped attic,
    /// bronze figures on the parapet, a flag
    static float CivicBlock(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, float az, int seed, out float top)
    {
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        Vector3 fwd = turn * Vector3.forward, right = turn * Vector3.right;
        float y1 = h * 0.6f;
        Tier(sheets, foot, w, d, y1, arcade, true, az, 0);
        Cornice(boxes, foot + Vector3.up * y1, w, d, az, stoneLime, glowWarm);
        Colonnade(boxes, foot + fwd * (d * 0.5f + 2.6f), right, fwd, w * 0.7f, y1 - 4f, Mathf.Clamp(Mathf.RoundToInt(w / 5f), 5, 9));
        Box(boxes, glowWhite, foot + fwd * (d * 0.5f + 0.3f) + Vector3.up * 5f, new Vector3(6f, 10f, 0.3f), turn);   // the great door, lit
        float aw = w * 0.7f, ad = d * 0.7f;
        Tier(sheets, foot + Vector3.up * y1, aw, ad, h - y1, limestone, true, az, 0, false, true);
        Cornice(boxes, foot + Vector3.up * h, aw, ad, az, stoneLime, glowGold);
        for (int c = 0; c < 4; c++)
            Statue(boxes, foot + Vector3.up * (y1 + 1.3f) + turn * new Vector3((c % 2 == 0 ? -1f : 1f) * (w * 0.5f - 3f), 0f, (c < 2 ? -1f : 1f) * (d * 0.5f - 3f)), 7f, az);
        Flagpole(boxes, foot + Vector3.up * (h + 1.3f), 12f);
        RoofFurniture(boxes, foot + Vector3.up * h, aw, ad, az, seed, 1);
        top = h + 14f;
        return y1;
    }

    /// A STACK TOWER: two masses under a flat roof with a SMOKESTACK PAIR and its painted smoke, a water tower
    /// beside them. The works' end of the Court
    static float StackTower(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, Skin skin, Material stone, float az, int seed, out float top)
    {
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        float y1 = h * 0.58f;
        Tier(sheets, foot, w, d, y1, skin, true, az, 0, true, false);
        Cornice(boxes, foot + Vector3.up * y1, w, d, az, stone, null);
        Gargoyles(boxes, foot + Vector3.up * y1, w, d, az, stone);
        float w2 = w * 0.78f, d2 = d * 0.78f;
        Tier(sheets, foot + Vector3.up * y1, w2, d2, h - y1, skin, true, az, 0, false, true);
        Cornice(boxes, foot + Vector3.up * h, w2, d2, az, stone, glowWarm, 2);
        float stack = Mathf.Clamp(h * 0.22f, 14f, 30f);
        StackPair(boxes, foot + Vector3.up * (h + 1.3f) + turn * new Vector3(0f, 0f, -d2 * 0.2f), az, stack, w2 * 0.36f);
        WaterTower(boxes, foot + Vector3.up * (h + 1.3f) + turn * new Vector3(w2 * 0.28f, 0f, d2 * 0.25f), 3.4f, 7f, iron);
        RoofFurniture(boxes, foot + Vector3.up * h, w2, d2, az, seed, 0);
        top = h + 1.3f + stack;
        return y1;
    }

    /// A SLAB TOWER, the painted fluted hero: four masses stepping in, each a fifth narrower, the last tall and
    /// thin and tapered, under a CROWN OF STACKED SLABS and a mast; its crown throws a fan of searchlights
    static float SlabTower(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, Skin skin, Material stone, float az, int seed, out float top)
    {
        Quaternion turn = Quaternion.Euler(0f, az, 0f);
        float[] part = { 0.4f, 0.22f, 0.16f, 0.22f };
        float y = 0f, cw = w, cd = d, y1 = h * part[0];
        for (int i = 0; i < part.Length; i++)
        {
            float th = h * part[i];
            if (i == part.Length - 1) TopMass(sheets, foot + Vector3.up * y, cw, cd, th, skin, az, 0, h >= 150f);
            else Tier(sheets, foot + Vector3.up * y, cw, cd, th, skin, true, az, 0, i == 0, false);
            if (i >= 1) FlutedPiers(boxes, foot + Vector3.up * y, cw, cd, i == part.Length - 1 ? th * 0.7f : th, az, stone == stoneChar ? stoneGranite : stoneCream);
            y += th;
            Cornice(boxes, foot + Vector3.up * y, cw * (i == part.Length - 1 && h >= 150f ? 0.9f : 1f), cd * (i == part.Length - 1 && h >= 150f ? 0.9f : 1f), az, stone, i >= 2 ? glowWarm : null, 2);
            if (i == 0) Gargoyles(boxes, foot + Vector3.up * y, cw, cd, az, stone);
            cw *= 0.78f; cd *= 0.78f;
        }
        float tw = cw / 0.78f * 0.9f, td = cd / 0.78f * 0.9f, crown = h * 0.07f;
        SlabCrown(boxes, foot + Vector3.up * (y + 1.3f), tw, td, az, crown, brass, glowGold);
        float mast = Mathf.Min(h * 0.12f, 26f);
        RadioMast(boxes, foot + Vector3.up * (y + 1.3f + crown + 3.5f), mast, true);
        top = y + 1.3f + crown + 3.5f + mast;
        return y1;
    }

    /// A SUNBURST TOWER: three masses, the last tall and thin, a SUNBURST of brass fins for a crown
    static float SunburstTower(MeshBaker boxes, SheetBaker sheets, Vector3 foot, float w, float d, float h, Skin skin, Material stone, float az, int seed, out float top)
    {
        float[] part = { 0.46f, 0.26f, 0.28f };
        float y = 0f, cw = w, cd = d, y1 = h * part[0];
        for (int i = 0; i < part.Length; i++)
        {
            float th = h * part[i];
            if (i == part.Length - 1) TopMass(sheets, foot + Vector3.up * y, cw, cd, th, skin, az, 0, h >= 150f);
            else Tier(sheets, foot + Vector3.up * y, cw, cd, th, skin, true, az, 0, i == 0, false);
            if (i >= 1) FlutedPiers(boxes, foot + Vector3.up * y, cw, cd, i == part.Length - 1 ? th * 0.7f : th, az, stone == stoneChar ? stoneGranite : stoneCream);
            y += th;
            Cornice(boxes, foot + Vector3.up * y, cw * (i == part.Length - 1 && h >= 150f ? 0.9f : 1f), cd * (i == part.Length - 1 && h >= 150f ? 0.9f : 1f), az, stone, i >= 1 ? glowWarm : null, 2);
            if (i == 0) Gargoyles(boxes, foot + Vector3.up * y, cw, cd, az, stone);
            cw *= 0.76f; cd *= 0.76f;
        }
        float tw = cw / 0.76f * 0.9f, td = cd / 0.76f * 0.9f, crown = h * 0.09f;
        SunburstCrown(boxes, foot + Vector3.up * (y + 1.3f), tw, td, az, crown, brass);
        top = y + 1.3f + crown + 3.5f;
        return y1;
    }

    // ------------------------------------------------------------ the rings beyond the belt

    /// What stands on lot `i` of ring `ring` by decree, or -1 for whatever the fabric brings: the landmarks are placed
    /// by hand. 0 clock tower, 1 cathedral, 2 bank, 3 terminus, 4 station, 5 observatory, 6 powerhouse, 7 city hall,
    /// 8 radio tower, 9 gasometer, 10 a dead one. (The rings hold 26, 18, 18, 28 and 36 lots)
    static int Landmark(int ring, int i)
    {
        switch (ring)
        {
            case 1: return i == 0 ? 0 : i == 3 ? 1 : i == 5 ? 2 : i == 13 ? 3 : i == 22 ? 2 : -1;
            case 2: return i == 1 ? 4 : i == 5 ? 5 : i == 10 ? 4 : i == 14 ? 6 : -1;
            case 3: return i == 2 ? 7 : i == 7 ? 1 : i == 11 ? 2 : i == 16 ? 3 : -1;
            case 4: return i == 0 ? 0 : i == 6 ? 8 : i == 16 ? 9 : i == 23 ? 4 : i == 10 || i == 24 ? 10 : -1;
            default: return i == 2 ? 8 : i == 8 ? 9 : i == 20 ? 1 : i == 26 ? 8 : i == 32 ? 9 : i == 5 || i == 21 ? 10 : -1;
        }
    }

    /// A RING OF LOTS at radius `r`, `count` of them: the landmarks on the lots they were given, and on the rest the
    /// fabric by turns, three parts in five low masonry, one in four a setback shaft, the odd giant, never two giants
    /// or two of the same low kind side by side. Every lot is jittered a little off the ring and off its bearing
    /// (never toward an avenue), so no two stand in a line. `maxD` is how deep a lot may go without reaching the rows.
    /// The first ring's shafts and giants LEAN in toward the belt a degree and a half
    static void Ring(MeshBaker boxes, SheetBaker sheets, float r, int count, int ring, float maxD)
    {
        int prevClass = -1, prevType = -1;
        float spacing = 2f * Mathf.PI * r / count;
        for (int i = 0; i < count; i++)
        {
            int seed = ring * 1000 + i;
            float az0 = (i + 0.5f) * 360f / count;
            float az = OffAvenue(az0) > 7f ? az0 + (H01(seed, 1) - 0.5f) * 4f : az0;
            float rr = r + (H01(seed, 2) - 0.5f) * 20f;
            Vector3 foot = Polar(rr, az);
            float h1 = H01(seed, 3), h2 = H01(seed, 4), h3 = H01(seed, 5);
            float w = Mathf.Min(spacing * 0.6f, 26f + 48f * h2), d = Mathf.Min(maxD, 20f + 30f * h3);
            int mark = Landmark(ring, i);
            if (mark >= 0)
            {
                float big = ring == 3 ? 1f : 0.45f;
                switch (mark)
                {
                    case 0: ClockTower(boxes, sheets, foot, 24f, 130f + 30f * h1, az, seed); break;
                    case 1: Cathedral(boxes, sheets, foot, ring == 3 ? 50f : 34f, ring == 3 ? 90f : ring == 5 ? 70f : 62f, ring == 3 ? 220f : 150f + 30f * h1, ring == 5 ? az + 90f : az, seed); break;
                    case 2: Bank(boxes, sheets, foot, ring == 3 ? 110f : 44f, ring == 3 ? 80f : 40f, ring == 3 ? 150f : 90f + 20f * h1, az, seed); break;
                    case 3: Terminus(boxes, sheets, foot, ring == 3 ? 110f : 70f, ring == 3 ? 60f : 40f, ring == 3 ? 150f : 90f, az, seed); break;
                    case 4: Station(boxes, sheets, foot, 34f, 60f + 10f * h1, az, seed); break;
                    case 5: Rotunda(boxes, sheets, foot, 40f, 150f + 40f * h1, limestone, glowWarm); break;
                    case 6: Powerhouse(boxes, sheets, foot, 52f, 40f, 36f + 8f * h1, az, seed); break;
                    case 7: CityHall(boxes, sheets, foot, 120f, 90f, 150f, az, seed); break;
                    case 8: RadioTower(boxes, sheets, foot, 170f + 50f * h1, seed); break;
                    case 9: Gasometer(boxes, sheets, foot, 44f + 10f * h1, 50f + 12f * h2, seed); break;
                    default: Dead(sheets, foot, 40f + 4f * big, 36f, 110f + 80f * h1, az); break;
                }
                prevClass = 3; prevType = -1;
                continue;
            }
            int cls = h1 < 0.6f ? 0 : h1 < 0.86f ? 1 : 2;
            if (cls == 2 && prevClass == 2) cls = 1;
            if (cls == 1 && prevClass == 1) cls = 0;
            if (cls == 0)
            {
                int type = (int)(h2 * 8f) % 8;
                if (type == prevType) type = (type + 3) % 8;
                switch (type)
                {
                    case 0: BrownstoneRow(boxes, sheets, foot, Mathf.Max(w, 36f), 16f, 12f + 6f * h3, az, seed); break;
                    case 1: CorniceBlock(boxes, sheets, foot, w, d, 18f + 22f * h3, az, seed); break;
                    case 2: Hotel(boxes, sheets, foot, w, d, 34f + 14f * h3, az, seed); break;
                    case 3: DepartmentStore(boxes, sheets, foot, Mathf.Max(w, 40f), d, 22f + 12f * h3, az, seed); break;
                    case 4: Theatre(boxes, sheets, foot, w, Mathf.Max(d, 30f), 22f + 10f * h3, az, seed); break;
                    case 5: Club(boxes, sheets, foot, Mathf.Min(w, 36f), Mathf.Min(d, 30f), 14f + 10f * h3, az, seed); break;
                    case 6: Tenement(boxes, sheets, foot, w, d, 28f + 14f * h3, az, seed); break;
                    default: Powerhouse(boxes, sheets, foot, w, Mathf.Max(d, w * 0.8f), 26f + 8f * h3, az, seed); break;
                }
                prevType = type;
            }
            else
            {
                if (ring == 1) LeanOn(-Radial(1f, az), 1.5f, foot.y);
                if (cls == 1) Setback(boxes, sheets, foot, Mathf.Min(w, 48f), Mathf.Min(d, 40f), 70f + 80f * h3, az, seed);
                else
                {
                    float gh = ring == 1 ? 170f + 60f * h3 : ring == 3 ? 240f + 100f * h3 : 180f + 100f * h3;
                    float gw = Mathf.Min(w, 50f), gd = Mathf.Min(d, 42f);
                    if (h2 < 0.4f) Spire(boxes, sheets, foot, gw, gd, gh, h3 < 0.5f ? pier : charcoal, az, glowGold, false);
                    else Setback(boxes, sheets, foot, gw, gd, gh, az, seed + 7);
                }
                LeanOff();
                prevType = -1;
            }
            prevClass = cls;
        }
    }

    // ------------------------------------------------------------ the street

    /// What stands in the street: lamp standards down both kerbs of every avenue inside the terraces, limousines
    /// parked along them, a newsstand at every avenue's mouth on the plaza, a flagpole beside it, bronze figures
    /// flanking every avenue where it leaves the belt, two fountains in the plaza's paving
    static void StreetLife(MeshBaker boxes)
    {
        for (int a = 0; a < Avenues; a++)
        {
            float az = a * 360f / Avenues;
            Quaternion turn = Quaternion.Euler(0f, az, 0f);
            Vector3 side = Radial(1f, az + 90f), along = Radial(1f, az);
            for (int k = 0; k < 3; k++)
                for (int s = -1; s <= 1; s += 2)
                    Standard(boxes, Polar(Plaza + 36f + k * 40f, az) + side * (s * 9.5f), 7f, az);
            if (a % 2 == 0)
                for (int k = 0; k < 4; k++)
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Vector3 p = Polar(Plaza + 24f + k * 22f, az) + side * (s * 7.6f);
                        if (Cut(p, 3f)) continue;
                        Box(boxes, marble, p + Vector3.up * 0.75f, new Vector3(1.9f, 1.5f, 5.6f), turn);                     // a limousine, black
                        Box(boxes, stoneChar, p + Vector3.up * 1.75f, new Vector3(1.6f, 0.55f, 2.6f), turn);                 // its cabin
                        Box(boxes, glowWhite, p + Vector3.up * 0.9f + along * (s < 0 ? 2.75f : -2.75f), new Vector3(1.4f, 0.2f, 0.1f), turn);   // a lamp glint
                    }
            // The newsstand on the plaza's edge, and the flag beside it
            Vector3 mouth = Polar(Plaza - 6f, az) + side * 14f;
            if (!Cut(mouth, 3f))
            {
                Box(boxes, stoneChar, mouth + Vector3.up * 1.3f, new Vector3(3.2f, 2.6f, 2.2f), turn);
                Box(boxes, oxblood, mouth + Vector3.up * 2.8f, new Vector3(3.8f, 0.2f, 2.8f), turn);
                Box(boxes, glowWarm, mouth + Vector3.up * 1.4f - along * 1.15f, new Vector3(2.4f, 1.2f, 0.1f), turn);
            }
            Flagpole(boxes, Polar(Plaza - 6f, az) - side * 14f, 11f);
            // The figures where the avenue leaves the belt
            for (int s = -1; s <= 1; s += 2) Statue(boxes, Polar(BeltOut + 12f, az) + side * (s * 12f), 7f, az + 180f);
        }
        // The fountains: a stone basin, a bronze figure, the water lit cream from under
        for (int f = 0; f < 2; f++)
        {
            Vector3 at = Polar(150f, f == 0 ? 150f : 210f);
            if (Cut(at, 8f)) continue;
            Box(boxes, stoneLime, at + Vector3.up * 0.6f, new Vector3(14f, 1.2f, 14f), null, PrimitiveType.Cylinder);
            Box(boxes, stoneLime, at + Vector3.up * 1.8f, new Vector3(5f, 1.2f, 5f), null, PrimitiveType.Cylinder);
            Box(boxes, glowWhite, at + Vector3.up * 3.2f, new Vector3(2.2f, 3f, 2.2f), null, PrimitiveType.Cylinder);
            Statue(boxes, at + Vector3.up * 2.4f, 8f, 180f);
            for (int k = 0; k < 4; k++) Box(boxes, glowWhite, at + Radial(6.4f, k * 90f + 45f) + Vector3.up * 1.6f, Vector3.one * 0.7f, null, PrimitiveType.Sphere);
        }
    }

    // ------------------------------------------------------------ for the levels

    /// Which OuterTower variant a level's Beyond() builds on a lot: cells `near` the route (the ones that get companions)
    /// are mostly the low fabric; the far cells, which are the skyline, more often the shafts. `a` and `b` are two
    /// randoms in [0,1)
    public static int FabricVariant(float a, float b, bool near)
    {
        float low = near ? 0.62f : 0.4f, mid = near ? 0.26f : 0.33f;
        int[] lows = { 14, 15, 16, 17, 18, 19, 11, 12 }, mids = { 6, 7, 13, 10, 8, 20, 24, 21 }, tall = { 0, 1, 2, 3, 5, 23, 1, 6 };
        int[] pick = a < low ? lows : a < low + mid ? mids : tall;
        return pick[Mathf.Clamp((int)(b * pick.Length), 0, pick.Length - 1)];
    }
}
