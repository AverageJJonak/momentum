
Design directive from the user (2026-08-31): the movement systems should deliberately contain "gaps" — unintended-feeling glitches that grant momentum. These are detected by a sudden speed boost with no known source and must REWARD the player.

**Why:** discovery of emergent tech is part of the fantasy (Deadlock-style "bugs become features"); patching them would kill the highest expression of mastery.

**How to apply:** `PlayerMotor` runs an attribution system — every intentional speed change (accel, jump multipliers, boosts, kicks) calls `Attribute()`; at tick end, unattributed horizontal gain above `glitchSpeedGainThreshold` (2 m/s) fires `MoveType.GlitchTech` ("??? HIDDEN TECH"), refunds a pip, respects a 1s cooldown. When adding new mechanics: attribute their intentional gains, but do NOT "fix" emergent speed exploits (seam pops, slope-storage conversions from AlignToPlane preserving magnitude, etc.) — leave them in; the detector turns them into rewards. Known intentional gap: falling fast onto a steep slope while sliding, then riding to flat, converts fall speed to horizontal. Related: [[skill-gated-momentum]].
