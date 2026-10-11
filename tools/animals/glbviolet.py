# turn the fire glow in a glb's textures violet (Abyssal Realm monsters): orange and red pixels that are bright and strongly
# coloured get the hue of the void, everything else is kept; meshes, skins and clips are copied byte for byte
import json, struct, sys, io
from PIL import Image
import numpy as np
src, out = sys.argv[1], sys.argv[2]
MS = int(sys.argv[3]) if len(sys.argv) > 3 else 110   # least saturation that counts as glow (raise it to spare tan skin)
N = 1 << 30
b = open(src, 'rb').read(); n = struct.unpack('<I', b[12:16])[0]; J = json.loads(b[20:20 + n])
o = 20 + n; bl = struct.unpack('<I', b[o:o + 4])[0]; B = b[o + 8:o + 8 + bl]
img_views = {}
for im in J.get('images', []):
    if 'bufferView' in im: img_views[im['bufferView']] = im
newB = bytearray(); changed = 0
for i, bv in enumerate(J['bufferViews']):
    s = bv.get('byteOffset', 0); data = B[s:s + bv['byteLength']]
    if i in img_views:
        im = img_views[i]
        try:
            pic = Image.open(io.BytesIO(data)); pic.load()
            if True:
                a = pic.mode == 'RGBA'; alpha = pic.getchannel('A') if a else None
                hsv = np.array(pic.convert('RGB').convert('HSV')).astype(np.float32)
                h, s_, v = hsv[..., 0], hsv[..., 1], hsv[..., 2]
                # warm hues (red through yellow), clearly coloured: these are the cracks and the glow
                warm = ((h < 40) | (h > 245)) & (s_ > MS) & (v > 70)
                k = np.clip((s_ - MS) / 60, 0, 1) * np.clip((v - 70) / 50, 0, 1) * warm
                h2 = 196.0  # violet on PIL's 0-255 hue wheel
                hsv[..., 0] = np.where(k > 0.5, h2, h)
                hsv[..., 2] = np.where(k > 0.5, np.minimum(255, v * 1.1), v)
                pic = Image.fromarray(hsv.astype(np.uint8), 'HSV').convert('RGB')
                if a: pic.putalpha(alpha)
                buf = io.BytesIO()
                if pic.mode in ('RGBA', 'LA') or im.get('mimeType') == 'image/png':
                    pic.save(buf, 'PNG', optimize=True); im['mimeType'] = 'image/png'
                else:
                    pic.convert('RGB').save(buf, 'JPEG', quality=88); im['mimeType'] = 'image/jpeg'
                data = buf.getvalue(); changed += 1
        except Exception as e:
            print('skip image', i, e)
    while len(newB) % 4: newB.append(0)
    bv['byteOffset'] = len(newB); bv['byteLength'] = len(data); newB.extend(data)
while len(newB) % 4: newB.append(0)
J['buffers'][0]['byteLength'] = len(newB)
js = json.dumps(J, separators=(',', ':')).encode()
while len(js) % 4: js += b' '
outb = struct.pack('<III', 0x46546C67, 2, 12 + 8 + len(js) + 8 + len(newB)) + struct.pack('<II', len(js), 0x4E4F534A) + js + struct.pack('<II', len(newB), 0x004E4942) + bytes(newB)
open(out, 'wb').write(outb); print('textures recoloured', changed, len(b), '->', len(outb))
