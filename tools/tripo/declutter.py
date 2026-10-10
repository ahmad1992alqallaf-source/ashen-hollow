# drop loose bits floating apart from the weapon (Meshy sometimes leaves a stray leaf or slab beside it). Works on space,
# not mesh links (the mesh is in many pieces after decimation): the vertices are dropped into a grid of small cells,
# touching cells join up, and only the biggest connected lump is kept.
import bpy, sys, bmesh
import numpy as np
from collections import deque
src, out, FR, SIDE = sys.argv[-4], sys.argv[-3], float(sys.argv[-2]), float(sys.argv[-1])
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=src)
o = [x for x in bpy.context.scene.objects if x.type == 'MESH'][0]
co = np.array([v.co[:] for v in o.data.vertices]); mn = co.min(0); H = (co.max(0) - mn).max(); cell = FR * H
key = np.floor((co - mn) / cell).astype(int); cells = {}
for i, k in enumerate(map(tuple, key)): cells.setdefault(k, []).append(i)
comp = {}; sizes = []
for k in cells:
    if k in comp: continue
    cid = len(sizes); q = deque([k]); comp[k] = cid; n = 0
    while q:
        a = q.popleft(); n += len(cells[a])
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                for dz in (-1, 0, 1):
                    b = (a[0] + dx, a[1] + dy, a[2] + dz)
                    if b in cells and b not in comp: comp[b] = cid; q.append(b)
    sizes.append(n)
big = int(np.argmax(sizes))
# a lump off the main body is dropped only when it hangs out to the side (gems and orbs on the shaft's line stay)
ax = np.median(co[[i for k, idx in cells.items() if comp[k] == big for i in idx]][:, :2], axis=0)
mem = {}
for k, idx in cells.items(): mem.setdefault(comp[k], []).extend(idx)
drop = []
for c, idx in mem.items():
    if c == big: continue
    if np.linalg.norm(co[idx][:, :2].mean(0) - ax) > SIDE * H: drop += idx
bm = bmesh.new(); bm.from_mesh(o.data); bm.verts.ensure_lookup_table()
bmesh.ops.delete(bm, geom=[bm.verts[i] for i in drop], context='VERTS'); bm.to_mesh(o.data); bm.free()
print('DECLUTTER dropped', len(drop), 'of', len(co), 'verts;', len(sizes) - 1, 'loose lumps')
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', export_image_format='JPEG', export_jpeg_quality=88)
