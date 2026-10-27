using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Single source of truth for the reset table (spec §3.3): listens to movement
/// events and fires pip refunds. Also tracks the chain-of-N reset — distinct
/// move types used without touching flat ground; re-arms after landing.
/// Combat resets (airborne kill, grapple-yank kill, near-miss) plug in here later.
/// </summary>
public class ResetDetector : MonoBehaviour
{
    public MovementConfig config;
    public StaminaSystem stamina;
    public PlayerMotor motor;

    /// Fired on every reset: the display label, and whether it SCORES style. A movement reset repeated inside
    /// styleResetRepeatWindow refunds the pip but scores nothing (the HUD says REPEAT); combat resets always score.
    public static event Action<string, bool> ResetFired;

    readonly Dictionary<string, float> lastScored = new Dictionary<string, float>();

    readonly HashSet<string> chainMoves = new HashSet<string>();
    bool chainArmed = true;
    /// The chain so far and its target, for the HUD (state: ticks beside the pips)
    public static int ChainProgress { get; private set; }
    public static int ChainTarget { get; private set; } = 3;
    int lastFlatTouchCount;

    void OnEnable() => MovementEvents.MoveExecuted += OnMove;
    void OnDisable() => MovementEvents.MoveExecuted -= OnMove;

    // Every flat-ground landing clears and re-arms the chain (spec §3.3). Keyed
    // on the motor's touch counter so one-tick touch-and-go landings count, and
    // slope landings (which are chain-safe) don't.
    void SyncChain()
    {
        if (motor == null || motor.FlatGroundTouchCount == lastFlatTouchCount) return;
        lastFlatTouchCount = motor.FlatGroundTouchCount;
        chainMoves.Clear();
        chainArmed = true;
        ChainProgress = 0;
    }

    void Update() => SyncChain();

    void OnMove(MoveType type, string detail)
    {
        SyncChain();

        switch (type)
        {
            case MoveType.CornerBoost: Movement(1, "CORNER BOOST"); break;   // the timed jump on a ledge's lip
            case MoveType.EdgeBoost: Movement(1, "EDGE BOOST"); break;       // the kick on a vertical corner: its pip comes back
            case MoveType.PerfectSlideJump: Movement(1, "PERFECT SLIDE-JUMP"); break;
            case MoveType.SlamStorageJump: Movement(config.slamCost, "SLAM STORAGE"); break;
            case MoveType.GlitchTech: Movement(config.glitchRefundPips, "HIDDEN TECH"); break;
            case MoveType.Parry:
                // A surface parry (BLAST, POGO) keeps its pip but goes through the repeat gate: a shallow wall parry at full keep is not a free score every half second
                if (detail == "BLAST" || (detail != null && detail.StartsWith("POGO"))) Movement(1, "PARRY"); else Fire(1, "PARRY");
                break;
        }

        TrackChain(type);
    }

    void TrackChain(MoveType type)
    {
        string key = StyleSystem.FreshnessKey(type);
        if (key == null) return;
        if (motor != null && motor.OnFlatGround)
            return; // moves executed while parked on flat ground don't chain

        if (!chainArmed) return;
        chainMoves.Add(key);
        ChainTarget = config.chainResetCount;
        ChainProgress = chainMoves.Count;
        if (chainMoves.Count >= config.chainResetCount)
        {
            chainArmed = false;
            ChainProgress = 0;
            Movement(1, $"CHAIN OF {config.chainResetCount}");
        }
    }

    /// Combat resets (spec §3.3): airborne kill +1; kill during/just after a
    /// grapple yank = full stamina refill.
    public void OnKill(bool playerAirborne, bool duringYank)
    {
        if (duringYank)
        {
            if (stamina != null) stamina.Refund(config.staminaMaxPips + 1); // full + overcharge
            ResetFired?.Invoke("YANK KILL — FULL REFILL", true);
        }
        else if (playerAirborne)
        {
            Fire(1, "AIRBORNE KILL");
        }
    }

    public void CombatReset(string label, int pips) => Fire(pips, label);

    /// A combat reset: always scores — the enemies set the pace
    void Fire(int pips, string label)
    {
        if (stamina != null) stamina.Refund(pips);
        ResetFired?.Invoke(label, true);
    }

    /// A movement reset: the pip always comes back, but the same one again inside the repeat window scores
    /// nothing — a reset is a trick, and a trick repeated is not one
    void Movement(int pips, string label)
    {
        if (stamina != null) stamina.Refund(pips);
        bool scores = !lastScored.TryGetValue(label, out float at) || Time.time - at > config.styleResetRepeatWindow;
        if (scores) lastScored[label] = Time.time;
        ResetFired?.Invoke(label, scores);
    }
}
