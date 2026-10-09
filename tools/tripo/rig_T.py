# rig a Tripo full-body armour (A-pose, Tripo units) onto the Quaternius outfit skeleton, so the game can wear it like
# the other outfits: the Quaternius knight is posed into the armour's stance, the armour is fitted round it, the knight's
# bone weights are copied across, the stance becomes the bind pose, and the armour is exported with that skeleton.
import bpy, sys, math, mathutils, numpy as np
A = sys.argv
src_q, src_t, out, prev = A[-4], A[-3], A[-2], A[-1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src_q)
for o in list(bpy.context.scene.objects):
    if o.type == 'MESH' and o.parent is None: bpy.data.objects.remove(o)
arm = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE'][0]
qmeshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
print('arm mw', [list(r) for r in arm.matrix_world])

# 1) pose the knight's arms down into the armour's stance
X = 2.02; LEGO = float(A[-5])
def aim(name, d):
    pb = arm.pose.bones[name]; bpy.context.view_layer.update()
    cur = (pb.tail - pb.head).normalized(); d = mathutils.Vector(d).normalized()
    R = cur.rotation_difference(d).to_matrix().to_4x4()
    T = mathutils.Matrix.Translation(pb.head)
    pb.matrix = T @ R @ T.inverted() @ pb.matrix
    bpy.context.view_layer.update()
for side, s in (('l', 1), ('r', -1)):
    aim('upperarm_' + side, (s * 0.31, 0.0, -0.045))   # the T-pose armour: arms out, a little down
    aim('lowerarm_' + side, (s * 0.30, 0.0, -0.02))
    aim('hand_' + side, (s * 0.10, 0.0, -0.03))
    # the legs in the armour's wide stance
    aim('thigh_' + side, (s * LEGO, 0.0, -0.405))
    aim('calf_' + side, (s * 0.05, 0.0, -0.446))
bpy.context.view_layer.update()
# how far each bone moved from its rest (a world-space transform): the game's skeleton keeps its own bone frames, so the
# bind matrices are rebuilt from these after export (see fixbind.py)
import json
D = {}
for pb in arm.pose.bones:
    P = arm.matrix_world @ pb.matrix; Rm = arm.matrix_world @ pb.bone.matrix_local
    D[pb.name] = [list(r) for r in (P @ Rm.inverted())]
json.dump(D, open(out + '.delta.json', 'w'))

# 2) the armour, fitted round the posed knight: across and front-to-back by X, up through the joints' heights
bpy.ops.import_scene.gltf(filepath=src_t)
t = [o for o in bpy.context.scene.objects if o.type == 'MESH' and o not in qmeshes][0]
t.name = 'LavaWarden_Armour'
bpy.context.view_layer.update()
mw = t.matrix_world.copy(); t.parent = None
tz = [0.0, 0.06, 0.28, 0.46, 0.72, 0.80, 0.974]          # the armour's ground, ankle, knee, hip, shoulder, head joint, top
qz = [0.0, 0.086, 0.542, 0.971, 1.456, 1.600, 1.600 + 0.18 * 2.06]
me = t.data
armv = []
for v in me.vertices:
    p = mw @ v.co
    armv.append((v.index, p.x, p.z, p.y))
    v.co = mathutils.Vector((p.x * X, p.y * X + 0.03, float(np.interp(p.z, tz, qz))))
t.matrix_world = mathutils.Matrix.Identity(4)
# the inside of the crotch (between the legs, behind the front panel and before the back flap) only ever showed as a
# sheet stretched from one leg to the other in a stride: it goes
import bmesh as _bm
_b = _bm.new(); _b.from_mesh(me); _b.verts.ensure_lookup_table()
_S = X
def _inner(v):
    x, y, z = v.co.x / _S, (v.co.y - 0.03) / _S, None
    return abs(x) < 0.06 and -0.085 < y < 0.075
_qlo, _qhi = float(np.interp(0.08, tz, qz)), float(np.interp(0.45, tz, qz))
_cut = [f for f in _b.faces if all(_inner(v) and _qlo < v.co.z < _qhi for v in f.verts)]
print('crotch faces cut', len(_cut))
_bm.ops.delete(_b, geom=_cut, context='FACES_ONLY'); _b.to_mesh(me); _b.free(); me.update()

