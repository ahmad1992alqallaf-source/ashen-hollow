# Ashen Hollow: thin the Tripo outfit meshes for phones (Blender, run headless):
#   python decimate_outfits.py -- <folder of original tqo_*.glb> <out folder> 0.55
# Collapse-decimates each outfit to the given share of its triangles, keeps the skin weights and smooth shading.
# 0.55 (about 30k triangles an outfit) looked the same as the original in game.
import bpy, sys, math, os, glob
args=sys.argv[sys.argv.index('--')+1:]
srcdir,dstdir,ratio=args[0],args[1],float(args[2])
done=set(os.listdir(dstdir))
for src in sorted(glob.glob(srcdir+'/tqo_*.glb')):
    name=os.path.basename(src)
    if name in done: continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=src)
    b=a=0; ok=True
    for o in list(bpy.context.scene.objects):
        if o.type!='MESH': continue
        if o.data.shape_keys: ok=False; break
        b+=sum(len(p.vertices)-2 for p in o.data.polygons)
        m=o.modifiers.new('dec','DECIMATE'); m.decimate_type='COLLAPSE'; m.ratio=ratio; m.use_collapse_triangulate=True
        if o.modifiers.find('dec')>0: o.modifiers.move(o.modifiers.find('dec'),0)
        for x in bpy.context.selected_objects: x.select_set(False)
        bpy.context.view_layer.objects.active=o; o.select_set(True)
        bpy.ops.object.modifier_apply(modifier='dec')
        try: bpy.ops.mesh.customdata_custom_splitnormals_clear()
        except Exception: pass
        try: bpy.ops.object.shade_smooth_by_angle(angle=math.radians(50))
        except Exception: bpy.ops.object.shade_smooth()
        a+=sum(len(p.vertices)-2 for p in o.data.polygons)
    if not ok: print('SKIP',name); continue
    bpy.ops.export_scene.gltf(filepath=os.path.join(dstdir,name), export_format='GLB', export_skins=True, export_animations=False, export_morph=True)
    print('DONE',name,b,a, flush=True)
