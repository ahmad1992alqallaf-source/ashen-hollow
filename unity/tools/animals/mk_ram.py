# mountain ram from the chamois (Quaternius rig): a heavy grey-brown fleece and great curled horns
import sys, numpy as np; sys.path.insert(0,'/tmp/kit')
from beast import *
from mk_farm import noise3
def build(out, coat=0x7a6a58, light=0xd8ccb4, dark=0x3e342a, fleece=0xb3a58c, horn=0xcdbb92):
    b = Beast('/tmp/an/pj_chamois.glb'); J = b.J
    for pr in b.prims:
        P = pr['P']; t = b.infl(pr, ['Body$','Back$','Torso']); n = b.infl(pr, ['Neck'])
        x, y, z = P[:,0].copy(), P[:,1].copy(), P[:,2].copy()
        x = x*(1 + 0.45*t + 0.3*n); cy = 0.66; dy = y - cy; y = cy + dy*(1 + 0.25*t)
        pr['P'] = np.c_[x,y,z]
    b.color('Main', coat, rough=0.95); b.color('Main_Light', light, rough=0.95); b.color('Main_Dark', dark, rough=0.8)
    b.color('Hooves', 0x221c18); b.color('Horn', horn, rough=0.6)
    fl = b.newmat('Fleece', fleece, 1.0); hm = b.newmat('RamHorn', horn, 0.55)
    zb = J['Back'][2] - 0.12; zf = J['Neck2'][2] + 0.04
    shell(b, lambda c, nn, pr: (c[:,2] > zb) & (c[:,2] < zf) & (c[:,1] > 0.45), 0, fl, offset_fn=lambda P: 0.05 + 0.025*noise3(P, 20.0))
    # curled horns: a thick spiral from the crown, back, down and round to the front under the ears
    h = J['Head']
    for sx in (-1, 1):
        pts = []; rad = []
        for i in range(16):
            a = i/15*1.75*np.pi; r = 0.13*(1 - 0.35*i/15)
            c = h + np.array([sx*(0.07 + 0.08*i/15), 0.05, -0.08])
            pts.append(c + np.array([sx*0.03*np.sin(a*0.5), r*np.sin(a) + 0.04, -r*np.cos(a) + r]))
            rad.append(0.055*(1 - 0.75*i/15) + 0.008)
        v, f = tube(pts, rad, seg=7); b.part(v, f, 'Head', hm)
    b.write(out)
if __name__ == '__main__':
    build('/tmp/kit/out/b_ram.glb')
