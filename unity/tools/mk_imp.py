# a new, detailed imp body on the old imp's skeleton (keeps every animation: Idle, Walk, Run, Attack, Hit, Die)
import numpy as np, sys, json
sys.path.insert(0, '/tmp/kit')
from beast import Beast, lin, normals
SRC = '/mnt/user-data/uploads/AshenHollow/Assets/AshenHollow/Resources/AH/Models/Web/mDiablous.glb'
OUT = sys.argv[1] if len(sys.argv) > 1 else '/tmp/imp/mImp.glb'
b = Beast(SRC); g = b.g; j = g.j
orig = b.prims[0]; oP = orig['P']; oI = g.acc(orig['p']['indices']).reshape(-1, 3).astype(int); oJ = orig['J']; oW = orig['W']
JI = {n: k for k, n in enumerate(b.jn)}
J = {k: np.array(v) for k, v in b.J.items()}
A = np.array

def nrm(v): v = np.asarray(v, float); return v / (np.linalg.norm(v) + 1e-12)
def frame(d, up=(0, 1, 0)):
    d = nrm(d); a = np.array(up, float)
    if abs(np.dot(a, d)) > 0.95: a = np.array([1.0, 0, 0]) if abs(d[0]) < 0.9 else np.array([0, 0, 1.0])
    u = nrm(np.cross(a, d)); v = np.cross(d, u); return u, v
def cr(pts, n):  # catmull-rom through control points -> n samples
    P = [np.array(p, float) for p in pts]; P = [2 * P[0] - P[1]] + P + [2 * P[-1] - P[-2]]
    out = []; segs = len(P) - 3
    for i in range(n):
        t = i / (n - 1) * segs; k = min(int(t), segs - 1); f = t - k
        p0, p1, p2, p3 = P[k], P[k + 1], P[k + 2], P[k + 3]
        out.append(0.5 * ((2 * p1) + (-p0 + p2) * f + (2 * p0 - 5 * p1 + 4 * p2 - p3) * f * f + (-p0 + 3 * p1 - 3 * p2 + p3) * f ** 3))
    return np.array(out)

# ---------------- skinning ----------------
SEG = {}
def seg(name, a, b_): SEG[name] = (np.array(a, float), np.array(b_, float))
seg('pelvis', J['pelvis'], J['pelvis'] + [0, -1.2, -0.1])
seg('spine_1', J['spine_1'], J['spine_2'])
seg('spine_2', J['spine_2'], J['spine_3'])
seg('spine_3', J['spine_3'], J['neck'] + [0, -0.3, 0])
seg('neck', J['neck'], J['skull'])
seg('skull', J['skull'], J['skull'] + [0, 1.6, 0.2])
for s, sx in (('l', 1), ('r', -1)):
    seg('upper_arm_' + s, J['upper_arm_' + s], J['lower_arm_' + s])
    seg('lower_arm_' + s, J['lower_arm_' + s], J['palm_' + s])
    seg('palm_' + s, J['palm_' + s], J['fingers_' + s])
    seg('fingers_' + s, J['fingers_' + s], J['fingers_' + s] + [sx * 1.2, -0.2, 0])
    seg('thumb_' + s, J['thumb_' + s], J['thumb_' + s] + [sx * 0.5, -0.1, 0.6])
    seg('thigh_' + s, J['thigh_' + s], J['shin_' + s])
    seg('shin_' + s, J['shin_' + s], J['foot_' + s])
    seg('foot_' + s, J['foot_' + s], J['toes_' + s])
    seg('toes_' + s, J['toes_' + s], J['toes_' + s] + [0, -0.05, 1.2])
def dseg(P, a, b_):
    ab = b_ - a; t = np.clip(((P - a) @ ab) / (ab @ ab), 0, 1); return np.linalg.norm(P - (a + t[:, None] * ab), axis=1)
def skin(P, names, soft=0.35):
    D = np.stack([dseg(P, *SEG[n]) for n in names], 1)
    Wt = np.exp(-(D - D.min(1, keepdims=True)) / soft)
    o = np.argsort(-Wt, 1)[:, :4]; Wk = np.take_along_axis(Wt, o, 1); Wk[Wk < 0.03] = 0; Wk /= Wk.sum(1, keepdims=True)
    Jk = np.array([[JI[names[i]] for i in row] for row in o])
    if Jk.shape[1] < 4: pad = 4 - Jk.shape[1]; Jk = np.c_[Jk, np.zeros((len(P), pad), int)]; Wk = np.c_[Wk, np.zeros((len(P), pad))]
    return Jk, Wk
def rigid(P, name): Jk = np.zeros((len(P), 4), int); Jk[:, 0] = JI[name]; Wk = np.zeros((len(P), 4)); Wk[:, 0] = 1; return Jk, Wk
def mixw(P, pairs):  # pairs: list of (joint, weight array)
    Jk = np.zeros((len(P), 4), int); Wk = np.zeros((len(P), 4))
    for k, (n, w) in enumerate(pairs): Jk[:, k] = JI[n]; Wk[:, k] = w
    Wk /= Wk.sum(1, keepdims=True); return Jk, Wk
