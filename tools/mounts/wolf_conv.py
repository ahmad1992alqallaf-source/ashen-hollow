import bpy, bmesh, sys, mathutils, math
a=sys.argv[sys.argv.index('--')+1:]; dst,var,target_h=a[0],a[1],float(a[2])
bpy.ops.wm.read_factory_settings(use_empty=True)
# 1) the horse's saddle (without the bridle), as it sits at rest, in world space
bpy.ops.import_scene.fbx(filepath='/tmp/claude-0/newmounts/horse_fbx/source/Horse.fbx')
sc=bpy.context.scene; harm=[o for o in sc.objects if o.type=='ARMATURE'][0]; harm.data.pose_position='REST'; bpy.context.view_layer.update()
so=bpy.data.objects['Saddle1_Saddle_0']; dg=bpy.context.evaluated_depsgraph_get(); ev=so.evaluated_get(dg)
sme=bpy.data.meshes.new_from_object(ev); sme.transform(so.matrix_world); smat=so.data.materials[0]
saddle=bpy.data.objects.new('Saddle',sme); 
for o in list(sc.objects): bpy.data.objects.remove(o)
sc.collection.objects.link(saddle)
# 2) the wolf
bpy.ops.import_scene.fbx(filepath='/tmp/claude-0/newmounts/wolf_fbx/source/Magic wolf render.fbx')
exec(open('/tmp/claude-0/mnt/wolftex.py').read())
arm=[o for o in sc.objects if o.type=='ARMATURE'][0]
ren={'Idle':'Idle','Idle.001':'Idle_2','Idle.002':'Idle_2_HeadLow','walk':'Walk','trot':'Trot','run':'Gallop','claw attack':'Attack','damage':'Idle_HitReact_Left','stunned':'Death'}
for act in list(bpy.data.actions):
    short=act.name.split('|')[-1]
    if short in ren: act.name=ren[short]; act.use_fake_user=True
    else: bpy.data.actions.remove(act)
print('KEPT',[x.name for x in bpy.data.actions])
arm.data.pose_position='REST'; bpy.context.view_layer.update()
W=arm.matrix_world
def bp(n): return W@arm.data.bones[n].head_local
body=bpy.data.objects['Body']; M=body.matrix_world
hind=(bp('HindLeg.01.L')+bp('HindLeg.01.R'))/2; fore=(bp('Clavicle.L')+bp('Clavicle.R'))/2; head=bp('Head')
fwd=(fore-hind); fwd.z=0; fwd.normalize()
mid=hind+(fore-hind)*0.6
# the top of the back there
pts=[M@v.co for v in body.data.vertices]
near=[p for p in pts if abs((p-mid).dot(fwd))<0.25 and abs((p-mid).cross(fwd).z)<0.18]
nz=sorted(p.z for p in near); topz=nz[int(len(nz)*0.8)]; width=max(abs((p-mid).cross(fwd).z) for p in pts if abs((p-mid).dot(fwd))<0.25)*2
print('WOLF fwd',tuple(round(x,2) for x in fwd),'mid',tuple(round(x,2) for x in mid),'top',round(topz,2),'width',round(width,2),'head',tuple(round(x,2) for x in head))
# fit the saddle: its forward (the horse faced -y) onto the wolf's, sized to the back, seated on top
sv=[v.co for v in sme.vertices]; smn=mathutils.Vector([min(p[i] for p in sv) for i in range(3)]); smx=mathutils.Vector([max(p[i] for p in sv) for i in range(3)])
sc_=width*1.35/(smx.x-smn.x)
ang=math.atan2(fwd.x,-fwd.y) if False else math.atan2(-fwd.x, -fwd.y)
R=mathutils.Matrix.Rotation(-math.atan2(fwd.x,-fwd.y),4,'Z')
cen=mathutils.Vector(((smn.x+smx.x)/2,(smn.y+smx.y)/2,smx.z))
T=mathutils.Matrix.Translation(mathutils.Vector((mid.x,mid.y,topz+0.02*sc_*10)))@R@mathutils.Matrix.Scale(sc_,4)@mathutils.Matrix.Translation(-cen)
# the saddle's seat top (its highest point) sits a little above the fur, its pommel at the shoulders
sme.transform(T)
zs=[v.co.z for v in sme.vertices]; seat_top=max(zs)
saddle.data.materials.clear(); saddle.data.materials.append(smat)
# the saddle's lowest leather should sink into the fur: put its underside (the seat's base) at the back's top
under=sorted(zs)[int(len(zs)*0.35)]
sme.transform(mathutils.Matrix.Translation(mathutils.Vector((0,0,topz-under-0.26))))
# bind to the spine bone nearest the saddle
spines=[b for b in arm.data.bones if b.name.startswith('Spine')]
best=min(spines,key=lambda b:((W@b.head_local)-mid).length).name
saddle.parent=arm; saddle.matrix_parent_inverse=arm.matrix_world.inverted()
vg=saddle.vertex_groups.new(name=best); vg.add(list(range(len(sme.vertices))),1.0,'REPLACE')
md=saddle.modifiers.new('Armature','ARMATURE'); md.object=arm
print('SADDLE scale',round(sc_,3),'bone',best)
# 3) colours: the glacier wolf keeps its own; the dire wolf goes steel grey
if var=='direwolf':
    import numpy as np
    for im in bpy.data.images:
        if 'AlbedoTransp' in im.name or 'mat_test_BaseColor' in im.name:
            px=np.array(im.pixels[:],dtype=np.float32).reshape(-1,4)
            Y=(0.3*px[:,0]+0.59*px[:,1]+0.11*px[:,2])[:,None]
            tint=np.array([0.62,0.68,0.78],np.float32)
            px[:,:3]=np.clip(Y*tint*1.05+ (px[:,:3]-Y)*0.25,0,1)
            im.pixels.foreach_set(px.ravel()); im.update()
            if im.filepath: im.pack()
arm.data.pose_position='POSE'
arm.animation_data.action=bpy.data.actions['Idle']
try: arm.animation_data.action_slot=bpy.data.actions['Idle'].slots[0]
except: pass
sc.frame_set(1); bpy.context.view_layer.update(); dg=bpy.context.evaluated_depsgraph_get(); zz=[]
for o in sc.objects:
    if o.type=='MESH' and o.name!='Icosphere':
        e=o.evaluated_get(dg); mm=e.to_mesh(); zz+=[(e.matrix_world@v.co).z for v in mm.vertices]; e.to_mesh_clear()
k=target_h/(max(zz)-min(zz))
top=[o for o in sc.objects if o.parent is None and o.name!='Icosphere']
holder=bpy.data.objects.new('WolfRoot',None); sc.collection.objects.link(holder)
for o in top: o.parent=holder
holder.scale=(k,k,k)
for o in list(sc.objects):
    if o.name=='Icosphere': bpy.data.objects.remove(o)
for im in bpy.data.images:
    if im.size[0]>1024: im.scale(1024,int(1024*im.size[1]/im.size[0]))
arm.animation_data.action=None
bpy.ops.export_scene.gltf(filepath=dst, export_format='GLB', export_skins=True, export_animations=True, export_animation_mode='ACTIONS', export_force_sampling=True, export_morph=False)
print('DONE',var,'scale',k)
