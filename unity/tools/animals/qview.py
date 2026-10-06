import sys, numpy as np
sys.path.insert(0,'/tmp/rig3'); from qlook import loadP
from PIL import Image, ImageDraw
P=loadP(sys.argv[1]); out=sys.argv[2]
lo=P.min(0); hi=P.max(0); W=900; span=(hi-lo).max()*1.05; c=(lo+hi)/2
def panel(ax_h, ax_v, depth, vlabel):
    im=Image.new("RGB",(W,int(W*0.8)),(255,255,255)); d=ImageDraw.Draw(im)
    def tp(a,b): return ((a-c[ax_h])/span+0.5)*W, int(W*0.4) - (b-c[ax_v])/span*W
    o=np.argsort(P[:,depth]); dn=(P[:,depth]-lo[depth])/(hi[depth]-lo[depth]+1e-9)
    for i in o[::max(1,len(o)//40000)]:
        x,y=tp(P[i,ax_h],P[i,ax_v]); v=int(30+200*dn[i]); d.point((x,y),(v,v//2,255-v))
    for k in np.arange(np.floor(lo[ax_h]*10)/10, hi[ax_h]+0.1, 0.1):
        x,_=tp(k,0); d.line([(x,0),(x,W)],(225,225,225) if round(k*10)%5 else (170,170,170))
        if round(k*10)%5==0: d.text((x+2,2),f'{k:.1f}',(0,0,0))
    for k in np.arange(np.floor(lo[ax_v]*10)/10, hi[ax_v]+0.1, 0.1):
        _,y=tp(0,k); d.line([(0,y),(W,y)],(225,225,225) if round(k*10)%5 else (170,170,170))
        if round(k*10)%5==0: d.text((2,y-11),f'{vlabel}{k:.1f}',(0,0,0))
    return im
a=panel(2,1,0,'y'); b=panel(2,0,1,'x')
c2=Image.new('RGB',(W,a.height+b.height)); c2.paste(a,(0,0)); c2.paste(b,(0,a.height)); c2.save(out)