# closest point on the old model (copies its wing weights)
def cp_tri(p, a, b_, c):
    ab, ac, ap = b_ - a, c - a, p - a; d1, d2 = ab @ ap, ac @ ap
    if d1 <= 0 and d2 <= 0: return 1, 0, 0
    bp = p - b_; d3, d4 = ab @ bp, ac @ bp
    if d3 >= 0 and d4 <= d3: return 0, 1, 0
    vc = d1 * d4 - d3 * d2
    if vc <= 0 and d1 >= 0 and d3 <= 0: v = d1 / (d1 - d3); return 1 - v, v, 0
    cp = p - c; d5, d6 = ab @ cp, ac @ cp
    if d6 >= 0 and d5 <= d6: return 0, 0, 1
    vb = d5 * d2 - d1 * d6
    if vb <= 0 and d2 >= 0 and d6 <= 0: w = d2 / (d2 - d6); return 1 - w, 0, w
    va = d3 * d6 - d5 * d4
    if va <= 0 and (d4 - d3) >= 0 and (d5 - d6) >= 0: w = (d4 - d3) / ((d4 - d3) + (d5 - d6)); return 0, 1 - w, w
    den = 1 / (va + vb + vc); v = vb * den; w = vc * den; return 1 - v - w, v, w
def transfer(P, tris):
    Jk = np.zeros((len(P), 4), int); Wk = np.zeros((len(P), 4))
    for i, p in enumerate(P):
        best = None
        for t in tris:
            bc = cp_tri(p, *oP[t]); q = bc[0] * oP[t[0]] + bc[1] * oP[t[1]] + bc[2] * oP[t[2]]; d = np.linalg.norm(p - q)
            if best is None or d < best[0]: best = (d, t, bc)
        _, t, bc = best; acc = {}
        for c, vi in zip(bc, t):
            for k in range(4):
                if oW[vi, k] > 0: acc[oJ[vi, k]] = acc.get(oJ[vi, k], 0) + c * oW[vi, k]
        top = sorted(acc.items(), key=lambda x: -x[1])[:4]; s = sum(w for _, w in top)
        for k, (jj, w) in enumerate(top): Jk[i, k] = jj; Wk[i, k] = w / s
    return Jk, Wk

# ---------------- parts ----------------
MATS = []
def mat(name, hexc, rough=0.75, metal=0.0, emis=None, ds=False):
    m = {'name': name, 'pbrMetallicRoughness': {'baseColorFactor': lin(hexc) + [1.0], 'roughnessFactor': rough, 'metallicFactor': metal}}
    if emis is not None: m['emissiveFactor'] = lin(emis)
    if ds: m['doubleSided'] = True
    MATS.append(m); return len(MATS) - 1
M_SKIN = mat('imp_skin', 0xb8321f, 0.62)
M_DARK = mat('imp_dark', 0x3a0f0b, 0.7)
M_HORN = mat('imp_horn', 0x2a1c16, 0.55)
M_BONE = mat('imp_bone', 0xe9dcbc, 0.45)
M_EYE = mat('imp_eye', 0xffe070, 0.3, emis=0xffa418)
M_WING = mat('imp_wing', 0x7a2016, 0.8, ds=True)
M_LEATH = mat('imp_leather', 0x3b2a20, 0.85)
M_GOLD = mat('imp_gold', 0xd4a640, 0.35, 0.85)
M_MOUTH = mat('imp_mouth', 0x1c0404, 0.9)
PARTS = []  # dict(P, F, Jk, Wk, mat, C)
def add(V, F, w, m, shade=None, smooth=True):
    V = np.asarray(V, float); F = np.asarray(F, int).reshape(-1, 3)
    if not smooth: V = V[F.reshape(-1)]; F = np.arange(len(V)).reshape(-1, 3)
    Jk, Wk = w(V) if callable(w) else w
    if not smooth and not callable(w): pass
    C = np.ones(len(V)) if shade is None else np.clip(shade(V), 0.05, 1.2)
    PARTS.append(dict(P=V, F=F, J=Jk, W=Wk, m=m, C=C))

def loft(rings, cap0=None, cap1=None):
    n = len(rings[0]); V = list(np.vstack(rings)); F = []
    for i in range(len(rings) - 1):
        for k in range(n):
            a = i * n + k; b_ = i * n + (k + 1) % n; c = (i + 1) * n + (k + 1) % n; d = (i + 1) * n + k
            F += [[a, b_, c], [a, c, d]]
    if cap0 is not None:
        ci = len(V); V.append(np.asarray(cap0, float))
        for k in range(n): F.append([ci, (k + 1) % n, k])
    if cap1 is not None:
        ci = len(V); V.append(np.asarray(cap1, float)); base = (len(rings) - 1) * n
        for k in range(n): F.append([ci, base + k, base + (k + 1) % n])
    return np.array(V), np.array(F)
def orient(V, F, inside):  # flip faces whose normal points toward 'inside' point(s) (per face)
    V = np.asarray(V); F = np.array(F)
    cen = V[F].mean(1); fn = np.cross(V[F[:, 1]] - V[F[:, 0]], V[F[:, 2]] - V[F[:, 0]])
    ins = inside(cen) if callable(inside) else np.broadcast_to(np.asarray(inside, float), cen.shape)
    bad = ((cen - ins) * fn).sum(1) < 0; F[bad] = F[bad][:, ::-1]; return F
