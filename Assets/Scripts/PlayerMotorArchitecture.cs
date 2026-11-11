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

    // ---------------------------------------------------------------- THE RAIL GRIND

    Collider groundCollider;                       // what the feet are on: the ground check and the snap write it; read only while grounded
    bool grinding;
    MoveSurface grindRail;
    float grindSign = 1f;                          // which way along the rail's line
    float grindGain = 1f;                          // the freshness the rail was caught at: the jump off it is paid at this
    float grindStartTime = float.NegativeInfinity;
    float grindEndTime = float.NegativeInfinity;   // when the feet ran off a rail's end (a jump inside the coyote time is still a jump off the rail)
    public bool Grinding => grinding;
    /// The piece under the feet, or null in the air (the gym's tools ask which station it belongs to)
    public Collider GroundCollider => grounded ? groundCollider : null;

    /// A teleport (a respawn, the gym's menu) lets go of a rail and of a pole: neither follows the body to where it is put
    void ClearArchitectureMoves()
    {
        grinding = false;
        grindRail = null;
        grindEndTime = float.NegativeInfinity;
        groundCollider = null;
        poleSwinging = false;
        pole = null;
        poleArmed = null;
        poleNear = null;
        PoleTurnRate = 0f;
    }

    /// Each tick, after the ground check and before the slide's: feet on a MARKED rail are grinding (a slide ends: the
    /// rail carries you), feet on anything else are not. Rail to rail without leaving the ground is one grind
    void UpdateGrind()
    {
        MoveSurface rail = config.grindEnabled && grounded ? MoveSurface.Of(groundCollider) : null;
        if (rail != null && rail.kind != MoveSurface.Kind.Grind) rail = null;
        if (rail == null)
        {
            if (grinding) grindEndTime = grounded ? float.NegativeInfinity : Time.time;   // stepped off onto a floor: over. Ran off the end: the coyote time
            grinding = false;
            grindRail = null;
            return;
        }
        if (grinding && rail == grindRail) return;
        rail.Line(out Vector3 a, out Vector3 b);
        Vector3 axis = (b - a).normalized;
        float along = Vector3.Dot(velocity, axis);
        if (Mathf.Abs(along) < 0.5f)   // all but still: downhill, or the way you face on a level rail
            along = Mathf.Abs(axis.y) > 0.01f ? -axis.y : Vector3.Dot(Quaternion.Euler(0f, look.Yaw, 0f) * Vector3.forward, axis);
        grindSign = along >= 0f ? 1f : -1f;
        bool caught = !grinding;
        grinding = true;
        grindRail = rail;
        sliding = false;
        if (!caught) return;
        velocity = axis * (grindSign * HorizontalSpeed);   // the catch turns ALL the speed brought to the rail down its line (no gain: a landing across the rail loses nothing either)
        grindStartTime = Time.time;
        grindGain = StyleSystem.GainScale(MoveType.Grind);
        MovementEvents.Emit(MoveType.Grind, "ON");
    }

    /// The tick's movement on a rail: the speed along its line (the rail's least if you brought less), a little more each
    /// second and gravity's share of a rail that falls, and the feet pulled onto the line. No friction, no steering: the
    /// rail is the line; the way off is a jump, or its end
    void GrindMove(float dt)
    {
        grindRail.Line(out Vector3 a, out Vector3 b);
        Vector3 axis = (b - a).normalized * grindSign;
        float hBefore = HorizontalSpeed;
        float least = Mathf.Max(0f, config.grindMinSpeed);
        float speed = Mathf.Max(Vector3.Dot(velocity, axis), least);
        float gain = config.grindAccel + config.gravity * -axis.y;
        if (gain <= 0f) speed = Mathf.Max(least, speed + gain * dt);
        else if (speed < config.grindMaxSpeed) speed = Mathf.Min(config.grindMaxSpeed, speed + gain * dt);
        Vector3 flat = new Vector3(axis.x, 0f, axis.z).normalized;
        Vector3 off = a + Vector3.Project(transform.position - a, b - a) - transform.position;
        off.y = 0f;
        off -= flat * Vector3.Dot(off, flat);   // only the offset ACROSS the rail
        velocity = axis * speed + Vector3.ClampMagnitude(off * Mathf.Min(Mathf.Max(0f, config.grindSnap), 1f / dt), least);   // (the pull is never faster across than the rail's least speed along)
        Attribute(hBefore);
    }

    /// A jump off a rail (TryJump asks as it leaves the ground): once the rail has been ridden a moment the jump takes the
    /// rail's multiplier with it, at the freshness the rail was caught at. Returns the event's detail, or null off a rail
    string GrindJumpOff(ref Vector3 v)
    {
        if (!grinding && Time.time - grindEndTime > config.coyoteTime) return null;
        bool pays = Time.time - grindStartTime >= config.grindJumpMinTime;
        float mult = pays ? 1f + Mathf.Max(0f, config.grindJumpMultiplier - 1f) * grindGain : 1f;
        v.x *= mult;
        v.z *= mult;
        grinding = false;
        grindRail = null;
        grindEndTime = float.NegativeInfinity;
        return $"OFF ×{mult:0.00}";
    }

    // ---------------------------------------------------------------- THE POLE SWING

    bool poleSwinging;
    MoveSurface pole;        // the pole swung round
    MoveSurface poleArmed;   // the key is held and the body still closing on this pole: the swing begins abeam of it
    MoveSurface poleNear;    // a pole inside the reach this tick (the whip key is the pole's while there is one)
    bool poleKeyLast;
    bool poleHoldSpent;      // this hold of the key has had its swing: let go and press again for another
    float poleR, poleSign = 1f, poleSpeed, poleTurned, poleGain = 1f;
    public bool PoleSwinging => poleSwinging;
    /// The whip key belongs to a pole right now: one is in reach, armed, or being swung round (GrappleAbility asks before it throws)
    public bool PoleHasWhipKey => poleSwinging || poleArmed != null || poleNear != null;
    /// How fast the swing is turning the body, degrees a second, signed like yaw (0 off a pole): the look turns with it
    public float PoleTurnRate { get; private set; }

    float PoleReach() => Mathf.Max(0.5f, config.poleReach) + HorizontalSpeed * Mathf.Max(0f, config.poleReachSeconds);

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    /// The nearest MARKED pole inside the reach, or null
    MoveSurface FindPole()
    {
        Vector3 at = transform.position + Vector3.up * (currentHeight * 0.5f);
        float reach = PoleReach(), best = float.MaxValue;
        MoveSurface found = null;
        var poles = MoveSurface.Poles;
        for (int i = 0; i < poles.Count; i++)
        {
            var m = poles[i];
            if (m == null || m.kind != MoveSurface.Kind.Swing || Mathf.Abs(at.y - m.transform.position.y) > m.halfHeight + 1f) continue;
            float d = Flat(at - m.AxisAt(at.y)).magnitude;
            if (d < best && d <= reach) { best = d; found = m; }
        }
        return found;
    }

    /// Each tick, before the jump: the whip key pressed beside a marked pole arms the swing (it begins as the body comes
    /// abeam of the pole, so the turn starts along the way you were going); held, the swing goes on; let go, or a jump,
    /// or the full turn, and the swing lets go. Nothing happens in a level with no marked pole
    void UpdatePoleSwing()
    {
        bool held = Keybinds.Held(GameAction.Grapple) && !PauseMenu.IsPaused;
        bool press = held && !poleKeyLast;
        poleKeyLast = held;
        if (!held) poleHoldSpent = false;
        if (!config.poleSwingEnabled || MoveSurface.Poles.Count == 0)
        {
            if (poleSwinging) EndPoleSwing(false);
            poleArmed = null; poleNear = null;
            return;
        }
        if (poleSwinging)
        {
            if (Dashing || slamming) EndPoleSwing(false);                                          // taken off it by something else
            else if (!held || Time.time <= jumpBufferedUntil || pole == null) EndPoleSwing(true);   // let go (a jump lets go as well, and then jumps)
            return;
        }
        poleNear = FindPole();
        if (poleArmed != null)
        {
            if (!held) poleArmed = null;
            else TryBeginPoleSwing();
            return;
        }
        if ((press || (held && !poleHoldSpent)) && poleNear != null && HorizontalSpeed >= config.poleMinSpeed) { poleArmed = poleNear; TryBeginPoleSwing(); }   // a press in reach, or a key already held on the way in: early is not a miss
    }

    void TryBeginPoleSwing()
    {
        Vector3 rel = Flat(transform.position - poleArmed.AxisAt(transform.position.y));
        Vector3 vh = Flat(velocity);
        float dist = rel.magnitude, h = vh.magnitude, reach = PoleReach();
        if (dist > reach + 1f || h < config.poleMinSpeed) { poleArmed = null; return; }   // went by without closing, or stopped
        float least = Mathf.Max(0.6f, config.poleRadius);
        float closing = dist > 0.01f ? -Vector3.Dot(vh, rel) / dist : 0f;
        if (closing > 0.5f && dist > least) return;   // still coming in: wait for abeam (or the tightest circle)

        pole = poleArmed;
        poleArmed = null;
        poleSwinging = true;
        poleR = Mathf.Clamp(dist, least, Mathf.Max(least, reach));
        Vector3 r0 = dist > 0.01f ? rel / dist : -vh / h;
        float side = Vector3.Dot(Quaternion.AngleAxis(90f, Vector3.up) * r0, vh);   // which way round agrees with the way you were going
        poleSign = side >= 0f ? 1f : -1f;
        poleSpeed = h;
        poleTurned = 0f;
        poleGain = StyleSystem.GainScale(MoveType.PoleSwing);
        EndWallRun();
        ReleaseGrappleIfPulling();
        MovementEvents.Emit(MoveType.PoleSwing, "ON");
    }

    /// The tick's movement on a pole: round its axis at the radius it was caught at and the speed that was brought, the
    /// height held. The step is a velocity, so anything in the way is still met
    void PoleMove(float dt)
    {
        Vector3 pos = transform.position, c = pole.AxisAt(pos.y);
        Vector3 rel = Flat(pos - c);
        Vector3 r0 = rel.sqrMagnitude > 1e-4f ? rel.normalized : Vector3.forward;
        float step = poleSpeed / Mathf.Max(0.3f, poleR) * Mathf.Rad2Deg * dt * poleSign;
        Vector3 next = c + Quaternion.AngleAxis(step, Vector3.up) * (r0 * poleR);
        float hBefore = HorizontalSpeed;
        velocity = Flat(next - pos) / dt;
        Attribute(hBefore);
        poleTurned += Mathf.Abs(step);
        PoleTurnRate = step / dt;
        if (poleTurned >= config.poleMaxTurn) EndPoleSwing(true);
    }

    /// Letting go: the body leaves along the circle's tangent with the speed it brought, and a swing that turned far
    /// enough pays its multiplier at the freshness it was caught at. `clean` false = taken off the pole by something else
    void EndPoleSwing(bool clean)
    {
        poleSwinging = false;
        poleHoldSpent = true;
        PoleTurnRate = 0f;
        if (!clean || pole == null) { pole = null; MovementEvents.Emit(MoveType.PoleSwing, "OFF"); return; }
        Vector3 rel = Flat(transform.position - pole.AxisAt(transform.position.y));
        Vector3 r0 = rel.sqrMagnitude > 1e-4f ? rel.normalized : Vector3.forward;
        Vector3 tangent = Quaternion.AngleAxis(90f * poleSign, Vector3.up) * r0;
        bool pays = poleTurned >= config.poleMinTurn;
        float mult = pays ? 1f + Mathf.Max(0f, config.poleReleaseMultiplier - 1f) * poleGain : 1f;
        float hBefore = HorizontalSpeed;
        velocity = tangent * (poleSpeed * mult);
        Attribute(hBefore);
        pole = null;
        MovementEvents.Emit(MoveType.PoleSwing, $"OFF {poleTurned:0}° ×{mult:0.00}");
    }
}
