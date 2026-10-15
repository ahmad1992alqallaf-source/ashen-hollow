# Ashen Hollow: walk, run and idle loops keyed by hand for a creature that has a skeleton but no clips (Tyrant Alpha)
# or no skeleton at all (the Sand Golem: one is built for it and the skin weighted to it automatically).
#   blender python: proc_anim.py -- <config.json> <id> <out.glb>
# The config (tools/mounts/procanim.json) names the bones that do each job:
#   build    (no skeleton) bones to make: [name, head [x,y,z], tail [x,y,z], parent]; positions as fractions of the
#            model's height (x and y too), from its feet, the model facing -y
#   root     the bone that bobs; spine, neck, tail: chains; legs: [[thigh, shin, foot], [...]] (left, right);
#   arms     [[upper, lower], [...]]
#   biped    true: a two-legged walk (the legs half a stride apart); quad (four legs): diagonal pairs
#   amp      scales every swing (1)
# Every turn is made about the model's own axes (x across it, y along it, z up), whatever way its bones point, so one
# set of numbers fits any rig: a leg swings about x, a tail sways about z, the body rolls about y.
import bpy, sys, json, math, mathutils
a = sys.argv[sys.argv.index('--') + 1:]; C = json.load(open(a[0]))[a[1]]; dst = a[2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=C['src'])
sc = bpy.context.scene; sc.render.fps = 30
for o in list(sc.objects):
    if o.name.startswith('Icosphere') or o.type in ('LIGHT', 'CAMERA'): bpy.data.objects.remove(o)
meshes = [o for o in sc.objects if o.type == 'MESH']
bpy.context.view_layer.update()
if 'build' in C:
    # a model straight from Tripo or Meshy: turned to face -y ("yaw" degrees about up), stood on the ground at the
    # middle (x, y), so the skeleton's fractions land on it
    tops = [o for o in sc.objects if o.parent is None]
    if C.get('yaw'):
        R = mathutils.Matrix.Rotation(math.radians(C['yaw']), 4, 'Z')
        for o in tops: o.matrix_world = R @ o.matrix_world
    bpy.context.view_layer.update()
    dg0 = bpy.context.evaluated_depsgraph_get(); q = []
    for o in meshes: q += [o.matrix_world @ v.co for v in o.evaluated_get(dg0).to_mesh().vertices]
    off = mathutils.Vector(((min(v.x for v in q) + max(v.x for v in q)) * -0.5, (min(v.y for v in q) + max(v.y for v in q)) * -0.5, -min(v.z for v in q)))
    for o in tops: o.matrix_world = mathutils.Matrix.Translation(off) @ o.matrix_world
    bpy.context.view_layer.update()

if 'build' in C:
    # a skeleton for a model that has none, weighted to the skin by bone heat
    dg = bpy.context.evaluated_depsgraph_get(); pts = []
    for o in meshes: pts += [o.matrix_world @ v.co for v in o.evaluated_get(dg).to_mesh().vertices]
    z0 = min(p.z for p in pts); H = max(p.z for p in pts) - z0
    ad = bpy.data.armatures.new('Rig'); arm = bpy.data.objects.new('Rig', ad); sc.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm; arm.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
    # "fit": "box": x and y as fractions of the model's half width and half length (it is centred), z of its height,
    # so one skeleton fits a fat golem and a thin one alike; else every number is a fraction of the height
    hw = (max(p.x for p in pts) - min(p.x for p in pts)) * 0.5; hl = (max(p.y for p in pts) - min(p.y for p in pts)) * 0.5
    kx, ky = (hw, hl) if C.get('fit') == 'box' else (H, H)
    for name, h, t, par in C['build']:
        b = ad.edit_bones.new(name); b.head = (h[0] * kx, h[1] * ky, z0 + h[2] * H); b.tail = (t[0] * kx, t[1] * ky, z0 + t[2] * H)
        if par: b.parent = ad.edit_bones[par]
    bpy.ops.object.mode_set(mode='OBJECT')
    for o in meshes:
        mw = o.matrix_world.copy(); o.parent = None; o.matrix_world = mw
    bpy.ops.object.select_all(action='DESELECT')
    for o in meshes: o.select_set(True)
    arm.select_set(True); bpy.context.view_layer.objects.active = arm
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')
    for o in meshes:
        if any(len(v.groups) == 0 for v in o.data.vertices):
            # bone heat failed (a mesh of many loose pieces): each vertex to its nearest bones by distance
            segs = [(b.name, arm.matrix_world @ b.head_local, arm.matrix_world @ b.tail_local) for b in ad.bones]
            groups = {n: (o.vertex_groups.get(n) or o.vertex_groups.new(name=n)) for n, _, _ in segs}
            for v in o.data.vertices:
                if len(v.groups) > 0: continue   # (bone heat reached it)
                p = o.matrix_world @ v.co; ds = []
                for n, h, t in segs:
                    ab = t - h; u = max(0.0, min(1.0, (p - h).dot(ab) / max(1e-9, ab.length_squared)))
                    ds.append(((h + ab * u - p).length, n))
                ds.sort(); best = ds[:2]; w = [1.0 / max(1e-4, d) ** 4 for d, _ in best]; sw = sum(w)
                for (d, n), wi in zip(best, w): groups[n].add([v.index], wi / sw, 'REPLACE')
    for o in meshes:
        n = sum(1 for v in o.data.vertices if len(v.groups) == 0)
        print('WEIGHTS', o.name, 'unweighted vertices', n, 'of', len(o.data.vertices))
