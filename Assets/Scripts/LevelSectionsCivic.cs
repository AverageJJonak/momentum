using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE CIVIC ROOMS — chapter one's variety. The first act used to be halls,
/// arenas and corridors in rotation; these are six rooms that do not repeat
/// each other, and none of them is a tube. Each one is built around a
/// different VERB, so the level reads as a sequence of decisions rather than a
/// sequence of corridors:
///   PLAZA     open sky over a square you can sprint across in any direction:
///             a pool in the middle, decks at the corners, pads that throw you
///             between them. The room the playtest liked, built bigger.
///   GALLERIA  a shopping street under a skylight with a mezzanine ring that
///             goes all the way round: take the high line or the low one, and
///             the shopfronts between them are glass.
///   ROOFTOPS  open air, and the floor is a service deck eight metres DOWN:
///             the route is roof to roof across real gaps, and missing one
///             costs you the climb back up rather than the run.
///   CARPARK   the opposite of all of them — low, dark, a dense grid of
///             pillars that cuts every sightline. No grapple line up there.
///   BATHS     the Underbelly's civic pool, flooded and split into tiers, the
///             rot in the water and a diving platform over it.
///   FOYER     one tall square void with galleries stacked up its walls: the
///             fight happens on four levels at once.
/// House rules as everywhere: detail geometry carries no colliders, anything
/// you can walk into is solid, and every section ends through Exit so the room
/// loop and the city outside both know about it.
/// </summary>
public static partial class LevelSections
{
    // ------------------------------------------------------------ the plaza

    public static Cursor Plaza(Transform root, MovementConfig config, Cursor c, float len = 84f, float halfW = 34f, float height = 26f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY;
        Vector3 mid = new Vector3(0f, y, z0 + len * 0.5f);
        var door = Door(y);
        var glass = HighGlass(y + 8f);
        Seal(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1),
            LevelKit.Faces.North | LevelKit.Faces.East | LevelKit.Faces.West | LevelKit.Faces.Floor, door, glass);
        LevelKit.Clouds(root, new Vector3(0f, 0f, mid.z), halfW * 1.6f, len * 0.6f, y + height + 8f, 8);
        LevelKit.DoorFrame(root, door, z1);
        LevelKit.Glass(root, new Vector3(0f, y + 10f, z1), new Vector3(6f, 4f, 0.3f), config);
        LandGlass(root, c);
        var avoid = new List<Vector3>();

        // The middle: a round pool with an island in it, the company's mark laid in the paving around it
        LevelKit.RoundInset(root, mid, 13f);
        LevelKit.Pool(root, mid, new Vector2(16f, 16f));
        LevelKit.Slab(root, mid + Vector3.up * 0.9f, new Vector3(5f, 1.8f, 5f), LevelKit.WhiteGloss, "FountainPlinth");
        LevelKit.Orb(root, mid + Vector3.up * 4.5f);
        LevelKit.FloorRing(root, mid, 15f);
        avoid.Add(mid);

        // Four corner decks, and a pad under each one: the square is crossed over the top as readily as across it
        float dx = halfW * 0.58f, dz = len * 0.28f;
        for (int i = 0; i < 4; i++)
        {
            float sx = (i % 2 == 0) ? -1f : 1f, sz = (i < 2) ? -1f : 1f;
            var deck = new Vector3(sx * dx, y + 6f, mid.z + sz * dz);
            LevelKit.FloorSlab(root, deck + Vector3.down * 0.5f, new Vector3(12f, 1f, 12f), "PlazaDeck");
            LevelKit.GlassRail(root, deck + new Vector3(-6f, 0f, 0f), deck + new Vector3(6f, 0f, 0f));
            LevelKit.PlanterBox(root, deck + new Vector3(sx * 3.5f, 0f, sz * 3.5f), new Vector3(3f, 0.7f, 3f), 4.5f);
            LevelKit.JumpPad(root, new Vector3(sx * (dx - 9f), y, mid.z + sz * (dz - 9f)), 0f, 17f, 7f);
            LevelKit.Light(root, deck + Vector3.up * 8f, 34f, 1.8f);
            avoid.Add(new Vector3(deck.x, y, deck.z));
            avoid.Add(new Vector3(sx * (dx - 9f), y, mid.z + sz * (dz - 9f)));
        }

