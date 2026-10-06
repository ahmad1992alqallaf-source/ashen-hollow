import sys, io, numpy as np
sys.path.insert(0,'/tmp/kit'); from gl import GLB
from PIL import Image
src, img, out = sys.argv[1:4]
g=GLB(src); j=g.j
im=Image.open(img).convert('RGB').resize((1024,1024),Image.LANCZOS); b=io.BytesIO(); im.save(b,'JPEG',quality=88); J=b.getvalue()
while len(g.bin)%4: g.bin.append(0)
o=len(g.bin); g.bin+=J; j['bufferViews'].append({'buffer':0,'byteOffset':o,'byteLength':len(J)})
j['images']=[{'bufferView':len(j['bufferViews'])-1,'mimeType':'image/jpeg'}]; j['textures']=[{'source':0}]
j['materials']=[{'pbrMetallicRoughness':{'baseColorTexture':{'index':0},'metallicFactor':0,'roughnessFactor':0.6},'doubleSided':True}]
for m in j['meshes']:
    for p in m['primitives']: p['material']=0
g.save(out)
