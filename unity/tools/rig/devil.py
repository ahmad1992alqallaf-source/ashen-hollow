import sys; sys.path.insert(0,'/tmp/rig3'); from rig import *
L=['pelvis','thigh_l','calf_l','foot_l','ball_l','thigh_r','calf_r','foot_r','ball_r']
AL=['clavicle_l','upperarm_l','lowerarm_l','hand_l']; AR=[a[:-1]+'r' for a in AL]
def skirt(P,N,Wk,d):
    # the cloth hanging round the hips (well outside the leg): more on the hips the further out, never on the knees
    import numpy as np
    dl=np.minimum(d(P,'thigh_l'),d(P,'calf_l')); dr=np.minimum(d(P,'thigh_r'),d(P,'calf_r')); dm=np.minimum(dl,dr)
    m=(P[:,1]>0.28)&(P[:,1]<0.98)&(abs(P[:,0])<0.27)&(dm>0.09)
    t=np.clip((dm-0.09)/0.06,0,1); f=np.clip((0.98-P[:,1])/0.6,0,1)
    side=np.where(dl<dr,'thigh_l','thigh_r')
    for i in np.where(m)[0]:
        w={}
        for n,x in zip(N[i],Wk[i]): w[n]=w.get(n,0)+x*(1-t[i])
        w['pelvis']=w.get('pelvis',0)+t[i]*(1-0.4*f[i]); w[side[i]]=w.get(side[i],0)+t[i]*0.4*f[i]
        top=sorted(w.items(),key=lambda kv:-kv[1])[:4]
        while len(top)<4: top.append(('pelvis',0.0))
        N[i]=[k for k,_ in top]; v=np.array([x for _,x in top]); Wk[i]=v/v.sum()
    print('skirt verts',m.sum(), (t[m]>0.99).sum())
C=dict(name='Devil', mesh='/tmp/rig3/devil_s.glb', offset=[0.05,1.0,0], out=sys.argv[1] if len(sys.argv)>1 else '/tmp/rig3/devil_rig.glb',
 JP={'pelvis':[0,1.00,-0.03],'spine_01':[0,1.10,-0.02],'spine_02':[0,1.22,-0.02],'spine_03':[0,1.34,-0.02],'neck_01':[0,1.47,0.0],'Head':[0,1.56,0.04],
     'clavicle_l':[0.06,1.40,-0.01],'upperarm_l':[0.25,1.36,-0.03],'lowerarm_l':[0.33,1.06,-0.04],'hand_l':[0.36,0.83,-0.02],
     'thigh_l':[0.12,0.95,-0.03],'calf_l':[0.175,0.55,-0.05],'foot_l':[0.175,0.12,-0.06],'ball_l':[0.19,0.03,0.11]},
 tip=[0.37,0.68,0.03], headtip=[0,1.75,0.10], tex='/tmp/dem/devil/source/devil/devil.png',
 regions=[
  (lambda P:(P[:,1]>1.52)&(abs(P[:,0])<0.32), ['neck_01','Head','spine_03'], 0.03),
  (lambda P:(P[:,1]<0.92)&(abs(P[:,0])<0.27), L, 0.03),
  (lambda P:(P[:,1]>0.3)&(P[:,1]<0.98)&(abs(P[:,0])<0.07)&(abs(P[:,2]+0.03)>0.09), ['pelvis','thigh_l','thigh_r'], 0.12),
  (lambda P:(P[:,1]>0.38)&(((P[:,0]>0.405)&(P[:,1]<0.76))|((P[:,0]>0.25)&(P[:,1]<0.615))|((P[:,0]>0.3)&(P[:,1]<0.72)&(P[:,2]<-0.09))), ['pelvis'], 0.05)],
 noarm=lambda P,c:((P[:,1]<0.82)&(abs(P[:,0])<0.245))|((P[:,1]<0.80)&(abs(P[:,0])<0.33)&(c[:,0]>1.8*c[:,1])&(c[:,0]>0.2))|((P[:,1]<1.0)&(abs(P[:,0])<0.2)),
 cuty=0.9,
 post=lambda P,N,Wk,d: skirt(P,N,Wk,d),
 extras={'title':'Realistic devil demon (game ready)','note':'rigged for Ashen Hollow'})
build(C)
