# wings that move: a Meshy-rigged winged monster has no wing bones (its wings are skinned to the spine and shoulders and
# stay stiff), so this adds WingL / WingR to the upper spine at the shoulder blades and hands them the wing membrane:
# vertices behind the back and out to the side, away from the arms, blended in over a few centimetres so the root
# bends rather than tears. The game flaps the two bones (AHWingBeat). Clips and everything else are kept.
#   blender python wingbones.py -- in.glb out.glb [root_back root_side test.png]
import bpy, sys, math, mathutils
a = sys.argv[sys.argv.index('--') + 1:]
src, out = a[0], a[1]
BACK = float(a[2]) if len(a) > 2 else -0.12     # y (towards the back) where the wing starts, metres from the hips' plane
SIDE = float(a[3]) if len(a) > 3 else 0.16      # how far out from the spine the wing takes over
test = a[4] if len(a) > 4 else None
ARM = float(a[5]) if len(a) > 5 else 0.16      # how near the arm bones a vertex stays the arm's
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=src)
sc = bpy.context.scene
arm = [o for o in sc.objects if o.type == 'ARMATURE'][0]
meshes = [o for o in sc.objects if o.type == 'MESH' and o.find_armature() == arm]
if arm.animation_data:
    arm.animation_data.action = None
    for tr in arm.animation_data.nla_tracks: tr.mute = False
W = arm.matrix_world; Wi = W.inverted()
def wpos(n): return W @ arm.data.bones[n].head_local
hips, top = wpos('Hips'), wpos('Spine')
la, ra = wpos('LeftArm'), wpos('RightArm')
cx = hips.x; body_y = hips.y
# the back is toward the side the arms are not reaching (the mesh lies mostly behind the bones)
mid = sum((m.matrix_world @ mathutils.Vector(c) for m in meshes for c in m.bound_box), mathutils.Vector()) / (8 * len(meshes))
back = 1.0 if mid.y > body_y else -1.0
z0 = (hips.z + top.z) * 0.5
K = top.z / 1.57   # the sizes below were set on a 2.2 m fiend; bigger monsters scale them
# the bones: from the shoulder blades out and up
bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode='EDIT')
eb = arm.data.edit_bones; parent = eb['Spine']
for side, sx in (('WingL', 1.0 if la.x > cx else -1.0), ('WingR', 1.0 if ra.x > cx else -1.0)):
    b = eb.new(side)
    h = mathutils.Vector((cx + sx * 0.12 * K, body_y + back * 0.2 * K, top.z - 0.05 * K))
    t = mathutils.Vector((cx + sx * 0.75 * K, body_y + back * 0.45 * K, top.z + 0.3 * K))
    b.head = Wi @ h; b.tail = Wi @ t; b.parent = parent; b.use_deform = True
bpy.ops.object.mode_set(mode='OBJECT')
def sm(x, a, b):
    t = min(1.0, max(0.0, (x - a) / (b - a))); return t * t * (3 - 2 * t)
# the arms and legs are kept by where they are, not by their weights (Meshy skins the wing roots to the shoulders)
def seg(n0, n1):
    a0 = wpos(n0); b0 = wpos(n1) if n1 is not None and n1 in arm.data.bones else W @ arm.data.bones[n0].tail_local
    return a0, b0
chains = [seg('LeftArm', 'LeftForeArm'), seg('LeftForeArm', 'LeftHand'), seg('LeftHand', None),
          seg('RightArm', 'RightForeArm'), seg('RightForeArm', 'RightHand'), seg('RightHand', None),
          seg('LeftUpLeg', 'LeftLeg'), seg('LeftLeg', 'LeftFoot'), seg('RightUpLeg', 'RightLeg'), seg('RightLeg', 'RightFoot')]
def limbd(p, cs):
    best = 9.0
    for a0, b0 in cs:
        ab = b0 - a0; t = max(0.0, min(1.0, (p - a0).dot(ab) / max(1e-6, ab.length_squared)))
        best = min(best, (p - (a0 + ab * t)).length)
    return best
arms_, legs_ = chains[:6], chains[6:]
moved = 0
for m in meshes:
    gl = m.vertex_groups.get('WingL') or m.vertex_groups.new(name='WingL')
    gr = m.vertex_groups.get('WingR') or m.vertex_groups.new(name='WingR')
    names = {g.index: g.name for g in m.vertex_groups}
    mw = m.matrix_world
    for v in m.data.vertices:
        p = mw @ v.co
        dy = (p.y - body_y) * back
        sd = (SIDE if p.z > hips.z - 0.15 * K else 0.55) * K   # low down only the hanging wing tips, far out (not the tail)
        w = sm(abs(p.x - cx), sd, sd + 0.22 * K) * sm(dy, (BACK + 0.12) * K, (BACK + 0.26) * K) * sm(p.z, hips.z - 0.9 * K, hips.z - 0.75 * K)
        if w <= 0.01: continue
        w *= sm(limbd(p, arms_), ARM * K, (ARM + 0.06) * K) * sm(limbd(p, legs_), 0.07 * K, 0.11 * K)
        if w <= 0.01: continue
        for g in v.groups:
            if names.get(g.group) not in ('WingL', 'WingR'): g.weight *= (1.0 - w)
        (gl if (p.x - cx) * (1 if la.x > cx else -1) > 0 else gr).add([v.index], w, 'REPLACE')
        moved += 1
print('wing verts', moved)
# at most four bones a vertex, weights adding to one, as the game's skinning takes them
for m in meshes:
    bpy.ops.object.select_all(action='DESELECT'); m.select_set(True); bpy.context.view_layer.objects.active = m
    bpy.ops.object.vertex_group_limit_total(group_select_mode='ALL', limit=4)
    bpy.ops.object.vertex_group_normalize_all(group_select_mode='ALL', lock_active=False)
if test:
    # the wings raised and lowered, to see that only the membrane moves
    pl, pr = arm.pose.bones['WingL'], arm.pose.bones['WingR']
    pl.rotation_mode = pr.rotation_mode = 'XYZ'
    cam = bpy.data.objects.new('c', bpy.data.cameras.new('c')); sc.collection.objects.link(cam); sc.camera = cam
    sc.render.engine = 'BLENDER_WORKBENCH'; sc.display.shading.color_type = 'TEXTURE'; sc.render.resolution_x = 500; sc.render.resolution_y = 500
    c = (hips + top) * 0.5
    i = 0
    for ang in (-20, 0, 20):
        for ax in ('X', 'Z'):
            e = [0, 0, 0]; e['XYZ'.index(ax)] = math.radians(ang); pl.rotation_euler = e; pr.rotation_euler = [-x for x in e] if ax == 'Z' else e
            cam.location = c + mathutils.Vector((0, back, 0)) * 6 + mathutils.Vector((0, 0, 0.3))
            cam.rotation_euler = (mathutils.Vector(c) - cam.location).to_track_quat('-Z', 'Y').to_euler()
            sc.render.filepath = test + '_%d.png' % i; bpy.ops.render.render(write_still=True); i += 1
    pl.rotation_euler = pr.rotation_euler = (0, 0, 0)
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', export_animations=True, export_animation_mode='NLA_TRACKS', export_image_format='JPEG', export_jpeg_quality=85)
print('out', out)
