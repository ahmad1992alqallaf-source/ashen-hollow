# Ashen Hollow: the still pets (toad, slimes, owls, parrot, baby dragon) are single meshes with no skeleton, so the
# game could only slide and bob them. This gives each a small skeleton (body, head, wings, legs, tail, as it has them)
# weighted by where each part of the mesh is, and keys the loops the game plays: Idle, Walk, Gallop and (fliers) Fly.
#   blender python: pet_rig.py -- <in.glb> <out.glb> <pet>
# All the bones point straight up, so on every bone: rotation x = nod (pitch), y = turn (yaw), z = roll about the way
# it faces; location y = up. The pets face -y with their feet near z 0 (the owls, parrot and dragon a little below).
import bpy, sys, math, mathutils
a = sys.argv[sys.argv.index('--') + 1:]; src, dst, pet = a[0], a[1], a[2]
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=src)
sc = bpy.context.scene; sc.render.fps = 24
for o in list(sc.objects):
    if o.name == 'Icosphere' or o.type in ('LIGHT', 'CAMERA'): bpy.data.objects.remove(o)
body = [o for o in sc.objects if o.type == 'MESH'][0]
mw = body.matrix_world.copy(); body.parent = None; body.matrix_world = mw
body.data.transform(body.matrix_world); body.matrix_world = mathutils.Matrix.Identity(4)
for o in list(sc.objects):
    if o.type == 'EMPTY': bpy.data.objects.remove(o)

def ss(a, b, t):   # smoothstep from a to b
    if a == b: return 1.0 if t >= b else 0.0
    t = max(0.0, min(1.0, (t - a) / (b - a))); return t * t * (3 - 2 * t)
def band(lo0, lo1, hi0, hi1, t): return ss(lo0, lo1, t) * (1 - ss(hi0, hi1, t))

# ---- each pet: its bones (name, head point, parent) and how much each bone holds a point p ----
# 'Body' takes whatever no other bone takes
def toad():
    B = [('Body', (0, 0.02, 0.12), 'Root'), ('Head', (0, -0.1, 0.27), 'Body'),
         ('LegBL', (-0.15, 0.05, 0.1), 'Body'), ('LegBR', (0.15, 0.05, 0.1), 'Body'),
         ('LegFL', (-0.11, -0.17, 0.1), 'Body'), ('LegFR', (0.11, -0.17, 0.1), 'Body'), ('Throat', (0, -0.18, 0.15), 'Body')]
    def W(p):
        x, y, z = p; w = {}
        w['Head'] = ss(0.22, 0.3, z) * (1 - ss(-0.02, 0.06, y))
        back = ss(0.11, 0.15, abs(x)) * ss(-0.08, -0.03, y) * (1 - ss(0.15, 0.19, z))
        w['LegBL' if x < 0 else 'LegBR'] = back
        front = ss(0.05, 0.08, abs(x)) * (1 - ss(-0.14, -0.1, y)) * (1 - ss(0.09, 0.13, z)) * (1 - back)
        w['LegFL' if x < 0 else 'LegFR'] = front
        w['Throat'] = (1 - ss(0.05, 0.09, abs(x))) * (1 - ss(-0.17, -0.12, y)) * band(0.04, 0.08, 0.2, 0.24, z)
        return w
    return B, W
def slime(top_from=0.18):
    B = [('Body', (0, 0, 0.02), 'Root'), ('Top', (0, 0, 0.22), 'Body')]
    def W(p): return {'Top': ss(top_from, 0.33, p[2])}
    return B, W
def owl(head_lo, wing_x, wing_z, tail_y, tail_z, feet_z, lantern=False):
    B = [('Body', (0, 0, feet_z + 0.03), 'Root'), ('Head', (0, -0.02, head_lo + 0.03), 'Body'),
         ('WingL', (-wing_x + 0.03, 0.02, wing_z[1] - 0.02), 'Body'), ('WingR', (wing_x - 0.03, 0.02, wing_z[1] - 0.02), 'Body'),
         ('Tail', (0, tail_y, tail_z + 0.03), 'Body'), ('Feet', (0, -0.05, feet_z), 'Root')]
    def W(p):
        x, y, z = p; w = {}
        w['Head'] = ss(head_lo - 0.03, head_lo + 0.04, z)
        wing = ss(wing_x - 0.03, wing_x + 0.02, abs(x)) * band(wing_z[0] - 0.03, wing_z[0] + 0.02, wing_z[1] - 0.03, wing_z[1] + 0.02, z) * (1 - w['Head'])
        if lantern: wing *= ss(-0.24, -0.17, y)
        w['WingL' if x < 0 else 'WingR'] = wing
        w['Tail'] = ss(tail_y - 0.04, tail_y + 0.02, y) * (1 - ss(tail_z + 0.04, tail_z + 0.1, z))
        w['Feet'] = 1 - ss(feet_z + 0.02, feet_z + 0.06, z)
        if lantern: w['Feet'] *= ss(-0.27, -0.22, y)   # (the lantern stays with the body)
        return w
    return B, W
