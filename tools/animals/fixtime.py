# every clip starts at time 0 (the exporter left them a frame late, which hitches a looping walk)
import json, struct, sys
f = sys.argv[1]; b = bytearray(open(f, 'rb').read())
n = struct.unpack('<I', b[12:16])[0]; J = json.loads(b[20:20 + n]); bin0 = 20 + n + 8
done = set()
for an in J.get('animations', []):
    ins = sorted(set(an['samplers'][c['sampler']]['input'] for c in an['channels']))
    m = min(J['accessors'][i]['min'][0] for i in ins)
    if m <= 1e-6: continue
    for i in ins:
        if i in done: continue
        a = J['accessors'][i]; bv = J['bufferViews'][a['bufferView']]
        o = bin0 + bv.get('byteOffset', 0) + a.get('byteOffset', 0)
        for k in range(a['count']):
            v = struct.unpack_from('<f', b, o + 4 * k)[0]; struct.pack_into('<f', b, o + 4 * k, v - m)
        a['min'] = [a['min'][0] - m]; a['max'] = [a['max'][0] - m]; done.add(i)
js = json.dumps(J, separators=(',', ':')).encode()
while len(js) % 4: js += b' '
rest = bytes(b[20 + n:])
out = struct.pack('<III', 0x46546C67, 2, 12 + 8 + len(js) + len(rest)) + struct.pack('<II', len(js), 0x4E4F534A) + js + rest
open(f, 'wb').write(out); print('times fixed', len(done))
