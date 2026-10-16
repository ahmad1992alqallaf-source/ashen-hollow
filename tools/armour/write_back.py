# Write simplified geometry (from dec.py) back into the original glb: the nodes, skin, bind poses, materials and
# pictures stay exactly as they were; only each mesh's vertices and triangles are replaced. Buffer repacked compactly.
import json, struct, sys, numpy as np
src, npz, out = sys.argv[1], sys.argv[2], sys.argv[3]
b = open(src, 'rb').read(); n = struct.unpack('<I', b[12:16])[0]
j = json.loads(b[20:20 + n]); o = 20 + n; bl = struct.unpack('<I', b[o:o + 4])[0]; BIN = b[o + 8:o + 8 + bl]
d = np.load(npz, allow_pickle=False)
meshes = {}
for k in d.files:
    nm, f = k.rsplit('|', 1); meshes.setdefault(nm, {})[f] = d[k]

skin = j['skins'][0]; jn = {j['nodes'][ji].get('name'): i for i, ji in enumerate(skin['joints'])}
new_views, new_acc, chunks = [], [], []
# keep every buffer view that is still used by something other than a replaced primitive
replaced = set()
for m in j['meshes']:
    if m.get('name') in meshes:
        for p in m['primitives']:
            replaced.update(p['attributes'].values());
            if 'indices' in p: replaced.add(p['indices'])
used_views = set()
for i, a in enumerate(j['accessors']):
    if i not in replaced and 'bufferView' in a: used_views.add(a['bufferView'])
for im in j.get('images', []):
    if 'bufferView' in im: used_views.add(im['bufferView'])
data = bytearray(); vmap = {}
def pad():
    while len(data) % 4: data.append(0)
for vi, v in enumerate(j['bufferViews']):
    if vi not in used_views: continue
    pad(); st = v.get('byteOffset', 0); raw = BIN[st:st + v['byteLength']]
    nv = dict(v); nv['byteOffset'] = len(data); data.extend(raw); vmap[vi] = len(new_views); new_views.append(nv)
amap = {}
for ai, a in enumerate(j['accessors']):
    if ai in replaced: continue
    na = dict(a)
    if 'bufferView' in a: na['bufferView'] = vmap[a['bufferView']]
    amap[ai] = len(new_acc); new_acc.append(na)
def add(arr, ctype, typ, target=None, minmax=False):
    pad(); bv = {'buffer': 0, 'byteOffset': len(data), 'byteLength': arr.nbytes}
    if target: bv['target'] = target
    data.extend(arr.tobytes()); new_views.append(bv)
    a = {'bufferView': len(new_views) - 1, 'componentType': ctype, 'count': int(arr.shape[0]), 'type': typ}
    if minmax: a['min'] = arr.min(0).tolist(); a['max'] = arr.max(0).tolist()
    new_acc.append(a); return len(new_acc) - 1
for m in j['meshes']:
    g = meshes.get(m.get('name'))
    for p in m['primitives']:
        if g is None:
            p['attributes'] = {k: amap[v] for k, v in p['attributes'].items()}
            if 'indices' in p: p['indices'] = amap[p['indices']]
            continue
        J = np.zeros((len(g['P']), 4), 'u1')
        for r in range(len(g['P'])):
            for c in range(4):
                nm = str(g['J'][r][c]);
                if nm: J[r, c] = jn[nm]
        W = g['W'].astype('f4'); W[J == 0] = np.where(np.char.str_len(g['J'].astype(str)) > 0, W, 0)[J == 0]
        attrs = {'POSITION': add(g['P'], 5126, 'VEC3', 34962, True), 'NORMAL': add(g['N'], 5126, 'VEC3', 34962),
                 'TEXCOORD_0': add(g['U'], 5126, 'VEC2', 34962), 'JOINTS_0': add(J, 5121, 'VEC4', 34962), 'WEIGHTS_0': add(W, 5126, 'VEC4', 34962)}
        ind = g['I'].astype('u2' if len(g['P']) < 65536 else 'u4')
        p['attributes'] = attrs; p['indices'] = add(ind, 5123 if ind.dtype == np.uint16 else 5125, 'SCALAR', 34963)
        p.pop('targets', None)
for s in j.get('skins', []):
    if 'inverseBindMatrices' in s: s['inverseBindMatrices'] = amap[s['inverseBindMatrices']]
j['accessors'] = new_acc; j['bufferViews'] = new_views; pad(); j['buffers'] = [{'byteLength': len(data)}]
js = json.dumps(j, separators=(',', ':')).encode();
while len(js) % 4: js += b' '
glb = struct.pack('<III', 0x46546C67, 2, 12 + 8 + len(js) + 8 + len(data)) + struct.pack('<I4s', len(js), b'JSON') + js + struct.pack('<I4s', len(data), b'BIN\0') + bytes(data)
open(out, 'wb').write(glb); print('wrote', out, len(glb) // 1024, 'KB')
