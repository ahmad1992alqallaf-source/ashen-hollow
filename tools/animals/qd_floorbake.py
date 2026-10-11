# the dungeon floor's stone pattern, seen from straight above, as a grey tile for the ground shader's paving map
import bpy, sys, math
src, out = sys.argv[-2], sys.argv[-1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
sc = bpy.context.scene; sc.render.engine = 'CYCLES'; sc.cycles.samples = 32; sc.render.resolution_x = sc.render.resolution_y = 512
w = bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True; w.node_tree.nodes['Background'].inputs[0].default_value = (1, 1, 1, 1); w.node_tree.nodes['Background'].inputs[1].default_value = 0.6
for m in bpy.data.materials:
    b = next((x for x in m.node_tree.nodes if x.type == 'BSDF_PRINCIPLED'), None) if m.use_nodes else None
    if b: b.inputs['Base Color'].default_value = (0.8, 0.8, 0.8, 1)   # grey: the shape alone, the colour comes from the ground
sun = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); sc.collection.objects.link(sun); sun.data.energy = 3; sun.rotation_euler = (math.radians(25), 0, math.radians(35))
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
cam.data.type = 'ORTHO'; cam.data.ortho_scale = 2.0; cam.location = (0, 0, 5); cam.rotation_euler = (0, 0, 0)
sc.view_settings.view_transform = 'Standard'
sc.render.filepath = out; bpy.ops.render.render(write_still=True)
