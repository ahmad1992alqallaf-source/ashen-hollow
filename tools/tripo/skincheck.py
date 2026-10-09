import json, struct, sys, numpy as np
from PIL import Image, ImageDraw
src_q, glb, outp = sys.argv[1:4]
code = open('/tmp/claude-0/tripo/fixbind.py').read(); pre = code.split("jq, _ = load(src_q)")[0].split("src_q, glb, delta, out = sys.argv[1:5]")
exec(pre[0]); exec(pre[1])
jq, _ = load(src_q); Gq = globals_(jq); M = {jq['nodes'][i].get('name'): Gq[i] for i in Gq}
ja, binb = load(glb)
def acc(i, comps, dt='f'):
    a = ja['accessors'][i]; bv = ja['bufferViews'][a['bufferView']]; off = bv.get('byteOffset', 0) + a.get('byteOffset', 0)
    n = a['count']; ct = {5126: ('f', 4), 5121: ('B', 1), 5123: ('H', 2)}[a['componentType']]
    stride = bv.get('byteStride', comps * ct[1])
    out = np.zeros((n, comps))
    for k in range(n): out[k] = struct.unpack_from('<' + ct[0] * comps, binb, off + k * stride)
    if a.get('normalized'): out /= {1: 255.0, 2: 65535.0}[ct[1]]
    return out
pr = ja['meshes'][0]['primitives'][0]; P = acc(pr['attributes']['POSITION'], 3)
J = acc(pr['attributes']['JOINTS_0'], 4).astype(int); W = acc(pr['attributes']['WEIGHTS_0'], 4)
sk = ja['skins'][0]; IB = acc(sk['inverseBindMatrices'], 16).reshape(-1, 4, 4).transpose(0, 2, 1)
names = [ja['nodes'][j].get('name') for j in sk['joints']]
Mats = np.array([M[n] @ IB[k] if n in M else np.eye(4) for k, n in enumerate(names)])
Ph = np.c_[P, np.ones(len(P))]
out = np.zeros((len(P), 3))
for c in range(4):
    T = Mats[J[:, c]]; out += W[:, c, None] * np.einsum('nij,nj->ni', T, Ph)[:, :3]
print('rest-pose bbox', out.min(0).round(2), out.max(0).round(2))
im = Image.new('RGB', (600, 600), 'white'); d = ImageDraw.Draw(im)
for x, y, z in out[::3]: d.point((300 + x * 250, 560 - y * 250), fill=(180, 60, 20))
im.save(outp)
