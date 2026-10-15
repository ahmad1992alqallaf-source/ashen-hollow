# Ashen Hollow: any rigged creature (a .glb, or an .fbx with its clips in other .fbx files) -> a store mount or pet
# (mount_<id>.glb / pet_<id>.glb for Resources/AH/Models/Comp). The models are never put in the repo: only this tool.
#   blender python: creature_mount.py -- <config.json> <id> <out.glb>
# The config (tools/mounts/creatures.json) says, for each creature:
#   src            the model file
#   anims          {"Game name": "source clip"} or {"Game name": ["source clip", first frame, last frame]};
#                  "file:<path>" as the source clip bakes the clip from another .fbx of the same skeleton
#   head, tail     bone names (or parts of them) at the front and back: the creature is turned to face the game's way
#   length         nose-to-tail length in metres it is scaled to (or "height": top of the back)
#   seat           the bone the saddle sits over (and rides on); "seat_at" [dx, dy] shifts it (metres, +y = back)
#   saddle         [width, length] of the saddle in metres, or 0 for none (a pet, or a chariot with its own seat)
#   lift           metres the creature floats above the ground (fliers)
#   tris           triangles to keep (it is thinned to about this many)
#   tex            {"mesh name part": "image path"} for .fbx files whose textures are not wired in
#   drop           names (or parts) of objects to leave out
#   still_root     bones whose movement is taken out of every clip (a clip that flies off across the scene)
#   colour         the saddle blanket's colour [r, g, b]
#   texsize        the largest a texture may be (1024)
import bpy, sys, json, math, os, mathutils, bmesh
from mathutils.bvhtree import BVHTree
a = sys.argv[sys.argv.index('--') + 1:]; C = json.load(open(a[0]))[a[1]]; dst = a[2]
base = os.path.dirname(os.path.abspath(a[0]))
def P(p): return p if os.path.isabs(p) else os.path.join(C.get('root', base), p)

bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene; sc.render.fps = 30
def imp(f):
    if f.lower().endswith('.fbx'): bpy.ops.import_scene.fbx(filepath=f, automatic_bone_orientation=False)
    else: bpy.ops.import_scene.gltf(filepath=f)
imp(P(C['src']))
for o in list(sc.objects):
    if o.name.startswith('Icosphere') or o.type in ('LIGHT', 'CAMERA') or any(d in o.name for d in C.get('drop', [])): bpy.data.objects.remove(o)
arms = [o for o in sc.objects if o.type == 'ARMATURE']; meshes = [o for o in sc.objects if o.type == 'MESH']
arm = max(arms, key=lambda o: len(o.data.bones)) if arms else None
def bone(name):
    for ar in arms:
        if name in ar.data.bones: return ar, ar.data.bones[name]
    for ar in arms:
        for b in ar.data.bones:
            if name.lower() in b.name.lower(): return ar, b
    raise Exception('no bone ' + name)

