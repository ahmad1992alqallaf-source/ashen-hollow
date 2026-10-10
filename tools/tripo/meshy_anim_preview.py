import bpy, sys, math, mathutils
from PIL import Image
f, outp = sys.argv[-2], sys.argv[-1]
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=f)
arm = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE'][0]
sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'; sc.display.shading.light = 'STUDIO'; sc.display.shading.color_type = 'TEXTURE'
sc.render.resolution_x = 300; sc.render.resolution_y = 400
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
cam.location = (0.0, -4.2, 1.0); cam.rotation_euler = (math.radians(88), 0, 0); cam.data.lens = 40
ims = []
acts = [a for a in bpy.data.actions]
print('ACTIONS', [a.name for a in acts])
if arm.animation_data is None: arm.animation_data_create()
for tr in list(arm.animation_data.nla_tracks): arm.animation_data.nla_tracks.remove(tr)
for a in acts:
    arm.animation_data.action = a
    for k in (0.3, 0.7):
        fr = a.frame_range[0] + (a.frame_range[1] - a.frame_range[0]) * k; sc.frame_set(int(fr))
        p = '/tmp/claude-0/mrig/_r.png'; sc.render.filepath = p; bpy.ops.render.render(write_still=True); ims.append(Image.open(p).convert('RGB').copy())
W = Image.new('RGB', (300 * len(ims), 400))
for i, im in enumerate(ims): W.paste(im, (300 * i, 0))
W.save(outp)
