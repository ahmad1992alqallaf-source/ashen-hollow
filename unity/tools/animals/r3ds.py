# minimal .3ds reader: objects (verts, faces, uvs, per-face material), materials (name, diffuse colour, texture file)
import struct, sys, numpy as np
def read(path):
    d = open(path, 'rb').read(); objs = []; mats = {}
    def cstr(o):
        e = d.index(b'\0', o); return d[o:e].decode('latin1'), e + 1
    def walk(o, end, ctx):
        while o + 6 <= end:
            cid, ln = struct.unpack('<HI', d[o:o+6]); body = o + 6; nxt = o + ln
            if ln < 6: break
            if cid in (0x4D4D, 0x3D3D): walk(body, nxt, ctx)
            elif cid == 0x4000:
                name, b = cstr(body); ob = {'name': name, 'faces_mat': []}; objs.append(ob); walk(b, nxt, ob)
            elif cid == 0x4100: walk(body, nxt, ctx)
            elif cid == 0x4110:
                n = struct.unpack('<H', d[body:body+2])[0]; ctx['V'] = np.frombuffer(d[body+2:body+2+12*n], '<f4').reshape(-1, 3).copy()
            elif cid == 0x4140:
                n = struct.unpack('<H', d[body:body+2])[0]; ctx['UV'] = np.frombuffer(d[body+2:body+2+8*n], '<f4').reshape(-1, 2).copy()
            elif cid == 0x4120:
                n = struct.unpack('<H', d[body:body+2])[0]; F = np.frombuffer(d[body+2:body+2+8*n], '<u2').reshape(-1, 4)
                ctx['F'] = F[:, :3].astype(int).copy(); walk(body + 2 + 8*n, nxt, ctx)
            elif cid == 0x4130:
                name, b = cstr(body); n = struct.unpack('<H', d[b:b+2])[0]
                ctx['faces_mat'].append((name, np.frombuffer(d[b+2:b+2+2*n], '<u2').astype(int).copy()))
            elif cid == 0x4160:
                ctx['M'] = np.frombuffer(d[body:body+48], '<f4').reshape(4, 3).copy()
            elif cid == 0xAFFF:
                m = {}; walk(body, nxt, m); mats[m.get('name')] = m
            elif cid == 0xA000: ctx['name'] = cstr(body)[0]
            elif cid == 0xA020:
                m = {}; walk(body, nxt, m); ctx['diffuse'] = m.get('rgb')
            elif cid == 0x0011: ctx['rgb'] = list(d[body:body+3])
            elif cid == 0x0010: ctx['rgb'] = [int(255*x) for x in struct.unpack('<3f', d[body:body+12])]
            elif cid == 0xA200:
                m = {}; walk(body, nxt, m); ctx['tex'] = m.get('file')
            elif cid == 0xA300: ctx['file'] = cstr(body)[0]
            o = nxt
    walk(0, len(d), {})
    return objs, mats
if __name__ == '__main__':
    objs, mats = read(sys.argv[1])
    for o in objs:
        V = o.get('V'); print(o['name'], None if V is None else (len(V), len(o.get('F', [])), V.min(0).round(1), V.max(0).round(1)), 'uv' if 'UV' in o else '', [(n, len(f)) for n, f in o['faces_mat']])
    for k, m in mats.items(): print('MAT', k, m.get('diffuse'), m.get('tex'))
