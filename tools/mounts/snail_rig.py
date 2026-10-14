# Ashen Hollow: the Fantasy Snail Mount (reczek.przemyslaw, CC-BY 4.0) has no skeleton, so it is given one here:
# a chain of bones along the body (tail to head), a head and two eye stalks; the shell, saddle and packs ride the
# "Back" bone. Three loops are keyed on it: Walk (a slow wave rolling along the foot), Gallop (the same, quicker and
# deeper, stalks swept back) and Idle (breathing, the stalks looking about). Then it is scaled to riding size.
#   blender python: snail_rig.py -- <in.glb> <out.glb> [scale]
import bpy, sys, math, mathutils
a = sys.argv[sys.argv.index('--') + 1:]; src, dst = a[0], a[1]; K = float(a[2]) if len(a) > 2 else 0.62
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=src)
sc = bpy.context.scene
for o in list(sc.objects):
    if o.name == 'Icosphere': bpy.data.objects.remove(o)
meshes = [o for o in sc.objects if o.type == 'MESH']
# bake every mesh into world space and free it of the Sketchfab empties
for o in meshes:
    o.data = o.data.copy()   # (two parts can share one mesh: each is baked on its own)
    mw = o.matrix_world.copy(); o.parent = None; o.matrix_world = mw
    o.data.transform(o.matrix_world); o.matrix_world = mathutils.Matrix.Identity(4)
for o in list(sc.objects):
    if o.type == 'EMPTY': bpy.data.objects.remove(o)

# the skeleton (the snail faces -y; its foot is at z 0)
CH = [('Tail', 2.7), ('Spine_1', 1.7), ('Back', 0.7), ('Neck', -0.3), ('Head', -1.1)]
arm_d = bpy.data.armatures.new('SnailRig'); arm = bpy.data.objects.new('SnailRig', arm_d); sc.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode='EDIT')
eb = arm_d.edit_bones
root = eb.new('Root'); root.head = (0, 0, 0); root.tail = (0, 0, 0.3)
for n, y in CH:
    b = eb.new(n); b.head = (0, y, 0.4); b.tail = (0, y, 1.0); b.parent = root
STALK = {'StalkL': -0.34, 'StalkR': 0.34}
for n, x in STALK.items():
    b = eb.new(n); b.head = (x, -1.12, 2.0); b.tail = (x * 1.03, -1.2, 2.6); b.parent = eb['Head']
bpy.ops.object.mode_set(mode='OBJECT')

def bind(o, weights):
    """weights: per vertex list of (bone, w)"""
    for v, ws in enumerate(weights):
        for bn, w in ws:
            g = o.vertex_groups.get(bn) or o.vertex_groups.new(name=bn); g.add([v], w, 'REPLACE')
    md = o.modifiers.new('Armature', 'ARMATURE'); md.object = arm
    o.parent = arm

def chain(y):
    ws = [(n, math.exp(-((y - by) / 0.65) ** 2)) for n, by in CH]
    s = sum(w for _, w in ws) or 1.0
    return [(n, w / s) for n, w in ws if w / s > 0.02]

HEADGEAR = ('Mask', 'BeardStripe', 'Button')
for o in meshes:
    vs = o.data.vertices; n = o.name
    if n.startswith('Snail_LP'):
        W = []
        for v in vs:
            p = v.co
            if p.y < -0.8 and p.z > 1.95 and abs(p.x) > 0.08:
                t = min(1.0, (p.z - 1.95) / 0.2); side = 'StalkL' if p.x < 0 else 'StalkR'
                W.append([(side, t), ('Head', 1 - t)] if t < 1 else [(side, 1.0)])
            else:
                W.append(chain(p.y))
        bind(o, W)
    elif n.startswith('Eyes'):
        side = 'StalkL' if sum(v.co.x for v in vs) < 0 else 'StalkR'
        bind(o, [[(side, 1.0)]] * len(vs))
    elif n.startswith(HEADGEAR):
        bind(o, [[('Head', 1.0)]] * len(vs))
    elif n.startswith('Reins'):
        bind(o, [chain(v.co.y) for v in vs])
    else:
        bind(o, [[('Back', 1.0)]] * len(vs))
print('BOUND', len(meshes))

# the loops (24 fps). Bones point up, so a bone's local y is world up and its local x is world x
FPS = 24; sc.render.fps = FPS
def key_loop(name, secs, fn):
    act = bpy.data.actions.new(name); act.use_fake_user = True
    if arm.animation_data is None: arm.animation_data_create()
    arm.animation_data.action = act
    n = int(round(secs * FPS))
    for f in range(n + 1):
        t = f / n
        for pb in arm.pose.bones:
            pb.location = (0, 0, 0); pb.rotation_mode = 'XYZ'; pb.rotation_euler = (0, 0, 0); pb.scale = (1, 1, 1)
        fn(t)
        for pb in arm.pose.bones:
            pb.keyframe_insert('location', frame=f); pb.keyframe_insert('rotation_euler', frame=f); pb.keyframe_insert('scale', frame=f)
    return act
P = arm.pose.bones
def crawl(t, amp, head, stalk_back, stalk_sway, stretch):
    w = 2 * math.pi * t
    for i, (n, _) in enumerate(CH):
        ph = w - i * 1.1            # the wave rolls from the tail to the head
        P[n].location = (0, amp * max(0.0, math.sin(ph)), 0)
        P[n].scale = (1, 1, 1 + stretch * math.sin(ph))
    P['Back'].location = (0, amp * 0.6 * (0.5 + 0.5 * math.sin(w - 2.2)), 0)
    P['Head'].rotation_euler = (math.radians(head * math.sin(w - 4.4)), 0, 0)
    for n, sx in (('StalkL', -1), ('StalkR', 1)):
        P[n].rotation_euler = (math.radians(-stalk_back + stalk_sway * math.sin(w * 2 - (0.6 if sx > 0 else 0))), 0, math.radians(sx * stalk_sway * 0.5 * math.sin(w)))
def idle(t):
    w = 2 * math.pi * t
    P['Back'].scale = (1 + 0.015 * math.sin(w), 1 + 0.02 * math.sin(w), 1)
    P['Head'].rotation_euler = (math.radians(4 * math.sin(w)), 0, math.radians(6 * math.sin(w * 1 + 1)))
    P['StalkL'].rotation_euler = (math.radians(10 * math.sin(w * 2)), 0, math.radians(-18 * math.sin(w + 0.5)))
    P['StalkR'].rotation_euler = (math.radians(10 * math.sin(w * 2 + 1.8)), 0, math.radians(14 * math.sin(w + 2.4)))
key_loop('Walk', 1.6, lambda t: crawl(t, 0.07, 5, 8, 6, 0.04))
key_loop('Gallop', 0.8, lambda t: crawl(t, 0.13, 8, 28, 9, 0.07))
key_loop('Idle', 4.0, idle)
arm.animation_data.action = None
for pb in P: pb.location = (0, 0, 0); pb.rotation_euler = (0, 0, 0); pb.scale = (1, 1, 1)

# riding size
holder = bpy.data.objects.new('SnailRoot', None); sc.collection.objects.link(holder)
arm.parent = holder; holder.scale = (K, K, K)
for im in bpy.data.images:
    if im.size[0] > 1024:
        im.scale(1024, max(1, int(1024 * im.size[1] / im.size[0])))
        try: im.pack()
        except Exception: pass
bpy.ops.export_scene.gltf(filepath=dst, export_format='GLB', export_skins=True, export_animations=True, export_animation_mode='ACTIONS', export_force_sampling=True, export_morph=False, export_image_format='AUTO')
print('DONE', dst, 'scale', K)
