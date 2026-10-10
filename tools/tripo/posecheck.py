# render a rigged suit glb: bind pose front/back, a stride from the side, 3/4 behind
import bpy, sys, math, mathutils
src, prev, TMP = sys.argv[-3], sys.argv[-2], sys.argv[-1]
import os; os.makedirs(TMP, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=src)
arm = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE'][0]
def aim(name, d):
    pb = arm.pose.bones[name]; bpy.context.view_layer.update()
    cur = (pb.tail - pb.head).normalized(); d = mathutils.Vector(d).normalized()
    R = cur.rotation_difference(d).to_matrix().to_4x4(); T = mathutils.Matrix.Translation(pb.head)
    pb.matrix = T @ R @ T.inverted() @ pb.matrix; bpy.context.view_layer.update()
def bend(nm, deg):
    pb = arm.pose.bones[nm]; bpy.context.view_layer.update()
    T = mathutils.Matrix.Translation(pb.head); R = mathutils.Matrix.Rotation(math.radians(deg), 4, 'X')
    pb.matrix = T @ R @ T.inverted() @ pb.matrix; bpy.context.view_layer.update()
sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'
sh = sc.display.shading; sh.light = 'STUDIO'; sh.color_type = 'TEXTURE'
w = bpy.data.worlds.new('w'); sc.world = w; w.color = (0.75, 0.76, 0.8)
sc.render.resolution_x = 400; sc.render.resolution_y = 480
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
cam.data.type = 'ORTHO'; cam.data.ortho_scale = 2.3
from PIL import Image
shots = []
def shot(yaw, i):
    a_ = math.radians(yaw); d = 6
    cam.location = (d * math.sin(a_), -d * math.cos(a_), 1.0); cam.rotation_euler = (math.radians(90), 0, a_)
    sc.render.filepath = TMP + f'/_c{i}.png'; bpy.ops.render.render(write_still=True); shots.append(sc.render.filepath)
bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode='POSE')
for side, s_ in (('l', 1), ('r', -1)):
    aim('upperarm_' + side, (s_ * 0.25, 0.0, -1.0)); aim('lowerarm_' + side, (s_ * 0.15, -0.15, -1.0))
shot(0, 0); shot(180, 1)
# walking stride: left leg forward, right leg back with the knee bent (foot lifted behind)
bend('thigh_l', -28); bend('thigh_r', 18); bend('calf_r', 40); bend('calf_l', 8)
aim('upperarm_l', (0.25, 0.35, -1.0)); aim('upperarm_r', (-0.25, -0.35, -1.0))
shot(90, 2); shot(215, 3)
ims = [Image.open(p).convert('RGB') for p in shots]; s_ = Image.new('RGB', (400 * 4, 480)); [s_.paste(im, (k * 400, 0)) for k, im in enumerate(ims)]; s_.save(prev, quality=88)
