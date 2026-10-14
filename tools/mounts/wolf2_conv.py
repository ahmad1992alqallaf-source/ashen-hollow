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
elif var=='warg':
    # the warg: a charcoal back fading to ash grey on the belly and legs, dark stripes down its flanks and a few
    # pale old scars over the shoulder. The coat texture is laid out like a side view of the wolf (head at the left,
    # back along the top), so the stripes and scars are painted straight onto it. The mouth (red) is left as it is.
    Hh,Ww=body_img.size[1],body_img.size[0]
    im=px.reshape(Hh,Ww,4)[::-1].copy()          # top row first
    rgb=im[...,:3]; L=0.3*rgb[...,0]+0.59*rgb[...,1]+0.11*rgb[...,2]
    red=(rgb[...,0]>rgb[...,1]*1.3)&(rgb[...,0]>0.15)
    t=np.clip((L-0.12)/0.5,0,1)[...,None]
    coat=np.array([0.24,0.23,0.235])*(1-t)+np.array([0.44,0.425,0.41])*t
    vv,uu=np.mgrid[0:Hh,0:Ww]; u=uu/Ww; v=vv/Hh
    # stripes: wavy, a little slanted, deepest on the back, gone by the belly
    ph=(u*16.0+np.sin(v*9.0+u*7.0)*0.35+v*1.2)%1.0
    stripe=np.clip(1-np.abs(ph-0.5)/0.13,0,1)**1.5*np.clip(1-(v-0.05)/0.36,0,1)*((u>0.2)&(u<0.74))
    coat=coat*(1-0.78*stripe[...,None])
    # scars: three pale slashes over the left shoulder area of the layout
    rng=np.random.default_rng(5)
    for k in range(4):
        u0,v0=0.24+0.05*k+rng.uniform(-0.01,0.01),0.12+0.04*k; ang=rng.uniform(0.9,1.2); ln=rng.uniform(0.07,0.11)
        du,dv=np.cos(ang)*ln,np.sin(ang)*ln*2.0
        tt=np.clip(((u-u0)*du+(v-v0)*dv)/(du*du+dv*dv),0,1); d=np.sqrt(((u0+tt*du-u)*Ww)**2+((v0+tt*dv-v)*Hh)**2)
        sc_=np.clip(1-d/5.0,0,1)*np.sin(tt*np.pi)
        coat=coat*(1-sc_[...,None])+np.array([0.58,0.47,0.44])*sc_[...,None]
    # keep the fine fur detail of the original on top of the new colours
    detail=np.clip((L/np.maximum(0.05,np.mean(L[L>0.05])))[...,None],0.6,1.5)
    newrgb=np.clip(coat*detail,0,1)
    rgb[~red]=newrgb[~red]; im[...,:3]=rgb
    px=im[::-1].reshape(-1,4)
elif var=='glacierwolf': # pale frost wolf
    px[:,:3]=np.clip(0.30+Y*np.array([0.78,0.86,0.98])*1.05+(px[:,:3]-Y)*0.15,0,1)
