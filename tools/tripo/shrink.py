# shrink a Tripo GLB to game size: decimate, textures to 1024, export GLB, render front/side/3-4 previews
import bpy, sys, math, os
src, out, tris, prev = sys.argv[-4], sys.argv[-3], int(sys.argv[-2]), sys.argv[-1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
obs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
for o in obs:
    n = len(o.data.polygons)
    m = o.modifiers.new('dec', 'DECIMATE'); m.ratio = min(1.0, tris / max(1, n))
    bpy.context.view_layer.objects.active = o; bpy.ops.object.modifier_apply(modifier='dec')
    print('tris', n, '->', sum(len(p.vertices) - 2 for p in o.data.polygons))
for im in bpy.data.images:
    if im.size[0] > 1024:
        s = 1024 if ('normal' in im.name.lower() or im.size[0] <= 4096) else 1024
        im.scale(s, s)
    im.file_format = 'JPEG' if im.file_format == 'JPEG' else im.file_format
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', export_image_format='AUTO', export_jpeg_quality=88, export_apply=True)
print('size', os.path.getsize(out))
# previews
sc = bpy.context.scene; sc.render.engine = 'CYCLES'; sc.cycles.samples = 24; sc.cycles.device = 'CPU'
sc.render.resolution_x = 520; sc.render.resolution_y = 640; sc.render.film_transparent = False
w = bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True; w.node_tree.nodes['Background'].inputs[0].default_value = (0.82, 0.83, 0.86, 1); w.node_tree.nodes['Background'].inputs[1].default_value = 1.0
import mathutils
mn = mathutils.Vector((1e9,)*3); mx = mathutils.Vector((-1e9,)*3)
for o in obs:
    for c in o.bound_box:
        v = o.matrix_world @ mathutils.Vector(c); mn = mathutils.Vector(map(min, mn, v)); mx = mathutils.Vector(map(max, mx, v))
ctr = (mn + mx) / 2; size = max(mx - mn)
sun = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); sc.collection.objects.link(sun); sun.data.energy = 3.5; sun.rotation_euler = (math.radians(50), 0, math.radians(30))
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
cam.data.type = 'ORTHO'; cam.data.ortho_scale = size * 1.15
from PIL import Image
shots = []
for i, yaw in enumerate([0, 90, 35]):
    a = math.radians(yaw); d = size * 3
    cam.location = (ctr.x + d * math.sin(a), ctr.y - d * math.cos(a), ctr.z)
    cam.rotation_euler = (math.radians(90), 0, a)
    sc.render.filepath = f'/tmp/claude-0/tripo/_p{i}.png'; bpy.ops.render.render(write_still=True); shots.append(sc.render.filepath)
ims = [Image.open(p) for p in shots]; s = Image.new('RGB', (520 * 3, 640)); [s.paste(im, (k * 520, 0)) for k, im in enumerate(ims)]; s.save(prev, quality=88)
