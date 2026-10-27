using UnityEngine;

/// <summary>
/// THE VIADUCT: Halcyon Railways' elevated line across the district, thirty metres up, on stone piers. The way out of
/// the Underground ran under it, and the line over it never stopped. (Level11Builder partial)
/// </summary>
public static partial class Level11Builder
{
    /// The elevated line across the district: both ends standing on their piers, the middle down. The west half is the
    /// line — up the fallen span, along the strip between the trains, to the halt at the far end and the valve on the
    /// main there. The east half is a perch, and a run-up. One pier a side is made to be climbed: cream flutes on its
    /// corners (an edge boost), stone corbels up its face (a ladder of lips)
    static void Viaduct()
    {
        Deck(X0 + 1f, BreakW, -123f, 1f);
        Deck(BreakE, X1 - 1f, 118f, -1f);
        // The fallen span: one end still on the line, the other in the street. It is a ramp now, and a long way down at speed
        SurfaceKit.SpanSlab(root, new Vector3(BreakW, DeckY, DeckZ), new Vector3(-26f, 0.15f, DeckZ), DeckW, 1.6f, LevelKit.Slope, "FallenSpan");
        SurfaceKit.Block(LevelKit.Limestone, new Vector3(34f, 0.8f, DeckZ - 6f), new Vector3(DeckW, 1.6f, 58f), Quaternion.Euler(0f, 98f, 3f));   // the rest of it, flat in the street
        Heap(new Vector3(-16f, 0f, DeckZ + 16f), 9f, 6);
        Beacon(new Vector3(-14f, 0f, DeckZ - 14f));
        LevelKit.LineRun(root, new Vector3(-26f, 0.15f, DeckZ), new Vector3(BreakW - 2f, DeckY, DeckZ), Zone.Surface);   // HARD LINE up the span

        // The line: two tracks, a train on each, out of step — and the strip between them, which is all the room there is
        for (int s = -1; s <= 1; s += 2)
        {
            float tz = DeckZ + s * 3.8f;
            LevelKit.Rail(root, new Vector3(X0 + 4f, DeckY, tz), new Vector3(BreakW - 1f, DeckY, tz));
            LevelKit.Rail(root, new Vector3(BreakE + 1f, DeckY, tz), new Vector3(X1 - 4f, DeckY, tz));
            SurfaceKit.Block(LevelKit.Charcoal, new Vector3(BreakW - 3f, DeckY + 0.6f, tz), new Vector3(1f, 1.2f, 3f));   // the buffers they put up at the break
            SurfaceKit.Look(LevelKit.CordonBand, new Vector3(BreakW - 3f, DeckY + 1.3f, tz), new Vector3(1.1f, 0.2f, 3.1f));
            Tram.Create(root, cfg, new Vector3(-250f, DeckY, tz), new Vector3(BreakW - 16f, DeckY, tz), s < 0 ? 0f : 0.5f);
        }
        LevelKit.BrassStrip(root, new Vector3(X0 + 6f, DeckY, DeckZ), new Vector3(BreakW - 7f, DeckY, DeckZ), 0.3f);   // the strip
        // Where they stop, the strip is a platform: a roof is one jump off it while the train stands, and you can run straight over it
        Platform(-251f);
        Platform(BreakW - 17f);
        // The main along the north parapet's outside, torn at the break: steam off the tear, a vent in the deck where it tore
        LevelKit.Pipe(root, new Vector3(-276f, DeckY + 1f, DeckZ + 8.7f), new Vector3(BreakW - 2f, DeckY + 1f, DeckZ + 8.7f), 1f, LevelKit.RivetIron);
        SurfaceKit.Solid(new Vector3((-276f + BreakW - 2f) * 0.5f, DeckY + 1f, DeckZ + 8.7f), new Vector3(274f + BreakW, 1f, 1f));
        LevelKit.SteamLeak(root, new Vector3(BreakW - 2f, DeckY + 1.5f, DeckZ + 8.7f), 4f);
        LevelKit.SteamVentPad(root, new Vector3(BreakW - 5f, DeckY, DeckZ), 6f, 1f);
        LevelKit.SignalGantry(root, new Vector3(-180f, DeckY, DeckZ), 14f, 5.5f, 90f);

        // The halt at the far end: the canopy over it on four columns, the mosaic on its fascia, the globes under it,
        // and the valve house on the main at the end of the platform
        Vector3 halt = new Vector3(-268f, DeckY, DeckZ);
        for (int s = -1; s <= 1; s += 2)
        {
            LevelKit.Column(root, halt + new Vector3(7f, 0f, s * 6.6f), 5f, 0.5f);
            LevelKit.Column(root, halt + new Vector3(-7f, 0f, s * 6.6f), 5f, 0.5f);
        }
        SurfaceKit.Look(LevelKit.Cream, halt + new Vector3(0f, 5.2f, 0f), new Vector3(16f, 0.4f, 14.4f));
        SurfaceKit.Look(LevelKit.Charcoal, halt + new Vector3(0f, 5.6f, 0f), new Vector3(13f, 0.4f, 11.4f));
        SurfaceKit.Look(LevelKit.Brass, halt + new Vector3(0f, 5.95f, 0f), new Vector3(9f, 0.3f, 8f));
        SurfaceKit.Look(LevelKit.Oxblood, halt + new Vector3(8.15f, 4.4f, 0f), new Vector3(0.3f, 1.2f, 14.4f));
        LevelKit.Mosaic(root, halt + new Vector3(8.3f, 4.4f, 0f), Vector3.right, "TERMINUS NORTH  ·  HALT", 9f, 1f);
        LevelKit.GlobeCage(root, halt + new Vector3(3f, 3.6f, 0f), halt.y + 5f);
        LevelKit.GlobeCage(root, halt + new Vector3(-3f, 3.6f, 0f), halt.y + 5f);
        LevelKit.StationBench(root, halt + new Vector3(2f, 0f, 5f), 180f);
        LevelKit.HoloSign(root, halt + new Vector3(8.2f, 2.6f, 0f), Vector3.right, 10f, 0.9f, "SERVICE AS USUAL  ·  MIND THE GAP");
        LevelKit.ReliefValve(root, district, new Vector3(-274f, DeckY, DeckZ), Vector3.right, 0, "THE VIADUCT MAIN");
        Beacon(new Vector3(-236f, DeckY, DeckZ));

        // The east half: a pad up to it, a carriage hanging off the break, a signal gantry across it (its posts kick
        // walls, its beam a lip)
        LevelKit.JumpPad(root, new Vector3(BreakE + 6f, 0f, DeckZ - DeckW * 0.5f - 4f), 0f, 40f, 0f);
        LevelKit.HoloSign(root, new Vector3(BreakE + 6f, 3.4f, DeckZ - DeckW * 0.5f - 8f), Vector3.back, 8f, 1f, "HALCYON RAILWAYS  ·  EMERGENCY ACCESS");
        Vector3 hang = new Vector3(-Mathf.Cos(52f * Mathf.Deg2Rad), -Mathf.Sin(52f * Mathf.Deg2Rad), 0f);
        Carriage(new Vector3(BreakE + 1.5f, DeckY + 1.8f, DeckZ + 3.5f) + hang * 6.5f, Quaternion.LookRotation(hang, Vector3.up));
        LevelKit.SignalGantry(root, new Vector3(160f, DeckY, DeckZ), 14f, 5f, 90f);
    }

