# Ashen Hollow: a beast from the Creatures package (Assets/Beasts: <Name>.fbx, Anims/<Name>@<Clip>.fbx, Textures)
# -> mount_<id>_hd.glb: the model, its walk/trot/run/idle clips (renamed to the game's Walk/Trot/Gallop/Idle), its
# texture and normal map, scaled to riding size. The clips walk in place (their root motion is on a separate
# "Motion" bone, which the game ignores). Parts can be left off (the warg's war-mask).
#   blender python: beast_conv.py -- <Beasts folder> <Name> <out.glb> <height m> [hide,parts] [saddle]
# 'saddle' puts the horse's saddle on its back (beasts without tack of their own: the mammoth)
# FUR=<n>,<length m> (environment) adds n shells of fur strands over the body (see fur_shells below)
import bpy, sys, os, mathutils
a = sys.argv[sys.argv.index('--') + 1:]
root, name, dst, height = a[0], a[1], a[2], float(a[3]); hide = a[4].split(',') if len(a) > 4 and a[4] and a[4] != '-' else []; want_saddle = len(a) > 5 and a[5] == 'saddle'
CLIPS = {'Walk': 'Walk', 'Trot': 'Trot', 'Run': 'Gallop', 'Idle': 'Idle'}
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene; sc.render.fps = 30
bpy.ops.import_scene.fbx(filepath=os.path.join(root, 'Models', name + '.fbx'))
arm = [o for o in sc.objects if o.type == 'ARMATURE'][0]
for o in list(sc.objects):
    if o.type == 'MESH' and any(h and o.name.startswith(h) for h in hide): bpy.data.objects.remove(o)
for act in list(bpy.data.actions): bpy.data.actions.remove(act)
# the clips. Each clip file keeps its own copy of the skeleton, posed at the clip's first frame, so its keys can't be
# copied onto the model's bones as they are (they are relative to that other rest pose: limbs twisted and stretched).
# Instead the clip's skeleton is loaded beside the model, every model bone is made to follow its twin in world space,
# and the result is baked onto the model's own bones, frame by frame.
for src, dstn in CLIPS.items():
    f = os.path.join(root, 'Models', 'Anims', name + '@' + src + '.fbx')
    if not os.path.exists(f): continue
    before_o = set(sc.objects); before_a = set(bpy.data.actions)
    bpy.ops.import_scene.fbx(filepath=f)
    twin = [o for o in sc.objects if o not in before_o and o.type == 'ARMATURE'][0]
    clip = [x for x in bpy.data.actions if x not in before_a][0]
    f0, f1 = int(clip.frame_range[0]), int(clip.frame_range[1])
    for pb in arm.pose.bones:
        if pb.name in twin.pose.bones:
            c = pb.constraints.new('COPY_TRANSFORMS'); c.target = twin; c.subtarget = pb.name; c.name = 'clip'
    bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True); bpy.context.view_layer.objects.active = arm
    if arm.animation_data is None: arm.animation_data_create()
    arm.animation_data.action = None
    bpy.ops.object.mode_set(mode='POSE'); bpy.ops.pose.select_all(action='SELECT')
    bpy.ops.nla.bake(frame_start=f0, frame_end=f1, only_selected=False, visual_keying=True, clear_constraints=True, use_current_action=False, bake_types={'POSE'})
    bpy.ops.object.mode_set(mode='OBJECT')
    baked = arm.animation_data.action; baked.name = dstn; baked.use_fake_user = True
    arm.animation_data.action = None
    for pb in arm.pose.bones:
        pb.location = (0, 0, 0); pb.rotation_quaternion = (1, 0, 0, 0); pb.rotation_euler = (0, 0, 0); pb.scale = (1, 1, 1)
    for o in [o for o in sc.objects if o not in before_o]: bpy.data.objects.remove(o)
    bpy.data.actions.remove(clip)
