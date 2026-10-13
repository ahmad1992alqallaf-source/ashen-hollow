# Ashen Hollow: draws the Voidwing's wing (one wing, the right; the game mirrors it for the left) as a feathered
# picture with see-through gaps: primaries fanned from the wrist, secondaries hanging from the arm, rows of coverts
# over them and the leading edge on top. Made by code, so nothing licensed.
#   python wing_tex.py <out.png> [palette: void|ember|frost]
import sys, math, random
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
out = sys.argv[1]; pal = sys.argv[2] if len(sys.argv) > 2 else 'void'
W, H = 2048, 1024; SS = 1     # drawn at this size, saved at half
P = {'void': dict(root=(14, 8, 26), mid=(44, 24, 78), tip=(120, 70, 210), rim=(190, 140, 255), edge=(6, 3, 12), shaft=(110, 80, 160)),
     'ember': dict(root=(30, 8, 4), mid=(110, 30, 10), tip=(240, 120, 30), rim=(255, 210, 120), edge=(14, 4, 2), shaft=(200, 110, 60)),
     'frost': dict(root=(60, 80, 110), mid=(140, 170, 205), tip=(220, 236, 255), rim=(255, 255, 255), edge=(40, 52, 72), shaft=(200, 220, 240))}[pal]
rng = random.Random(7)
rgb = np.zeros((H, W, 3), np.float32); alpha = np.zeros((H, W), np.float32)
S, E, R = np.array([30., 330.]), np.array([760., 205.]), np.array([1250., 300.])   # shoulder, elbow, wrist (pixels)

def lerp(a, b, t): return a + (b - a) * t
def feather(base, ang, length, width, bright=1.0, tipglow=1.0, pointy=0.35):
    """one feather: base point, direction (degrees, 0 = +x, 90 = down), length and widest width, in pixels"""
    d = np.array([math.cos(math.radians(ang)), math.sin(math.radians(ang))]); n = np.array([-d[1], d[0]])
    tip = base + d * length
    x0, y0 = np.floor(np.minimum(base, tip) - width).astype(int); x1, y1 = np.ceil(np.maximum(base, tip) + width).astype(int)
    x0, y0 = max(0, x0), max(0, y0); x1, y1 = min(W, x1), min(H, y1)
    if x1 <= x0 or y1 <= y0: return
    yy, xx = np.mgrid[y0:y1, x0:x1].astype(np.float32)
    rx, ry = xx - base[0], yy - base[1]
    t = (rx * d[0] + ry * d[1]) / length          # 0 at the base, 1 at the tip
    s = (rx * n[0] + ry * n[1])                   # across, pixels (+ one vane, - the other)
    # the vane's half width along the feather: narrow quill, full by a third, tapering to a rounded point
    prof = np.where(t < 0.25, np.sqrt(np.clip(t / 0.25, 0, 1)), np.power(np.clip((1 - t) / 0.75, 0, 1), pointy))
    hw = width * 0.5 * prof * np.where(s > 0, 1.0, 0.72)        # the trailing vane wider than the leading one
    inside = (t >= 0) & (t <= 1) & (np.abs(s) <= hw)
    if not inside.any(): return
    # a soft edge (1.5 px) so the picture is not jagged
    cov = np.clip((hw - np.abs(s)) / 1.5, 0, 1) * (t >= 0) * (t <= 1)
    # little notches in the vane: barbs splitting near the edge
    # barbs: fine diagonal stripes running from the shaft toward the tip
    barbs = 0.5 + 0.5 * np.sin(np.abs(s) * 0.7 - t * length * 0.35)
    edgeband = np.clip(np.abs(s) / np.maximum(hw, 1e-3), 0, 1)
    # one split in the vane now and then (a real feather is never perfect)
    if rng.random() < 0.5:
        tc = rng.uniform(0.35, 0.8); side = 1 if rng.random() < 0.5 else -1
        cut = (np.abs(t - tc - (np.abs(s) / length) * 0.6) < 0.012) & (edgeband > 0.55) & (s * side > 0)
        cov *= np.where(cut, 0.0, 1.0)
    # colour: root -> mid -> tip along the feather; a lit rim along the trailing edge; darker toward the shaft's sides
    c0, c1, c2 = (np.array(P[k], np.float32) for k in ('root', 'mid', 'tip'))
    tt = np.clip(t, 0, 1)[..., None]
    col = np.where(tt < 0.55, lerp(c0, c1, tt / 0.55), lerp(c1, c2, np.clip((tt - 0.55) / 0.45, 0, 1) * tipglow))
    col = col * (0.75 + 0.35 * edgeband[..., None]) * bright
    col = col * (0.9 + 0.1 * barbs[..., None])                 # the barbs' fine stripes
    rim = np.clip((edgeband - 0.86) / 0.14, 0, 1) * (s > 0) * np.clip((t - 0.25) / 0.5, 0, 1)
    col = lerp(col, np.array(P['rim'], np.float32), (rim * 0.55)[..., None])
    # the shaft
    shaft = np.clip(1 - np.abs(s) / 1.6, 0, 1) * (t < 0.93)
    col = lerp(col, np.array(P['shaft'], np.float32), (shaft * 0.7)[..., None])
    # a dark outline where this feather lies over the ones below
    outl = np.clip((np.abs(s) - (hw - 2.5)) / 2.5, 0, 1)
    col = lerp(col, np.array(P['edge'], np.float32), (outl * 0.85)[..., None])
    a = cov[..., None]
    rgb[y0:y1, x0:x1] = rgb[y0:y1, x0:x1] * (1 - a) + col * a
    alpha[y0:y1, x0:x1] = np.maximum(alpha[y0:y1, x0:x1], cov)

