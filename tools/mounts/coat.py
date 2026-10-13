import numpy as np, sys, json
from PIL import Image, ImageFilter
src='/tmp/claude-0/newmounts/horse_fbx/textures/'
alb=np.asarray(Image.open(src+'TX_Horse.Pinto_Albedo.png').convert('RGB').resize((1024,1024),Image.LANCZOS)).astype(np.float32)/255
hair=Image.open(src+'TX_HorseHair_Albedo.png').convert('LA').resize((1024,1024),Image.LANCZOS)
hl=np.asarray(hair).astype(np.float32)/255
r,g,b=alb[...,0],alb[...,1],alb[...,2]
Y=0.3*r+0.59*g+0.11*b; mx=alb.max(-1); mn=alb.min(-1); sat=(mx-mn)/(mx+1e-4)
pink=(r>0.55)&(r-g>0.18)&(sat>0.3)
white=(Y>0.45)&(sat<0.25)&~pink
dark=(Y<0.07)&~pink
brown=~white&~pink&~dark
def detail(mask):
    m=mask.astype(np.float32); Ym=Y*m
    # local mean inside the region (blurred), so the patch edges don't step
    bm=np.asarray(Image.fromarray((m*255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(24))).astype(np.float32)/255
    by=np.asarray(Image.fromarray((Ym*255).clip(0,255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(24))).astype(np.float32)/255
    return np.clip(Y/(by/np.maximum(bm,1e-3)+1e-3),0.35,1.4)
dw,db=detail(white),detail(brown)
def make(name, coat, mane, socks=None, hairsheen=1.0):
    c=np.array(coat,np.float32)/255
    out=alb.copy()
    for mask,d in ((white,dw),(brown,db)):
        out[mask]=(c[None,:]*d[mask][:,None])
    if socks is not None:
        s=np.array(socks,np.float32)/255; out[dark]=np.clip(s[None,:]*(0.6+Y[dark][:,None]*4),0,1)
    # soft blend at the old patch edges
    Image.fromarray((np.clip(out,0,1)*255).astype(np.uint8)).save('tex/%s_coat.png'%name)
    m=np.array(mane,np.float32)/255
    L=hl[...,0]; A=hl[...,1]
    h=np.zeros((1024,1024,4),np.float32); h[...,:3]=np.clip(m[None,None,:]*(0.45+0.9*L[...,None])*hairsheen,0,1); h[...,3]=A
    Image.fromarray((h*255).astype(np.uint8),'RGBA').save('tex/%s_mane.png'%name)
V=json.load(open('variants.json'))
for k,v in V.items(): make(k, v['coat'], v['mane'], v.get('socks'))
print('white %.2f brown %.2f dark %.2f pink %.3f'%(white.mean(),brown.mean(),dark.mean(),pink.mean()))
