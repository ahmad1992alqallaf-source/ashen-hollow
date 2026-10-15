# Ashen Hollow: rough stand-in models for the four new pets (Rock Golem, Night Prowler, Flame Hatchling, Spirit Wisp),
# shaped like the concept sheets, one joined mesh each like a Tripo export, facing -y. They exist only to test the
# skeletons and moves (tools/mounts/procanim.json pet_*) before the real models are made.
#   blender python: standins.py -- <out dir>
import bpy, sys, math, os
out = sys.argv[sys.argv.index('--') + 1]
def clear():
    bpy.ops.wm.read_factory_settings(use_empty=True)
def mat(name, col, emit=0.0):
    m = bpy.data.materials.new(name); m.use_nodes = True; b = m.node_tree.nodes['Principled BSDF']
    b.inputs['Base Color'].default_value = col + (1,)
    if emit: b.inputs['Emission Color'].default_value = col + (1,); b.inputs['Emission Strength'].default_value = emit
    return m
def part(kind, loc, scale=(1, 1, 1), rot=(0, 0, 0), m=None, **kw):
    if kind == 'sphere': bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=10, location=loc, rotation=rot)
    elif kind == 'cube': bpy.ops.mesh.primitive_cube_add(location=loc, rotation=rot)
    elif kind == 'cyl': bpy.ops.mesh.primitive_cylinder_add(vertices=10, location=loc, rotation=rot)
    elif kind == 'cone': bpy.ops.mesh.primitive_cone_add(vertices=10, location=loc, rotation=rot)
    o = bpy.context.active_object; o.scale = scale
    if m: o.data.materials.append(m)
    # subdivide so it bends when skinned
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.subdivide(number_cuts=kw.get('cuts', 3)); bpy.ops.object.mode_set(mode='OBJECT')
    return o
def seg(a, b, r, m, cuts=4):
    # a cylinder from point a to point b
    import mathutils
    a = mathutils.Vector(a); b = mathutils.Vector(b); d = b - a
    o = part('cyl', (a + b) / 2, (r, r, d.length / 2), (0, 0, 0), m, cuts=cuts)
    o.rotation_mode = 'QUATERNION'; o.rotation_quaternion = d.to_track_quat('Z', 'Y')
    return o
def save(name):
    objs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]; bpy.ops.object.transform_apply(location=False, rotation=True, scale=True); bpy.ops.object.join()
    bpy.context.active_object.name = name
    bpy.ops.export_scene.gltf(filepath=os.path.join(out, name + '.glb'), export_format='GLB')

# Rock Golem (about 1 unit tall): a big chest, long arms with heavy fists, short legs, crystals
clear(); st = mat('stone', (0.45, 0.42, 0.36)); cr = mat('crystal', (0.2, 0.5, 1.0), 3)
part('cube', (0, 0, 0.62), (0.26, 0.18, 0.2), m=st); part('cube', (0, 0, 0.42), (0.18, 0.14, 0.08), m=st)
part('sphere', (0, -0.05, 0.9), (0.12, 0.11, 0.1), m=st)
for sx in (1, -1):
    seg((sx * 0.3, 0, 0.8), (sx * 0.38, 0, 0.55), 0.09, st); seg((sx * 0.38, 0, 0.55), (sx * 0.4, -0.02, 0.32), 0.08, st)
    part('sphere', (sx * 0.4, -0.02, 0.26), (0.11, 0.11, 0.1), m=st)
    seg((sx * 0.13, 0, 0.4), (sx * 0.14, 0, 0.2), 0.08, st); seg((sx * 0.14, 0, 0.2), (sx * 0.14, 0, 0.05), 0.075, st)
    part('cube', (sx * 0.14, -0.04, 0.03), (0.08, 0.11, 0.03), m=st)
    part('cone', (sx * 0.3, 0.02, 0.92), (0.05, 0.05, 0.1), m=cr)
part('cone', (0, 0, 1.02), (0.05, 0.05, 0.1), m=cr)
save('standin_golem')

