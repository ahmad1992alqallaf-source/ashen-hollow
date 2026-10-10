import bpy, sys, math, mathutils
f, outp = sys.argv[-2], sys.argv[-1]
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=f)
for o in bpy.context.scene.objects:
    if o.type != 'MESH': continue
    me = o.data; names = {g.index: g.name for g in o.vertex_groups}
    ca = me.color_attributes.new('wv', 'BYTE_COLOR', 'POINT')
    for v in me.vertices:
        tw = sum(g.weight for g in v.groups if names[g.group].lower().startswith('tail'))
        lw = sum(g.weight for g in v.groups if 'Leg' in names[g.group])
        ca.data[v.index].color = (tw, lw, 0.2, 1)
    me.color_attributes.active_color = ca
sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'; sc.display.shading.light = 'FLAT'; sc.display.shading.color_type = 'VERTEX'
sc.render.resolution_x = 700; sc.render.resolution_y = 500
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
cam.data.type = 'ORTHO'; cam.data.ortho_scale = 2.6
cam.location = (6, 0, 0.9); cam.rotation_euler = (math.radians(90), 0, math.radians(90))
sc.render.filepath = outp; bpy.ops.render.render(write_still=True)
