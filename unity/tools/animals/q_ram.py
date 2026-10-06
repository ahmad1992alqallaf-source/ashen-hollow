import sys; sys.path.insert(0, '/tmp/rig3'); from qrig import build
import numpy as np
T = [[0, 0.86 - 0.02*i, -0.62 - 0.01*i] for i in range(9)]
JP = {
 'Back': [0, 0.85, -0.5], 'Torso': [0, 0.88, -0.2], 'Torso2': [0, 0.9, 0.05], 'Torso3': [0, 0.92, 0.25],
 'Neck1': [0, 0.98, 0.35], 'Neck2': [-0.01, 1.04, 0.45], 'Neck3': [-0.03, 1.1, 0.53], 'Head': [-0.05, 1.15, 0.62], 'HeadTip': [-0.14, 1.02, 0.83],
 'FrontShoulder.L': [0.06, 0.85, 0.24], 'FrontUpperLeg.L': [0.08, 0.68, 0.22], 'FrontLowerLeg.L': [0.08, 0.45, 0.17], 'FrontPaw.L': [0.07, 0.02, 0.19],
 'FrontShoulder.R': [-0.06, 0.85, 0.24], 'FrontUpperLeg.R': [-0.09, 0.68, 0.22], 'FrontLowerLeg.R': [-0.1, 0.45, 0.19], 'FrontPaw.R': [-0.11, 0.02, 0.23],
 'BackShoulder.L': [0.07, 0.82, -0.48], 'BackLeg.L': [0.15, 0.72, -0.52], 'BackUpperLeg.L': [0.2, 0.5, -0.62], 'BackLowerLeg.L': [0.23, 0.22, -0.77], 'BackPaw.L': [0.23, 0.03, -0.79],
 'BackShoulder.R': [-0.07, 0.82, -0.45], 'BackLeg.R': [-0.05, 0.72, -0.45], 'BackUpperLeg.R': [-0.03, 0.58, -0.45], 'BackLowerLeg.R': [-0.03, 0.25, -0.55], 'BackPaw.R': [-0.03, 0.02, -0.48],
}
for i, p in enumerate(T[:8]): JP['Tail%d' % (i+1)] = p
JP['TailTip'] = T[8]
build(dict(name='Bighorn', mesh='/tmp/uap/glb/Bighorn.glb', offset=[-0.05, 0, 0], JP=JP, out=sys.argv[1] if len(sys.argv) > 1 else '/tmp/rig3/qram.glb', soft=0.025))
