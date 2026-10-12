# a tool (axe / pickaxe) for the hand: joined, decimated, haft along +z (glTF +y) with the head at the top,
# the axe's blade (the heavier side of the head) toward +x, centred on the haft, its bottom end at 0; textures 1024
import bpy, sys, math, mathutils, os
import numpy as np
src, out, tris = sys.argv[-3], sys.argv[-2], int(sys.argv[-1])
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=src)
obs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
bpy.ops.object.select_all(action='DESELECT')
for o in obs: o.select_set(True)
bpy.context.view_layer.objects.active = obs[0]
if len(obs) > 1: bpy.ops.object.join()
o = bpy.context.view_layer.objects.active
bpy.ops.object.parent_clear(type='CLEAR_KEEP_TRANSFORM'); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
n = len(o.data.polygons)
if n > tris:
    m = o.modifiers.new('d', 'DECIMATE'); m.ratio = tris / n; bpy.ops.object.modifier_apply(modifier='d')
def V(): return np.array([v.co[:] for v in o.data.vertices])
v = V(); s = v.max(0) - v.min(0)
ax = int(np.argmax(s))
if ax == 0: o.data.transform(mathutils.Matrix.Rotation(math.radians(90), 4, 'Y'))
elif ax == 1: o.data.transform(mathutils.Matrix.Rotation(math.radians(90), 4, 'X'))
v = V(); zmin, zmax = v[:, 2].min(), v[:, 2].max(); L = zmax - zmin
def spread(sel): p = v[sel]; return (p[:, 0].max() - p[:, 0].min()) * (p[:, 1].max() - p[:, 1].min()) if len(p) else 0
if spread(v[:, 2] < zmin + L * 0.25) > spread(v[:, 2] > zmax - L * 0.25):
    o.data.transform(mathutils.Matrix.Rotation(math.radians(180), 4, 'X')); v = V()
zmin, zmax = v[:, 2].min(), v[:, 2].max(); L = zmax - zmin
# the haft: the middle of the lower half
low = v[(v[:, 2] > zmin + L * 0.15) & (v[:, 2] < zmin + L * 0.5)]
hx, hy = np.median(low[:, 0]), np.median(low[:, 1])
o.data.transform(mathutils.Matrix.Translation((-hx, -hy, -zmin))); v = V()
# the head: where it reaches furthest from the haft is the blade (or a pick's long point)
head = v[v[:, 2] > L * 0.7]
far = head[np.argmax(head[:, 0] ** 2 + head[:, 1] ** 2)]
ang = math.atan2(far[1], far[0])
o.data.transform(mathutils.Matrix.Rotation(math.pi - ang, 4, 'Z')); v = V()   # the blade toward -x: in the fist it faces the way the hand swings
for im in bpy.data.images:
    if im.size[0] > 1024: im.scale(1024, 1024)
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', export_image_format='JPEG', export_jpeg_quality=88)
print('AXE', out, n, len(o.data.polygons), tuple(round(x, 3) for x in (v.max(0) - v.min(0))), 'size', os.path.getsize(out))
