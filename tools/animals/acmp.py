# the same clip frames on two animal models, side by side (old low-poly above, detailed below)
import bpy, sys, math, mathutils
from PIL import Image
a_glb, b_glb, outp = sys.argv[-4], sys.argv[-3], sys.argv[-2]; clips = sys.argv[-1].split(',')
rows = []
for f in (a_glb, b_glb):
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=f)
    arm = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE'][0]
    if arm.animation_data is None: arm.animation_data_create()
    for tr in list(arm.animation_data.nla_tracks): arm.animation_data.nla_tracks.remove(tr)
    sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'; sc.display.shading.light = 'STUDIO'
    ms = [o for o in sc.objects if o.type == 'MESH']
    tex = any(n.type == 'TEX_IMAGE' for o in ms for m in o.data.materials if m and m.node_tree for n in m.node_tree.nodes)
    sc.display.shading.color_type = 'TEXTURE' if tex else 'VERTEX'
    w = bpy.data.worlds.new('w'); sc.world = w; w.color = (0.75, 0.76, 0.8)
    sc.render.resolution_x = 400; sc.render.resolution_y = 300
    dg = bpy.context.evaluated_depsgraph_get(); P = []
    for o in ms:
        e = o.evaluated_get(dg); P += [e.matrix_world @ v.co for v in e.data.vertices]
    lo = mathutils.Vector([min(p[i] for p in P) for i in range(3)]); hi = mathutils.Vector([max(p[i] for p in P) for i in range(3)]); c = (lo + hi) / 2; ext = max(hi - lo)
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = ext * 1.6
    ax = 0 if (hi - lo).x > (hi - lo).y else 1; side = mathutils.Vector((1, 0, 0)) if ax == 1 else mathutils.Vector((0, 1, 0))
    ims = []
    for cl in clips:
        nm, k = cl.split(':'); k = float(k)
        act = next(x for x in bpy.data.actions if x.name == nm or x.name.startswith(nm + '_') or x.name.endswith('|' + nm) or x.name.startswith(nm))
        arm.animation_data.action = act
        try: arm.animation_data.action_slot = act.slots[0]
        except Exception: pass
        sc.frame_set(int(act.frame_range[0] + (act.frame_range[1] - act.frame_range[0]) * k))
        cam.location = c + side * ext * 3 + mathutils.Vector((0, 0, ext * 0.2)); cam.rotation_euler = (c - cam.location).to_track_quat('-Z', 'Y').to_euler()
        p = '/tmp/claude-0/hd/_c.png'; sc.render.filepath = p; bpy.ops.render.render(write_still=True); ims.append(Image.open(p).convert('RGB').copy())
    rows.append(ims)
W = Image.new('RGB', (400 * len(clips), 600))
for r, ims in enumerate(rows):
    for i, im in enumerate(ims): W.paste(im, (400 * i, 300 * r))
W.save(outp, quality=85); print('ok')