def tube(path, radii, n=8, up=(0, 1, 0), squash=1.0, cap0=True, cap1=True, prof=None):
    path = np.asarray(path, float); rings = []; axis = []
    for i, p in enumerate(path):
        d = path[min(i + 1, len(path) - 1)] - path[max(i - 1, 0)]; u, v = frame(d, up)
        r = radii[i]; ring = []
        for k in range(n):
            a = 2 * np.pi * k / n; rr = r * (prof(i, a) if prof else 1)
            ring.append(p + rr * np.cos(a) * u + rr * squash * np.sin(a) * v)
        rings.append(np.array(ring)); axis += [p] * n
    V, F = loft(rings, path[0] if cap0 else None, path[-1] if cap1 else None)
    ax = np.array(axis + ([path[0]] if cap0 else []) + ([path[-1]] if cap1 else []))
    # inside point per face: the axis point of its first vertex (caps: step back along the path)
    def inside(cen):
        k = np.argmin(((cen[:, None, :] - path[None]) ** 2).sum(2), 1); q = path[k].copy()
        ends = (k == 0) | (k == len(path) - 1)
        q[k == 0] = path[0] + (path[1] - path[0]) * 0.5; q[k == len(path) - 1] = path[-1] + (path[-2] - path[-1]) * 0.5
        return q
    return V, orient(V, F, inside)
def ell(c, r, n=10, rings=7, rot=None):
    c = np.asarray(c, float); V = []
    for i in range(rings + 1):
        th = np.pi * i / rings
        for k in range(n):
            ph = 2 * np.pi * k / n; d = np.array([np.sin(th) * np.cos(ph), np.cos(th), np.sin(th) * np.sin(ph)]) * r
            V.append(c + (rot @ d if rot is not None else d))
    F = []
    for i in range(rings):
        for k in range(n):
            a = i * n + k; b_ = i * n + (k + 1) % n; cc = (i + 1) * n + (k + 1) % n; d = (i + 1) * n + k
            F += [[a, cc, b_], [a, d, cc]]
    V = np.array(V); return V, orient(V, F, c)
def cone(base, tip, r, n=6, bend=None):
    base, tip = np.asarray(base, float), np.asarray(tip, float)
    mid = (base + tip) / 2 + (np.asarray(bend, float) if bend is not None else 0)
    path = cr([base, mid, tip], 5); return tube(path, [r, r * 0.75, r * 0.5, r * 0.25, 0.0], n, cap1=False)
def Rz(a): c, s = np.cos(a), np.sin(a); return np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]])
def Ry(a): c, s = np.cos(a), np.sin(a); return np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]])
def sstep(a, b_, x): t = np.clip((x - a) / (b_ - a), 0, 1); return t * t * (3 - 2 * t)

# ---- torso (hips to neck) ----
prof = np.array([  # y, half width, centre z, front depth, back depth
    [-1.7, 1.15, -0.10, 1.00, 1.00],
    [-1.0, 1.42, -0.05, 1.15, 1.10],
    [-0.2, 1.38, 0.05, 1.30, 1.05],
    [0.6, 1.22, 0.18, 1.42, 0.98],
    [1.4, 1.30, 0.22, 1.30, 1.00],
    [2.3, 1.72, 0.12, 1.22, 1.15],
    [3.1, 2.02, -0.05, 1.10, 1.20],
    [3.65, 1.65, -0.18, 0.92, 1.05],
    [4.0, 0.95, -0.20, 0.72, 0.78],
    [4.4, 0.78, -0.10, 0.66, 0.66],
    [4.8, 0.70, 0.0, 0.62, 0.62]])
NT = 18; rows = []
ys = np.linspace(prof[0, 0], prof[-1, 0], 17)
for y in ys:
    hw, zc, fd, bd = [np.interp(y, prof[:, 0], prof[:, k]) for k in (1, 2, 3, 4)]
    ring = []
    for k in range(NT):
        a = 2 * np.pi * k / NT; x = np.sin(a); z = np.cos(a)  # a=0 front
        dz = fd if z > 0 else bd
        r = 1.0
        # sternum and spine grooves, rib flare
        if 0.8 < y < 3.4: r -= 0.07 * np.exp(-(a ** 2 if a < np.pi else (2 * np.pi - a) ** 2) / 0.04)
        if 0.0 < y < 3.6: r -= 0.06 * np.exp(-((a - np.pi) ** 2) / 0.05)
        ring.append([hw * x * r, y, zc + dz * z * r])
    rows.append(np.array(ring))
V, F = loft(rows, cap0=[0, prof[0, 0] - 0.35, -0.1])
F = orient(V, F, lambda c: np.c_[np.zeros(len(c)), c[:, 1], np.interp(c[:, 1], prof[:, 0], prof[:, 2])])
TORSO_J = ['pelvis', 'spine_1', 'spine_2', 'spine_3', 'neck', 'skull']
def torso_w(P):
    Jk, Wk = skin(P, TORSO_J, 0.3)
    return Jk, Wk
