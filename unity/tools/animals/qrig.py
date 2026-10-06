# rigs a static four-legged mesh (facing +z, feet on y=0, metres) to the Quaternius wolf skeleton and its 12 clips.
# The bones keep the wolf's rest rotations and only move to the animal's joints, so the wolf's motion carries over
# as it is; the hips' and the foot targets' paths are scaled to the new size.
import sys, io, json, numpy as np
sys.path.insert(0, '/tmp/kit'); sys.path.insert(0, '/tmp/rig3')
from gl import GLB
from beast import normals
WOLF = '/tmp/an/wolf.glb'
def build(C):
    m = GLB(C['mesh']); parts = []
    for p in m.j['meshes'][0]['primitives']:
        P = m.acc(p['attributes']['POSITION']).astype(float); P = np.c_[-P[:,2], P[:,1], P[:,0]] + np.array(C.get('offset', [0,0,0]))
        P *= C.get('scale', 1.0)
        UV = m.acc(p['attributes']['TEXCOORD_0']).astype(np.float32); I = m.acc(p['indices']).reshape(-1, 3).astype(int)
        mat = m.j['materials'][p['material']]; img = None
        t = mat.get('pbrMetallicRoughness', {}).get('baseColorTexture')
        if t is not None:
            iv = m.j['images'][m.j['textures'][t['index']]['source']]; bv = m.j['bufferViews'][iv['bufferView']]; o = bv.get('byteOffset', 0)
            img = (bytes(m.bin[o:o+bv['byteLength']]), iv['mimeType'])
        parts.append(dict(P=P, UV=UV, I=I, img=img, alpha=mat.get('alphaMode')))
    k = C.get('scale', 1.0); JP = {n: np.array(v, float)*k for n, v in C['JP'].items()}
    g = GLB(WOLF); j = g.j; N = j['nodes']; NM = {n.get('name'): i for i, n in enumerate(N)}
    W0, par = g.world()
    pos0 = {n: W0[i][:3,3] for n, i in NM.items() if n}
    R0 = {n: W0[i][:3,:3] for n, i in NM.items() if n}
    # positions: given ones, then helpers placed with what they belong to
    NP = dict(JP)
    for s in ('L', 'R'):
        NP['FF.'+s] = JP['FrontPaw.'+s]; NP['IKFrontLeg.'+s] = JP['FrontPaw.'+s] + [0, 0.02*k, 0]
        NP['FFB.'+s] = JP['BackPaw.'+s]; NP['IKBackLeg.'+s] = JP['BackPaw.'+s] + [0, 0.02*k, 0]
        for e in ('PoleTarget.', 'PoleTargetBack.'): NP[e+s] = pos0[e+s] * (JP['Back'][1]/pos0['Back'][1])
    hk = JP['Back'][1] / pos0['Back'][1]
    if 'Body' not in NP: NP['Body'] = np.array([0, pos0['Body'][1]*hk, JP['Back'][2] + (pos0['Body'][2]-pos0['Back'][2])*hk])
    WB = {}
    for n, i in NM.items():
        if not n: continue
        M = W0[i].copy()
        if n in NP: M[:3,3] = NP[n]
        elif n not in ('AnimalArmature', 'RootNode'): print('kept', n)
        WB[n] = M
    oldT = {n: np.array(N[i].get('translation', [0,0,0]), float) for n, i in NM.items() if n}
    for n, i in NM.items():
        if not n or n in ('AnimalArmature', 'RootNode'): continue
        pw = WB[N[par[i]]['name']] if i in par and N[par[i]].get('name') in WB else W0[par[i]] if i in par else np.eye(4)
        L = np.linalg.inv(pw) @ WB[n]; N[i]['translation'] = [float(x) for x in L[:3,3]]
    Wc, _ = g.world()
    for n in JP:
        if n in NM: assert np.allclose(Wc[NM[n]][:3,3], NP[n], atol=1e-3), n
    # clips: rotations as they are; the moving roots' paths scaled to the new size
    KEEP = C.get('clips')
    if KEEP: j['animations'] = [a for a in j['animations'] if a['name'] in KEEP]
    done = set()
    for a in j['animations']:
        for c in a['channels']:
            nd = N[c['target']['node']].get('name')
            if c['target']['path'] != 'translation': continue
            o_ = a['samplers'][c['sampler']]['output']
            if o_ in done: continue
            done.add(o_); v = g.acc(o_)
            g.setacc(o_, (np.array(N[c['target']['node']]['translation']) + (v - oldT[nd]) * hk).astype(np.float32))
    # bone segments for the skin
    SEG = {}; GRP = {}
    def seg(n, a, b, grp): SEG[n] = (np.array(a, float), np.array(b, float)); GRP[n] = grp
    sp = ['Back', 'Torso', 'Torso2', 'Torso3', 'Neck1']
    for a_, b_ in zip(sp, sp[1:]): seg(a_, JP[a_], JP[b_], 'body')
    nk = ['Neck1', 'Neck2', 'Neck3', 'Head']
    for a_, b_ in zip(nk, nk[1:]): seg(a_, JP[a_], JP[b_], 'head' if a_ != 'Neck1' else 'body')
    seg('Head', JP['Head'], JP['HeadTip'], 'head')
    for s in ('L', 'R'):
        ch = ['FrontShoulder.'+s, 'FrontUpperLeg.'+s, 'FrontLowerLeg.'+s, 'FrontPaw.'+s]
        for a_, b_ in zip(ch, ch[1:]): seg(a_, JP[a_], JP[b_], 'F'+s if a_ != ch[0] else 'body')
        ch = ['BackShoulder.'+s, 'BackLeg.'+s, 'BackUpperLeg.'+s, 'BackLowerLeg.'+s, 'BackPaw.'+s]
        for a_, b_ in zip(ch, ch[1:]): seg(a_, JP[a_], JP[b_], 'B'+s if a_ != ch[0] else 'body')
        ear = ['Ear1.'+s, 'Ear2.'+s, 'Ear3.'+s, 'Ear4.'+s]
        if all(e in JP for e in ear):
            for a_, b_ in zip(ear, ear[1:]): seg(a_, JP[a_], JP[b_], 'head')
    tl = ['Tail%d' % i for i in range(1, 9)] + ['TailTip']
    for a_, b_ in zip(tl, tl[1:]):
        if a_ in JP and b_ in JP: seg(a_, JP[a_], JP[b_], 'tail')
    names = list(SEG)
    def dseg(Q, a, b):
        ab = b - a; t = np.clip(((Q - a) @ ab) / max(ab @ ab, 1e-9), 0, 1); return np.linalg.norm(Q - (a + t[:, None]*ab), axis=1)
    soft = C.get('soft', 0.04) * k
    JL = [NM[n] for n in NM if n and n not in ('AnimalArmature', 'RootNode')]
    JIDX = {N[ji]['name']: q for q, ji in enumerate(JL)}
    def skin(P):
        D = np.stack([dseg(P, *SEG[n]) for n in names], 1)
        near = np.argmin(D, 1); G = np.array([GRP[n] for n in names])
        ok = (G[None, :] == G[near][:, None]) | (G[None, :] == 'body')
        Dm = np.where(ok, D, 1e9)
        Wt = np.exp(-(Dm - Dm.min(1, keepdims=True)) / soft); Wt[~ok] = 0
        o = np.argsort(-Wt, 1)[:, :4]; Wk = np.take_along_axis(Wt, o, 1); Wk[Wk < 0.05] = 0; Wk /= Wk.sum(1, keepdims=True)
        Jx = np.vectorize(lambda q: JIDX[names[q]])(o).astype(np.uint16)
        if 'fix' in C: C['fix'](P, Jx, Wk, JIDX)
        return Jx, Wk.astype(np.float32)
    IBM = np.stack([np.linalg.inv(Wc[ji]) for ji in JL]).transpose(0, 2, 1).reshape(-1, 16).astype(np.float32)
    j['materials'] = []; j['textures'] = []; j['images'] = []; j['samplers'] = [{'magFilter': 9729, 'minFilter': 9987}]
    prims = []
    for pt in parts:
        Jx, Wk = skin(pt['P'])
        mat = {'pbrMetallicRoughness': {'baseColorFactor': C.get('tint', [1,1,1,1]), 'metallicFactor': 0, 'roughnessFactor': 0.85}, 'doubleSided': True}
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
    # replace the wolf's own mesh with this one, on the same skin
    mesh_nodes = [i for i, n in enumerate(N) if 'mesh' in n]
    j['meshes'] = [{'name': C['name'], 'primitives': prims}]
    j['skins'][0]['inverseBindMatrices'] = g.add(IBM, 'MAT4'); j['skins'][0]['joints'] = JL
    for q, i in enumerate(mesh_nodes):
        if q == 0: N[i]['mesh'] = 0; N[i]['skin'] = 0; N[i]['name'] = C['name']
        else: N[i].pop('mesh', None); N[i].pop('skin', None)
    j['asset']['extras'] = C.get('extras', {})
    g.save(C['out']); print('ok', C['name'], sum(len(p['I']) for p in parts), 'tris')
