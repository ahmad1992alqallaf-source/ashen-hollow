# a weapon for the hand: decimated, stood upright (longest side up), point/head up, centred on the floor of its bounds
import bpy, sys, math, mathutils
src, out, tris, flip = sys.argv[-4], sys.argv[-3], int(sys.argv[-2]), sys.argv[-1] == '1'
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
def ext():
    vs = [v.co for v in o.data.vertices]
    mn = mathutils.Vector([min(v[i] for v in vs) for i in range(3)]); mx = mathutils.Vector([max(v[i] for v in vs) for i in range(3)]); return mn, mx
mn, mx = ext(); s = mx - mn
if s.x > s.z and s.x >= s.y: o.data.transform(mathutils.Matrix.Rotation(math.radians(90), 4, 'Y'))
elif s.y > s.z: o.data.transform(mathutils.Matrix.Rotation(math.radians(90), 4, 'X'))
if flip: o.data.transform(mathutils.Matrix.Rotation(math.radians(180), 4, 'X'))
mn, mx = ext(); c = (mn + mx) / 2
o.data.transform(mathutils.Matrix.Translation((-c.x, -c.y, -mn.z)))
for im in bpy.data.images:
    if im.size[0] > 1024: im.scale(1024, 1024)
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', export_image_format='JPEG', export_jpeg_quality=88)
print('WPN', out, n, len(o.data.polygons), tuple(round(x, 2) for x in (mx - mn)))
