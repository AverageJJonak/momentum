"""
MOMENTUM clip export for Blender 3.x / 4.x.

Bakes the animation of a rig built by momentum_rig_import.py (an EMPTY hierarchy: the game's pivots) into a clip
JSON the game plays: Assets/Resources/Clips/<name>.json. Constraints, drivers and any IK you added are baked
(every frame is read from the evaluated world matrices), so animate however you like. See
Tools/Animation/ANIMATOR_GUIDE.md for the contract (60 fps, names, the hit-frame table).

USAGE
  In Blender: select any object of the rig (or nothing: the first rig in the scene is used), set CLIP_NAME below or
  the scene's custom property momentum_clip (Scene properties > Custom Properties; momentum_loop for a loop),
  Run Script. The file goes to OUT_DIR, else the scene's momentum_out (set by the import to the project's
  Assets/Resources/Clips), else next to the .blend.
  Command line: blender file.blend --background --python momentum_clip_export.py -- --name cane_slash_1
                --out "C:/.../Assets/Resources/Clips" [--loop] [--rig Gauntlets] [--start 0] [--end 48]

WHAT IS WRITTEN
  - every bone (empty) whose local pose changes over the range (HandR/HandL always): rotation 4 per frame (Unity
    x y z w) relative to its parent bone, and position 3 per frame only when the position itself moves
  - the arm rig's fingers, per hand: 5 curls per frame (index, middle, ring, pinky, thumb), 0 straight … 1 closed,
    from the phalanx pivots' fold (MCP+PIP+DIP over 240°; the thumb's two joints over 95°)
  - events: timeline markers in the range (the name is the event) and pose markers of the root's action
  - fps (the scene's; author at 60), frames (= end − start + 1), loop
AXES  Blender (x, y, z) -> Unity (x, z, y); quaternion Blender (w, x, y, z) -> Unity (-x, -z, -y, w).
"""
import bpy, json, sys, os, math

CLIP_NAME = ""     # empty = the scene property momentum_clip
OUT_DIR = ""       # empty = the scene property momentum_out, else beside the .blend
LOOP = None        # None = the scene property momentum_loop
RIG_ROOT = ""      # empty = the selected object's rig, else the first rig in the scene
START, END = None, None   # None = the scene's frame range

ROUND = 5
MOVE_EPS = 1e-5
TURN_EPS = 1e-4
FINGER_FULL = 240.0   # MCP + PIP + DIP of a closed fist, degrees (ArmRigFingers' tables: 80 + 100 + 60)
THUMB_FULL = 95.0     # the thumb's base pitch + tip curl of a closed fist (25 + 70)


def b2u_vec(v):
    return [round(v.x, ROUND), round(v.z, ROUND), round(v.y, ROUND)]


def b2u_quat(q):
    return [round(-q.x, ROUND), round(-q.z, ROUND), round(-q.y, ROUND), round(q.w, ROUND)]


def short(name):
    return name.split(".")[-1]


def find_root(name):
    if name:
        o = bpy.data.objects.get(name)
        if o is not None:
            return o
    o = bpy.context.active_object
    while o is not None:
        if "momentum_rig" in o:
            return o
        o = o.parent
    for o in bpy.context.scene.objects:
        if "momentum_rig" in o:
            return o
    return None


def bones_under(root):
    out = []

    def walk(o):
        for c in o.children:
            if "momentum_bone" in c:
                out.append(c)
                walk(c)
    walk(root)
    return out


def local_matrix(o):
    p = o.parent
    return o.matrix_world.copy() if p is None else (p.matrix_world.inverted() @ o.matrix_world)


def flex_deg(o):
    """A joint's fold about its local X in the game's sense (positive toward the palm). A Unity turn about X is
    the negative of Blender's under the axis swap."""
    loc, rot, sca = local_matrix(o).decompose()
    return -math.degrees(rot.to_euler('XYZ').x)


def hand_fingers(root, side):
    hand = None
    for b in bones_under(root):
        if short(b.name) == "Hand" + side:
            hand = b
            break
    if hand is None:
        return None
    joints = {short(b.name): b for b in bones_under(hand)}
    fingers = [[joints.get("F%d%d" % (i, j)) for j in range(3)] for i in range(4)]
    thumb = [joints.get("T0"), joints.get("T1")]
    if all(j is None for f in fingers for j in f):
        return None
    return fingers, thumb


def curls(fingers, thumb):
    out = []
    for f in fingers:
        deg = sum(flex_deg(j) for j in f if j is not None)
        out.append(round(max(0.0, min(1.0, deg / FINGER_FULL)), 3))
    tdeg = sum(flex_deg(j) for j in thumb if j is not None)
    out.append(round(max(0.0, min(1.0, tdeg / THUMB_FULL)), 3))
    return out


