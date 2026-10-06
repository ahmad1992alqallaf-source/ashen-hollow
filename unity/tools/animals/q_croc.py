import sys; sys.path.insert(0, '/tmp/rig3'); from qrig import build
JP = {
 'Back': [0, 0.5, -0.2], 'Torso': [0, 0.55, 0.25], 'Torso2': [0, 0.58, 0.65], 'Torso3': [0, 0.58, 1.0],
 'Neck1': [0, 0.6, 1.22], 'Neck2': [0, 0.64, 1.36], 'Neck3': [0, 0.7, 1.48], 'Head': [0, 0.76, 1.58], 'HeadTip': [0, 0.85, 2.05],
 'FrontShoulder.L': [0.15, 0.45, 1.0], 'FrontUpperLeg.L': [0.3, 0.35, 1.02], 'FrontLowerLeg.L': [0.45, 0.18, 1.08], 'FrontPaw.L': [0.55, 0.02, 1.15],
 'FrontShoulder.R': [-0.15, 0.45, 1.0], 'FrontUpperLeg.R': [-0.28, 0.33, 1.0], 'FrontLowerLeg.R': [-0.4, 0.17, 1.03], 'FrontPaw.R': [-0.47, 0.02, 1.08],
 'BackShoulder.L': [0.12, 0.42, -0.15], 'BackLeg.L': [0.2, 0.36, -0.13], 'BackUpperLeg.L': [0.27, 0.25, -0.12], 'BackLowerLeg.L': [0.3, 0.12, -0.12], 'BackPaw.L': [0.3, 0.02, -0.08],
 'BackShoulder.R': [-0.12, 0.42, -0.1], 'BackLeg.R': [-0.22, 0.35, -0.02], 'BackUpperLeg.R': [-0.32, 0.25, 0.08], 'BackLowerLeg.R': [-0.42, 0.12, 0.18], 'BackPaw.R': [-0.5, 0.02, 0.3],
 'Tail1': [0, 0.45, -0.45], 'Tail2': [-0.03, 0.38, -0.7], 'Tail3': [-0.08, 0.3, -0.95], 'Tail4': [-0.15, 0.22, -1.2], 'Tail5': [-0.24, 0.16, -1.42],
 'Tail6': [-0.33, 0.1, -1.62], 'Tail7': [-0.42, 0.06, -1.8], 'Tail8': [-0.5, 0.04, -1.95], 'TailTip': [-0.57, 0.02, -2.12],
}
build(dict(name='Alligator', mesh='/tmp/uap/glb/American_Alligator.glb', JP=JP, out=sys.argv[1] if len(sys.argv) > 1 else '/tmp/rig3/qcroc.glb', soft=0.03))
