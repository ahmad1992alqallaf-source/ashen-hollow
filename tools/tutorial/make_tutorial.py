# Ashen Hollow: Dawnrest Isle, the training island, made for the Unity version in the same form as the other areas
# (the web game drew it in code: TUTZ in index.html). Writes, into OUT:
#   tutorial.json             the area: bounds, spawn, the training toads, trunk circles, the hut
#   tutorial_ground.bytes     the ground colour map (PNG) the grass and the sea read
#   tutorial_world.glb        the ground picture, the sea plane, the hut, the pier, the rocks to mine, the axis marks
#   tutorial_world_tex/img0.png
#   treelook_tutorial.json    the trees (merged into Data/treelook.json)
# Web units -> metres: x 0.04 (the island's corner is web x 175000 -> 7000 m).
import json, math, struct, sys, os, random
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

S = 0.04; X0W = 175000; W_W, H_W = 2400, 1800
X0, Z0, WM, HM = X0W * S, 0.0, W_W * S, H_W * S          # 7000, 0, 96, 72
OUT = sys.argv[1] if len(sys.argv) > 1 else 'out'
WORLD_TREES = sys.argv[2] if len(sys.argv) > 2 else None
os.makedirs(os.path.join(OUT, 'tutorial_world_tex'), exist_ok=True)
rng = np.random.default_rng(1717); random.seed(1717)
def M(u, v): return (X0 + u * S, v * S)   # island web units (u, v) -> metres (x, z)

# ---------- the land: the web zoneFill ellipse with a little noise ----------
def vnoise(h, w, scale, seed):
    r = np.random.default_rng(seed); gh, gw = int(h / scale) + 3, int(w / scale) + 3
    g = (r.random((gh, gw)) * 255).astype(np.uint8)
    return np.asarray(Image.fromarray(g).resize((int(gw * scale), int(gh * scale)), Image.BICUBIC)).astype(np.float32)[:h, :w] / 255.0
PPM = 10                                     # picture pixels a metre (as the other areas: 1280 x 960 for 128 x 96 m)
PW, PH = int(WM * PPM), int(HM * PPM)        # 960 x 720
yy, xx = np.mgrid[0:PH, 0:PW].astype(np.float32)
u = (xx + 0.5) / PPM / S; v = (yy + 0.5) / PPM / S      # web units; picture row 0 = z 0 (as the other areas' pictures)
nz = vnoise(PH, PW, 60, 3) * 0.6 + vnoise(PH, PW, 20, 4) * 0.4
f = 1 - ((u - 1200) / 990) ** 2 - ((v - 900) / 700) ** 2 + (nz - 0.5) * 0.12
GRASS = np.array([63, 106, 42]); SAND = np.array([217, 199, 154]); SEA = np.array([63, 134, 168]); ROAD = np.array([150, 118, 72])
img = np.zeros((PH, PW, 3), np.float32)
land = f >= 0.22; sand = (f > 0) & ~land; sea = f <= 0
# grass: a few shades in patches, darker toward the sand
shade = 0.9 + 0.2 * vnoise(PH, PW, 30, 7) + 0.06 * (vnoise(PH, PW, 6, 8) - 0.5)
img[land] = (GRASS[None, :] * shade[land][:, None])
img[sand] = SAND * (0.96 + 0.08 * vnoise(PH, PW, 12, 9)[sand][:, None])
# the wet edge of the sand, a little darker, and the shallows lighter than the open sea
wet = sand & (f < 0.05); img[wet] = SAND * 0.86
deepk = np.clip(-f / 0.25, 0, 1)
img[sea] = (SEA * (1.12 - 0.12 * deepk[sea][:, None]))
pic = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8))
base = pic.copy()
d = ImageDraw.Draw(pic)
def P(uv): return ((uv[0]) * S * PPM, (uv[1]) * S * PPM)
# roads (web ZROADS.tutorial, 75 units wide)
roads = [[(420, 900), (900, 900), (1200, 700), (1650, 700), (1850, 900), (2380, 900)], [(900, 900), (1100, 1150), (1520, 1180), (1850, 900)], [(900, 900), (900, 560), (1200, 560)]]
for r in roads:
    pts = [P(p) for p in r]
    d.line(pts, fill=tuple(ROAD), width=int(75 * S * PPM), joint='curve')
    for p in pts: rr = 75 * S * PPM / 2; d.ellipse([p[0] - rr, p[1] - rr, p[0] + rr, p[1] + rr], fill=tuple(ROAD))
