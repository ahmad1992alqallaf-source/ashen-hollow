# reshape, recolour and add parts to a Quaternius animal (skinned glTF), keeping its animations
import numpy as np, sys
sys.path.insert(0,'/tmp/kit')
from gl import GLB
def lin(h):  # sRGB hex -> linear rgb
    c = np.array([(h>>16)&255,(h>>8)&255,h&255])/255.0
    return list(np.where(c<=0.04045, c/12.92, ((c+0.055)/1.055)**2.4))
class Beast:
    def __init__(s, path):
        s.g = GLB(path); s.g.dedupe_anims(); j = s.g.j
        s.sk = j['skins'][0]; s.jn = [j['nodes'][i]['name'] for i in s.sk['joints']]
        W,_ = s.g.world(); IBM = s.g.acc(s.sk['inverseBindMatrices']).reshape(-1,4,4).transpose(0,2,1)
        s.B = W[s.sk['joints'][0]] @ IBM[0]; s.Bi = np.linalg.inv(s.B)
        s.J = {s.jn[k]: (W[ji] @ np.array([0,0,0,1.0]))[:3] for k,ji in enumerate(s.sk['joints'])}
        s.prims = []   # mesh index, prim, world pos, joints, weights
        for mi,m in enumerate(j['meshes']):
            for p in m['primitives']:
                a = p['attributes']
                if 'JOINTS_0' not in a: continue
                P = s.g.acc(a['POSITION']); Pw = (np.c_[P, np.ones(len(P))] @ s.B.T)[:,:3]
                s.prims.append(dict(m=mi, p=p, P=Pw, J=s.g.acc(a['JOINTS_0']).astype(int), W=s.g.acc(a['WEIGHTS_0']).astype(float)))
    def infl(s, pr, names):
        idx = [k for k,n in enumerate(s.jn) if any((n == x[:-1]) if x.endswith('$') else n.startswith(x) for x in names)]
        w = np.zeros(len(pr['P']))
        for k in idx: w += (pr['W'] * (pr['J']==k)).sum(1)
        return w
    def mat(s, name):
        for i,m in enumerate(s.g.j['materials']):
            if m['name'] == name: return i
    def color(s, name, hexc, rough=None, metal=None, emis=None):
        i = s.mat(name)
        if i is None: return
        pbr = s.g.j['materials'][i].setdefault('pbrMetallicRoughness', {})
        pbr['baseColorFactor'] = lin(hexc) + [1.0]
        if rough is not None: pbr['roughnessFactor'] = rough
        if metal is not None: pbr['metallicFactor'] = metal
        if emis is not None: s.g.j['materials'][i]['emissiveFactor'] = lin(emis)
    def newmat(s, name, hexc, rough=0.8, metal=0.0, emis=None):
        m = {'name':name,'pbrMetallicRoughness':{'baseColorFactor':lin(hexc)+[1.0],'roughnessFactor':rough,'metallicFactor':metal}}
        if emis is not None: m['emissiveFactor'] = lin(emis)
        s.g.j['materials'].append(m); return len(s.g.j['materials'])-1
    def drop_mesh_with(s, test):
        # remove primitives whose world positions pass test (e.g. antlers mesh)
        pass
    def write(s, out):
        g = s.g
        for pr in s.prims:
            P = (np.c_[pr['P'], np.ones(len(pr['P']))] @ s.Bi.T)[:,:3]
            p = pr['p']; I = g.acc(p['indices']).reshape(-1)
            g.setacc(p['attributes']['POSITION'], P.astype(np.float32))
            g.setacc(p['attributes']['NORMAL'], normals(P, I).astype(np.float32))
        g.save(out)
    def part(s, verts, faces, joint, matidx):
        # verts in world space; rigidly bound to joint
        P = (np.c_[verts, np.ones(len(verts))] @ s.Bi.T)[:,:3].astype(np.float32)
        I = np.array(faces, dtype=np.uint32).reshape(-1)
        # unshare for flat shading
        P = P[I]; I = np.arange(len(P), dtype=np.uint32)
        k = s.jn.index(joint)
        Jt = np.zeros((len(P),4), np.uint16); Jt[:,0] = k
        Wt = np.zeros((len(P),4), np.float32); Wt[:,0] = 1
        g = s.g
        prim = {'attributes':{'POSITION':g.add(P,'VEC3',minmax=True,target=34962),'NORMAL':g.add(normals(P,I).astype(np.float32),'VEC3',target=34962),
                'JOINTS_0':g.add(Jt,'VEC4',5123,target=34962),'WEIGHTS_0':g.add(Wt,'VEC4',target=34962)},
                'indices':g.add(I,'SCALAR',5125,target=34963),'material':matidx}
        g.j['meshes'][s.prims[0]['m']]['primitives'].append(prim)
def normals(P, I):
    N = np.zeros_like(P, dtype=np.float64); T = I.reshape(-1,3)
    fn = np.cross(P[T[:,1]]-P[T[:,0]], P[T[:,2]]-P[T[:,0]])
    for k in range(3): np.add.at(N, T[:,k], fn)
    l = np.linalg.norm(N, axis=1, keepdims=True); l[l==0] = 1
    return N / l
# ---- shapes (world space): return verts, faces
def frame(d):
    d = np.array(d,float); d/=np.linalg.norm(d)
    a = np.array([0,1.0,0]) if abs(d[1])<0.9 else np.array([1.0,0,0])
    u = np.cross(d,a); u/=np.linalg.norm(u); v = np.cross(d,u); return d,u,v
