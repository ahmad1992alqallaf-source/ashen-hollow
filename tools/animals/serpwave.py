# the serpent's code-driven swim (as AHSerpentRig: a side-to-side wave travelling down the body and tail, the neck
# swaying), on the old and the new body, seen from above and from the side
import bpy, sys, math, mathutils
from PIL import Image
files, outp = sys.argv[-2].split(','), sys.argv[-1]
rows = []
for f in files:
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=f)
    for _o in list(bpy.context.scene.objects):
        if _o.type == 'MESH' and _o.name.startswith('Icosphere'): bpy.data.objects.remove(_o)
    arm = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE'][0]
    sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'; sc.display.shading.light = 'STUDIO'; sc.display.shading.color_type = 'TEXTURE'
    w = bpy.data.worlds.new('w'); sc.world = w; w.color = (0.75, 0.76, 0.8)
    sc.render.resolution_x = 520; sc.render.resolution_y = 300
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    bones = sorted([pb for pb in arm.pose.bones if pb.name.lower().startswith(('tail', 'abdomen'))], key=lambda b: (0 if b.name.lower().startswith('abdomen') else 1, int(''.join(c for c in b.name.split('_')[0] if c.isdigit()) or 0)))
    necks = sorted([pb for pb in arm.pose.bones if pb.name.lower().startswith('neck')], key=lambda b: int(''.join(c for c in b.name.split('_')[0] if c.isdigit()) or 0))
    up = mathutils.Vector((0, 0, 1))
    ims = []
    for t in (0.0, 1.4):
        for pb in arm.pose.bones: pb.matrix_basis = mathutils.Matrix.Identity(4)
        bpy.context.view_layer.update()
        for i, pb in enumerate(bones):
            k = (i + 1) / len(bones); yaw = math.sin(t * 2.2 - i * 0.45) * (3 + 9 * k) * 1.4
            M = pb.matrix.copy(); T = mathutils.Matrix.Translation(M.to_translation())
            R = mathutils.Matrix.Rotation(math.radians(yaw), 4, (arm.matrix_world.inverted().to_3x3() @ up).normalized())
            pb.matrix = T @ R @ T.inverted() @ M; bpy.context.view_layer.update()
        for i, pb in enumerate(necks):
            yaw = math.sin(t * 1.3 + i * 0.3) * 8
            M = pb.matrix.copy(); T = mathutils.Matrix.Translation(M.to_translation())
            R = mathutils.Matrix.Rotation(math.radians(yaw), 4, (arm.matrix_world.inverted().to_3x3() @ up).normalized())
            pb.matrix = T @ R @ T.inverted() @ M; bpy.context.view_layer.update()
        dg = bpy.context.evaluated_depsgraph_get(); P = []
        for o in sc.objects:
            if o.type == 'MESH':
                e = o.evaluated_get(dg); P += [e.matrix_world @ v.co for v in e.data.vertices]
        lo = mathutils.Vector([min(p[i] for p in P) for i in range(3)]); hi = mathutils.Vector([max(p[i] for p in P) for i in range(3)]); c = (lo + hi) / 2; ext = max(hi - lo)
        cam.data.type = 'ORTHO'; cam.data.ortho_scale = ext * 1.05; cam.data.clip_end = ext * 20
        for view in ('top', 'side'):
            if view == 'top': cam.location = c + mathutils.Vector((0, 0, ext * 3)); cam.rotation_euler = (0, 0, math.radians(90))
            else: cam.location = c + mathutils.Vector((ext * 3, 0, ext * 0.3)); cam.rotation_euler = (c - cam.location).to_track_quat('-Z', 'Y').to_euler()
            p = '/tmp/claude-0/hd/_w.png'; sc.render.filepath = p; bpy.ops.render.render(write_still=True); ims.append(Image.open(p).convert('RGB').copy())
    rows.append(ims)
W = Image.new('RGB', (520 * 4, 300 * len(rows)))
for r, ims in enumerate(rows):
    for i, im in enumerate(ims): W.paste(im, (520 * i, 300 * r))
W.save(outp, quality=85); print('ok')
