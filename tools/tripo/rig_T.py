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
    return abs(x) < 0.05 and -0.06 < y < 0.03
_qlo, _qhi = float(np.interp(0.08, tz, qz)), float(np.interp(0.38, tz, qz))
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
_tm = t.material_slots[0].material; _ti = None
for _n in _tm.node_tree.nodes:
    if _n.type == 'TEX_IMAGE' and _n.outputs['Color'].links and any(l.to_socket.name == 'Base Color' for l in _n.outputs['Color'].links): _ti = _n.image
_TW, _TH = _ti.size; _tp = np.array(_ti.pixels[:], dtype=np.float32).reshape(_TH, _TW, 4)
_uvl = me.uv_layers.active.data; vcloth = [False] * len(me.vertices)
for _poly in me.polygons:
    for _li in _poly.loop_indices:
        _vi = me.loops[_li].vertex_index; _u, _v = _uvl[_li].uv
        _r, _g, _b = _tp[min(_TH - 1, max(0, int(_v * _TH))), min(_TW - 1, max(0, int(_u * _TW)))][:3]
        if _r > 0.12 and _r > _g * 2.2 and _r > _b * 1.8 and _g < 0.2: vcloth[_vi] = True
for i in range(len(me.vertices)):
    if cls[i]:
        W = [(n, w) for n, w in WA[i] if isarm(n) or n.startswith('clavicle')]
        if not W: W = [('hand_l' if armv[i][1] > 0 else 'hand_r', 1.0)]
        # the pauldron (the plates on top of the shoulder, inboard of the elbow): it rides the shoulder blade and only
        # partly turns with the arm, so dropping the arm from the T-pose doesn't fold it into the chest and open a hole
        _, x, z, y = armv[i]; sd = 'l' if x > 0 else 'r'
        if abs(x) < 0.25 and z > 0.70:
            k = min(1.0, max(0.0, (0.25 - abs(x)) / 0.12))     # all clavicle near the neck, more arm toward the elbow
            ca = 0.5 * k + 0.2 * (1 - k)
            W = [('clavicle_' + sd, ca), ('upperarm_' + sd, 1.0 - ca)]
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
        if z < 0.5 and vcloth[i] and armv[i][2] > 0.2: W = [('pelvis', 1.0)]   # the red cloth hangs from the belt like a skirt: it never stretches after a leg
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
# 3f) a core: the game's own knight body, posed the same and shrunk a little, sits inside the armour in the darkest
# colour of the armour's texture, so any chink between Tripo's plates (the neck, under the pauldrons) shows dark armour
bpy.ops.object.select_all(action='DESELECT'); src.select_set(True); bpy.context.view_layer.objects.active = src
bpy.ops.object.duplicate(); core = bpy.context.view_layer.objects.active; core.name = 'Core'
for _m in list(core.modifiers):
    if _m.type == 'ARMATURE': bpy.ops.object.modifier_apply(modifier=_m.name)
    else: core.modifiers.remove(_m)
_dcc = core.modifiers.new('dc', 'DECIMATE'); _dcc.ratio = 0.12; bpy.ops.object.modifier_apply(modifier='dc')
_dp = core.modifiers.new('dp', 'DISPLACE'); _dp.strength = -0.012; _dp.mid_level = 0.0; _dp.direction = 'NORMAL'
bpy.ops.object.modifier_apply(modifier='dp')
_tm2 = t.material_slots[0].material; _ti2 = None
for _n in _tm2.node_tree.nodes:
    if _n.type == 'TEX_IMAGE' and _n.outputs['Color'].links and any(l.to_socket.name == 'Base Color' for l in _n.outputs['Color'].links): _ti2 = _n.image
_W2, _H2 = _ti2.size; _p2 = np.array(_ti2.pixels[:], dtype=np.float32).reshape(_H2, _W2, 4)[..., :3].sum(2)
_y, _x = np.unravel_index(np.argmin(_p2[8:-8, 8:-8]), (_H2 - 16, _W2 - 16)); _du, _dv = (_x + 8 + 0.5) / _W2, (_y + 8 + 0.5) / _H2
core.data.materials.clear(); core.data.materials.append(_tm2)
for _poly in core.data.polygons: _poly.material_index = 0
if not core.data.uv_layers: core.data.uv_layers.new()
for _l in core.data.uv_layers.active.data: _l.uv = (_du, _dv)
# only the main bones, as the armour
for _v in core.data.vertices:
    _keep = {}
    for _g in _v.groups:
        _mn = main(core.vertex_groups[_g.group].name)
        if _mn: _keep[_mn] = _keep.get(_mn, 0) + _g.weight
    for _g in list(_v.groups): core.vertex_groups[_g.group].remove([_v.index])
    for _n, _w in _keep.items():
        if _n not in core.vertex_groups: core.vertex_groups.new(name=_n)
        core.vertex_groups[_n].add([_v.index], _w, 'REPLACE')