def parrot():
    B = [('Root2', (0, 0.03, -0.12), 'Root'), ('Body', (0, 0.03, -0.06), 'Root'), ('Head', (0, -0.08, 0.13), 'Body'),
         ('WingL', (-0.1, 0.05, 0.1), 'Body'), ('WingR', (0.1, 0.05, 0.1), 'Body'), ('Tail', (0, 0.18, 0.0), 'Body')]
    def W(p):
        x, y, z = p; w = {}
        w['Head'] = ss(0.1, 0.16, z) * (1 - ss(0.0, 0.06, y))
        wing = ss(0.06, 0.1, abs(x)) * band(-0.07, -0.03, 0.12, 0.16, z) * band(-0.08, -0.03, 0.2, 0.24, y) * (1 - w['Head'])
        w['WingL' if x < 0 else 'WingR'] = wing
        w['Tail'] = ss(0.18, 0.23, y)
        w['Root2'] = 1 - ss(-0.1, -0.07, z)   # the feet and the branch
        return w
    return B, W
def drake():
    B = [('Body', (0, 0.02, -0.12), 'Root'), ('Head', (0, -0.12, 0.16), 'Body'),
         ('WingL', (-0.06, 0.03, 0.12), 'Body'), ('WingR', (0.06, 0.03, 0.12), 'Body'),
         ('Tail1', (0, 0.12, -0.13), 'Body'), ('Tail2', (0, 0.26, -0.15), 'Tail1'),
         ('LegFL', (-0.06, -0.12, -0.08), 'Body'), ('LegFR', (0.06, -0.12, -0.08), 'Body'),
         ('LegBL', (-0.08, 0.02, -0.08), 'Body'), ('LegBR', (0.08, 0.02, -0.08), 'Body')]
    def W(p):
        x, y, z = p; w = {}
        w['Head'] = ss(0.14, 0.2, z) * (1 - ss(-0.02, 0.04, y))
        wing = ss(-0.04, 0.01, y) * ss(0.02, 0.06, z) * ss(0.04, 0.07, abs(x)) * (1 - w['Head'])
        w['WingL' if x < 0 else 'WingR'] = wing
        t = ss(0.08, 0.14, y) * (1 - ss(-0.06, 0.0, z)) * (1 - wing)
        w['Tail1'] = t * (1 - ss(0.22, 0.3, y)); w['Tail2'] = t * ss(0.22, 0.3, y)
        low = 1 - ss(-0.1, -0.05, z)
        fl = low * (1 - ss(-0.1, -0.06, y)) * ss(0.015, 0.04, abs(x)); w['LegFL' if x < 0 else 'LegFR'] = fl
        bl = low * band(-0.08, -0.04, 0.08, 0.12, y) * ss(0.05, 0.08, abs(x)) * (1 - fl); w['LegBL' if x < 0 else 'LegBR'] = bl
        return w
    return B, W
RIGS = {'toad': toad, 'slime': slime, 'pumpkin_slime': lambda: slime(0.2),
        'owl': lambda: owl(0.13, 0.16, (-0.12, 0.12), 0.13, -0.2, -0.22),
        'lantern_owl': lambda: owl(0.17, 0.17, (-0.13, 0.15), 0.12, -0.1, -0.22, lantern=True),
        'parrot': parrot, 'drake': drake}
bones, weigh = RIGS[pet]()

# the skeleton
arm_d = bpy.data.armatures.new(pet + 'Rig'); arm = bpy.data.objects.new(pet + 'Rig', arm_d); sc.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode='EDIT'); eb = arm_d.edit_bones
r = eb.new('Root'); r.head = (0, 0, min(v.co.z for v in body.data.vertices)); r.tail = r.head + mathutils.Vector((0, 0, 0.05))
for n, h, par in bones:
    b = eb.new(n); b.head = h; b.tail = mathutils.Vector(h) + mathutils.Vector((0, 0, 0.06)); b.parent = eb[par]
