# Rig a static frog/toad mesh: a 16-bone skeleton found from its own shape (feet = the lowest vertices in each
# quarter), capsule-distance skinning, and keyframed clips Idle / Walk (a hop) / Attack / HitReact / Death.
# usage: python3 frogrig.py in.glb out.glb forward(+z|+x|-x|-z)
import sys, json, math, numpy as np
sys.path.insert(0, '/tmp/kit')
from gl import GLB

src, dst, fwd = sys.argv[1], sys.argv[2], sys.argv[3]
m = GLB(src)
prim = m.j['meshes'][0]['primitives'][0]
P = m.acc(prim['attributes']['POSITION']).astype(np.float64)

# canonical frame: x right, y up, z forward. A maps canonical -> mesh (a turn about y)
yaw = {'+z': 0.0, '+x': 90.0, '-z': 180.0, '-x': -90.0}[fwd]
def roty(d):
    a = math.radians(d); c, s = math.cos(a), math.sin(a)
    return np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]])
A = roty(yaw); Ai = A.T
C = P @ Ai.T            # vertices in the canonical frame
lo, hi = C.min(0), C.max(0); size = hi - lo
H, W, L = size[1], size[0], size[2]
ymin = lo[1]; zc = (lo[2] + hi[2]) / 2; xc = (lo[0] + hi[0]) / 2

# feet: the lowest 8% of the body, split into four quarters around the middle
low = C[C[:, 1] < ymin + 0.10 * H]
zmid = np.median(low[:, 2])
feet = {}
for side, sx in (('L', -1), ('R', 1)):
    for end, front in (('F', True), ('B', False)):
        q = low[((low[:, 0] - xc) * sx > 0.05 * W) & ((low[:, 2] > zmid) == front)]
        if len(q) < 5: q = low[((low[:, 0] - xc) * sx > 0)]
        feet[side + end] = q.mean(0)

def V(x, y, z): return np.array([x, y, z], float)
J = {}   # joint positions, canonical
J['root'] = V(xc, ymin, zc)
J['body'] = V(xc, ymin + 0.45 * H, zc - 0.05 * L)
J['pelvis'] = V(xc, ymin + 0.42 * H, zc - 0.28 * L)
J['chest'] = V(xc, ymin + 0.50 * H, zc + 0.12 * L)
J['head'] = V(xc, ymin + 0.62 * H, zc + 0.32 * L)
for side, sx in (('L', -1), ('R', 1)):
    fb, ff = feet[side + 'B'], feet[side + 'F']
    hip = V(xc + sx * 0.20 * W, ymin + 0.40 * H, zc - 0.28 * L)
    knee = V((hip[0] + fb[0]) / 2 + sx * 0.08 * W, ymin + 0.30 * H, max(hip[2], fb[2]) + 0.10 * L)
    ankle = V(fb[0], ymin + 0.10 * H, fb[2] + 0.02 * L)
    J['hip' + side], J['knee' + side], J['ankle' + side] = hip, knee, ankle
    sh = V(xc + sx * 0.18 * W, ymin + 0.45 * H, zc + 0.20 * L)
    el = V((sh[0] + ff[0]) / 2 + sx * 0.04 * W, ymin + 0.22 * H, (sh[2] + ff[2]) / 2)
    wr = V(ff[0], ymin + 0.06 * H, ff[2])
    J['shoulder' + side], J['elbow' + side], J['wrist' + side] = sh, el, wr

parent = {'root': None, 'body': 'root', 'pelvis': 'body', 'chest': 'body', 'head': 'chest'}
for s in 'LR':
    parent.update({'hip' + s: 'pelvis', 'knee' + s: 'hip' + s, 'ankle' + s: 'knee' + s,
                   'shoulder' + s: 'chest', 'elbow' + s: 'shoulder' + s, 'wrist' + s: 'elbow' + s})
names = list(parent.keys())

