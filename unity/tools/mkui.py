# map dressing for the in-game maps: parchment, an inked frame with corner knots, a compass rose
import numpy as np, math
from PIL import Image, ImageDraw, ImageFilter
OUT = '/tmp/wm/'
def vnoise(n, scale, seed):
    r = np.random.default_rng(seed); g = r.random((int(n / scale) + 3, int(n / scale) + 3)).astype(np.float32)
    return np.asarray(Image.fromarray((g * 255).astype(np.uint8)).resize((int(g.shape[1] * scale), int(g.shape[0] * scale)), Image.BICUBIC)).astype(np.float32)[:n, :n] / 255
def fbm(n, base, octs, seed):
    t = 0; a = 1; s = 0
    for o in range(octs): t = t + a * vnoise(n, base / 2 ** o, seed + o); s += a; a *= 0.5
    return t / s
# parchment 512 (tileable enough for a single map)
N = 512; p = fbm(N, 128, 5, 3); fib = vnoise(N, 2, 9)
base = np.array([244, 228, 190], np.float32)
img = base * (0.88 + 0.16 * p[..., None]) * (0.97 + 0.05 * fib[..., None])
Image.fromarray(np.clip(img, 0, 255).astype(np.uint8)).save(OUT + 'map_parchment.png')
# frame 1024 with a clear middle
F = 1024; fr = Image.new('RGBA', (F, F), (0, 0, 0, 0)); d = ImageDraw.Draw(fr)
INK = (64, 44, 28, 255); GOLD = (178, 132, 60, 255); PAP = (236, 220, 182, 255)
yy, xx = np.mgrid[0:F, 0:F].astype(np.float32)
edge = np.minimum(np.minimum(xx, F - 1 - xx), np.minimum(yy, F - 1 - yy))
burn = fbm(F, 60, 4, 17)
# a torn, burnt parchment edge: paper colour fading in over the outer band, darker at the very rim
band = np.clip(1 - (edge - 18 - burn * 26) / 30, 0, 1)
rim = np.clip(1 - (edge - 4 - burn * 10) / 10, 0, 1)
rgba = np.zeros((F, F, 4), np.float32)
rgba[..., :3] = np.array([226, 206, 160]) * (1 - rim[..., None] * 0.55)
rgba[..., 3] = np.clip(band * 0.92 + rim * 0.3, 0, 1) * 255
fr = Image.fromarray(rgba.astype(np.uint8), 'RGBA'); d = ImageDraw.Draw(fr)
for pad, w, c in [(40, 5, INK), (50, 2, GOLD), (56, 2, INK)]: d.rectangle([pad, pad, F - pad, F - pad], outline=c, width=w)
# dashes between the lines
for i in range(60, F - 60, 18):
    for (x0, y0, x1, y1) in [(i, 45, i + 8, 45), (i, F - 45, i + 8, F - 45), (45, i, 45, i + 8), (F - 45, i, F - 45, i + 8)]: d.line([(x0, y0), (x1, y1)], fill=GOLD, width=2)
for (cx, cy) in [(48, 48), (F - 48, 48), (48, F - 48), (F - 48, F - 48)]:
    d.ellipse([cx - 26, cy - 26, cx + 26, cy + 26], fill=PAP, outline=INK, width=4)
    d.ellipse([cx - 15, cy - 15, cx + 15, cy + 15], outline=GOLD, width=3)
    d.polygon([(cx, cy - 11), (cx + 11, cy), (cx, cy + 11), (cx - 11, cy)], fill=(150, 44, 30, 255))
# side knots
for (cx, cy) in [(F // 2, 48), (F // 2, F - 48), (48, F // 2), (F - 48, F // 2)]:
    d.polygon([(cx, cy - 16), (cx + 16, cy), (cx, cy + 16), (cx - 16, cy)], fill=PAP, outline=INK)
    d.polygon([(cx, cy - 7), (cx + 7, cy), (cx, cy + 7), (cx - 7, cy)], fill=GOLD)
fr.save(OUT + 'map_frame.png')
# compass 256
C = 256; cp = Image.new('RGBA', (C, C), (0, 0, 0, 0)); d = ImageDraw.Draw(cp); cx = cy = C / 2; cr = 104
d.ellipse([cx - cr * 0.72, cy - cr * 0.72, cx + cr * 0.72, cy + cr * 0.72], fill=(240, 226, 190, 200), outline=INK, width=3)
d.ellipse([cx - cr * 0.62, cy - cr * 0.62, cx + cr * 0.62, cy + cr * 0.62], outline=GOLD, width=2)
for a in range(0, 360, 45):
    big = a % 90 == 0; L = cr if big else cr * 0.62; ra = math.radians(a - 90)
    tip = (cx + math.cos(ra) * L, cy + math.sin(ra) * L)
    l1 = (cx + math.cos(ra + 0.4) * cr * 0.17, cy + math.sin(ra + 0.4) * cr * 0.17); l2 = (cx + math.cos(ra - 0.4) * cr * 0.17, cy + math.sin(ra - 0.4) * cr * 0.17)
    d.polygon([tip, l1, (cx, cy)], fill=(160, 40, 28, 255) if a == 0 else INK)
    d.polygon([tip, l2, (cx, cy)], fill=(244, 232, 200, 255), outline=INK)
d.ellipse([cx - 9, cy - 9, cx + 9, cy + 9], fill=GOLD, outline=INK, width=2)
cp.save(OUT + 'map_compass.png')
# a pin for 'you are here'
P = 96; pin = Image.new('RGBA', (P, P), (0, 0, 0, 0)); d = ImageDraw.Draw(pin)
d.ellipse([18, 6, 78, 66], fill=(200, 40, 30, 255), outline=(60, 20, 14, 255), width=5)
d.polygon([(26, 52), (48, 92), (70, 52)], fill=(200, 40, 30, 255), outline=(60, 20, 14, 255))
d.ellipse([18, 6, 78, 66], fill=(200, 40, 30, 255)); d.ellipse([18, 6, 78, 66], outline=(60, 20, 14, 255), width=5)
d.ellipse([36, 22, 60, 46], fill=(250, 236, 200, 255))
pin.save(OUT + 'map_pin.png')
print('ok')
