# crest wings (Tripo: a crest plate in the middle, a wing each side, one mesh) for the hero's back: decimated, the
# plate cut out as "Hub" and the right wing as "Wing" (the left is the right mirrored in the game), the wing's root at
# its origin and its span along +x normalised to 0.9 (AHWingRig's units), the plate scaled alike; textures 1024
# usage: blender python wing_prep.py -- in.glb out.glb tris hubfrac
import bpy, sys, os, bmesh, mathutils
import numpy as np
src, out, tris, hubf = sys.argv[-4], sys.argv[-3], int(sys.argv[-2]), float(sys.argv[-1])
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=src)
obs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
bpy.ops.object.select_all(action='DESELECT')
for o in obs: o.select_set(True)
bpy.context.view_layer.objects.active = obs[0]
if len(obs) > 1: bpy.ops.object.join()
o = bpy.context.view_layer.objects.active
bpy.ops.object.parent_clear(type='CLEAR_KEEP_TRANSFORM'); bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
n0 = len(o.data.polygons)
if n0 > tris:
    m = o.modifiers.new('d', 'DECIMATE'); m.ratio = tris / n0; bpy.ops.object.modifier_apply(modifier='d')
v = np.array([x.co[:] for x in o.data.vertices]); xmin, xmax = v[:, 0].min(), v[:, 0].max()
cx = (xmin + xmax) / 2; half = (xmax - xmin) / 2; h = half * hubf
def part(name, keep):
    c = o.copy(); c.data = o.data.copy(); c.name = name; c.data.name = name; bpy.context.scene.collection.objects.link(c)
    bm = bmesh.new(); bm.from_mesh(c.data)
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if not keep(f.calc_center_median().x - cx)], context='FACES')
    bmesh.ops.delete(bm, geom=[e for e in bm.verts if not e.link_faces], context='VERTS')
    bm.to_mesh(c.data); bm.free(); return c
hub = part('Hub', lambda x: abs(x) < h)
wing = part('Wing', lambda x: x >= h)
bpy.data.objects.remove(o)
# the wing's root: the middle of its first sliver
wv = np.array([x.co[:] for x in wing.data.vertices]); root = wv[wv[:, 0] < cx + h + half * 0.06]
if len(root) < 10: root = wv[np.argsort(wv[:, 0])[:50]]
r = mathutils.Vector((cx + h, float(np.median(root[:, 1])), float(np.median(root[:, 2]))))
span = wv[:, 0].max() - r.x; k = 0.9 / span
hv = np.array([x.co[:] for x in hub.data.vertices]); hc = mathutils.Vector(((hv.min(0) + hv.max(0)) / 2).tolist())
for ob, at in ((wing, r), (hub, hc)):
    ob.data.transform(mathutils.Matrix.Translation(-at)); ob.data.transform(mathutils.Matrix.Scale(k, 4))
# where the plate sits from the wing's root (in the same units), for the game to place them
off = (hc - r) * k
# the sweep: seen from above the wing runs back from the root at an angle; turned flat onto +x (the game sweeps it back
# again at the shoulder, so the flap turns about the wing's own plane)
wv = np.array([x.co[:] for x in wing.data.vertices]); far = wv[wv[:, 0] > 0.45]
d = np.polyfit(far[:, 0], far[:, 1], 1)[0]; import math; ang = math.atan(d)
wing.data.transform(mathutils.Matrix.Rotation(-ang, 4, 'Z'))
wv = np.array([x.co[:] for x in wing.data.vertices]); k2 = 0.9 / wv[:, 0].max()
for ob in (wing, hub): ob.data.transform(mathutils.Matrix.Scale(k2, 4))
off = off * k2
# a few come out with the tips curling forward (toward the side the crest does not face): the wing is mirrored front to
# back so it sweeps the same way as the crest faces, as the others do
if ang > 0:
    # (the winding is left as it is, so the side that faced the crest's way still does: single-sided feathers show from
    # behind the hero; the normals are worked out afresh from it)
    wing.data.transform(mathutils.Matrix.Scale(-1, 4, (0, 1, 0))); ang = -ang
    bpy.context.view_layer.objects.active = wing
    if wing.data.has_custom_normals: bpy.ops.mesh.customdata_custom_splitnormals_clear()
print('SWEEP', round(math.degrees(ang), 1), 'rescale', round(k2, 3))
for im in bpy.data.images:
    if im.size[0] > 1024: im.scale(1024, 1024)
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', export_image_format='JPEG', export_jpeg_quality=88)
wb = np.array([x.co[:] for x in wing.data.vertices])
print('WING', os.path.basename(out), n0, 'hub', len(hub.data.polygons), 'wing', len(wing.data.polygons), 'hubOff(blender xyz)', tuple(round(x, 3) for x in off), 'wing bbox', tuple(round(x, 3) for x in wb.min(0)), tuple(round(x, 3) for x in wb.max(0)), 'size', os.path.getsize(out))
