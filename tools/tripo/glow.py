# add an emissive map to a Tripo model: the lava (bright orange/yellow) parts of its colour texture glow
import bpy, sys, numpy as np
src, out = sys.argv[-2], sys.argv[-1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
for mat in bpy.data.materials:
    mat.use_backface_culling = False   # double-sided: the plates' insides show, not the world through them
    if not mat.use_nodes: continue
    nt = mat.node_tree; bsdf = next((n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    if bsdf is None: continue
    link = bsdf.inputs['Base Color'].links
    if not link: continue
    tn = link[0].from_node
    while tn.type != 'TEX_IMAGE' and tn.inputs and tn.inputs[0].links: tn = tn.inputs[0].links[0].from_node
    if tn.type != 'TEX_IMAGE': continue
    im = tn.image; w, h = im.size
    px = np.array(im.pixels[:], dtype=np.float32).reshape(h, w, 4)
    r, g, b = px[..., 0], px[..., 1], px[..., 2]
    # image pixels are linear here; lava = strong red-orange with little blue
    mask = np.clip((r - 0.25) * 2.5, 0, 1) * np.clip((r - g * 1.25) * 6, 0, 1) * np.clip((0.3 - b) * 5, 0, 1)
    mask = np.clip(mask * 1.6, 0, 1)
    em = np.zeros_like(px); em[..., :3] = px[..., :3] * mask[..., None] * 1.0; em[..., 3] = 1
    eim = bpy.data.images.new(im.name + '_glow', w, h); eim.pixels[:] = em.ravel(); eim.file_format = 'JPEG'
    en = nt.nodes.new('ShaderNodeTexImage'); en.image = eim; en.interpolation = 'Linear'
    nt.links.new(en.outputs['Color'], bsdf.inputs['Emission Color']); bsdf.inputs['Emission Strength'].default_value = 1.0
    eim.pack()
    print('glow', mat.name, w, h, float(mask.mean()))
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', export_jpeg_quality=88)
