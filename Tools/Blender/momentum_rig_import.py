"""
MOMENTUM rig import for Blender 3.x / 4.x.

Builds a game rig from Tools/Animation/rig_<name>.json (written by Unity: Momentum > Animation > Export Rig
Proxies) as a hierarchy of EMPTIES — exactly the game's pivots, same names, same proportions — with the proxy
primitives parented to them, so what you animate is what the game drives. Animate the empties; export with
momentum_clip_export.py. See Tools/Animation/ANIMATOR_GUIDE.md.

USAGE
  In Blender: Scripting workspace > Text > Open this file > set RIG_JSON below (or leave it empty and a file
  dialog opens) > Run Script (Alt+P).
  Command line: blender --python momentum_rig_import.py -- "C:\path\to\rig_arms.json"

AXES  Unity (x right, y up, z forward, left-handed) <-> Blender (x right, z up, y forward):
      position (x, y, z) -> (x, z, y); quaternion (x, y, z, w) -> Blender (w, -x, -z, -y); scale (x, y, z) -> (x, z, y).
      The same swap is applied to every local transform, so the hierarchy comes out upright and right-handed with
      the game's forward along Blender +Y. Never rotate the root to "fix" anything: the export converts back.
"""
import bpy, bmesh, json, sys, os, math
from mathutils import Vector, Quaternion, Matrix

RIG_JSON = ""   # e.g. r"C:\Users\D\MovementShooter\Tools\Animation\rig_arms.json"

MESH_CACHE = {}
MAT_CACHE = {}


def u2b_vec(v):
    return Vector((v["x"], v["z"], v["y"]))


def u2b_quat(q):
    return Quaternion((q["w"], -q["x"], -q["z"], -q["y"]))


def u2b_scale(v):
    return Vector((v["x"], v["z"], v["y"]))


def material_for(color):
    key = (round(color["r"], 2), round(color["g"], 2), round(color["b"], 2))
    if key in MAT_CACHE:
        return MAT_CACHE[key]
    mat = bpy.data.materials.new("Proxy %.2f %.2f %.2f" % key)
    rgba = (min(1.0, color["r"]), min(1.0, color["g"]), min(1.0, color["b"]), 1.0)
    mat.diffuse_color = rgba
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = rgba
    MAT_CACHE[key] = mat
    return mat


def mesh_for(ptype):
    """Unity's primitives at scale one: Cube 1x1x1, Sphere d=1, Cylinder r=0.5 h=2 along Y, Capsule r=0.5 h=2 along Y,
    Quad 1x1 in XY. Unity's Y is Blender's Z here, so the cylinder and capsule run along Z and the quad lies in XZ."""
    if ptype in MESH_CACHE:
        return MESH_CACHE[ptype]
    bm = bmesh.new()
    if ptype == "Sphere":
        bmesh.ops.create_uvsphere(bm, u_segments=24, v_segments=12, radius=0.5)
    elif ptype == "Cylinder":
        bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=24, radius1=0.5, radius2=0.5, depth=2.0)
    elif ptype == "Capsule":
        bmesh.ops.create_cone(bm, cap_ends=False, cap_tris=False, segments=24, radius1=0.5, radius2=0.5, depth=1.0)
        bmesh.ops.create_uvsphere(bm, u_segments=24, v_segments=12, radius=0.5, matrix=Matrix.Translation((0.0, 0.0, 0.5)))
        bmesh.ops.create_uvsphere(bm, u_segments=24, v_segments=12, radius=0.5, matrix=Matrix.Translation((0.0, 0.0, -0.5)))
    elif ptype == "Quad":
        bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=0.5)
        bmesh.ops.rotate(bm, verts=bm.verts, cent=(0.0, 0.0, 0.0), matrix=Matrix.Rotation(math.radians(90.0), 3, 'X'))
    else:
        bmesh.ops.create_cube(bm, size=1.0)
    me = bpy.data.meshes.new("Proxy_" + ptype)
    bm.to_mesh(me)
    bm.free()
    me.materials.append(None)   # one slot, filled per object
    MESH_CACHE[ptype] = me
    return me


def place(obj, parent, pos, rot, scale):
    obj.parent = parent
    obj.matrix_parent_inverse = Matrix.Identity(4)
    obj.location = u2b_vec(pos)
    obj.rotation_mode = 'QUATERNION'
    obj.rotation_quaternion = u2b_quat(rot)
    obj.scale = u2b_scale(scale)


