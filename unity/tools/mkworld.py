# Ashen Hollow world map: an inked, watercoloured parchment map of the realm (made for the game, no outside art)
import numpy as np, json, math, random
from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageChops
W, H = 2048, 1440
rng = np.random.default_rng(7); random.seed(7)
OUT = '/tmp/wm/'
SERIF = '/usr/share/fonts/truetype/google-fonts/Lora-Variable.ttf'
def SF(size, w=b'Bold'):
    f = ImageFont.truetype(SERIF, size)
    try: f.set_variation_by_name(w)
    except Exception: pass
    return f
ITAL = '/usr/share/fonts/truetype/google-fonts/Lora-Italic-Variable.ttf'
BOLD = '/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf'

# ---------- noise ----------
def vnoise(shape, scale, seed):
    r = np.random.default_rng(seed); gh, gw = int(shape[0] / scale) + 3, int(shape[1] / scale) + 3
    g = r.random((gh, gw)).astype(np.float32)
    im = Image.fromarray((g * 255).astype(np.uint8)).resize((int(gw * scale), int(gh * scale)), Image.BICUBIC)
    a = np.asarray(im).astype(np.float32)[:shape[0], :shape[1]] / 255.0
    return a
def fbm(shape, base, oct, seed):
    t = np.zeros(shape, np.float32); amp = 1; tot = 0
    for o in range(oct):
        t += amp * vnoise(shape, base / (2 ** o), seed + o); tot += amp; amp *= 0.5
    return t / tot
Y, X = np.mgrid[0:H, 0:W].astype(np.float32)
U, V = X / W, Y / H

# ---------- the realm ----------
R = {  # id: (u, v, name, levels, colour)
 'meadow':  (0.235, 0.33, 'Hollow Meadow', '1–9', (150, 178, 96)),
 'silkwood':(0.43, 0.27, 'Silkwood', '8–12', (78, 118, 72)),
 'frost':   (0.635, 0.21, 'Frostfang Reach', '20–24', (214, 224, 228)),
 'fossil':  (0.835, 0.17, 'Fossil Lands', '40–60', (176, 156, 128)),
 'mire':    (0.215, 0.60, 'Duskmire', '6–16', (104, 116, 78)),
 'vale':    (0.43, 0.575, 'Kingsvale', '4–12', (188, 186, 108)),
 'sands':   (0.635, 0.57, 'Sunscar Wastes', '15–26', (226, 196, 132)),
 'ember':   (0.835, 0.52, 'Emberreach', '36–50', (132, 84, 70)),
}
SEED = {k: (v[0], v[1]) for k, v in R.items()}
# land: blobs around the regions, warped by noise
land = np.zeros((H, W), np.float32)
def blob(u, v, ru, rv, w=1.0):
    return w * np.exp(-(((U - u) / ru) ** 2 + ((V - v) / rv) ** 2))
for k, (u, v) in SEED.items(): land += blob(u, v, 0.15, 0.2)
land += blob(0.53, 0.42, 0.3, 0.25, 0.6)
warp_u = (fbm((H, W), 300, 5, 11) - 0.5) * 0.12; warp_v = (fbm((H, W), 300, 5, 12) - 0.5) * 0.12
def warped_blob(u, v, ru, rv, w=1.0):
    return w * np.exp(-(((U + warp_u - u) / ru) ** 2 + ((V + warp_v - v) / rv) ** 2))
land = np.zeros((H, W), np.float32)
for k, (u, v) in SEED.items(): land += warped_blob(u, v, 0.14, 0.19)
land += warped_blob(0.53, 0.40, 0.32, 0.26, 0.7)
land += (fbm((H, W), 120, 4, 3) - 0.5) * 0.35
mainland = land > 0.62
# islands: Tidewake (six), Dragonscale, Dawnrest
ISL = {'tide': [(0.58, 0.86, 0.035, 0.03), (0.645, 0.80, 0.03, 0.028), (0.66, 0.91, 0.04, 0.026), (0.73, 0.86, 0.055, 0.05), (0.75, 0.76, 0.035, 0.024), (0.61, 0.76, 0.03, 0.022)],
       'isle': [(0.075, 0.82, 0.055, 0.075)], 'tutorial': [(0.055, 0.22, 0.025, 0.03)]}