else:
    arm = [o for o in sc.objects if o.type == 'ARMATURE'][0]
# the pose the loops start from: a frame of one of its own clips ("base": [clip, frame]; a Sketchfab rig lies on its
# side at rest and stands up in its clips), else the rest pose
P0 = arm.pose.bones; amp = C.get('amp', 1.0)
class Bones:
    # a bone by its name, or by its name with the glTF importer's number after it ("DEF-thigh.L" -> "DEF-thigh.L_070")
    def __getitem__(self, n):
        if n in P0: return P0[n]
        for pb in P0:
            if pb.name.startswith(n + '_'): return pb
        raise KeyError(n)
    def __iter__(self): return iter(P0)
P = Bones()
base = {}
if 'base' in C:
    ba = [x for x in bpy.data.actions if x.name == C['base'][0] or x.name.startswith(C['base'][0] + '_')][0]
    if arm.animation_data is None: arm.animation_data_create()
    arm.animation_data.action = ba
    try: arm.animation_data.action_slot = ba.slots[0]
    except Exception: pass
    sc.frame_set(int(C['base'][1])); bpy.context.view_layer.update()
for pb in P:
    q = pb.rotation_quaternion.copy() if pb.rotation_mode == 'QUATERNION' else pb.rotation_euler.to_quaternion()
    base[pb.name] = (pb.location.copy(), q, pb.scale.copy())
if not C.get('keep_clips'):
    for act in list(bpy.data.actions): bpy.data.actions.remove(act)
if arm.animation_data: arm.animation_data.action = None
for pb in P: pb.rotation_mode = 'QUATERNION'
def reset():
    for pb in P:
        l, q, s_ = base[pb.name]; pb.location = l; pb.rotation_quaternion = q; pb.scale = s_
    bpy.context.view_layer.update()
reset()
# the model's own axes, from where its head and tail are as it stands: forward, across, up
def wpos(n): return arm.matrix_world @ P[n].head
up = mathutils.Vector((0, 0, 1))
fw = (wpos(C['face'][0]) - wpos(C['face'][1])) if 'face' in C else mathutils.Vector((0, -1, 0))   # face: [a bone at the front, one at the back]
fw.z = 0; fw.normalize()   # (level: a forward that pointed down made the planted feet sink as they slid back)
lat = fw.cross(up).normalized()
# (the numbers below were made for a model facing -y: x = -across, y = -forward)
AX = {'x': -lat, 'y': -fw, 'z': up}
# feet planted by IK ("ik": true): each foot follows a path (on the ground going back, lifted coming forward) and the
# thigh and shin bend to reach it, so a stance foot stays put instead of swinging through the floor like a pendulum
IKL = []
if C.get('ik'):
    bpy.context.view_layer.update()
    for i, (th, sh, ft) in enumerate(C.get('legs', [])):
        ank = arm.matrix_world @ P[sh].tail; knee = arm.matrix_world @ P[sh].head; hip = arm.matrix_world @ P[th].head
        tg = bpy.data.objects.new('IKT%d' % i, None); sc.collection.objects.link(tg); tg.location = ank
        # the pole: far out the way the knee already points from the hip-ankle line, so it bends the same way
        mid = (hip + ank) * 0.5; out = (knee - mid); out = out - out.project(ank - hip)
        if out.length < 1e-4: out = fw.copy()
        pole = bpy.data.objects.new('IKP%d' % i, None); sc.collection.objects.link(pole); pole.location = knee + out.normalized() * 3.0
        c = P[sh].constraints.new('IK'); c.target = tg; c.pole_target = pole; c.chain_count = 2; c.pole_angle = math.radians(C.get('pole_angle', -90))
        IKL.append((th, sh, ft, tg, ank.copy(), pole))
    GROUND = min(x[4].z for x in IKL)
    FOOT0 = {ft: P[ft].matrix.to_quaternion() for th, sh, ft, tg, a0, pole in IKL if ft}

