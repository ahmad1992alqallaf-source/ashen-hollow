# mark a rigged suit as a robe (an empty node named AH_Robe at the scene root) when its robe closure is high
import sys, json, struct
sys.path.insert(0, '/tmp/claude-0/runchk')
f, val = sys.argv[1], float(open(sys.argv[2]).read())
b = open(f, 'rb').read(); n = struct.unpack('<I', b[12:16])[0]; J = json.loads(b[20:20 + n]); rest = b[20 + n:]
J['nodes'] = [x for x in J['nodes']]
had = any(x.get('name') == 'AH_Robe' for x in J['nodes'])
if val >= 0.55 and not had:
    J['nodes'].append({'name': 'AH_Robe'}); J['scenes'][J.get('scene', 0)]['nodes'].append(len(J['nodes']) - 1)
    js = json.dumps(J, separators=(',', ':')).encode()
    while len(js) % 4: js += b' '
    out = struct.pack('<III', 0x46546C67, 2, 12 + 8 + len(js) + len(rest)) + struct.pack('<II', len(js), 0x4E4F534A) + js + rest
    open(f, 'wb').write(out); print('robe marked', val)
else: print('robe', val, 'marked already' if had else 'not a robe')