isl = np.zeros((H, W), bool); isl_lab = np.full((H, W), '', object)
nz = fbm((H, W), 60, 3, 21) - 0.5
for k, lst in ISL.items():
    for (u, v, ru, rv) in lst:
        m = (((U + warp_u * 0.3 - u) / ru) ** 2 + ((V + warp_v * 0.3 - v) / rv) ** 2) + nz * 0.9 < 1.0
        isl |= m; isl_lab[m] = k
landm = mainland | isl
# keep the mainland off the frame
edge = np.minimum(np.minimum(U, 1 - U), np.minimum(V, 1 - V))
landm &= edge > 0.045
# regions: nearest seed after a warp (crooked borders)
wu = (fbm((H, W), 160, 4, 31) - 0.5) * 0.09; wv = (fbm((H, W), 160, 4, 32) - 0.5) * 0.09
keys = list(SEED)
D = np.stack([((U + wu - SEED[k][0]) * 1.35) ** 2 + (V + wv - SEED[k][1]) ** 2 for k in keys], 0)
lab = np.argmin(D, 0)
REG = np.where(mainland & landm, lab, -1)
for i, k in enumerate(['tide', 'isle', 'tutorial']):
    REG[(isl_lab == k) & landm] = 8 + i
ALLK = keys + ['tide', 'isle', 'tutorial']
COL = {**{k: R[k][4] for k in keys}, 'tide': (214, 200, 150), 'isle': (120, 140, 80), 'tutorial': (170, 190, 110)}

# ---------- paint ----------
paper = np.zeros((H, W, 3), np.float32)
base = np.array([240, 224, 184], np.float32)
pn = fbm((H, W), 220, 5, 41)[..., None]
paper[:] = base * (0.9 + 0.16 * pn)
# stains
for s in range(10):
    u, v, r = rng.random(), rng.random(), 0.04 + rng.random() * 0.1
    paper *= 1 - 0.06 * np.exp(-(((U - u) / r) ** 2 + ((V - v) / r) ** 2))[..., None]
sea = np.array([120, 168, 190], np.float32)
img = paper.copy()
seamask = ~landm
seatone = paper * 0.42 + sea * 0.58 * (0.92 + 0.12 * fbm((H, W), 300, 3, 51)[..., None])
img[seamask] = seatone[seamask]
# land watercolour: region colour, mottled
mott = fbm((H, W), 90, 4, 61)[..., None]
for i, k in enumerate(ALLK):
    m = REG == i
    if not m.any(): continue
    c = np.array(COL[k], np.float32)
    tone = paper * 0.35 + c * 0.65 * (0.85 + 0.3 * mott)
    img[m] = tone[m]
# darker wash inside the coast (the watercolour pools at the edge)
lm = Image.fromarray((landm * 255).astype(np.uint8))
dist_in = np.asarray(lm.filter(ImageFilter.GaussianBlur(10))).astype(np.float32) / 255
rim = np.clip((1 - dist_in) * 2.2, 0, 1) * landm
img *= (1 - 0.25 * rim)[..., None]
# coast ripples in the sea
sm = Image.fromarray((landm * 255).astype(np.uint8))
for i, rad in enumerate([5, 13, 23, 36]):
    grow = np.asarray(sm.filter(ImageFilter.MaxFilter(rad * 2 + 1))) > 0
    ring = grow & ~np.asarray(sm.filter(ImageFilter.MaxFilter(max(1, rad * 2 - 1)))).astype(bool)
    img[ring & seamask] = img[ring & seamask] * (0.8 + 0.04 * i)
# the coastline itself in ink
edges = np.asarray(sm.filter(ImageFilter.FIND_EDGES)) > 0
thick = np.asarray(Image.fromarray((edges * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(3))) > 0
img[thick] = img[thick] * 0.25 + np.array([60, 44, 30]) * 0.75
# region borders: dotted ink lines
lab_im = REG.astype(np.int16)
bd = np.zeros((H, W), bool)
bd[:, 1:] |= (lab_im[:, 1:] != lab_im[:, :-1]) & (lab_im[:, 1:] >= 0) & (lab_im[:, :-1] >= 0)
bd[1:, :] |= (lab_im[1:, :] != lab_im[:-1, :]) & (lab_im[1:, :] >= 0) & (lab_im[:-1, :] >= 0)
bdt = np.asarray(Image.fromarray((bd * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(5))) > 0
dash = ((X + Y) % 22) < 12
bline = bdt & dash & landm
img[bline] = img[bline] * 0.3 + np.array([120, 40, 30]) * 0.7
base_img = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8))
d = ImageDraw.Draw(base_img)
INK = (58, 42, 30)