print('CLIPS', [(x.name, tuple(x.frame_range)) for x in bpy.data.actions])
# textures: T_<material name without M_>, its _Normal map if there is one
tex = os.path.join(root, 'Textures')
for m in bpy.data.materials:
    if not m.node_tree: continue
    nt = m.node_tree; b = next((n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    if b is None: continue
    for n in list(nt.nodes):
        if n.type in ('TEX_IMAGE', 'NORMAL_MAP'): nt.nodes.remove(n)
    base = os.path.join(tex, 'T_' + m.name[2:] + '.png') if m.name.startswith('M_') else None
    if base and os.path.exists(base):
        t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(base); nt.links.new(t.outputs['Color'], b.inputs['Base Color'])
    nrm = base[:-4] + '_Normal.png' if base else None
    if nrm and os.path.exists(nrm):
        t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(nrm); t.image.colorspace_settings.name = 'Non-Color'
        nm = nt.nodes.new('ShaderNodeNormalMap'); nt.links.new(t.outputs['Color'], nm.inputs['Color']); nt.links.new(nm.outputs['Normal'], b.inputs['Normal'])
    b.inputs['Metallic'].default_value = 0.0; b.inputs['Roughness'].default_value = 0.75
print('MATS', [m.name for m in bpy.data.materials])
# fur: the body copied a few times, each copy pushed out along the skin a little further, drawn with the coat's own
# colours but cut to fine strands (a noise of dots in the texture's alpha; each shell keeps only the taller strands,
# so they taper). Eyes, nose, mouth, teeth, claws and tusks are left bare (picked out by their colour). The shells'
# alpha cut is set in the exported file (fix_glb below).
FUR = os.environ.get('FUR')
fur_shells = []
if FUR:
    import numpy as np
    n_sh, flen = int(FUR.split(',')[0]), float(FUR.split(',')[1])
    FUR_ROOT = float(FUR.split(',')[2]) if len(FUR.split(',')) > 2 else 0.62   # how dark the innermost strands are
    body = max((o for o in sc.objects if o.type == 'MESH'), key=lambda o: len(o.data.vertices))
    bmat = body.data.materials[0]; base_img = next(n.image for n in bmat.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image.colorspace_settings.name != 'Non-Color')
    W_, H_ = base_img.size; px = np.array(base_img.pixels[:], dtype=np.float32).reshape(H_, W_, 4)
    rgb = px[..., :3]; mx = rgb.max(-1); mn = rgb.min(-1); sat = (mx - mn) / np.maximum(mx, 1e-4)
    r, g_, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    bare = (mx > 0.62) | (mx < 0.03) | ((sat > 0.55) & (r > g_ * 1.25))   # teeth/tusks/claws, holes, eyes/gums/nose
    rng = np.random.default_rng(3)
    strands = np.zeros((H_, W_), np.float32)
    count = int(W_ * H_ * 0.22)
    ys = rng.integers(0, H_, count); xs = rng.integers(0, W_, count); hs = rng.uniform(0.25, 1.0, count).astype(np.float32)
    np.maximum.at(strands, (ys, xs), hs)
    # a strand is two texels long (along v), so it reads as a hair rather than a dot
    strands = np.maximum(strands, np.maximum(np.roll(strands, 1, 0) * 0.94, np.roll(strands, 2, 0) * 0.88))
    strands[bare] = 0.0
    # widen the bare areas a little (no stray strand on the edge of an eye)
    for sh in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        strands[np.roll(bare, sh, (0, 1))] = 0.0
    # the strands' colour: the coat, with the dark gaps between the texture's pieces filled in from their edges (a
    # strand sampled on the edge of a piece must not pick up the black gap: it drew a black outline round the beast)
    valid = mx > 0.09; col = rgb.copy(); vm = valid.astype(np.float32)
    for _ in range(12):
        acc = np.zeros_like(col); wsum = np.zeros_like(vm)
        for sh in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            acc += np.roll(col * vm[..., None], sh, (0, 1)); wsum += np.roll(vm, sh, (0, 1))
        fill = (vm == 0) & (wsum > 0)
        col[fill] = acc[fill] / wsum[fill][:, None]; vm[fill] = 1.0
    gapw = ~valid
    near = gapw.copy()
    for sh in ((1, 0), (-1, 0), (0, 1), (0, -1), (2, 0), (-2, 0), (0, 2), (0, -2)): near |= np.roll(gapw, sh, (0, 1))
    strands[near] = 0.0
    out = np.concatenate([np.clip(col * 1.45 + 0.02, 0, 1), strands[..., None]], -1)   # lighter: each shell darkens it by its own factor (roots dark, tips light)
    fur_img = bpy.data.images.new(name + '_Fur', W_, H_, alpha=True); fur_img.pixels.foreach_set(out.ravel()); fur_img.update()
    for i in range(1, n_sh + 1):
        sh = body.copy(); sh.data = body.data.copy(); sh.name = name + 'FurShell' + str(i); sc.collection.objects.link(sh)
        d = flen * i / n_sh
        for v in sh.data.vertices: v.co = v.co + v.normal * d
        # where the skin is sharply curved (the spiky fringes, ear edges) pushing out blows a face up into a big
        # flat patch, and its strands with it (solid dark blotches): those faces are left out of the shell
        import bmesh
        base_area = [f.area for f in body.data.polygons]
        bm = bmesh.new(); bm.from_mesh(sh.data); bm.faces.ensure_lookup_table()
        kill = [f for f in bm.faces if base_area[f.index] <= 1e-9 or f.calc_area() / base_area[f.index] > 1.5]
        bmesh.ops.delete(bm, geom=kill, context='FACES'); bm.to_mesh(sh.data); bm.free()
        m = bpy.data.materials.new('FurShell' + str(i)); m.use_nodes = True; nt = m.node_tree
        pb = nt.nodes.get('Principled BSDF'); t = nt.nodes.new('ShaderNodeTexImage'); t.image = fur_img
        nt.links.new(t.outputs['Color'], pb.inputs['Base Color']); nt.links.new(t.outputs['Alpha'], pb.inputs['Alpha'])
        pb.inputs['Roughness'].default_value = 0.9
        # the tips catch a little more light than the roots
        pb.inputs['Base Color'].default_value = (1, 1, 1, 1)
        sh.data.materials.clear(); sh.data.materials.append(m)
        fur_shells.append(('FurShell' + str(i), 0.25 + 0.6 * i / n_sh, FUR_ROOT + (1 - FUR_ROOT) * i / n_sh))
    print('FUR', n_sh, 'shells', flen)
    # a fur bump for a coat that has no normal map of its own (the mammoth)
    if not any(n.type == 'NORMAL_MAP' for n in bmat.node_tree.nodes):
        lum = (0.3 * r + 0.59 * g_ + 0.11 * b)
        lum = (lum + np.roll(lum, 1, 0) + np.roll(lum, -1, 0) + np.roll(lum, 1, 1) + np.roll(lum, -1, 1)) / 5.0
        dx = (np.roll(lum, -1, 1) - np.roll(lum, 1, 1)) * 1.6; dy = (np.roll(lum, -1, 0) - np.roll(lum, 1, 0)) * 1.6
        # no bump across the edge of a piece of the texture (the dark gaps between its islands)
        gap = mx < 0.06
        edge = gap | np.roll(gap, 1, 0) | np.roll(gap, -1, 0) | np.roll(gap, 1, 1) | np.roll(gap, -1, 1) | np.roll(gap, 2, 0) | np.roll(gap, -2, 0) | np.roll(gap, 2, 1) | np.roll(gap, -2, 1)
        dx[edge] = 0.0; dy[edge] = 0.0
        dx = np.clip(dx, -0.6, 0.6); dy = np.clip(dy, -0.6, 0.6)
        nz = np.ones_like(lum); ln = np.sqrt(dx * dx + dy * dy + nz)
        nm = np.stack([(-dx / ln) * 0.5 + 0.5, (-dy / ln) * 0.5 + 0.5, (nz / ln) * 0.5 + 0.5, np.ones_like(lum)], -1)
        nimg = bpy.data.images.new(name + '_Normal', W_, H_); nimg.colorspace_settings.name = 'Non-Color'; nimg.pixels.foreach_set(nm.astype(np.float32).ravel()); nimg.update()
        nt = bmat.node_tree; pb = next(n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED')
        t = nt.nodes.new('ShaderNodeTexImage'); t.image = nimg; nmn = nt.nodes.new('ShaderNodeNormalMap'); nmn.inputs['Strength'].default_value = 1.0
        nt.links.new(t.outputs['Color'], nmn.inputs['Color']); nt.links.new(nmn.outputs['Normal'], pb.inputs['Normal'])
        print('NORMAL made')

# the horse's saddle on the back, between the hind legs and the forelegs, bound to the nearest spine bone
if want_saddle:
    import math
    keep = set(bpy.data.objects); kept_acts = set(bpy.data.actions)
    bpy.ops.import_scene.fbx(filepath='/tmp/claude-0/newmounts/horse_fbx/source/Horse.fbx')
    harm = [o for o in sc.objects if o.type == 'ARMATURE' and o not in keep][0]; harm.data.pose_position = 'REST'; bpy.context.view_layer.update()
    so = bpy.data.objects['Saddle1_Saddle_0']; ev = so.evaluated_get(bpy.context.evaluated_depsgraph_get())
    sme = bpy.data.meshes.new_from_object(ev); sme.transform(so.matrix_world); smat = so.data.materials[0]; smat.use_fake_user = True
    for o in list(sc.objects):
        if o not in keep: bpy.data.objects.remove(o)
    for x in list(bpy.data.actions):
        if x not in kept_acts: bpy.data.actions.remove(x)
    arm.data.pose_position = 'REST'; bpy.context.view_layer.update(); W = arm.matrix_world
    def bp(n): return W @ arm.data.bones[n].head_local
    hind = (bp('ThighL') + bp('ThighR')) / 2; fore = (bp('UpperArmL') + bp('UpperArmR')) / 2
    fwd = fore - hind; fwd.z = 0; L = fwd.length; fwd.normalize(); mid = hind + (fore - hind) * 0.5
    body = max((o for o in sc.objects if o.type == 'MESH'), key=lambda o: len(o.data.vertices)); M = body.matrix_world
    pts = [M @ v.co for v in body.data.vertices]
    near = [p for p in pts if abs((p - mid).dot(fwd)) < 0.12 * L and abs((p - mid).cross(fwd).z) < 0.12 * L]
    topz = max(p.z for p in near)
    width = max(abs((p - mid).cross(fwd).z) for p in pts if abs((p - mid).dot(fwd)) < 0.12 * L) * 2
    sv = [v.co for v in sme.vertices]; smn = mathutils.Vector([min(q[i] for q in sv) for i in range(3)]); smx = mathutils.Vector([max(q[i] for q in sv) for i in range(3)])
    k_s = width * 0.9 / (smx.x - smn.x)
    R = mathutils.Matrix.Rotation(-math.atan2(fwd.x, -fwd.y), 4, 'Z'); cen = mathutils.Vector(((smn.x + smx.x) / 2, (smn.y + smx.y) / 2, smx.z))
    sme.transform(mathutils.Matrix.Translation(mathutils.Vector((mid.x, mid.y, topz))) @ R @ mathutils.Matrix.Scale(k_s, 4) @ mathutils.Matrix.Translation(-cen))
    zs = sorted(v.co.z for v in sme.vertices); under = zs[int(len(zs) * 0.35)]
    sme.transform(mathutils.Matrix.Translation(mathutils.Vector((0, 0, topz - under - 0.32 * (smx.z - smn.z) * k_s))))
    saddle = bpy.data.objects.new('Saddle', sme); sc.collection.objects.link(saddle); sme.materials.clear(); sme.materials.append(smat)
    best = min([n for n in ('Spine1', 'Spine2', 'Chest', 'Hips') if n in arm.data.bones], key=lambda n: (bp(n) - mid).length)
    saddle.parent = arm; saddle.matrix_parent_inverse = arm.matrix_world.inverted()
    vg = saddle.vertex_groups.new(name=best); vg.add(list(range(len(sme.vertices))), 1.0, 'REPLACE')
    md = saddle.modifiers.new('Armature', 'ARMATURE'); md.object = arm
    arm.data.pose_position = 'POSE'
    print('SADDLE', round(k_s, 3), best, 'width', round(width, 2), 'top', round(topz, 2))

# riding size, measured standing (the first frame of the idle)
if arm.animation_data is None: arm.animation_data_create()
idle = bpy.data.actions.get('Idle') or bpy.data.actions[0]
arm.animation_data.action = idle
try: arm.animation_data.action_slot = idle.slots[0]
except Exception: pass
sc.frame_set(int(idle.frame_range[0])); bpy.context.view_layer.update(); dg = bpy.context.evaluated_depsgraph_get(); zz = []
for o in sc.objects:
    if o.type == 'MESH' and o.name != 'Saddle' and 'FurShell' not in o.name:   # (the beast's own height)
        e = o.evaluated_get(dg); mm = e.to_mesh(); zz += [(e.matrix_world @ v.co).z for v in mm.vertices]; e.to_mesh_clear()
k = height / (max(zz) - min(zz))
holder = bpy.data.objects.new(name + 'Root', None); sc.collection.objects.link(holder)
for o in sc.objects:
    if o.parent is None and o is not holder: o.parent = holder
holder.scale = (k, k, k)
arm.animation_data.action = None
for im in bpy.data.images:
    if im.size[0] > 1024:
        im.scale(1024, max(1, int(1024 * im.size[1] / im.size[0])))
    try: im.pack()
    except Exception: pass
bpy.ops.export_scene.gltf(filepath=dst, export_format='GLB', export_skins=True, export_animations=True, export_animation_mode='ACTIONS', export_force_sampling=True, export_morph=False, export_image_format='AUTO')
# the fur shells: cut out (MASK) at their own height, both faces drawn
if fur_shells:
    import json, struct
    data = open(dst, 'rb').read(); jl = struct.unpack('<I', data[12:16])[0]; js = json.loads(data[20:20 + jl]); rest = data[20 + jl:]
    cuts = {n: (c, f) for n, c, f in fur_shells}
    for m in js.get('materials', []):
        if m.get('name') in cuts:
            c, f = cuts[m['name']]
            m['alphaMode'] = 'MASK'; m['alphaCutoff'] = c; m['doubleSided'] = True
            m.setdefault('pbrMetallicRoughness', {})['baseColorFactor'] = [f, f, f, 1.0]
    nj = json.dumps(js, separators=(',', ':')).encode(); nj += b' ' * ((4 - len(nj) % 4) % 4)
    out = struct.pack('<III', 0x46546C67, 2, 12 + 8 + len(nj) + len(rest)) + struct.pack('<II', len(nj), 0x4E4F534A) + nj + rest
    open(dst, 'wb').write(out); print('FIXED', len(fur_shells), 'fur materials')
print('DONE', name, 'scale', round(k, 3))
