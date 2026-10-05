import numpy as np
# landmarks in mesh space (left side; right mirrored)
JP = {
 'pelvis': [0, 0.80, -0.12], 'spine_01': [0, 0.93, -0.13], 'spine_02': [0, 1.06, -0.11], 'spine_03': [0, 1.18, -0.07],
 'neck_01': [0, 1.29, 0.02], 'Head': [0, 1.36, 0.14],
 'clavicle_l': [0.05, 1.25, -0.02], 'upperarm_l': [0.19, 1.20, -0.03], 'lowerarm_l': [0.43, 1.04, -0.02], 'hand_l': [0.55, 0.93, 0.38],
 'thigh_l': [0.14, 0.78, -0.07], 'calf_l': [0.27, 0.53, 0.06], 'foot_l': [0.26, 0.38, -0.19], 'ball_l': [0.34, 0.05, -0.02],
}
for k in list(JP):
    if k.endswith('_l'): p = JP[k]; JP[k[:-2] + '_r'] = [-p[0], p[1], p[2]]
JP = {k: np.array(v, float) for k, v in JP.items()}
