# Hellish imp (Sketchfab, CC-BY 4.0 by GAMEBR0VIP): decimated, made decent, rigged to the Quaternius humanoid skeleton
import sys, io, json, numpy as np
sys.path.insert(0, '/tmp/kit'); sys.path.insert(0, '/tmp/himp')
from gl import GLB
from beast import lin, normals
exec(open('/tmp/himp/joints.py').read())
RIG = '/mnt/user-data/uploads/AshenHollow/Assets/AshenHollow/Resources/AH/Models/Web/qAnims.glb'
MESH = '/tmp/himp/s2.glb'
OUT = sys.argv[1] if len(sys.argv) > 1 else '/tmp/himp/imp_rig.glb'
KEEP = ['Idle_Loop', 'Walk_Loop', 'Jog_Fwd_Loop', 'Sword_Attack', 'Hit_Chest', 'Death01', 'Spell_Simple_Shoot']

# ---------- mesh ----------
m = GLB(MESH); mp = m.j['meshes'][0]['primitives'][0]
Wm, _ = m.world(); P = m.acc(mp['attributes']['POSITION']).astype(float); P = (np.c_[P, np.ones(len(P))] @ Wm[0].T)[:, :3]
UV = m.acc(mp['attributes']['TEXCOORD_0']).astype(np.float32); I = m.acc(mp['indices']).reshape(-1, 3).astype(int)
# modesty: drop the bits hanging between the legs (a loincloth covers the spot)
cen = P[I].mean(1)
vm = (np.abs(P[:, 0]) < 0.12) & (P[:, 1] > 0.38) & (P[:, 1] < 0.765) & (P[:, 2] > -0.14)
crotch = vm[I].any(1)
I = I[~crotch]; print('removed tris', crotch.sum())
from PIL import Image, ImageEnhance
iv = m.j['images'][0]; bv = m.j['bufferViews'][iv['bufferView']]; o = bv.get('byteOffset', 0); im = Image.open(io.BytesIO(bytes(m.bin[o:o + bv['byteLength']]))).convert('RGB').resize((1024, 1024), Image.LANCZOS)
a_ = np.asarray(im).astype(float) / 255; a_ = np.clip(a_ * np.array([1.55, 1.3, 1.25]) + np.array([0.03, 0.0, 0.0]), 0, 1) ** 0.92
im = Image.fromarray((a_ * 255).astype(np.uint8)); bb = io.BytesIO(); im.save(bb, 'JPEG', quality=86); JPG = bb.getvalue()

# ---------- rig: bind pose fitted to the mesh ----------
g = GLB(RIG); j = g.j; nodes = j['nodes']; NM = {n['name']: i for i, n in enumerate(nodes)}
W0, par = g.world()
def rot_between(a, b_):
    a = a / np.linalg.norm(a); b_ = b_ / np.linalg.norm(b_); v = np.cross(a, b_); c = a @ b_
    if c < -0.9999: return -np.eye(3)
    vx = np.array([[0, -v[2], v[1]], [v[2], 0, -v[0]], [-v[1], v[0], 0]]); return np.eye(3) + vx + vx @ vx / (1 + c)
pos0 = {n: W0[i][:3, 3] for n, i in NM.items()}
R0 = {}
for n, i in NM.items():
    M = W0[i][:3, :3]; s = np.linalg.norm(M, axis=0); R0[n] = M / s
WB = {}  # bind world 4x4 (no scale) per node
def setw(n, R, p): M = np.eye(4); M[:3, :3] = R; M[:3, 3] = p; WB[n] = M
for n in NM:
    if n in ('Armature', 'Mannequin', 'root'): WB[n] = W0[NM[n]].copy(); continue
for n, p in JP.items(): setw(n, R0[n], p)
H = 1.0  # scale for leaf offsets
for s in ('l', 'r'):
    ua, la, ha = 'upperarm_' + s, 'lowerarm_' + s, 'hand_' + s
    Ru = rot_between(pos0[la] - pos0[ua], JP[la] - JP[ua]); setw(ua, Ru @ R0[ua], JP[ua])
    Rl = rot_between(pos0[ha] - pos0[la], JP[ha] - JP[la]); setw(la, Rl @ R0[la], JP[la])
    claw = np.array([0.61 if s == 'l' else -0.61, 0.83, 0.50])
    Rh = rot_between(pos0['middle_01_' + s] - pos0[ha], claw - JP[ha]); setw(ha, Rh @ R0[ha], JP[ha])
    k = 1.15
    for n in NM:
        if n.endswith('_' + s) and any(n.startswith(f) for f in ('index', 'middle', 'pinky', 'ring', 'thumb')):
            setw(n, Rh @ R0[n], JP[ha] + Rh @ (pos0[n] - pos0[ha]) * k)
    setw('ball_leaf_' + s, R0['ball_leaf_' + s], JP['ball_' + s] + np.array([0, -0.03, 0.10]))
