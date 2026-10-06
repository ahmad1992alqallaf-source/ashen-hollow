# builds Resources/AH/Icons/item_icons_id.png (+ _index.json): one full-colour picture per item id
# usage: git clone https://github.com/game-icons/icons /tmp/gi/icons; (cd /tmp/gi/icons && find . -name '*.svg' | sed 's#^./##' > ../all.txt)
#        python3 build.py <Assets/AshenHollow/Resources/AH/Icons/item_icons_id>   (items.json path: ITEMS env var)
import json, sys, os, numpy as np
from PIL import Image, ImageFilter, ImageDraw
sys.path.insert(0, os.path.dirname(__file__))
from svgr import mask as svgmask
from mapping import M

OUT = sys.argv[1]
ITEMS = json.load(open(os.environ.get('ITEMS', os.path.join(os.path.dirname(__file__), '../../Resources/AH/Data/items.json'))))['ITEMS']
paths = {}
for l in open('/tmp/gi/all.txt'):
    l = l.strip(); n = l.split('/')[-1][:-4]
    # prefer lorc / delapouite (the classic look) when several authors have one
    if n not in paths or l.startswith('lorc/'): paths[n] = '/tmp/gi/icons/' + l
S = 112; R = 2  # draw at 2x then shrink
cache = {}
def glyph(n, size):
    k = (n, size)
    if k not in cache: cache[k] = svgmask(paths[n], size)
    return cache[k]

def hexc(h): h = h.lstrip('#'); return np.array([int(h[i:i+2], 16) / 255 for i in (0, 2, 4)])
def lum(c): return 0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2]

def paint(m, col, outline=None, ow=5):
    """a glyph mask (HxW float) -> RGBA float: gradient fill, rim light, dark outline"""
    H, W = m.shape
    y = np.linspace(0, 1, H)[:, None]
    top = np.clip(col * 1.25 + 0.12, 0, 1); bot = col * 0.62
    rgb = top[None, None, :] * (1 - y[..., None]) + bot[None, None, :] * y[..., None]
    rgb = np.broadcast_to(rgb, (H, W, 3)).copy()
    # rim light: where the shape ends just above-left, add a bright edge
    sh = np.zeros_like(m); sh[3:, 3:] = m[:-3, :-3]
    rim = np.clip(m - sh, 0, 1)
    rgb = rgb + rim[..., None] * 0.35
    # shade the lower-right inner edge
    sh2 = np.zeros_like(m); sh2[:-3, :-3] = m[3:, 3:]
    rgb = rgb * (1 - np.clip(m - sh2, 0, 1)[..., None] * 0.35)
    rgb = np.clip(rgb, 0, 1)
    if outline is None: outline = np.array([0.08, 0.05, 0.03]) if lum(col) > 0.16 else np.array([0.85, 0.72, 0.45])
    im = Image.fromarray((m * 255).astype('uint8'))
    om = np.asarray(im.filter(ImageFilter.MaxFilter(ow * 2 + 1))).astype(np.float32) / 255
    out = np.zeros((H, W, 4), np.float32)
    out[..., :3] = outline; out[..., 3] = om
    out[..., :3] = out[..., :3] * (1 - m[..., None]) + rgb * m[..., None]
    return out

def over(dst, src, x=0, y=0):
    h, w = src.shape[:2]
    d = dst[y:y+h, x:x+w]
    a = src[..., 3:4]
    na = a + d[..., 3:4] * (1 - a)
    d[..., :3] = np.where(na > 0, (src[..., :3] * a + d[..., :3] * d[..., 3:4] * (1 - a)) / np.maximum(na, 1e-6), 0)
    d[..., 3:4] = na

def shadow(img, dx=5, dy=7, blur=6, a=0.55):
    al = Image.fromarray((img[..., 3] * 255).astype('uint8')).filter(ImageFilter.GaussianBlur(blur))
    sm = np.asarray(al).astype(np.float32) / 255 * a
    s = np.zeros_like(img); s[..., 3] = 0
    s[dy:, dx:, 3] = sm[:-dy, :-dx]
    over(s, img); return s