def export(root, name, out_dir, loop, start, end):
    scene = bpy.context.scene
    fps = scene.render.fps / scene.render.fps_base
    if abs(fps - 60.0) > 0.01:
        print("MOMENTUM: the scene runs at %.2f fps; the contract is 60 (the game resamples, the hit-frame table assumes 60)" % fps)
    frames = end - start + 1
    if frames < 1:
        raise ValueError("empty frame range")
    bones = bones_under(root)
    data = {b.name: ([], []) for b in bones}
    fingers = {}
    for side in ("R", "L"):
        hf = hand_fingers(root, side)
        if hf:
            fingers[side] = (hf, [])
    current = scene.frame_current
    for f in range(start, end + 1):
        scene.frame_set(f)
        for b in bones:
            loc, rot, sca = local_matrix(b).decompose()
            pl, rl = data[b.name]
            q = b2u_quat(rot)
            if rl and (rl[-1][0] * q[0] + rl[-1][1] * q[1] + rl[-1][2] * q[2] + rl[-1][3] * q[3]) < 0.0:
                q = [-x for x in q]   # keep the shortest arc between frames
            pl.append(b2u_vec(loc))
            rl.append(q)
        for side, (hf, cl) in fingers.items():
            cl.append(curls(*hf))
    scene.frame_set(current)

    out_bones = []
    for b in bones:
        pl, rl = data[b.name]
        pos_moved = any(abs(pl[i][k] - pl[0][k]) > MOVE_EPS for i in range(frames) for k in range(3))
        rot_moved = any(abs(rl[i][k] - rl[0][k]) > TURN_EPS for i in range(frames) for k in range(4))
        is_hand = short(b.name) in ("HandR", "HandL")
        keyed = b.animation_data is not None and b.animation_data.action is not None
        if not (pos_moved or rot_moved or keyed or is_hand):
            continue
        entry = {"name": b.name, "rot": [x for q in rl for x in q]}
        entry["pos"] = [x for p in pl for x in p] if (pos_moved or is_hand) else []
        out_bones.append(entry)
    out_fingers = [{"hand": side, "curl": [x for c in cl for x in c]} for side, (hf, cl) in fingers.items()]
    events = []
    for m in scene.timeline_markers:
        if start <= m.frame <= end:
            events.append({"frame": m.frame - start, "name": m.name})
    if root.animation_data and root.animation_data.action:
        for m in root.animation_data.action.pose_markers:
            if start <= m.frame <= end:
                events.append({"frame": m.frame - start, "name": m.name})
    events.sort(key=lambda e: e["frame"])

    clip = {
        "name": name,
        "rig": str(root.get("momentum_rig", "arms")),
        "fps": int(round(fps)),
        "frames": frames,
        "loop": bool(loop),
        "speed": 1.0,
        "blendIn": float(scene.get("momentum_blend_in", 0.1)),
        "blendOut": float(scene.get("momentum_blend_out", 0.1)),
        "layerScale": float(scene.get("momentum_layer_scale", 1.0)),
        "snap": bool(scene.get("momentum_snap", False)),
        "weaponHand": str(scene.get("momentum_weapon_hand", "R")),
        "bones": out_bones,
        "fingers": out_fingers,
        "events": events,
    }
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, name + ".json")
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(clip, fh, separators=(",", ":"))
    print("MOMENTUM: wrote %s — %d frames at %d fps, %d bones, %d finger tracks, %d events, loop=%s" %
          (path, frames, clip["fps"], len(out_bones), len(out_fingers), len(events), clip["loop"]))
    return path


def main():
    name, out_dir, loop, rig, start, end = CLIP_NAME, OUT_DIR, LOOP, RIG_ROOT, START, END
    argv = sys.argv
    if "--" in argv:
        import argparse
        ap = argparse.ArgumentParser()
        ap.add_argument("--name")
        ap.add_argument("--out")
        ap.add_argument("--loop", action="store_true", default=None)
        ap.add_argument("--rig")
        ap.add_argument("--start", type=int)
        ap.add_argument("--end", type=int)
        a = ap.parse_args(argv[argv.index("--") + 1:])
        name, out_dir, rig = a.name or name, a.out or out_dir, a.rig or rig
        loop = a.loop if a.loop is not None else loop
        start, end = (a.start if a.start is not None else start), (a.end if a.end is not None else end)
    scene = bpy.context.scene
    root = find_root(rig)
    if root is None:
        raise RuntimeError("no MOMENTUM rig in the scene: run momentum_rig_import.py first")
    name = name or str(scene.get("momentum_clip", "")) or root.get("momentum_rig", "clip") + "_clip"
    out_dir = out_dir or str(scene.get("momentum_out", "")) or os.path.dirname(bpy.data.filepath or os.getcwd())
    if loop is None:
        loop = bool(scene.get("momentum_loop", False))
    start = scene.frame_start if start is None else start
    end = scene.frame_end if end is None else end
    export(root, name, out_dir, loop, start, end)


if __name__ == "__main__":
    main()
