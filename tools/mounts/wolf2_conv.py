# Roo's "Animated Wolf Scene" (CC-BY) -> mount_<var>_hd.glb: textures wired, horse saddle on the back, colour variant, scaled
import bpy, sys, mathutils, math, os
import numpy as np
a=sys.argv[sys.argv.index('--')+1:]; dst,var,target_h=a[0],a[1],float(a[2])
SRC='/tmp/claude-0/newmounts/wolf2/x/'; TEX=SRC+'textures/'
bpy.ops.wm.read_factory_settings(use_empty=True)
# 1) the horse's saddle, at rest, world space
bpy.ops.import_scene.fbx(filepath='/tmp/claude-0/newmounts/horse_fbx/source/Horse.fbx')
sc=bpy.context.scene; harm=[o for o in sc.objects if o.type=='ARMATURE'][0]; harm.data.pose_position='REST'; bpy.context.view_layer.update()
so=bpy.data.objects['Saddle1_Saddle_0']; dg=bpy.context.evaluated_depsgraph_get(); ev=so.evaluated_get(dg)
sme=bpy.data.meshes.new_from_object(ev); sme.transform(so.matrix_world); smat=so.data.materials[0]; smat.use_fake_user=True
for o in list(sc.objects): bpy.data.objects.remove(o)
# 2) the wolf: append its objects and actions from the .blend
blend=SRC+'source/Wolf_With_Baked_Action_Animations_For_Export.blend'
with bpy.data.libraries.load(blend) as (src,dstd):
    dstd.objects=[n for n in src.objects]; dstd.actions=[n for n in src.actions]
for o in dstd.objects:
    if o.type in ('MESH','ARMATURE'): sc.collection.objects.link(o)
arm=[o for o in sc.objects if o.type=='ARMATURE'][0]
saddle=bpy.data.objects.new('Saddle',sme); sc.collection.objects.link(saddle)
# textures
def img(n):
    i=bpy.data.images.load(TEX+n); return i
body_img=img('Wolf_Body.jpeg')
# recolour the coat for the variant
px=np.array(body_img.pixels[:],dtype=np.float32).reshape(-1,4)
Y=(0.3*px[:,0]+0.59*px[:,1]+0.11*px[:,2])[:,None]
if var=='direwolf':      # dark steel grey wolf
    px[:,:3]=np.clip(Y*np.array([0.80,0.84,0.92])*0.85+(px[:,:3]-Y)*0.35,0,1)
elif var=='warg':        # the warg: near black, a faint ash-grey on the belly and muzzle
    px[:,:3]=np.clip(Y*np.array([0.42,0.40,0.40])*0.75+(px[:,:3]-Y)*0.1,0,1)
elif var=='glacierwolf': # pale frost wolf
    px[:,:3]=np.clip(0.30+Y*np.array([0.78,0.86,0.98])*1.05+(px[:,:3]-Y)*0.15,0,1)
body_img.pixels.foreach_set(px.ravel()); body_img.update()
# fur cards: colour = the coat's mean of the back, alpha = the mask (dark strands)
fm=np.array(img('Wolf_Fur.jpeg').pixels[:],dtype=np.float32).reshape(-1,4)
ok=Y[:,0]>0.05; coat=np.median(px[ok,:3],0)*0.85
fur=bpy.data.images.new('Wolf_Fur_RGBA',400,200,alpha=True)
fp=np.zeros_like(fm); fp[:,:3]=coat; fp[:,3]=np.clip((1-(0.3*fm[:,0]+0.59*fm[:,1]+0.11*fm[:,2]))*1.6,0,1)
fur.pixels.foreach_set(fp.ravel()); fur.update()
def mk(mat, image=None, col=None, alpha=False, rough=0.85):
    nt=mat.node_tree; nt.nodes.clear()
    out=nt.nodes.new('ShaderNodeOutputMaterial'); b=nt.nodes.new('ShaderNodeBsdfPrincipled'); nt.links.new(b.outputs[0],out.inputs[0])
    b.inputs['Roughness'].default_value=rough
    if image:
        t=nt.nodes.new('ShaderNodeTexImage'); t.image=image; nt.links.new(t.outputs['Color'],b.inputs['Base Color'])
        if alpha: nt.links.new(t.outputs['Alpha'],b.inputs['Alpha'])
    if col: b.inputs['Base Color'].default_value=col
    mat.blend_method='CLIP' if alpha else 'OPAQUE'
    try: mat.alpha_threshold=0.5
    except: pass