def shape_layer(size, kind, col):
    """drawn backdrops: plate, bowl, card"""
    im = Image.new('RGBA', (size, size), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    c = tuple(int(v * 255) for v in col)
    if kind == 'plate':
        d.ellipse([size*0.04, size*0.46, size*0.96, size*0.94], fill=(28, 20, 14, 255))
        d.ellipse([size*0.07, size*0.48, size*0.93, size*0.91], fill=(236, 230, 214, 255))
        d.ellipse([size*0.2, size*0.55, size*0.8, size*0.84], fill=(214, 206, 188, 255))
    elif kind == 'bowl':
        d.chord([size*0.06, size*0.2, size*0.94, size*0.96], 0, 180, fill=(28, 20, 14, 255))
        d.chord([size*0.1, size*0.24, size*0.9, size*0.92], 0, 180, fill=(150, 98, 58, 255))
        d.ellipse([size*0.06, size*0.48, size*0.94, size*0.66], fill=(28, 20, 14, 255))
        d.ellipse([size*0.1, size*0.5, size*0.9, size*0.64], fill=c + (255,))
    elif kind == 'card':
        d.rounded_rectangle([size*0.14, size*0.03, size*0.86, size*0.97], radius=size*0.08, fill=(28, 20, 14, 255))
        d.rounded_rectangle([size*0.17, size*0.06, size*0.83, size*0.94], radius=size*0.06, fill=c + (255,))
        d.rounded_rectangle([size*0.23, size*0.12, size*0.77, size*0.88], radius=size*0.04, fill=(240, 228, 200, 255))
    return np.asarray(im).astype(np.float32) / 255

def star(size, col):
    im = Image.new('RGBA', (size, size), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    import math
    def pts(r1, r2):
        p = []
        for i in range(10):
            a = -math.pi / 2 + i * math.pi / 5; r = r1 if i % 2 == 0 else r2
            p.append((size / 2 + r * math.cos(a), size / 2 + r * math.sin(a)))
        return p
    d.polygon(pts(size * 0.5, size * 0.22), fill=(30, 18, 8, 255))
    d.polygon(pts(size * 0.4, size * 0.17), fill=col)
    return np.asarray(im).astype(np.float32) / 255

def make(iid):
    v = ITEMS[iid]
    base = iid[:-5] if iid.endswith('_fine') else iid[:-7] if iid.endswith('_master') else iid
    spec = M[base]; spec = {'g': spec} if isinstance(spec, str) else spec
    col = hexc(spec.get('c', ITEMS[base].get('c', v.get('c', '#c0a080'))))
    Z = S * R
    can = np.zeros((Z, Z, 4), np.float32)
    under = spec.get('under')
    if under:
        over(can, shape_layer(Z, under, col if under != 'card' else col))
    if under == 'card':
        gm = glyph(spec['g'], int(Z * 0.5)); g = paint(gm, col * 0.6, ow=3)
        over(can, g, int(Z * 0.25), int(Z * 0.24))
    else:
        gs = int(Z * (0.62 if under == 'plate' else 0.56 if under == 'bowl' else 0.84))
        gy = int(Z * (0.12 if under == 'plate' else 0.02 if under == 'bowl' else 0.08))
        g = paint(glyph(spec['g'], gs), col)
        over(can, g, (Z - gs) // 2, gy)
    if 'badge' in spec:
        bs = int(Z * 0.46); bc = hexc(spec.get('bc', '#f0e6d0'))
        disc = Image.new('RGBA', (bs, bs), (0, 0, 0, 0)); dd = ImageDraw.Draw(disc)
        dd.ellipse([0, 0, bs - 1, bs - 1], fill=(26, 18, 12, 255)); dd.ellipse([4, 4, bs - 5, bs - 5], fill=(70, 52, 36, 255))
        disc = np.asarray(disc).astype(np.float32) / 255
        over(can, disc, Z - bs - 2, Z - bs - 2)
        bg = paint(glyph(spec['badge'], int(bs * 0.78)), bc, ow=3)
        over(can, bg, Z - bs - 2 + int(bs * 0.11), Z - bs - 2 + int(bs * 0.11))
    can = shadow(can, 4, 6, 5, 0.5)
    if iid.endswith('_fine') or iid.endswith('_master'):
        st = star(int(Z * 0.36), (255, 214, 74, 255) if iid.endswith('_master') else (214, 226, 238, 255))
        over(can, st, Z - int(Z * 0.36) - 2, 2)
        if iid.endswith('_master'):
            st2 = star(int(Z * 0.26), (255, 214, 74, 255)); over(can, st2, Z - int(Z * 0.36) - int(Z * 0.2), 4)
    im = Image.fromarray((np.clip(can, 0, 1) * 255).astype('uint8'), 'RGBA').resize((S, S), Image.LANCZOS)
    return im

ids = [k for k, v in ITEMS.items() if not v.get('slot')]
cols = 18; rows = (len(ids) + cols - 1) // cols
atlas = Image.new('RGBA', (cols * S, rows * S), (0, 0, 0, 0))
index = {}
for n, iid in enumerate(ids):
    atlas.paste(make(iid), ((n % cols) * S, (n // cols) * S)); index[iid] = n
atlas.save(OUT + '.png', optimize=True)
json.dump({'cols': cols, 'cell': S, 'index': index}, open(OUT + '_index.json', 'w'))
print(len(ids), atlas.size)