# ---- skinning: capsule distance to each bone's segment (bone -> its child, or a short stub) ----
child_of = {}
for n, p in parent.items():
    if p is not None and p not in child_of: child_of[p] = n
tip = {'head': J['head'] + V(0, 0.02 * H, 0.25 * L), 'ankleL': feet['LB'] + V(0, 0, 0.08 * L), 'ankleR': feet['RB'] + V(0, 0, 0.08 * L),
       'wristL': feet['LF'] + V(0, 0, 0.05 * L), 'wristR': feet['RF'] + V(0, 0, 0.05 * L), 'pelvis': J['pelvis'] + V(0, 0, -0.2 * L)}
seg = {}
for n in names:
    if n in ('root',): continue
    a = J[n]; b = tip.get(n, J[child_of[n]] if n in child_of else a + V(0, 0.01, 0))
    if n == 'body': b = J['chest']
    if n == 'chest': b = J['head']
    seg[n] = (a, b)
rad = {'body': 0.30, 'pelvis': 0.30, 'chest': 0.28, 'head': 0.22}
def segdist(p, a, b):
    ab = b - a; t = np.clip(((p - a) @ ab) / max(1e-9, ab @ ab), 0, 1)
    return np.linalg.norm(p - (a + np.outer(t, ab)), axis=1)
bn = [n for n in names if n in seg]
D = np.stack([np.maximum(0, segdist(C, *seg[n]) - rad.get(n, 0.05) * min(W, H)) for n in bn], 1)
Wt = 1.0 / (D + 0.02 * H) ** 4
top = np.argsort(-Wt, 1)[:, :4]
w4 = np.take_along_axis(Wt, top, 1); w4 /= w4.sum(1, keepdims=True)
jidx = np.array([names.index(bn[k]) for k in range(len(bn))])[top]

# ---- nodes: bones with identity rest rotation, in mesh space ----
base = len(m.j['nodes'])
Jm = {n: A @ J[n] for n in names}
for n in names:
    p = parent[n]; t = Jm[n] - (Jm[p] if p else 0)
    m.j['nodes'].append({'name': 'frog_' + n, 'translation': [float(x) for x in t]})
for n in names:
    kids = [base + names.index(c) for c, p in parent.items() if p == n]
    if kids: m.j['nodes'][base + names.index(n)]['children'] = kids
root_node = base + names.index('root')
mesh_node = 0
m.j['scenes'][0]['nodes'] = [mesh_node, root_node]
ibm = np.stack([np.linalg.inv(np.block([[np.eye(3), Jm[n].reshape(3, 1)], [np.zeros((1, 3)), np.ones((1, 1))]])).T.reshape(-1) for n in names]).astype(np.float32)
m.j.setdefault('skins', []).append({'joints': [base + i for i in range(len(names))], 'skeleton': root_node, 'inverseBindMatrices': m.add(ibm, 'MAT4')})
m.j['nodes'][mesh_node]['skin'] = len(m.j['skins']) - 1
prim['attributes']['JOINTS_0'] = m.add(jidx.astype(np.uint16), 'VEC4', 5123, target=34962)
prim['attributes']['WEIGHTS_0'] = m.add(w4.astype(np.float32), 'VEC4', 5126, target=34962)

# ---- clips ----
def quat_axis(ax, deg):
    a = math.radians(deg) / 2; v = np.array(ax, float); v /= np.linalg.norm(v)
    return np.array([*(v * math.sin(a)), math.cos(a)])
def qmul(a, b):
    x1, y1, z1, w1 = a; x2, y2, z2, w2 = b
    return np.array([w1*x2 + x1*w2 + y1*z2 - z1*y2, w1*y2 - x1*z2 + y1*w2 + z1*x2, w1*z2 + x1*y2 - y1*x2 + z1*w2, w1*w2 - x1*x2 - y1*y2 - z1*z2])
