# tiny glTF/GLB editor: numpy access to accessors, add data, save
import json, struct, numpy as np, copy
CT = {5120:np.int8,5121:np.uint8,5122:np.int16,5123:np.uint16,5125:np.uint32,5126:np.float32}
NC = {'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}
class GLB:
    def __init__(s, path):
        b = open(path,'rb').read(); l = struct.unpack('<I', b[12:16])[0]
        s.j = json.loads(b[20:20+l]); o = 20+l
        bl = struct.unpack('<I', b[o:o+4])[0]; s.bin = bytearray(b[o+8:o+8+bl])
    def acc(s, i):
        a = s.j['accessors'][i]; bv = s.j['bufferViews'][a['bufferView']]
        dt = CT[a['componentType']]; n = NC[a['type']]; off = bv.get('byteOffset',0)+a.get('byteOffset',0)
        stride = bv.get('byteStride'); isz = np.dtype(dt).itemsize*n
        if stride and stride != isz:
            raw = np.frombuffer(bytes(s.bin), dtype=np.uint8)
            rows = np.stack([raw[off+k*stride: off+k*stride+isz] for k in range(a['count'])])
            arr = rows.view(dt).reshape(a['count'], n)
        else:
            arr = np.frombuffer(bytes(s.bin[off:off+a['count']*isz]), dtype=dt).reshape(a['count'], n)
        arr = arr.copy()
        if a.get('normalized'):
            arr = arr.astype(np.float32) / np.iinfo(dt).max
        return arr
    def add(s, arr, typ, ctype=5126, target=None, minmax=False):
        arr = np.ascontiguousarray(arr.astype(CT[ctype]))
        while len(s.bin) % 4: s.bin.append(0)
        off = len(s.bin); s.bin += arr.tobytes()
        bv = {'buffer':0,'byteOffset':off,'byteLength':arr.nbytes}
        if target: bv['target'] = target
        s.j['bufferViews'].append(bv)
        a = {'bufferView':len(s.j['bufferViews'])-1,'componentType':ctype,'count':int(arr.shape[0]),'type':typ}
        if minmax: a['min'] = [float(x) for x in arr.reshape(arr.shape[0],-1).min(0)]; a['max'] = [float(x) for x in arr.reshape(arr.shape[0],-1).max(0)]
        s.j['accessors'].append(a); return len(s.j['accessors'])-1
    def setacc(s, i, arr):
        # replace accessor data with new data (float vec), keep index
        a = s.j['accessors'][i]; norm = a.get('normalized') and a['componentType'] != 5126
        if norm: arr = np.clip(np.round(np.asarray(arr, np.float64) * np.iinfo(CT[a['componentType']]).max), 0, np.iinfo(CT[a['componentType']]).max)
        ni = s.add(arr, a['type'], a['componentType'], minmax='min' in a)
        s.j['accessors'][i] = s.j['accessors'][ni]; s.j['accessors'].pop()
        if norm: s.j['accessors'][i]['normalized'] = True
    def node(s, name):
        for i,n in enumerate(s.j['nodes']):
            if n.get('name') == name: return i
    def world(s):
        # world matrices of all nodes (rest pose)
        N = s.j['nodes']; par = {}
        for i,n in enumerate(N):
            for c in n.get('children',[]): par[c] = i
        W = [None]*len(N)
        def loc(n):
            if 'matrix' in n: return np.array(n['matrix']).reshape(4,4).T
            t = n.get('translation',[0,0,0]); r = n.get('rotation',[0,0,0,1]); sc = n.get('scale',[1,1,1])
            x,y,z,w = r
            R = np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])
            M = np.eye(4); M[:3,:3] = R*np.array(sc); M[:3,3] = t; return M
        def w(i):
            if W[i] is None: W[i] = (w(par[i]) if i in par else np.eye(4)) @ loc(N[i])
            return W[i]
        for i in range(len(N)): w(i)
        return W, par
    def dedupe_anims(s):
        A = s.j.get('animations',[]); keep = [a for a in A if '|' not in a.get('name','')]
        if keep: s.j['animations'] = keep
    def save(s, path):
        # drop unused bufferViews by rebuilding the binary from referenced views
        used = set(a['bufferView'] for a in s.j['accessors'] if 'bufferView' in a)
        for a in s.j['accessors']:
            sp = a.get('sparse')
            if sp: used.add(sp['indices']['bufferView']); used.add(sp['values']['bufferView'])
        for im in s.j.get('images',[]):
            if 'bufferView' in im: used.add(im['bufferView'])
        newbin = bytearray(); remap = {}; views = []
        for i,bv in enumerate(s.j['bufferViews']):
            if i not in used: continue
            while len(newbin) % 4: newbin.append(0)
            o = bv.get('byteOffset',0); data = s.bin[o:o+bv['byteLength']]
            nb = dict(bv); nb['byteOffset'] = len(newbin); newbin += data
            remap[i] = len(views); views.append(nb)
        for a in s.j['accessors']:
            if 'bufferView' in a: a['bufferView'] = remap[a['bufferView']]
            sp = a.get('sparse')
            if sp: sp['indices']['bufferView'] = remap[sp['indices']['bufferView']]; sp['values']['bufferView'] = remap[sp['values']['bufferView']]
        for im in s.j.get('images',[]):
            if 'bufferView' in im: im['bufferView'] = remap[im['bufferView']]
        s.j['bufferViews'] = views
        while len(newbin) % 4: newbin.append(0)
        s.j['buffers'] = [{'byteLength':len(newbin)}]
        js = json.dumps(s.j, separators=(',',':')).encode()
        while len(js) % 4: js += b' '
        out = struct.pack('<III',0x46546C67,2,12+8+len(js)+8+len(newbin)) + struct.pack('<II',len(js),0x4E4F534A) + js + struct.pack('<II',len(newbin),0x004E4942) + bytes(newbin)
        open(path,'wb').write(out); s.bin = newbin