        // The civic furniture, out where the walls are: kiosks, benches, planting, a bollard line at the way in
        for (int s = -1; s <= 1; s += 2)
        {
            LevelKit.Kiosk(root, new Vector3(s * (halfW - 6f), y, z0 + len * 0.22f), s > 0 ? -90f : 90f);
            LevelKit.Bench(root, new Vector3(s * (halfW - 8f), y, z0 + len * 0.62f), 0f);
            LevelKit.Lawn(root, new Vector3(s * (halfW - 10f), y, z0 + len * 0.8f), new Vector2(14f, 18f));
            LevelKit.Hedge(root, new Vector3(s * (halfW - 17f), y, z0 + len * 0.8f), new Vector3(0.9f, 0.9f, 18f));
            for (int t = 0; t < 3; t++)
                LevelKit.Tree(root, new Vector3(s * (halfW - 11f), y, z0 + len * (0.72f + 0.08f * t)), R(5f, 7f), t != 1);
        }
        for (int i = 0; i < 6; i++) LevelKit.Bollard(root, new Vector3(-12f + i * 5f, y, z0 + 4f));
        LevelKit.Chevrons(root, new Vector3(0f, y, z0 + 9f), Vector3.forward, 4);
        LevelKit.Runner(root, new Vector3(0f, y, z0 + len * 0.5f), new Vector2(5f, len - 8f));

        // Overhead: rings to swing from, banners on the wall heads, the mark over the way out
        LevelKit.LightRing(root, new Vector3(0f, y + 15f, z0 + len * 0.28f), y + height, true);
        LevelKit.LightRing(root, new Vector3(0f, y + 15f, z0 + len * 0.72f), y + height, true);
        LevelKit.HoloBanner(root, new Vector3(-halfW + 2f, y + height - 1f, mid.z), 4f, 9f, y + height, 1);
        LevelKit.HoloBanner(root, new Vector3(halfW - 2f, y + height - 1f, mid.z), 4f, 9f, y + height, 2);
        LevelKit.Emblem(root, new Vector3(0f, y + 14f, z1 - 0.25f), Quaternion.LookRotation(Vector3.back), 5f, LevelKit.OrbMat, LevelKit.Graphite);
        LevelKit.Sign(root, new Vector3(0f, y + 7f, z1), Vector3.back, 12f, 2.4f, "PALM COURT");

