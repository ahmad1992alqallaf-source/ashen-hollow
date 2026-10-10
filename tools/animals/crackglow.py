# lava in the cracks: the darkest seams of a rock texture glow orange (a magma golem whose model came out plain basalt)
import bpy, sys, numpy as np
src, out, pct = sys.argv[-3], sys.argv[-2], float(sys.argv[-1])
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=src)
for mat in bpy.data.materials:
    if not mat.use_nodes: continue
    nt = mat.node_tree; bsdf = next((n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    if bsdf is None or not bsdf.inputs['Base Color'].links: continue
    tn = bsdf.inputs['Base Color'].links[0].from_node
    if tn.type != 'TEX_IMAGE': continue
    im = tn.image; w, h = im.size
    px = np.array(im.pixels[:], dtype=np.float32).reshape(h, w, 4); lum = px[..., :3].mean(2)
    t = np.percentile(lum, pct); mask = np.clip((t - lum) / max(1e-4, t * 0.5), 0, 1)
    em = np.zeros_like(px); em[..., 0] = 1.0 * mask; em[..., 1] = 0.35 * mask; em[..., 2] = 0.05 * mask; em[..., 3] = 1
    eim = bpy.data.images.new(im.name + '_lava', w, h); eim.pixels[:] = em.ravel(); eim.file_format = 'JPEG'
    en = nt.nodes.new('ShaderNodeTexImage'); en.image = eim
    nt.links.new(en.outputs['Color'], bsdf.inputs['Emission Color']); bsdf.inputs['Emission Strength'].default_value = 2.0
    eim.pack(); print('lava', mat.name, float(mask.mean()))
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', export_animations=True, export_jpeg_quality=85)