for m in bpy.data.materials:
    if m.name=='Wolf_Body': mk(m,body_img)
    elif m.name=='Wolf_Fur': mk(m,fur,alpha=True)
    elif m.name=='Wolf_Eyes':
        if var=='warg':
            # ember-red eyes that glow a little in the dark
            ei=img('Wolf_Eyes_1.jpeg'); ep=np.array(ei.pixels[:],dtype=np.float32).reshape(-1,4); EY=(0.3*ep[:,0]+0.59*ep[:,1]+0.11*ep[:,2])
            ep[:,0]=np.clip(EY*1.6+0.15,0,1); ep[:,1]=EY*0.25; ep[:,2]=EY*0.12; ei.pixels.foreach_set(ep.ravel()); ei.update()
            mk(m,ei,rough=0.2); nt=m.node_tree; b=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED'); t=next(n for n in nt.nodes if n.type=='TEX_IMAGE')
            nt.links.new(t.outputs['Color'],b.inputs['Emission Color']); b.inputs['Emission Strength'].default_value=1.5
        else: mk(m,img('Wolf_Eyes_1.jpeg'),rough=0.2)
    elif m.name=='Wolf_Claws': mk(m,col=(0.03,0.03,0.03,1),rough=0.5)
    elif m.name=='Wolf_Teeth': mk(m,col=(0.75,0.72,0.62,1),rough=0.4)
# clips
ren={'Wolf_Walk_cycle_':'Walk','Wolf_Run_Cycle_':'Gallop','Wolf_Idle_':'Idle','Wolf_seat_':'Sit','Wolf_creep_cycle':'Creep'}
for act in list(bpy.data.actions):
    if act.name in ren: act.name=ren[act.name]; act.use_fake_user=True
    elif act.library is None and act.name not in ren.values(): bpy.data.actions.remove(act)
