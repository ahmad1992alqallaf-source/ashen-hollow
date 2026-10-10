# after merging Meshy's rig and clips: a game-sized mesh (decimated, weights kept) with 1K textures
import bpy, sys
src, out, tris = sys.argv[-3], sys.argv[-2], int(sys.argv[-1])
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=src)
arm = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE'][0]
for _o in list(bpy.context.scene.objects):
    if _o.type == 'MESH' and _o.name.startswith('Icosphere'): bpy.data.objects.remove(_o, do_unlink=True)
for o in [o for o in bpy.context.scene.objects if o.type == 'MESH']:
    n = len(o.data.polygons)
    if n > tris:
        bpy.context.view_layer.objects.active = o; d = o.modifiers.new('d', 'DECIMATE'); d.ratio = tris / n
        bpy.ops.object.modifier_move_to_index(modifier='d', index=0); bpy.ops.object.modifier_apply(modifier='d')
    print('mesh', o.name, n, '->', len(o.data.polygons))
for im in bpy.data.images:
    if im.size[0] > 1024: im.scale(1024, 1024)
if arm.animation_data:
    arm.animation_data.action = None
    for tr in arm.animation_data.nla_tracks: tr.mute = False
print('actions', [a.name for a in bpy.data.actions])
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', export_animations=True, export_animation_mode='NLA_TRACKS', export_image_format='JPEG', export_jpeg_quality=85)
print('out', out)