def torso_shade(P):  # lighter belly, darker back, painted-in muscle lines
    x, y = P[:, 0], P[:, 1]; z = (P[:, 2] - np.interp(y, prof[:, 0], prof[:, 2])); front = sstep(0.2, 0.8, z)
    c = 0.82 + 0.22 * np.tanh(z * 0.9) - 0.12 * sstep(1.0, -1.6, y)
    pec = np.exp(-((y - (2.05 - 0.18 * np.abs(x))) / 0.16) ** 2) * sstep(0.15, 0.5, np.abs(x)) * sstep(1.7, 1.2, np.abs(x))
    abs_ = (np.exp(-((y - 1.25) / 0.09) ** 2) + np.exp(-((y - 0.6) / 0.09) ** 2) + np.exp(-((y - -0.05) / 0.09) ** 2)) * sstep(0.85, 0.5, np.abs(x)) * 0.7
    mid = np.exp(-(x / 0.09) ** 2) * sstep(-0.4, 0.2, y) * sstep(3.3, 2.6, y)
    return c - 0.32 * front * np.clip(pec + abs_ + mid, 0, 1)
add(V, F, torso_w, M_SKIN, torso_shade)

# ---- head ----
HC = np.array([0, 5.40, 0.30]); HR = np.array([1.22, 1.12, 1.15])
def headpos(d):
    d = nrm(d); x, y, z = d; p = HC + d * HR
    fz = max(z, 0)
    p[2] += 0.42 * fz * fz * sstep(0.15, -0.55, y)          # snout / jaw forward
    p[0] *= 1 + 0.10 * sstep(0.2, -0.3, y) * (1 - fz)       # cheeks
    p[1] -= 0.30 * fz * sstep(-0.45, -0.9, y)                # chin
    p[2] += 0.26 * fz ** 2 * np.exp(-((y - 0.28) / 0.13) ** 2) * (1 - 0.6 * np.exp(-(x / 0.12) ** 2))  # brow (dip in the middle)
    p[2] -= 0.22 * sstep(-0.2, -0.8, z) * (0.5 + 0.5 * y)    # skull back
    p[1] += 0.10 * sstep(0.3, 0.9, y) * (1 - abs(x))         # crown
    return p
NH, RH = 22, 16; V = []
for i in range(RH + 1):
    th = np.pi * i / RH
    for k in range(NH):
        ph = 2 * np.pi * k / NH; V.append(headpos([np.sin(th) * np.sin(ph), np.cos(th), np.sin(th) * np.cos(ph)]))
F = []
for i in range(RH):
    for k in range(NH):
        a = i * NH + k; b_ = i * NH + (k + 1) % NH; c = (i + 1) * NH + (k + 1) % NH; d = (i + 1) * NH + k; F += [[a, b_, c], [a, c, d]]
V = np.array(V); F = orient(V, F, HC)
add(V, F, lambda P: skin(P, ['neck', 'skull'], 0.25), M_SKIN, lambda P: 0.95 + 0.15 * np.tanh((P[:, 2] - HC[2]) * 1.5) - 0.15 * sstep(5.5, 4.5, P[:, 1]))
SK = lambda P: rigid(P, 'skull')
def hsurf(d, out=0.0):
    p = headpos(d); e = 1e-3; dd = nrm(d)
    # outward normal by finite differences on the sphere param
    t1 = nrm(np.cross(dd, [0, 1, 0]) if abs(dd[1]) < 0.95 else np.cross(dd, [1, 0, 0])); t2 = np.cross(dd, t1)
    n = nrm(np.cross(headpos(dd + t1 * e) - p, headpos(dd + t2 * e) - p))
    if n @ dd < 0: n = -n
    return p + n * out, n
# eyes: angry slanted glowing almonds under the brow, with dark sockets
for sx in (1, -1):
    p, n = hsurf([sx * 0.40, 0.12, 0.90], -0.02)
    rot = Rz(sx * 0.38) @ Ry(sx * 0.35)
    V, F = ell(p - n * 0.04, np.array([0.30, 0.18, 0.16]), 10, 6, rot); add(V, F, SK, M_MOUTH, smooth=True)
    V, F = ell(p + n * 0.02, np.array([0.24, 0.11, 0.11]), 10, 6, rot); add(V, F, SK, M_EYE)
    # nostrils
    p2, n2 = hsurf([sx * 0.13, -0.18, 1.0], -0.01); V, F = ell(p2, np.array([0.07, 0.05, 0.06]), 6, 4); add(V, F, SK, M_MOUTH)
# grin: a dark curved mouth with fangs
gp = []
for t in np.linspace(-1, 1, 13):
    p, n = hsurf([0.62 * t, -0.40 + 0.20 * t * t, 0.82], 0.0); gp.append(p)
gp = np.array(gp); V, F = tube(gp, [0.04] + [0.085] * 11 + [0.04], 6, squash=0.7); add(V, F, SK, M_MOUTH)
for t, L in ((-0.72, 0.16), (-0.42, 0.30), (-0.15, 0.13), (0.15, 0.13), (0.42, 0.30), (0.72, 0.16)):
    k = int(round((t + 1) / 2 * 12)); p = gp[k] + [0, 0.05, 0.02]
    V, F = cone(p, p + [0, -L, 0.05], 0.06 if L > 0.2 else 0.045, 5); add(V, F, SK, M_BONE)