    /// A boarding platform on the strip, 1.2 m up, 14 m long, a ramp at either end, brass along its edges
    static void Platform(float x)
    {
        LevelKit.FloorSlab(root, new Vector3(x, DeckY + 0.6f, DeckZ), new Vector3(14f, 1.2f, 3.2f), "Platform");
        SurfaceKit.SpanSlab(root, new Vector3(x - 12f, DeckY + 0.02f, DeckZ), new Vector3(x - 7f, DeckY + 1.2f, DeckZ), 3.2f, 0.4f, LevelKit.Slope, "PlatformRamp");
        SurfaceKit.SpanSlab(root, new Vector3(x + 12f, DeckY + 0.02f, DeckZ), new Vector3(x + 7f, DeckY + 1.2f, DeckZ), 3.2f, 0.4f, LevelKit.Slope, "PlatformRamp");
        SurfaceKit.Look(LevelKit.Brass, new Vector3(x, DeckY + 1.215f, DeckZ - 1.5f), new Vector3(14f, 0.024f, 0.12f));
        SurfaceKit.Look(LevelKit.Brass, new Vector3(x, DeckY + 1.215f, DeckZ + 1.5f), new Vector3(14f, 0.024f, 0.12f));
        SurfaceKit.Look(LevelKit.OxbloodTile, new Vector3(x, DeckY + 0.6f, DeckZ - 1.62f), new Vector3(14f, 0.36f, 0.04f));
        SurfaceKit.Look(LevelKit.OxbloodTile, new Vector3(x, DeckY + 0.6f, DeckZ + 1.62f), new Vector3(14f, 0.36f, 0.04f));
    }

    /// One standing length of the line, from x0 to x1: the deck (you run on it: a kit slab), its parapets (lips), its
    /// stone piers on their plinths — and one pier, at `dressedX`, made to climb: cream flutes on its corners, corbels up
    /// its face on `side` every six metres (a corner boost each; the parapet is one more), a hairline to its foot
    static void Deck(float x0, float x1, float dressedX, float side)
    {
        SurfaceKit.SpanSlab(root, new Vector3(x0, DeckY, DeckZ), new Vector3(x1, DeckY, DeckZ), DeckW, 1.6f, LevelKit.Floor, "Deck");
        float mid = (x0 + x1) * 0.5f, len = x1 - x0, pierH = DeckY - 1.6f;
        for (int s = -1; s <= 1; s += 2)
            LevelKit.Parapet(root, new Vector3(mid, DeckY + 0.5f, DeckZ + s * (DeckW * 0.5f - 0.25f)), new Vector3(len, 1f, 0.5f));
        for (float x = x0 + 18f; x < x1 - 6f; x += 46f)
        {
            if (Mathf.Abs(x - dressedX) < 1f)
            {
                LevelKit.BoostCorner(root, new Vector3(x, pierH * 0.5f, DeckZ), new Vector3(4f, pierH, 4f), LevelKit.Limestone);
                for (float y = 6f; y < DeckY - 4f; y += 6f)
                    LevelKit.Lip(root, new Vector3(x + side * 3.2f, y + 0.6f, DeckZ), new Vector3(2.4f, 1.2f, 4f), LevelKit.Limestone, "Corbel");
                LevelKit.LineRun(root, new Vector3(x + side * 5f, 0f, DeckZ - 22f), new Vector3(x + side * 5f, 0f, DeckZ - 3f), Zone.Surface);
                continue;
            }
            SurfaceKit.Block(LevelKit.Limestone, new Vector3(x, pierH * 0.5f, DeckZ), new Vector3(4f, pierH, 4f));
            SurfaceKit.Look(LevelKit.Limestone, new Vector3(x, 1.2f, DeckZ), new Vector3(5.2f, 2.4f, 5.2f));
            SurfaceKit.Look(LevelKit.Limestone, new Vector3(x, pierH - 0.6f, DeckZ), new Vector3(5f, 1.2f, 5f));
        }
    }
}
