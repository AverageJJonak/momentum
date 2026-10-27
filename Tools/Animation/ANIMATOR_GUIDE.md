# MOMENTUM — the animator's guide

Everything in MOMENTUM is built from primitives at runtime: there are no meshes, no skinned characters, no
Mecanim. The first-person arms are a pair of jointed arms with floating wrists; every enemy is a clockwork
mannequin whose parts hang off a handful of pivots. This folder is the bridge: the game exports its rigs as
JSON, Blender rebuilds them as proxy objects you can animate, and your clips come back as JSON the game plays
on top of its own procedural motion. Nothing you do here changes the game's mechanics, timings or damage.

Read the CONTRACT first. It is short and every rule in it exists because the runtime depends on it.

--------------------------------------------------------------------------------------------------------------

## 1. THE CONTRACT

### Units, axes, time
- **Metres.** A hand is about 9 cm across; an enemy stands about 1.8 m tall; the arm is 0.61 m long.
- **Unity's axes** are the truth: Y up, Z forward, X right, left-handed. The Blender scripts convert both ways
  (Blender Z-up / Y-forward); you never think about it. Do NOT rotate the rig to "fix" its orientation.
- **60 fps.** Author on a 60 fps timeline. One frame = 16.7 ms. The game samples clips with interpolation, so
  nothing breaks at another rate, but every number in the hit-frame table below is given in frames at 60.
- Time in the game is **scaled**: clips freeze in hitstop and slow-motion with everything else.

### The two rigs
**THE ARMS** (`rig_arms.json`): the root is the first-person camera. Both wrists float in the camera's space:
- `HandR`, `HandL` — the WRISTS. These are what you animate. The game solves the shoulder and the elbow with a
  two-bone IK every frame, so `ShoulderR/L` and `ElbowR/L` are reference only: move them for your own preview,
  the export ignores them.
- The hand frame: the origin is the wrist, **+Z runs out along the fingers, +Y is the back of the hand**, the
  thumb sits on the inner side (−X on the right hand, +X on the left).
- Fingers: four fingers of three phalanx pivots each, `F00 F01 F02` (index, knuckle→tip), `F10..F12` (middle),
  `F20..F22` (ring), `F30..F32` (pinky); the thumb `T0` (base) and `T1` (tip). They fold about their local X.
  The game does not play finger joints one by one: the export summarises each finger as ONE curl (0 straight …
  1 closed) and the runtime picks the nearest of its five hand states (OPEN, RELAXED, PINCH, GRIP, FIST), so
  pose the fingers honestly and the right state follows.
- `Fist` (the brass knuckle bar) and `Grip` (where a handle sits) are sockets. Do not move them.
- **The camera has no translation keys.** It is the root; the game moves it. Rotation keys on it are ignored.
  If you want the player's head to move in a clip, you can't: that is the game's camera follow (see §1.6).