print('KEPT',[x.name for x in bpy.data.actions])
# 3) the saddle onto the back
arm.data.pose_position='REST'; bpy.context.view_layer.update()
W=arm.matrix_world
def bp(n): return W@arm.data.bones[n].head_local
body=bpy.data.objects['Wolf_obj_body.004']; M=body.matrix_world
hind=(bp('Oberschenkel_L')+bp('Oberschenkel_R'))/2; fore=(bp('Oberarm_L')+bp('Oberarm_R'))/2
fwd=(fore-hind); fwd.z=0; L=fwd.length; fwd.normalize()
mid=hind+(fore-hind)*float(a[4] if len(a)>4 else 0.45)
pts=[M@v.co for v in body.data.vertices]
near=[p for p in pts if abs((p-mid).dot(fwd))<0.15*L and abs((p-mid).cross(fwd).z)<0.3*L]
nz=sorted(p.z for p in near); topz=nz[-1]
width=max(abs((p-mid).cross(fwd).z) for p in pts if abs((p-mid).dot(fwd))<0.15*L)*2
print('WOLF L',round(L,3),'mid',tuple(round(x,3) for x in mid),'top',round(topz,3),'width',round(width,3))
sv=[v.co for v in sme.vertices]; smn=mathutils.Vector([min(p[i] for p in sv) for i in range(3)]); smx=mathutils.Vector([max(p[i] for p in sv) for i in range(3)])
width=max(width,0.21); sc_=width*1.3/(smx.x-smn.x)
R=mathutils.Matrix.Rotation(-math.atan2(fwd.x,-fwd.y),4,'Z')
cen=mathutils.Vector(((smn.x+smx.x)/2,(smn.y+smx.y)/2,smx.z))
sme.transform(mathutils.Matrix.Translation(mathutils.Vector((mid.x,mid.y,topz)))@R@mathutils.Matrix.Scale(sc_,4)@mathutils.Matrix.Translation(-cen))
zs=sorted(v.co.z for v in sme.vertices); under=zs[int(len(zs)*0.35)]
SINK=float(a[3]) if len(a)>3 else 0.0
sme.transform(mathutils.Matrix.Translation(mathutils.Vector((0,0,topz-under-SINK*(smx.z-smn.z)*sc_))))
saddle.data.materials.clear(); saddle.data.materials.append(smat)
spines=['Becken','Bauch','Bauch.001','Bauch.003','Brust']
best=min([n for n in spines if n in arm.data.bones],key=lambda n:(bp(n)-mid).length)
saddle.parent=arm; saddle.matrix_parent_inverse=arm.matrix_world.inverted()
vg=saddle.vertex_groups.new(name=best); vg.add(list(range(len(sme.vertices))),1.0,'REPLACE')
md=saddle.modifiers.new('Armature','ARMATURE'); md.object=arm
print('SADDLE scale',round(sc_,3),'bone',best)
# 3b) the warg's war gear: a spiked iron collar round the neck and iron plates over the shoulders
if var=='warg':
    import bmesh
    iron=bpy.data.materials.new('WargIron'); iron.use_nodes=True; ib=iron.node_tree.nodes.get('Principled BSDF')
    ib.inputs['Base Color'].default_value=(0.09,0.085,0.08,1); ib.inputs['Metallic'].default_value=0.85; ib.inputs['Roughness'].default_value=0.45
    def gear(name, bone, build):
        me=bpy.data.meshes.new(name); bm=bmesh.new(); build(bm); bm.to_mesh(me); bm.free()
        o=bpy.data.objects.new(name,me); sc.collection.objects.link(o); me.materials.append(iron)
        for f in me.polygons: f.use_smooth=True
        o.parent=arm; o.matrix_parent_inverse=arm.matrix_world.inverted()
        vg=o.vertex_groups.new(name=bone); vg.add(list(range(len(me.vertices))),1.0,'REPLACE')
        md=o.modifiers.new('Armature','ARMATURE'); md.object=arm
        return o
    # the neck: where the collar sits, and how thick the neck is there
    nk=bp('Hals'); hd=bp('Kopf'); axis=(hd-nk).normalized(); cen=nk+(hd-nk)*0.35
    side=mathutils.Vector((1,0,0)); up=axis.cross(side).normalized()
    near=[p for p in pts if abs((p-cen).dot(axis))<0.025]
    rad=max(((p-cen)-axis*(p-cen).dot(axis)).length for p in near)*1.02 if near else 0.08
    def collar(bm):
        N=20; ring=[]
        for i in range(N):
            a=2*math.pi*i/N; d=side*math.cos(a)+up*math.sin(a)
            for w in (-0.018,0.018):
                for rr in (rad, rad*1.08): ring.append(bm.verts.new(cen+d*rr+axis*w))
        bm.verts.ensure_lookup_table()
        for i in range(N):
            j=(i+1)%N; a=[ring[i*4+k] for k in range(4)]; b=[ring[j*4+k] for k in range(4)]
            bm.faces.new((a[1],b[1],b[3],a[3])); bm.faces.new((a[0],a[2],b[2],b[0])); bm.faces.new((a[0],b[0],b[1],a[1])); bm.faces.new((a[2],a[3],b[3],b[2]))
            # a spike on every other plate, on the top and sides only
            ang=2*math.pi*(i+0.5)/N; dd=side*math.cos(ang)+up*math.sin(ang)
            if i%2==0 and dd.dot(up)>-0.3:
                base=cen+dd*rad*1.08; tip=base+dd*rad*0.55; k=6; bv=[]
                t1=dd.cross(axis).normalized(); t2=axis
                for q in range(k):
                    aq=2*math.pi*q/k; bv.append(bm.verts.new(base+(t1*math.cos(aq)+t2*math.sin(aq))*rad*0.12))
                tv=bm.verts.new(tip)
                for q in range(k): bm.faces.new((bv[q],bv[(q+1)%k],tv))
    gear('WargCollar','Hals',collar)
    # (shoulder plates were tried: they read as two helmets on its back, so they are left off)
    for sd in ():
        sb=bp('Schalterplatte_'+sd); ua=bp('Oberarm_'+sd)
        out=mathutils.Vector((1 if sb.x>0 else -1,0,0))
        def plate(bm, sb=sb, ua=ua, out=out):
            c=sb+(ua-sb)*0.3+out*0.035+mathutils.Vector((0,0,0.02)); R=0.075; rows=[]
            for i in range(5):
                th=math.radians(-60+30*i); row=[]
                for j in range(5):
                    ph=math.radians(-50+25*j)
                    d=mathutils.Vector((0,math.sin(ph),0))+out*math.cos(ph)*math.cos(th)+mathutils.Vector((0,0,math.cos(ph)*math.sin(th)))
                    row.append(bm.verts.new(c+d*R))
                rows.append(row)
            for i in range(4):
                for j in range(4): bm.faces.new((rows[i][j],rows[i][j+1],rows[i+1][j+1],rows[i+1][j]))
        gear('WargPlate'+sd,'Schalterplatte_'+sd,plate)
    print('WARG GEAR collar r',round(rad,3))

# 4) scale to the game size
arm.data.pose_position='POSE'
if arm.animation_data is None: arm.animation_data_create()
arm.animation_data.action=bpy.data.actions['Walk']
try: arm.animation_data.action_slot=bpy.data.actions['Walk'].slots[0]
except: pass
sc.frame_set(0); bpy.context.view_layer.update()
k=target_h/(max(p.z for p in pts)-min(p.z for p in pts))
holder=bpy.data.objects.new('WolfRoot',None); sc.collection.objects.link(holder)
for o in sc.objects:
    if o.parent is None and o is not holder: o.parent=holder
holder.scale=(k,k,k)
for im in bpy.data.images:
    if im.size[0]>1024: im.scale(1024,int(1024*im.size[1]/im.size[0]))
    try:
        if im.size[0]>0 and im.users>0: im.pack()
    except Exception as e: print('nopack',im.name)
arm.animation_data.action=None
bpy.ops.export_scene.gltf(filepath=dst, export_format='GLB', export_apply=True, export_skins=True, export_animations=True, export_animation_mode='ACTIONS', export_force_sampling=True, export_morph=False, export_image_format='JPEG')
print('DONE',var,'scale',round(k,3))