me.update()

# 3) the knight's weights onto the armour (from the posed knight, nearest surface)
names = set(b.name for b in arm.data.bones)
for o in qmeshes: names |= set(g.name for g in o.vertex_groups)
for n in sorted(names): t.vertex_groups.new(name=n)
bpy.ops.object.select_all(action='DESELECT')
for o in qmeshes: o.select_set(True)
bpy.context.view_layer.objects.active = qmeshes[0]
bpy.ops.object.duplicate(); dup = list(bpy.context.selected_objects)
bpy.context.view_layer.objects.active = dup[0]; bpy.ops.object.join(); src = bpy.context.view_layer.objects.active
# a second source without the knight's arms, for everything that is not an arm (the hands hang beside the thighs)
bpy.ops.object.select_all(action='DESELECT')
for o in qmeshes:
    if 'Arms' not in o.name and 'Pauldron' not in o.name: o.select_set(True)
bpy.context.view_layer.objects.active = [o for o in qmeshes if o.select_get()][0]
bpy.ops.object.duplicate(); bpy.ops.object.join(); src2 = bpy.context.view_layer.objects.active
def weights_from(so):
    c = t.copy(); c.data = t.data.copy(); bpy.context.scene.collection.objects.link(c)
    dt = c.modifiers.new('dt', 'DATA_TRANSFER'); dt.object = so; dt.use_vert_data = True; dt.data_types_verts = {'VGROUP_WEIGHTS'}
    dt.vert_mapping = 'POLYINTERP_NEAREST'; dt.layers_vgroup_select_src = 'ALL'; dt.layers_vgroup_select_dst = 'NAME'
    bpy.ops.object.select_all(action='DESELECT'); c.select_set(True); bpy.context.view_layer.objects.active = c
    bpy.ops.object.modifier_apply(modifier='dt')
    gn = {g.index: g.name for g in c.vertex_groups}
    W = [[(gn[g.group], g.weight) for g in v.groups if g.weight > 1e-4] for v in c.data.vertices]
    bpy.data.objects.remove(c); return W
WA = weights_from(src); WB = weights_from(src2)
ARMK = ('upperarm', 'lowerarm', 'hand', 'thumb', 'index', 'middle', 'ring', 'pinky')
isarm = lambda n: any(k in n for k in ARMK)
cls = [False] * len(me.vertices)
for i, x, z, y in armv:
    if z < 0.55: cls[i] = False
    elif z < 0.0: cls[i] = abs(x) > 0.172                      # by the hands: only what stands clear of the thighs
    else: cls[i] = bool(WA[i]) and isarm(max(WA[i], key=lambda e: e[1])[0])   # higher: as the knight's nearest surface says
MAIN = ['pelvis','spine_01','spine_02','spine_03','neck_01','Head'] + [b + s for b in ('clavicle_','upperarm_','lowerarm_','hand_','thigh_','calf_','foot_') for s in 'lr']
def main(n):
    if n in MAIN: return n
    sd = n[-1] if n[-2:] in ('_l', '_r') else None
    if sd is None: return None
    for k, m in (('upperarm', 'upperarm_'), ('lowerarm', 'lowerarm_'), ('thumb', 'hand_'), ('index', 'hand_'), ('middle', 'hand_'), ('ring', 'hand_'), ('pinky', 'hand_'), ('thigh', 'thigh_'), ('calf', 'calf_'), ('ball', 'foot_')):
        if k in n: return m + sd
    return None
def clean(W):
    d = {}
    for n, w in W:
        m = main(n)
        if m: d[m] = d.get(m, 0) + w
    return list(d.items())
