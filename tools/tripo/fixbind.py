# rebuild a rigged armour's inverse bind matrices in the game skeleton's own bone frames:
# bind_j = C * D_j * C^-1 * M_j, with M_j the skeleton's rest joint (global, glTF space, from the source outfit) and
# D_j how far Blender posed that bone (world, Blender space); C turns Blender's Z-up into glTF's Y-up
import json, struct, sys, numpy as np
src_q, glb, delta, out = sys.argv[1:5]
def load(p):
    b = open(p, 'rb').read(); n = struct.unpack('<I', b[12:16])[0]; j = json.loads(b[20:20 + n])
    bin_off = 20 + n; bl = struct.unpack('<I', b[bin_off:bin_off + 4])[0]; binb = bytearray(b[bin_off + 8:bin_off + 8 + bl])
    return j, binb
def local(n):
    if 'matrix' in n: return np.array(n['matrix'], dtype=float).reshape(4, 4).T
    t = n.get('translation', [0, 0, 0]); r = n.get('rotation', [0, 0, 0, 1]); s = n.get('scale', [1, 1, 1])
    x, y, z, w = r
    R = np.array([[1-2*(y*y+z*z), 2*(x*y-z*w), 2*(x*z+y*w)], [2*(x*y+z*w), 1-2*(x*x+z*z), 2*(y*z-x*w)], [2*(x*z-y*w), 2*(y*z+x*w), 1-2*(x*x+y*y)]])
    M = np.eye(4); M[:3, :3] = R * np.array(s)[None, :]; M[:3, 3] = t; return M
def globals_(j):
    par = {}
    for i, n in enumerate(j['nodes']):
        for c in n.get('children', []): par[c] = i
    G = {}
    def g(i):
        if i in G: return G[i]
        m = local(j['nodes'][i]); G[i] = (g(par[i]) @ m) if i in par else m; return G[i]
    for i in range(len(j['nodes'])): g(i)
    return G
jq, _ = load(src_q); Gq = globals_(jq); M = {jq['nodes'][i].get('name'): Gq[i] for i in Gq}
ja, binb = load(glb)
D = {k: np.array(v) for k, v in json.load(open(delta)).items()}
C = np.array([[1, 0, 0, 0], [0, 0, 1, 0], [0, -1, 0, 0], [0, 0, 0, 1]], dtype=float)   # Blender (x, y, z) -> glTF (x, z, -y)
sk = ja['skins'][0]; acc = ja['accessors'][sk['inverseBindMatrices']]; bv = ja['bufferViews'][acc['bufferView']]
off = bv.get('byteOffset', 0) + acc.get('byteOffset', 0)
changed = 0
for k, ji in enumerate(sk['joints']):
    name = ja['nodes'][ji].get('name')
    if name not in M: continue
    Dj = D.get(name, np.eye(4))
    bind = C @ Dj @ np.linalg.inv(C) @ M[name]
    ibm = np.linalg.inv(bind)
    struct.pack_into('<16f', binb, off + k * 64, *ibm.T.reshape(-1)); changed += 1
# write back
js = json.dumps(ja, separators=(',', ':')).encode(); js += b' ' * ((4 - len(js) % 4) % 4)
binb += b'\0' * ((4 - len(binb) % 4) % 4)
total = 12 + 8 + len(js) + 8 + len(binb)
with open(out, 'wb') as f:
    f.write(struct.pack('<III', 0x46546C67, 2, total)); f.write(struct.pack('<I', len(js)) + b'JSON'); f.write(js)
    f.write(struct.pack('<I', len(binb)) + b'BIN\0'); f.write(binb)
print('ibm rewritten', changed, 'of', len(sk['joints']))
