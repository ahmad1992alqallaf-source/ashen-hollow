# shrink one incoming GLB (cap tris), textures to 1024, export, render 3 views + print bounds
import bpy, sys, math, os, mathutils, json
src, out, tris, prev, tmp = sys.argv[-5], sys.argv[-4], int(sys.argv[-3]), sys.argv[-2], sys.argv[-1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
obs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
tot = sum(len(o.data.polygons) for o in obs)
for o in obs:
    n = len(o.data.polygons)
    if tot > tris:
        m = o.modifiers.new('dec', 'DECIMATE'); m.ratio = tris / tot
        bpy.context.view_layer.objects.active = o; bpy.ops.object.modifier_apply(modifier='dec')
for im in bpy.data.images:
    if im.size[0] > 1024: im.scale(1024, 1024)
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', export_image_format='JPEG', export_jpeg_quality=88, export_apply=True)
mn = mathutils.Vector((1e9,)*3); mx = mathutils.Vector((-1e9,)*3)
for o in obs:
    for c in o.bound_box:
        v = o.matrix_world @ mathutils.Vector(c); mn = mathutils.Vector(map(min, mn, v)); mx = mathutils.Vector(map(max, mx, v))
ctr = (mn + mx) / 2; size = max(mx - mn)
print('INFO', json.dumps({'tris': tot, 'size': list(mx - mn), 'mb': os.path.getsize(out) / 1e6}))
sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'
sh = sc.display.shading; sh.light = 'STUDIO'; sh.color_type = 'TEXTURE'; sh.background_type = 'VIEWPORT'; sh.background_color = (0.82, 0.83, 0.86)
sc.render.resolution_x = 300; sc.render.resolution_y = 380
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
cam.data.type = 'ORTHO'; cam.data.ortho_scale = size * 1.1
from PIL import Image, ImageDraw
shots = []
for i, yaw in enumerate([0, 90, 180]):
    a = math.radians(yaw); d = size * 3
    cam.location = (ctr.x + d * math.sin(a), ctr.y - d * math.cos(a), ctr.z); cam.rotation_euler = (math.radians(90), 0, a)
    sc.render.filepath = f'{tmp}/_p{i}.png'; bpy.ops.render.render(write_still=True); shots.append(sc.render.filepath)
ims = [Image.open(p).convert('RGB') for p in shots]; s = Image.new('RGB', (900, 400), 'white'); [s.paste(im, (k * 300, 0)) for k, im in enumerate(ims)]
ImageDraw.Draw(s).text((4, 384), os.path.basename(src)[:60] + '  %d tris  %.2fx%.2fx%.2f' % (tot, *(mx - mn)), fill=(0, 0, 0)); s.save(prev, quality=85)
