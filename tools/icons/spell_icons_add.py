# adds round spell icons to Resources/AH/Icons/spell_icons.png (+ _index.json) in the atlas's own style: a glossy
# coloured disc with a dark rim and a cream glyph (glyphs: game-icons.net, CC BY 3.0, read from /tmp/gi).
# usage: spell_icons_add.py atlas.png index.json out_atlas.png out_index.json
import sys, os, json, numpy as np
from PIL import Image, ImageDraw, ImageFilter
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from svgr import mask as svgmask
NEW = {   # spell id: (glyph, disc colour)
 # Shaman
 'lbolt': ('lightning-helix', '#3a8ad8'), 'healtotem': ('totem', '#3f9a5a'), 'wartotem': ('totem-head', '#c8502a'), 'chainheal': ('healing', '#3f9a5a'),
 'earthshock': ('earth-crack', '#8a6a44'), 'frostshock': ('frostfire', '#4a8ad0'), 'searingtotem': ('snake-totem', '#d0602a'), 'healingwave': ('big-wave', '#2a9a9a'),
 'windtotem': ('whirlwind', '#5a9aa8'), 'ghostwolf': ('wolf-howl', '#6a5aa8'), 'lavaburst': ('lava', '#d0502a'), 'earthquake': ('quake-stomp', '#8a6a44'),
 'riptide': ('wave-crest', '#2a9a9a'), 'stormtotem': ('lightning-storm', '#3a7ad0'), 'bloodlust': ('blood', '#b8303a'), 'spirittotem': ('totem-mask', '#3f9a5a'),
 'chainlight': ('lightning-dissipation', '#3a8ad8'), 'ancestors': ('spark-spirit', '#c8a03a'), 'greattotem': ('totem', '#8a6a44'), 'thunderstorm': ('thunder-struck', '#3a6ac0'),
 'magmatotem': ('fire-shrine', '#d0502a'), 'terror': ('terror', '#6a4a9a'), 'nightmare': ('cracked-mask', '#5a3a8a'), 'phoenixburst': ('fire-tail', '#e0702a'),
 # Warden
 'stonehammer': ('flat-hammer', '#8a6a4a'), 'bedrockroar': ('shouting', '#c8702a'), 'rockwall': ('stone-wall', '#7a7068'), 'stoneskin': ('rock-golem', '#8a8a86'),
 'emberquake': ('earth-spit', '#c8502a'), 'shieldslam': ('shield-impact', '#b8902a'), 'pebblethrow': ('stone-sphere', '#8a6a4a'), 'earthengrip': ('monster-grasp', '#6a7a4a'),
 'guardward': ('defensive-wall', '#6a7a8a'), 'fossilshell': ('ammonite-fossil', '#a8946a'), 'avalanche': ('falling-rocks', '#7a7068'), 'tremor': ('stone-pile', '#8a6a4a'),
 'emberbrand': ('fire-punch', '#d0602a'), 'faultline': ('edge-crack', '#8a6a44'), 'rampart': ('brick-wall', '#7a7068'), 'bulwark': ('armoured-shell', '#8a8a86'),
 'magmaquake': ('volcano', '#d0502a'), 'mountain': ('mountaintop', '#6a7a8a'), 'titanfall': ('golem-head', '#8a6a4a'), 'magmacore': ('smoking-volcano', '#b8303a'),
 'eruption': ('eruption', '#e0702a'),
}
paths = {}
for l in open('/tmp/gi/all.txt'):
    l = l.strip(); n = l.split('/')[-1][:-4]
    if n not in paths or l.startswith('lorc/'): paths[n] = '/tmp/gi/icons/' + l
def hexc(h): h = h.lstrip('#'); return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], float) / 255
def icon(glyph, col, C=128):
    S = C * 2; c = hexc(col); im = np.zeros((S, S, 4))
    yy, xx = np.mgrid[0:S, 0:S]; r = np.hypot(xx - S / 2 + 0.5, yy - S / 2 + 0.5) / (S / 2)
    disc = np.clip((0.985 - r) * S / 3, 0, 1)
    rim = np.clip((r - 0.86) * 25, 0, 1) * disc
    t = yy / S
    body = c[None, None, :] * (1.28 - 0.6 * t[..., None])
    body = body * (1 - rim[..., None]) + (c * 0.38)[None, None, :] * rim[..., None]
    hi = np.exp(-(((xx - S * 0.5) / (S * 0.32)) ** 2 + ((yy - S * 0.24) / (S * 0.16)) ** 2)) * 0.28 * (1 - rim)
    body = np.clip(body + hi[..., None], 0, 1)
    im[..., :3] = body; im[..., 3] = disc
    g = svgmask(paths[glyph], int(S * 0.62)); gs = g.shape[0]; o = (S - gs) // 2
    sh = np.zeros((S, S)); sh[o + 4:o + 4 + gs, o + 3:o + 3 + gs] = g
    sh = np.array(Image.fromarray((sh * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(3))) / 255.0
    gm = np.zeros((S, S)); gm[o:o + gs, o:o + gs] = g
    cream = np.array([0.96, 0.92, 0.82])
    for k in range(3): im[..., k] = im[..., k] * (1 - sh * 0.45 * disc)
    for k in range(3): im[..., k] = im[..., k] * (1 - gm) + cream[k] * gm
    out = Image.fromarray((np.clip(im, 0, 1) * 255).astype(np.uint8), 'RGBA')
    return out.resize((C, C), Image.LANCZOS)
src, idx, out, outidx = sys.argv[1:5]
A = Image.open(src).convert('RGBA'); D = json.load(open(idx)); C = D['cell']; cols = D['cols']; ids = D['ids']
add = [k for k in NEW if k not in ids]
n0 = max(ids.values()) + 1; total = n0 + len(add); rows = (total + cols - 1) // cols
B = Image.new('RGBA', (cols * C, rows * C), (0, 0, 0, 0)); B.paste(A, (0, 0))
for i, k in enumerate(add):
    n = n0 + i; g, col = NEW[k]
    if g not in paths: print('no glyph', g); continue
    B.paste(icon(g, col, C), ((n % cols) * C, (n // cols) * C)); ids[k] = n
D['h'] = rows * C
B.save(out); json.dump(D, open(outidx, 'w'))
print('added', len(add), 'icons; atlas', B.size)
