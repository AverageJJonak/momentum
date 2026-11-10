using UnityEngine;

/// <summary>
/// ARCHITECTURE MOVES (STEP 3, 2026-11-10): the motor's part of the ledge vault (the rail grind and the pole swing
/// follow in their own commits). Add-only: every path here begins by asking for a piece the kit has marked
/// (MoveSurface) and for its own switch in the config, so with no marked piece in a level, or the switch off, the
/// motor is exactly what it was. The numbers are MovementConfigArchitecture's.
/// </summary>
public partial class PlayerMotor
{
    // ---------------------------------------------------------------- THE LEDGE VAULT

    float vaultPaidPressTime = float.NegativeInfinity;   // one vault per press

    /// The lip of a MARKED ledge over a wall contact: the corner boost's probe (a ray down from above the contact, a hair
    /// inside the wall) with the vault's reach, and the top or the face it found must carry the vault's mark
    bool VaultLip(RaycastHit wall, float feetY, out float lipY)
    {
        lipY = 0f;
        Vector3 n = wall.normal;
        n.y = 0f;
        if (n.sqrMagnitude < 1e-4f) return false;
        n.Normalize();
        float reach = Mathf.Max(0.1f, config.vaultLipReach);
        Vector3 origin = new Vector3(wall.point.x, feetY + reach + LipProbeUp, wall.point.z) - n * LipProbeInset;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit top, reach + LipProbeUp + LipProbeDown, mask, QueryTriggerInteraction.Ignore)) return false;
        if (Vector3.Angle(top.normal, Vector3.up) > config.maxSlopeAngle) return false;
        float above = top.point.y - feetY;
        if (above < LipMinAbove || above > reach + 0.01f) return false;
        if (!MoveSurface.Is(top.collider, MoveSurface.Kind.Vault) && !MoveSurface.Is(wall.collider, MoveSurface.Kind.Vault)) return false;
        lipY = top.point.y;
        return true;
    }

    /// A wall contact in the slide loop: a marked ledge within reach, with room on top, is vaulted there and then. A jump
    /// pressed inside the window before the contact makes it a timed vault (graded); with no press it is carried, slower.
    /// Returns true when the vault fired (the loop re-casts the rest of the step from over the lip)
    bool TryVaultAtContact(ref Vector3 pos, RaycastHit hit, float intoSurface, float hBefore)
    {
        if (!config.vaultEnabled || wallRunning || intoSurface < 1f || hBefore < config.vaultMinSpeed) return false;
        if (!VaultLip(hit, pos.y, out float lipY)) return false;
        float rise = lipY + LipClear - pos.y;
        if (rise > 0f && !ClearAt(pos + Vector3.up * rise)) return false;   // no room on top of it
        Vector3 vh = new Vector3(velocity.x, 0f, velocity.z);   // what was brought to the ledge, before the wall takes it
        float since = Time.time - lastJumpPressTime;
        bool timed = lastJumpPressTime != vaultPaidPressTime && since >= 0f && since <= config.vaultWindow;
        PayVault(ref pos, hBefore, vh / Mathf.Max(0.01f, hBefore), lipY, timed ? since : float.NaN);
        return true;
    }

    /// A jump press with a marked ledge dead ahead and the contact inside the window: the vault pays now, early by the
    /// time to the ledge, so the press is not spent on a wall kick or a double jump a moment before the lip
    bool TryVaultFromPress()
    {
        if (!config.vaultEnabled || wallRunning || lastJumpPressTime == vaultPaidPressTime) return false;
        Vector3 vh = new Vector3(velocity.x, 0f, velocity.z);
        float h = vh.magnitude;
        if (h < config.vaultMinSpeed) return false;
        Vector3 travel = vh / h, pos = transform.position;
        float range = Mathf.Max(config.wallKickRange, h * config.vaultWindow);
        if (!CastCapsule(pos, travel, range, out RaycastHit ahead)
            || Vector3.Angle(ahead.normal, Vector3.up) <= config.maxSlopeAngle || ahead.normal.y < -0.5f) return false;
        if (!VaultLip(ahead, pos.y, out float lipY)) return false;
        Vector3 n = ahead.normal;
        n.y = 0f;
        float into = n.sqrMagnitude > 1e-4f ? -Vector3.Dot(vh, n.normalized) : h;
        if (into < 1f) return false;
        float eta = ahead.distance / into;
        if (eta > config.vaultWindow) return false;
        float rise = lipY + LipClear - pos.y;
        if (rise > 0f && !ClearAt(pos + Vector3.up * rise)) return false;
        PayVault(ref pos, h, travel, lipY, eta);
        transform.position = pos;
        return true;
    }

    /// THE VAULT: the feet go to the lip, the speed brought to the ledge is kept (all of it timed, the untimed keep
    /// without a press) and a perfect press adds its tier on top, scaled by freshness; a small hop carries the body on
    void PayVault(ref Vector3 pos, float hSpeed, Vector3 travel, float lipY, float sincePress)
    {
        ReleaseGrappleIfPulling();
        bool timed = !float.IsNaN(sincePress);
        TimingGrade grade = !timed ? TimingGrade.Late
            : config.gradesEnabled ? TimingGrades.Of(sincePress, config.vaultPerfectWindow, config.vaultPerfectWindow + config.gradeGoodBand)
            : TimingGrade.Perfect;
        float frac = !timed ? 0f : grade == TimingGrade.Perfect ? 1f : grade == TimingGrade.Good ? config.gradeGoodFraction : config.gradeLateFraction;
        float mult = (timed ? 1f : Mathf.Clamp01(config.vaultAutoKeep))
                     + Mathf.Max(0f, config.vaultPerfectMultiplier - 1f) * frac * StyleSystem.GainScale(MoveType.Vault);
        if (lipY + LipClear > pos.y) pos.y = lipY + LipClear;
        Launch(travel * (hSpeed * mult) + Vector3.up * config.vaultUp);   // attributed
        jumpedThisTick = true;
        jumpBufferedUntil = float.NegativeInfinity;
        if (timed) vaultPaidPressTime = lastJumpPressTime;
        cornerPaidPressTime = lastJumpPressTime;   // the same press is not a corner boost as well
        lipContactTime = float.NegativeInfinity;
        MovementEvents.Emit(MoveType.Vault, (timed ? TimingGrades.Label(grade) : "CARRIED") + $" ×{mult:0.00}");
        if (timed && config.gradesEnabled) TimingGrades.Report(config, MoveType.Vault, grade);
    }
}
