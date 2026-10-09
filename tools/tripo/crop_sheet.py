# crop pieces from a concept sheet (coords in a 1350-wide frame), clear only the background that touches the edges, pad square to 1024
import sys, json, os
import numpy as np
from scipy import ndimage
from PIL import Image
src, out, spec = sys.argv[1], sys.argv[2], json.loads(sys.argv[3])
im = Image.open(src).convert('RGB'); k = im.width / 1350
os.makedirs(out, exist_ok=True)
for name, (x0, y0, x1, y1) in spec.items():
    c = np.array(im.crop((int(x0*k), int(y0*k), int(x1*k), int(y1*k)))).astype(int)
    lo, hi = c.min(2), c.max(2)
    bgm = (lo > 190) & (hi - lo < 34)
    lab, n = ndimage.label(bgm)
    edge = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
    m = np.isin(lab, list(edge))
    m = ndimage.binary_dilation(m, iterations=1) & (lo > 160)   # soften the halo a little
    c[m] = 255
    c = Image.fromarray(c.astype('uint8'))
    s = max(c.size); S = int(s * 1.15)
    bg = Image.new('RGB', (S, S), (255, 255, 255)); bg.paste(c, ((S - c.width)//2, (S - c.height)//2))
    bg.resize((1024, 1024), Image.LANCZOS).save(f'{out}/{name}.png')
