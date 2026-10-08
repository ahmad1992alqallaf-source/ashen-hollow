import re, sys
from PIL import Image, ImageDraw, ImageFont
src, out, who = sys.argv[1], sys.argv[2], sys.argv[3]
B={}
for l in open(src):
    m=re.match(r'bone (\w+) = (\S+) pos=\(([-\d.]+), ([-\d.]+), ([-\d.]+)\)',l)
    if m: B[m.group(1)]=(float(m.group(3)),float(m.group(4)),float(m.group(5)))
k=B['Head'][1]/1.571; top=B['Head'][1]+0.19*k
F=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',18); FB=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',26); FS=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',14)
W,H=2300,1260; im=Image.new('RGB',(W,H),(250,248,244)); d=ImageDraw.Draw(im); sc=560
def grid(ox,half,title):
    d.text((ox-half,22),title,font=FB,fill=(30,30,40))
    for cm in range(0,190,10):
        y=1150-cm/100*sc; d.line([(ox-half,y),(ox+half,y)],fill=(170,170,185) if cm%50==0 else (220,220,225),width=1)
        d.text((ox-half-38,y-9),str(cm),font=FS,fill=(120,120,130))
def P(ox,x,y): return (ox+x*sc,1150-y*sc)
bones=[('Hips','Spine'),('Spine','Chest'),('Chest','UpperChest'),('UpperChest','Neck'),('Neck','Head')]
for s in ('Left','Right'):
    bones+=[('UpperChest',s+'Shoulder'),(s+'Shoulder',s+'UpperArm'),(s+'UpperArm',s+'LowerArm'),(s+'LowerArm',s+'Hand'),(s+'Hand',s+'MiddleDistal'),('Hips',s+'UpperLeg'),(s+'UpperLeg',s+'LowerLeg'),(s+'LowerLeg',s+'Foot'),(s+'Foot',s+'Toes')]
skip=lambda n:any(k in n for k in('Proximal','Intermediate','Distal','Eye'))
ox=560; grid(ox,480,'FRONT (T-pose) — '+who)
for a,b in bones: d.line([P(ox,B[a][0],B[a][1]),P(ox,B[b][0],B[b][1])],fill=(60,70,110),width=6)
for n,(x,y,z) in B.items():
    if skip(n): continue
    px,py=P(ox,x,y); d.ellipse([px-8,py-8,px+8,py+8],fill=(230,120,40),outline=(90,40,10))
hx,hy=P(ox,0,B['Head'][1]); d.ellipse([hx-0.105*k*sc,hy-0.235*k*sc,hx+0.105*k*sc,hy+0.01*sc],outline=(60,70,110),width=3)
cm=lambda v:f'{v*100:.1f}'
lab={'Head':'Head','Neck':'Neck','UpperChest':'Upper chest','Chest':'Chest','Spine':'Waist (spine)','Hips':'Hips','RightUpperLeg':'Hip joint','RightLowerLeg':'Knee','RightFoot':'Ankle','RightUpperArm':'Shoulder','RightLowerArm':'Elbow','RightHand':'Wrist'}
for n,t in lab.items():
    x,y,z=B[n]; px,py=P(ox,x,y); d.text((px-185,py-12) if t=='Upper chest' else (px-30,py-42) if t=='Elbow' else (px-60,py+14) if t=='Shoulder' else (px-30,py+14) if t=='Wrist' else (px+12,py-24),f'{t} {cm(y)}',font=F,fill=(20,20,30))
ox=1300; grid(ox,200,'SIDE (facing right)')
for a,b in bones:
    if 'Left' in a+b: continue
    d.line([P(ox,B[a][2],B[a][1]),P(ox,B[b][2],B[b][1])],fill=(60,70,110),width=6)
for n,(x,y,z) in B.items():
    if 'Left' in n or skip(n): continue
    px,py=P(ox,z,y); d.ellipse([px-8,py-8,px+8,py+8],fill=(230,120,40),outline=(90,40,10))
hx,hy=P(ox,B['Head'][2],B['Head'][1]); d.ellipse([hx-0.11*k*sc,hy-0.235*k*sc,hx+0.12*k*sc,hy+0.01*sc],outline=(60,70,110),width=3)
# measurements table
L=lambda a,b:sum((B[a][i]-B[b][i])**2 for i in range(3))**0.5
rows=[('Shoulder joints, left to right',2*abs(B['RightUpperArm'][0])),('Hip joints, left to right',2*abs(B['RightUpperLeg'][0])),
('Upper arm (shoulder to elbow)',L('RightUpperArm','RightLowerArm')),('Forearm (elbow to wrist)',L('RightLowerArm','RightHand')),('Hand (wrist to fingertip)',L('RightHand','RightMiddleDistal')+0.02),
('Thigh (hip to knee)',L('RightUpperLeg','RightLowerLeg')),('Shin (knee to ankle)',L('RightLowerLeg','RightFoot')),('Ankle height',B['RightFoot'][1]),
('Torso (hips to neck)',L('Hips','Neck')),('Neck to head joint',L('Neck','Head')),('Arm span, fingertip to fingertip',2*abs(B['RightMiddleDistal'][0])+0.04)]
x0=1560; d.text((x0,90),'Measurements (cm, joint to joint)',font=FB,fill=(30,30,40)); y=140
for t,v in rows: d.text((x0,y),t,font=F,fill=(30,30,40)); d.text((x0+520,y),cm(v),font=F,fill=(170,70,20)); y+=34
notes=[f'Height: about {top*100:.0f} cm to the top of the head.','Units: metres in the files; 1 grid line = 10 cm.','Rest pose: T-pose, arms straight out, palms down,','facing +Z (toes point forward).','These are bone joints (the centre of the limb), not',
'the skin: the real body surface comes next, from','the hero model itself (front/side/back pictures and','a body file you can load in Tripo, Meshy or Blender).','Armour should leave about 1–3 cm over the skin','and keep joints (elbow, knee, hip, shoulder) open','so the hero can bend.']
y+=20
for t in notes: d.text((x0,y),t,font=F,fill=(60,60,70)); y+=28
im.save(out)
