# crocodile from the Quaternius wolf: low and long, short splayed legs, a long flat snout lined with teeth, a heavy
# tapering tail and rows of scutes down the back
import sys, numpy as np; sys.path.insert(0,'/tmp/kit')
from beast import *
def build(out, coat=0x3c4a2a, light=0xb7ae78, scute=0x2a3320, eye=0xd8b020, size=1.0):
    b = Beast('/tmp/an/wolf.glb'); J = b.J; head = J['Head']
    def low(y): return np.where(y < 1.43, y*0.3, y - 1.0)
    for pr in b.prims:
        P = pr['P']
        t = b.infl(pr, ['Body$','Back$','Torso']); n = b.infl(pr, ['Neck']); h = b.infl(pr, ['Head']); e = b.infl(pr, ['Ear'])
        tl = b.infl(pr, ['Tail']); lf = b.infl(pr, ['FrontShoulder','FrontUpperLeg','FrontLowerLeg','IKFront','FF.']); lb = b.infl(pr, ['BackShoulder','BackLeg','BackUpperLeg','BackLowerLeg','IKBack','FFB'])
        legs = np.clip(lf + lb, 0, 1)
        x, y, z = P[:,0].copy(), P[:,1].copy(), P[:,2].copy()
        # ears gone (folded into the head)
        m = e > 0.3; x[m], y[m], z[m] = head[0], head[1]+0.05, head[2]-0.1
        # head: a long flat snout
        hz0 = head[2] - 0.1; m = (h > 0.4) & (z > hz0)
        z[m] = hz0 + (z[m]-hz0)*2.6; y[m] = head[1] + (y[m]-head[1])*0.45 - 0.08*(z[m]-hz0); x[m] = x[m]*np.clip(1.15 - 0.12*(z[m]-hz0), 0.55, 1.2)
        # body: long, wide, flat
        body = np.clip(t + n, 0, 1)*(1 - legs)
        x = x*(1 + 0.55*body)
        y = np.where(body > 0.2, 1.85 + (y-1.85)*(1 - 0.45*body), y)
        z = z*(1 + 0.12*body)
        # tail: longer, thick at the root
        t1 = J['Tail1']; m = tl > 0.3
        d = np.clip((t1[2]-z[m]), 0, None); f = np.exp(-d/1.2)
        x[m] = x[m]*(1 + 2.4*f); y[m] = np.maximum(y[m], 1.25) - 0.2*(1-f)*(y[m]-1.25)
        z[m] = t1[2] - d*1.45
        # legs: splayed out
        for w, front in ((lf, True), (lb, False)):
            for side in ('L','R'):
                sx = 1 if side == 'L' else -1; m = (w > 0.5) & (np.sign(P[:,0]) == sx)
                x[m] = x[m]*1.35 + sx*0.25
        # no ruff or hump: the back is flat
        y = np.where(h < 0.3, np.minimum(y, 2.12), y)
        y = low(y)
        pr['P'] = np.c_[x,y,z]
    allP = np.vstack([pr['P'] for pr in b.prims])
    b.color('Main', coat, rough=0.8); b.color('Main_Light', light, rough=0.9); b.color('Nose', coat); b.color('Eyes_Black', eye, rough=0.2, emis=0x201800)
    ms = b.newmat('Scute', scute, 0.9); mt = b.newmat('Tooth', 0xeee6cc, 0.5)
    sn = allP[(allP[:,2] > head[2]+0.2)]
    # teeth down both sides of the snout
    if len(sn):
        z0, z1 = sn[:,2].min(), sn[:,2].max()
        for zz in np.linspace(z0+0.05, z1-0.12, 7):
            sl = sn[np.abs(sn[:,2]-zz) < 0.08]
            if not len(sl): continue
            yb = sl[:,1].min() + 0.06; xw = np.abs(sl[:,0]).max()
            for sx in (-1, 1):
                base = np.array([sx*(xw-0.02), yb+0.04, zz]); v, f = spike(base, base + [0, -0.09, 0], 0.025); b.part(v, f, 'Head', mt)
    # scutes: two rows down the back and one down the tail
    for nm in ('Neck2','Neck1','Torso3','Torso2','Torso','Back','Tail1','Tail2','Tail3','Tail4','Tail5','Tail6','Tail7'):
        jz = J[nm][2]
        if nm.startswith('Tail'): jz = J['Tail1'][2] - (J['Tail1'][2]-jz)*1.45
        sl = allP[(np.abs(allP[:,2]-jz) < 0.12) & (np.abs(allP[:,0]) < 0.3)]
        if not len(sl): continue
        top = sl[:,1].max()
        rows = (0,) if nm.startswith('Tail') and nm not in ('Tail1','Tail2') else (-0.13, 0.13)
        for dx in rows:
            base = np.array([dx, top-0.03, jz]); v, f = tube([base, base + [0, 0.12, -0.05]], [0.06, 0.0], seg=4); b.part(v, f, nm, ms)
    if size != 1.0: scale_root(b, size)
    # a stiff tail: everything past the root follows the first two tail bones (the wolf's tail swings far too much)
    k1, k2 = b.jn.index('Tail1'), b.jn.index('Tail2')
    for pr in b.prims:
        if 'JOINTS_0' not in pr['p']['attributes']: continue
        tl = b.infl(pr, ['Tail']); m = tl > 0.05
        if not m.any(): continue
        Jt = pr['J'].copy(); Wt = pr['W'].copy()
        Jt[m] = 0; Wt[m] = 0
        Jt[m, 0] = k1; Wt[m, 0] = 0.6; Jt[m, 1] = k2; Wt[m, 1] = 0.4
        rest = np.clip(1 - tl[m], 0, 1)   # keep some of the body's own weights at the root
        Wt[m, :2] *= (1 - rest)[:, None]
        oj = pr['J'][m]; ow = pr['W'][m]; nt = b.infl(pr, ['Tail'])[m]
        for i, (jj, ww) in enumerate(zip(oj, ow)):
            keep = [(a, c) for a, c in zip(jj, ww) if not b.jn[a].startswith('Tail') and c > 0][:2]
            for q, (a, c) in enumerate(keep): Jt[np.where(m)[0][i], 2+q] = a; Wt[np.where(m)[0][i], 2+q] = c
        Wt = Wt / np.maximum(Wt.sum(1, keepdims=True), 1e-6)
        b.g.setacc(pr['p']['attributes']['JOINTS_0'], Jt.astype(np.uint16) if b.g.j['accessors'][pr['p']['attributes']['JOINTS_0']]['componentType'] == 5123 else Jt.astype(np.uint8))
        b.g.setacc(pr['p']['attributes']['WEIGHTS_0'], Wt.astype(np.float32))
    b.write(out)
if __name__ == '__main__':
    build('/tmp/kit/out/b_croc.glb')