for t in (-0.3, 0.3):  # lower tusks pointing up at the corners
    k = int(round((t + 1) / 2 * 12)); p = gp[k] + [0, -0.06, 0.02]; V, F = cone(p, p + [0, 0.17, 0.04], 0.04, 5); add(V, F, SK, M_BONE)
# ears: long pointed bat ears
for sx in (1, -1):
    base, n = hsurf([sx * 1.0, 0.18, -0.15], -0.18)
    path = cr([base, base + [sx * 0.7, 0.32, -0.12], base + [sx * 1.35, 0.62, -0.38], base + [sx * 1.75, 0.92, -0.62]], 7)
    rad = [0.42, 0.40, 0.34, 0.26, 0.17, 0.08, 0.0]
    V, F = tube(path, rad, 10, up=(0, 0, 1), squash=0.32, cap0=False, cap1=False)
    def eshade(P, base=base, sx=sx): return 0.9 - 0.35 * np.clip((P[:, 2] - base[2] - 0.0) * 3, 0, 1) * 0 + 0.0
    add(V, F, lambda P: skin(P, ['skull'], 1), M_SKIN, lambda P, base=base: 0.95 - 0.25 * np.clip(np.linalg.norm(P - base, axis=1) / 2.0, 0, 1))
    # inner ear: darker membrane just in front
    V2 = path[1:-1] + [0, 0, 0.05]
    VV, FF = tube(path[:-1] + np.array([0, 0, 0.04]), [0.30, 0.29, 0.24, 0.18, 0.11, 0.0], 8, up=(0, 0, 1), squash=0.12, cap0=False, cap1=False)
    add(VV, FF, SK, M_DARK)
# horns: thick ridged goat horns sweeping back, dark at the root, bone at the tips
for sx in (1, -1):
    base, n = hsurf([sx * 0.42, 0.82, 0.22], -0.12)
    ctrl = [base, base + [sx * 0.22, 0.75, -0.02], base + [sx * 0.70, 1.35, -0.42], base + [sx * 1.12, 1.62, -1.05], base + [sx * 1.25, 1.42, -1.72], base + [sx * 1.10, 1.02, -2.05]]
    path = cr(ctrl, 22); N = len(path); t = np.linspace(0, 1, N)
    rad = 0.40 * (1 - t) ** 0.85 * (1 + 0.07 * np.sin(t * 46)); rad[-1] = 0
    cut = 13
    V, F = tube(path[:cut + 1], rad[:cut + 1], 9, cap1=False); add(V, F, SK, M_HORN, lambda P, base=base: 0.8 + 0.3 * np.clip(np.linalg.norm(P - base, axis=1) / 2.2, 0, 1))
    V, F = tube(path[cut:], rad[cut:], 9, cap0=False, cap1=False); add(V, F, SK, M_BONE, lambda P: np.full(len(P), 0.9))
    # spiky crest between the horns
for z, h in ((0.05, 0.42), (-0.45, 0.36), (-0.9, 0.28)):
    p, n = hsurf([0, 0.9, z], -0.05); V, F = cone(p, p + n * h + [0, 0, -0.12], 0.12, 5); add(V, F, SK, M_HORN)

