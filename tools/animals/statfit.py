# a detailed model in place of a simple unrigged one (pets built from spheres): turned the same way (Meshy keeps the
# reference picture's facing), scaled to the old one's height, standing on the same spot; exported with its textures
import bpy, sys, mathutils, numpy as np
base, hd, out, tris = sys.argv[-4], sys.argv[-3], sys.argv[-2], int(sys.argv[-1])
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=base)
P = []
for o in bpy.context.scene.objects:
    if o.type == 'MESH': P += [tuple(o.matrix_world @ v.co) for v in o.data.vertices]
P = np.array(P); lo, hi = P.min(0), P.max(0)
for o in list(bpy.context.scene.objects): bpy.data.objects.remove(o)
bpy.ops.import_scene.gltf(filepath=hd)
ms = [o for o in bpy.context.scene.objects if o.type == 'MESH']
bpy.ops.object.select_all(action='DESELECT')
for o in ms: o.select_set(True)
bpy.context.view_layer.objects.active = ms[0]
if len(ms) > 1: bpy.ops.object.join()
t = bpy.context.view_layer.objects.active
mw = t.matrix_world.copy(); t.parent = None; t.matrix_world = mw
for o in list(bpy.context.scene.objects):
    if o != t: bpy.data.objects.remove(o)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
n = len(t.data.polygons)
if n > tris:
    d = t.modifiers.new('d', 'DECIMATE'); d.ratio = tris / n; bpy.ops.object.modifier_apply(modifier='d')
Q = np.array([tuple(v.co) for v in t.data.vertices]); ql, qh = Q.min(0), Q.max(0)
s = (hi[2] - lo[2]) / max(1e-6, qh[2] - ql[2])
c_old = (lo + hi) / 2; c_new = (ql + qh) / 2
for v in t.data.vertices:
    p = (np.array(v.co) - [c_new[0], c_new[1], ql[2]]) * s + [c_old[0], c_old[1], lo[2]]
    v.co = mathutils.Vector(p)
t.data.update(); t.name = 'Pet'
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', export_image_format='JPEG', export_jpeg_quality=88)
print('fitted', out, 'scale', round(float(s), 4), 'tris', len(t.data.polygons), 'old size', np.round(hi - lo, 3), 'new size', np.round((qh - ql) * s, 3))
