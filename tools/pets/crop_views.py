from PIL import Image, ImageFilter
import os,sys
T='/tmp/claude-0/sheets/All_Four_Pets/full_turnaround_sheet.png'; G='/tmp/claude-0/sheets/Rock_Golem_Smaller/full_concept_sheet.png'
boxes={
 'Rock_Golem':(G,{'Front':(12,160,245,412),'Left':(262,160,415,412),'Back':(440,160,695,412),'Right':(700,160,840,412)}),
 'Spirit_Wisp':(T,{'Front':(785,68,918,256),'Left':(925,68,1062,256),'Back':(1062,68,1195,256),'Right':(1190,68,1305,256)}),
 'Night_Prowler':(T,{'Front':(8,600,190,760),'Left':(185,590,322,762),'Back':(300,590,402,762),'Right':(388,590,545,762)}),
 'Flame_Hatchling':(T,{'Front':(778,598,908,756),'Left':(905,598,1055,756),'Back':(1048,565,1148,756),'Right':(1140,565,1302,756)}),
}
out='/tmp/claude-0/tripo_in'; sheet=Image.new('RGB',(4*256,4*256))
for r,(n,(src,bx)) in enumerate(boxes.items()):
    im=Image.open(src).convert('RGB'); os.makedirs(f'{out}/{n}',exist_ok=True)
    for c,(v,b) in enumerate(bx.items()):
        cr=im.crop(b); w,h=cr.size; s=max(w,h)
        import numpy as np
        a=np.asarray(cr).astype(float); border=np.concatenate([a[0],a[-1],a[:,0],a[:,-1]]); bgc=np.median(border,0)
        lum=a.mean(2); d=np.abs(a-bgc).sum(2)
        # anything close to the dark background (panel edges, faint haze) becomes one flat colour
        m=np.clip((d-28)/30,0,1)[...,None]; a=a*m+bgc*(1-m); cr=Image.fromarray(a.astype('uint8')); bg=tuple(int(x) for x in bgc)
        sq=Image.new('RGB',(int(s*1.15),int(s*1.15)),bg); sq.paste(cr,((sq.width-w)//2,(sq.height-h)//2))
        big=sq.resize((1024,1024),Image.LANCZOS).filter(ImageFilter.UnsharpMask(radius=2,percent=60,threshold=2))
        big.save(f'{out}/{n}/{n}_{v}.png'); sheet.paste(big.resize((256,256)),(c*256,r*256))
sheet.save('/tmp/claude-0/tripo_in_sheet.jpg')