# ---- arms ----
for s, sx in (('l', 1), ('r', -1)):
    ua, la, pa = J['upper_arm_' + s], J['lower_arm_' + s], J['palm_' + s]
    ctrl = [ua + [-sx * 0.45, -0.05, 0.05], ua + [sx * 0.2, 0.0, 0], (ua + la) / 2 + [0, 0.02, 0], la + [-sx * 0.1, 0, 0], (la + pa) / 2 - [sx * 0.4, 0, 0], pa - [sx * 0.15, 0, 0]]
    path = cr(ctrl, 15); t = np.linspace(0, 1, 15)
    rad = np.interp(t, [0, 0.08, 0.22, 0.38, 0.5, 0.6, 0.75, 1], [0.70, 0.74, 0.60, 0.62, 0.44, 0.52, 0.44, 0.34])
    def aprof(i, a): return 1 + 0.10 * np.cos(a * 2)  # slightly flattened
    V, F = tube(path, rad, 12, up=(0, 0, 1), squash=0.92, cap0=False, cap1=True)
    nm = ['spine_3', 'upper_arm_' + s, 'lower_arm_' + s, 'palm_' + s]
    def armw(P, nm=nm, sx=sx):
        Jk, Wk = skin(P, nm, 0.28)
        return Jk, Wk
    add(V, F, armw, M_SKIN, lambda P, sx=sx: 0.95 - 0.40 * sstep(4.4, 6.6, P[:, 0] * sx))
    # bracer with a gold rim
    bp = cr([la + [sx * 0.75, 0, 0], la + [sx * 1.55, 0.1, 0.05], la + [sx * 2.15, 0.2, 0.15]], 5)
    V, F = tube(bp, [0.55, 0.54, 0.50, 0.45, 0.42], 12, up=(0, 0, 1), squash=0.92, cap0=False, cap1=False); add(V, F, lambda P, s=s: rigid(P, 'lower_arm_' + s), M_LEATH)
    for q, rr in ((bp[0], 0.57), (bp[-1], 0.44)):
        V, F = tube(np.array([q - (bp[1] - bp[0]) * 0.12, q + (bp[1] - bp[0]) * 0.12]), [rr, rr], 12, up=(0, 0, 1), squash=0.92); add(V, F, lambda P, s=s: rigid(P, 'lower_arm_' + s), M_GOLD)
    # elbow spike and shoulder spikes
    e = la + [0, 0.05, -0.35]; V, F = cone(e, e + [sx * -0.2, 0.05, -0.75], 0.17, 6, bend=[0, 0.12, 0]); add(V, F, lambda P, s=s: rigid(P, 'lower_arm_' + s), M_HORN)
    # hand: palm, three clawed fingers and a thumb
    pal = cr([pa - [sx * 0.2, 0, 0], pa + [sx * 0.35, 0, 0], pa + [sx * 0.8, -0.02, 0]], 4)
    V, F = tube(pal, [0.36, 0.42, 0.40, 0.34], 10, up=(0, 1, 0), squash=0.55); add(V, F, lambda P, s=s: skin(P, ['lower_arm_' + s, 'palm_' + s, 'fingers_' + s], 0.2), M_SKIN, lambda P: np.full(len(P), 0.55))
    fb = J['fingers_' + s]
    for dz in (-0.3, 0.0, 0.3):
        k0 = fb + [-sx * 0.05, 0.0, dz]
        fp = cr([k0, k0 + [sx * 0.45, -0.02, dz * 0.15], k0 + [sx * 0.85, -0.18, dz * 0.2], k0 + [sx * 1.05, -0.38, dz * 0.25]], 6)
        V, F = tube(fp, [0.15, 0.15, 0.13, 0.12, 0.11, 0.10], 7, cap0=True, cap1=True); add(V, F, lambda P, s=s: rigid(P, 'fingers_' + s), M_SKIN, lambda P: np.full(len(P), 0.45))
        tip = fp[-1]; V, F = cone(tip, tip + [sx * 0.25, -0.38, 0.0], 0.09, 5, bend=[sx * 0.06, 0, 0]); add(V, F, lambda P, s=s: rigid(P, 'fingers_' + s), M_BONE)
    th = J['thumb_' + s]
    tp = cr([th - [sx * 0.15, 0, 0.1], th + [sx * 0.15, -0.08, 0.3], th + [sx * 0.35, -0.18, 0.62]], 5)
    V, F = tube(tp, [0.16, 0.15, 0.14, 0.12, 0.11], 7); add(V, F, lambda P, s=s: rigid(P, 'thumb_' + s), M_SKIN, lambda P: np.full(len(P), 0.45))
    V, F = cone(tp[-1], tp[-1] + [sx * 0.1, -0.3, 0.2], 0.08, 5); add(V, F, lambda P, s=s: rigid(P, 'thumb_' + s), M_BONE)

# ---- legs ----
for s, sx in (('l', 1), ('r', -1)):
    th, sh, ft, to = J['thigh_' + s], J['shin_' + s], J['foot_' + s], J['toes_' + s]
    ctrl = [th + [-sx * 0.15, 0.55, 0.05], th + [0, -0.4, 0.05], (th + sh) / 2 + [sx * 0.05, 0, 0.08], sh + [0, 0, 0.05], sh + [0, -1.4, -0.05], ft + [0, 0.45, 0.05], ft + [0, -0.25, 0.1]]
    path = cr(ctrl, 18); t = np.linspace(0, 1, 18)
    rad = np.interp(t, [0, 0.1, 0.28, 0.45, 0.52, 0.62, 0.8, 1], [1.0, 1.0, 0.88, 0.58, 0.55, 0.66, 0.40, 0.36])
    def lprof(i, a, t=t): # calf bulges at the back
        return 1 + 0.18 * max(0, -np.cos(a)) * np.exp(-((t[i] - 0.63) / 0.1) ** 2) + 0.06 * np.cos(2 * a)
    V, F = tube(path, rad, 12, up=(0, 0, 1), prof=lambda i, a: 1 + 0.16 * max(0, np.sin(a)) * np.exp(-((t[i] - 0.63) / 0.1) ** 2), cap0=False, cap1=True)
    add(V, F, lambda P, s=s: skin(P, ['pelvis', 'thigh_' + s, 'shin_' + s, 'foot_' + s], 0.32), M_SKIN, lambda P: 0.95 - 0.45 * sstep(-6.0, -9.0, P[:, 1]))
    # knee spike
    # foot: heel to ball, then three clawed toes
    fp = cr([ft + [0, -0.35, -0.55], ft + [0, -0.5, 0.0], to + [0, -0.05, -0.1], to + [0, -0.08, 0.35]], 5)
    V, F = tube(fp, [0.38, 0.48, 0.5, 0.46, 0.36], 10, up=(0, 1, 0), squash=0.62)
    add(V, F, lambda P, s=s: skin(P, ['foot_' + s, 'toes_' + s], 0.25), M_SKIN, lambda P: np.full(len(P), 0.45))
    for dx in (-0.32, 0.0, 0.32):
        k0 = to + [dx * sx, -0.1, 0.2]
        tp = cr([k0, k0 + [dx * 0.25 * sx, -0.02, 0.45], k0 + [dx * 0.4 * sx, -0.08, 0.8]], 5)
        V, F = tube(tp, [0.2, 0.19, 0.17, 0.15, 0.13], 7); add(V, F, lambda P, s=s: rigid(P, 'toes_' + s), M_SKIN, lambda P: np.full(len(P), 0.4))
        V, F = cone(tp[-1], tp[-1] + [0, -0.12, 0.38], 0.12, 5, bend=[0, 0.06, 0]); add(V, F, lambda P, s=s: rigid(P, 'toes_' + s), M_BONE)
    # spur at the heel
    hp = ft + [0, -0.3, -0.8]; V, F = cone(hp, hp + [0, 0.1, -0.45], 0.12, 5); add(V, F, lambda P, s=s: rigid(P, 'foot_' + s), M_HORN)