        Dress(root, c, halfW, y, height, z0, z1, true);
        Squad(config, -halfW + 6f, halfW - 6f, z0 + 12f, z1 - 12f, y, y + 10f, 3, 3, 2, avoid);
        EnemyPool.Air(new Vector3(-dx, y + 12f, mid.z - dz), config);
        EnemyPool.Air(new Vector3(dx, y + 12f, mid.z + dz), config);
        Postcard(new Vector3(-halfW * 0.6f, y + 12f, z0 + 8f), mid + Vector3.up * 4f, 76f);
        return Exit(c, z1, y, halfW, y, y + height);
    }

    // ------------------------------------------------------------ the galleria

    public static Cursor Galleria(Transform root, MovementConfig config, Cursor c, float len = 110f, float halfW = 22f, float height = 18f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY;
        var door = Door(y);
        Seal(root, c, halfW, y, height);
        var win = WindowHoles(c, halfW, y, height, z0, z1);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), NoSouth, With(win, door));
        Windows(root, win, halfW);
        LevelKit.DoorFrame(root, door, z1);
        LandGlass(root, c);
        var avoid = new List<Vector3>();
        float mezY = y + 7f, mezW = 7f;

        // The ring: a gallery down both sides and across both ends, so the high line goes all the way round
        for (int s = -1; s <= 1; s += 2)
        {
            LevelKit.FloorSlab(root, new Vector3(s * (halfW - mezW * 0.5f), mezY - 0.5f, z0 + len * 0.5f), new Vector3(mezW, 1f, len - 4f), "Mezzanine");
            LevelKit.GlassRail(root, new Vector3(s * (halfW - mezW), mezY, z0 + 3f), new Vector3(s * (halfW - mezW), mezY, z1 - 3f));
            LevelKit.NeonStrip(root, new Vector3(s * (halfW - mezW + 0.1f), mezY + 0.05f, z0 + len * 0.5f), new Vector3(0.1f, 0.06f, len - 6f));
        }
        LevelKit.FloorSlab(root, new Vector3(0f, mezY - 0.5f, z0 + 4f), new Vector3(halfW * 2f - 2f, 1f, 8f), "MezzanineS");
        LevelKit.FloorSlab(root, new Vector3(0f, mezY - 0.5f, z1 - 4f), new Vector3(halfW * 2f - 2f, 1f, 8f), "MezzanineN");
        // The escalators: one up at each end, on opposite sides, so the ring is a loop and not a dead end
        LevelKit.Ramp(root, -halfW + mezW * 0.5f, mezW, z0 + 12f, y, z0 + 26f, mezY);
        LevelKit.Ramp(root, halfW - mezW * 0.5f, mezW, z1 - 26f, y, z1 - 12f, mezY);
        LevelKit.JumpPad(root, new Vector3(0f, y, z0 + len * 0.5f), 0f, 14f, 0f);

        // The shopfronts under the gallery: glass you can go through, lit from inside
        for (int s = -1; s <= 1; s += 2)
            for (int i = 0; i < 5; i++)
            {
                float z = z0 + len * (0.14f + 0.18f * i);
                LevelKit.Glass(root, new Vector3(s * (halfW - 0.6f), y + 2.4f, z), new Vector3(0.3f, 4.8f, 7f), config);
                LevelKit.Slab(root, new Vector3(s * (halfW - 0.55f), y + 2.4f, z + 3.8f), new Vector3(0.5f, 5f, 0.6f), LevelKit.Dark, "ShopJamb");
                LevelKit.Light(root, new Vector3(s * (halfW - 2.5f), y + 3.5f, z), 12f, 1.3f);
                if (i % 2 == 0) LevelKit.Poster(root, new Vector3(s * (halfW - 0.3f), y + 5.4f, z), s > 0 ? Vector3.left : Vector3.right, 3f, 1.6f, i);
            }

        // The middle of the floor: planting and pods to break the sprint, a skylight the whole length over it
        for (int i = 0; i < 4; i++)
        {
            var p = new Vector3(i % 2 == 0 ? -5f : 5f, y, z0 + len * (0.2f + 0.2f * i));
            LevelKit.PlanterBox(root, p, new Vector3(4f, 0.7f, 4f), 5f);
            avoid.Add(p);
        }
        LevelKit.MeetingPod(root, new Vector3(0f, y, z0 + len * 0.34f), new Vector3(6f, 3.2f, 6f), 12f);
        LevelKit.Lounge(root, new Vector3(0f, y, z0 + len * 0.66f), 0f);
        avoid.Add(new Vector3(0f, y, z0 + len * 0.34f));
        for (int i = 0; i < 3; i++)
            LevelKit.Skylight(root, new Vector3(0f, y + height, z0 + len * (0.25f + 0.25f * i)), new Vector2(10f, 16f));
        LevelKit.Ticker(root, new Vector3(0f, y + height - 2.5f, z1 - 0.2f), Vector3.back, halfW * 1.4f);
        LevelKit.Sign(root, new Vector3(0f, y + 5.5f, z1), Vector3.back, 10f, 2f, "THE GALLERIA");

        Dress(root, c, halfW, y, height, z0, z1);
        Squad(config, -halfW + 9f, halfW - 9f, z0 + 12f, z1 - 12f, y, mezY + 2f, 3, 2, 2, avoid);
        EnemyPool.Ground(new Vector3(halfW - 3.5f, mezY, z0 + len * 0.3f), config);
        EnemyPool.Ground(new Vector3(-halfW + 3.5f, mezY, z0 + len * 0.7f), config);
        Enemy.CreateDummy(new Vector3(halfW - 3.5f, mezY, z0 + len * 0.55f), config);
        return Exit(c, z1, y, halfW, y, y + height);
    }

    // ------------------------------------------------------------ the rooftops

    public static Cursor Rooftops(Transform root, MovementConfig config, Cursor c, float len = 100f, float halfW = 30f, float height = 30f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY, roofY = y + 8f;
        var door = Door(roofY);                               // the way on is at ROOF level: the route is up here
        Seal(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1),
            LevelKit.Faces.North | LevelKit.Faces.East | LevelKit.Faces.West | LevelKit.Faces.Floor, door);
        LevelKit.Clouds(root, new Vector3(0f, 0f, z0 + len * 0.5f), halfW * 1.7f, len * 0.65f, y + height + 10f, 9);
        LevelKit.DoorFrame(root, door, z1);
        LandGlass(root, c);
        var avoid = new List<Vector3>();

        // The service deck at the bottom: what you land on if you miss, and the pads that put you back on the roofs
        LevelKit.Runner(root, new Vector3(0f, y, z0 + len * 0.5f), new Vector2(6f, len - 6f));
        for (int i = 0; i < 4; i++)
        {
            LevelKit.JumpPad(root, new Vector3(i % 2 == 0 ? -8f : 8f, y, z0 + len * (0.18f + 0.22f * i)), 0f, 19f, 4f);
            LevelKit.Duct(root, new Vector3(-halfW + 1f, y + 4f, z0 + len * (0.2f + 0.22f * i)), new Vector3(halfW - 1f, y + 4f, z0 + len * (0.2f + 0.22f * i)), 1.4f, 1.1f);
        }
        LevelKit.Grate(root, new Vector3(0f, y, z0 + len * 0.5f), new Vector2(7f, 7f));

        // The roofs: four decks with real gaps between them, staggered left and right so no gap is a straight line
        float[] gz = { 0.14f, 0.38f, 0.62f, 0.86f };
        float[] gx = { -1f, 1f, -1f, 1f };
        for (int i = 0; i < 4; i++)
        {
            var deck = new Vector3(gx[i] * halfW * 0.42f, roofY, z0 + len * gz[i]);
            LevelKit.FloorSlab(root, deck + Vector3.down * 0.5f, new Vector3(halfW * 0.95f, 1f, len * 0.17f), "Roof");
            LevelKit.GlassRail(root, deck + new Vector3(-halfW * 0.45f, 0f, 0f), deck + new Vector3(halfW * 0.45f, 0f, 0f));
            // Roof furniture: a plant hut to break the line, tanks, aerials, a vent bank
            LevelKit.Slab(root, deck + new Vector3(gx[i] * -5f, 1.6f, 2f), new Vector3(5f, 3.2f, 5f), LevelKit.Wall, "StairHut");
            LevelKit.Slab(root, deck + new Vector3(gx[i] * 6f, 1.2f, -3f), new Vector3(3.4f, 2.4f, 3.4f), LevelKit.Steel, "Tank");
            LevelKit.Scaffold(root, deck + new Vector3(0f, 0f, -5f), 5f, 2.4f, 1.6f);
            LevelKit.Light(root, deck + Vector3.up * 5f, 26f, 1.6f);
            avoid.Add(new Vector3(deck.x, roofY, deck.z));
        }
        // One plank across the widest gap: take it, or take the jump
        LevelKit.FloorSlab(root, new Vector3(0f, roofY - 0.5f, z0 + len * 0.5f), new Vector3(3.2f, 1f, len * 0.1f), "Plank");
        LevelKit.Stripes(root, new Vector3(0f, roofY + 0.05f, z0 + len * 0.5f), new Vector3(3.2f, 0.08f, 0.2f));
        LevelKit.LightRing(root, new Vector3(0f, roofY + 9f, z0 + len * 0.32f), y + height, true);
        LevelKit.LightRing(root, new Vector3(0f, roofY + 9f, z0 + len * 0.74f), y + height, true);
        LevelKit.Billboard(root, new Vector3(0f, roofY + 8f, z0 + len * 0.94f), 14f, 6f, 0f);

        Dress(root, c, halfW, y, height, z0, z1, true);
        Squad(config, -halfW + 6f, halfW - 6f, z0 + 10f, z1 - 10f, roofY, roofY + 7f, 3, 2, 1, avoid);
        EnemyPool.Ground(new Vector3(0f, y, z0 + len * 0.45f), config);   // one waiting on the service deck
        Postcard(new Vector3(-halfW * 0.7f, roofY + 6f, z0 + 6f), new Vector3(0f, roofY + 2f, z0 + len * 0.6f), 78f);
        return Exit(c, z1, roofY, halfW, roofY, y + height);
    }

    // ------------------------------------------------------------ the carpark

    public static Cursor Carpark(Transform root, MovementConfig config, Cursor c, float len = 90f, float halfW = 26f, float height = 7f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY;
        var door = Door(y);
        Seal(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), NoSouth, door);
        LevelKit.DoorFrame(root, door, z1);
        LandGlass(root, c);
        var avoid = new List<Vector3>();

        // The grid: low and dense, so nothing is visible for long and the fight is always close
        for (int ix = -2; ix <= 2; ix++)
            for (int iz = 0; iz < 6; iz++)
            {
                var p = new Vector3(ix * halfW * 0.36f, y + height * 0.5f, z0 + len * (0.1f + 0.16f * iz));
                LevelKit.Pillar(root, p, new Vector3(1.6f, height, 1.6f), LevelKit.Wall);
                if (iz % 2 == 0) LevelKit.NeonStrip(root, new Vector3(p.x, y + height - 0.15f, p.z), new Vector3(1.8f, 0.06f, 1.8f));
                avoid.Add(new Vector3(p.x, y, p.z));
            }
        // Bays painted on the floor, carts parked in some of them
        for (int s = -1; s <= 1; s += 2)
            for (int i = 0; i < 5; i++)
            {
                float z = z0 + len * (0.14f + 0.17f * i);
                LevelKit.Runner(root, new Vector3(s * (halfW - 5f), y, z), new Vector2(7f, 4.5f));
                if (i % 2 == 0) { LevelKit.Cart(root, new Vector3(s * (halfW - 5f), y, z), 90f); avoid.Add(new Vector3(s * (halfW - 5f), y, z)); }
            }
        // A half-deck up one side with a ramp to it: the only high ground in the room
        float deckY = y + height * 0.52f;
        LevelKit.FloorSlab(root, new Vector3(-halfW + 7f, deckY - 0.5f, z0 + len * 0.68f), new Vector3(14f, 1f, len * 0.42f), "HalfDeck");
        LevelKit.Ramp(root, -halfW + 7f, 14f, z0 + len * 0.3f, y, z0 + len * 0.47f, deckY);
        LevelKit.Stripes(root, new Vector3(-halfW + 14.05f, deckY + 0.05f, z0 + len * 0.68f), new Vector3(0.1f, 0.08f, len * 0.42f));
        LevelKit.Chevrons(root, new Vector3(0f, y, z0 + 6f), Vector3.forward, 3);
        for (int i = 0; i < 4; i++) LevelKit.LightStrip(root, new Vector3(0f, y + height - 0.1f, z0 + len * (0.15f + 0.23f * i)), halfW * 1.3f, true, 1.4f);
        LevelKit.Stencil(root, new Vector3(-halfW + 0.1f, y + 2.4f, z0 + len * 0.5f), Vector3.right, "BELOW STAIRS  ·  TRADESMEN'S ENTRANCE", 0.1f, 10f);
        LevelKit.HoloSign(root, new Vector3(0f, y + 4.6f, z1 - 0.3f), Vector3.back, 7f, 1.4f, "WAY OUT  ·  RAMP UP");

        Dress(root, c, halfW, y, height, z0, z1);
        Squad(config, -halfW + 4f, halfW - 4f, z0 + 10f, z1 - 10f, y, y + height - 2f, 5, 1, 3, avoid);
        EnemyPool.Ground(new Vector3(-halfW + 7f, deckY, z0 + len * 0.7f), config);
        return Exit(c, z1, y, halfW, y, y + height);
    }

    // ------------------------------------------------------------ the baths

    public static Cursor Baths(Transform root, MovementConfig config, Cursor c, float len = 76f, float halfW = 24f, float height = 16f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY;
        var door = Door(y);
        Seal(root, c, halfW, y, height);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), NoSouth, door);
        LevelKit.DoorFrame(root, door, z1);
        LandGlass(root, c);
        var avoid = new List<Vector3>();

        // Three tiers stepping down into the water: the room is a bowl, and the bottom of it is flooded
        for (int i = 0; i < 3; i++)
        {
            float inset = 5f + i * 5f, ty = y - i * 1.2f;
            LevelKit.FloorSlab(root, new Vector3(0f, ty - 0.6f, z0 + len * 0.5f), new Vector3(halfW * 2f - inset * 2f, 1.2f, len - inset * 2f), "BathTier");
            LevelKit.NeonStrip(root, new Vector3(0f, ty + 0.05f, z0 + inset + 0.4f), new Vector3(halfW * 2f - inset * 2f, 0.06f, 0.12f));
        }
        LevelKit.Pool(root, new Vector3(0f, y - 3.6f, z0 + len * 0.5f), new Vector2(halfW * 2f - 32f, len - 32f));
        LevelKit.Trough(root, new Vector3(-halfW + 3f, y, z0 + 4f), new Vector3(-halfW + 3f, y, z1 - 4f), 1.6f, 0.7f);
        LevelKit.Trough(root, new Vector3(halfW - 3f, y, z0 + 4f), new Vector3(halfW - 3f, y, z1 - 4f), 1.6f, 0.7f);

        // The colonnade round the top tier, tiled, and the rot coming through it
        for (int s = -1; s <= 1; s += 2)
            for (int i = 0; i < 5; i++)
            {
                var p = new Vector3(s * (halfW - 3f), y, z0 + len * (0.12f + 0.19f * i));
                LevelKit.Column(root, p, height - 2f, 1.6f);
                if (i % 2 == 0) LevelKit.Tendril(root, p + Vector3.up * 0.3f, p + new Vector3(-s * 5f, height * 0.6f, 3f), 0.3f, 7);
                avoid.Add(p);
            }
        // The diving platform over the deep end, and the stair up to it
        float divY = y + 7f;
        LevelKit.FloorSlab(root, new Vector3(0f, divY - 0.5f, z0 + len * 0.78f), new Vector3(10f, 1f, 9f), "DivingDeck");
        LevelKit.Ramp(root, halfW - 6f, 6f, z0 + len * 0.5f, y, z0 + len * 0.72f, divY);
        LevelKit.GlassRail(root, new Vector3(-5f, divY, z0 + len * 0.735f), new Vector3(5f, divY, z0 + len * 0.735f));
        LevelKit.JumpPad(root, new Vector3(0f, y - 2.4f, z0 + len * 0.3f), 0f, 16f, 5f);
        LevelKit.Skylight(root, new Vector3(0f, y + height, z0 + len * 0.45f), new Vector2(14f, 22f));
        LevelKit.Light(root, new Vector3(0f, y + height - 3f, z0 + len * 0.5f), 40f, 1.5f);
        LevelKit.Stencil(root, new Vector3(halfW - 0.1f, y + 3.4f, z0 + len * 0.3f), Vector3.left, "TURKISH BATHS  ·  CLOSED TO GUESTS", 0.1f, 9f);

        Dress(root, c, halfW, y, height, z0, z1);
        Squad(config, -halfW + 6f, halfW - 6f, z0 + 12f, z1 - 12f, y, y + 8f, 3, 2, 2, avoid);
        EnemyPool.Air(new Vector3(0f, divY + 2f, z0 + len * 0.78f), config);
        return Exit(c, z1, y, halfW, y, y + height);
    }

    // ------------------------------------------------------------ the foyer

    public static Cursor Foyer(Transform root, MovementConfig config, Cursor c, float len = 52f, float halfW = 22f, float height = 32f)
    {
        LevelKit.SetZone(c.zone);
        float z0 = c.z, z1 = c.z + len, y = c.floorY;
        var door = Door(y);
        Seal(root, c, halfW, y, height);
        var win = WindowHoles(c, halfW, y, height, z0, z1);
        LevelKit.Room(root, new Vector3(-halfW, y, z0), new Vector3(halfW, y + height, z1), NoSouth, With(win, door));
        Windows(root, win, halfW);
        LevelKit.DoorFrame(root, door, z1);
        LandGlass(root, c);
        var avoid = new List<Vector3>();

        // Four galleries stacked up the walls, each one a three-sided ring, alternating which side is open
        for (int lvl = 0; lvl < 4; lvl++)
        {
            float gy = y + 6f + lvl * 6f, w = 5.5f;
            float openSide = (lvl % 2 == 0) ? 1f : -1f;     // the missing side alternates, so the climb spirals
            for (int s = -1; s <= 1; s += 2)
            {
                if (s == openSide && lvl < 3) continue;
                LevelKit.FloorSlab(root, new Vector3(s * (halfW - w * 0.5f), gy - 0.5f, z0 + len * 0.5f), new Vector3(w, 1f, len - 4f), "Gallery");
                LevelKit.GlassRail(root, new Vector3(s * (halfW - w), gy, z0 + 3f), new Vector3(s * (halfW - w), gy, z1 - 3f));
            }
            LevelKit.FloorSlab(root, new Vector3(0f, gy - 0.5f, z1 - 3f), new Vector3(halfW * 2f - 2f, 1f, 6f), "GalleryN");
            LevelKit.NeonStrip(root, new Vector3(0f, gy + 0.05f, z1 - 6.1f), new Vector3(halfW * 2f - 4f, 0.06f, 0.12f));
            LevelKit.JumpPad(root, new Vector3(-openSide * (halfW - 8f), gy, z0 + len * (lvl % 2 == 0 ? 0.3f : 0.7f)), 0f, 15f, 5f);
        }
        // The floor of the void: the pad that starts the climb, the desk ring, the mark on the end wall
        LevelKit.JumpPad(root, new Vector3(0f, y, z0 + len * 0.35f), 0f, 17f, 4f);
        LevelKit.DeskRing(root, new Vector3(0f, y, z0 + len * 0.62f), 7f);
        LevelKit.RoundInset(root, new Vector3(0f, y, z0 + len * 0.62f), 9f);
        avoid.Add(new Vector3(0f, y, z0 + len * 0.62f));
        for (int i = 0; i < 3; i++) LevelKit.LightRing(root, new Vector3(0f, y + 10f + i * 7f, z0 + len * 0.5f), y + height, true);
        LevelKit.Emblem(root, new Vector3(0f, y + height - 5f, z1 - 0.25f), Quaternion.LookRotation(Vector3.back), 6f, LevelKit.OrbMat, LevelKit.Graphite);
        LevelKit.Banner(root, new Vector3(-halfW + 3f, y + height - 1f, z0 + len * 0.5f), 3.5f, 16f, y + height, true);
        LevelKit.Banner(root, new Vector3(halfW - 3f, y + height - 1f, z0 + len * 0.5f), 3.5f, 16f, y + height, false);
        LevelKit.Sign(root, new Vector3(0f, y + 6f, z1), Vector3.back, 9f, 1.8f, "FOYER  ·  FLOORS 1 – 4");

        Dress(root, c, halfW, y, height, z0, z1);
        Squad(config, -halfW + 8f, halfW - 8f, z0 + 8f, z1 - 8f, y, y + 12f, 2, 2, 2, avoid);
        for (int lvl = 1; lvl < 4; lvl++)
            EnemyPool.Ground(new Vector3((lvl % 2 == 0 ? -1f : 1f) * (halfW - 3f), y + 6f + lvl * 6f, z0 + len * 0.5f), config);
        Postcard(new Vector3(0f, y + 3f, z0 + 5f), new Vector3(0f, y + height - 6f, z1 - 2f), 80f);
        return Exit(c, z1, y, halfW, y, y + height);
    }
}
