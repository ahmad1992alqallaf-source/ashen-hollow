# tiny SVG path rasterizer for game-icons (single-path, viewBox 0 0 512 512) using matplotlib Agg
import re, math, numpy as np
import matplotlib; matplotlib.use('Agg')
from matplotlib.path import Path
from matplotlib.backends.backend_agg import FigureCanvasAgg
from matplotlib.figure import Figure
from matplotlib.patches import PathPatch
TOK=re.compile(r'[MmLlHhVvCcSsQqTtAaZz]|[-+]?(?:\d*\.\d+|\d+\.?)(?:[eE][-+]?\d+)?')
def arc(x1,y1,rx,ry,phi,fa,fs,x2,y2):
    if rx==0 or ry==0: return [(x2,y2)]
    phi=math.radians(phi); c,s=math.cos(phi),math.sin(phi)
    dx,dy=(x1-x2)/2,(y1-y2)/2; x1p=c*dx+s*dy; y1p=-s*dx+c*dy
    rx,ry=abs(rx),abs(ry); lam=x1p**2/rx**2+y1p**2/ry**2
    if lam>1: rx*=math.sqrt(lam); ry*=math.sqrt(lam)
    num=rx*rx*ry*ry-rx*rx*y1p*y1p-ry*ry*x1p*x1p; den=rx*rx*y1p*y1p+ry*ry*x1p*x1p
    co=math.sqrt(max(0,num/den)) if den else 0
    if fa==fs: co=-co
    cxp=co*rx*y1p/ry; cyp=-co*ry*x1p/rx
    cx=c*cxp-s*cyp+(x1+x2)/2; cy=s*cxp+c*cyp+(y1+y2)/2
    def ang(u,v): a=math.atan2(v[1],v[0])-math.atan2(u[1],u[0]); return a
    t1=math.atan2((y1p-cyp)/ry,(x1p-cxp)/rx); dt=ang(((x1p-cxp)/rx,(y1p-cyp)/ry),((-x1p-cxp)/rx,(-y1p-cyp)/ry))
    if not fs and dt>0: dt-=2*math.pi
    elif fs and dt<0: dt+=2*math.pi
    n=max(2,int(abs(dt)/0.2)); pts=[]
    for i in range(1,n+1):
        t=t1+dt*i/n; pts.append((cx+rx*math.cos(t)*c-ry*math.sin(t)*s, cy+rx*math.cos(t)*s+ry*math.sin(t)*c))
    return pts
def parse(d):
    t=TOK.findall(d); i=0; verts=[]; codes=[]; cx=cy=sx=sy=0; cmd=None; lc=None
    def num():
        nonlocal i; v=float(t[i]); i+=1; return v
    while i<len(t):
        if re.match(r'[A-Za-z]',t[i]): cmd=t[i]; i+=1
        elif cmd in 'Mm': cmd='L' if cmd=='M' else 'l'
        rel=cmd.islower(); C=cmd.upper()
        if C=='Z':
            codes.append(Path.CLOSEPOLY); verts.append((sx,sy)); cx,cy=sx,sy; lc=None; continue
        if C=='M':
            x,y=num(),num()
            if rel: x+=cx;y+=cy
            cx,cy=sx,sy=x,y; verts.append((x,y)); codes.append(Path.MOVETO); lc=None
            cmd='l' if rel else 'L'; continue
        if C=='L':
            x,y=num(),num()
            if rel: x+=cx;y+=cy
            cx,cy=x,y; verts.append((x,y)); codes.append(Path.LINETO); lc=None
        elif C=='H':
            x=num(); cx=x+cx if rel else x; verts.append((cx,cy)); codes.append(Path.LINETO); lc=None
        elif C=='V':
            y=num(); cy=y+cy if rel else y; verts.append((cx,cy)); codes.append(Path.LINETO); lc=None
        elif C in 'CS':
            if C=='C':
                x1,y1=num(),num()
                if rel: x1+=cx;y1+=cy
            else:
                x1,y1=(2*cx-lc[0],2*cy-lc[1]) if lc and lc[2]=='c' else (cx,cy)
            x2,y2,x,y=num(),num(),num(),num()
            if rel: x2+=cx;y2+=cy;x+=cx;y+=cy
            verts+= [(x1,y1),(x2,y2),(x,y)]; codes+=[Path.CURVE4]*3; lc=(x2,y2,'c'); cx,cy=x,y
        elif C in 'QT':
            if C=='Q':
                x1,y1=num(),num()
                if rel: x1+=cx;y1+=cy
            else:
                x1,y1=(2*cx-lc[0],2*cy-lc[1]) if lc and lc[2]=='q' else (cx,cy)
            x,y=num(),num()
            if rel: x+=cx;y+=cy
            verts+=[(x1,y1),(x,y)]; codes+=[Path.CURVE3]*2; lc=(x1,y1,'q'); cx,cy=x,y
        elif C=='A':
            rx,ry,ph,fa,fs,x,y=num(),num(),num(),num(),num(),num(),num()
            if rel: x+=cx;y+=cy
            for p in arc(cx,cy,rx,ry,ph,int(fa),int(fs),x,y): verts.append(p); codes.append(Path.LINETO)
            cx,cy=x,y; lc=None
    return Path(np.array(verts),codes)
def mask(svgfile,size=256):
    s=open(svgfile).read()
    ds=re.findall(r'<path([^>]*)/?>',s)
    fig=Figure(figsize=(1,1),dpi=size); FigureCanvasAgg(fig)
    ax=fig.add_axes([0,0,1,1]); ax.set_xlim(0,512); ax.set_ylim(512,0); ax.axis('off')
    fig.patch.set_alpha(0); ax.patch.set_alpha(0)
    for attrs in ds:
        d=re.search(r'\bd="([^"]+)"',attrs).group(1)
        if d.strip() in ('M0 0h512v512H0z',): continue
        ax.add_patch(PathPatch(parse(d),facecolor='white',edgecolor='none',lw=0))
    fig.canvas.draw(); a=np.asarray(fig.canvas.buffer_rgba())[...,3].astype(np.float32)/255
    return a
if __name__=='__main__':
    import sys
    from PIL import Image
    m=mask(sys.argv[1]); Image.fromarray((m*255).astype('uint8')).save(sys.argv[2])