def inland(u, v, k=None):
    x, y = int(u * W), int(v * H)
    if not (0 <= x < W and 0 <= y < H): return False
    if not landm[y, x]: return False
    if k is not None and REG[y, x] != ALLK.index(k): return False
    return True
# ---------- icons ----------
def mountain(x, y, s, snow=False, col=(150, 140, 125)):
    pts = [(x - s, y), (x - s * 0.15, y - s * 1.25), (x + s * 0.2, y - s * 1.05), (x + s, y)]
    d.polygon([(x - s, y), (x - s * 0.15, y - s * 1.25), (x + s, y)], fill=col)
    d.polygon([(x - s * 0.15, y - s * 1.25), (x + s, y), (x + s * 0.1, y)], fill=tuple(int(c * 0.72) for c in col))
    if snow: d.polygon([(x - s * 0.15, y - s * 1.25), (x - s * 0.42, y - s * 0.78), (x - s * 0.15, y - s * 0.86), (x + s * 0.12, y - s * 0.72), (x + s * 0.3, y - s * 0.86)], fill=(250, 250, 248))
    d.line([(x - s, y), (x - s * 0.15, y - s * 1.25), (x + s, y)], fill=INK, width=2)
    for i in range(3): d.line([(x + s * (0.1 + i * 0.2), y - s * (0.95 - i * 0.28)), (x + s * (0.05 + i * 0.22), y - s * 0.1)], fill=INK, width=1)
def pine(x, y, s, col=(54, 90, 56)):
    d.line([(x, y), (x, y - s * 0.3)], fill=INK, width=2)
    for i in range(3):
        w = s * (0.55 - i * 0.14); yy = y - s * (0.25 + i * 0.32)
        d.polygon([(x - w, yy), (x, yy - s * 0.45), (x + w, yy)], fill=col, outline=INK)
def tree(x, y, s, col=(96, 140, 70)):
    d.line([(x, y), (x, y - s * 0.5)], fill=INK, width=2)
    d.ellipse([x - s * 0.45, y - s * 1.15, x + s * 0.45, y - s * 0.35], fill=col, outline=INK, width=2)
    d.arc([x - s * 0.3, y - s * 1.0, x + s * 0.2, y - s * 0.55], 200, 300, fill=tuple(min(255, c + 40) for c in col), width=2)
def dune(x, y, s):
    d.arc([x - s, y - s * 0.6, x + s, y + s * 0.6], 200, 340, fill=(150, 112, 60), width=3)
    d.arc([x - s * 0.6, y - s * 0.3, x + s * 1.2, y + s * 0.7], 205, 320, fill=(176, 134, 74), width=2)
def reeds(x, y, s):
    for i in range(-2, 3):
        d.line([(x + i * s * 0.18, y), (x + i * s * 0.3, y - s * (0.7 + 0.1 * abs(i)))], fill=(60, 74, 40), width=2)
    d.ellipse([x - s * 0.8, y - s * 0.1, x + s * 0.8, y + s * 0.25], outline=(70, 90, 80), width=2)
def field(x, y, s):
    for i in range(3):
        c = [(200, 180, 90), (170, 160, 80), (210, 196, 120)][i]
        d.polygon([(x - s + i * s * 0.6, y), (x - s * 0.6 + i * s * 0.6, y - s * 0.5), (x - s * 0.1 + i * s * 0.6, y - s * 0.5), (x - s * 0.5 + i * s * 0.6, y)], fill=c, outline=(120, 100, 50))
def bones(x, y, s):
    d.arc([x - s, y - s * 0.8, x + s, y + s * 0.8], 200, 340, fill=(240, 232, 210), width=4)
    for i in range(4):
        xx = x - s * 0.6 + i * s * 0.4
        d.line([(xx, y - s * 0.55), (xx + s * 0.1, y + s * 0.2)], fill=(236, 228, 206), width=3)
    d.line([(x - s, y + s * 0.1), (x + s, y + s * 0.1)], fill=INK, width=1)