# the roads stop at the shore (the pier carries the last stretch out over the water)
pa = np.asarray(pic).copy(); pa[sea] = np.asarray(base)[sea]; pic = Image.fromarray(pa); d = ImageDraw.Draw(pic)
# the toad pen (a worn ring of earth) and the dodging ground (a red warning ring), as the web map drew them
c = P((1520, 1180)); d.ellipse([c[0] - 230 * S * PPM, c[1] - 150 * S * PPM, c[0] + 230 * S * PPM, c[1] + 150 * S * PPM], outline=(120, 82, 46), width=int(12 * S * PPM) + 2)
c = P((1660, 700)); rr = 200 * S * PPM; d.ellipse([c[0] - rr, c[1] - rr, c[0] + rr, c[1] + rr], outline=(170, 70, 55), width=int(10 * S * PPM) + 2)
# soften the hard edges a touch, then a fine grain over the land
pic = pic.filter(ImageFilter.GaussianBlur(0.8))
a = np.asarray(pic).astype(np.float32); grain = (rng.random((PH, PW)) - 0.5) * 10
a[~sea] += grain[~sea][:, None]
pic = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).convert('RGBA')
pic.save(os.path.join(OUT, 'tutorial_world_tex', 'img0.png'))

# ---------- the ground colour map: the land plus 26 m all round, z up (row 0 = the far side), black outside ----------
GX0, GZ0, GW, GH = X0 - 26, Z0 - 26, WM + 52, HM + 52
gw, gh = 421, int(round(421 * GH / GW))
gm = np.zeros((gh, gw, 4), np.uint8); gm[..., 3] = 255
src = np.asarray(pic)
for r in range(gh):
    z = GZ0 + GH - (r + 0.5) * GH / gh
    py = int((z - Z0) * PPM)
    if py < 0 or py >= PH: continue
    for cidx in range(gw):
        x = GX0 + (cidx + 0.5) * GW / gw; px = int((x - X0) * PPM)
        if 0 <= px < PW: gm[r, cidx] = src[py, px]
Image.fromarray(gm).save(os.path.join(OUT, 'tutorial_ground.bytes'), format='PNG')

# ---------- the trees: the web's trees on the island (world_trees.json), each with a height and a crown ----------
trees = []
if WORLD_TREES:
    for t in json.load(open(WORLD_TREES))['trees']:
        if X0W <= t[0] < X0W + W_W:
            x, z = t[0] * S, t[1] * S; h = 4.6 + rng.random() * 1.8; rad = max(1.4, t[2] * S * 2.1)
            col = [0.058 + rng.random() * 0.012, 0.28 + rng.random() * 0.04, 0.038 + rng.random() * 0.006]
            trees.append([round(x, 2), round(z, 2), 0.0, round(h, 2), round(rad, 2), 0 if t[3] != 'pine' else 1] + [round(c, 3) for c in col])
json.dump({'hide': [], 'trees': trees}, open(os.path.join(OUT, 'treelook_tutorial.json'), 'w'))

