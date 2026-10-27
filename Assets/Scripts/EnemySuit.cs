using UnityEngine;

/// <summary>
/// The staff uniform, 1939, PAINTED ON. A clockwork mannequin's coat is not
/// cloth: it is livery painted on the enamel shell (EnemyMannequin), so what
/// hangs here is thin — lapels, a waistcoat, buttons, a bow tie — a few
/// millimetres proud of the chest shell's front face, never a plate (the plates
/// the windup flashes gold are the shells themselves, so the parry language
/// reads exactly as before). The footman wears the tailcoat front over an
/// oxblood waistcoat; the bell staff (the page, the bellhop) wear the short
/// oxblood jacket that is their shell, with the brass buttons that say so.
/// </summary>
public partial class Enemy
{
    static Material suitTrim;

    /// The oxblood of the waistcoat: lit (never an unlit red — that is the danger language)
    static Material Trim() => suitTrim ??= Lit(new Color(0.32f, 0.08f, 0.08f), 0.2f, 0.5f);

    /// The painted coat, added over the skeleton. Renderers: footman 6, page 2, bellhop 3.
    static void SuitUp(Transform root, Rig rig, EnemyKind kind)
    {
        var torso = rig.torso;
        if (torso == null) return;
        const float front = 0.13f;   // the chest shell's front face (BuildRig: 0.26 deep)
        Tie(torso, new Vector3(0f, 0.355f, front + 0.008f), 0.12f, Satin());   // the black bow tie at the collar
        if (kind == EnemyKind.Dummy)
        {
            // THE FOOTMAN: tailcoat lapels painted down from the collar, the oxblood waistcoat between them, two buttons
            for (int s = -1; s <= 1; s += 2)
                Turned(Part(torso, PrimitiveType.Cube, new Vector3(s * 0.085f, 0.23f, front + 0.006f), new Vector3(0.09f, 0.26f, 0.012f), Cloth(), "Lapel"), 0f, 0f, s * -14f);
            Part(torso, PrimitiveType.Cube, new Vector3(0f, 0.11f, front + 0.005f), new Vector3(0.17f, 0.14f, 0.01f), Trim(), "Waistcoat");
            Buttons(torso, new Vector3(0f, 0.15f, front + 0.012f), 2, Vector3.down * 0.06f);
            return;
        }
        // THE BELL STAFF: the short oxblood jacket is the shell itself; a row of brass buttons down it says so
        Buttons(torso, new Vector3(0f, 0.27f, front + 0.012f), kind == EnemyKind.Turret ? 1 : 2, Vector3.down * 0.07f);
    }
}
