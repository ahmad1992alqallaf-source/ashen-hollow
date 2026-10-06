# general re-rig: put a static mesh (already facing +z, feet at y=0, metres) on an existing skeleton with clips.
# Bones keep their rest rotations and move to the given joint positions; unlisted bones move with their parent;
# translation keys keep their motion (scaled); skin weights by distance to bone segments within body-part groups.
import sys, numpy as np
sys.path.insert(0, '/tmp/kit'); sys.path.insert(0, '/tmp/rig3')
from gl import GLB
from beast import normals
def build(C):
    m = GLB(C['mesh']); parts = []
    for mm in m.j['meshes']:
        for p in mm['primitives']:
            P = m.acc(p['attributes']['POSITION']).astype(float) * C.get('scale', 1.0) + np.array(C.get('offset', [0,0,0]))
            UV = m.acc(p['attributes']['TEXCOORD_0']).astype(np.float32); I = m.acc(p['indices']).reshape(-1, 3).astype(int)
            mat = m.j['materials'][p['material']]; img = None
            t = mat.get('pbrMetallicRoughness', {}).get('baseColorTexture')
            if t is not None:
                iv = m.j['images'][m.j['textures'][t['index']]['source']]; bv = m.j['bufferViews'][iv['bufferView']]; o = bv.get('byteOffset', 0)
                img = (bytes(m.bin[o:o+bv['byteLength']]), iv['mimeType'])
            parts.append(dict(P=P, UV=UV, I=I, img=img, alpha=mat.get('alphaMode')))
    g = GLB(C['skeleton']); j = g.j; N = j['nodes']; NM = {n.get('name'): i for i, n in enumerate(N) if n.get('name')}
    W0, par = g.world()
    sk = j['skins'][0]; JL = sk['joints']; jset = set(JL)
    JP = {n: np.array(v, float) for n, v in C['JP'].items()}
    # new world matrices: given bones take their positions; others follow their nearest moved ancestor
    WB = {}
    def newW(i):
        if i in WB: return WB[i]
        n = N[i].get('name'); M = W0[i].copy()
        if n in JP: M[:3,3] = JP[n]
        elif i in par and i in jset:
            pi = par[i]; pw = newW(pi)
            # keep the offset from the parent, scaled like the parent chain
            M[:3,3] = pw[:3,3] + (W0[i][:3,3] - W0[pi][:3,3]) * C.get('kids', 1.0)
        WB[i] = M; return M
    for i in range(len(N)): newW(i)
    oldT = {i: np.array(N[i].get('translation', [0,0,0]), float) for i in range(len(N))}
    for i in range(len(N)):
        if i not in jset: continue
        pw = WB[par[i]] if i in par else np.eye(4)
        L = np.linalg.inv(pw) @ WB[i]; N[i]['translation'] = [float(x) for x in L[:3,3]]
    Wc, _ = g.world()
    for n in JP:
        if n in NM: assert np.allclose(Wc[NM[n]][:3,3], JP[n], atol=1e-3), (n, Wc[NM[n]][:3,3], JP[n])
    hk = C.get('hk', 1.0)
    if C.get('clips'): j['animations'] = [a for a in j['animations'] if a['name'] in C['clips']]
    done = set()
    for a in j['animations']:
        for c in a['channels']:
            i = c['target']['node']
            if c['target']['path'] != 'translation' or i not in jset: continue
            o_ = a['samplers'][c['sampler']]['output']
            if o_ in done: continue
            done.add(o_); v = g.acc(o_)
            g.setacc(o_, (np.array(N[i]['translation']) + (v - oldT[i]) * hk).astype(np.float32))
    SEG = C['segs']   # list of (bone, a, b, group)
    names = [s[0] for s in SEG]; A = [np.array(s[1], float) for s in SEG]; B = [np.array(s[2], float) for s in SEG]; G = np.array([s[3] for s in SEG])
    def dseg(Q, a, b):
        ab = b - a; t = np.clip(((Q - a) @ ab) / max(ab @ ab, 1e-9), 0, 1); return np.linalg.norm(Q - (a + t[:, None]*ab), axis=1)
    soft = C.get('soft', 0.04)
    JIDX = {N[ji]['name']: q for q, ji in enumerate(JL)}
    def skin(P):
        D = np.stack([dseg(P, A[q], B[q]) for q in range(len(SEG))], 1)
        if 'bias' in C: D = C['bias'](P, D, names)
        near = np.argmin(D, 1)
        ok = (G[None, :] == G[near][:, None]) | (G[None, :] == 'body')
        Dm = np.where(ok, D, 1e9)
        Wt = np.exp(-(Dm - Dm.min(1, keepdims=True)) / soft); Wt[~ok] = 0
        o = np.argsort(-Wt, 1)[:, :4]; Wk = np.take_along_axis(Wt, o, 1); Wk[Wk < 0.05] = 0; Wk /= Wk.sum(1, keepdims=True)
        Jx = np.vectorize(lambda q: JIDX[names[q]])(o).astype(np.uint16)
        return Jx, Wk.astype(np.float32)
    IBM = np.stack([np.linalg.inv(Wc[ji]) for ji in JL]).transpose(0, 2, 1).reshape(-1, 16).astype(np.float32)
    j['materials'] = []; j['textures'] = []; j['images'] = []; j['samplers'] = [{'magFilter': 9729, 'minFilter': 9987}]
    prims = []
    for pt in parts:
        Jx, Wk = skin(pt['P'])
        mat = {'pbrMetallicRoughness': {'baseColorFactor': [1,1,1,1], 'metallicFactor': 0, 'roughnessFactor': 0.85}, 'doubleSided': True}
        if pt['alpha']: mat['alphaMode'] = pt['alpha']
        if pt['img']:
            b, mime = pt['img']
            while len(g.bin) % 4: g.bin.append(0)
            o = len(g.bin); g.bin += b; j['bufferViews'].append({'buffer': 0, 'byteOffset': o, 'byteLength': len(b)})
            j['images'].append({'bufferView': len(j['bufferViews'])-1, 'mimeType': mime}); j['textures'].append({'sampler': 0, 'source': len(j['images'])-1})
            mat['pbrMetallicRoughness']['baseColorTexture'] = {'index': len(j['textures'])-1}
        j['materials'].append(mat)
        I = pt['I'].reshape(-1).astype(np.uint32)
        att = {'POSITION': g.add(pt['P'].astype(np.float32), 'VEC3', minmax=True, target=34962), 'NORMAL': g.add(normals(pt['P'], I).astype(np.float32), 'VEC3', target=34962),
               'TEXCOORD_0': g.add(pt['UV'], 'VEC2', target=34962), 'JOINTS_0': g.add(Jx, 'VEC4', 5123, target=34962), 'WEIGHTS_0': g.add(Wk, 'VEC4', target=34962)}
        prims.append({'attributes': att, 'indices': g.add(I, 'SCALAR', 5125, target=34963), 'material': len(j['materials'])-1})
    for n in N: n.pop('mesh', None); n.pop('skin', None)
    j['meshes'] = [{'name': C['name'], 'primitives': prims}]
    sk['inverseBindMatrices'] = g.add(IBM, 'MAT4')
    N.append({'name': C['name'], 'mesh': 0, 'skin': 0}); j['scenes'][j.get('scene', 0)]['nodes'].append(len(N)-1)
    j['asset']['extras'] = C.get('extras', {})
    g.save(C['out']); print('ok', C['name'], sum(len(p['I']) for p in parts), 'tris')
