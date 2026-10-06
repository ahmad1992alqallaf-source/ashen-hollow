# Ashen Hollow: the talent tree's painted art (trunk, left branch, right branch) and the talent glyphs.
# usage: python3 build.py <out dir: Assets/AshenHollow/Resources/AH/UI>   (game-icons clone at /tmp/gi, see tools/icons)
import sys, os, math, random, json, numpy as np
from PIL import Image, ImageDraw, ImageFilter
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '../icons'))
OUT = sys.argv[1]
W, H, SS = 560, 400, 4          # the tree box in UI units, drawn 4x and shrunk to 2x
def P(x, y): return (x * SS, (H - y) * SS)
def bez(p0, p1, p2, p3, t):
    u = 1 - t; return tuple(u*u*u*a + 3*u*u*t*b + 3*u*t*t*c + t*t*t*d for a, b, c, d in zip(p0, p1, p2, p3))
BARK_D, BARK, BARK_L = (34, 22, 14, 255), (96, 64, 40, 255), (150, 108, 70, 255)
def limb(dr, pts, w0, w1, n=90):
    seq = [bez(*pts, i / n) for i in range(n + 1)]
    for layer, col, grow, off in [(0, BARK_D, 3.2, 0), (1, BARK, 0, 0), (2, BARK_L, -0.62, -0.3)]:
        for i, (x, y) in enumerate(seq):
            w = (w0 + (w1 - w0) * i / n) / 2
            r = (w + grow) if layer < 2 else w * 0.32
            if r <= 0: continue
            cx, cy = P(x + off * w, y)
            dr.ellipse([cx - r*SS, cy - r*SS, cx + r*SS, cy + r*SS], fill=col)
    # bark grooves
    rnd = random.Random(int(w0 * 100))
    for g in range(int(w0 / 5)):
        o = rnd.uniform(-0.35, 0.35); a = rnd.uniform(0.05, 0.4); b = min(1, a + rnd.uniform(0.2, 0.5))
        line = []
        for i in range(int(a * n), int(b * n)):
            x, y = seq[i]; w = (w0 + (w1 - w0) * i / n) / 2; line.append(P(x + o * w, y))
        if len(line) > 1: dr.line(line, fill=(52, 34, 22, 200), width=int(1.6 * SS))
def leaves(dr, centres, seed, front):
    rnd = random.Random(seed)
    for (x, y, spread, count) in centres:
        for i in range(count):
            a = rnd.uniform(0, 6.283); d = spread * math.sqrt(rnd.random())
            lx, ly = x + math.cos(a) * d, y + math.sin(a) * d * 0.75
            r = rnd.uniform(5, 11) if front else rnd.uniform(9, 19)
            g = rnd.uniform(0.75, 1.15)
            base = (52, 104, 48) if not front else (96, 160, 70)
            col = tuple(int(min(255, c * g)) for c in base) + (235 if not front else 245,)
            cx, cy = P(lx, ly); rx, ry = r * SS, r * SS * rnd.uniform(0.6, 0.85)
            dr.ellipse([cx - rx, cy - ry, cx + rx, cy + ry], fill=col)
            if front and i % 3 == 0: dr.ellipse([cx - rx * 0.5, cy - ry * 0.7, cx + rx * 0.05, cy - ry * 0.2], fill=(150, 205, 110, 150))
def layer(): im = Image.new('RGBA', (W * SS, H * SS), (0, 0, 0, 0)); return im, ImageDraw.Draw(im)
def save(im, name):
    im = im.resize((W * 2, H * 2), Image.LANCZOS)
    a = np.asarray(im).astype(np.float32); sh = np.zeros_like(a); sh[..., 3] = 0
    al = Image.fromarray(a[..., 3].astype('uint8')).filter(ImageFilter.GaussianBlur(5)); s = np.asarray(al).astype(np.float32) * 0.45
    out = np.zeros_like(a); out[6:, 4:, 3] = s[:-6, :-4]
    A = a[..., 3:4] / 255; out[..., :3] = a[..., :3] * A; out[..., 3:4] = np.maximum(a[..., 3:4], out[..., 3:4])
    out[..., :3] = np.where(out[..., 3:4] > 0, a[..., :3] * A / np.maximum(out[..., 3:4] / 255, 1e-4), 0)
    Image.fromarray(np.clip(out, 0, 255).astype('uint8')).save(os.path.join(OUT, name), optimize=True)