def tube(points, radii, seg=6, cap=True):
    # a tapering tube through points (list of 3d), radii per point (last may be 0 -> point)
    V=[]; F=[]
    pts = [np.array(p,float) for p in points]
    for i,p in enumerate(pts):
        d = pts[min(i+1,len(pts)-1)] - pts[max(i-1,0)]
        _,u,v = frame(d)
        for k in range(seg):
            a = 2*np.pi*k/seg; V.append(p + radii[i]*(np.cos(a)*u + np.sin(a)*v))
    for i in range(len(pts)-1):
        for k in range(seg):
            a=i*seg+k; b=i*seg+(k+1)%seg; c=(i+1)*seg+(k+1)%seg; d=(i+1)*seg+k
            F += [a,b,c, a,c,d]
    if cap:
        c0 = len(V); V.append(pts[0])
        for k in range(seg): F += [c0, (k+1)%seg, k]
        c1 = len(V); V.append(pts[-1]); base=(len(pts)-1)*seg
        for k in range(seg): F += [c1, base+k, base+(k+1)%seg]
    return np.array(V), F
def spike(base, tip, r, seg=4):
    return tube([base, tip], [r, 0.0], seg)
def blob(c, r, seg=6, rings=4, sq=(1,1,1)):
    V=[]; F=[]
    for i in range(rings+1):
        th = np.pi*i/rings
        for k in range(seg):
            ph = 2*np.pi*k/seg
            V.append(np.array(c)+np.array([np.sin(th)*np.cos(ph)*r*sq[0], np.cos(th)*r*sq[1], np.sin(th)*np.sin(ph)*r*sq[2]]))
    for i in range(rings):
        for k in range(seg):
            a=i*seg+k; b=i*seg+(k+1)%seg; c2=(i+1)*seg+(k+1)%seg; d=(i+1)*seg+k
            F += [a,c2,b, a,d,c2]
    return np.array(V), F
def merge(*parts):
    V=[]; F=[]; o=0
    for v,f in parts: V.append(v); F += [x+o for x in f]; o += len(v)
    return np.vstack(V), F

def _smooth_normals(P, I):
    key = np.round(P, 4); _, inv = np.unique(key, axis=0, return_inverse=True); inv = inv.reshape(-1)
    N = normals(P, I); acc = np.zeros((inv.max()+1, 3)); np.add.at(acc, inv, N)
    l = np.linalg.norm(acc, axis=1, keepdims=True); l[l==0] = 1; return (acc/l)[inv]
def shell(b, pick, offset, matidx, offset_fn=None):
    """copy body triangles chosen by pick(centroid[n,3], normal[n,3], prim) -> bool mask, pushed out along the
    smoothed normals by offset (or offset_fn(points)), keeping the original skinning so it bends with the body"""
    g = b.g
    for pr in list(b.prims):
        if pr.get('part'): continue
        P = pr['P']; I = g.acc(pr['p']['indices']).reshape(-1).astype(np.int64); T = I.reshape(-1,3)
        Ns = _smooth_normals(P, I)
        cen = P[T].mean(1); fn = normals(P, I)[T].mean(1)
        m = pick(cen, fn, pr)
        if not m.any(): continue
        T2 = T[m].reshape(-1)
        off = offset if offset_fn is None else offset_fn(P[T2])
        Pn = P[T2] + Ns[T2] * (np.array(off).reshape(-1,1) if np.ndim(off) else off)
        Pb = (np.c_[Pn, np.ones(len(Pn))] @ b.Bi.T)[:,:3].astype(np.float32)
        I2 = np.arange(len(Pb), dtype=np.uint32)
        Jt = pr['J'][T2].astype(np.uint16); Wt = pr['W'][T2].astype(np.float32)
        prim = {'attributes':{'POSITION':g.add(Pb,'VEC3',minmax=True,target=34962),'NORMAL':g.add(normals(Pb,I2).astype(np.float32),'VEC3',target=34962),
                'JOINTS_0':g.add(Jt,'VEC4',5123,target=34962),'WEIGHTS_0':g.add(Wt,'VEC4',target=34962)},
                'indices':g.add(I2,'SCALAR',5125,target=34963),'material':matidx}
        g.j['meshes'][pr['m']]['primitives'].append(prim)
def scale_root(b, k):
    for n in b.g.j['scenes'][0]['nodes']:
        nd = b.g.j['nodes'][n]; sc = nd.get('scale',[1,1,1]); nd['scale'] = [x*k for x in sc]

def rebind(b, names, joint, thresh=0.05):
    """vertices led by these bones follow one joint only (a stiff tail or a stub)"""
    k = b.jn.index(joint)
    for pr in b.prims:
        w = b.infl(pr, names); m = w > thresh
        if not m.any(): continue
        Jt = pr['J'].copy(); Wt = pr['W'].copy()
        for i in np.where(m)[0]:
            ww = {}
            for a, c in zip(Jt[i], Wt[i]):
                if c <= 0: continue
                nm = joint if any((b.jn[a] == x[:-1]) if x.endswith('$') else b.jn[a].startswith(x) for x in names) else b.jn[a]
                ww[nm] = ww.get(nm, 0) + c
            top = sorted(ww.items(), key=lambda kv: -kv[1])[:4]
            Jt[i] = 0; Wt[i] = 0
            for q, (nm, c) in enumerate(top): Jt[i, q] = b.jn.index(nm); Wt[i, q] = c
        Wt = Wt / np.maximum(Wt.sum(1, keepdims=True), 1e-6)
        acc = b.g.j['accessors'][pr['p']['attributes']['JOINTS_0']]
        b.g.setacc(pr['p']['attributes']['JOINTS_0'], Jt.astype(np.uint16 if acc['componentType'] == 5123 else np.uint8))
        b.g.setacc(pr['p']['attributes']['WEIGHTS_0'], Wt.astype(np.float32))
        pr['J'] = Jt; pr['W'] = Wt