body_img.pixels.foreach_set(px.ravel()); body_img.update()
# fur cards: colour = the coat's mean of the back, alpha = the mask (dark strands)
fm=np.array(img('Wolf_Fur.jpeg').pixels[:],dtype=np.float32).reshape(-1,4)
ok=Y[:,0]>0.05; coat=np.median(px[ok,:3],0)*0.85
if var=='warg': coat=np.array([0.07,0.066,0.07],np.float32)
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
# 2b) the warg's build: a bigger head, a heavier, hunched neck and shoulders (the mesh moved in its rest pose, each
# point by how much the head or shoulder bones hold it, so it still bends with them)
arm.data.pose_position='REST'; bpy.context.view_layer.update()
if var=='warg':
    AW=arm.matrix_world
    def bpos(n): return AW@arm.data.bones[n].head_local
    HEAD={'Kopf','Kopf.002','Unterkiefer','Maulunten','Mauloben','Ohr_L','Ohr_R','Braun_O_L','Braun_O_R','aug_lied_O_L','aug_lied_O_R','aug_lied_U_L','aug_lied_U_R','MundW_L','MundW_R'}
    BULK={'Brust':1.0,'Hals':1.0,'Hals_fett':1.0,'Schalterplatte_L':0.8,'Schalterplatte_R':0.8,'Bauch.003':0.5}
    hc=bpos('Kopf'); neck=bpos('Hals')
    for o in [o for o in sc.objects if o.type=='MESH' and o.name.startswith('Wolf_obj_body')]:
        gi={g.index:g.name for g in o.vertex_groups}; Mo=o.matrix_world; Mi=Mo.inverted()
        for vtx in o.data.vertices:
            wh=sum(g.weight for g in vtx.groups if gi.get(g.group) in HEAD)
            wb=sum(g.weight*BULK[gi[g.group]] for g in vtx.groups if gi.get(g.group) in BULK)
            pw=Mo@vtx.co
            pw=pw+(pw-hc)*0.3*min(1.0,wh)
            pw=pw+mathutils.Vector((pw.x*0.14,0,0.022))*min(1.0,wb)
            vtx.co=Mi@pw
    print('WARG BUILD done')
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
    bonew=bpy.data.materials.new('WargBone'); bonew.use_nodes=True; bb=bonew.node_tree.nodes.get('Principled BSDF')
    bb.inputs['Base Color'].default_value=(0.42,0.36,0.28,1); bb.inputs['Roughness'].default_value=0.55
    def cone(bm,base,tip,r,k=8):
        ax=(tip-base).normalized(); t1=ax.cross(mathutils.Vector((0,0,1)) if abs(ax.z)<0.9 else mathutils.Vector((1,0,0))).normalized(); t2=ax.cross(t1)
        ring=[bm.verts.new(base+(t1*math.cos(2*math.pi*q/k)+t2*math.sin(2*math.pi*q/k))*r) for q in range(k)]; tv=bm.verts.new(tip)
        for q in range(k): bm.faces.new((ring[q],ring[(q+1)%k],tv))
        bm.faces.new(list(reversed(ring)))
    def curved_horn(bm,base,dirs,r0):
        # a horn that sweeps back and up in a few segments, thinning to a point
        k=8; prev=None; pos=base.copy(); rings=[]
        for i,dv in enumerate(dirs):
            r=r0*(1-i/len(dirs))
            ax=dv.normalized(); t1=ax.cross(mathutils.Vector((1,0,0))).normalized(); t2=ax.cross(t1)
            rings.append([bm.verts.new(pos+(t1*math.cos(2*math.pi*q/k)+t2*math.sin(2*math.pi*q/k))*r) for q in range(k)])
            pos=pos+dv
        tv=bm.verts.new(pos)
        for i in range(len(rings)-1):
            for q in range(k): bm.faces.new((rings[i][q],rings[i][(q+1)%k],rings[i+1][(q+1)%k],rings[i+1][q]))
        for q in range(k): bm.faces.new((rings[-1][q],rings[-1][(q+1)%k],tv))
    def gear2(name,bone,build,mat):
        o=gear(name,bone,build); o.data.materials.clear(); o.data.materials.append(mat); return o
    # the bone war-mask: a copy of the skin over the brow and the bridge of the nose, lifted off it, in bone
    bd=bpy.data.objects['Wolf_obj_body.004']; gi={g.index:g.name for g in bd.vertex_groups}; Mb=bd.matrix_world
    hc=bp('Kopf'); snout=bp('Mauloben')
    def mask(bm):
        import bmesh as _b
        src=_b.new(); src.from_mesh(bd.data); src.verts.ensure_lookup_table()
        def ok(vx):
            p=Mb@vx.co; n=(Mb.to_3x3()@vx.normal).normalized()
            wk=sum(g.weight for g in bd.data.vertices[vx.index].groups if gi.get(g.group) in ('Kopf','Kopf.002'))
            return wk>0.5 and n.z>0.35 and p.y<hc.y+0.02 and p.y>snout.y-0.01 and abs(p.x)<0.06
        keep=[]   # (a bone plate copied from the skin looked like a sticker: horns only)
        vm={}
        for f in keep:
            vs=[]
            for vx in f.verts:
                if vx.index not in vm:
                    p=Mb@vx.co; n=(Mb.to_3x3()@vx.normal).normalized(); vm[vx.index]=bm.verts.new(p+n*0.006)
                vs.append(vm[vx.index])
            try: bm.faces.new(vs)
            except ValueError: pass
        src.free()
        # two horns from the back of the mask, sweeping back and up
        for sx in (-1,1):
            base=hc+mathutils.Vector((sx*0.036,-0.02,0.045))
            dirs=[mathutils.Vector((sx*0.012,0.006,0.04)),mathutils.Vector((sx*0.016,0.03,0.03)),mathutils.Vector((sx*0.012,0.04,0.006)),mathutils.Vector((sx*0.004,0.034,-0.014)),mathutils.Vector((-sx*0.002,0.02,-0.02))]
            curved_horn(bm,base,dirs,0.02)
    gear2('WargMask','Kopf',mask,bonew)
    # two long fangs hanging over the lower lip
    def fangs(bm):
        tv=[bpy.data.objects['Wolf_obj_body.001'].matrix_world@v.co for v in bpy.data.objects['Wolf_obj_body.001'].data.vertices]
        zc=sum(p.z for p in tv)/len(tv)
        for sx in (-1,1):
            up=[p for p in tv if p.z>zc and p.x*sx>0.004]
            if not up: continue
            front=min(up,key=lambda p:p.y); base=front+mathutils.Vector((0,0.002,0.004))
            cone(bm,base,base+mathutils.Vector((0,-0.006,-0.045)),0.007)
    gear2('WargFangs','Mauloben',fangs,bonew)
    # a ridge of iron spikes along the top of the neck, behind the collar
    def ridge(bm):
        for i,f in enumerate((0.2,0.45,0.7)):
            p=bp('Hals')+(bp('Brust')-bp('Hals'))*f
            top=max((Mb@v.co for v in bd.data.vertices if abs((Mb@v.co).y-p.y)<0.012 and abs((Mb@v.co).x)<0.02),key=lambda q:q.z,default=p)
            cone(bm,top-mathutils.Vector((0,0,0.004)),top+mathutils.Vector((0,0.012,0.04-0.008*i)),0.009)
    gear2('WargRidge','Brust',ridge,iron)
    # a red war blanket under the saddle, laid over the back and down the flanks (cast onto the skin)
    from mathutils.bvhtree import BVHTree
    dg2=bpy.context.evaluated_depsgraph_get(); bev=bd.evaluated_get(dg2); tree=BVHTree.FromObject(bev,dg2)
    cloth=bpy.data.materials.new('WargBlanket'); cloth.use_nodes=True; cb=cloth.node_tree.nodes.get('Principled BSDF')
    cb.inputs['Base Color'].default_value=(0.32,0.03,0.03,1); cb.inputs['Roughness'].default_value=0.85
    trim=bpy.data.materials.new('WargTrim'); trim.use_nodes=True; tb=trim.node_tree.nodes.get('Principled BSDF')
    tb.inputs['Base Color'].default_value=(0.55,0.4,0.12,1); tb.inputs['Metallic'].default_value=0.6; tb.inputs['Roughness'].default_value=0.4
    def blanket_mesh():
        me=bpy.data.meshes.new('WargBlanket'); import bmesh as _b; bm=_b.new(); NU,NV=10,12; grid=[]
        span=0.34*L
        for i in range(NU+1):
            yy=mid.y+(-span/2+span*i/NU); row=[]
            for j in range(NV+1):
                th=math.radians(-95+190*j/NV); dirv=mathutils.Vector((math.sin(th),0,math.cos(th)))
                c0=mathutils.Vector((mid.x,yy,mid.z-0.02)); hit=tree.ray_cast(c0+dirv*0.5,-dirv)
                p=(hit[0]+dirv*0.006) if hit[0] is not None else c0+dirv*0.1
                row.append(bm.verts.new(Bi@p))
            grid.append(row)
        for i in range(NU):
            for j in range(NV):
                f=bm.faces.new((grid[i][j],grid[i+1][j],grid[i+1][j+1],grid[i][j+1])); f.material_index=1 if (i==0 or i==NU-1 or j==0 or j==NV-1) else 0
        bm.to_mesh(me); bm.free(); return me
    Bi=bd.matrix_world.inverted()
    bme=blanket_mesh(); bo=bpy.data.objects.new('WargBlanket',bme); sc.collection.objects.link(bo); bo.matrix_world=bd.matrix_world.copy()
    bme.materials.append(cloth); bme.materials.append(trim)
    for f in bme.polygons: f.use_smooth=True
    bo.parent=arm; bo.matrix_parent_inverse=arm.matrix_world.inverted()@bd.matrix_world@bd.matrix_world.inverted()
    bo.matrix_world=bd.matrix_world.copy()
    # the blanket bends with the back: each point held by the nearest spine bones
    spn=['Becken','Bauch','Bauch.001','Bauch.003','Brust']
    for n in spn: bo.vertex_groups.new(name=n)
    for v in bme.vertices:
        p=bo.matrix_world@v.co; ds=sorted(((abs(p.y-bp(n).y),n) for n in spn))[:2]
        w0=ds[1][0]/(ds[0][0]+ds[1][0]+1e-6); bo.vertex_groups[ds[0][1]].add([v.index],w0,'REPLACE'); bo.vertex_groups[ds[1][1]].add([v.index],1-w0,'REPLACE')
    md=bo.modifiers.new('Armature','ARMATURE'); md.object=arm
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