# ---- tail with a spade tip ----
tctrl = [[0, -0.6, -0.6], [0, -1.4, -1.6], [0, -2.8, -2.5], [0.35, -4.3, -3.0], [0.95, -5.5, -2.6], [1.45, -6.1, -1.8]]
path = cr(tctrl, 16); t = np.linspace(0, 1, 16)
V, F = tube(path, 0.42 * (1 - t) ** 0.8 + 0.08, 8, cap0=False, cap1=True)
add(V, F, lambda P: rigid(P, 'pelvis'), M_SKIN, lambda P: 0.8 - 0.25 * sstep(-2, -6, P[:, 1]))
E = path[-1]; d = nrm(path[-1] - path[-2]); side = nrm(np.cross(d, [1, 0, 0.6])); up = nrm(np.cross(side, d))
pts = [E + d * 0.95, E + side * 0.5 + d * 0.15, E - d * 0.1, E - side * 0.5 + d * 0.15]
V = np.array(pts + [E + d * 0.3 + up * 0.1, E + d * 0.3 - up * 0.1]); F = [[4, 0, 1], [4, 1, 2], [4, 2, 3], [4, 3, 0], [5, 1, 0], [5, 2, 1], [5, 3, 2], [5, 0, 3]]
F = orient(V, F, E + d * 0.3); add(V, F, lambda P: rigid(P, 'pelvis'), M_HORN, smooth=False)
# back ridge spikes
for y, h in ((0.4, 0.35), (1.3, 0.45), (2.2, 0.5), (3.0, 0.45)):
    zc = np.interp(y, prof[:, 0], prof[:, 2]) - np.interp(y, prof[:, 0], prof[:, 4]) + 0.12
    p = np.array([0, y, zc]); V, F = cone(p, p + [0, 0.32, -h], 0.16, 5)
    add(V, F, lambda P: skin(P, ['spine_1', 'spine_2', 'spine_3'], 0.3), M_HORN)

# ---- loincloth: belt, buckle, front and back flaps ----
by = -0.75
bp = []
for k in range(20):
    a = 2 * np.pi * k / 20; bp.append([1.48 * np.sin(a), by + 0.05 * np.cos(a), -0.05 + 1.22 * np.cos(a)])
V, F = tube(np.array(bp + [bp[0]]), [0.17] * 21, 6, cap0=False, cap1=False); add(V, F, lambda P: rigid(P, 'pelvis'), M_LEATH)
V, F = ell([0, by + 0.05, 1.22], np.array([0.30, 0.28, 0.1]), 10, 6); add(V, F, lambda P: rigid(P, 'pelvis'), M_GOLD)
V, F = cone([0, by + 0.05, 1.28], [0, by + 0.05, 1.45], 0.1, 5); add(V, F, lambda P: rigid(P, 'pelvis'), M_BONE)
for front in (1, -1):
    rows = []
    for i, y in enumerate(np.linspace(by - 0.1, -3.3, 6)):
        f = i / 5; hw = 0.62 - 0.12 * f
        z0 = front * (1.25 + 0.10 * f) - 0.05
        row = [[x, y + (0.12 * f * (1 - (x / hw) ** 2) if i == 5 else 0) * -1, z0 + front * 0.05 * (1 - (x / hw) ** 2)] for x in np.linspace(-hw, hw, 5)]
        rows.append(np.array(row))
    # jagged hem
    rows[-1][1::2, 1] += 0.22
    V = np.vstack(rows); F = []
    for i in range(5):
        for k in range(4):
            a = i * 5 + k; F += [[a, a + 1, a + 6], [a, a + 6, a + 5]]
    F = orient(V, np.array(F), lambda c, front=front: c - [0, 0, front])
    def flapw(P):
        f = sstep(by, -3.2, P[:, 1]); return mixw(P, [('pelvis', 1 - 0.6 * f), ('thigh_l', 0.3 * f + 1e-4), ('thigh_r', 0.3 * f + 1e-4)])
    MATS[M_LEATH]['doubleSided'] = True
    add(V, F, flapw, M_LEATH, lambda P: 1.0 - 0.3 * sstep(-1, -3.2, P[:, 1]))

