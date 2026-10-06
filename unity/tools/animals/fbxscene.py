# static meshes out of a Sketchfab-made FBX scene: world transforms, uvs, the diffuse texture of each mesh
import sys, numpy as np, re, os
sys.path.insert(0, '/tmp/rig3'); from fbx import parse
def eul(r):
    x, y, z = np.radians(r); cx, sx, cy, sy, cz, sz = np.cos(x), np.sin(x), np.cos(y), np.sin(y), np.cos(z), np.sin(z)
    Rx = np.array([[1,0,0],[0,cx,-sx],[0,sx,cx]]); Ry = np.array([[cy,0,sy],[0,1,0],[-sy,0,cy]]); Rz = np.array([[cz,-sz,0],[sz,cz,0],[0,0,1]])
    return Rz @ Ry @ Rx
def load(path):
    t = parse(path); ob = [n for n in t if n[0] == 'Objects'][0]; cn = [n for n in t if n[0] == 'Connections'][0]
    O = {}
    for n in ob[2]:
        props = {}
        for k in n[2]:
            if k[0] == 'Properties70':
                for p in k[2]: props[p[1][0].decode()] = p[1][4:]
        O[n[1][0]] = dict(kind=n[0], name=n[1][1].split(b'\x00')[0].decode(errors='ignore'), sub=n[1][2] if len(n[1]) > 2 else b'', props=props, node=n)
    par = {}; kids = {}; op = []
    for c in cn[2]:
        typ, a, b_ = c[1][0], c[1][1], c[1][2]
        if typ == b'OO': par.setdefault(a, []).append(b_); kids.setdefault(b_, []).append(a)
        else: op.append((a, b_, c[1][3] if len(c[1]) > 3 else b''))
    def local(i):
        p = O[i]['props']; M = np.eye(4)
        T = p.get('Lcl Translation', [0,0,0]); R = p.get('Lcl Rotation', [0,0,0]); S = p.get('Lcl Scaling', [1,1,1]); Pre = p.get('PreRotation', [0,0,0])
        M[:3,:3] = eul(Pre) @ eul(R) @ np.diag(S); M[:3,3] = T; return M
    def world(i):
        M = local(i)
        for pp in par.get(i, []):
            if pp in O and O[pp]['kind'] == 'Model': return world(pp) @ M
        return M
    meshes = []
    for i, o in O.items():
        if o['kind'] != 'Model' or o['sub'] != b'Mesh': continue
        geo = [k for k in kids.get(i, []) if k in O and O[k]['kind'] == 'Geometry']
        mat = [k for k in kids.get(i, []) if k in O and O[k]['kind'] == 'Material']
        tex = None
        if mat:
            for a, b_, prop in op:
                if b_ == mat[0] and a in O and O[a]['kind'] == 'Texture' and b'Diffuse' in prop:
                    for k in O[a]['node'][2]:
                        if k[0] in ('RelativeFilename', 'FileName'): tex = os.path.basename(k[1][0].decode(errors='ignore').replace('\\', '/'))
        g = O[geo[0]]['node']; K = {k[0]: k for k in g[2]}
        V = K['Vertices'][1][0].reshape(-1, 3); PV = K['PolygonVertexIndex'][1][0].astype(int)
        uvl = {k[0]: k for k in K['LayerElementUV'][2]}; UVs = uvl['UV'][1][0].reshape(-1, 2)
        UVI = uvl['UVIndex'][1][0].astype(int) if 'UVIndex' in uvl else np.arange(len(PV))
        tris = []; poly = []
        for c, idx in enumerate(PV):
            last = idx < 0; poly.append((~idx if last else idx, c))
            if last:
                for k in range(1, len(poly) - 1): tris.append((poly[0], poly[k], poly[k+1]))
                poly = []
        pairs = np.array([(vi, UVI[c]) for tr in tris for vi, c in tr])
        u, inv = np.unique(pairs, axis=0, return_inverse=True)
        W = world(i); P = V[u[:,0]] @ W[:3,:3].T + W[:3,3]
        UV = UVs[u[:,1]].astype(np.float32).copy(); UV[:,1] = 1 - UV[:,1]
        meshes.append(dict(name=o['name'], P=P, UV=UV, I=inv.reshape(-1).astype(np.uint32), tex=tex))
    return meshes
if __name__ == '__main__':
    for m in load(sys.argv[1]):
        print(m['name'], len(m['P']), len(m['I'])//3, m['tex'], m['P'].min(0).round(1), m['P'].max(0).round(1))