# Night Prowler (a cat, about 1 unit long): body, head, ears, four legs, a long raised tail
clear(); fur = mat('fur', (0.08, 0.06, 0.12)); glow = mat('glow', (0.6, 0.2, 1.0), 2)
part('sphere', (0, 0.05, 0.45), (0.15, 0.36, 0.13), m=fur)
part('sphere', (0, -0.4, 0.58), (0.12, 0.12, 0.11), m=fur)
for sx in (1, -1):
    part('cone', (sx * 0.07, -0.4, 0.71), (0.04, 0.03, 0.06), m=fur)
    part('sphere', (sx * 0.05, -0.51, 0.6), (0.02, 0.01, 0.02), m=glow)
    for y in (-0.25, 0.3):
        seg((sx * 0.08, y, 0.42), (sx * 0.08, y, 0.22), 0.04, fur); seg((sx * 0.08, y, 0.22), (sx * 0.08, y, 0.03), 0.035, fur)
seg((0, 0.38, 0.5), (0, 0.6, 0.62), 0.03, fur); seg((0, 0.6, 0.62), (0, 0.72, 0.82), 0.025, fur); seg((0, 0.72, 0.82), (0, 0.7, 0.98), 0.02, fur)
save('standin_cat')

# Flame Hatchling (a round chick, about 1 unit tall): body, head, beak, crest, wings out, tail feathers, little legs
clear(); fe = mat('feather', (1.0, 0.6, 0.25)); fl = mat('flame', (1.0, 0.35, 0.05), 3)
part('sphere', (0, 0, 0.42), (0.2, 0.2, 0.2), m=fe); part('sphere', (0, -0.04, 0.7), (0.15, 0.14, 0.14), m=fe)
part('cone', (0, -0.2, 0.68), (0.03, 0.03, 0.05), (math.radians(90), 0, 0), m=fl)
part('cone', (0, 0, 0.9), (0.07, 0.05, 0.12), m=fl)
for sx in (1, -1):
    part('sphere', (sx * 0.34, 0.03, 0.48), (0.17, 0.08, 0.05), (0, sx * 0.3, 0), m=fl)
    seg((sx * 0.08, 0, 0.24), (sx * 0.08, 0, 0.08), 0.02, fl); part('cube', (sx * 0.08, -0.04, 0.02), (0.03, 0.05, 0.01), m=fl)
part('cone', (0, 0.3, 0.36), (0.06, 0.12, 0.05), (math.radians(-80), 0, 0), m=fl)
save('standin_chick')

# Spirit Wisp (about 1 unit tall): a glowing core, a flame rising, four tendrils trailing down
clear(); co = mat('core', (0.3, 0.5, 1.0), 4); wi = mat('wisp', (0.4, 0.3, 1.0), 2)
part('sphere', (0, 0, 0.45), (0.17, 0.17, 0.17), m=co)
seg((0, 0, 0.58), (0, 0.04, 0.75), 0.07, wi); seg((0, 0.04, 0.75), (0, 0.09, 0.9), 0.045, wi); seg((0, 0.09, 0.9), (0, 0.13, 1.0), 0.02, wi)
for ax, ay in ((1, 0), (-1, 0), (0, 1), (0, -1)):
    seg((ax * 0.12, ay * 0.12, 0.38), (ax * 0.22, ay * 0.22, 0.22), 0.03, wi); seg((ax * 0.22, ay * 0.22, 0.22), (ax * 0.25, ay * 0.25, 0.04), 0.02, wi)
save('standin_wisp')

# Spellbook (the mage's pet, about 1 unit wide): an open book flying with its covers spread like wings, a block of
# pages in the middle, the spine running front to back, and two ribbon bookmarks hanging below
clear(); lea = mat('leather', (0.35, 0.12, 0.45)); pg = mat('pages', (0.92, 0.88, 0.75)); rib = mat('ribbon', (0.85, 0.65, 0.15))
for sx in (1, -1):
    part('cube', (sx * 0.24, 0, 0.62), (0.22, 0.3, 0.015), (0, sx * math.radians(-12), 0), m=lea, cuts=4)
    part('cube', (sx * 0.21, 0, 0.645), (0.19, 0.27, 0.02), (0, sx * math.radians(-12), 0), m=pg, cuts=3)
part('cyl', (0, 0, 0.6), (0.03, 0.03, 0.3), (math.radians(90), 0, 0), m=lea)
for x in (0.04, -0.04):
    seg((x, 0.22, 0.58), (x, 0.24, 0.3), 0.012, rib); seg((x, 0.24, 0.3), (x, 0.25, 0.05), 0.012, rib)
save('standin_book')
print('STANDINS DONE')
