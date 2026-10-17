import sys; sys.path.insert(0,'/tmp/claude-0/fix'); from glbio import *
LEG=('thigh','calf','foot','ball'); UP=('upperarm','lowerarm','hand','clavicle','neck','Head','head','spine_03','thumb','index','middle','ring','pinky')
def wrong(f):
  j,B=load(f); pr=j['meshes'][0]['primitives'][0]; p=pr['attributes']
  P=acc(j,B,p['POSITION']); Jn=acc(j,B,p['JOINTS_0']); W=acc(j,B,p['WEIGHTS_0']); JW,names=joints_world(j,B)
  main=Jn[np.arange(len(P)),W.argmax(1)]
  hip=JW[[i for i,n in enumerate(names) if n=='pelvis'][0]][1]
  nm=np.array([names[k] or '' for k in main])
  leg=np.array([any(n.startswith(x) for x in LEG) for n in nm]); up=np.array([any(n.startswith(x) for x in UP) for n in nm])
  bad=(leg&(P[:,1]>hip+0.25))|(up&(P[:,1]<hip-0.25))
  return bad, P, nm
if __name__=='__main__':
  import glob,os
  tot=0
  for f in sorted(glob.glob('/mnt/user-data/outputs/arm2/*.glb')+['/mnt/user-data/outputs/arm1/tqo_starfallen_warrior.glb','/mnt/user-data/outputs/arm1/tqo_starfallen_warrior_f.glb']):
    bad,P,nm=wrong(f)
    if bad.sum()>0:
      from collections import Counter
      tot+=1; print('%-28s %4d wrong  %s'%(os.path.basename(f)[:-4],bad.sum(),dict(Counter(nm[bad]).most_common(4))))
  print('suits with wrong ties:',tot)