def mirror(pts): return [(W - x, y) for x, y in pts]
BR = [(280, 172), (250, 205), (180, 228), (140, 272)]
TA = [(140, 272), (118, 298), (82, 328), (55, 366)]
TB = [(140, 272), (160, 300), (186, 332), (205, 370)]
SUB = [(214, 222), (190, 215), (175, 190), (168, 175)]
# trunk and roots
im, dr = layer()
for root in [[(280, 34), (240, 16), (200, 12), (150, 6)], [(280, 34), (320, 16), (360, 12), (410, 6)], [(272, 26), (250, 12), (232, 6), (212, 2)], [(288, 26), (310, 12), (328, 6), (348, 2)]]:
    limb(dr, root, 22, 6)
limb(dr, [(280, 2), (270, 60), (292, 120), (280, 168)], 50, 34)
save(im, 'talent_tree_trunk.png')
for side, f in (('left', lambda p: p), ('right', mirror)):
    im, dr = layer()
    tips = f([TA[3], TB[3], TA[2], TB[2], BR[3], BR[2]])
    leaves(dr, [(x, y, 40, 34) for x, y in tips[:2]] + [(x, y, 34, 20) for x, y in tips[2:]], 7 if side == 'left' else 11, False)
    limb(dr, f(BR), 30, 15); limb(dr, f(TA), 15, 4); limb(dr, f(TB), 15, 4)
    leaves(dr, [(x, y, 26, 8) for x, y in tips[:2]] + [(x, y, 18, 4) for x, y in tips[2:5]], 3 if side == 'left' else 5, True)
    save(im, 'talent_tree_' + side + '.png')

# talent glyphs, keyed by the talent's main stat
from svgr import mask
G = {'dmg': 'crossed-swords', 'hp': 'heart-plus', 'armor': 'checked-shield', 'cdr': 'hourglass', 'leech': 'bleeding-heart', 'thorns': 'spiked-armor',
     'block': 'magic-shield', 'burn': 'flame', 'fireball': 'fireball', 'holyfire': 'sun', 'frozen': 'snowflake-2', 'novaCd': 'frozen-orb',
     'divineCd': 'angel-wings', 'chargeCd': 'charging-bull', 'barrier': 'magic-shield', 'heal': 'healing', 'killHeal': 'skull-crossed-bones',
     'atkspd': 'sprint', 'evo': 'upgrade', 'arc': 'lightning-arc', 'smite': 'lightning-helix', 'rage': 'enrage', 'explode': 'explosion-rays',
     'cheat': 'angel-outfit', 'crit': 'bullseye', 'evade': 'dodging', 'stunHit': 'knockout', 'fearHit': 'terror', 'venom': 'poison-bottle',
     'ranged': 'thrown-daggers', 'pctHeal': 'health-increase', 'manaHeal': 'water-drop', 'healShield': 'shield-reflect', 'totem': 'totem', 'lock': 'padlock'}
paths = {}
for l in open('/tmp/gi/all.txt'):
    l = l.strip(); n = l.split('/')[-1][:-4]
    if n not in paths or l.startswith('lorc/'): paths[n] = '/tmp/gi/icons/' + l
C = 96; cols = 8; keys = list(G); rows = (len(keys) + cols - 1) // cols
at = Image.new('RGBA', (cols * C, rows * C), (0, 0, 0, 0)); idx = {}
for i, k in enumerate(keys):
    m = mask(paths[G[k]], C * 2); g = Image.fromarray((m * 255).astype('uint8')).resize((C - 8, C - 8), Image.LANCZOS)
    cell = Image.new('RGBA', (C, C), (255, 255, 255, 0)); cell.putalpha(0)
    white = Image.new('RGBA', (C - 8, C - 8), (255, 255, 255, 255)); white.putalpha(g)
    cell.alpha_composite(white, (4, 4)); at.paste(cell, ((i % cols) * C, (i // cols) * C)); idx[k] = i
at.save(os.path.join(OUT, 'talent_icons.png'), optimize=True)
json.dump({'cols': cols, 'cell': C, 'index': idx}, open(os.path.join(OUT, 'talent_icons_index.json'), 'w'))
print('ok')
