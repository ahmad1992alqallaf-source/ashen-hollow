# Quaternius FBX packs to GLB for Resources/AH/Models/QD: run with Blender python: qd_convert.py <fbx dir> <out dir> <name prefix>
import bpy, sys, os, glob, mathutils
src_dir, out_dir, pre = sys.argv[-3], sys.argv[-2], sys.argv[-1]
for f in sorted(glob.glob(os.path.join(src_dir, '*.fbx'))):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    try: bpy.ops.import_scene.fbx(filepath=f)
    except Exception as e: print('FAIL', f, e); continue
    obs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    mn = mathutils.Vector((1e9,)*3); mx = -mn
    tris = 0
    for o in obs:
        for c in o.bound_box:
            v = o.matrix_world @ mathutils.Vector(c); mn = mathutils.Vector(map(min, mn, v)); mx = mathutils.Vector(map(max, mx, v))
        o.data.calc_loop_triangles(); tris += len(o.data.loop_triangles)
    # the FBX materials come in see-through (alpha 0) and mirror-like: make them solid, matte colours
    for m in bpy.data.materials:
        m.blend_method = 'OPAQUE' if hasattr(m, 'blend_method') else None
        if m.use_nodes:
            b = next((x for x in m.node_tree.nodes if x.type == 'BSDF_PRINCIPLED'), None)
            if b:
                for l in list(b.inputs['Alpha'].links): m.node_tree.links.remove(l)
                b.inputs['Alpha'].default_value = 1.0
                b.inputs['Metallic'].default_value = 0.0; b.inputs['Roughness'].default_value = 0.85
                for k in ('Specular IOR Level', 'Specular'):
                    if k in b.inputs: b.inputs[k].default_value = 0.2
    n = pre + os.path.splitext(os.path.basename(f))[0].lower()
    bpy.ops.export_scene.gltf(filepath=os.path.join(out_dir, n + '.glb'), export_format='GLB')
    s = mx - mn
    print('PIECE', n, 'size(x,y,z blender)', tuple(round(x, 2) for x in s), 'min', tuple(round(x,2) for x in mn), 'tris', tris, 'mats', len(bpy.data.materials))