missing = [n for n in NM if n not in WB]; assert not missing, missing
# locals from worlds
def m2q(R):
    t = np.trace(R)
    if t > 0: s = np.sqrt(t + 1) * 2; return [(R[2, 1] - R[1, 2]) / s, (R[0, 2] - R[2, 0]) / s, (R[1, 0] - R[0, 1]) / s, 0.25 * s]
    i = np.argmax(np.diag(R))
    if i == 0: s = np.sqrt(1 + R[0, 0] - R[1, 1] - R[2, 2]) * 2; return [0.25 * s, (R[0, 1] + R[1, 0]) / s, (R[0, 2] + R[2, 0]) / s, (R[2, 1] - R[1, 2]) / s]
    if i == 1: s = np.sqrt(1 + R[1, 1] - R[0, 0] - R[2, 2]) * 2; return [(R[0, 1] + R[1, 0]) / s, 0.25 * s, (R[1, 2] + R[2, 1]) / s, (R[0, 2] - R[2, 0]) / s]
    s = np.sqrt(1 + R[2, 2] - R[0, 0] - R[1, 1]) * 2; return [(R[0, 2] + R[2, 0]) / s, (R[1, 2] + R[2, 1]) / s, 0.25 * s, (R[1, 0] - R[0, 1]) / s]
oldT = {n: np.array(nodes[i].get('translation', [0, 0, 0]), float) for n, i in NM.items()}
for n, i in NM.items():
    if n in ('Armature', 'Mannequin', 'root'): continue
    pw = WB[nodes[par[i]]['name']] if i in par else np.eye(4)
    # parent world may carry scale (root): use the true parent world matrix
    L = np.linalg.inv(pw) @ WB[n]
    sc = np.linalg.norm(L[:3, :3], axis=0); R = L[:3, :3] / sc
    nd = nodes[i]; nd.pop('matrix', None); nd['translation'] = [float(x) for x in L[:3, 3]]; nd['rotation'] = [float(x) for x in m2q(R)]; nd['scale'] = [float(x) for x in sc]
Wc, _ = g.world()
for n, i in NM.items():
    if n in JP: assert np.allclose(Wc[i][:3, 3], JP[n], atol=1e-4), (n, Wc[i][:3, 3], JP[n])

# ---------- animations: keep a few, move the hips to the imp's hip height ----------
A = [a for a in j['animations'] if a['name'] in KEEP]; j['animations'] = A
dp = np.array(nodes[NM['pelvis']]['translation']) - oldT['pelvis']
done = set()
for a in A:
    for c in a['channels']:
        if c['target']['path'] == 'translation' and c['target']['node'] == NM['pelvis']:
            o_ = a['samplers'][c['sampler']]['output']
            if o_ in done: continue
            done.add(o_); g.setacc(o_, (g.acc(o_) + dp).astype(np.float32))

# ---------- skinning ----------
SEGS = {}
def seg(n, a, b_): SEGS[n] = (np.array(a, float), np.array(b_, float))
seg('pelvis', JP['pelvis'], JP['spine_01']); seg('spine_01', JP['spine_01'], JP['spine_02']); seg('spine_02', JP['spine_02'], JP['spine_03'])
seg('spine_03', JP['spine_03'], JP['neck_01']); seg('neck_01', JP['neck_01'], JP['Head']); seg('Head', JP['Head'], [0, 1.45, 0.45])
for s, sx in (('l', 1), ('r', -1)):
    seg('clavicle_' + s, JP['clavicle_' + s], JP['upperarm_' + s]); seg('upperarm_' + s, JP['upperarm_' + s], JP['lowerarm_' + s])
    seg('lowerarm_' + s, JP['lowerarm_' + s], JP['hand_' + s]); seg('hand_' + s, JP['hand_' + s], [0.61 * sx, 0.83, 0.5])
    seg('thigh_' + s, JP['thigh_' + s], JP['calf_' + s]); seg('calf_' + s, JP['calf_' + s], JP['foot_' + s])
    seg('foot_' + s, JP['foot_' + s], JP['ball_' + s]); seg('ball_' + s, JP['ball_' + s], JP['ball_' + s] + [0.02 * sx, -0.03, 0.12])