# not the knight's helm or boots: they poke past the lava helm's visor and the toes
import bmesh as _bm3
_cb = _bm3.new(); _cb.from_mesh(core.data); _dl3 = _cb.verts.layers.deform.verify()
_gi = {g.index: g.name for g in core.vertex_groups}
_drop = [v for v in _cb.verts if v[_dl3] and _gi[max(v[_dl3].items(), key=lambda kv: kv[1])[0]] in ('Head', 'foot_l', 'foot_r')]
_bm3.ops.delete(_cb, geom=_drop, context='VERTS'); _cb.to_mesh(core.data); _cb.free()
print('core faces', len(core.data.polygons), 'dark uv', round(_du, 3), round(_dv, 3))
bpy.ops.object.select_all(action='DESELECT'); core.select_set(True); t.select_set(True); bpy.context.view_layer.objects.active = t
bpy.ops.object.join(); me = t.data
# a dark gorget round the neck, under the helm's rim, so no daylight shows between the helm and the shoulders
_nk = arm.data.bones['neck_01']; _nh = arm.matrix_world @ _nk.head_local; _nt = arm.matrix_world @ arm.data.bones['Head'].head_local
bpy.ops.mesh.primitive_cylinder_add(vertices=14, radius=0.095, depth=(_nt - _nh).length + 0.1, location=((_nh + _nt) / 2))
gor = bpy.context.view_layer.objects.active; gor.name = 'Gorget'
gor.data.materials.append(_tm2)
if not gor.data.uv_layers: gor.data.uv_layers.new()
for _l in gor.data.uv_layers.active.data: _l.uv = (_du, _dv)
_g1 = gor.vertex_groups.new(name='neck_01'); _g1.add([v.index for v in gor.data.vertices], 1.0, 'REPLACE')
bpy.ops.object.select_all(action='DESELECT'); gor.select_set(True); t.select_set(True); bpy.context.view_layer.objects.active = t
bpy.ops.object.join(); me = t.data
for o in qmeshes + [src, src2]: bpy.data.objects.remove(o)

# 3e) a lining: a lighter copy of the armour, a little smaller, just inside it, so the hair-thin cracks between
# Tripo's plates (and the gaps the knees and elbows open) show armour behind them, never the world
bpy.ops.object.select_all(action='DESELECT'); t.select_set(True); bpy.context.view_layer.objects.active = t
bpy.ops.object.duplicate(); lin = bpy.context.view_layer.objects.active; lin.name = 'Lining'
for _m in list(lin.modifiers): lin.modifiers.remove(_m)
_dc = lin.modifiers.new('dc', 'DECIMATE'); _dc.ratio = 0.2
bpy.ops.object.modifier_apply(modifier='dc')
_dp = lin.modifiers.new('dp', 'DISPLACE'); _dp.strength = -0.022; _dp.mid_level = 0.0; _dp.direction = 'NORMAL'
bpy.ops.object.modifier_apply(modifier='dp')
print('lining faces', len(lin.data.polygons))
bpy.ops.object.select_all(action='DESELECT'); lin.select_set(True); t.select_set(True); bpy.context.view_layer.objects.active = t
bpy.ops.object.join(); me = t.data

# 3c) the red cloth (tabard, back flap) is one thin sheet: seen from behind it would be see-through, so each cloth face
# gets a back face of its own (the plates keep one side: their insides are never meant to be seen)
import bmesh as _bm2, numpy as _np
_mat = t.material_slots[0].material; _img = None
for _n in _mat.node_tree.nodes:
    if _n.type == 'TEX_IMAGE' and _n.outputs['Color'].links and any(l.to_socket.name == 'Base Color' for l in _n.outputs['Color'].links): _img = _n.image
_W, _H = _img.size; _px = _np.array(_img.pixels[:], dtype=_np.float32).reshape(_H, _W, 4)
_b2 = _bm2.new(); _b2.from_mesh(me); _uv = _b2.loops.layers.uv.active
def _cloth(f):
    u = sum(l[_uv].uv.x for l in f.loops) / len(f.loops); v = sum(l[_uv].uv.y for l in f.loops) / len(f.loops)
    r, g, b = _px[min(_H - 1, max(0, int(v * _H))), min(_W - 1, max(0, int(u * _W)))][:3]
    return r > 0.12 and r > g * 2.2 and r > b * 1.8 and g < 0.2
def _pale(f):
    u = sum(l[_uv].uv.x for l in f.loops) / len(f.loops); v = sum(l[_uv].uv.y for l in f.loops) / len(f.loops)
    r, g, b = _px[min(_H - 1, max(0, int(v * _H))), min(_W - 1, max(0, int(u * _W)))][:3]
    return min(r, g, b) > 0.35 and max(r, g, b) - min(r, g, b) < 0.12
_pf = [f for f in _b2.faces if _pale(f)]
print('pale untextured faces removed', len(_pf))
_bm2.ops.delete(_b2, geom=_pf, context='FACES_ONLY')
_cf = [f for f in _b2.faces if _cloth(f)]
_d = _bm2.ops.duplicate(_b2, geom=_cf)
_nf = [g for g in _d['geom'] if isinstance(g, _bm2.types.BMFace)]
_bm2.ops.reverse_faces(_b2, faces=_nf)
for _f in _nf:
    for _v in _f.verts: _v.co -= _f.normal * 0.002   # a hair behind the front face, no flicker
_b2.to_mesh(me); _b2.free(); me.update()
print('cloth faces given a back', len(_cf))

# 3d) bake the armour into an A-pose (arms 40 degrees down) before binding: the hero's arms hang down most of the
# time, so binding halfway there halves how far the shoulders ever have to bend
bpy.context.view_layer.update()
_am = t.modifiers.new('bake', 'ARMATURE'); _am.object = arm
for side, s_ in (('l', 1), ('r', -1)):
    aim('upperarm_' + side, (s_ * 0.64, 0.0, -0.77))
    aim('lowerarm_' + side, (s_ * 0.62, 0.0, -0.78))
    aim('hand_' + side, (s_ * 0.6, 0.0, -0.8))
bpy.context.view_layer.update()
bpy.ops.object.select_all(action='DESELECT'); t.select_set(True); bpy.context.view_layer.objects.active = t
bpy.ops.object.modifier_apply(modifier='bake')
D = {}
for pb in arm.pose.bones:
    P = arm.matrix_world @ pb.matrix; Rm = arm.matrix_world @ pb.bone.matrix_local
    D[pb.name] = [list(r) for r in (P @ Rm.inverted())]
json.dump(D, open(out + '.delta.json', 'w'))

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
