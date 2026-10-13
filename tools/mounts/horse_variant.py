import bpy, bmesh, sys, mathutils, math, json
a=sys.argv[sys.argv.index('--')+1:]; src,dst,var=a[0],a[1],a[2]
V=json.load(open('/tmp/claude-0/conv/variants.json'))[var]
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=src)
sc=bpy.context.scene; arm=[o for o in sc.objects if o.type=='ARMATURE'][0]
ren={'1 Ilde':'Idle','2 Ilde':'Idle_2','3 Ilde':'Idle_Headlow','Walk':'Walk','Gallop':'Gallop','Jump':'Jump_toIdle','Jump_Run':'Gallop_Jump','6 Eat':'Eating','4 Attack_1':'Attack_Kick','5 Attack_2':'Attack_Headbutt','Death':'Death','Buck_1':'Idle_HitReact_Left','Run_Stop':'Run_Stop'}
for act in list(bpy.data.actions):
    short=act.name.split('|')[-1]
    if short in ren: act.name=ren[short]; act.use_fake_user=True
    else: bpy.data.actions.remove(act)
# coat and mane
def setimg(matname, path):
    m=bpy.data.materials[matname]
    for n in m.node_tree.nodes:
        if n.type=='TEX_IMAGE' and n.image and 'normal' not in n.image.name.lower():
            n.image=bpy.data.images.load(path); return m
    return m
coat=setimg('Horse','/tmp/claude-0/conv/tex/%s_coat.png'%var); mane=setimg('Hair','/tmp/claude-0/conv/tex/%s_mane.png'%var)
for m in (coat,mane):
    for n in m.node_tree.nodes:
        if n.type=='BSDF_PRINCIPLED':
            n.inputs['Roughness'].default_value=0.85
            if 'Specular IOR Level' in n.inputs: n.inputs['Specular IOR Level'].default_value=0.25
def solid(name, rgb, metal=0.0, rough=0.8, emit=0.0):
    m=bpy.data.materials.new(name); m.use_nodes=True; b=m.node_tree.nodes['Principled BSDF']
    c=[(x/255)**2.2 for x in rgb]+[1]; b.inputs['Base Color'].default_value=c; b.inputs['Metallic'].default_value=metal; b.inputs['Roughness'].default_value=rough
    if emit: b.inputs['Emission Color'].default_value=c; b.inputs['Emission Strength'].default_value=emit
    return m
# extras are built at rest and bound to a bone
arm.data.pose_position='REST'; bpy.context.view_layer.update()
body=[o for o in sc.objects if o.type=='MESH' and o.data.materials and o.data.materials[0].name=='Horse'][0]
W=arm.matrix_world
def bonepos(n, tail=False): b=arm.data.bones[n]; return W@(b.tail_local if tail else b.head_local)
def bind(obj, bone):
    obj.parent=arm; obj.matrix_parent_inverse=arm.matrix_world.inverted()
    vg=obj.vertex_groups.new(name=bone); vg.add(list(range(len(obj.data.vertices))),1.0,'REPLACE')
    md=obj.modifiers.new('Armature','ARMATURE'); md.object=arm
def cone(name, base, tip, r, mat, seg=12):
    bm=bmesh.new(); axis=(tip-base); L=axis.length; q=axis.normalized().to_track_quat('Z','Y')
    bmesh.ops.create_cone(bm, cap_ends=True, segments=seg, radius1=r, radius2=0.0, depth=L)
    for v in bm.verts: v.co=q@(v.co+mathutils.Vector((0,0,L/2)))+base
    me=bpy.data.meshes.new(name); bm.to_mesh(me); bm.free(); o=bpy.data.objects.new(name,me); sc.collection.objects.link(o); me.materials.append(mat); 
    for p in me.polygons: p.use_smooth=True
    return o
head=bonepos('BN_Head_00_016'); nose=bonepos('BN_Head_01_029'); hl=(nose-head).length
up=mathutils.Vector((0,0,1)); fwd=(nose-head).normalized(); side=fwd.cross(up).normalized(); upn=side.cross(fwd).normalized()
if upn.z<0: upn=-upn
ex=V.get('extra')
if ex=='horn':
    base=head+fwd*hl*0.28+upn*hl*0.38
    o=cone('Horn', base, base+(upn*1.0+fwd*0.4).normalized()*hl*0.9, hl*0.1, solid('Gold',(235,190,80),metal=0.9,rough=0.3,emit=0.15)); bind(o,'BN_Head_00_016')
if ex=='horns':
    m=solid('HornBone',(60,52,44),rough=0.6)
    for s in (-1,1):
        base=head+fwd*hl*0.05+upn*hl*0.36+side*s*hl*0.16
        o=cone('Horn%d'%s, base, base+(upn*0.75+side*s*0.55-fwd*0.3).normalized()*hl*0.7, hl*0.13, m); bind(o,'BN_Head_00_016')
if ex=='caparison':
    # a cloth over the back and sides, between the shoulders and the hips, hanging to the belly: a copy of the body's
    # own skin there (same bones and weights), pushed out a little and dyed
    cap=body.copy(); cap.data=body.data.copy(); cap.name='Caparison'; sc.collection.objects.link(cap)
    M=body.matrix_world
    sp0=bonepos('BN_Spine_00_02'); sp3=bonepos('BN_Spine_03_05'); y0,y1=sorted([sp0.y,sp3.y]); span=y1-y0
    zs=[(M@v.co).z for v in body.data.vertices]; zmin,zmax=min(zs),max(zs); belly=zmin+(zmax-zmin)*0.40
    bm=bmesh.new(); bm.from_mesh(cap.data)
    def inside(v): w=M@v.co; return (y0-span*0.15)<w.y<(y1+span*0.1) and w.z>belly
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if not all(inside(v) for v in f.verts)], context='FACES')
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    bm.normal_update(); push=hl*0.03/M.to_scale()[0]
    for v in bm.verts: v.co=v.co+v.normal*push
    bm.to_mesh(cap.data); bm.free()
    cap.data.materials.clear(); cap.data.materials.append(solid('Caparison',(150,22,24),rough=0.9))
    print('CAPARISON faces',len(cap.data.polygons), 'y',y0,y1,'belly',belly)
arm.data.pose_position='POSE'
# real size
top=[o for o in sc.objects if o.parent is None and o.name not in ('Icosphere',)]
arm.animation_data.action=bpy.data.actions['Idle']
try: arm.animation_data.action_slot=bpy.data.actions['Idle'].slots[0]
except: pass
sc.frame_set(1); bpy.context.view_layer.update()
dg=bpy.context.evaluated_depsgraph_get(); zz=[]
for o in sc.objects:
    if o.type=='MESH' and o.name!='Icosphere':
        e=o.evaluated_get(dg); mm=e.to_mesh(); zz+=[(e.matrix_world@v.co).z for v in mm.vertices]; e.to_mesh_clear()
k=float(V.get('height',1.95))/(max(zz)-min(zz))
for o in top: o.scale=o.scale*k
for o in list(sc.objects):
    if o.name=='Icosphere': bpy.data.objects.remove(o)
for im in bpy.data.images:
    if im.size[0]>1024: im.scale(1024,int(1024*im.size[1]/im.size[0]))
arm.animation_data.action=None
bpy.ops.export_scene.gltf(filepath=dst, export_format='GLB', export_skins=True, export_animations=True, export_animation_mode='ACTIONS', export_force_sampling=True, export_morph=False)
print('DONE',var,dst)
