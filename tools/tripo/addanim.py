# copy named animations from one glb into another, matching target nodes by name (as Unity's animator does)
import json, struct, sys
def rd(p):
    b = open(p, 'rb').read(); n = struct.unpack('<I', b[12:16])[0]; j = json.loads(b[20:20 + n])
    o = 20 + n; bl = struct.unpack('<I', b[o:o + 4])[0]; return j, bytearray(b[o + 8:o + 8 + bl])
def wr(p, j, bin_):
    while len(bin_) % 4: bin_ += b'\0'
    js = json.dumps(j, separators=(',', ':')).encode()
    while len(js) % 4: js += b' '
    out = struct.pack('<III', 0x46546C67, 2, 12 + 8 + len(js) + 8 + len(bin_)) + struct.pack('<II', len(js), 0x4E4F534A) + js + struct.pack('<II', len(bin_), 0x004E4942) + bin_
    open(p, 'wb').write(out)
dst, src, out, names = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4].split(',')
J, B = rd(dst); S, SB = rd(src)
J['buffers'][0]['byteLength'] = len(B)
idx = {n.get('name'): i for i, n in enumerate(J['nodes'])}
J.setdefault('animations', []); J.setdefault('accessors', []); J.setdefault('bufferViews', [])
def copy_acc(ai):
    a = dict(S['accessors'][ai]); bv = S['bufferViews'][a['bufferView']]
    data = SB[bv.get('byteOffset', 0): bv.get('byteOffset', 0) + bv['byteLength']]
    while len(B) % 4: B.append(0)
    nb = {'buffer': 0, 'byteOffset': len(B), 'byteLength': len(data)}
    B.extend(data); J['bufferViews'].append(nb); a['bufferView'] = len(J['bufferViews']) - 1
    J['accessors'].append(a); return len(J['accessors']) - 1
for an in S['animations']:
    if an['name'] not in names: continue
    na = {'name': an['name'], 'samplers': [], 'channels': []}; cache = {}
    for ch in an['channels']:
        nn = S['nodes'][ch['target']['node']].get('name')
        if nn not in idx: continue
        s = an['samplers'][ch['sampler']]
        k = (s['input'], s['output'])
        if k not in cache:
            na['samplers'].append({'input': copy_acc(s['input']), 'output': copy_acc(s['output']), 'interpolation': s.get('interpolation', 'LINEAR')}); cache[k] = len(na['samplers']) - 1
        na['channels'].append({'sampler': cache[k], 'target': {'node': idx[nn], 'path': ch['target']['path']}})
    J['animations'].append(na); print('added', an['name'], len(na['channels']))
J['buffers'][0]['byteLength'] = len(B)
wr(out, J, B)
