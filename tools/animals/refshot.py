# a clean reference picture of an animal model in its rest pose (3/4 front and side), for the concept painter
import bpy, sys, math, mathutils
from PIL import Image
src, outp = sys.argv[-2], sys.argv[-1]
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=src)
arm = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE']
if arm and arm[0].animation_data: arm[0].animation_data.action = None
for a in list(bpy.data.actions): bpy.data.actions.remove(a)
for ob in bpy.context.scene.objects:
    if ob.type == 'ARMATURE':
        for pb in ob.pose.bones: pb.matrix_basis = mathutils.Matrix.Identity(4)
bpy.context.view_layer.update()
for _o in list(bpy.context.scene.objects):
    if _o.type == 'MESH' and _o.name.startswith('Icosphere'): bpy.data.objects.remove(_o)
ms = [o for o in bpy.context.scene.objects if o.type == 'MESH']
dg = bpy.context.evaluated_depsgraph_get(); P = []
for o in ms:
    e = o.evaluated_get(dg); P += [e.matrix_world @ v.co for v in e.data.vertices]
lo = mathutils.Vector([min(p[i] for p in P) for i in range(3)]); hi = mathutils.Vector([max(p[i] for p in P) for i in range(3)])
c = (lo + hi) / 2; ext = max(hi - lo)
sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'; sh = sc.display.shading; sh.light = 'STUDIO'
_tex = any(n.type == 'TEX_IMAGE' for o in ms for m in o.data.materials if m and m.node_tree for n in m.node_tree.nodes)
sh.color_type = 'TEXTURE' if _tex else ('VERTEX' if any(o.data.color_attributes for o in ms) else 'MATERIAL')
sh.show_cavity = True
sc.render.resolution_x = 1024; sc.render.resolution_y = 1024
w = bpy.data.worlds.new('w'); sc.world = w; w.color = (0.86, 0.86, 0.87)
sc.view_settings.view_transform = 'Standard'
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
cam.data.type = 'ORTHO'; cam.data.ortho_scale = ext * 1.0; cam.data.clip_end = ext * 20
# which way does it face? toward its head bone
hb = None
for ob in bpy.context.scene.objects:
    if ob.type == 'ARMATURE':
        for b in ob.data.bones:
            if b.name.lower().startswith('head'): hb = ob.matrix_world @ b.head_local; break
ax = 0 if (hi - lo).x > (hi - lo).y else 1
fw = 1 if hb is None or hb[ax] > c[ax] else -1
print('forward axis', 'xy'[ax], fw)
fwd = mathutils.Vector((0, 0, 0)); fwd[ax] = fw
side = mathutils.Vector((-fwd.y, fwd.x, 0))
for i, (k, yawdeg) in enumerate((('q', 40), ('s', 90))):
    d = (fwd * math.cos(math.radians(yawdeg)) + side * math.sin(math.radians(yawdeg))).normalized()
    cam.location = c + d * ext * 3 + mathutils.Vector((0, 0, ext * (0.35 if k == 'q' else 0.05)))
    cam.rotation_euler = (c - cam.location).to_track_quat('-Z', 'Y').to_euler()
    sc.render.filepath = outp + '_' + k + '.png'; bpy.ops.render.render(write_still=True)
print('ok')
