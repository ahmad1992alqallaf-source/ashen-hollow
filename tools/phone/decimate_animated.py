# Ashen Hollow: thin animated models (beasts, mounts, pets) for phones, keeping every animation as it was
import bpy, sys, math, os, glob, json, struct
args=sys.argv[sys.argv.index('--')+1:]
srcdir,dstdir=args[0],args[1]
def anims(f):
    b=open(f,'rb').read(); n=struct.unpack('<I',b[12:16])[0]; j=json.loads(b[20:20+n])
    return sorted(a.get('name','') for a in j.get('animations',[])), sum(len(a['channels']) for a in j.get('animations',[]))
for src in sorted(glob.glob(srcdir+'/*.glb')):
    name=os.path.basename(src); dst=os.path.join(dstdir,name)
    if os.path.exists(dst): continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=src)
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    tot=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
    if tot<9000 or any(o.data.shape_keys for o in meshes): print('SKIP',name,tot,flush=True); continue
    ratio=max(0.6, 8000.0/tot); a=0
    for o in meshes:
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
    bpy.ops.export_scene.gltf(filepath=dst, export_format='GLB', export_skins=True, export_animations=True, export_animation_mode='ACTIONS', export_force_sampling=False, export_morph=True)
    a0,c0=anims(src); a1,c1=anims(dst)
    if a0!=a1 or c0!=c1: print('ANIMDIFF',name,len(a0),len(a1),c0,c1,flush=True); os.rename(dst,dst+'.bad'); continue
    print('DONE',name,tot,a,flush=True)
