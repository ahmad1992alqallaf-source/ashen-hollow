# drops triangles that stretch badly in any of the clips (bridges between parts that touched in the sculpt)
import sys, numpy as np
sys.path.insert(0, '/tmp/kit'); from gl import GLB
def q2m(q):
    x, y, z, w = q; return np.array([[1-2*(y*y+z*z), 2*(x*y-z*w), 2*(x*z+y*w)], [2*(x*y+z*w), 1-2*(x*x+z*z), 2*(y*z-x*w)], [2*(x*z-y*w), 2*(y*z+x*w), 1-2*(x*x+y*y)]])
def sample(g, a, t):
    over = {}
    for c in a['channels']:
        s = a['samplers'][c['sampler']]; T = g.acc(s['input']).reshape(-1); V = g.acc(s['output']).reshape(len(T), -1)
        k = np.searchsorted(T, t); k = min(max(k, 1), len(T) - 1); f = np.clip((t - T[k-1]) / max(T[k] - T[k-1], 1e-6), 0, 1)
        v = V[k-1] * (1 - f) + V[k] * f
        if c['target']['path'] == 'rotation': v = v / np.linalg.norm(v)
        over[(c['target']['node'], c['target']['path'])] = v
    return over
def posed(g, prim, skin, over):
    N = g.j['nodes']; par = {}
    for i, n in enumerate(N):
        for ch in n.get('children', []): par[ch] = i
    Wm = [None] * len(N)
    def loc(i):
        n = N[i]
        if 'matrix' in n: return np.array(n['matrix']).reshape(4, 4).T
        t = over.get((i, 'translation'), n.get('translation', [0, 0, 0])); r = over.get((i, 'rotation'), n.get('rotation', [0, 0, 0, 1])); s = n.get('scale', [1, 1, 1])
        M = np.eye(4); M[:3, :3] = q2m(r) * np.array(s); M[:3, 3] = t; return M
    def w(i):
        if Wm[i] is None: Wm[i] = (w(par[i]) if i in par else np.eye(4)) @ loc(i)
        return Wm[i]
    JL = skin['joints']; IBM = g.acc(skin['inverseBindMatrices']).reshape(-1, 4, 4).transpose(0, 2, 1)
    S = np.stack([w(ji) @ IBM[k] for k, ji in enumerate(JL)])
    P = g.acc(prim['attributes']['POSITION']).astype(float); J = g.acc(prim['attributes']['JOINTS_0']).astype(int); Wt = g.acc(prim['attributes']['WEIGHTS_0'])
    Ph = np.c_[P, np.ones(len(P))]; out = np.zeros((len(P), 3))
    for k in range(4): out += Wt[:, k:k+1] * np.einsum('nij,nj->ni', S[J[:, k]], Ph)[:, :3]
    return out
def cut(path, outp, ratio=2.2, minlen=0.04):
    g = GLB(path); j = g.j; mi = len(j['meshes']) - 1; prim = j['meshes'][mi]['primitives'][0]; skin = j['skins'][-1]
    P0 = g.acc(prim['attributes']['POSITION']).astype(float); I = g.acc(prim['indices']).reshape(-1, 3).astype(int)
    def elen(P): T = P[I]; return np.stack([np.linalg.norm(T[:, a] - T[:, b], axis=1) for a, b in ((0, 1), (1, 2), (2, 0))], 1).max(1)
    L0 = elen(P0); bad = np.zeros(len(I), bool)
    for a in j['animations']:
        T = np.concatenate([g.acc(s['input']).reshape(-1) for s in a['samplers']]); tmax = T.max()
        for t in np.linspace(0, tmax, 9):
            L = elen(posed(g, prim, skin, sample(g, a, t))); bad |= (L > ratio * L0) & (L - L0 > minlen)
    print('stretchy tris', bad.sum(), 'of', len(I))
    I2 = I[~bad].reshape(-1).astype(np.uint32)
    prim['indices'] = g.add(I2, 'SCALAR', 5125, target=34963); g.save(outp)
if __name__ == '__main__': cut(sys.argv[1], sys.argv[2], *(float(x) for x in sys.argv[3:]))
