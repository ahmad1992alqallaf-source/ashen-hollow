import sys, numpy as np, os
sys.path.insert(0, '/tmp/rig3'); from r3ds import read; import scene2glb
src, out = sys.argv[1], sys.argv[2]; texdir = os.path.dirname(src) + '/'
objs, mats = read(src)
groups = {}
for o in objs:
    if 'V' not in o or 'F' not in o: continue
    V = o['V'].astype(float); P = np.c_[V[:,0], V[:,2], -V[:,1]]
    UV = o.get('UV', np.zeros((len(V), 2), np.float32)).astype(np.float32).copy(); UV[:,1] = 1 - UV[:,1]
    fm = o['faces_mat'] or [(None, np.arange(len(o['F'])))]
    for mn, idx in fm:
        F = o['F'][idx]; key = mn
        g = groups.setdefault(key, {'P': [], 'UV': [], 'I': [], 'n': 0})
        g['P'].append(P); g['UV'].append(UV); g['I'].append(F.reshape(-1) + g['n']); g['n'] += len(P)
# solid-colour materials get a 4x4 swatch so the writer can treat them all as textured
from PIL import Image
parts = []
for mn, g in groups.items():
    m = mats.get(mn, {}); tex = m.get('tex')
    if tex and os.path.exists(texdir + tex): tname = tex
    else:
        c = m.get('diffuse') or [128, 128, 128]; tname = 'sw_%s.png' % ''.join(ch if ch.isalnum() else '_' for ch in str(mn))
        Image.new('RGB', (4, 4), tuple(c)).save(texdir + tname)
    parts.append(dict(P=np.vstack(g['P'])*100, UV=np.vstack(g['UV']), I=np.concatenate(g['I']).astype(np.uint32), tex=tname))
    print(mn, tname, sum(len(x) for x in g['P']), len(np.concatenate(g['I']))//3)
scene2glb.TEX = texdir
scene2glb.write(parts, out)
