# one GLB from Meshy's rigged model and its separate animation files: the model, its skeleton, and every animation as
# a named clip (Idle, Walk, Attack, Hit, Death) for the game's animator
import bpy, sys, os
rig, out, names = sys.argv[-3], sys.argv[-2], sys.argv[-1].split(',')
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=rig)
arm = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE'][0]
for a in list(bpy.data.actions): bpy.data.actions.remove(a)
# a stray helper sphere Meshy leaves in some files
for o in list(bpy.context.scene.objects):
    if o.type == 'MESH' and o.name.startswith('Icosphere'): bpy.data.objects.remove(o, do_unlink=True)
keep = set(bpy.context.scene.objects)
arm.animation_data_create()
for nm in names:
    f = rig.replace('/rig_', '/anim_').replace('.glb', '_' + nm + '.glb')
    before = set(bpy.data.actions)
    bpy.ops.import_scene.gltf(filepath=f)
    new = [a for a in bpy.data.actions if a not in before]
    for o in list(bpy.context.scene.objects):
        if o not in keep: bpy.data.objects.remove(o, do_unlink=True)
    act = new[0]; act.name = nm; act.use_fake_user = True
    tr = arm.animation_data.nla_tracks.new(); tr.name = nm
    tr.strips.new(nm, int(act.frame_range[0]), act)
    print('CLIP', nm, act.frame_range[:])
arm.animation_data.action = None
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', export_animations=True, export_animation_mode='NLA_TRACKS', export_image_format='JPEG', export_jpeg_quality=88)
print('MERGED', out, os.path.getsize(out))
