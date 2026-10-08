# drop the Quaternius skin (MI_Regular_*) primitives, materials, textures and images from a .gltf; writes a new .gltf next to the original's .bin
import json, sys, os
src, out = sys.argv[1], sys.argv[2]
d = json.load(open(src))
mats = d['materials']; drop = {i for i, m in enumerate(mats) if m['name'].startswith('MI_Regular')}
keepM = [i for i in range(len(mats)) if i not in drop]; mmap = {o: n for n, o in enumerate(keepM)}
for m in d['meshes']:
    m['primitives'] = [p for p in m['primitives'] if p.get('material', 0) not in drop]
    for p in m['primitives']:
        if 'material' in p: p['material'] = mmap[p['material']]
d['materials'] = [mats[i] for i in keepM]
# textures still used
used = set()
def walk(o):
    if isinstance(o, dict):
        for k, v in o.items():
            if k.endswith('Texture') and isinstance(v, dict) and 'index' in v: used.add(v['index'])
            walk(v)
    elif isinstance(o, list):
        for v in o: walk(v)
walk(d['materials'])
keepT = sorted(used); tmap = {o: n for n, o in enumerate(keepT)}
def remap(o):
    if isinstance(o, dict):
        for k, v in o.items():
            if k.endswith('Texture') and isinstance(v, dict) and 'index' in v: v['index'] = tmap[v['index']]
            remap(v)
    elif isinstance(o, list):
        for v in o: remap(v)
remap(d['materials'])
tex = [d['textures'][i] for i in keepT]
keepI = sorted({t['source'] for t in tex}); imap = {o: n for n, o in enumerate(keepI)}
for t in tex: t['source'] = imap[t['source']]
d['textures'] = tex; d['images'] = [d['images'][i] for i in keepI]
# meshes left with no primitives: drop their nodes' mesh reference
empty = {i for i, m in enumerate(d['meshes']) if not m['primitives']}
if empty:
    print('empty meshes', empty)
    for n in d['nodes']:
        if n.get('mesh') in empty: n.pop('mesh'); n.pop('skin', None)
json.dump(d, open(out, 'w'))
print(out, 'dropped materials', [mats[i]['name'] for i in drop])
