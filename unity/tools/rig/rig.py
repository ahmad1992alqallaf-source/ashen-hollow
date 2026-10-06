# rigs a static humanoid mesh to the Quaternius skeleton (qAnims): generalised from the hellish imp build
import sys, io, json, numpy as np
sys.path.insert(0, '/tmp/kit')
from gl import GLB
from beast import lin, normals
from PIL import Image
RIG = '/mnt/user-data/uploads/AshenHollow/Assets/AshenHollow/Resources/AH/Models/Web/qAnims.glb'
KEEP = ['Idle_Loop', 'Walk_Loop', 'Jog_Fwd_Loop', 'Sword_Attack', 'Sword_Regular_Combo', 'Hit_Chest', 'Death01', 'Spell_Simple_Shoot']

def rot_between(a, b_):
    a = a / np.linalg.norm(a); b_ = b_ / np.linalg.norm(b_); v = np.cross(a, b_); c = a @ b_
    if c < -0.9999: return -np.eye(3)
    vx = np.array([[0, -v[2], v[1]], [v[2], 0, -v[0]], [-v[1], v[0], 0]]); return np.eye(3) + vx + vx @ vx / (1 + c)
def m2q(R):
    t = np.trace(R)
    if t > 0: s = np.sqrt(t + 1) * 2; return [(R[2, 1] - R[1, 2]) / s, (R[0, 2] - R[2, 0]) / s, (R[1, 0] - R[0, 1]) / s, 0.25 * s]
    i = np.argmax(np.diag(R))
    if i == 0: s = np.sqrt(1 + R[0, 0] - R[1, 1] - R[2, 2]) * 2; return [0.25 * s, (R[0, 1] + R[1, 0]) / s, (R[0, 2] + R[2, 0]) / s, (R[2, 1] - R[1, 2]) / s]
    if i == 1: s = np.sqrt(1 + R[1, 1] - R[0, 0] - R[2, 2]) * 2; return [(R[0, 1] + R[1, 0]) / s, 0.25 * s, (R[1, 2] + R[2, 1]) / s, (R[0, 2] - R[2, 0]) / s]
    s = np.sqrt(1 + R[2, 2] - R[0, 0] - R[1, 1]) * 2; return [(R[0, 2] + R[2, 0]) / s, (R[1, 2] + R[2, 1]) / s, 0.25 * s, (R[1, 0] - R[0, 1]) / s]
def img_bytes(path, fx=None, size=1024, fmt='JPEG'):
    im = Image.open(path).convert('RGB').resize((size, size), Image.LANCZOS)
    if fx: a = np.asarray(im).astype(float) / 255; a = np.clip(fx(a), 0, 1); im = Image.fromarray((a * 255).astype(np.uint8))
    b = io.BytesIO(); im.save(b, fmt, quality=86); return b.getvalue()

