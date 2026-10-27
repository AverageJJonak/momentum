MOMENTUM — Resources/Clips

Every *.json in this folder is an animation CLIP the game can play on top of its procedural rigs. Unity imports
them as TextAssets; ClipLibrary (Assets/Scripts/Clips/ClipLibrary.cs) loads them all once, keyed by the clip's
"name" field (falling back to the file name), and ClipLibrary.Has(name) is the gate every caller uses: a clip
that is not here simply never plays and the procedural motion stays.

Where they come from: Blender, through Tools/Blender/momentum_clip_export.py, which writes straight into this
folder when the rig was imported with momentum_rig_import.py. The full contract for the animator (units, axes,
60 fps, bone names, the hit-frame table, reach limits) is Tools/Animation/ANIMATOR_GUIDE.md.

Naming: <weapon>_<action>[_<n>] for the arms (knuckles_jab_1, cane_slash_2, deck_flick, blunderbuss_fire,
maul_overhead), <enemykind>_<action>[_<n>] for enemies with the kind's CODE name in lowercase (chaser_swing,
dummy_idle, hound_lunge). Lowercase, underscores, no spaces.

Playing one (opt-in; no weapon or enemy file plays anything unless its author asks):
  arms     if (ClipLibrary.Has("cane_slash_1")) ArmClipDriver.Play("cane_slash_1", 1f, false, sword);
  enemies  if (ClipLibrary.Has("chaser_swing")) EnemyClipDriver.Play(this, "chaser_swing");
Markers in the clip raise ArmClipDriver.Marker / EnemyClipDriver.Marker (and the callback passed to Play).

example_idle.json is a hand-written 2 s breathing loop of the right hand at the knuckles' stance (a closed fist),
there to prove the pipeline end to end: ArmClipDriver.Play("example_idle", 1f, true) from any script while the
knuckles are up shows it; ArmClipDriver.Stop() hands the hand back. There are no debug keys.

Format (what the Blender export writes; flat arrays, frames × 3 for positions, × 4 for rotations as Unity x y z w):
  { "name", "rig": "arms" | "<enemykind>", "fps": 60, "frames", "loop", "speed", "blendIn", "blendOut",
    "layerScale", "snap", "weaponHand": "R" | "L",
    "bones": [ { "name": "HandR", "pos": [...], "rot": [...] } ],
    "fingers": [ { "hand": "R", "curl": [ index, middle, ring, pinky, thumb, ... per frame, 0 straight … 1 closed ] } ],
    "events": [ { "frame": 11, "name": "hit" } ] }
While playing in the editor, a clip dropped in here is picked up after ClipLibrary.Reload() (or the next play).