# ---------- the area ----------
mob = {'id': 'tutrat', 'name': 'Training toad', 'lvl': 1, 'hp': 22, 'dmg': 3, 'speed': round(70 * S, 2), 'radius': round(14 * S, 2), 'aggro': 0, 'xp': 20, 'flee': False, 'model': 'Mobs/bogtoad'}
mobs = [{'type': 'tutrat', 'x': round(M(a, b)[0], 2), 'z': round(M(a, b)[1], 2)} for a, b in [(1450, 1120), (1600, 1130), (1500, 1260), (1640, 1250), (1560, 1190)]]
circles = [{'x': t[0], 'z': t[1], 'r': 0.7} for t in trees]
HUT = (600, 1000, 140, 100)     # web addHouse: centre and size
hx, hz = M(HUT[0], HUT[1]); hw, hd = HUT[2] * S, HUT[3] * S
boxes = [{'x': round(hx - hw / 2, 2), 'z': round(hz - hd / 2, 2), 'w': round(hw, 2), 'h': round(hd, 2)}]
area = {
    'version': 2, 'id': 'tutorial', 'groundMap': {'x0': GX0, 'z0': GZ0, 'w': GW, 'h': GH}, 'region': 'Dawnrest Isle', 'levels': '1',
    'bounds': {'x0': X0, 'z0': Z0, 'x1': X0 + WM, 'z1': Z0 + HM}, 'spawn': {'x': round(M(420, 900)[0], 2), 'z': round(M(420, 900)[1], 2)},
    'playerSpeed': 6.4, 'mobTypes': [mob], 'mobs': mobs, 'circles': circles, 'boxes': boxes, 'water': [], 'exits': [], 'gates': [], 'dgates': [], 'walk': [], 'dayLength': 1440,
}
json.dump(area, open(os.path.join(OUT, 'tutorial.json'), 'w'), indent=1)

# ---------- the world model (glTF, metres, y up; the axis marks tell the game which way is which) ----------
nodes, meshes, mats, acc, views, blob = [], [], [], [], [], bytearray()
def pad():
    while len(blob) % 4: blob.append(0)
def add(arr, ctype, typ, target=None, mm=False):
    pad(); arr = np.ascontiguousarray(arr)
    views.append({'buffer': 0, 'byteOffset': len(blob), 'byteLength': arr.nbytes, **({'target': target} if target else {})}); blob.extend(arr.tobytes())
    a = {'bufferView': len(views) - 1, 'componentType': ctype, 'count': int(arr.shape[0]), 'type': typ}
    if mm: a['min'] = arr.min(0).tolist(); a['max'] = arr.max(0).tolist()
    acc.append(a); return len(acc) - 1
def mat(col, name=None, tex=None, rough=0.9):
    m = {'pbrMetallicRoughness': {'metallicFactor': 0, 'roughnessFactor': rough}}
    if tex is not None: m['pbrMetallicRoughness']['baseColorTexture'] = {'index': tex}
    else: m['pbrMetallicRoughness']['baseColorFactor'] = [*col, 1]
    if name: m['name'] = name
    mats.append(m); return len(mats) - 1
def mesh(P, N, I, m, UV=None):
    attrs = {'POSITION': add(np.array(P, 'f4'), 5126, 'VEC3', 34962, True), 'NORMAL': add(np.array(N, 'f4'), 5126, 'VEC3', 34962)}
    if UV is not None: attrs['TEXCOORD_0'] = add(np.array(UV, 'f4'), 5126, 'VEC2', 34962)
    meshes.append({'primitives': [{'attributes': attrs, 'indices': add(np.array(I, 'u2'), 5123, 'SCALAR', 34963), 'material': m, 'mode': 4}]}); return len(meshes) - 1
def node(name=None, m=None, matrix=None, t=None, children=None):
    n = {}
    if matrix: n['matrix'] = matrix
    if t: n['translation'] = list(t)
    if name: n['name'] = name
    if m is not None: n['mesh'] = m
    if children: n['children'] = children
    nodes.append(n); return len(nodes) - 1
def box(cx, cy, cz, sx, sy, sz):
    P, N, I = [], [], []
    for ax in range(3):
        for sgn in (-1, 1):
            n = [0, 0, 0]; n[ax] = sgn; a1, a2 = [(1, 2), (0, 2), (0, 1)][ax]
            base = len(P)
            for (s1, s2) in [(-1, -1), (1, -1), (1, 1), (-1, 1)]:
                p = [0, 0, 0]; p[ax] = sgn * 0.5; p[a1] = s1 * 0.5; p[a2] = s2 * 0.5
                P.append([cx + p[0] * sx, cy + p[1] * sy, cz + p[2] * sz]); N.append(n)
            tri = [0, 1, 2, 0, 2, 3]
            # keep the faces outward: flip the winding where the corner order runs the wrong way round
            e1 = np.subtract(P[base + 1], P[base]); e2 = np.subtract(P[base + 2], P[base])
            if np.dot(np.cross(e1, e2), n) < 0: tri = [0, 2, 1, 0, 3, 2]
            I += [base + k for k in tri]
    return P, N, I
