import sys; sys.path.insert(0,'/tmp/rig3')
from fbx import parse
import numpy as np, json, struct
t=parse(sys.argv[1]); out=sys.argv[2]
ob=[n for n in t if n[0]=='Objects'][0]; geo=[n for n in ob[2] if n[0]=='Geometry'][0]
K={k[0]:k for k in geo[2]}
V=K['Vertices'][1][0].reshape(-1,3); PV=K['PolygonVertexIndex'][1][0].astype(int)
uvl={k[0]:k for k in K['LayerElementUV'][2]}; UVs=uvl['UV'][1][0].reshape(-1,2); UVI=uvl['UVIndex'][1][0].astype(int)
tris=[]; poly=[]
for c,i in enumerate(PV):
    if i<0: poly.append((~i,c)); 
    else: poly.append((i,c)); continue
    for k in range(1,len(poly)-1): tris.append((poly[0],poly[k],poly[k+1]))
    poly=[]
pairs=np.array([(vi,UVI[c]) for tr in tris for vi,c in tr])
u,inv=np.unique(pairs,axis=0,return_inverse=True)
P=V[u[:,0]].astype(np.float32); UV=UVs[u[:,1]].astype(np.float32); UV[:,1]=1-UV[:,1]; I=inv.reshape(-1).astype(np.uint32)
print('verts',len(P),'tris',len(I)//3,P.min(0),P.max(0))
exec(open('/tmp/rig3/wglb.py').read())