WA = [clean(W) for W in WA]; WB = [clean(W) for W in WB]
for i in range(len(me.vertices)):
    if cls[i]:
        W = [(n, w) for n, w in WA[i] if isarm(n) or n.startswith('clavicle')]
        if not W: W = [('hand_l' if armv[i][1] > 0 else 'hand_r', 1.0)]
    else:
        W = [(n, w) for n, w in WB[i] if not isarm(n)]
        _, x, z, y = armv[i]
        # the tabard in front and the cape flap behind hang from the belt, not from either thigh
        if abs(x) < 0.075 and ((0.1 < z < 0.47 and y < -0.09) or (0.22 < z < 0.47 and y > 0.08)): W = [('pelvis', 1.0)]
        # a leg's plates follow that leg only (a weight from the other leg stretched glowing skirt between the legs)
        elif z < 0.5 and abs(x) > 0.012:
            other = '_r' if x > 0 else '_l'
            W2 = [(n, w) for n, w in W if not ((n.startswith('thigh') or n.startswith('calf') or n.startswith('foot')) and n.endswith(other))]
            if W2: W = W2
        elif z < 0.5: W = [('pelvis', 1.0)]   # the middle seam between the legs stays with the hips
    for n, w in W: t.vertex_groups[n].add([i], w, 'REPLACE')
# the few faces joining a hand to a thigh plate (or an arm to the body) would stretch between the two: they are split
# off and stay with the body (their own copies of the arm-side corners follow the body), so no hole is left behind
import bmesh
bm = bmesh.new(); bm.from_mesh(me)
dl = bm.verts.layers.deform.verify()
bm.verts.ensure_lookup_table()
bridge = [f for f in bm.faces if len(set(cls[v.index] for v in f.verts)) > 1]
print('bridging faces kept with the body', len(bridge))
bodyW = {}
for f in bridge:
    for v in f.verts:
        if cls[v.index]:
            # the body's weights for this spot: the nearest body-side corner of the same face
            o = [u for u in f.verts if not cls[u.index]]
            if o: bodyW[v.index] = dict(o[0][dl])
res = bmesh.ops.split(bm, geom=bridge, use_only_faces=True)
newf = [g for g in res['geom'] if isinstance(g, bmesh.types.BMFace)]
for f in newf:
    for v in f.verts:
        near = min(bodyW.keys(), key=lambda k: (bm.verts[k].co - v.co).length_squared) if False else None
for f in newf:
    body_corner = None
    for v in f.verts:
        d = dict(v[dl]); armish = any(t.vertex_groups[g].name.startswith(('upperarm', 'lowerarm', 'hand')) for g in d)
        if not armish: body_corner = d
    if body_corner is None: continue
    for v in f.verts:
        d = dict(v[dl])
        if any(t.vertex_groups[g].name.startswith(('upperarm', 'lowerarm', 'hand')) for g in d):
            v[dl].clear()
            for g, w in body_corner.items(): v[dl][g] = w
bm.to_mesh(me); bm.free(); me.update()

# smooth the weights a little, keep four per vertex
bpy.ops.object.select_all(action='DESELECT'); t.select_set(True); bpy.context.view_layer.objects.active = t
bpy.ops.object.mode_set(mode='WEIGHT_PAINT')
bpy.ops.object.vertex_group_smooth(group_select_mode='ALL', factor=0.5, repeat=2)
bpy.ops.object.vertex_group_limit_total(group_select_mode='ALL', limit=4)
bpy.ops.object.vertex_group_normalize_all(lock_active=False)
bpy.ops.object.mode_set(mode='OBJECT')
for o in qmeshes + [src, src2]: bpy.data.objects.remove(o)

# 4) the stance becomes the rest (bind) pose, and the armour hangs from the skeleton
bpy.context.view_layer.objects.active = arm; arm.select_set(True)
bpy.ops.object.mode_set(mode='POSE'); bpy.ops.pose.select_all(action='SELECT'); bpy.ops.pose.armature_apply(selected=False); bpy.ops.object.mode_set(mode='OBJECT')
t.parent = arm; am = t.modifiers.new('Armature', 'ARMATURE'); am.object = arm
bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True); t.select_set(True)
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', use_selection=True, export_skins=True, export_animations=False, export_jpeg_quality=88)
print('exported', out)

# 5) a test: arms raised to the side and one knee bent, to see the weights at work
def rot(name, axis, deg):
    pb = arm.pose.bones[name]; pb.rotation_mode = 'XYZ'; e = list(pb.rotation_euler); e['XYZ'.index(axis)] += math.radians(deg); pb.rotation_euler = e
bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode='POSE')
exec(open('/tmp/claude-0/tripo/testpose.py').read())
bpy.ops.object.mode_set(mode='OBJECT')
exec(open('/tmp/claude-0/tripo/render3.py').read())