def arm(u):   # a point on the arm (leading edge) at u 0..1 from shoulder to wrist
    return lerp(S, E, u / 0.6) if u < 0.6 else lerp(E, R, (u - 0.6) / 0.4)

# 1) primaries: ten, outermost first (fanned from nearly straight out to steeply down)
for i in range(12):
    k = i / 11.0
    ang = lerp(6, 62, k) + rng.uniform(-1.0, 1.0)
    L = lerp(790, 590, k) * rng.uniform(0.97, 1.03)
    base = R + np.array([lerp(30, -130, k), lerp(-12, 40, k)])
    feather(base, ang, L, lerp(118, 135, k), bright=lerp(1.05, 0.95, k), pointy=0.4)
# 2) secondaries: hanging from the arm, shoulder to wrist, longest in the middle, the trailing edge scalloped
n2 = 19
for i in range(n2 - 1, -1, -1):
    u = 0.06 + 0.92 * i / (n2 - 1)
    base = arm(u) + np.array([0, 40])
    L = (360 + 300 * math.sin(math.pi * min(1, u * 1.15))) * rng.uniform(0.96, 1.04)
    ang = lerp(102, 78, u) + rng.uniform(-2, 2)
    feather(base, ang, L, 150, bright=0.92, tipglow=0.9, pointy=0.22)
# 3) greater coverts over the primaries' and secondaries' roots, then median, then lesser rows
def row(n, u0, u1, off, Lr, wd, ang0, ang1, br, pointy=0.3):
    for i in range(n - 1, -1, -1):
        u = lerp(u0, u1, i / max(1, n - 1)); base = arm(u) + np.array([0, off])
        feather(base, lerp(ang0, ang1, u) + rng.uniform(-3, 3), Lr * rng.uniform(0.92, 1.06), wd, bright=br, tipglow=0.75, pointy=pointy)
for i in range(7):   # primary coverts at the wrist
    k = i / 6.0; feather(R + np.array([lerp(10, -90, k), lerp(0, 30, k)]), lerp(10, 60, k), lerp(380, 300, k), 105, bright=0.85, tipglow=0.7)
row(22, 0.04, 1.0, 22, 300, 112, 100, 70, 0.82, pointy=0.35)
row(26, 0.03, 1.0, 12, 175, 92, 104, 66, 0.74, pointy=0.5)
row(34, 0.02, 1.02, 4, 100, 70, 110, 60, 0.66, pointy=0.8)
# 4) the leading edge: a dark rounded band along the arm with small scale feathers
pts = [arm(u) for u in np.linspace(0, 1, 240)]
for j, p in enumerate(pts):
    u = j / 239.0; r = lerp(26, 18, u)
    yy, xx = np.mgrid[0:H, 0:W] if False else (None, None)
    x0, y0, x1, y1 = int(p[0] - r - 2), int(p[1] - r - 2), int(p[0] + r + 3), int(p[1] + r + 3)
    x0, y0 = max(0, x0), max(0, y0)
    Y, X = np.mgrid[y0:y1, x0:x1].astype(np.float32); dd = np.sqrt((X - p[0]) ** 2 + (Y - p[1]) ** 2)
    cov = np.clip((r - dd) / 1.5, 0, 1); shade = np.clip(1 - (Y - (p[1] - r)) / (2 * r), 0, 1)
    col = lerp(np.array(P['root'], np.float32), np.array(P['mid'], np.float32), shade[..., None] * 0.9)
    rgb[y0:y1, x0:x1] = rgb[y0:y1, x0:x1] * (1 - cov[..., None]) + col * cov[..., None]
    alpha[y0:y1, x0:x1] = np.maximum(alpha[y0:y1, x0:x1], cov)
# a little bleed of colour into the see-through texels so mipmaps do not draw dark halos
img = Image.fromarray(np.clip(rgb, 0, 255).astype(np.uint8)); a = Image.fromarray((alpha * 255).astype(np.uint8))
bled = img.filter(ImageFilter.GaussianBlur(6)); m = np.asarray(a).astype(np.float32)[..., None] / 255
mix = (np.asarray(img).astype(np.float32) * m + np.asarray(bled).astype(np.float32) * (1 - m)).astype(np.uint8)
res = Image.fromarray(mix).convert('RGBA'); res.putalpha(a)
res = res.resize((W // 2, H // 2), Image.LANCZOS)
res.save(out); print('wrote', out, res.size)