qA = quat_axis([0, 1, 0], yaw); qAi = qA * np.array([-1, -1, -1, 1])
def canon(q): return qmul(qmul(qA, q), qAi)   # a canonical-frame turn, as the mesh frame sees it
def euler(xd=0, yd=0, zd=0):
    return canon(qmul(qmul(quat_axis([0, 1, 0], yd), quat_axis([1, 0, 0], xd)), quat_axis([0, 0, 1], zd)))
def lerp(a, b, t): return a + (b - a) * t
def smooth(t): return t * t * (3 - 2 * t)
def keys(pts, t):
    # piecewise smooth interpolation through (time, value) pairs
    for i in range(len(pts) - 1):
        t0, v0 = pts[i]; t1, v1 = pts[i + 1]
        if t <= t1: return lerp(v0, v1, smooth((t - t0) / max(1e-6, t1 - t0)))
    return pts[-1][1]

FPS = 30
def clip(name, dur, pose, loop=True):
    n = int(round(dur * FPS)) + 1; ts = np.linspace(0, dur, n).astype(np.float32)
    tin = m.add(ts.reshape(-1, 1), 'SCALAR', minmax=True)
    chans, samps = [], []
    rots = {b: [] for b in names}; trs = []; scs = {b: [] for b in names}
    for t in ts:
        f = float(t) / dur
        R, T, S = pose(f)
        for b in names: rots[b].append(R.get(b, (0, 0, 0)))
        trs.append(T); [scs[b].append(S.get(b, (1, 1, 1))) for b in names]
    for b in names:
        q = np.array([euler(*e) for e in rots[b]], np.float32)
        if np.allclose(q, q[0]) and np.allclose(q[0], [0, 0, 0, 1], atol=1e-6) and b != 'root': continue
        samps.append({'input': tin, 'output': m.add(q, 'VEC4'), 'interpolation': 'LINEAR'})
        chans.append({'sampler': len(samps) - 1, 'target': {'node': base + names.index(b), 'path': 'rotation'}})
        s = np.array(scs[b], np.float32)
        if not np.allclose(s, 1):
            samps.append({'input': tin, 'output': m.add(s, 'VEC3'), 'interpolation': 'LINEAR'})
            chans.append({'sampler': len(samps) - 1, 'target': {'node': base + names.index(b), 'path': 'scale'}})
    tr = np.array([Jm['root'] + A @ np.array(v, float) for v in trs], np.float32)
    samps.append({'input': tin, 'output': m.add(tr, 'VEC3'), 'interpolation': 'LINEAR'})
    chans.append({'sampler': len(samps) - 1, 'target': {'node': root_node, 'path': 'translation'}})
    m.j.setdefault('animations', []).append({'name': name, 'channels': chans, 'samplers': samps})

def legs(R, thigh, shin, foot, arm, fore, hand, spread=0.0):
    # thigh/arm: + swings the limb back and down; legs mirrored side to side only in their splay (y turn)
    for s, sg in (('L', -1), ('R', 1)):
        R['hip' + s] = (thigh, sg * spread, 0); R['knee' + s] = (shin, 0, 0); R['ankle' + s] = (foot, 0, 0)
        R['shoulder' + s] = (arm, 0, 0); R['elbow' + s] = (fore, 0, 0); R['wrist' + s] = (hand, 0, 0)

def idle(f):
    a = math.sin(f * 2 * math.pi); R = {}; legs(R, 0, 0, 0, 0, 0, 0)
    R['head'] = (a * 2.5, math.sin(f * 4 * math.pi) * 4, 0)
    return R, (0, 0.006 * H * (a * 0.5 + 0.5), 0), {'chest': (1 + 0.025 * (a * 0.5 + 0.5), 1 + 0.04 * (a * 0.5 + 0.5), 1)}

