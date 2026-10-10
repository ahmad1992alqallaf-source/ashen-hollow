# put a detailed animal (Meshy, textured) on the skeleton and animations of the game's low-poly animal of the same kind:
# the new body is turned and stretched onto the old one, takes the old body's bone weights from its nearest points
# (a leg only from the same side), the weights are smoothed, and it is exported with the old skeleton and every clip.
import bpy, sys, math, mathutils, json, numpy as np
from mathutils.kdtree import KDTree
A = sys.argv
base, hd, out, prev = A[-5], A[-4], A[-3], A[-2]
OPT = json.loads(A[-1])
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=base)
arm = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE'][0]
acts = list(bpy.data.actions)
if arm.animation_data: arm.animation_data.action = None
for pb in arm.pose.bones: pb.matrix_basis = mathutils.Matrix.Identity(4)
bpy.context.view_layer.update()
for _o in list(bpy.context.scene.objects):
    if _o.type == 'MESH' and _o.name.startswith('Icosphere'): bpy.data.objects.remove(_o)
bmeshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
# the old body's points (rest pose) and weights by bone
bones = [b.name for b in arm.data.bones]; bi = {n: i for i, n in enumerate(bones)}
PB, WB = [], []
for o in bmeshes:
    gn = {g.index: g.name for g in o.vertex_groups}
    for v in o.data.vertices:
        PB.append(tuple(o.matrix_world @ v.co)); w = np.zeros(len(bones), np.float32)
        for g in v.groups:
            n = gn.get(g.group)
            if n in bi: w[bi[n]] += g.weight
        s = w.sum(); WB.append(w / s if s > 0 else w)
PB = np.array(PB); WB = np.array(WB)
lo, hi = PB.min(0), PB.max(0); ctr = (lo + hi) / 2
# which way the old animal faces (toward its head bone), and its long axis
hb = next((arm.matrix_world @ b.head_local for b in arm.data.bones if b.name.lower().startswith('head')), None)
ax = 0 if (hi - lo)[0] > (hi - lo)[1] else 1; lat = 1 - ax
fw = 1 if hb is None or hb[ax] > ctr[ax] else -1
print('base', base.split('/')[-1], 'verts', len(PB), 'size', np.round(hi - lo, 3), 'forward', 'xy'[ax], fw)

# the new body
before = set(bpy.context.scene.objects)
bpy.ops.import_scene.gltf(filepath=hd)
new = [o for o in bpy.context.scene.objects if o not in before]
nm = [o for o in new if o.type == 'MESH']
for o in new:
    if o.type != 'MESH': o.select_set(False)
bpy.ops.object.select_all(action='DESELECT')
for o in nm: o.select_set(True)
bpy.context.view_layer.objects.active = nm[0]
if len(nm) > 1: bpy.ops.object.join()
t = bpy.context.view_layer.objects.active; t.name = 'Body'
for o in new:
    if o != t and o.name in bpy.data.objects:
        try: bpy.data.objects.remove(o)
        except Exception: pass
mw = t.matrix_world.copy(); t.parent = None; t.matrix_world = mw
for _m in list(t.modifiers): t.modifiers.remove(_m)
t.vertex_groups.clear()
for o in list(bpy.context.scene.objects):
    if o in new and o.type == 'ARMATURE': bpy.data.objects.remove(o)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
nt = len(t.data.polygons); tris = OPT.get('tris', 24000)
if nt > tris:
    d = t.modifiers.new('dec', 'DECIMATE'); d.ratio = tris / nt; bpy.ops.object.modifier_apply(modifier='dec')
print('hd tris', nt, '->', len(t.data.polygons))
P = np.array([tuple(v.co) for v in t.data.vertices])
# its long horizontal axis (by spread), then fitted onto the old body's box; both ends tried, the closer fit kept
import itertools
ROTS = []
for perm in itertools.permutations(range(3)):
    for sg in itertools.product((1, -1), repeat=3):
        R = np.zeros((3, 3))
        for r_, (c_, s_) in enumerate(zip(perm, sg)): R[r_, c_] = s_
        if np.linalg.det(R) > 0: ROTS.append(R)