bpy.ops.object.mode_set(mode='OBJECT')
names = [n for n, _, _ in bones]
for n in names: body.vertex_groups.new(name=n)
for v in body.data.vertices:
    w = {k: max(0.0, min(1.0, x)) for k, x in weigh(tuple(v.co)).items() if x > 0.001}
    s = sum(w.values())
    if s > 1: w = {k: x / s for k, x in w.items()}; s = 1.0
    w['Body'] = w.get('Body', 0) + (1 - s)
    for k, x in w.items():
        if x > 0.001: body.vertex_groups[k].add([v.index], x, 'REPLACE')
md = body.modifiers.new('Armature', 'ARMATURE'); md.object = arm; body.parent = arm
print('RIG', pet, {n: sum(1 for v in body.data.vertices if any(g.group == body.vertex_groups[n].index and g.weight > 0.5 for g in v.groups)) for n in names})

# ---- the loops ----
P = arm.pose.bones
def has(n): return n in P
def rot(n, x=0, y=0, z=0):
    if has(n): P[n].rotation_euler = (math.radians(x), math.radians(y), math.radians(z))
def loc(n, up=0.0, fwd=0.0, side=0.0):
    if has(n): P[n].location = (side, up, fwd)   # bone axes: x side, y up, z forward (toward -y in the world)
def scl(n, wide=1.0, tall=1.0):
    if has(n): P[n].scale = (wide, tall, wide)
def key_loop(name, secs, fn):
    act = bpy.data.actions.new(name); act.use_fake_user = True
    if arm.animation_data is None: arm.animation_data_create()
    arm.animation_data.action = act
    n = max(2, int(round(secs * 24)))
    for f in range(n + 1):
        for pb in P: pb.location = (0, 0, 0); pb.rotation_mode = 'XYZ'; pb.rotation_euler = (0, 0, 0); pb.scale = (1, 1, 1)
        fn(f / n, 2 * math.pi * f / n)
        for pb in P:
            pb.keyframe_insert('location', frame=f); pb.keyframe_insert('rotation_euler', frame=f); pb.keyframe_insert('scale', frame=f)
H = max(v.co.z for v in body.data.vertices) - min(v.co.z for v in body.data.vertices)

def breathe(t, w, amt=0.025):
    scl('Body', 1 + amt * math.sin(w), 1 + amt * 0.6 * math.sin(w))
if pet in ('slime', 'pumpkin_slime'):
    def idle(t, w):
        scl('Body', 1 + 0.05 * math.sin(w), 1 - 0.06 * math.sin(w)); rot('Top', 4 * math.sin(w + 1), 0, 5 * math.sin(w * 2))
    def hop(t, w, h):
        # squash, spring up stretched, land, squash
        air = max(0.0, math.sin(w)); land = max(0.0, -math.sin(w))
        loc('Body', up=h * H * air)
        scl('Body', 1 - 0.1 * air + 0.16 * land, 1 + 0.16 * air - 0.2 * land)
        rot('Top', -10 * math.sin(w), 0, 0)
    key_loop('Idle', 2.4, idle); key_loop('Walk', 0.7, lambda t, w: hop(t, w, 0.35)); key_loop('Gallop', 0.5, lambda t, w: hop(t, w, 0.5))
elif pet == 'toad':
    def idle(t, w):
        breathe(t, w, 0.02); scl('Throat', 1 + 0.35 * max(0.0, math.sin(w * 3)) ** 2, 1 + 0.15 * max(0.0, math.sin(w * 3)) ** 2)
        rot('Head', 3 * math.sin(w), 8 * math.sin(w * 0.5 + 1), 0)
    def hop(t, w, h):
        air = max(0.0, math.sin(w)); crouch = max(0.0, -math.sin(w))
        loc('Body', up=h * H * air - 0.04 * H * crouch); rot('Body', -14 * air + 6 * crouch, 0, 0)
        # the hind legs kick out behind and stay down; the forelegs reach ahead to land
        for n in ('LegBL', 'LegBR'): rot(n, 55 * air, 0, 0); loc(n, up=-h * H * air * 0.6, fwd=-0.05 * air)
        for n in ('LegFL', 'LegFR'): rot(n, -30 * air, 0, 0); loc(n, up=-h * H * air * 0.3, fwd=0.03 * air)
        rot('Head', 10 * air - 4 * crouch, 0, 0)
    key_loop('Idle', 3.0, idle); key_loop('Walk', 0.8, lambda t, w: hop(t, w, 0.3)); key_loop('Gallop', 0.55, lambda t, w: hop(t, w, 0.45))
