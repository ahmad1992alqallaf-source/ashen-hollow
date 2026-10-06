import struct, zlib, numpy as np, sys
def parse(path):
    d=open(path,'rb').read(); assert d[:18]==b'Kaydara FBX Binary'
    ver=struct.unpack('<I',d[23:27])[0]; big=ver>=7500
    def rd_node(o):
        if big: end,np_,pl=struct.unpack('<QQQ',d[o:o+24]); o+=24
        else: end,np_,pl=struct.unpack('<III',d[o:o+12]); o+=12
        nl=d[o]; o+=1
        if end==0: return None,o
        name=d[o:o+nl].decode(errors='ignore'); o+=nl; props=[]
        for _ in range(np_):
            t=chr(d[o]); o+=1
            if t in 'YCILFD': fm={'Y':'<h','C':'<?','I':'<i','L':'<q','F':'<f','D':'<d'}[t]; sz=struct.calcsize(fm); props.append(struct.unpack(fm,d[o:o+sz])[0]); o+=sz
            elif t in 'fdlib':
                n,enc,cl=struct.unpack('<III',d[o:o+12]); o+=12; raw=d[o:o+cl]; o+=cl
                if enc: raw=zlib.decompress(raw)
                props.append(np.frombuffer(raw,{'f':'<f4','d':'<f8','l':'<i8','i':'<i4','b':'<u1'}[t]))
            elif t in 'SR': n=struct.unpack('<I',d[o:o+4])[0]; o+=4; props.append(d[o:o+n]); o+=n
        kids=[]
        while o<end:
            k,o=rd_node(o)
            if k is None: break
            kids.append(k)
        return (name,props,kids),end
    o=27; top=[]
    while o<len(d)-200:
        n,o=rd_node(o)
        if n is None: break
        top.append(n)
    return top
def walk(n,depth=0,mx=3):
    name,props,kids=n
    ps=[(p.shape if isinstance(p,np.ndarray) else (p[:40] if isinstance(p,bytes) else p)) for p in props]
    print('  '*depth+name,ps[:4])
    if depth<mx:
        for k in kids: walk(k,depth+1,mx)
if __name__=='__main__':
    t=parse(sys.argv[1])
    for n in t:
        if n[0] in ('Objects','Connections','GlobalSettings'): walk(n,0,int(sys.argv[2]) if len(sys.argv)>2 else 2)
