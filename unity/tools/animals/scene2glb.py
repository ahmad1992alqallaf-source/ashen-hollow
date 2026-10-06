import sys, json, struct, io, numpy as np
sys.path.insert(0, '/tmp/rig3'); from fbxscene import load
from PIL import Image
TEX = '/tmp/uap/pack/Realistic Animated Pack/textures/'
def write(parts, out, center=True):
    allP = np.vstack([p['P'] for p in parts]); lo, hi = allP.min(0), allP.max(0)
    off = np.array([-(lo[0]+hi[0])/2, -lo[1], -(lo[2]+hi[2])/2]) if center else np.zeros(3)
    bin_ = bytearray(); bvs = []; accs = []; mats = []; texs = []; imgs = []; prims = []
    def add(a, typ, ct, tgt=None, mm=False):
        nonlocal bin_
        while len(bin_) % 4: bin_.append(0)
        o = len(bin_); bin_ += a.tobytes(); bv = {'buffer': 0, 'byteOffset': o, 'byteLength': a.nbytes}
        if tgt: bv['target'] = tgt
        bvs.append(bv); ac = {'bufferView': len(bvs)-1, 'componentType': ct, 'count': len(a), 'type': typ}
        if mm: ac['min'] = a.min(0).tolist(); ac['max'] = a.max(0).tolist()
        accs.append(ac); return len(accs)-1
    for p in parts:
        P = ((p['P'] + off)/100.0).astype(np.float32)
        att = {'POSITION': add(P, 'VEC3', 5126, 34962, True), 'TEXCOORD_0': add(p['UV'], 'VEC2', 5126, 34962)}
        m = {'pbrMetallicRoughness': {'baseColorFactor': [1,1,1,1], 'metallicFactor': 0, 'roughnessFactor': 0.8}, 'doubleSided': True}
        if p['tex']:
            im = Image.open(TEX + p['tex']); mode = 'RGBA' if im.mode in ('RGBA', 'LA', 'P') else 'RGB'
            im = im.convert(mode); im.thumbnail((1024, 1024)); b = io.BytesIO()
            if mode == 'RGBA' and np.asarray(im)[..., 3].min() < 250: im.save(b, 'PNG'); mime = 'image/png'; m['alphaMode'] = 'MASK'
            else: im.convert('RGB').save(b, 'JPEG', quality=85); mime = 'image/jpeg'
            bb = b.getvalue()
            while len(bin_) % 4: bin_.append(0)
            o = len(bin_); bin_ += bb; bvs.append({'buffer': 0, 'byteOffset': o, 'byteLength': len(bb)})
            imgs.append({'bufferView': len(bvs)-1, 'mimeType': mime}); texs.append({'source': len(imgs)-1})
            m['pbrMetallicRoughness']['baseColorTexture'] = {'index': len(texs)-1}
        mats.append(m)
        prims.append({'attributes': att, 'indices': add(p['I'].astype(np.uint32), 'SCALAR', 5125, 34963), 'material': len(mats)-1})
    j = {'asset': {'version': '2.0'}, 'scene': 0, 'scenes': [{'nodes': [0]}], 'nodes': [{'mesh': 0}], 'meshes': [{'primitives': prims}],
         'materials': mats, 'buffers': [{'byteLength': len(bin_)}], 'bufferViews': bvs, 'accessors': accs}
    if imgs: j['images'] = imgs; j['textures'] = texs
    js = json.dumps(j).encode(); js += b' ' * ((4 - len(js) % 4) % 4)
    while len(bin_) % 4: bin_.append(0)
    with open(out, 'wb') as f:
        f.write(struct.pack('<III', 0x46546C67, 2, 12+8+len(js)+8+len(bin_))); f.write(struct.pack('<II', len(js), 0x4E4F534A)); f.write(js)
        f.write(struct.pack('<II', len(bin_), 0x004E4942)); f.write(bin_)
if __name__ == '__main__':
    M = load('/tmp/uap/pack/Realistic Animated Pack/Realistic Animated Pack.fbx')
    groups = {}
    for m in M:
        if m['name'] in ('-50%', '50'): continue
        key = m['name'].split('_applied')[0].split('_clean')[0].split('_M_')[0] if not m['name'].startswith('sm_') else m['name']
        groups.setdefault(key, []).append(m)
    # sm_ pieces: join those that overlap into one animal
    sm = [k for k in groups if k.startswith('sm_')]
    def box(ps): A = np.vstack([p['P'] for p in ps]); return A.min(0), A.max(0)
    merged = []
    for k in sm:
        lo, hi = box(groups[k]); placed = False
        for g in merged:
            glo, ghi = box(g['parts'])
            if np.all(lo < ghi + 5) and np.all(hi > glo - 5): g['parts'] += groups[k]; g['names'].append(k); placed = True; break
        if not placed: merged.append({'parts': list(groups[k]), 'names': [k]})
        del groups[k]
    for i, g in enumerate(merged): groups['extra%d' % i] = g['parts']; print('extra%d' % i, g['names'])
    for k, ps in groups.items():
        fn = '/tmp/uap/glb/' + k.replace(' ', '_').replace('.', '_') + '.glb'; write(ps, fn); print(k, fn)
