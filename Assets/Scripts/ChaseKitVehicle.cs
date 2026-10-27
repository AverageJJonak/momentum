using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// THE VEHICLES the bosses ride ahead of you. THE TROLLEY: the Concierge's brass luggage trolley on the House's rail —
/// four brass wheels, a walnut deck with a brass plate, a canopy frame over the luggage up front, two amber lamps
/// and the desk bell; he stands at its open tail, where the fists reach him. THE LOCOMOTIVE: the Stationmaster's tank
/// engine — riveted frame on six wheels, the boiler, the dome and the chimney, side tanks with cream piping, an
/// oxblood cab with the company plate, buffer beams, an amber headlamp; he stands on the open footplate behind the
/// cab. The root is moved by ChaseBoss (a kinematic body, so its colliders move for free); the solids are the deck
/// and the frame, the boiler and the cab; every lamp is handed back so the break can turn them red.
/// </summary>
public static partial class LevelKit
{
    /// Builds the vehicle under `parent` at the origin; `stand` is where the boss's feet go (local), `lamps` its lights
    public static Transform ChaseVehicle(Transform parent, ChaseKind k, out Vector3 stand, out Light[] lamps, out Transform cab)
    {
        var root = new GameObject("ChaseVehicle");
        root.transform.SetParent(parent);
        root.transform.localPosition = Vector3.zero;
        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        var lights = new List<Light>(2);
        Transform t = root.transform;
        Quaternion axle = Quaternion.Euler(0f, 0f, 90f);
        cab = null;
        if (k == ChaseKind.Concierge)
        {
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    Prim(t, new Vector3(sx * 0.95f, 0.35f, sz * 1.7f), new Vector3(0.7f, 0.1f, 0.7f), Brass, "TrolleyWheel", PrimitiveType.Cylinder, axle, true);
            Prim(t, new Vector3(0f, 0.35f, 0f), new Vector3(0.12f, 1.7f, 0.12f), Bronze, "Axle", PrimitiveType.Cylinder, axle);
            Slab(t, new Vector3(0f, 0.95f, 0f), new Vector3(2.4f, 0.5f, 5.2f), WoodPanel, "TrolleyDeck");
            Prim(t, new Vector3(0f, 1.2f + MarkProud, 0f), new Vector3(2.3f, 0.006f, 5.1f), Brass, "DeckPlate");
            for (int sx = -1; sx <= 1; sx += 2)
            {
                Prim(t, new Vector3(sx * 1.05f, 2.2f, 0.4f), new Vector3(0.08f, 2f, 0.08f), Brass, "CanopyPost");
                Prim(t, new Vector3(sx * 1.05f, 2.2f, 2.3f), new Vector3(0.08f, 2f, 0.08f), Brass, "CanopyPost");
                Prim(t, new Vector3(sx * 1.0f, 1.7f, 2.72f), Vector3.one * 0.3f, SignalAmber, "TrolleyLamp", PrimitiveType.Sphere);
                var l = Light(t, new Vector3(sx * 1.0f, 1.8f, 2.9f), 16f, 2.2f);
                l.color = new Color(1f, 0.62f, 0.2f);
                lights.Add(l);
            }
            Prim(t, new Vector3(0f, 3.2f, 1.35f), new Vector3(2.3f, 0.08f, 2.1f), Brass, "CanopyRail");
            Prim(t, new Vector3(0f, 3.26f, 1.35f), new Vector3(2.2f, 0.03f, 2f), Cream, "Canopy");
            Prim(t, new Vector3(-0.5f, 1.55f, 1.4f), new Vector3(0.9f, 0.6f, 0.7f), WoodPanel, "Trunk");
            Prim(t, new Vector3(0.45f, 1.5f, 1.8f), new Vector3(0.8f, 0.5f, 0.6f), Oxblood, "Trunk");
            Prim(t, new Vector3(0.3f, 2.0f, 1.3f), new Vector3(0.6f, 0.4f, 0.6f), WoodPanel, "Hatbox", PrimitiveType.Cylinder);
            Prim(t, new Vector3(0f, 3.45f, 0.4f), Vector3.one * 0.22f, Brass, "DeskBell", PrimitiveType.Sphere);
            stand = new Vector3(0f, 1.2f, -1.9f);
        }
        else
        {
            Slab(t, new Vector3(0f, 0.75f, 0f), new Vector3(2.2f, 0.3f, 7f), RivetIron, "LocoFrame");
            for (int sx = -1; sx <= 1; sx += 2)
                for (int i = -1; i <= 1; i++)
                    Prim(t, new Vector3(sx * 1.0f, 0.5f, i * 2f), new Vector3(1f, 0.12f, 1f), Graphite, "LocoWheel", PrimitiveType.Cylinder, axle, true);
            Solid(t, PrimitiveType.Cylinder, new Vector3(0f, 1.85f, 1.3f), Quaternion.Euler(90f, 0f, 0f), new Vector3(1.7f, 2.1f, 1.7f), RivetIron, "Boiler");
            Prim(t, new Vector3(0f, 2.9f, 1.3f), new Vector3(0.9f, 0.7f, 0.9f), Brass, "Dome", PrimitiveType.Sphere);
            Prim(t, new Vector3(0f, 3.3f, 3.0f), new Vector3(0.5f, 0.55f, 0.5f), Graphite, "Chimney", PrimitiveType.Cylinder);
            Prim(t, new Vector3(0f, 3.85f, 3.0f), new Vector3(0.7f, 0.1f, 0.7f), Brass, "ChimneyCap", PrimitiveType.Cylinder);
            for (int sx = -1; sx <= 1; sx += 2)
            {
                Prim(t, new Vector3(sx * 0.98f, 1.95f, 1.2f), new Vector3(0.5f, 1.2f, 3.4f), RivetIron, "SideTank");
                Prim(t, new Vector3(sx * 1.24f, 2.3f, 1.2f), new Vector3(0.03f, 0.05f, 3.3f), Cream, "TankPiping");
                Prim(t, new Vector3(sx * 0.8f, 1.1f, 3.62f), new Vector3(0.3f, 0.3f, 0.3f), RivetIron, "Buffer", PrimitiveType.Cylinder, Quaternion.Euler(90f, 0f, 0f));
                Prim(t, new Vector3(sx * 0.8f, 1.1f, -3.62f), new Vector3(0.3f, 0.3f, 0.3f), RivetIron, "Buffer", PrimitiveType.Cylinder, Quaternion.Euler(90f, 0f, 0f));
            }
            Prim(t, new Vector3(0f, 1.0f, 3.55f), new Vector3(2.4f, 0.5f, 0.16f), SignalRed, "BufferBeam");
            Prim(t, new Vector3(0f, 1.0f, -3.55f), new Vector3(2.4f, 0.5f, 0.16f), SignalRed, "BufferBeam");
            // The cab on its own unscaled pivot (the break twists it): the box and its trim in world coordinates under it
            var pivot = new GameObject("CabPivot");
            pivot.transform.SetParent(t);
            pivot.transform.localPosition = new Vector3(0f, 0.9f, -1.2f);
            cab = pivot.transform;
            Slab(cab, new Vector3(0f, 2.05f, -1.2f), new Vector3(2.2f, 2.3f, 1.6f), Oxblood, "LocoCab");
            Prim(cab, new Vector3(0f, 3.25f, -1.2f), new Vector3(2.4f, 0.1f, 1.8f), Graphite, "CabRoof");
            Prim(cab, new Vector3(0f, 2.3f, -0.38f), new Vector3(1.5f, 1.0f, 0.03f), Graphite, "CabWindow");
            Prim(cab, new Vector3(0f, 1.5f, -0.39f), new Vector3(1.3f, 0.3f, 0.03f), Brass, "CompanyPlate");
            Prim(t, new Vector3(0f, 2.6f, 3.6f), Vector3.one * 0.4f, SignalAmber, "Headlamp", PrimitiveType.Sphere);
            var head = Light(t, new Vector3(0f, 2.6f, 3.9f), 22f, 2.6f);
            head.color = new Color(1f, 0.62f, 0.2f);
            lights.Add(head);
            Prim(t, new Vector3(0f, 1.5f, -3.6f), Vector3.one * 0.25f, SignalRed, "TailLamp", PrimitiveType.Sphere);
            var tail = Light(t, new Vector3(0f, 1.6f, -3.8f), 10f, 1.4f);
            tail.color = new Color(1f, 0.25f, 0.15f);
            lights.Add(tail);
            stand = new Vector3(0f, 0.9f, -2.95f);
        }
        lamps = lights.ToArray();
        return t;
    }
}