def place_feet(ph, k):
    # a foot spends 60% of the step on the ground sliding back, then lifts and swings forward
    L = C.get('stride', 0.5) * k; H = C.get('lift', 0.14) * (0.7 + 0.3 * k); biped = C.get('biped', True)
    for i, (th, sh, ft, tg, a0, pole) in enumerate(IKL):
        u = ((ph / (2 * math.pi)) + (0.5 if (biped and i == 1) or (not biped and i in (1, 2)) else 0.0)) % 1.0
        if u < 0.6: along = 0.5 - u / 0.6; up = 0.0
        else: w = (u - 0.6) / 0.4; along = -0.5 + w; up = math.sin(w * math.pi)
        tg.location = a0 + fw * (along * L) + up_v * (up * H)
up_v = mathutils.Vector((0, 0, 1))

def rot(bn, axis, deg, q=None):
    # a turn of the bone about one of the model's axes, made from where the bone is now (its parents already turned)
    bpy.context.view_layer.update(); pb = P[bn]
    m = (arm.matrix_world.to_3x3() @ pb.matrix.to_3x3()).normalized().inverted()
    ax = (m @ AX[axis]).normalized(); r = mathutils.Quaternion(ax, math.radians(deg * amp))
    return r if q is None else q @ r
def setq(bn, q): P[bn].rotation_quaternion = P[bn].rotation_quaternion @ q
def bob(bn, dz):
    bpy.context.view_layer.update(); pb = P[bn]; m = (arm.matrix_world.to_3x3() @ pb.matrix.to_3x3()).inverted()
    pb.location = pb.location + m @ mathutils.Vector((0, 0, dz))

def clip(name, n, step):
    act = bpy.data.actions.new(name); act.use_fake_user = True
    if arm.animation_data is None: arm.animation_data_create()
    arm.animation_data.action = act
    for f in range(n + 1):
        sc.frame_set(f); reset()
        step(2 * math.pi * f / n)
        if IKL:
            # the IK's bend written into the leg bones' own channels (the constraint itself is not exported)
            bpy.context.view_layer.update()
            for th, sh, ft, tg, a0, pole in IKL:
                for bn in (th, sh):
                    pb = P[bn]; pb.matrix = pb.matrix.copy()   # (setting the visual matrix stores it in the basis)
                    bpy.context.view_layer.update()
                if ft and C.get('level_feet', True):
                    # the foot kept as it stands at rest (level on the ground, not hanging toes-down off the shin)
                    pf = P[ft]; m = pf.matrix.copy(); r0 = FOOT0[ft]
                    pf.matrix = mathutils.Matrix.LocRotScale(m.to_translation(), r0, m.to_scale()); bpy.context.view_layer.update()
        for pb in P:
            pb.keyframe_insert('rotation_quaternion'); pb.keyframe_insert('location'); pb.keyframe_insert('scale')
    arm.animation_data.action = None
    return act

