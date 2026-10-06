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
