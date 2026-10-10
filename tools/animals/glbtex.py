# shrink every texture inside a glb to at most N pixels (phones): each embedded image is decoded, resized and stored
# again in place; meshes, skins and clips are copied byte for byte
import json, struct, sys, io
from PIL import Image
src, out, N = sys.argv[1], sys.argv[2], int(sys.argv[3])
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
            if max(pic.size) > N:
                k = N / max(pic.size); pic = pic.resize((max(1, int(pic.size[0] * k)), max(1, int(pic.size[1] * k))), Image.LANCZOS)
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
open(out, 'wb').write(outb); print('textures shrunk', changed, len(b), '->', len(outb))
