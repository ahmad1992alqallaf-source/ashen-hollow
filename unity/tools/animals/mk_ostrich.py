# ostrich from the raptor rig: a plump body of black feathers, a long bare neck held up, a small head with a flat beak,
# little white-tipped wings, a white tail plume and long bare legs
import sys, numpy as np; sys.path.insert(0,'/tmp/kit')
from beast import *
SRC = '/mnt/user-data/outputs/AshenHollow/Resources/AH/Models/Beasts/b_raptor.glb'
def build(out, skin=0xc39a8c, body=0x1c1a1a, plume=0xf4efe4, beak=0xd8b48a, leg=0xb48a7c):
    b = Beast(SRC); J = b.J
    n1 = J['Neck_1']; hd = J['Head']; t1 = J['Tail_1']; sp = (J['Spine_1'] + J['Spine_3'])/2
    for pr in b.prims:
        P = pr['P']
        nk = b.infl(pr, ['Neck']); h = b.infl(pr, ['Head','Jaw','Tongue']); tl = b.infl(pr, ['Tail'])
        arm = b.infl(pr, ['Shoulder','UpperArm','LowerArm','Hand','Pinky','Middle','Fore']); torso = b.infl(pr, ['Spine','Breast','Pelvis'])
        x, y, z = P[:,0].copy(), P[:,1].copy(), P[:,2].copy()
        # body: round and plump
        m = torso > 0.2; f = np.clip(torso, 0, 1)
        x = x + (x - sp[0])*0.45*f; y = y + (y - sp[1])*0.4*f; z = z + (z - sp[2])*0.1*f
        # neck: long and upright; head small
        w = np.clip(nk + h, 0, 1)
        v = np.c_[x,y,z] - n1
        v = v*np.c_[1 - 0.35*w, 1 + 1.1*w, 1 - 0.15*w]
        x, y, z = (n1 + v).T
        hd2 = n1 + (hd - n1)*np.array([0.65, 2.1, 0.85])
        m = h > 0.3
        v = np.c_[x,y,z][m] - hd2; v *= 0.55; x[m], y[m], z[m] = (hd2 + v).T
        # tail: a short stub
        m = tl > 0.3; v = np.c_[x,y,z][m] - t1; v *= np.c_[0.9, 0.5, 0.22][0]; x[m], y[m], z[m] = (t1 + v).T
        # arms: little wings folded at the sides
        m = arm > 0.5
        for sx, side in ((1,'L'), (-1,'R')):
            s0 = J['Shoulder.'+side]; mm = m & (np.sign(P[:,0]) == sx)
            v = np.c_[x,y,z][mm] - s0; v *= 0.5; x[mm], y[mm], z[mm] = (s0 + v + [0.12*sx, 0.02, -0.05]).T
        pr['P'] = np.c_[x,y,z]
    rebind(b, ['Tail'], 'Tail_1')
    # bare skin everywhere first, then the feathers over the body
    for mt in b.g.j['materials']:
        pbr = mt.setdefault('pbrMetallicRoughness', {}); pbr.pop('baseColorTexture', None); pbr['baseColorFactor'] = lin(skin) + [1.0]; pbr['roughnessFactor'] = 0.8; pbr['metallicFactor'] = 0.0
    mf = b.newmat('Feathers', body, 1.0); mp = b.newmat('Plume', plume, 1.0); mb = b.newmat('Beak', beak, 0.5); me = b.newmat('Eye', 0x100c0a, 0.2)
    def tri_infl(pr, names):
        w = b.infl(pr, names); I = b.g.acc(pr['p']['indices']).reshape(-1,3); return w[I].mean(1)
    def pick(cen, fn, pr):
        t = tri_infl(pr, ['Spine','Breast','Pelvis','Tail_1','Tail_2','Hip','Shoulder','UpperArm']); return (t > 0.45) & (cen[:,1] > 0.85)
    shell(b, pick, 0.05, mf)
    allP = np.vstack([pr['P'] for pr in b.prims])
    # beak and eyes on the small head
    hd2 = n1 + (hd - n1)*np.array([0.65, 2.1, 0.85])
    near = allP[np.linalg.norm(allP - hd2, axis=1) < 0.35]
    fz = near[:,2].max(); cy = np.median(near[:,1])
    v, f = tube([np.array([0, cy-0.02, fz-0.08]), np.array([0, cy-0.04, fz+0.14])], [0.07, 0.02], seg=6); b.part(v, f, 'Head', mb)
    for sx in (-1, 1):
        xs = near[:,0].max() if sx > 0 else near[:,0].min()
        v, f = blob(np.array([xs*0.9, cy+0.04, fz-0.14]), 0.03, seg=6, rings=3); b.part(v, f, 'Head', me)
    # white plumes: the tail fan and the wing tips
    tt = t1 + (J['Tail_3'] - t1)*0.22
    for a in np.linspace(-0.9, 0.9, 7):
        base = tt + [0, 0.05, 0.05]; tip = base + [0.35*np.sin(a), 0.32 + 0.1*np.cos(a), -0.28]
        v, f = tube([base, (base+tip)/2 + [0, 0.05, 0], tip], [0.07, 0.08, 0.0], seg=5); b.part(v, f, 'Tail_1', mp)
    for sx, side in ((1,'L'), (-1,'R')):
        s0 = J['Shoulder.'+side]; c = s0 + (J['Hand.'+side] - s0)*0.5 + [0.22*sx, 0.02, -0.25]
        for k in range(4):
            base = c + [0, 0.05*k - 0.05, 0.08*k]; tip = base + [0.08*sx, -0.05, -0.38]
            v, f = tube([base, tip], [0.07, 0.0], seg=5); b.part(v, f, 'LowerArm.'+side, mp)
    b.write(out)
if __name__ == '__main__':
    build('/tmp/kit/out/b_ostrich.glb')