def fit(P, R):
    Q = P @ R.T; ql, qh = Q.min(0), Q.max(0)
    return lo + (Q - ql) / np.maximum(qh - ql, 1e-6) * (hi - lo)
kd = KDTree(len(PB))
for i, p in enumerate(PB): kd.insert(p, i)
kd.balance()
_rng = np.random.default_rng(0)
_pbs = PB[_rng.choice(len(PB), min(1500, len(PB)), replace=False)]
def cost(Q, both=True):
    idx = _rng.choice(len(Q), min(1500, len(Q)), replace=False)
    c1 = float(np.mean([kd.find(Q[i])[2] for i in idx]))
    if not both: return c1
    k2 = KDTree(len(idx))
    for n_, i in enumerate(idx): k2.insert(Q[i], n_)
    k2.balance()
    return c1 + float(np.mean([k2.find(p)[2] for p in _pbs]))
# the new body's own proportions must roughly match the old one's (long, wide, tall): only turns that keep them are tried
_ext = hi - lo
cands = []
for R in ROTS:
    e = np.abs(R @ (P.max(0) - P.min(0)))
    r = (e / e.max()) / (_ext / _ext.max())
    if np.all(r > 0.55) and np.all(r < 1.8): cands.append((cost(fit(P, R)), len(cands), R))
if not cands: cands = [(cost(fit(P, R)), i, R) for i, R in enumerate(ROTS)]
cands.sort(key=lambda c: c[0])
print('fit costs', [round(c[0], 4) for c in cands[:6]])
def refine(Q):
    Q = Q.copy()
    for it in range(OPT.get('icp', 8)):
        tgt = np.array([kd.find(q)[0] for q in Q])
        for a in range(3):
            x, y = Q[:, a], tgt[:, a]; A_ = np.c_[x, np.ones_like(x)]
            s = float(np.clip(np.linalg.lstsq(A_, y, rcond=None)[0][0], 0.85, 1.15)); Q[:, a] = x * s + (np.mean(y) - np.mean(x * s))
    return Q
# the best few turns are each refined, and the one that ends closest is kept (a wolf's head and tail end look much alike
# in a box, but not once the legs and the back are pulled onto the old body)
fins = []
for c_, i_, R in cands[:OPT.get('tries', 3)]:
    if 'force' in OPT and i_ != OPT['force']: continue
    Qr = refine(fit(P, R)); fins.append((cost(Qr), i_, Qr, R))
fins.sort(key=lambda f: f[0])
print('refined', [(round(f[0], 4), f[1], f[3].astype(int).tolist()) for f in fins])
Q = fins[0][2]
print('fit after', round(cost(Q), 4))
for i, v in enumerate(t.data.vertices): v.co = mathutils.Vector(Q[i])
t.data.update()
# weights: from the nearest points of the old body; away from the middle, only points on the same side (a leg never takes
# the other leg's bones)
half = (hi - lo)[lat] / 2; mid = ctr[lat]
W = np.zeros((len(Q), len(bones)), np.float32)
K = OPT.get('k', 8)
for i, q in enumerate(Q):
    side = q[lat] - mid; near = kd.find_n(q, K * 3); acc = np.zeros(len(bones), np.float32); tot = 0.0
    for co, j, d in near:
        if abs(side) > 0.18 * half and (PB[j][lat] - mid) * side < 0: continue
        w_ = 1.0 / (d + 0.004) ** 2; acc += WB[j] * w_; tot += w_
    if tot == 0:
        for co, j, d in near[:K]: w_ = 1.0 / (d + 0.004) ** 2; acc += WB[j] * w_; tot += w_
    W[i] = acc / max(tot, 1e-9)