# ---- the clips ----
src_actions = list(bpy.data.actions)
clips = {}   # game name -> (action, f0, f1)
SPEED = {}
for game, s in C['anims'].items():
    rng = None
    if isinstance(s, list):
        if len(s) > 3: SPEED[game] = s[3]   # played this many times faster (a slow wingbeat made brisk)
        s, rng = s[0], (s[1], s[2])
    if s.startswith('file:'):
        # another .fbx of the same skeleton: its own skeleton is loaded beside the model, every bone made to follow its
        # twin in world space, and the result baked onto the model's bones
        fpath, _, want = s[5:].partition('#')   # "file:<path>#<clip>" picks one clip of several in that file
        before_o = set(sc.objects); before_a = set(bpy.data.actions)
        imp(P(fpath))
        new_o = [o for o in sc.objects if o not in before_o]; twin = [o for o in new_o if o.type == 'ARMATURE'][0]
        news = [x for x in bpy.data.actions if x not in before_a]
        clip = [x for x in news if want and (x.name == want or x.name.startswith(want + '_'))][:1] or news[:1]; clip = clip[0]
        if twin.animation_data is None: twin.animation_data_create()
        twin.animation_data.action = clip
        try: twin.animation_data.action_slot = clip.slots[0]
        except Exception: pass
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
        act = arm.animation_data.action; act.name = 'bake_' + game; arm.animation_data.action = None
        for o in new_o: bpy.data.objects.remove(o)
        for x in news: bpy.data.actions.remove(x)
        clips[game] = ([(arm, act)], f0, f1)
    else:
        # the named clip (the glTF importer names it per object: "<clip>" or "<clip>_<object>")
        users = []
        for ob in sc.objects:
            for act in src_actions:
                if act.name == s or act.name.startswith(s + '_') and act.name[len(s) + 1:] in ob.name:
                    if ob.type == 'ARMATURE' or (ob.animation_data and ob.animation_data.action == act): users.append((ob, act))
        if not users:
            # an .fbx clip carries the armature's name: "Armature|Take 001" ...
            for act in src_actions:
                if s in act.name: users.append((arm, act))
        if not users: raise Exception('no clip ' + s)
        # several objects that move (the chariot's two horses): each keeps its own action under the one game name
        seen = set(); u2 = []
        for ob, act in users:
            if ob.name in seen: continue
            seen.add(ob.name); u2.append((ob, act))
        f0, f1 = rng if rng else (int(u2[0][1].frame_range[0]), int(u2[0][1].frame_range[1]))
        clips[game] = (u2, f0, f1)
# movement taken out of the clips (a clip that flies away across the scene stays on the spot)
def fcurves(act):
    # (Blender 4.4+ keeps them in layers / strips / channel bags)
    if hasattr(act, 'layers') and len(act.layers):
        out = []
        for ly in act.layers:
            for st in ly.strips:
                for cb in st.channelbags: out += list(cb.fcurves)
        return out
    return list(getattr(act, 'fcurves', []))
for act in bpy.data.actions:
    for fc in fcurves(act):
        for b in C.get('still_root', []):
            if ('"' + b + '"') in fc.data_path and fc.data_path.endswith('location'):
                v = fc.keyframe_points[0].co[1] if len(fc.keyframe_points) else 0.0
                for k in fc.keyframe_points: k.co[1] = v; k.handle_left[1] = v; k.handle_right[1] = v
for ob in sc.objects:
    if ob.animation_data:
        ob.animation_data.action = None
        for t in list(ob.animation_data.nla_tracks): ob.animation_data.nla_tracks.remove(t)

# ---- standing as in its first clip (the idle), thinned ----
# (not the rest pose: some rigs - Sketchfab's - lie on their side at rest, the stand-up turn kept in the clips)
g0 = list(clips)[0]; pu, pf0, _ = clips[g0]
for ob, act in pu:
    if ob.animation_data is None: ob.animation_data_create()
    ob.animation_data.action = act
    try: ob.animation_data.action_slot = act.slots[0]
    except Exception: pass
sc.frame_set(int(pf0)); bpy.context.view_layer.update()
for o in meshes:
    if 'tex' in C:
        for part, path in C['tex'].items():
            if part in o.name or part == '*':
                img = bpy.data.images.load(P(path)); m = bpy.data.materials.new(o.name + '_m'); m.use_nodes = True
                t = m.node_tree.nodes.new('ShaderNodeTexImage'); t.image = img
                b = m.node_tree.nodes['Principled BSDF']; m.node_tree.links.new(t.outputs[0], b.inputs['Base Color'])
                b.inputs['Roughness'].default_value = 0.75
                o.data.materials.clear(); o.data.materials.append(m)
# parts cut away: the vertices of a mesh moved mostly by bones matching a pattern (the chariot's flying dust cards)
import re
for part, pat in C.get('cut', {}).items():
    for o in meshes:
        if part not in o.name: continue
        bm = bmesh.new(); bm.from_mesh(o.data); dl = bm.verts.layers.deform.active; names = {g.index: g.name for g in o.vertex_groups}; gone = []
        for v in bm.verts:
            if dl is None or not v[dl]:
                if C.get('cut_loose'): gone.append(v)   # vertices no bone moves (cards left standing in the scene)
                continue
            gi, w = max(v[dl].items(), key=lambda kv: kv[1])
            if re.search(pat, names.get(gi, '')): gone.append(v)
        bmesh.ops.delete(bm, geom=gone, context='VERTS'); bm.to_mesh(o.data); bm.free()
        print('CUT', o.name, len(gone), 'vertices')