def gait(ph, k):
    # k: 1 walk, ~1.6 run. A leg swings forward (a turn about -x brings a hanging leg forward, the model facing -y),
    # its knee folds as it comes through and the foot is kept near level
    legs = [] if IKL else C.get('legs', []); biped = C.get('biped', True)
    if IKL: place_feet(ph, k)
    for i, (th, sh, ft) in enumerate(legs):
        p = ph + (math.pi if (biped and i == 1) or (not biped and i in (1, 2)) else 0.0)
        swing = -24 * k * math.sin(p); knee = 34 * k * max(0.0, math.sin(p + math.pi * 0.35)) ** 1.3
        setq(th, rot(th, 'x', swing))
        if sh: setq(sh, rot(sh, 'x', knee * C.get('knee', 1.0)))
        if ft: setq(ft, rot(ft, 'x', -(swing + knee * C.get('knee', 1.0)) * 0.6))
    for i, (up, lo) in enumerate(C.get('arms', [])):
        p = ph + (0.0 if i == 1 else math.pi)
        setq(up, rot(up, 'x', -14 * k * math.sin(p) * C.get('arm', 1.0)))
        if lo: setq(lo, rot(lo, 'x', (-8 - 6 * k * max(0.0, math.sin(p))) * C.get('arm', 1.0)))
    sp = C.get('spine', [])
    for j, b in enumerate(sp): setq(b, rot(b, 'y', 3.0 * k * math.sin(ph) / max(1, len(sp)), rot(b, 'z', 2.5 * k * math.sin(ph) / max(1, len(sp)))))
    for j, b in enumerate(C.get('tail', [])): setq(b, rot(b, 'z', -5 * k * math.sin(ph - 0.5 * j), rot(b, 'x', 1.5 * math.sin(2 * ph - 0.4 * j))))
    nk = C.get('neck', [])
    for j, b in enumerate(nk): setq(b, rot(b, 'x', 3.5 * k * math.sin(2 * ph + 0.6) / max(1, len(nk)), rot(b, 'z', -2 * math.sin(ph) / max(1, len(nk)))))
    if C.get('root'): bob(C['root'], -C.get('bob', 0.03) * k * abs(math.cos(ph)))

def idle(ph):
    # breathing, a slow sway of the tail, a look about
    for b in C.get('spine', []): setq(b, rot(b, 'x', 1.2 * math.sin(ph)))
    for j, b in enumerate(C.get('tail', [])): setq(b, rot(b, 'z', 4 * math.sin(ph - 0.45 * j)))
    nk = C.get('neck', [])
    for b in nk: setq(b, rot(b, 'z', 6 * math.sin(ph) / max(1, len(nk)), rot(b, 'x', 2 * math.sin(2 * ph) / max(1, len(nk)))))
    for i, (up, lo) in enumerate(C.get('arms', [])): setq(up, rot(up, 'x', 2 * math.sin(ph + i)))
    if C.get('root'): bob(C['root'], 0.008 * math.sin(2 * ph))

def slam(ph):
    # both fists up over the head, then down on the ground in front, the body bending into the blow
    u = ph / (2 * math.pi)
    def ease(t): t = max(0.0, min(1.0, t)); return t * t * (3 - 2 * t)
    lift = ease(u / 0.5) * (1 - ease((u - 0.5) / 0.14)); down = ease((u - 0.5) / 0.14) * (1 - ease((u - 0.75) / 0.25))
    for up, lo in C.get('arms', []):
        setq(up, rot(up, 'x', -120 * lift + 25 * down)); 
        if lo: setq(lo, rot(lo, 'x', -30 * lift))
    for b in C.get('spine', []): setq(b, rot(b, 'x', (-8 * lift + 14 * down) / max(1, len(C.get('spine', [])))))
    if C.get('root'): bob(C['root'], -0.06 * down)

# ---- wings, tendrils, a flame and floating (the pets: Flame Hatchling, Spirit Wisp) ----
def flap(ph, deg, fold=0.0):
    # wings: [[upper, lower], ...] left then right; a beat turns them about the body's long axis, the outer bone a
    # little behind the inner one; fold > 0 tucks them against the body
    for i, (w1, w2) in enumerate(C.get('wings', [])):
        sg = 1 if i == 0 else -1
        setq(w1, rot(w1, 'y', sg * (deg * math.sin(ph) - fold)))
        if w2: setq(w2, rot(w2, 'y', sg * (deg * 0.6 * math.sin(ph - 0.7) - fold * 0.6)))
def sway(ph, k):
    # loose chains (tendrils, tail feathers, the wisp's flame) waving, each bone a little later than the one before
    for ci, ch in enumerate(C.get('tendrils', [])):
        for j, b in enumerate(ch):
            setq(b, rot(b, 'x', 9 * k * math.sin(ph - 0.7 * j + ci * 1.3), rot(b, 'z', 7 * k * math.sin(0.5 * ph - 0.6 * j + ci))))
    for j, b in enumerate(C.get('flame', [])):
        setq(b, rot(b, 'z', 6 * k * math.sin(2 * ph - 0.8 * j), rot(b, 'x', 5 * k * math.sin(3 * ph - 0.9 * j))))