def build(path):
    with open(path, "r", encoding="utf-8") as f:
        rig = json.load(f)
    name = rig.get("rig", "rig")
    is_arms = name == "arms"
    scene = bpy.context.scene
    scene.render.fps = 60
    scene.render.fps_base = 1.0
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0
    if "momentum_loop" not in scene:
        scene["momentum_loop"] = False
    if "momentum_clip" not in scene:
        scene["momentum_clip"] = ("knuckles" if is_arms else name) + "_idle"
    scene["momentum_out"] = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(path)), "..", "..", "Assets", "Resources", "Clips"))

    coll = bpy.data.collections.new("Rig " + name)
    scene.collection.children.link(coll)
    bones = {}
    root = None
    size = 0.04 if is_arms else 0.12
    for b in rig["bones"]:
        empty = bpy.data.objects.new(b["name"], None)
        empty.empty_display_type = 'ARROWS'
        empty.empty_display_size = size
        empty["momentum_bone"] = 1
        empty["momentum_animatable"] = 1 if b.get("animatable", True) else 0
        coll.objects.link(empty)
        bones[b["name"]] = empty
        if root is None:
            root = empty
    for b in rig["bones"]:
        empty = bones[b["name"]]
        parent = bones.get(b.get("parent", ""), None)
        place(empty, parent, b["pos"], b["rot"], b["scale"])
        empty.rotation_mode = 'XYZ'   # friendlier keys; the export reads matrices, so any mode works
        empty.rotation_euler = u2b_quat(b["rot"]).to_euler('XYZ')   # a mode switch does not convert: set the euler too
        if not b.get("active", True):
            empty.hide_viewport = True
            empty.hide_render = True
    root["momentum_rig"] = name
    root["momentum_source"] = os.path.abspath(path)
    root.empty_display_type = 'PLAIN_AXES'
    root.empty_display_size = 0.2

    for p in rig.get("proxies", []):
        bone = bones.get(p["bone"], root)
        obj = bpy.data.objects.new(p.get("name", "Proxy"), mesh_for(p.get("type", "Cube")))
        obj["momentum_proxy"] = 1
        coll.objects.link(obj)
        place(obj, bone, p["pos"], p["rot"], p["scale"])
        if len(obj.material_slots):   # the mesh is shared per primitive type: the colour lives on the object's slot
            obj.material_slots[0].link = 'OBJECT'
            obj.material_slots[0].material = material_for(p["color"])
        obj.hide_select = True   # you animate the bones, never the shells
        if not p.get("active", True):
            obj.hide_viewport = True
            obj.hide_render = True

    if is_arms:
        # THE CAMERA is the root: the game's first-person view at its 58° vertical FOV. A Blender camera looks down
        # its own -Z, so it is turned to look along the rig's forward (+Y) with +Z up. It is a viewing device, not
        # a bone: never key it, the export skips it
        cam_data = bpy.data.cameras.new("FirstPersonCamera")
        cam_data.sensor_fit = 'VERTICAL'
        cam_data.angle_y = math.radians(58.0)
        cam_data.clip_start = 0.02
        cam_data.clip_end = 12.0
        cam = bpy.data.objects.new("FirstPersonCamera", cam_data)
        cam["momentum_camera"] = 1
        coll.objects.link(cam)
        cam.parent = root
        cam.matrix_parent_inverse = Matrix.Identity(4)
        cam.rotation_mode = 'XYZ'
        cam.rotation_euler = (math.radians(90.0), 0.0, 0.0)
        cam.hide_select = True
        scene.camera = cam
        scene.render.resolution_x = 1920
        scene.render.resolution_y = 1080
    scene.frame_start = 0
    scene.frame_end = 60
    scene.frame_set(0)
    for n in rig.get("notes", []):
        print("rig note:", n)
    print("MOMENTUM: built rig '%s' — %d bones, %d proxies, from %s" % (name, len(rig["bones"]), len(rig.get("proxies", [])), path))
    return root


class MOMENTUM_OT_import_rig(bpy.types.Operator):
    """Build a MOMENTUM rig from a rig_<name>.json"""
    bl_idname = "momentum.import_rig"
    bl_label = "Import MOMENTUM rig (.json)"
    filepath: bpy.props.StringProperty(subtype='FILE_PATH')
    filter_glob: bpy.props.StringProperty(default="*.json", options={'HIDDEN'})

    def execute(self, context):
        build(self.filepath)
        return {'FINISHED'}

    def invoke(self, context, event):
        context.window_manager.fileselect_add(self)
        return {'RUNNING_MODAL'}


def main():
    path = RIG_JSON
    argv = sys.argv
    if "--" in argv and len(argv) > argv.index("--") + 1:
        path = argv[argv.index("--") + 1]
    if path:
        build(path)
        return
    try:
        bpy.utils.register_class(MOMENTUM_OT_import_rig)
    except ValueError:
        pass
    bpy.ops.momentum.import_rig('INVOKE_DEFAULT')


if __name__ == "__main__":
    main()
