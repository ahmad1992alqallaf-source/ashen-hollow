import bpy, bmesh, sys, math, mathutils
src, out, prev, zc, xr, zh = sys.argv[-6], sys.argv[-5], sys.argv[-4], float(sys.argv[-3]), float(sys.argv[-2]), float(sys.argv[-1])
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
o = [o for o in bpy.context.scene.objects if o.type == 'MESH'][0]
bm = bmesh.new(); bm.from_mesh(o.data)
H = max(v.co.z for v in bm.verts)
keep = lambda v: v.co.z > zc * H and (abs(v.co.x) < xr or v.co.z > zh * H)
bmesh.ops.delete(bm, geom=[f for f in bm.faces if not all(keep(v) for v in f.verts)], context='FACES')
bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
# drop small loose islands
import collections
seen = set(); islands = []
for f in bm.faces:
    if f.index in seen: continue
    st = [f]; isl = []; seen.add(f.index)
    while st:
        g = st.pop(); isl.append(g)
        for e in g.edges:
            for h in e.link_faces:
                if h.index not in seen: seen.add(h.index); st.append(h)
    islands.append(isl)
bm.faces.ensure_lookup_table()
big = max(len(i) for i in islands)
bmesh.ops.delete(bm, geom=[f for i in islands if len(i) < big * 0.02 for f in i], context='FACES')
bm.to_mesh(o.data); bm.free()
print('tris', len(o.data.polygons))
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', export_jpeg_quality=88)
exec(open('/tmp/claude-0/tripo/render3.py').read())
