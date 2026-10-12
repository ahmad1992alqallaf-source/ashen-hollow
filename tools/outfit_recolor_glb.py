# recolour the greens of every texture inside an outfit .glb (as outfit_recolor.py does for one image) and write a
# new .glb: used for the class-coloured copies of the Ranger outfit (its hood worn by mages and druids)
#   python outfit_recolor_glb.py in.glb out.glb hue sat_k val_k
import sys, json, struct, io
import numpy as np
from PIL import Image
src, out, hue, sat_k, val_k = sys.argv[1], sys.argv[2], float(sys.argv[3]), float(sys.argv[4]), float(sys.argv[5])
b = open(src, 'rb').read()
jl = struct.unpack('<I', b[12:16])[0]; j = json.loads(b[20:20 + jl])
bo = 20 + jl; bl = struct.unpack('<I', b[bo:bo + 4])[0]; bin_ = b[bo + 8:bo + 8 + bl]
def recolor(data):
    im = Image.open(io.BytesIO(data)); fmt = im.format; im = im.convert('RGBA'); a = im.split()[3]
    hsv = np.array(im.convert('RGB').convert('HSV')).astype(np.float32)
    h = hsv[..., 0] * 360 / 255
    m = (h >= 55) & (h <= 170) & (hsv[..., 1] > 25)
    hsv[..., 0][m] = hue * 255 / 360
    hsv[..., 1][m] = np.clip(hsv[..., 1][m] * sat_k, 0, 255)
    hsv[..., 2][m] = np.clip(hsv[..., 2][m] * val_k, 0, 255)
    o = Image.fromarray(hsv.astype(np.uint8), 'HSV').convert('RGB'); o.putalpha(a)
    buf = io.BytesIO()
    if fmt == 'JPEG': o.convert('RGB').save(buf, 'JPEG', quality=92)
    else: o.save(buf, 'PNG')
    print('image', fmt, im.size, int(m.sum()), 'px recoloured')
    return buf.getvalue()
# only the colour textures (not the normal or metal/roughness maps, whose green channel would be 'recoloured' too)
base = set()
for mt in j.get('materials', []):
    t = mt.get('pbrMetallicRoughness', {}).get('baseColorTexture')
    if t is not None: base.add(j['textures'][t['index']]['source'])
imgviews = {img['bufferView'] for k, img in enumerate(j.get('images', [])) if 'bufferView' in img and k in base}
views = j['bufferViews']; chunks = []; off = 0
for i, v in enumerate(views):
    data = bin_[v.get('byteOffset', 0):v.get('byteOffset', 0) + v['byteLength']]
    if i in imgviews: data = recolor(data)
    pad = (-off) % 4; chunks.append(b'\0' * pad); off += pad
    v['byteOffset'] = off; v['byteLength'] = len(data); chunks.append(data); off += len(data)
nb = b''.join(chunks); nb += b'\0' * ((-len(nb)) % 4)
j['buffers'][0]['byteLength'] = len(nb)
js = json.dumps(j, separators=(',', ':')).encode(); js += b' ' * ((-len(js)) % 4)
total = 12 + 8 + len(js) + 8 + len(nb)
o = struct.pack('<III', 0x46546C67, 2, total) + struct.pack('<II', len(js), 0x4E4F534A) + js + struct.pack('<II', len(nb), 0x004E4942) + nb
open(out, 'wb').write(o); print('out', out, total)