def dseg(Q, a, b_):
    ab = b_ - a; t = np.clip(((Q - a) @ ab) / (ab @ ab), 0, 1); return np.linalg.norm(Q - (a + t[:, None] * ab), axis=1)
def skin(Q, names, soft):
    D = np.stack([dseg(Q, *SEGS[n]) for n in names], 1)
    Wt = np.exp(-(D - D.min(1, keepdims=True)) / soft); o_ = np.argsort(-Wt, 1)[:, :4]
    Wk = np.take_along_axis(Wt, o_, 1); Wk[Wk < 0.04] = 0; Wk /= Wk.sum(1, keepdims=True)
    N_ = np.array([[names[i] for i in r] for r in o_], dtype=object)
    if N_.shape[1] < 4:
        pad = 4 - N_.shape[1]; N_ = np.c_[N_, np.repeat(N_[:, :1], pad, 1)]; Wk = np.c_[Wk, np.zeros((len(Q), pad))]
    return N_, Wk
ALL = list(SEGS)
names, Wk = skin(P, ALL, 0.025)
# region rules: head mass and horns -> head/neck only; tail -> pelvis; legs never pick up arms and vice versa
head = (P[:, 1] > 1.22) & (P[:, 2] > 0.08) | (P[:, 1] > 1.5)
tail = (P[:, 2] < -0.27) & (P[:, 1] < 0.86)
for mask, cand, soft in ((head, ['neck_01', 'Head', 'spine_03'], 0.03), (tail, ['pelvis', 'spine_01'], 0.05)):
    nn, ww = skin(P[mask], cand, soft); names[mask] = nn; Wk[mask] = ww
low = (P[:, 1] < 0.76) & ~tail
nn, ww = skin(P[low], ['pelvis', 'thigh_l', 'calf_l', 'foot_l', 'ball_l', 'thigh_r', 'calf_r', 'foot_r', 'ball_r'], 0.025); names[low] = nn; Wk[low] = ww
JL = [NM[n] for n in NM if n not in ('Armature', 'Mannequin')]
JIDX = {nodes[ji]['name']: k for k, ji in enumerate(JL)}
Jk = np.vectorize(JIDX.get)(names).astype(np.uint16)
IBM = np.stack([np.linalg.inv(Wc[ji]) for ji in JL]).transpose(0, 2, 1).reshape(-1, 16).astype(np.float32)

# ---------- loincloth (leather, weighted to hips and thighs) ----------
EX = []  # extra parts: P, I, names, weights, material
def flap():
    rows = []
    for i, y in enumerate(np.linspace(0.80, 0.55, 6)):
        f = i / 5; hw = 0.115 - 0.03 * f; z0 = 0.05 + 0.035 * f
        rows.append([[x, y + (0.03 if (i == 5 and k % 2) else 0), z0 + 0.012 * (1 - (x / hw) ** 2)] for k, x in enumerate(np.linspace(-hw, hw, 5))])
    V = np.array(rows, float).reshape(-1, 3); F = []
    for i in range(5):
        for k in range(4):
            a = i * 5 + k; F += [[a, a + 1, a + 6], [a, a + 6, a + 5]]
    f = np.clip((0.80 - V[:, 1]) / 0.25, 0, 1)
    nm = np.array([['pelvis', 'thigh_l', 'thigh_r', 'pelvis']] * len(V), dtype=object); w = np.c_[1 - 0.6 * f, 0.3 * f, 0.3 * f, 0 * f]
    return V, np.array(F), nm, w
EX.append(flap())
# belt hugging the hips
ring = []; NB = 24
for k in range(NB):
    a = 2 * np.pi * k / NB; zc = -0.135; rz = 0.18 if np.cos(a) > 0 else 0.215
    ring.append([0.215 * np.sin(a), 0.805 - 0.015 * np.cos(a), zc + rz * np.cos(a)])
ring = np.array(ring); V = []; F = []
for k, p in enumerate(ring):
    out = np.array([p[0], 0, p[2] + 0.135]); out /= np.linalg.norm(out) + 1e-9
    V += [p + [0, 0.022, 0], p + out * 0.012, p - [0, 0.022, 0]]
for k in range(NB):
    a = k * 3; b_ = ((k + 1) % NB) * 3
    for r in range(2): F += [[a + r, b_ + r, b_ + r + 1], [a + r, b_ + r + 1, a + r + 1]]