# smooth over the new mesh
adj = [[] for _ in range(len(Q))]
for e in t.data.edges: a, b = e.vertices; adj[a].append(b); adj[b].append(a)
for it in range(OPT.get('smooth', 3)):
    W2 = W.copy()
    for i in range(len(Q)):
        if adj[i]: W2[i] = 0.5 * W[i] + 0.5 * W[adj[i]].mean(0)
    W = W2
# keep four per vertex
for i in range(len(Q)):
    r = W[i]; k4 = np.argsort(r)[-4:]; m = np.zeros_like(r); m[k4] = r[k4]; s = m.sum(); W[i] = m / s if s > 0 else m
for n in bones: t.vertex_groups.new(name=n)
for j, n in enumerate(bones):
    ix = np.nonzero(W[:, j] > 1e-4)[0]
    g = t.vertex_groups[n]
    for i in ix: g.add([int(i)], float(W[i, j]), 'REPLACE')
for o in bmeshes: bpy.data.objects.remove(o)
t.parent = arm; t.matrix_parent_inverse = arm.matrix_world.inverted(); md = t.modifiers.new('Armature', 'ARMATURE'); md.object = arm
# every clip of the old animal on the armature, for export
arm.animation_data_create()
for a in acts:
    a.use_fake_user = True
    tr = arm.animation_data.nla_tracks.new(); tr.name = a.name; st = tr.strips.new(a.name, int(a.frame_range[0]), a)
    try:
        if a.slots: st.action_slot = a.slots[0]
    except Exception: pass
arm.animation_data.action = None
bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True); t.select_set(True)
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', use_selection=True, export_skins=True, export_animations=True,
                          export_animation_mode='NLA_TRACKS', export_image_format='JPEG', export_jpeg_quality=88)
print('exported', out)
# pictures: rest, and clips mid-way, from the side and 3/4
from PIL import Image
sc = bpy.context.scene; sc.render.engine = 'BLENDER_WORKBENCH'; sc.display.shading.light = 'STUDIO'; sc.display.shading.color_type = 'TEXTURE'
wd = bpy.data.worlds.new('w'); sc.world = wd; wd.color = (0.75, 0.76, 0.8)
sc.render.resolution_x = 360; sc.render.resolution_y = 300
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
ext = float(max(hi - lo)); cam.data.type = 'ORTHO'; cam.data.ortho_scale = ext * 1.5; cam.data.clip_end = ext * 20
fwv = mathutils.Vector((0, 0, 0)); fwv[ax] = fw; sdv = mathutils.Vector((-fwv.y, fwv.x, 0)); C = mathutils.Vector(ctr)
ims = []
for tr in arm.animation_data.nla_tracks: tr.mute = True
def shot(yaw):
    d = (fwv * math.cos(math.radians(yaw)) + sdv * math.sin(math.radians(yaw))).normalized()
    cam.location = C + d * ext * 3 + mathutils.Vector((0, 0, ext * 0.4)); cam.rotation_euler = (C - cam.location).to_track_quat('-Z', 'Y').to_euler()
    p = '/tmp/claude-0/hd/_p.png'; sc.render.filepath = p; bpy.ops.render.render(write_still=True); ims.append(Image.open(p).convert('RGB').copy())
shot(90); shot(35)
for nm_ in OPT.get('clips', ['Gallop', 'Attack', 'Walk']):
    a = next((x for x in acts if x.name.startswith(nm_)), None)
    if a is None: continue
    arm.animation_data.action = a
    try: arm.animation_data.action_slot = a.slots[0]
    except Exception: pass
    sc.frame_set(int(a.frame_range[0] + (a.frame_range[1] - a.frame_range[0]) * 0.4)); shot(90); shot(35)
Wd = Image.new('RGB', (360 * 4, 300 * ((len(ims) + 3) // 4)))
for i, im in enumerate(ims): Wd.paste(im, (360 * (i % 4), 300 * (i // 4)))
Wd.save(prev, quality=85); print('preview', prev)
