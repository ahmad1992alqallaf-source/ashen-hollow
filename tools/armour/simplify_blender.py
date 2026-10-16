# Blender: simplify the meshes of a skinned glb and save their new geometry (glTF axes) to an .npz per mesh.
# The skeleton is not exported from here: armpack.py writes the new geometry back into the original file.
import bpy, bmesh, sys, math, numpy as np
src, outp, ratio, angle = sys.argv[-4], sys.argv[-3], float(sys.argv[-2]), float(sys.argv[-1])
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
res = {}
for o in [o for o in bpy.context.scene.objects if o.type == 'MESH']:
    me = o.data; n0 = len(me.polygons)
    # 1) weld the copies the hard edges made (UV seams stay: UVs live on the corners)
    bm = bmesh.new(); bm.from_mesh(me)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bm.to_mesh(me); bm.free()
    # 2) simplify, keeping UV seams and the skin weights (vertex groups are carried over by the decimator)
    bpy.context.view_layer.objects.active = o; o.select_set(True)
    for m in list(o.modifiers): o.modifiers.remove(m)
    d = o.modifiers.new('dec', 'DECIMATE'); d.decimate_type = 'COLLAPSE'; d.ratio = ratio; d.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier='dec')
    # 3) shading: smooth, with hard edges kept where faces meet at more than `angle` degrees
    for p in me.polygons: p.use_smooth = True
    try: me.set_sharp_from_angle(angle=math.radians(angle))
    except Exception as e: print('sharp', e)
    me.update()
    bm = bmesh.new(); bm.from_mesh(me); bmesh.ops.triangulate(bm, faces=bm.faces); bm.to_mesh(me); bm.free(); me.update()
    names = [g.name for g in o.vertex_groups]
    uvl = me.uv_layers.active.data if me.uv_layers.active else None
    cn = me.corner_normals
    key = {}; P = []; N = []; U = []; J = []; W = []; I = []
    for poly in me.polygons:
        for li in poly.loop_indices:
            vi = me.loops[li].vertex_index; nv = cn[li].vector; uv = uvl[li].uv if uvl else (0, 0)
            k = (vi, round(nv.x, 3), round(nv.y, 3), round(nv.z, 3), round(uv[0], 5), round(uv[1], 5))
            idx = key.get(k)
            if idx is None:
                idx = len(P); key[k] = idx
                v = me.vertices[vi]
                P.append((v.co.x, v.co.z, -v.co.y)); N.append((nv.x, nv.z, -nv.y)); U.append((uv[0], 1.0 - uv[1]))
                gs = sorted([(g.weight, names[g.group]) for g in v.groups if g.weight > 0], reverse=True)[:4]
                s = sum(w for w, _ in gs) or 1.0
                J.append([nm for _, nm in gs] + [''] * (4 - len(gs))); W.append([w / s for w, _ in gs] + [0.0] * (4 - len(gs)))
            I.append(idx)
    res[me.name] = dict(P=np.array(P, 'f4'), N=np.array(N, 'f4'), U=np.array(U, 'f4'), J=np.array(J), W=np.array(W, 'f4'), I=np.array(I, 'u4'))
    print('mesh', me.name, 'tris', n0, '->', len(I) // 3, 'verts', len(P))
np.savez(outp, **{nm + '|' + k: v for nm, d in res.items() for k, v in d.items()})
