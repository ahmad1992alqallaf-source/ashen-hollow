import bpy, sys, math
from PIL import Image
f, outp, clip, k, yaw = sys.argv[-5], sys.argv[-4], sys.argv[-3], float(sys.argv[-2]), float(sys.argv[-1])
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=f)
arm = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE'][0]
act = [a for a in bpy.data.actions if a.name.startswith(clip)][0]
if arm.animation_data is None: arm.animation_data_create()
for tr in list(arm.animation_data.nla_tracks): arm.animation_data.nla_tracks.remove(tr)
arm.animation_data.action = act
try: arm.animation_data.action_slot = act.slots[0]
except Exception: pass
sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'; sc.display.shading.light = 'STUDIO'; sc.display.shading.color_type = 'TEXTURE'
w = bpy.data.worlds.new('w'); sc.world = w; w.color = (0.75, 0.76, 0.8)
sc.render.resolution_x = 640; sc.render.resolution_y = 760
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
cam.data.type = 'ORTHO'; cam.data.ortho_scale = 2.1
f0, f1 = act.frame_range; sc.frame_set(int(f0 + (f1 - f0) * k))
a_ = math.radians(yaw); cam.location = (6 * math.sin(a_), -6 * math.cos(a_), 0.95); cam.rotation_euler = (math.radians(90), 0, a_)
sc.render.filepath = outp; bpy.ops.render.render(write_still=True)
