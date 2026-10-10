import bpy, sys, math, mathutils
from PIL import Image
f, outp = sys.argv[-2], sys.argv[-1]
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=f)
ms = [o for o in bpy.context.scene.objects if o.type == 'MESH']
P = [o.matrix_world @ v.co for o in ms for v in o.data.vertices]
lo = mathutils.Vector([min(p[i] for p in P) for i in range(3)]); hi = mathutils.Vector([max(p[i] for p in P) for i in range(3)]); c = (lo + hi) / 2; ext = max(hi - lo)
sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'; sc.display.shading.light = 'STUDIO'; sc.display.shading.color_type = 'TEXTURE'
sc.render.resolution_x = 240; sc.render.resolution_y = 240
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
cam.data.type = 'ORTHO'; cam.data.ortho_scale = ext * 1.3; cam.data.clip_end = ext * 20
ims = []
# looking from -Y (front in Blender), +X, +Y, -X
for d in ((0, -1), (1, 0), (0, 1), (-1, 0)):
    cam.location = c + mathutils.Vector((d[0], d[1], 0.3)) * ext * 3; cam.rotation_euler = (c - cam.location).to_track_quat('-Z', 'Y').to_euler()
    sc.render.filepath = '/tmp/claude-0/hd/_y.png'; bpy.ops.render.render(write_still=True); ims.append(Image.open('/tmp/claude-0/hd/_y.png').convert('RGB').copy())
W = Image.new('RGB', (960, 240))
for i, im in enumerate(ims): W.paste(im, (240 * i, 0))
W.save(outp)
