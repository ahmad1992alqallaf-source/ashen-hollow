# materials for a rigged suit: double-sided as the lava suit; lava suits also get their glow map
import bpy, sys, numpy as np
src, out, glowArg = sys.argv[-3], sys.argv[-2], sys.argv[-1]
glow = glowArg in ('1', '2')   # 1: lava (orange) glows, 2: star cracks (blue-white) glow
exec(open('/tmp/claude-0/tripo/glow.py').read().split('for mat in bpy.data.materials:')[0].replace("src, out = sys.argv[-2], sys.argv[-1]", ""))
for mat in bpy.data.materials:
    mat.use_backface_culling = False
    # matte: Tripo's metal/roughness map made glaring white patches on dark leather under the game's sun
    if mat.use_nodes:
        b_ = next((n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
        if b_ is not None:
            for inp in ('Metallic', 'Roughness'):
                for l in list(b_.inputs[inp].links): mat.node_tree.links.remove(l)
            b_.inputs['Metallic'].default_value = 0.0; b_.inputs['Roughness'].default_value = 0.85
if glow:
    code = open('/tmp/claude-0/tripo/glow.py').read()
    code = code[code.index('for mat in bpy.data.materials:'):code.index('bpy.ops.export_scene')]
    if glowArg == '2':
        # Starfallen: the bright, clearly blue parts (star cracks, crystals) glow, not the grey steel
        code = code.replace("mask = np.clip((r - 0.25) * 2.5, 0, 1) * np.clip((r - g * 1.25) * 6, 0, 1) * np.clip((0.3 - b) * 5, 0, 1)",
                            "mask = np.clip((b - 0.3) * 2.5, 0, 1) * np.clip((b - r * 1.6) * 4, 0, 1)")
    exec(code)
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', export_jpeg_quality=88)
