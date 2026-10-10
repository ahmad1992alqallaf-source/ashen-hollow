import bpy, sys, math, colorsys
from PIL import Image
f, outp = sys.argv[-2], sys.argv[-1]
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=f)
COL = {'pelvis': (0.9, 0.9, 0.2), 'thigh_l': (0.2, 0.4, 1), 'thigh_r': (1, 0.3, 0.2), 'calf_l': (0.1, 0.9, 1), 'calf_r': (1, 0.6, 0.1), 'foot_l': (0, 0, 0.4), 'foot_r': (0.4, 0, 0)}
for o in bpy.context.scene.objects:
    if o.type != 'MESH': continue
    me = o.data; names = {g.index: g.name for g in o.vertex_groups}
    ca = me.color_attributes.new('wv', 'BYTE_COLOR', 'POINT')
    for v in me.vertices:
        c = [0.0, 0.0, 0.0]; tot = 0
        for g in v.groups:
            col = COL.get(names[g.group], (0.6, 0.6, 0.6)); c = [c[i] + col[i] * g.weight for i in range(3)]; tot += g.weight
        if tot > 0: c = [x / tot for x in c]
        ca.data[v.index].color = (c[0], c[1], c[2], 1)
    me.color_attributes.active_color = ca
sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'; sc.display.shading.light = 'FLAT'; sc.display.shading.color_type = 'VERTEX'
sc.render.resolution_x = 360; sc.render.resolution_y = 480
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
cam.data.type = 'ORTHO'; cam.data.ortho_scale = 2.2
ims = []
for yaw in (0, 90, 180):
    a_ = math.radians(yaw); cam.location = (6 * math.sin(a_), -6 * math.cos(a_), 0.95); cam.rotation_euler = (math.radians(90), 0, a_)
    p = '/tmp/claude-0/runchk/_w.png'; sc.render.filepath = p; bpy.ops.render.render(write_still=True); ims.append(Image.open(p).convert('RGB').copy())
W = Image.new('RGB', (360 * 3, 480))
for i, im in enumerate(ims): W.paste(im, (360 * i, 0))
W.save(outp); print('ok')
