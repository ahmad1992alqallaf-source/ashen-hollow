import json,struct,numpy as np
CT={5126:'f4',5123:'u2',5121:'u1',5125:'u4'}; NC={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}
def load(f):
  b=open(f,'rb').read(); n=struct.unpack('<I',b[12:16])[0]; j=json.loads(b[20:20+n]); o=20+n; bl=struct.unpack('<I',b[o:o+4])[0]
  return j, bytearray(b[o+8:o+8+bl])
def acc(j,BIN,i):
  a=j['accessors'][i]; bv=j['bufferViews'][a['bufferView']]; o=bv.get('byteOffset',0)+a.get('byteOffset',0)
  dt=np.dtype(CT[a['componentType']]); c=NC[a['type']]; st=bv.get('byteStride',dt.itemsize*c)
  raw=np.frombuffer(bytes(BIN),dtype=np.uint8)
  return np.lib.stride_tricks.as_strided(raw[o:], shape=(a['count'],st), strides=(st,1))[:, :dt.itemsize*c].copy().view(dt).reshape(a['count'],c)
def put(j,BIN,i,arr):
  a=j['accessors'][i]; bv=j['bufferViews'][a['bufferView']]; o=bv.get('byteOffset',0)+a.get('byteOffset',0)
  assert bv.get('byteStride') in (None, arr.dtype.itemsize*arr.shape[1])
  BIN[o:o+arr.nbytes]=arr.astype(CT[a['componentType']]).tobytes()
  if 'min' in a: a['min']=arr.min(0).tolist(); a['max']=arr.max(0).tolist()
def save(f,j,BIN):
  js=json.dumps(j,separators=(',',':')).encode()
  while len(js)%4: js+=b' '
  while len(BIN)%4: BIN.append(0)
  open(f,'wb').write(struct.pack('<III',0x46546C67,2,28+len(js)+len(BIN))+struct.pack('<I4s',len(js),b'JSON')+js+struct.pack('<I4s',len(BIN),b'BIN\0')+bytes(BIN))
def joints_world(j,BIN):
  s=j['skins'][0]; ibm=acc(j,BIN,s['inverseBindMatrices']).reshape(-1,4,4)
  W=[np.linalg.inv(m.T) for m in ibm]   # glTF matrices are column-major
  return np.array([w[:3,3] for w in W]), [j['nodes'][k].get('name') for k in s['joints']]