# ---- wings: bat wings folded up behind the shoulders (weights copied from the old wing) ----
for s, sx in (('l', 1), ('r', -1)):
    side = [ti for ti in oI if (oP[ti][:, 0].mean() * sx > 0.3) and b.infl(orig, ['wing_'])[ti].max() > 0.2]
    X = lambda p: np.array([p[0] * sx, p[1], p[2]])
    S = X([0.95, 3.1, -1.4]); El = X([1.55, 6.9, -2.45]); Wr = X([1.85, 9.75, -2.95])
    tips = [X([4.95, 0.1, -2.2]), X([4.7, -4.5, -2.0]), X([4.05, -8.1, -1.8]), X([2.55, -7.0, -2.9])]
    inner = X([1.85, -3.0, -3.05])
    # leading arm bone and fingers
    arm = cr([S, El, Wr], 7); V, F = tube(arm, np.linspace(0.30, 0.20, 7), 8)
    add(V, F, lambda P, side=side: transfer(P, side), M_DARK, lambda P: np.full(len(P), 1.0))
    V, F = cone(Wr, Wr + X([0.25, 0.65, 0.35]), 0.16, 6, bend=X([0.05, 0.1, -0.1])); add(V, F, lambda P, side=side: transfer(P, side), M_BONE)
    fingers = []
    for tp in tips:
        mid = Wr + (tp - Wr) * 0.5 + X([0.35, 0, -0.15]); fp = cr([Wr, mid, tp], 9); fingers.append(fp)
        V, F = tube(fp, np.linspace(0.16, 0.05, 9), 6)
        add(V, F, lambda P, side=side: transfer(P, side), M_DARK)
    # membrane: rows from the wrist out to a scalloped trailing edge
    edge = []
    def scal(a, c, n=4, depth=0.22):
        out = []
        for k in range(n):
            f = k / n; p = a + (c - a) * f; p = p + (Wr - p) * depth * np.sin(np.pi * f); out.append(p)
        return out
    chain = [fingers[0][4]] + []  # start partway up the first finger
    edge += [fingers[0][4 + k] for k in range(5)][:-1]
    for a, c in zip(tips[:-1], tips[1:]): edge += scal(a, c)
    edge += scal(tips[-1], inner, 4, 0.15) + scal(inner, S, 5, 0.12) + [arm[k] for k in range(0, 6)]
    edge = np.array(edge); NR = 4; V = [Wr]; F = []
    for r in range(1, NR + 1):
        f = r / NR
        for p in edge:
            q = Wr + (p - Wr) * f
            q = q + X([0.32, 0, -0.30]) * np.sin(np.pi * f) * 0.9   # billow outward
            V.append(q)
    n = len(edge)
    for k in range(n - 1): F.append([0, 1 + k, 1 + k + 1])
    for r in range(NR - 1):
        for k in range(n - 1):
            a = 1 + r * n + k; b_ = a + 1; c = a + n + 1; d = a + n; F += [[a, d, c], [a, c, b_]]
    V = np.array(V); F = orient(V, np.array(F), lambda c, sx=sx: c - np.array([sx * 3.0, 0, 1.5]))
    add(V, F, lambda P, side=side: transfer(P, side), M_WING, lambda P: 0.75 + 0.35 * np.clip(np.linalg.norm(P - Wr, axis=1) / 14, 0, 1))

# ---------------- write ----------------
mi = orig['m']; prims = []
for pt in PARTS:
    Pw = pt['P']; Pb = (np.c_[Pw, np.ones(len(Pw))] @ b.Bi.T)[:, :3]; I = pt['F'].reshape(-1)
    Nn = normals(Pb, I)
    C = np.c_[np.repeat(pt['C'][:, None], 3, 1), np.ones(len(Pw))].astype(np.float32)
    Jk = pt['J'].astype(np.uint16); Wk = pt['W'].astype(np.float32)
    prims.append(dict(m=pt['m'], P=Pb.astype(np.float32), N=Nn.astype(np.float32), C=C, J=Jk, W=Wk, I=I.astype(np.uint32)))
# merge by material (fewer draw calls)
out = []
for m in range(len(MATS)):
    ps = [p for p in prims if p['m'] == m]
    if not ps: continue
    off = 0; P, N, C, Jk, Wk, I = [], [], [], [], [], []
    for p in ps: P.append(p['P']); N.append(p['N']); C.append(p['C']); Jk.append(p['J']); Wk.append(p['W']); I.append(p['I'] + off); off += len(p['P'])
    P, N, C, Jk, Wk, I = map(np.concatenate, (P, N, C, Jk, Wk, I))
    out.append({'attributes': {'POSITION': g.add(P, 'VEC3', minmax=True, target=34962), 'NORMAL': g.add(N, 'VEC3', target=34962), 'COLOR_0': g.add(C, 'VEC4', target=34962),
                               'JOINTS_0': g.add(Jk, 'VEC4', 5123, target=34962), 'WEIGHTS_0': g.add(Wk, 'VEC4', target=34962)}, 'indices': g.add(I, 'SCALAR', 5125, target=34963), 'material': m})
j['meshes'][mi]['primitives'] = out
j['materials'] = MATS
for k in ('images', 'textures', 'samplers'): j.pop(k, None)
j['meshes'][mi]['name'] = 'Imp'
g.save(OUT)
print('tris', sum(len(p['F']) for p in PARTS), 'prims', len(out))
