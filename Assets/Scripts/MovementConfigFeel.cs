using UnityEngine;

/// <summary>
/// WEAPON FEEL (2026-10-12, the House bible §2d: "the weapons are a bit clunky"). The numbers of the HAND, never of
/// the hit: how fast a change of weapon is, how long a press is kept, how quickly the rig's hands catch a change,
/// how long the cane's draw takes. Hit timings, parry windows, reach, damage and every cap live elsewhere and are
/// untouched by anything here (combat-ladder.md: the ladder laws).
/// </summary>
public partial class MovementConfig
{
    [Header("WEAPON FEEL · the change (slots 1–4, the wheel, NEXT WEAPON)")]
    [Tooltip("Seconds the weapon in hand takes to go (its morph 1 → 0). The next one waits until it is away")]
    public float weaponSwitchOut = 0.15f;
    [Tooltip("Seconds the called-for weapon takes to come up (its morph 0 → 1). The cane's own 'in' is its flourish")]
    public float weaponSwitchIn = 0.15f;
    [Tooltip("Seconds between two wheel notches or NEXT WEAPON presses that are honoured (a slot key has no lockout)")]
    public float weaponSwitchRepeat = 0.12f;
    [Tooltip("Seconds the cane's equip flourish takes; a strike interrupts it at any point")]
    public float caneFlourishTime = 0.25f;

    [Header("WEAPON FEEL · presses")]
    [Tooltip("Seconds a Strike / alt press is kept while the weapon is not ready (a cooldown, a draw). Never a reload: a press waits for the breech on its own. A press made during a change is kept until the new weapon is in, then this long")]
    public float inputBuffer = 0.1f;

    [Header("WEAPON FEEL · the rig during a change")]
    [Tooltip("The rig's catch-up rate while a change of weapon is fresh (viewmodelBlendRate otherwise): the hands reach the new hold inside the change instead of trailing it")]
    public float viewmodelSwitchBlendRate = 40f;
    [Tooltip("Seconds after a change during which the faster rate applies")]
    public float viewmodelSwitchBlendWindow = 0.4f;
}