# loose single quads (smoke and dust cards) taken off
for o in meshes:
    if not any(part in o.name for part in C.get('cut_cards', [])): continue
    bm = bmesh.new(); bm.from_mesh(o.data); seen = set(); gone = []
    for v in bm.verts:
        if v in seen: continue
        st = [v]; comp = []; seen.add(v)
        while st:
            x = st.pop(); comp.append(x)
            for e in x.link_edges:
                y = e.other_vert(x)
                if y not in seen: seen.add(y); st.append(y)
        if len(comp) == 4: gone += comp
    bmesh.ops.delete(bm, geom=gone, context='VERTS'); bm.to_mesh(o.data); bm.free(); print('CARDS', o.name, len(gone) // 4)
total = sum(len(p.vertices) - 2 for o in meshes for p in o.data.polygons)
if C.get('tris') and total > C['tris'] * 1.1:
    r = C['tris'] / total
    for o in meshes:
        if o.data.shape_keys: continue
        bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active = o
        d = o.modifiers.new('thin', 'DECIMATE'); d.ratio = r
        while o.modifiers[0] != d: bpy.ops.object.modifier_move_up(modifier='thin')
        bpy.ops.object.modifier_apply(modifier='thin')
    print('THINNED', total, '->', sum(len(p.vertices) - 2 for o in meshes for p in o.data.polygons))

# a holder for turning, scaling and placing the whole creature
holder = bpy.data.objects.new('MountRoot', None); sc.collection.objects.link(holder)
for o in list(sc.objects):
    if o is not holder and o.parent is None:
        mw = o.matrix_world.copy(); o.parent = holder; o.matrix_world = mw
bpy.context.view_layer.update()
def bw(name):
    # where a bone is: the middle of the skin it moves (some imported rigs keep their bones in another space from
    # their mesh), else the bone's own head
    ar, b = bone(name); dg = bpy.context.evaluated_depsgraph_get(); acc = mathutils.Vector(); wsum = 0.0
    for o in meshes:
        vg = o.vertex_groups.get(b.name)
        if vg is None: continue
        me = o.evaluated_get(dg).to_mesh(); src = o.data
        for v, ve in zip(src.vertices, me.vertices):
            for g in v.groups:
                if g.group == vg.index and g.weight > 0.3: acc += (o.matrix_world @ ve.co) * g.weight; wsum += g.weight
        o.evaluated_get(dg).to_mesh_clear()
    if wsum > 0: return acc / wsum
    return ar.matrix_world @ b.head_local
def world_pts():
    dg = bpy.context.evaluated_depsgraph_get(); pts = []; tris = []
    for o in meshes:
        me = o.evaluated_get(dg).to_mesh(); n = len(pts)
        pts += [o.matrix_world @ v.co for v in me.vertices]
        tris += [[n + i for i in p.vertices] for p in me.polygons]
        o.evaluated_get(dg).to_mesh_clear()
    return pts, tris
# face -y (the game's forward): from the tail bone to the head bone
hd, tl = bw(C['head']), bw(C['tail']); print('HEAD', [round(v, 2) for v in hd], 'TAIL', [round(v, 2) for v in tl]); fw = hd - tl; fw.z = 0; fw.normalize()
yaw = math.atan2(fw.x, -fw.y)   # turning by -yaw brings fw onto -y
holder.rotation_euler = (0, 0, -yaw) if 'yaw' not in C else (0, 0, math.radians(C['yaw']))
bpy.context.view_layer.update()
pts, _ = world_pts()
mn = mathutils.Vector([min(p[i] for p in pts) for i in range(3)]); mx = mathutils.Vector([max(p[i] for p in pts) for i in range(3)])
if 'length' in C:
    # nose to tail along the body, measured on the bones (wings and tails that stick out do not count)
    hd, tl = bw(C['head']), bw(C['tail']); L = (hd - tl).length; k = C['length'] / max(1e-4, L)
else:
    k = C['height'] / max(1e-4, mx.z - mn.z)
holder.scale = (k, k, k); bpy.context.view_layer.update()
pts, tris = world_pts()
mn = mathutils.Vector([min(p[i] for p in pts) for i in range(3)])
# the seat: above the seat bone, where a ray going up from inside the body comes out through the back
sb = bw(C['seat']); dx, dy = C.get('seat_at', [0, 0])
holder.location = (-sb.x, -sb.y - dy, -mn.z + C.get('lift', 0.0)); bpy.context.view_layer.update()
pts, tris = world_pts(); bvh = BVHTree.FromPolygons(pts, tris)
sb = bw(C['seat']); sb.x += dx; sb.y += dy
def top_at(x, y, z0):
    # the last surface a ray going up from inside the body passes (the back, not a lump on its way)
    o = mathutils.Vector((x, y, z0)); best = None
    for _ in range(6):
        h = bvh.ray_cast(o, mathutils.Vector((0, 0, 1)), 3.0)
        if h[0] is None: break
        if best is not None and h[0].z - best > 0.1: break   # a wing, spikes or a mane above the back: stop
        best = h[0].z; o = h[0] + mathutils.Vector((0, 0, 0.002))
    return best
top = top_at(sb.x, sb.y, sb.z) or sb.z + 0.2
if 'seat_z' in C: top = sb.z + C['seat_z']   # a seat of its own (the chariot's floor): measured from the bone
print('SEAT bone', [round(v, 2) for v in sb], 'back', round(top, 3), 'scale', round(k, 4))

# ---- the saddle: a blanket, a padded seat with a pommel and a raised cantle, laid on the back ----
parts = []; seat_top = top
S = C.get('saddle', [0.5, 0.6])
def mat(name, col, rough=0.7, metal=0.0, emit=None):
    m = bpy.data.materials.new(name); m.use_nodes = True; b = m.node_tree.nodes['Principled BSDF']
    b.inputs['Base Color'].default_value = (*col, 1); b.inputs['Roughness'].default_value = rough; b.inputs['Metallic'].default_value = metal
    if emit: b.inputs['Emission Color'].default_value = (*emit, 1); b.inputs['Emission Strength'].default_value = 1.5
    return m
if S:
    W, Lh = S
    # the back's shape under the saddle: heights sampled on a grid, fitted with a smooth bowl (no lumps)
    import numpy as np
    rows = []
    for i in range(9):
        for j in range(9):
            x = sb.x + (i / 8 - 0.5) * W * 1.1; y = sb.y + (j / 8 - 0.5) * Lh * 1.1
            z = top_at(x, y, sb.z)
            if z is not None and abs(z - top) < 0.35: rows.append((x - sb.x, y - sb.y, z))
    A = np.array([[1, r[0], r[1], r[0] ** 2, r[1] ** 2, r[0] * r[1]] for r in rows]); Z = np.array([r[2] for r in rows])
    cf = np.linalg.lstsq(A, Z, rcond=None)[0] if len(rows) >= 8 else np.array([top, 0, 0, 0, 0, 0])
    def shell(x, y): u, v = x - sb.x, y - sb.y; return float(cf[0] + cf[1] * u + cf[2] * v + cf[3] * u * u + cf[4] * v * v + cf[5] * u * v)
    col = C.get('colour', [0.14, 0.018, 0.012])
    CLOTH = mat('Blanket', tuple(col), 0.85); TRIM = mat('Trim', (0.75, 0.48, 0.15), 0.35, 1.0)
    LEATHER = mat('Leather', (0.08, 0.04, 0.018), 0.55); EMBER = mat('Ember', (1.0, 0.35, 0.05), 0.5, 0.0, (1.0, 0.32, 0.04))
    def finish(o, m):
        o.data.materials.clear(); o.data.materials.append(m)
        for p in o.data.polygons: p.use_smooth = True
        return o
    def draped(name, w, l, cy, lift, m, thick, shape=None):
        R, Sg = 10, 40; me = bpy.data.meshes.new(name); verts = [(0.0, 0.0)]; faces = []
        for i in range(1, R + 1):
            for j in range(Sg):
                an = 2 * math.pi * j / Sg; verts.append((i / R * math.cos(an), i / R * math.sin(an)))
        for j in range(Sg): faces.append((0, 1 + j, 1 + (j + 1) % Sg))
        for i in range(1, R):
            b0, b1 = 1 + (i - 1) * Sg, 1 + i * Sg
            for j in range(Sg): faces.append((b0 + j, b1 + j, b1 + (j + 1) % Sg, b0 + (j + 1) % Sg))
        co = []
        for u, t in verts:
            x, y = sb.x + u * w / 2, cy + t * l / 2; co.append((x, y, shell(x, y) + lift + (shape(u, t) if shape else 0.0)))
        me.from_pydata(co, [], faces); me.update()
        for f in me.polygons:
            if f.normal.z < 0: f.flip()
        o = bpy.data.objects.new(name, me); sc.collection.objects.link(o)
        bpy.ops.object.select_all(action='DESELECT'); bpy.context.view_layer.objects.active = o; o.select_set(True)
        so = o.modifiers.new('thick', 'SOLIDIFY'); so.thickness = thick; so.offset = 1.0
        bpy.ops.object.modifier_apply(modifier='thick')
        return finish(o, m)
    f = W / 0.46   # the spider's saddle was 0.46 m across: everything in proportion
    parts.append(draped('Blanket', W, Lh, sb.y, 0.016 * f, CLOTH, 0.012 * f))
    parts.append(draped('BlanketTrim', W * 1.08, Lh * 1.07, sb.y, 0.009 * f, TRIM, 0.009 * f))
    parts.append(draped('Seat', W * 0.56, Lh * 0.56, sb.y, 0.03 * f, LEATHER, 0.045 * f,
                        lambda u, t: f * (0.08 * max(0.0, t) ** 3 + 0.04 * max(0.0, -t) ** 3 + 0.01 * u * u)))
    seat_top = shell(sb.x, sb.y) + (0.03 + 0.045) * f
    def prim(kind, name, loc, scl, m, **kw):
        getattr(bpy.ops.mesh, 'primitive_' + kind + '_add')(location=loc, **kw)
        o = bpy.context.active_object; o.name = name; o.scale = scl; bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
        return finish(o, m)
    py = sb.y - Lh * 0.28
    parts.append(prim('cylinder', 'Pommel', (sb.x, py, shell(sb.x, py) + 0.08 * f), (0.022 * f, 0.022 * f, 0.04 * f), TRIM, vertices=12))
    parts.append(prim('uv_sphere', 'PommelGem', (sb.x, py, shell(sb.x, py) + 0.13 * f), (0.026 * f, 0.026 * f, 0.026 * f), EMBER, segments=12, ring_count=8))
# the saddle rides the seat bone. It was laid on the creature as it stands in its idle; the armature moves it from
# where it would be at rest, so it is first put back where the seat bone's rest would carry it
sar, sbone = bone(C['seat']); pb = sar.pose.bones[sbone.name]
to_rest = (sar.matrix_world @ sbone.matrix_local) @ (sar.matrix_world @ pb.matrix).inverted()
for o in parts:
    for v in o.data.vertices: v.co = o.matrix_world.inverted() @ (to_rest @ (o.matrix_world @ v.co))
    vg = o.vertex_groups.new(name=sbone.name); vg.add(list(range(len(o.data.vertices))), 1.0, 'REPLACE')
    md = o.modifiers.new('Armature', 'ARMATURE'); md.object = sar
    mw = o.matrix_world.copy(); o.parent = sar; o.matrix_world = mw
# the rider's seat: a Socket_Saddle bone on the seat bone (at rest, likewise)
bpy.ops.object.select_all(action='DESELECT'); bpy.context.view_layer.objects.active = sar; sar.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
inv = sar.matrix_world.inverted(); eb = sar.data.edit_bones; s0 = eb.new('Socket_Saddle')
hp = to_rest @ mathutils.Vector((sb.x, sb.y, seat_top)); tp = to_rest @ mathutils.Vector((sb.x, sb.y, seat_top + 0.15))
s0.head = inv @ hp; s0.tail = inv @ tp; s0.parent = eb[sbone.name]; s0.use_deform = False
bpy.ops.object.mode_set(mode='OBJECT')
print('SOCKET', round(seat_top, 3), '(SEAT for companions.json)')

# ---- the clips, each an action named as the game wants it (a copy where one source clip serves two names, its
# frame range set where only part of a long clip is wanted). (Exporting NLA tracks instead wrecks some rigs.)
used = set(); keep = []; MULTI = False
# each clip baked onto every bone, starting from the idle's first pose: bones a clip leaves alone (some Sketchfab clips
# key only some bones) would otherwise fall back to the rest pose, which for those rigs lies on its side
def idle_pose():
    for ob, act in pu:
        ob.animation_data.action = act
        try: ob.animation_data.action_slot = act.slots[0]
        except Exception: pass
    sc.frame_set(int(pf0)); bpy.context.view_layer.update()
for game, (users, f0, f1) in clips.items():
    multi = len(users) > 1
    for ob, act in users:
        if ob.type != 'ARMATURE': continue
        idle_pose()
        ob.animation_data.action = act
        try: ob.animation_data.action_slot = act.slots[0] if len(act.slots) == 1 else [sl for sl in act.slots if ob.name in sl.name_display or sl.name_display in ob.name][0]
        except Exception: pass
        bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
        bpy.ops.object.mode_set(mode='POSE'); bpy.ops.pose.select_all(action='SELECT')
        bpy.ops.nla.bake(frame_start=int(f0), frame_end=int(f1), only_selected=False, visual_keying=True, clear_constraints=False, use_current_action=False, bake_types={'POSE'})
        bpy.ops.object.mode_set(mode='OBJECT')
        a2 = ob.animation_data.action; a2.use_fake_user = True
        k = SPEED.get(game, 1.0); g1 = f1
        if k != 1.0:
            for fc in fcurves(a2):
                for kp in fc.keyframe_points:
                    for attr in ('co', 'handle_left', 'handle_right'):
                        c = getattr(kp, attr); c[0] = f0 + (c[0] - f0) / k
            g1 = f0 + (f1 - f0) / k
        a2.use_frame_range = True; a2.frame_start = f0; a2.frame_end = g1
        if multi:
            # several skeletons (the chariot's two horses): one NLA track of this name on each, merged into one clip
            ob.animation_data.action = None
            tr = ob.animation_data.nla_tracks.new(); tr.name = game; st = tr.strips.new(game, int(f0), a2)
            a2.name = 'm_' + game + '_' + ob.name
        else: keep.append(a2)
        if not multi: break
    if multi: MULTI = True
if MULTI:
    for act in list(bpy.data.actions):
        if not act.name.startswith('m_'): bpy.data.actions.remove(act)
    for ob in sc.objects:
        if ob.animation_data: ob.animation_data.action = None
else:
  names = [a2.name for a2 in keep]
  for act in list(bpy.data.actions):
    if act not in keep: bpy.data.actions.remove(act)
  for a2, game in zip(keep, list(clips)): a2.name = game
if arm is not None and not MULTI:
    if arm.animation_data is None: arm.animation_data_create()
    arm.animation_data.action = keep[0]
    try: arm.animation_data.action_slot = keep[0].slots[0]
    except Exception: pass
for im in bpy.data.images:
    lim = C.get('texsize', 1024)
    if im.size[0] > lim:
        im.scale(lim, max(1, int(lim * im.size[1] / im.size[0])))
        try: im.pack()
        except Exception: pass
bpy.ops.export_scene.gltf(filepath=dst, export_format='GLB', export_skins=True, export_animations=True, export_animation_mode='NLA_TRACKS' if MULTI else 'ACTIONS', export_merge_animation='NLA_TRACK',
                          export_force_sampling=True, export_morph=False, export_image_format='AUTO', export_def_bones=False)
print('DONE', dst, 'clips', list(clips))