elif pet in ('owl', 'lantern_owl', 'parrot'):
    sx = {'WingL': -1, 'WingR': 1}
    def idle(t, w):
        breathe(t, w, 0.02)
        # the head turns to look about (owls turn a long way), and the wings ruffle now and then
        look = 55 if 'owl' in pet else 30
        rot('Head', 4 * math.sin(w * 2), look * math.sin(w) * abs(math.sin(w)), 0)
        for n in ('WingL', 'WingR'): rot(n, 0, 0, sx[n] * 6 * max(0.0, math.sin(w * 2 - 1)) ** 4)
        rot('Tail', 6 * math.sin(w * 2), 0, 0)
    def fly(t, w):
        beat = math.sin(w)
        for n in ('WingL', 'WingR'): rot(n, -10, 0, sx[n] * (35 + 55 * beat))
        loc('Body', up=0.04 * H * -beat); rot('Body', -12, 0, 0); rot('Head', 10, 0, 0)
        rot('Tail', -10 + 8 * math.sin(w - 1), 0, 0)
        if has('Feet'): rot('Feet', 30, 0, 0)
    def walk(t, w, s):
        # a waddle: rocking side to side, a little hop on each step
        rot('Body', 0, 0, s * 7 * math.sin(w)); loc('Body', up=0.025 * H * abs(math.sin(w)))
        rot('Head', 0, 0, -s * 5 * math.sin(w)); rot('Tail', 0, s * 8 * math.sin(w), 0)
        for n in ('WingL', 'WingR'): rot(n, 0, 0, sx[n] * 8 * abs(math.sin(w)))
    key_loop('Idle', 4.0, idle); key_loop('Fly', 0.45 if pet == 'parrot' else 0.6, fly)
    key_loop('Walk', 0.8, lambda t, w: walk(t, w, 1.0)); key_loop('Gallop', 0.5, lambda t, w: walk(t, w, 1.4))
elif pet == 'drake':
    sx = {'WingL': -1, 'WingR': 1}
    def idle(t, w):
        breathe(t, w, 0.025)
        rot('Head', 5 * math.sin(w * 2), 18 * math.sin(w), 0)
        for n in ('WingL', 'WingR'): rot(n, 0, 0, sx[n] * (4 + 10 * max(0.0, math.sin(w * 2)) ** 3))
        rot('Tail1', 0, 12 * math.sin(w), 0); rot('Tail2', 0, 18 * math.sin(w - 0.8), 0)
    def fly(t, w):
        beat = math.sin(w)
        for n in ('WingL', 'WingR'): rot(n, -15, 0, sx[n] * (30 + 60 * beat))
        loc('Body', up=0.05 * H * -beat); rot('Body', -15, 0, 0); rot('Head', 15, 0, 0)
        for n in ('LegFL', 'LegFR', 'LegBL', 'LegBR'): rot(n, 25, 0, 0)
        rot('Tail1', 8 * math.sin(w - 1), 0, 0); rot('Tail2', 12 * math.sin(w - 2), 0, 0)
    def walk(t, w, s):
        loc('Body', up=0.03 * H * abs(math.sin(w))); rot('Body', 0, 0, 4 * s * math.sin(w))
        rot('LegFL', 25 * s * math.sin(w), 0, 0); rot('LegBR', 25 * s * math.sin(w), 0, 0)
        rot('LegFR', -25 * s * math.sin(w), 0, 0); rot('LegBL', -25 * s * math.sin(w), 0, 0)
        rot('Head', 4 * math.sin(w * 2), 0, 0)
        rot('Tail1', 0, 15 * s * math.sin(w), 0); rot('Tail2', 0, 22 * s * math.sin(w - 0.9), 0)
        for n in ('WingL', 'WingR'): rot(n, 0, 0, sx[n] * 8 * abs(math.sin(w)))
    key_loop('Idle', 3.0, idle); key_loop('Fly', 0.55, fly)
    key_loop('Walk', 0.8, lambda t, w: walk(t, w, 1.0)); key_loop('Gallop', 0.5, lambda t, w: walk(t, w, 1.4))
arm.animation_data.action = None
for pb in P: pb.location = (0, 0, 0); pb.rotation_euler = (0, 0, 0); pb.scale = (1, 1, 1)
print('CLIPS', [x.name for x in bpy.data.actions])
bpy.ops.export_scene.gltf(filepath=dst, export_format='GLB', export_skins=True, export_animations=True, export_animation_mode='ACTIONS', export_force_sampling=True, export_morph=False, export_image_format='AUTO')
print('DONE', dst)
