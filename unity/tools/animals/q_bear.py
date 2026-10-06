import sys; sys.path.insert(0, '/tmp/rig3'); from qrig import build
JP = {
 'Back': [0, 1.22, -0.95], 'Torso': [0, 1.28, -0.6], 'Torso2': [0, 1.32, -0.2], 'Torso3': [0, 1.33, 0.2],
 'Neck1': [0.01, 1.33, 0.48], 'Neck2': [0.03, 1.36, 0.66], 'Neck3': [0.06, 1.39, 0.84], 'Head': [0.09, 1.42, 1.0], 'HeadTip': [0.17, 1.3, 1.38],
 'Ear1.L': [0.27, 1.62, 0.93], 'Ear2.L': [0.28, 1.66, 0.93], 'Ear3.L': [0.29, 1.7, 0.93], 'Ear4.L': [0.3, 1.74, 0.93],
 'Ear1.R': [-0.08, 1.62, 0.93], 'Ear2.R': [-0.09, 1.66, 0.93], 'Ear3.R': [-0.1, 1.7, 0.93], 'Ear4.R': [-0.11, 1.74, 0.93],
 'FrontShoulder.L': [0.14, 1.2, 0.18], 'FrontUpperLeg.L': [0.24, 1.0, 0.18], 'FrontLowerLeg.L': [0.27, 0.45, 0.16], 'FrontPaw.L': [0.28, 0.04, 0.3],
 'FrontShoulder.R': [-0.14, 1.2, 0.12], 'FrontUpperLeg.R': [-0.2, 1.0, 0.12], 'FrontLowerLeg.R': [-0.22, 0.45, 0.12], 'FrontPaw.R': [-0.22, 0.04, 0.25],
 'BackShoulder.L': [0.14, 1.08, -1.0], 'BackLeg.L': [0.17, 0.95, -1.08], 'BackUpperLeg.L': [0.14, 0.55, -1.18], 'BackLowerLeg.L': [0.13, 0.2, -1.27], 'BackPaw.L': [0.12, 0.03, -1.12],
 'BackShoulder.R': [-0.14, 1.08, -0.92], 'BackLeg.R': [-0.2, 0.95, -0.88], 'BackUpperLeg.R': [-0.25, 0.55, -0.8], 'BackLowerLeg.R': [-0.26, 0.2, -0.76], 'BackPaw.R': [-0.27, 0.03, -0.62],
 'Tail1': [0, 1.15, -1.32], 'Tail2': [0, 1.13, -1.36], 'Tail3': [0, 1.11, -1.39], 'Tail4': [0, 1.09, -1.42], 'Tail5': [0, 1.07, -1.44], 'Tail6': [0, 1.05, -1.46], 'Tail7': [0, 1.03, -1.47], 'Tail8': [0, 1.01, -1.48], 'TailTip': [0, 0.99, -1.49],
}
build(dict(name='Bear', mesh='/tmp/uap/glb/Bear_001.glb', offset=[-0.05, 0, 0], JP=JP, out=sys.argv[1] if len(sys.argv) > 1 else '/tmp/rig3/qbear.glb', soft=0.035))