V = np.array(V); EX.append((V, np.array(F), np.array([['pelvis'] * 4] * len(V), dtype=object), np.c_[np.ones(len(V)), np.zeros((len(V), 3))]))
# buckle: a small bone-coloured skull-ish disc at the front
c = np.array([0, 0.805, 0.05]); V = [c + [0, 0, 0.012]]; F = []
for k in range(10):
    a = 2 * np.pi * k / 10; V.append(c + [0.028 * np.cos(a), 0.028 * np.sin(a), 0])
for k in range(10): F.append([0, 1 + k, 1 + (k + 1) % 10])
V = np.array(V); BUCKLE = (V, np.array(F), np.array([['pelvis'] * 4] * len(V), dtype=object), np.c_[np.ones(len(V)), np.zeros((len(V), 3))])

# ---------- write ----------
j['meshes'] = j.get('meshes', []); j['materials'] = []; j['textures'] = []; j['images'] = []; j['samplers'] = [{'magFilter': 9729, 'minFilter': 9987}]
while len(g.bin) % 4: g.bin.append(0)
off = len(g.bin); g.bin += JPG; j['bufferViews'].append({'buffer': 0, 'byteOffset': off, 'byteLength': len(JPG)})
j['images'].append({'bufferView': len(j['bufferViews']) - 1, 'mimeType': 'image/jpeg'}); j['textures'].append({'sampler': 0, 'source': 0})
j['materials'].append({'name': 'imp_body', 'pbrMetallicRoughness': {'baseColorTexture': {'index': 0}, 'baseColorFactor': [1.0, 1.0, 1.0, 1.0], 'metallicFactor': 0.0, 'roughnessFactor': 0.62}, 'doubleSided': True})
j['materials'].append({'name': 'imp_leather', 'pbrMetallicRoughness': {'baseColorFactor': lin(0x3d2a1c) + [1.0], 'metallicFactor': 0.0, 'roughnessFactor': 0.85}, 'doubleSided': True})
j['materials'].append({'name': 'imp_bone', 'pbrMetallicRoughness': {'baseColorFactor': lin(0xc9b48a) + [1.0], 'metallicFactor': 0.3, 'roughnessFactor': 0.5}, 'doubleSided': True})
prims = []
def prim(Pp, Ii, Nm, Ww, mat, uv=None):
    Jx = np.vectorize(JIDX.get)(Nm).astype(np.uint16); Ww = Ww / Ww.sum(1, keepdims=True)
    att = {'POSITION': g.add(Pp.astype(np.float32), 'VEC3', minmax=True, target=34962), 'NORMAL': g.add(normals(Pp, Ii.reshape(-1)).astype(np.float32), 'VEC3', target=34962),
           'JOINTS_0': g.add(Jx, 'VEC4', 5123, target=34962), 'WEIGHTS_0': g.add(Ww.astype(np.float32), 'VEC4', target=34962)}
    if uv is not None: att['TEXCOORD_0'] = g.add(uv, 'VEC2', target=34962)
    return {'attributes': att, 'indices': g.add(Ii.reshape(-1).astype(np.uint32), 'SCALAR', 5125, target=34963), 'material': mat}
# body normals: smooth across uv seams
Nb = None
prims.append(prim(P, I, names, Wk, 0, UV))
EV = []; EI = []; EN = []; EW = []; o_ = 0
for V, F, nm, w in EX: EV.append(V); EI.append(F + o_); EN.append(nm); EW.append(w); o_ += len(V)
prims.append(prim(np.vstack(EV), np.vstack(EI), np.vstack(EN), np.vstack(EW), 1))
prims.append(prim(BUCKLE[0], BUCKLE[1], BUCKLE[2], BUCKLE[3], 2))
j['meshes'].append({'name': 'HellishImp', 'primitives': prims})
j.setdefault('skins', []).append({'joints': JL, 'inverseBindMatrices': g.add(IBM, 'MAT4'), 'skeleton': NM['root']})
j['nodes'].append({'name': 'HellishImp', 'mesh': len(j['meshes']) - 1, 'skin': len(j['skins']) - 1})
j['scenes'][0]['nodes'].append(len(j['nodes']) - 1)
j['asset']['extras'] = {'title': 'Hellish imp 01', 'author': 'GAMEBR0VIP (https://sketchfab.com/GAMEBR0VIP)', 'license': 'CC-BY-4.0',
                        'source': 'https://sketchfab.com/3d-models/hellish-imp-01-f4f361ed0bcf4782a3dae72264faefba', 'note': 'decimated, loincloth added, rigged for Ashen Hollow'}
g.save(OUT); print('ok', len(I), 'tris body')
