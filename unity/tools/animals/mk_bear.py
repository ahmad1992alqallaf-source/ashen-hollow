# brown bear from the Quaternius wolf: barrel body with a shoulder hump, thick legs, broad round head, short muzzle,
# round ears, a stub tail and pale claws
import sys, numpy as np; sys.path.insert(0,'/tmp/kit')
from beast import *
def build(out, coat=0x5b3b24, light=0x7d5838, muzzle=0x9c7a55, claw=0xe8dcc0, nose=0x161010, size=1.0):
    b = Beast('/tmp/an/wolf.glb'); J = b.J; head = J['Head']
    for pr in b.prims:
        P = pr['P']
        t = b.infl(pr, ['Body$','Back$','Torso']); n = b.infl(pr, ['Neck']); h = b.infl(pr, ['Head']); e = b.infl(pr, ['Ear'])
        tl = b.infl(pr, ['Tail']); lf = b.infl(pr, ['FrontShoulder','FrontUpperLeg','FrontLowerLeg','IKFront','FF.']); lb = b.infl(pr, ['BackShoulder','BackLeg','BackUpperLeg','BackLowerLeg','IKBack','FFB'])
        x, y, z = P[:,0].copy(), P[:,1].copy(), P[:,2].copy()
        cy = 1.80; dy = y - cy
        k = t + 0.7*n
        x = x*(1 + 0.8*k)
        dy = np.where(dy < 0, dy*(1 + 1.1*t), dy*(1 + 0.35*t))
        hump = 0.42*(t+n)*np.exp(-((z-0.95)/0.45)**2)*(dy > -0.1)
        y = cy + dy + hump
        # neck short and thick, head pulled in
        x = x*(1 + 0.6*n); z = z - 0.18*n - 0.28*h
        y = y - 0.06*n + 0.02*h
        # head: broad and round; the muzzle short
        hz = z - (head[2]-0.28); hy = y - (head[1]-0.12)
        x = x*(1 + 0.3*h)
        z = np.where(h > 0.3, (head[2]-0.28) + np.where(hz > 0, hz*0.55, hz*1.1), z)
        y = y + h*hy*0.15
        # ears: small and round
        for side in ('L','R'):
            e1 = J['Ear1.'+side]; es = b.infl(pr, ['Ear1.'+side,'Ear2.'+side,'Ear3.'+side,'Ear4.'+side]); m = es > 0.3
            if m.any():
                c0 = e1 + np.array([0, -0.2, -0.46])
                v = np.c_[x,y,z][m] - c0; v *= 0.38; x[m], y[m], z[m] = (c0 + v + [0,0.02,0]).T
        # tail: a stub
        t1 = J['Tail1']; m = tl > 0.4
        v = np.c_[x,y,z][m] - t1; v *= 0.18; x[m], y[m], z[m] = (t1 + v).T
        # legs: thick pillars
        for w, front in ((lf, True), (lb, False)):
            for side in ('L','R'):
                sx = 1 if side == 'L' else -1; m = (w > 0.5) & (np.sign(P[:,0]) == sx)
                bone = J[('FrontUpperLeg.' if front else 'BackUpperLeg.')+side]
                x[m] = bone[0] + (x[m]-bone[0])*2.1 + sx*0.16
                zc = bone[2] - (0.02 if front else 0.2)
                z[m] = zc + (z[m]-zc)*1.7
        pr['P'] = np.c_[x,y,z]
    allP = np.vstack([pr['P'] for pr in b.prims])
    b.color('Main', coat, rough=1.0); b.color('Main_Light', light, rough=1.0); b.color('Nose', nose, rough=0.4); b.color('Eyes_Black', 0x0c0806, rough=0.3)
    mm = b.newmat('Muzzle', muzzle, 1.0); mc = b.newmat('Claw', claw, 0.5); mn = b.newmat('BearNose', nose, 0.35)
    # muzzle: a short rounded snout with a black nose
    hd = allP[allP[:,2] > head[2]-0.3]; tip = hd[hd[:,2].argmax()]
    c = np.array([0, tip[1]-0.06, tip[2]-0.12])
    v, f = blob(c, 0.2, seg=8, rings=5, sq=(1.05, 0.85, 1.0)); b.part(v, f, 'Head', mm)
    v, f = blob(c + [0, 0.07, 0.17], 0.075, seg=6, rings=3, sq=(1.3, 0.8, 0.9)); b.part(v, f, 'Head', mn)
    # round ears
    for sx, side in ((1,'L'), (-1,'R')):
        ep = J['Ear1.'+side] + np.array([0.02*sx, -0.16, -0.48])
        v, f = blob(ep, 0.12, seg=6, rings=4, sq=(1, 1, 0.55)); b.part(v, f, 'Head', b.mat('Main'))
    if size != 1.0: scale_root(b, size)
    b.write(out)
if __name__ == '__main__':
    build('/tmp/kit/out/b_bear.glb')