def volcano(x, y, s):
    d.polygon([(x - s * 1.4, y), (x - s * 0.35, y - s * 1.3), (x + s * 0.35, y - s * 1.3), (x + s * 1.4, y)], fill=(90, 70, 62), outline=INK)
    d.polygon([(x - s * 0.35, y - s * 1.3), (x - s * 0.1, y - s * 0.6), (x + s * 0.05, y - s * 0.95), (x + s * 0.2, y - s * 0.4), (x + s * 0.35, y - s * 1.3)], fill=(232, 96, 40))
    for i in range(4): d.ellipse([x - s * 0.3 + i * s * 0.18, y - s * (1.6 + i * 0.25), x + s * 0.1 + i * s * 0.22, y - s * (1.35 + i * 0.25)], fill=(110, 104, 100), outline=None)
def castle(x, y, s, col=(222, 210, 180)):
    d.rectangle([x - s, y - s * 0.8, x + s, y], fill=col, outline=INK, width=2)
    for i in (-1, 0, 1):
        tx = x + i * s * 0.75
        d.rectangle([tx - s * 0.28, y - s * 1.45, tx + s * 0.28, y - s * 0.6], fill=col, outline=INK, width=2)
        d.polygon([(tx - s * 0.36, y - s * 1.45), (tx, y - s * 1.95), (tx + s * 0.36, y - s * 1.45)], fill=(170, 60, 50), outline=INK)
    d.rectangle([x - s * 0.22, y - s * 0.45, x + s * 0.22, y], fill=(80, 56, 40))
def town(x, y, s, roof=(160, 70, 50)):
    for i, (dx, h) in enumerate([(-0.7, 0.7), (0, 1.0), (0.7, 0.75)]):
        xx = x + dx * s
        d.rectangle([xx - s * 0.35, y - h * s, xx + s * 0.35, y], fill=(232, 220, 196), outline=INK, width=2)
        d.polygon([(xx - s * 0.45, y - h * s), (xx, y - (h + 0.5) * s), (xx + s * 0.45, y - h * s)], fill=roof, outline=INK)
def scatter(k, n, fn, s, tries=4000, margin=0.0, avoid=None):
    placed = []; t = 0
    while len(placed) < n and t < tries:
        t += 1; u, v = rng.random(), rng.random()
        if not inland(u, v, k): continue
        if avoid and any((u - a) ** 2 + ((v - b) * H / W) ** 2 < avoid ** 2 for a, b in placed + LABELS): continue
        placed.append((u, v))
    placed.sort(key=lambda p: p[1])
    for (u, v) in placed: fn(u * W, v * H, s * (0.8 + 0.4 * rng.random()))
LABELS = [(R[k][0], R[k][1]) for k in keys]
scatter('frost', 34, lambda x, y, s: mountain(x, y, s, True, (196, 200, 204)), 26, avoid=0.022)
scatter('fossil', 14, lambda x, y, s: mountain(x, y, s, False, (150, 128, 104)), 22, avoid=0.03)
scatter('fossil', 8, bones, 16, avoid=0.04)
scatter('silkwood', 70, pine, 22, avoid=0.012)
scatter('meadow', 26, tree, 20, avoid=0.022)
scatter('mire', 30, reeds, 18, avoid=0.02)
scatter('vale', 16, field, 26, avoid=0.035)
scatter('vale', 10, tree, 18, avoid=0.03)
scatter('sands', 28, dune, 24, avoid=0.025)
scatter('ember', 12, lambda x, y, s: mountain(x, y, s, False, (92, 72, 64)), 24, avoid=0.035)
scatter('isle', 8, lambda x, y, s: mountain(x, y, s, False, (110, 120, 80)), 18, avoid=0.03)
scatter('tide', 10, lambda x, y, s: tree(x, y, s, (80, 150, 90)), 14, avoid=0.025)
volcano(0.86 * W, 0.44 * H, 46)
volcano(0.085 * W, 0.80 * H, 28)

# ---------- roads (the long roads between the lands) ----------
CONN = [('mill', 'meadow', 'silkwood', 'Old Mill Road'), ('whitepine', 'silkwood', 'frost', 'Whitepine Pass'), ('fenwick', 'meadow', 'mire', 'Fenwick Trail'),
        ('kingsroad', 'silkwood', 'vale', 'The King’s Road'), ('causeway', 'mire', 'vale', 'Drowned Causeway'), ('scorchwind', 'vale', 'sands', 'Scorchwind Canyon'), ('ashfall', 'sands', 'ember', 'Ashfall Pass')]
