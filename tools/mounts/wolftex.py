import bpy, os
T='/tmp/claude-0/newmounts/wolf_fbx/textures/'
def img(n,nc=False):
    i=bpy.data.images.load(T+n); 
    if nc: i.colorspace_settings.name='Non-Color'
    return i
def mk(mat, base, normal=None, rough=None, metal=None, emis=None, alpha=False):
    mat.use_nodes=True; nt=mat.node_tree; nt.nodes.clear()
    out=nt.nodes.new('ShaderNodeOutputMaterial'); b=nt.nodes.new('ShaderNodeBsdfPrincipled'); nt.links.new(b.outputs[0],out.inputs[0])
    t=nt.nodes.new('ShaderNodeTexImage'); t.image=img(base); nt.links.new(t.outputs['Color'],b.inputs['Base Color'])
    if alpha: nt.links.new(t.outputs['Alpha'],b.inputs['Alpha']); mat.blend_method='HASHED' if hasattr(mat,'blend_method') else None
    if normal:
        n=nt.nodes.new('ShaderNodeTexImage'); n.image=img(normal,True); nm=nt.nodes.new('ShaderNodeNormalMap'); nt.links.new(n.outputs['Color'],nm.inputs['Color']); nt.links.new(nm.outputs[0],b.inputs['Normal'])
    if rough:
        r=nt.nodes.new('ShaderNodeTexImage'); r.image=img(rough,True); nt.links.new(r.outputs['Color'],b.inputs['Roughness'])
    if metal:
        m=nt.nodes.new('ShaderNodeTexImage'); m.image=img(metal,True); nt.links.new(m.outputs['Color'],b.inputs['Metallic'])
    if emis:
        e=nt.nodes.new('ShaderNodeTexImage'); e.image=img(emis); nt.links.new(e.outputs['Color'],b.inputs['Emission Color']); b.inputs['Emission Strength'].default_value=1.0
for m in bpy.data.materials:
    if m.name.startswith('mat test.002'): mk(m,'magic_wolf_Final_retopo2_bake_AlbedoTransp.png','magic_wolf_Final_retopo2_bake_Normal.png','bake_Roughness.png',None,'magic_wolf_Final_retopo2_bake_Emission.png',alpha=True)
    elif m.name.startswith('mat test.001') or m.name.startswith('Gum'): mk(m,'magic_wolf_Final_retopo_mat_test_BaseColor.png','magic_wolf_Final_retopo_mat_test_Normal.png','magic_wolf_Final_retopo_mat_test_Roughness.png','magic_wolf_Final_retopo_mat_test_Metallic.png')
print('MATS',[m.name for m in bpy.data.materials])
