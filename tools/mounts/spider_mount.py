# Ashen Hollow: the Giant Spider (Literock Studios, free for games; the model is never put in the repo) made into a
# store mount. Its own clips are renamed for the game (idle_loop -> Idle, walk_loop -> Walk, run_loop -> Gallop,
# bite -> Bite); a saddle is drawn here and laid on its back (a blanket laid on the carapace, a padded
# seat with a pommel and a raised cantle), all riding the "body" bone; a Socket_Saddle bone marks the
# seat; then it is scaled to riding size and its textures halved for phones.
#   blender python: spider_mount.py -- <Giant_Spider_Lite.glb> <out.glb> [scale]
import bpy, sys, math, mathutils, bmesh
a = sys.argv[sys.argv.index('--') + 1:]; src, dst = a[0], a[1]; K = float(a[2]) if len(a) > 2 else 1.75
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=src)
sc = bpy.context.scene
for o in list(sc.objects):
    if o.name == 'Icosphere' or o.type in ('LIGHT', 'CAMERA'): bpy.data.objects.remove(o)
arm = [o for o in sc.objects if o.type == 'ARMATURE'][0]
body = [o for o in sc.objects if o.type == 'MESH'][0]
for act in bpy.data.actions:
    act.name = {'idle_loop': 'Idle', 'walk_loop': 'Walk', 'run_loop': 'Gallop', 'bite': 'Bite'}.get(act.name, act.name)
    act.use_fake_user = True
if arm.animation_data: arm.animation_data.action = None
bpy.context.view_layer.update()

# the spider faces -y; the top of its carapace is about z 0.69 at y -0.2, the abdomen rises behind it
SEAT_Y = -0.2
def mat(name, col, rough=0.7, metal=0.0, emit=None):
    m = bpy.data.materials.new(name); m.use_nodes = True; b = m.node_tree.nodes['Principled BSDF']
    b.inputs['Base Color'].default_value = (*col, 1); b.inputs['Roughness'].default_value = rough; b.inputs['Metallic'].default_value = metal
    if emit: b.inputs['Emission Color'].default_value = (*emit, 1); b.inputs['Emission Strength'].default_value = 1.5
    return m
CLOTH = mat('SpiderBlanket', (0.14, 0.018, 0.012), 0.85)
TRIM = mat('SpiderTrim', (0.75, 0.48, 0.15), 0.35, 1.0)
LEATHER = mat('SpiderLeather', (0.08, 0.04, 0.018), 0.55)
EMBER = mat('SpiderEmber', (1.0, 0.35, 0.05), 0.5, 0.0, (1.0, 0.32, 0.04))

def finish(o, m):
    o.data.materials.clear(); o.data.materials.append(m)
    for p in o.data.polygons: p.use_smooth = True
    return o

# the carapace, smoothed: its top is near z = 0.705 - 3 x^2 - (y + 0.2)^2 (measured; the real mesh is too bumpy
# to lay cloth on)
def shell(x, y): return 0.705 - 3.0 * x * x - (y + 0.2) ** 2

def draped(name, w, l, cy, lift, m, thick, shape=None):
    """an oval laid on the carapace, then given thickness; shape(x, y) adds height (y: -1 front .. 1 back)"""
    # rings of points out from the middle (an oval with a clean edge)
    R, S = 12, 48; me = bpy.data.meshes.new(name); verts = [(0.0, 0.0)]; faces = []
    for i in range(1, R + 1):
        for j in range(S):
            a = 2 * math.pi * j / S; verts.append((i / R * math.cos(a), i / R * math.sin(a)))
    for j in range(S): faces.append((0, 1 + j, 1 + (j + 1) % S))
    for i in range(1, R):
        b0, b1 = 1 + (i - 1) * S, 1 + i * S
        for j in range(S): faces.append((b0 + j, b1 + j, b1 + (j + 1) % S, b0 + (j + 1) % S))
    co = []
    for u, t in verts:
        x, y = u * w / 2, cy + t * l / 2; co.append((x, y, shell(x, y) + lift + (shape(u, t) if shape else 0.0)))
    me.from_pydata(co, [], faces); me.update()
    for f in me.polygons:
        if f.normal.z < 0: f.flip()
    o = bpy.data.objects.new(name, me); sc.collection.objects.link(o)
    bpy.context.view_layer.objects.active = o; o.select_set(True)
    so = o.modifiers.new('thick', 'SOLIDIFY'); so.thickness = thick; so.offset = 1.0
    bpy.ops.object.modifier_apply(modifier='thick')
    return finish(o, m)

parts = []
parts.append(draped('Blanket', 0.46, 0.56, SEAT_Y - 0.01, 0.016, CLOTH, 0.012))
parts.append(draped('BlanketTrim', 0.50, 0.60, SEAT_Y - 0.01, 0.009, TRIM, 0.009))
# the seat: dished, rising to a high cantle at the back and a low swell at the front
seat = draped('Seat', 0.26, 0.32, SEAT_Y, 0.03, LEATHER, 0.045, lambda u, t: 0.08 * max(0.0, t) ** 3 + 0.04 * max(0.0, -t) ** 3 + 0.01 * u * u)
parts.append(seat)
top = shell(0, SEAT_Y) + 0.03 + 0.045

def prim(kind, name, loc, scl, m, rot=(0, 0, 0), **kw):
    getattr(bpy.ops.mesh, 'primitive_' + kind + '_add')(location=loc, rotation=rot, **kw)
    o = bpy.context.active_object; o.name = name; o.scale = scl; bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    return finish(o, m)
# a gold pommel with an ember stone at the front
parts.append(prim('cylinder', 'Pommel', (0, SEAT_Y - 0.15, top + 0.035), (0.022, 0.022, 0.04), TRIM, vertices=12))
parts.append(prim('uv_sphere', 'PommelGem', (0, SEAT_Y - 0.15, top + 0.085), (0.026, 0.026, 0.026), EMBER, segments=12, ring_count=8))
# everything rides the body bone
for o in parts:
    vg = o.vertex_groups.new(name='body'); vg.add(list(range(len(o.data.vertices))), 1.0, 'REPLACE')
    md = o.modifiers.new('Armature', 'ARMATURE'); md.object = arm
    o.parent = arm; o.matrix_parent_inverse = arm.matrix_world.inverted()
print('SADDLE top', round(top, 3))

# the rider's seat
bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode='EDIT')
eb = arm.data.edit_bones; sb = eb.new('Socket_Saddle'); sb.head = (0, SEAT_Y, top); sb.tail = (0, SEAT_Y, top + 0.15); sb.parent = eb['body']
bpy.ops.object.mode_set(mode='OBJECT')

# riding size
holder = bpy.data.objects.new('SpiderRoot', None); sc.collection.objects.link(holder)
arm.parent = holder; holder.scale = (K, K, K)
for im in bpy.data.images:
    if im.size[0] > 1024:
        im.scale(1024, max(1, int(1024 * im.size[1] / im.size[0])))
        try: im.pack()
        except Exception: pass
bpy.ops.export_scene.gltf(filepath=dst, export_format='GLB', export_skins=True, export_animations=True, export_animation_mode='ACTIONS', export_force_sampling=True, export_morph=False, export_image_format='AUTO')
print('DONE', dst, 'scale', K)