def quad_xy(hx, hz):
    return [[-hx, hz, 0], [hx, hz, 0], [-hx, -hz, 0], [hx, -hz, 0]], [[0, 0, 1]] * 4, [0, 2, 1, 2, 3, 1], [[0, 1], [1, 1], [0, 0], [1, 0]]
FLAT = lambda tx, ty, tz: [1, 0, 0, 0, 0, 2.220446049250313e-16, -1, 0, 0, 1, 2.220446049250313e-16, 0, tx, ty, tz, 1]
kids = []
# the ground picture and the sea plane (as every area has them)
gmat = mat(None, 'AH_GROUND_MAT', tex=0, rough=1)
P, N, I, UV = quad_xy(WM / 2, HM / 2); kids.append(node('AH_GROUND_0', mesh(P, N, I, gmat, UV), FLAT(X0 + WM / 2, 0, Z0 + HM / 2)))
wmat = mat([0.028426, 0.158961, 0.300544], 'AH_WATER_MAT', rough=0.2)
P, N, I, UV = quad_xy(230, 200); kids.append(node('AH_WATER_0_K0_M0', mesh(P, N, I, wmat, UV), FLAT(X0 + WM / 2, 0.04, Z0 + HM / 2)))
# (Elara's cottage, the toad pen's fence and the pier are built by the game: AHTrainingIsland)
# the rocks to mine (world.json rocks on the island): a body and ore specks, as the area models have them (the game
# swaps in its own ore boulders)
rockm = mat([0.42, 0.4, 0.37]); oreC = {'copper': [0.72, 0.38, 0.18], 'tin': [0.78, 0.78, 0.74]}
for i, (t, a, b) in enumerate([('copper', 1200, 520), ('tin', 1320, 560), ('copper', 1240, 430)]):
    x, z = M(a, b)
    P, N, I = box(0, 0.45, 0, 1.6, 0.9, 1.4); body = node(None, mesh(P, N, I, rockm))
    om = mat(oreC[t]); sp = []
    for k in range(4):
        ang = k * 1.7; P, N, I = box(math.cos(ang) * 0.5, 0.75 + 0.1 * (k % 2), math.sin(ang) * 0.45, 0.22, 0.22, 0.22); sp.append(node(None, mesh(P, N, I, om)))
    specks = node(None, children=sp)
    kids.append(node('obj%d' % i, t=(x, 0, z), children=[body, specks]))
kids.append(node('AH_MARK_X', matrix=[1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 100, 0, 0, 1]))
kids.append(node('AH_MARK_Z', matrix=[1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 100, 1]))
root = node('AH_World', children=kids)
pad()
gl = {'asset': {'version': '2.0', 'generator': 'Ashen Hollow make_tutorial.py'}, 'scene': 0, 'scenes': [{'name': 'AuxScene', 'nodes': [root]}],
      'nodes': nodes, 'meshes': meshes, 'materials': mats, 'accessors': acc, 'bufferViews': views, 'buffers': [{'byteLength': len(blob)}],
      'images': [{'uri': 'tutorial_world_tex/img0.png', 'mimeType': 'image/png'}], 'textures': [{'sampler': 0, 'source': 0}],
      'samplers': [{'magFilter': 9729, 'minFilter': 9987, 'wrapS': 33071, 'wrapT': 33071}]}
js = json.dumps(gl, separators=(',', ':')).encode()
while len(js) % 4: js += b' '
out = struct.pack('<III', 0x46546C67, 2, 12 + 8 + len(js) + 8 + len(blob)) + struct.pack('<I4s', len(js), b'JSON') + js + struct.pack('<I4s', len(blob), b'BIN\0') + bytes(blob)
open(os.path.join(OUT, 'tutorial_world.glb'), 'wb').write(out)
print('trees', len(trees), 'glb', len(out) // 1024, 'KB', 'ground', gw, gh)