def build(C):
    m = GLB(C['mesh']); mp = m.j['meshes'][0]['primitives'][0]
    P = m.acc(mp['attributes']['POSITION']).astype(float) * C.get('scale', 1) + np.array(C.get('offset', [0, 0, 0]))
    UV = m.acc(mp['attributes']['TEXCOORD_0']).astype(np.float32); I = m.acc(mp['indices']).reshape(-1, 3).astype(int)
    _T = np.asarray(Image.open(C['tex']).convert('RGB')).astype(float) / 255; _h, _w = _T.shape[:2]
    VC = _T[(np.clip(UV[:, 1], 0, 1) * (_h - 1)).astype(int), (np.clip(UV[:, 0], 0, 1) * (_w - 1)).astype(int)]
    if 'drop' in C:
        vm = C['drop'](P); bad = vm[I].any(1); I = I[~bad]; print('dropped tris', bad.sum())
    JP = {k: np.array(v, float) for k, v in C['JP'].items()}
    for k in list(JP):
        if k.endswith('_l') and k[:-2] + '_r' not in JP: p = JP[k]; JP[k[:-2] + '_r'] = np.array([-p[0], p[1], p[2]])
    g = GLB(RIG); j = g.j; nodes = j['nodes']; NM = {n['name']: i for i, n in enumerate(nodes)}
    W0, par = g.world()
    pos0 = {n: W0[i][:3, 3] for n, i in NM.items()}
    R0 = {n: W0[i][:3, :3] / np.linalg.norm(W0[i][:3, :3], axis=0) for n, i in NM.items()}
    WB = {}
    def setw(n, R, p): M = np.eye(4); M[:3, :3] = R; M[:3, 3] = p; WB[n] = M
    for n in NM:
        if n in ('Armature', 'Mannequin', 'root'): WB[n] = W0[NM[n]].copy()
    for n, p in JP.items(): setw(n, R0[n], p)
    TIP = {'l': np.array(C['tip'], float)}; TIP['r'] = TIP['l'] * [-1, 1, 1]
    for s in ('l', 'r'):
        ua, la, ha = 'upperarm_' + s, 'lowerarm_' + s, 'hand_' + s
        Ru = rot_between(pos0[la] - pos0[ua], JP[la] - JP[ua]); setw(ua, Ru @ R0[ua], JP[ua])
        Rl = rot_between(pos0[ha] - pos0[la], JP[ha] - JP[la]); setw(la, Rl @ R0[la], JP[la])
        Rh = rot_between(pos0['middle_01_' + s] - pos0[ha], TIP[s] - JP[ha]); setw(ha, Rh @ R0[ha], JP[ha])
        k = np.linalg.norm(TIP[s] - JP[ha]) / np.linalg.norm(pos0['middle_03_' + s] - pos0[ha] if 'middle_03_' + s in pos0 else pos0['middle_01_' + s] - pos0[ha]) * 0.8
        for n in NM:
            if n.endswith('_' + s) and any(n.startswith(f) for f in ('index', 'middle', 'pinky', 'ring', 'thumb')):
                setw(n, Rh @ R0[n], JP[ha] + Rh @ (pos0[n] - pos0[ha]) * k)
        setw('ball_leaf_' + s, R0['ball_leaf_' + s], JP['ball_' + s] + np.array([0, 0, 0.08]) * C.get('foot', 1))
    missing = [n for n in NM if n not in WB]; assert not missing, missing
    oldT = {n: np.array(nodes[i].get('translation', [0, 0, 0]), float) for n, i in NM.items()}
    for n, i in NM.items():
        if n in ('Armature', 'Mannequin', 'root'): continue
        pw = WB[nodes[par[i]]['name']] if i in par else np.eye(4)
        L = np.linalg.inv(pw) @ WB[n]; sc = np.linalg.norm(L[:3, :3], axis=0); R = L[:3, :3] / sc
        nd = nodes[i]; nd.pop('matrix', None); nd['translation'] = [float(x) for x in L[:3, 3]]; nd['rotation'] = [float(x) for x in m2q(R)]; nd['scale'] = [float(x) for x in sc]
    Wc, _ = g.world()
    # animations: only rotations (and the hips' own path, moved to this body's hip height)
    A = [a for a in j['animations'] if a['name'] in KEEP]; j['animations'] = A
    dp = np.array(nodes[NM['pelvis']]['translation']) - oldT['pelvis']; done = set()
    for a in A:
        keep = []
        for c in a['channels']:
            if c['target']['path'] == 'translation' and c['target']['node'] != NM['pelvis']: continue
            if c['target']['path'] == 'scale': continue
            keep.append(c)
            if c['target']['path'] == 'translation':
                o_ = a['samplers'][c['sampler']]['output']
                if o_ not in done: done.add(o_); g.setacc(o_, ((g.acc(o_) - oldT['pelvis']) * C.get('hipk', 1) + np.array(nodes[NM['pelvis']]['translation'])).astype(np.float32))
        a['channels'] = keep
    # skinning by distance to bone segments
    SEGS = {}
    def seg(n, a, b_): SEGS[n] = (np.array(a, float), np.array(b_, float))
    seg('pelvis', JP['pelvis'], JP['spine_01']); seg('spine_01', JP['spine_01'], JP['spine_02']); seg('spine_02', JP['spine_02'], JP['spine_03'])
    seg('spine_03', JP['spine_03'], JP['neck_01']); seg('neck_01', JP['neck_01'], JP['Head']); seg('Head', JP['Head'], C['headtip'])
    for s, sx in (('l', 1), ('r', -1)):
        seg('clavicle_' + s, JP['clavicle_' + s], JP['upperarm_' + s]); seg('upperarm_' + s, JP['upperarm_' + s], JP['lowerarm_' + s])
        seg('lowerarm_' + s, JP['lowerarm_' + s], JP['hand_' + s]); seg('hand_' + s, JP['hand_' + s], TIP[s])
        seg('thigh_' + s, JP['thigh_' + s], JP['calf_' + s]); seg('calf_' + s, JP['calf_' + s], JP['foot_' + s])
        seg('foot_' + s, JP['foot_' + s], JP['ball_' + s]); seg('ball_' + s, JP['ball_' + s], JP['ball_' + s] + np.array([0.0, -0.01, 0.08]) * C.get('foot', 1))
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
    soft = C.get('soft', 0.03)
    names, Wk = skin(P, list(SEGS), soft)
    for mask_fn, cand, sf in C.get('regions', []):
        mask = mask_fn(P)
        if mask.any(): nn, ww = skin(P[mask], cand, sf); names[mask] = nn; Wk[mask] = ww
    # a vertex led by an arm bone takes no leg weight, and the other way round (hands hang by the thighs)
    ARM = {p + s for s in ('_l', '_r') for p in ('upperarm', 'lowerarm', 'hand')}; LEG = {p + s for s in ('_l', '_r') for p in ('thigh', 'calf', 'foot', 'ball')} | {'pelvis'}
    for i in range(len(P)):
        top = names[i][int(np.argmax(Wk[i]))]
        bad = LEG if top in ARM else (ARM if top in LEG else set())
        if not bad: continue
        w = np.array([0.0 if n in bad else x for n, x in zip(names[i], Wk[i])])
        if w.sum() > 0: Wk[i] = w / w.sum()
    if 'noarm' in C:
        for i in np.where(C['noarm'](P, VC))[0]:
            if any(n in ARM for n in names[i]):
                keep = np.array([n not in ARM for n in names[i]]); w = Wk[i] * keep
                if w.sum() < 0.05: nn, ww = skin(P[i:i + 1], ['pelvis', 'spine_01', 'thigh_l', 'thigh_r'], soft); names[i] = nn[0]; Wk[i] = ww[0]
                else: Wk[i] = w / w.sum()
    if 'cuty' in C:
        # triangles bridging a hand and the hips (they touched in the sculpt) would stretch out when the arm moves
        dom = np.array([names[i][int(np.argmax(Wk[i]))] for i in range(len(P))], dtype=object)
        grp = np.array([1 if d_.endswith('_l') and d_ in ARM else 2 if d_.endswith('_r') and d_ in ARM else 0 for d_ in dom])
        G = grp[I]; mix = (G.min(1) != G.max(1)) & (P[I][:, :, 1].max(1) < C['cuty'])
        I = I[~mix]; print('cut bridging tris', mix.sum())
    if 'post' in C: C['post'](P, names, Wk, lambda Q, n: dseg(Q, *SEGS[n]))
    JL = [NM[n] for n in NM if n not in ('Armature', 'Mannequin')]
    JIDX = {nodes[ji]['name']: k for k, ji in enumerate(JL)}
    IBM = np.stack([np.linalg.inv(Wc[ji]) for ji in JL]).transpose(0, 2, 1).reshape(-1, 16).astype(np.float32)
    # material
    j['materials'] = []; j['textures'] = []; j['images'] = []; j['samplers'] = [{'magFilter': 9729, 'minFilter': 9987}]
    def addimg(b, mime='image/jpeg'):
        while len(g.bin) % 4: g.bin.append(0)
        off = len(g.bin); g.bin += b; j['bufferViews'].append({'buffer': 0, 'byteOffset': off, 'byteLength': len(b)})
        j['images'].append({'bufferView': len(j['bufferViews']) - 1, 'mimeType': mime}); j['textures'].append({'sampler': 0, 'source': len(j['images']) - 1}); return len(j['textures']) - 1
    mat = {'name': C['name'] + '_body', 'pbrMetallicRoughness': {'baseColorTexture': {'index': addimg(img_bytes(C['tex'], C.get('texfx')))}, 'metallicFactor': 0.0, 'roughnessFactor': C.get('rough', 0.6)}, 'doubleSided': True}
    if 'emis' in C: mat['emissiveTexture'] = {'index': addimg(img_bytes(C['emis'], C.get('emisfx'), 512))}; mat['emissiveFactor'] = C.get('emisk', [1, 1, 1])
    j['materials'].append(mat)
    Jx = np.vectorize(JIDX.get)(names).astype(np.uint16)
    att = {'POSITION': g.add(P.astype(np.float32), 'VEC3', minmax=True, target=34962), 'NORMAL': g.add(normals(P, I.reshape(-1)).astype(np.float32), 'VEC3', target=34962),
           'TEXCOORD_0': g.add(UV, 'VEC2', target=34962), 'JOINTS_0': g.add(Jx, 'VEC4', 5123, target=34962), 'WEIGHTS_0': g.add((Wk / Wk.sum(1, keepdims=True)).astype(np.float32), 'VEC4', target=34962)}
    j['meshes'] = j.get('meshes', []); j['meshes'].append({'name': C['name'], 'primitives': [{'attributes': att, 'indices': g.add(I.reshape(-1).astype(np.uint32), 'SCALAR', 5125, target=34963), 'material': 0}]})
    j.setdefault('skins', []).append({'joints': JL, 'inverseBindMatrices': g.add(IBM, 'MAT4'), 'skeleton': NM['root']})
    j['nodes'].append({'name': C['name'], 'mesh': len(j['meshes']) - 1, 'skin': len(j['skins']) - 1})
    j['scenes'][0]['nodes'].append(len(j['nodes']) - 1)
    j['asset']['extras'] = C.get('extras', {})
    g.save(C['out']); print('ok', C['name'], len(I), 'tris')
