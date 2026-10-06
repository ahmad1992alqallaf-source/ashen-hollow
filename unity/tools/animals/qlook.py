import sys, numpy as np
sys.path.insert(0,'/tmp/kit'); from gl import GLB
def loadP(f):
    g=GLB(f); P=[]
    for p in g.j['meshes'][0]['primitives']: P.append(g.acc(p['attributes']['POSITION']))
    P=np.vstack(P).astype(float); return np.c_[-P[:,2], P[:,1], P[:,0]]   # face +z
if __name__=='__main__':
    P=loadP(sys.argv[1]); H=P[:,1].max(); L=P[:,2].max()-P[:,2].min()
    print('bbox',P.min(0).round(2),P.max(0).round(2))
    st=(P[:,2].max()-P[:,2].min())/24
    for z in np.arange(P[:,2].min(),P[:,2].max(),st):
        S=P[(P[:,2]>=z)&(P[:,2]<z+st)]
        if not len(S): continue
        ys=np.sort(S[:,1]); 
        # gaps in y = legs separate from body
        gp=np.where(np.diff(ys)>0.04*H)[0]
        parts=np.split(ys,gp+1)
        desc=[]
        for q in parts:
            m=(S[:,1]>=q[0])&(S[:,1]<=q[-1]); xx=S[m,0]
            desc.append(f'y[{q[0]:.2f},{q[-1]:.2f}]x[{xx.min():.2f},{xx.max():.2f}]')
        print(f'z{z:.2f}', ' '.join(desc))
