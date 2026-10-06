# OBJ (quads/tris with v/vt) -> GLB with unwelded uv corners, for gltfpack to simplify
import sys, numpy as np, json, struct
src, out = sys.argv[1], sys.argv[2]
V=[];T=[];F=[]
with open(src, errors='ignore') as f:
    for ln in f:
        if ln.startswith('v '): V.append(ln.split()[1:4])
        elif ln.startswith('vt '): T.append(ln.split()[1:3])
        elif ln.startswith('f '):
            ps=[p.split('/') for p in ln.split()[1:]]
            vi=[int(p[0]) for p in ps]; ti=[int(p[1]) if len(p)>1 and p[1] else 0 for p in ps]
            for k in range(1,len(ps)-1): F.append((vi[0],ti[0],vi[k],ti[k],vi[k+1],ti[k+1]))
V=np.array(V,float); T=np.array(T,float) if T else np.zeros((1,2)); F=np.array(F,int)
pairs=np.stack([F[:,[0,1]],F[:,[2,3]],F[:,[4,5]]],1).reshape(-1,2)
pairs[pairs<0]+=0
u,inv=np.unique(pairs,axis=0,return_inverse=True)
P=V[u[:,0]-1].astype(np.float32); UV=T[np.maximum(u[:,1]-1,0)].astype(np.float32); UV[:,1]=1-UV[:,1]
I=inv.reshape(-1).astype(np.uint32)
print('verts',len(P),'tris',len(I)//3,'bbox',P.min(0),P.max(0))
bin=bytearray(); bvs=[]; accs=[]
def add(a,typ,ct,tgt,mm=False):
    global bin
    while len(bin)%4: bin.append(0)
    o=len(bin); bin+=a.tobytes(); bvs.append({'buffer':0,'byteOffset':o,'byteLength':a.nbytes,'target':tgt})
    ac={'bufferView':len(bvs)-1,'componentType':ct,'count':len(a) if a.ndim>1 else a.size,'type':typ}
    if mm: ac['min']=a.min(0).tolist(); ac['max']=a.max(0).tolist()
    accs.append(ac); return len(accs)-1
pa=add(P,'VEC3',5126,34962,True); ta=add(UV,'VEC2',5126,34962); ia=add(I,'SCALAR',5125,34963)
j={'asset':{'version':'2.0'},'scenes':[{'nodes':[0]}],'scene':0,'nodes':[{'mesh':0}],'meshes':[{'primitives':[{'attributes':{'POSITION':pa,'TEXCOORD_0':ta},'indices':ia,'material':0}]}],
 'materials':[{'pbrMetallicRoughness':{'baseColorFactor':[1,1,1,1]}}],'buffers':[{'byteLength':len(bin)}],'bufferViews':bvs,'accessors':accs}
js=json.dumps(j).encode(); js+=b' '*((4-len(js)%4)%4)
while len(bin)%4: bin.append(0)
with open(out,'wb') as f:
    f.write(struct.pack('<III',0x46546C67,2,12+8+len(js)+8+len(bin))); f.write(struct.pack('<II',len(js),0x4E4F534A)); f.write(js); f.write(struct.pack('<II',len(bin),0x004E4942)); f.write(bin)
