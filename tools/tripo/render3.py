sc = bpy.context.scene; sc.render.engine = 'CYCLES'; sc.cycles.samples = 16; sc.render.resolution_x = 480; sc.render.resolution_y = 480
w = bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True; w.node_tree.nodes['Background'].inputs[0].default_value = (0.82, 0.83, 0.86, 1)
obs = [o for o in sc.objects if o.type == 'MESH']
mn = mathutils.Vector((1e9,)*3); mx = mathutils.Vector((-1e9,)*3)
for ob in obs:
    for c in ob.bound_box:
        v = ob.matrix_world @ mathutils.Vector(c); mn = mathutils.Vector(map(min, mn, v)); mx = mathutils.Vector(map(max, mx, v))
ctr = (mn + mx) / 2; size = max(mx - mn)
sun = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); sc.collection.objects.link(sun); sun.data.energy = 3.5; sun.rotation_euler = (math.radians(50), 0, math.radians(30))
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
cam.data.type = 'ORTHO'; cam.data.ortho_scale = size * 1.15
from PIL import Image
shots = []
for i, yaw in enumerate([0, 90, 180]):
    a = math.radians(yaw); d = size * 3
    cam.location = (ctr.x + d * math.sin(a), ctr.y - d * math.cos(a), ctr.z); cam.rotation_euler = (math.radians(90), 0, a)
    sc.render.filepath = f'/tmp/claude-0/tripo/_q{i}.png'; bpy.ops.render.render(write_still=True); shots.append(sc.render.filepath)
ims = [Image.open(p) for p in shots]; s = Image.new('RGB', (480 * 3, 480)); [s.paste(im, (k * 480, 0)) for k, im in enumerate(ims)]; s.save(prev, quality=88)
print('bbox', tuple(round(x,3) for x in mn), tuple(round(x,3) for x in mx))
