# Meshy's rigged model plus each of its animation files (same skeleton): every file's clip copied onto the model's own
# joints by name and named Idle / Walk / Attack / ... , without loading any of the heavy meshes into Blender
import sys
sys.argv = [sys.argv[0]] + sys.argv[1:]
exec(open('/tmp/claude-0/runchk/addanim.py').read().split('dst, src')[0])
rig, out = sys.argv[1], sys.argv[2]; pairs = sys.argv[3:]
J, B = rd(rig); J['buffers'][0]['byteLength'] = len(B)
idx = {n.get('name'): i for i, n in enumerate(J['nodes'])}
J.setdefault('animations', [])
J['animations'] = []
for pr in pairs:
    name, f = pr.split('=', 1); S, SB = rd(f)
    def copy_acc(ai):
        a = dict(S['accessors'][ai]); bv = S['bufferViews'][a['bufferView']]
        data = SB[bv.get('byteOffset', 0): bv.get('byteOffset', 0) + bv['byteLength']]
        while len(B) % 4: B.append(0)
        nb = {'buffer': 0, 'byteOffset': len(B), 'byteLength': len(data)}
        if 'byteStride' in bv: nb['byteStride'] = bv['byteStride']
        B.extend(data); J['bufferViews'].append(nb); a['bufferView'] = len(J['bufferViews']) - 1
        J['accessors'].append(a); return len(J['accessors']) - 1
    an = S['animations'][0]; na = {'name': name, 'samplers': [], 'channels': []}; cache = {}
    for ch in an['channels']:
        nn = S['nodes'][ch['target']['node']].get('name')
        if nn not in idx: continue
        s = an['samplers'][ch['sampler']]; k = (s['input'], s['output'])
        if k not in cache:
            na['samplers'].append({'input': copy_acc(s['input']), 'output': copy_acc(s['output']), 'interpolation': s.get('interpolation', 'LINEAR')}); cache[k] = len(na['samplers']) - 1
        na['channels'].append({'sampler': cache[k], 'target': {'node': idx[nn], 'path': ch['target']['path']}})
    J['animations'].append(na); print('clip', name, len(na['channels']))
J['buffers'][0]['byteLength'] = len(B)
wr(out, J, B); print('merged', out)