**THE ENEMIES** (`rig_<kind>.json`, one per kind): the root is at the FEET, on the ground, facing +Z. Every rig
keeps the same core pivots: `Torso` (the hips/chest), `HeadPivot` (under Torso), `ArmL`, `ArmR` (under Torso),
`LegL`, `LegR` (under the root). Each kind adds its own sub-pivots (`Muzzle`, `Boom`, `Hatch`, `JawL/JawR`,
`Balance`, `Key`…); the JSON lists them all with their parents. Animate rotations on the pivots; positions
too if you must (the Torso bobs in the game's gait — a position key on it overrides that). Never move the
root: the game walks the unit.

### Clip names
`<weapon>_<action>[_<n>]` for the arms, `<enemykind>_<action>[_<n>]` for enemies, lowercase, underscores:
`knuckles_jab_1`, `knuckles_cross`, `cane_slash_2`, `cane_guard`, `deck_flick`, `blunderbuss_fire`,
`blunderbuss_reload`, `maul_overhead`, `chaser_swing`, `dummy_idle`, `hound_lunge`. The enemy kind is the CODE
name (the staff names are what the player reads): dummy = footman, turret = page, chaser = bellhop, sentry =
doorman, warden = bouncer, marshal = house officer, lurker = valet, skitter = busboy, ocular = usher, overseer =
floor manager, borer = platelayer, rigger = signalman, blaster = ganger, kettler = stoker, damagecontrol =
housekeeping, scourer = sweeper, janitor = sanitation, pursuit = patrolman, hound = hound, interceptor =
courier, moth = moth. The bosses are not driven by clips (their controllers own them).

### Loop flag, events
- A clip is either a one-shot (plays, then the game's own motion springs back in) or a LOOP (idles, walks).
  Set it at export (`--loop`, or the scene property `momentum_loop`). A loop interpolates from its last frame
  back to its first: make the first and last frames different poses, not the same one.
- EVENTS are timeline markers (M in the timeline, F2 to name). The name is the event: `hit`, `step`, `shell`,
  `sound`, `flash`, anything. The game raises them as C# events at that frame, so the weapon's author can hang
  sounds, hit checks or effects on your markers. Pose markers on an action work too.

### 1.5 The hit-frame table — key impacts to the game's clock
The game applies damage and effects on its own clock, from the press. Your clip starts ON THE PRESS (frame 0 =
the frame the key went down). Put the visual impact on these frames so the picture and the hit agree:

| action | the game's moment | frame at 60 | notes |
|---|---|---|---|
| knuckles jab (right) | damage on the press; the fist is at full extension 0.06 s later | 4 | a 30 ms flick back first (frames 0–2), the snap lands on 4, follow-through, recover with overshoot |
| knuckles cross (left) | same clock, bigger travel | 4 | the whole torso turns 6° with it (the game adds the root turn) |
| cane strike | the cut connects at 0.18 s | 11 | wind past the shoulder before 0, the tip crosses the crosshair on 11, the follow-through after |
| maul overhead | 0.2 s wind-up, then the land | 12 | 0–55 % of the wind-up goes up behind the right shoulder; the head is AT the strike pose on 12; 0.55 s loaded recovery (to ~frame 45) |
| blunderbuss fire | the shot leaves on the press | 0 | the recoil starts on frame 0: muzzle climbs 24° (36° both barrels), the off hand slides back and re-seats |
| deck flick | the card leaves at ~0.045 s | 3 | a wrist snap, then a rising arc of ~140 ms (to frame 8) |
| kick | connects at ~0.08 s | 5 | |
| parry / guard | the parry window is 0.15 s from the press | up by 9 | the guard must READ as up by frame 9 at the latest; aim for the sweep to land by frame 5, hold, flick on a connect, recover |

Mark the impact frame with a marker named `hit` even when the game has its own clock — the author can use it.

### 1.6 Reach, and what the engine adds on top
- Keep the wrists **within 0.6 m of the camera** at rest and reachable in motion: the shoulders sit at
  (±0.27, −0.30, −0.12) behind the eye and the arm is 0.32 + 0.29 = 0.61 m. The IK clamps reach at 98.5 % of
  that, then lets the shoulder lead forward up to 0.22 m, then stretches the arm up to 1.8×, and only then
  pulls the hand in. A punch to 0.75 m is fine (the shoulder follows); a hand at 1.2 m is not.
- Poses are framed for the game's **58° vertical viewmodel FOV**. The Blender import sets the camera to it.
  Keep the hands OUT of the crosshair at rest (the centre of the frame is the player's aim).
- The arm length is fixed; do not scale bones.
- **You animate the authored motion only.** The engine layers on top of whatever you key: look sway, walk bob,
  breath, landing/jump springs, dash pull, slide drop, wall plant, sprint pump, hit shudder, the root (torso)
  lean, and the camera follow per swing. Don't bake those in. Don't add "weight" with sways — the springs do
  it. A clip is read as the pure intent: the hands' path, the fingers, the timing.
- Every change of driver in the game is a damped spring, never a cut: your first frame does not have to match
  the game's rest pose exactly (it springs there in ~70 ms), but the closer it starts and ends to the stance,
  the cleaner the hand-over. The rest poses are listed in the rig JSON under `notes`.
- Enemies: the game blends your clip in and out over 0.1 s on top of its own gait/scan/attack motion; while a
  clip plays it fully overrides the pivots it keys and leaves the rest procedural.

### 1.7 Wrist rule (the arms)
Author fist poses so the knuckles continue the forearm's line; never fix an odd wrist by rolling the hand
frame — fix the pose. The IK puts the elbow where the wrist's lean asks, so a straight-looking wrist in
Blender stays straight in the game.

--------------------------------------------------------------------------------------------------------------

## 2. THE WORKFLOW

```
Unity: Momentum > Animation > Export Rig Proxies  ──►  Tools/Animation/rig_arms.json, rig_<kind>.json
Blender: momentum_rig_import.py (one rig per .blend)  ──►  empties = the pivots, proxies = the shells, the camera
         animate the empties, name the markers, set the frame range
Blender: momentum_clip_export.py  ──►  Assets/Resources/Clips/<name>.json
Game: the weapon's / unit's author calls ArmClipDriver.Play / EnemyClipDriver.Play at the beat; ClipLibrary.Has gates it
```

### 2.1 Getting the rigs (done on the Unity side, once per rig change)
Momentum → Animation → Export Rig Proxies. It builds the arm rig and one enemy of every kind in a throw-away
scene (nothing is left behind) and writes one JSON per rig to `Tools/Animation/`. Send the animator the folder.
JSON only: there is no FBX exporter in the project, and none is needed.

### 2.2 Blender: import (per rig)
Blender 3.x or 4.x. Scripting workspace → open `Tools/Blender/momentum_rig_import.py` → set `RIG_JSON` at the top
(or leave it empty: a file dialog opens) → Run Script. You get:
- a collection "Rig <name>" with the pivots as EMPTIES (arrows), proxies as meshes parented to them (not
  selectable: you animate pivots, never shells), the game's colours;
- the scene at 60 fps, metres, frame range 0–60;
- for the arms: `FirstPersonCamera`, a child of the root, 58° vertical FOV, set as the scene camera. Look through
  it (Numpad 0) to see what the player sees. Do not key it.
- custom properties on the scene (Scene properties → Custom Properties): `momentum_clip` (the clip name),
  `momentum_loop`, and `momentum_out` (where the export writes: the project's Resources/Clips if the rig JSON
  sat in the project's Tools/Animation). Optional: `momentum_layer_scale` (arms: the engine's sway/bob scale
  during the clip, 1 = full, 0.3 = a calibrated strike), `momentum_snap` (arms: fingers snap instead of
  rippling), `momentum_weapon_hand` (arms: "R" or "L", the hand the weapon rides on; the deck's is "L"),
  `momentum_blend_in` / `momentum_blend_out` (enemies, seconds, default 0.1).

One rig per .blend file; one .blend per clip is the easy habit (the clip name lives on the scene).

### 2.3 Blender: animate
- Keyframe the empties' rotation (and location where it matters: the wrists always, an enemy's Torso when it
  bobs or lunges). Rotation mode is XYZ Euler; use whatever you like, the export reads matrices.
- You may add your own helpers (an IK target for a preview, constraints, drivers): everything is baked on export.
- Frame 0 = the press / the beat. Set the scene's frame range to the clip (`frame_start`/`frame_end`); a
  one-shot ends on its last frame; a loop runs from the last frame back into the first.
- Markers: `M` in the timeline, `F2` to rename: `hit`, `step`, `shell`, `sound`…
- Fingers (arms): fold `F00..F32`, `T0`, `T1` about their local X. The export reduces each finger to one 0…1 curl.

### 2.4 Blender: export
Open `Tools/Blender/momentum_clip_export.py`, Run Script (with the rig selected, or anything: the first rig in
the scene is used). It writes `<momentum_out>/<momentum_clip>.json` and prints what it wrote. Headless:
`blender clip.blend --background --python momentum_clip_export.py -- --name cane_slash_1 --out ".../Assets/Resources/Clips" --loop`.
Only bones that move are written (plus the wrists); positions only where they move; everything in Unity's axes.

### 2.5 The game plays it
Drop the JSON in `Assets/Resources/Clips/`. Nothing plays by itself: the authors of the weapon and enemy files
call the drivers at the moment they would run their own procedural beat, gated on `ClipLibrary.Has`:

```csharp
// in a weapon file, at the press that starts its procedural cut:
if (ClipLibrary.Has("cane_slash_1")) ArmClipDriver.Play("cane_slash_1", 1f, false, sword);   // the cane rides in the right hand
// in a unit's file, at its windup:
if (ClipLibrary.Has("chaser_swing")) EnemyClipDriver.Play(this, "chaser_swing");
```
Hands or pivots a clip does not key keep their procedural motion. When the clip ends (or `Stop()`), the game's
own motion springs back (arms) or fades back over `blendOut` (enemies). Markers arrive as C# events
(`ArmClipDriver.Marker`, `EnemyClipDriver.Marker`, or the callback given to `Play`).

To see the pipeline work before any clip exists: `example_idle` is a hand-written 2 s breathing loop of the right
fist at the knuckles' stance. `ArmClipDriver.Play("example_idle", 1f, true)` from any script while the knuckles
are up shows it; `ArmClipDriver.Stop()` ends it. There are no debug keys.

--------------------------------------------------------------------------------------------------------------

## 3. THE RUNTIME API (for the code side)

`ClipLibrary` — `Has(name)`, `Get(name)` (null if missing), `Names`, `Count`, `Reload()`. Loads `Resources/Clips/*.json` once.

`ArmClipDriver` (a component on the rig's camera, made on first use; runs before `ArmRig`)
- `static bool Play(string name, float speed = 1, bool? loop = null, Transform weapon = null, ArmRig.HandSide? weaponHand = null, Action<string> onMarker = null)`
  `weapon` = the transform the hand is holding (the cane, the deck, the gun, the maul): it rides in `weaponHand`
  (default: the clip's `weaponHand`) for the clip's length, measured off the live hold on the first frame — pass
  it, or the weapon stays at its own pose while the hand moves. Returns false when the clip or the rig is missing.
- `static void Stop()`, `static bool Playing`, `static string Current`, `static float Progress`, `static event Action<string,string> Marker` (clip, marker).
- How it overrides: each keyed hand is held on a driver-owned anchor (`HoldWeapon` on the anchor every frame, before
  the rig solves). A hold outranks the weapon file's own `Drive`/`HoldWeapon` of that frame; the anchor being a
  different "weapon" makes the rig run its own catch-up spring in and out. The weapon's own per-frame calls resume
  the instant the clip ends. Public API only; nothing in `ArmRig*` changed.
- Two-handed weapons: key both hands (a hand without a track keeps the weapon's own follower hold, which reads the
  weapon one frame late while the clip carries it).
- Time is scaled (`Time.deltaTime`): clips freeze in hitstop.

`EnemyClipDriver` (a plain object on `Enemy.clips`, made on first use; applied by `Enemy.Update` right after the kind's `Animate*`)
- `static bool Play(Enemy e, string name, float speed = 1, bool? loop = null, Action<string> onMarker = null)`, `static void Stop(Enemy e)`,
  `Playing`, `Current`, `static event Action<Enemy,string,string> Marker`.
- Blends in/out over the clip's `blendIn`/`blendOut` (0.1 s) over the procedural pose; keyed pivots are overridden,
  the rest stay procedural; a bone name the rig lacks is skipped with a warning; bosses are not supported.

Hook: `Enemy.cs` — the field `public EnemyClipDriver clips;` and the one call `clips?.Apply();` after the Animate switch.

--------------------------------------------------------------------------------------------------------------

## 4. TROUBLESHOOTING
- The hand goes to the right place but the weapon stays behind → `Play` was called without the weapon transform.
- The rig looks mirrored or lying down in Blender → the JSON was edited by hand, or an older script ran; re-import.
- A clip warns "keys 'X', which EnemyY has no pivot for" → the clip was exported from another kind's rig.
- Fingers never close → the finger pivots were not folded about local X (check `F00` turns toward the palm).
- The hit lands before the picture → the impact frame is later than the table in §1.5; the game's clock does
  not wait for the clip.
- Nothing plays → `ClipLibrary.Has` is false: the file is not under `Assets/Resources/Clips`, or its `name`
  field differs from what the code asks for (the Console lists skipped clips on load).