def hop(f):
    # 0..0.10 spring, ..0.45 flying with legs trailing, ..0.55 reaching to land, then sitting, gathering at the end
    R = {}
    th = keys([(0, 8), (0.10, 95), (0.40, 85), (0.52, 10), (0.60, 0), (0.85, 0), (1.0, 8)], f)
    sh = keys([(0, -10), (0.10, -70), (0.40, -60), (0.52, -5), (0.60, 0), (0.85, 0), (1.0, -10)], f)
    ft = keys([(0, 0), (0.10, 40), (0.40, 30), (0.55, 0), (1.0, 0)], f)
    ar = keys([(0, 0), (0.10, 35), (0.35, 30), (0.48, -40), (0.56, -10), (0.65, 0), (1.0, 0)], f)
    fo = keys([(0, 0), (0.10, 20), (0.35, 15), (0.48, -15), (0.6, 0), (1.0, 0)], f)
    legs(R, th, sh, ft, ar, fo, 0)
    R['body'] = (keys([(0, 4), (0.10, -18), (0.35, -6), (0.52, 10), (0.62, 0), (1.0, 4)], f), 0, 0)
    R['head'] = (keys([(0, 0), (0.10, 6), (0.52, -6), (0.62, 0), (1.0, 0)], f), 0, 0)
    return R, (0, keys([(0, -0.03 * H), (0.6, 0), (0.9, 0), (1.0, -0.03 * H)], f), 0), {}

def attack(f):
    R = {}; legs(R, keys([(0, 0), (0.3, 30), (0.6, 0), (1, 0)], f), keys([(0, 0), (0.3, -20), (0.6, 0), (1, 0)], f), 0,
                 keys([(0, 0), (0.25, -20), (0.45, 10), (1, 0)], f), 0, 0)
    R['body'] = (keys([(0, 0), (0.25, -14), (0.45, 8), (1, 0)], f), 0, 0)
    R['head'] = (keys([(0, 0), (0.25, -28), (0.42, 14), (0.7, 0), (1, 0)], f), 0, 0)
    return R, (0, keys([(0, 0), (0.25, 0.06 * H), (0.45, 0), (1, 0)], f), keys([(0, 0), (0.3, 0.22 * L), (0.5, 0.18 * L), (1, 0)], f)), {}

def hitreact(f):
    R = {}; legs(R, 0, 0, 0, keys([(0, 0), (0.3, 15), (1, 0)], f), 0, 0)
    R['body'] = (keys([(0, 0), (0.25, -12), (1, 0)], f), 0, keys([(0, 0), (0.25, 6), (1, 0)], f))
    R['head'] = (keys([(0, 0), (0.25, -16), (1, 0)], f), 0, 0)
    return R, (0, 0, keys([(0, 0), (0.25, -0.08 * L), (1, 0)], f)), {}

def death(f):
    # a jolt, a hop up as it rolls onto its back, a bounce, legs curling up and a last twitch
    R = {}
    curl = keys([(0, 0), (0.35, 40), (0.7, 70), (0.85, 60), (0.92, 75), (1, 70)], f)
    legs(R, -curl * 0.6, curl, curl * 0.5, -curl * 0.8, curl * 0.6, 0)
    roll = keys([(0, 0), (0.12, -10), (0.55, 185), (0.68, 175), (0.8, 180), (1, 180)], f)
    R['root'] = (0, 0, roll)
    R['head'] = (keys([(0, 0), (0.15, -25), (0.6, 10), (1, 15)], f), 0, 0)
    lift = keys([(0, 0), (0.12, 0.05 * H), (0.38, 1.25 * H), (0.55, 0.82 * H), (0.66, 0.92 * H), (0.78, 0.8 * H), (1, 0.8 * H)], f)
    return R, (0, lift, 0), {}

clip('Idle', 2.4, idle)
clip('Walk', 0.95, hop)
clip('Attack', 0.7, attack, False)
clip('HitReact', 0.4, hitreact, False)
clip('Death', 1.1, death, False)
m.save(dst)
print('rigged', dst, 'bones', len(names), 'feet', {k: [round(x, 2) for x in v] for k, v in feet.items()}, 'size', size.round(2))
