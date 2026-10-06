import sys; sys.path.insert(0,'/tmp/rig3'); from rig import *
import numpy as np
K=0.08625646923519263
L=['pelvis','thigh_l','calf_l','foot_l','ball_l','thigh_r','calf_r','foot_r','ball_r']
C=dict(name='BlackDemon', mesh='/tmp/rig3/black_s.glb', scale=K, offset=[0,1.99675,0], out=sys.argv[1] if len(sys.argv)>1 else '/tmp/rig3/black_rig.glb',
 JP={'pelvis':[0,1.0,-0.2],'spine_01':[0,1.15,-0.22],'spine_02':[0,1.32,-0.24],'spine_03':[0,1.50,-0.24],'neck_01':[0,1.76,-0.18],'Head':[0,1.90,-0.08],
     'clavicle_l':[0.08,1.62,-0.22],'upperarm_l':[0.30,1.57,-0.19],'lowerarm_l':[0.70,1.15,-0.05],'hand_l':[0.80,0.97,0.06],
     'thigh_l':[0.15,0.95,-0.18],'calf_l':[0.25,0.60,-0.06],'foot_l':[0.28,0.31,-0.21],'ball_l':[0.30,0.03,0.04]},
 tip=[0.83,0.78,0.11], headtip=[0,2.06,0.02], foot=1.2, tex='/tmp/dem/black/textures/demon_Albedo.png',
 texfx=lambda a: (a*1.9)**0.9,
 regions=[
  (lambda P:(P[:,1]>1.88)&(abs(P[:,0])<0.27), ['neck_01','Head','spine_03'], 0.04),
  (lambda P:(P[:,1]<0.98)&(abs(P[:,0])<0.42), L, 0.03)],
 extras={'title':'Black demon','note':'decimated and rigged for Ashen Hollow'})
build(C)
