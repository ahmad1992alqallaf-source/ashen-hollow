import sys; sys.path.insert(0, '/tmp/rig3'); from grig import build
import numpy as np
JP = {
 'Spine_1': [0, 1.2, -0.3], 'Spine_2': [0, 1.25, 0.0], 'Spine_3': [0, 1.28, 0.25],
 'Neck_1': [0, 1.32, 0.45], 'Neck_2': [0, 1.55, 0.72], 'Neck_3': [0.01, 1.95, 0.76], 'Head': [0.01, 2.33, 0.74],
 'Pelvis.L': [0.07, 1.16, -0.22], 'Hip.L': [0.13, 1.14, -0.2], 'UpperLeg.L': [0.15, 1.12, -0.18], 'LowerLeg.L': [0.17, 1.0, -0.12], 'Ankle.L': [0.17, 0.86, -0.28], 'Foot.L': [0.16, 0.07, -0.26],
 'Pelvis.R': [-0.07, 1.16, -0.15], 'Hip.R': [-0.13, 1.14, -0.13], 'UpperLeg.R': [-0.15, 1.12, -0.1], 'LowerLeg.R': [-0.18, 1.0, -0.05], 'Ankle.R': [-0.19, 0.85, 0.0], 'Foot.R': [-0.19, 0.07, 0.25],
 'Thigh.L': [0.14, 1.08, -0.15], 'Thigh.R': [-0.14, 1.08, -0.08],
 'Shoulder.L': [0.15, 1.3, 0.2], 'UpperArm.L': [0.3, 1.28, 0.1], 'LowerArm.L': [0.41, 1.22, -0.05], 'Hand.L': [0.45, 1.15, -0.2],
 'Shoulder.R': [-0.15, 1.3, 0.2], 'UpperArm.R': [-0.3, 1.28, 0.1], 'LowerArm.R': [-0.41, 1.22, -0.05], 'Hand.R': [-0.45, 1.15, -0.2],
 'Tail_1': [0, 1.2, -0.55], 'Tail_2': [0, 1.15, -0.7], 'Tail_3': [0, 1.1, -0.84],
}
T = {'L': [0.18, 0.0, -0.06], 'R': [-0.2, 0.0, 0.46]}
segs = [('Spine_1', JP['Spine_1'], JP['Spine_2'], 'body'), ('Spine_2', JP['Spine_2'], JP['Spine_3'], 'body'), ('Spine_3', JP['Spine_3'], JP['Neck_1'], 'body'),
        ('Neck_1', JP['Neck_1'], JP['Neck_2'], 'head'), ('Neck_2', JP['Neck_2'], JP['Neck_3'], 'head'), ('Neck_3', JP['Neck_3'], JP['Head'], 'head'),
        ('Head', JP['Head'], [0.01, 2.35, 0.93], 'head'),
        ('Tail_1', JP['Tail_1'], JP['Tail_2'], 'tail'), ('Tail_2', JP['Tail_2'], JP['Tail_3'], 'tail'), ('Tail_3', JP['Tail_3'], [0, 1.0, -0.93], 'tail')]
for s in ('L', 'R'):
    segs += [('UpperLeg.'+s, JP['UpperLeg.'+s], JP['LowerLeg.'+s], 'L'+s), ('LowerLeg.'+s, JP['LowerLeg.'+s], JP['Ankle.'+s], 'L'+s),
             ('Ankle.'+s, JP['Ankle.'+s], JP['Foot.'+s], 'L'+s), ('Foot.'+s, JP['Foot.'+s], T[s], 'L'+s),
             ('UpperArm.'+s, JP['UpperArm.'+s], JP['LowerArm.'+s], 'W'+s), ('LowerArm.'+s, JP['LowerArm.'+s], JP['Hand.'+s], 'W'+s),
             ('Hand.'+s, JP['Hand.'+s], np.array(JP['Hand.'+s]) + [0.02, -0.05, -0.12], 'W'+s)]
def bias(P, D, names):
    # bare legs below the feathers belong to the legs only
    low = P[:, 1] < 0.9
    for q, n in enumerate(names):
        if not (n.startswith(('UpperLeg', 'LowerLeg', 'Ankle', 'Foot'))): D[low, q] += 1.0
    return D
R = 'RaptorArmature|Raptor_'
build(dict(name='Ostrich', mesh='/tmp/ost/ost_s.glb', skeleton='/mnt/user-data/outputs/AshenHollow/Resources/AH/Models/Beasts/b_raptor.glb',
           JP=JP, segs=segs, bias=bias, soft=0.03, hk=1.0, out=sys.argv[1] if len(sys.argv) > 1 else '/tmp/ost/ost_rig.glb',
           clips=[R+'Idle1_Anim', R+'Idle2_Anim', R+'Walk_Anim', R+'Run1_Anim', R+'Bite1_Anim', R+'Bite2_Anim', R+'Hit1_Anim', R+'Death1_Anim'],
           extras={'title': 'African ostrich (Revised version)', 'author': 'Андрей (sketchfab.com/andrey.tnt12561)', 'license': 'CC-BY-4.0', 'note': 'reduced and rigged to the raptor clips for Ashen Hollow'}))
