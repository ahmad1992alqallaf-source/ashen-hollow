import sys; sys.path.insert(0,'/tmp/rig3'); from rig import *
import numpy as np
K=0.17630
L=['pelvis','thigh_l','calf_l','foot_l','ball_l','thigh_r','calf_r','foot_r','ball_r']
C=dict(name='Demone', mesh='/tmp/rig3/demone_s.glb', scale=K, offset=[0,1.0655,0], out=sys.argv[1] if len(sys.argv)>1 else '/tmp/rig3/demone_rig.glb',
 JP={'pelvis':[0,0.95,-0.05],'spine_01':[0,1.05,0.0],'spine_02':[0,1.15,0.08],'spine_03':[0,1.25,0.15],'neck_01':[0,1.30,0.30],'Head':[0,1.30,0.42],
     'clavicle_l':[0.12,1.28,0.2],'upperarm_l':[0.42,1.25,0.25],'lowerarm_l':[0.80,0.88,0.28],'hand_l':[0.95,0.52,0.35],
     'thigh_l':[0.15,0.92,-0.03],'calf_l':[0.20,0.48,0.08],'foot_l':[0.19,0.12,-0.08],'ball_l':[0.21,0.02,0.08]},
 drop=lambda P:(abs(P[:,0])<0.09)&(P[:,1]>0.42)&(P[:,1]<0.745)&(P[:,2]>-0.08),
 tip=[0.85,0.12,0.42], headtip=[0,1.27,0.68], tex='/tmp/dem/demonzip/textures/demone_hipoly_defaultMat_BaseColor.png',
 texfx=lambda a: (a*2.3)**0.9, emis='/tmp/dem/demonzip/textures/demone_hipoly_defaultMat_Emissive.png', emisk=[1.0,0.55,0.3],
 regions=[
  (lambda P:(P[:,1]>1.40), ['spine_03'], 0.05),
  (lambda P:(P[:,1]<0.95)&(abs(P[:,0])<0.40)&(P[:,2]>-0.25), L, 0.03),
  (lambda P:(P[:,2]<-0.22)&(P[:,1]<0.75), ['pelvis'], 0.05),
  (lambda P:(P[:,1]>1.15)&(P[:,1]<=1.40)&(abs(P[:,0])<0.2)&(P[:,2]>0.3), ['neck_01','Head'], 0.04)],
 extras={'title':'Demone','note':'decimated and rigged for Ashen Hollow'})
build(C)