ROADS = {}
fi = ImageFont.truetype(ITAL, 22)
for cid, a, b, nm in CONN:
    (u0, v0), (u1, v1) = SEED[a], SEED[b]
    pts = []
    mx, my = (u0 + u1) / 2, (v0 + v1) / 2; nx, ny = -(v1 - v0), (u1 - u0); off = 0.03 * (1 if hash(cid) % 2 else -1)
    for t in np.linspace(0.12, 0.88, 60):
        q = (1 - t) ** 2; r2 = 2 * (1 - t) * t; s2 = t * t
        pts.append(((q * u0 + r2 * (mx + nx * off) + s2 * u1) * W, (q * v0 + r2 * (my + ny * off) + s2 * v1) * H))
    for i in range(0, len(pts) - 1, 2): d.line([pts[i], pts[i + 1]], fill=(120, 74, 40), width=4)
    mid = pts[len(pts) // 2]; ROADS[cid] = (mid[0] / W, mid[1] / H)
    tw = d.textlength(nm, font=fi); d.text((mid[0] - tw / 2, mid[1] + 8), nm, font=fi, fill=(96, 60, 36))
# sea lanes to the islands
for (u0, v0), (u1, v1) in [((0.43, 0.70), (0.68, 0.83)), ((0.2, 0.7), (0.09, 0.78)), ((0.2, 0.3), (0.07, 0.23))]:
    for t in np.linspace(0, 1, 30)[::2]:
        x = (u0 + (u1 - u0) * t) * W; y = (v0 + (v1 - v0) * t) * H + math.sin(t * 9) * 8
        d.line([(x, y), (x + 9, y + 2)], fill=(70, 90, 110), width=2)

# ---------- cities ----------
CITY = {'city': (0.455, 0.63, 'Varrow', 'castle'), 'hc_city': (0.655, 0.135, 'Highcairn', 'town'), 'mw_city': (0.165, 0.665, 'Mirewatch', 'town'),
        'ss_city': (0.675, 0.625, 'Sunspire Oasis', 'town'), 'ch_city': (0.875, 0.585, 'Cinderhold', 'town'), 'co_city': (0.735, 0.86, 'Coralport', 'town'),
        'homestead': (0.295, 0.43, 'Your Homestead', 'home')}
fc = SF(26)
for cid, (u, v, nm, kind) in CITY.items():
    x, y = u * W, v * H
    if kind == 'castle': castle(x, y, 22)
    elif kind == 'home':
        d.rectangle([x - 12, y - 14, x + 12, y], fill=(232, 220, 196), outline=INK, width=2); d.polygon([(x - 16, y - 14), (x, y - 28), (x + 16, y - 14)], fill=(120, 80, 50), outline=INK)
    else: town(x, y, 15, {'hc_city': (70, 80, 100), 'mw_city': (70, 100, 70), 'ss_city': (200, 150, 60), 'ch_city': (150, 50, 40), 'co_city': (60, 120, 150)}[cid])
    tw = d.textlength(nm, font=fc)
    for ox, oy in ((-1, 0), (1, 0), (0, -1), (0, 1), (-2, 0), (2, 0)): d.text((x - tw / 2 + ox, y + 6 + oy), nm, font=fc, fill=(244, 234, 206))
    d.text((x - tw / 2, y + 6), nm, font=fc, fill=(40, 28, 20))

# ---------- region names ----------
def label(text, u, v, size, col=(52, 36, 26), spacing=4, sub=None):
    f = SF(size); x0 = u * W; y0 = v * H
    widths = [d.textlength(ch, font=f) for ch in text]; tw = sum(widths) + spacing * (len(text) - 1); x = x0 - tw / 2
    for ch, wch in zip(text, widths):
        for ox, oy in ((-2, 0), (2, 0), (0, -2), (0, 2)): d.text((x + ox, y0 - size / 2 + oy), ch, font=f, fill=(246, 238, 214))
        d.text((x, y0 - size / 2), ch, font=f, fill=col); x += wch + spacing
    if sub:
        fs = ImageFont.truetype(ITAL, 22); sw = d.textlength(sub, font=fs)
        d.text((x0 - sw / 2, y0 + size / 2 + 2), sub, font=fs, fill=(90, 60, 40))
for k in keys:
    u, v, nm, lv, _ = R[k]
    label(nm.upper(), u, v, 40, sub='Level ' + lv)
label('TIDEWAKE ISLES', 0.66, 0.725, 30, sub='Level 30–45'); label('DRAGONSCALE ISLE', 0.09, 0.905, 24, sub='Level 28–40'); label('DAWNREST', 0.07, 0.16, 22)
label('THE SUNDERED SEA', 0.44, 0.86, 34, col=(60, 84, 100), spacing=10); label('THE GREY DEEP', 0.885, 0.73, 26, col=(60, 84, 100), spacing=6)
label('NORTHERN WASTES', 0.43, 0.06, 26, col=(60, 84, 100), spacing=8)

# ---------- compass rose ----------
cx, cy, cr = 0.92 * W, 0.88 * H, 70
for a in range(0, 360, 45):
    big = a % 90 == 0; L = cr if big else cr * 0.6; ra = math.radians(a - 90)
    tip = (cx + math.cos(ra) * L, cy + math.sin(ra) * L)
    l1 = (cx + math.cos(ra + 0.35) * cr * 0.18, cy + math.sin(ra + 0.35) * cr * 0.18); l2 = (cx + math.cos(ra - 0.35) * cr * 0.18, cy + math.sin(ra - 0.35) * cr * 0.18)
    d.polygon([tip, l1, (cx, cy)], fill=(150, 40, 30) if a == 0 else (60, 44, 30)); d.polygon([tip, l2, (cx, cy)], fill=(236, 222, 190), outline=INK)
d.ellipse([cx - cr * 0.75, cy - cr * 0.75, cx + cr * 0.75, cy + cr * 0.75], outline=INK, width=2)
d.text((cx - 9, cy - cr - 34), 'N', font=SF(30), fill=(120, 30, 24))

# ---------- title cartouche ----------
tx, ty = 0.33 * W, 0.94 * H
d.rounded_rectangle([tx - 300, ty - 44, tx + 300, ty + 44], 18, fill=(240, 228, 196), outline=INK, width=3)
d.rounded_rectangle([tx - 292, ty - 36, tx + 292, ty + 36], 14, outline=(140, 100, 60), width=2)
f = SF(38); t = 'The Realm of Ashen Hollow'; tw = d.textlength(t, font=f); d.text((tx - tw / 2, ty - 24), t, font=f, fill=(60, 30, 20))

# ---------- frame ----------
for i, (pad, wd, c) in enumerate([(10, 6, (70, 48, 30)), (22, 2, (120, 84, 50)), (30, 2, (70, 48, 30))]):
    d.rectangle([pad, pad, W - pad, H - pad], outline=c, width=wd)
for (x, y) in [(30, 30), (W - 30, 30), (30, H - 30), (W - 30, H - 30)]:
    d.ellipse([x - 16, y - 16, x + 16, y + 16], fill=(236, 222, 190), outline=(70, 48, 30), width=3)
    d.ellipse([x - 7, y - 7, x + 7, y + 7], fill=(150, 40, 30))
# a light vignette and paper grain over everything
arr = np.asarray(base_img).astype(np.float32)
vig = 1 - 0.28 * np.clip(((U - 0.5) ** 2 + (V - 0.5) ** 2) * 2.4, 0, 1) ** 1.5
grain = 0.96 + 0.08 * vnoise((H, W), 2, 99)
arr = arr * (vig * grain)[..., None]
final = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8))
final.save(OUT + 'worldmap.png'); final.convert('RGB').save(OUT + 'worldmap.jpg', quality=90)
# where things are on it (0..1 across, 0..1 down)
pos = {k: [R[k][0], R[k][1]] for k in keys}
pos.update({'tide': [0.66, 0.84], 'isle': [0.08, 0.80], 'tutorial': [0.055, 0.22]})
for cid, (u, v, nm, kind) in CITY.items(): pos[cid] = [u, v - 0.02]
for cid, (u, v) in ROADS.items(): pos[cid] = [u, v]
json.dump(pos, open(OUT + 'worldmap.json', 'w'), indent=1)
final.resize((1024, 720)).save(OUT + 'worldmap_s.png')
print('ok')
