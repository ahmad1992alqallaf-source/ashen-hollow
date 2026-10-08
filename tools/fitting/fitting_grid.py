import sys
from PIL import Image, ImageDraw, ImageFont
src,who,out=sys.argv[1],sys.argv[2],sys.argv[3]
PX=500; F=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',18); FB=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',28); FS=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',15)
views=[(n,Image.open(f'{src}/fit_{who}_{n}.png').convert('RGB')) for n in ('front','side','back')]
H=views[0][1].height; top=80; left=60; gap=40
W=left+sum(v.width for _,v in views)+gap*2+30
im=Image.new('RGB',(W,H+top+70),(250,248,244)); d=ImageDraw.Draw(im)
title={'m':'Male','f':'Female'}[who]+' fitting body: front, right side, back (true scale, 1 square = 10 cm)'
d.text((left,22),title,font=FB,fill=(30,30,40))
x=left
for n,v in views:
    # grid drawn into the picture: light lines every 10 cm, strong every 50 cm, centre line
    g=v.copy(); gd=ImageDraw.Draw(g,'RGBA')
    for cm in range(0,201,10):
        y=H-1-cm*PX/100; strong=cm%50==0
        gd.line([(0,y),(v.width,y)],fill=(40,60,120,150 if strong else 60),width=2 if strong else 1)
    cx=v.width/2
    for k in range(-20,21):
        xx=cx+k*PX/10
        if 0<=xx<v.width: gd.line([(xx,0),(xx,H)],fill=(40,60,120,150 if k==0 else (110 if k%5==0 else 45)),width=2 if k==0 else 1)
    im.paste(g,(x,top))
    d.text((x+6,top+H+10),{'front':'FRONT','side':'RIGHT SIDE (faces right)','back':'BACK'}[n],font=F,fill=(30,30,40))
    if x==left:
        for cm in range(0,201,10): d.text((6,top+H-1-cm*PX/100-9),str(cm),font=FS,fill=(80,80,95))
    x+=v.width+gap
d.text((left,top+H+38),'Ground at the bottom line; vertical centre line = middle of the body. Measure any armour piece against the squares.',font=FS,fill=(90,90,100))
im.save(out,quality=92)
print(im.size)