def hover(ph, k, lean=0.0):
    if C.get('root'):
        bob(C['root'], C.get('float', 0.04) * math.sin(ph))
        if lean: setq(C['root'], rot(C['root'], 'x', lean))
def tuck():
    # legs drawn up in flight
    for th, sh, ft in C.get('legs', []):
        setq(th, rot(th, 'x', -35));
        if sh: setq(sh, rot(sh, 'x', 50))
def pounce(ph):
    # a cat's pounce: crouch, spring forward with the forelegs reaching, land
    u = ph / (2 * math.pi)
    def ease(t): t = max(0.0, min(1.0, t)); return t * t * (3 - 2 * t)
    crouch = ease(u / 0.35) * (1 - ease((u - 0.35) / 0.15)); leap = ease((u - 0.35) / 0.15) * (1 - ease((u - 0.65) / 0.35))
    sp = C.get('spine', [])
    for b in sp: setq(b, rot(b, 'x', (6 * crouch - 10 * leap) / max(1, len(sp))))
    for i, (th, sh, ft) in enumerate(C.get('legs', [])):
        front = i < 2
        setq(th, rot(th, 'x', (-40 * leap if front else 25 * leap) + 15 * crouch))
        if sh: setq(sh, rot(sh, 'x', 30 * crouch))
    if C.get('root'): bob(C['root'], -0.05 * crouch + 0.08 * leap)
def peck(ph):
    u = ph / (2 * math.pi); d = math.sin(min(1.0, u / 0.5) * math.pi)
    for b in C.get('neck', []): setq(b, rot(b, 'x', 35 * d / max(1, len(C.get('neck', [])))))
    flap(ph * 2, 25)

kind = C.get('kind', 'beast')
if kind == 'bird':
    # Flame Hatchling: waddles on the ground, flies with its wings beating fast and its legs tucked up
    clip('Idle', 90, lambda ph: (idle(ph), flap(ph * 2, 4, 5), sway(ph, 0.6)))
    clip('Walk', C.get('walk_frames', 24), lambda ph: (gait(ph, 1.0), flap(ph * 2, 8, 5), sway(ph, 1.0)))
    clip('Fly', C.get('fly_frames', 12), lambda ph: (flap(ph, C.get('flap', 55)), tuck(), hover(ph, 1.0, 8), sway(ph, 1.5)))
    clip('Attack', 30, peck)
elif kind == 'float':
    # Spirit Wisp: no legs, it floats; its tendrils and flame never stop moving
    clip('Idle', 90, lambda ph: (hover(ph, 1.0), sway(ph, 1.0)))
    clip('Fly', 60, lambda ph: (hover(ph, 1.4, 10), sway(ph, 1.8)))
    clip('Walk', 60, lambda ph: (hover(ph, 1.4, 10), sway(ph, 1.8)))
    clip('Attack', 30, lambda ph: (hover(ph, 2.5), sway(ph, 3.0)))
else:
    if not C.get('keep_clips'): clip('Idle', 120, lambda ph: (idle(ph), sway(ph, 0.5)))
    if C.get('attack') == 'pounce': clip('Attack', 36, pounce)
    elif C.get('attack'): clip('Attack', 40, slam)
    clip('Walk', C.get('walk_frames', 36), lambda ph: (gait(ph, 1.0), sway(ph, 1.0)))
    clip('Gallop', C.get('run_frames', 22), lambda ph: (gait(ph, C.get('run', 1.6)), sway(ph, 1.6)))
for th, sh, ft, tg, a0, pole in IKL:
    for c in list(P[sh].constraints):
        if c.type == 'IK': P[sh].constraints.remove(c)
    bpy.data.objects.remove(tg); bpy.data.objects.remove(pole)
arm.animation_data.action = bpy.data.actions.get('Idle') or bpy.data.actions.get('Walk') or bpy.data.actions[0]
bpy.ops.export_scene.gltf(filepath=dst, export_format='GLB', export_skins=True, export_animations=True, export_animation_mode='ACTIONS',
                          export_force_sampling=True, export_image_format='AUTO')
print('DONE', dst)
