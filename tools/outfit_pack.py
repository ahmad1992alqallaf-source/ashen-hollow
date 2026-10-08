# pack a .gltf (+ .bin + png textures) into one .glb, textures shrunk to max px
import json, struct, sys, os, io
from PIL import Image
src, out, maxpx = sys.argv[1], sys.argv[2], int(sys.argv[3])
d = json.load(open(src)); base = os.path.dirname(src)
binb = open(os.path.join(base, d['buffers'][0]['uri']), 'rb').read()
blob = bytearray(binb)
def pad(b):
    while len(b) % 4: b.append(0)
pad(blob)
for im in d.get('images', []):
    uri = im.pop('uri'); p = os.path.join(base, uri)
    img = Image.open(p); img.thumbnail((maxpx, maxpx), Image.LANCZOS)
    bio = io.BytesIO(); img.save(bio, 'PNG', optimize=True); data = bio.getvalue()
    off = len(blob); blob += data; pad(blob)
    d['bufferViews'].append({'buffer': 0, 'byteOffset': off, 'byteLength': len(data)})
    im['bufferView'] = len(d['bufferViews']) - 1; im['mimeType'] = 'image/png'
d['buffers'] = [{'byteLength': len(blob)}]
js = json.dumps(d, separators=(',', ':')).encode()
while len(js) % 4: js += b' '
glb = struct.pack('<III', 0x46546C67, 2, 12 + 8 + len(js) + 8 + len(blob)) + struct.pack('<II', len(js), 0x4E4F534A) + js + struct.pack('<II', len(blob), 0x004E4942) + bytes(blob)
open(out, 'wb').write(glb); print(out, len(glb) // 1024, 'KB')
