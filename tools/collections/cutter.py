# cut a concept sheet into clean pieces for Tripo from rough boxes (in a 1000-wide frame):
#  figs: [x0, y0, x1, y1, [names...]] one band of standing figures, split at the emptiest columns
#  items: {name: [x0, y0, x1, y1]} each box tightened to the main thing in it (labels and neighbours' edges dropped)
import sys, json, os, numpy as np
from scipy import ndimage
from PIL import Image
def run(src, out, spec):
    im = Image.open(src).convert('RGB'); a = np.asarray(im).astype(int); H, W = a.shape[:2]; k = W / 1000.0
    bgc = np.median(np.concatenate([a[:6].reshape(-1, 3), a[-6:].reshape(-1, 3)]), 0)
    fgall = (np.abs(a - bgc).sum(2) > 70) | ((a.max(2) - a.min(2)) > 50)
    os.makedirs(out, exist_ok=True)
    def save(name, x0, y0, x1, y1, erase=()):
        x0, y0, x1, y1 = [int(v) for v in (max(0, x0), max(0, y0), min(W, x1), min(H, y1))]
        fgb = fgall[y0:y1, x0:x1].copy()
        for e in erase:
            ex0, ey0, ex1, ey1 = [int(v * k) for v in e]; fgb[max(0, ey0 - y0):max(0, ey1 - y0), max(0, ex0 - x0):max(0, ex1 - x0)] = False
        fg = ndimage.binary_opening(fgb, iterations=1)
        lab, n = ndimage.label(ndimage.binary_dilation(fg, iterations=3))
        if n == 0: return
        sizes = ndimage.sum(fg, lab, range(1, n + 1)); big = sizes.max()
        keep = np.isin(lab, [i + 1 for i, s in enumerate(sizes) if s > big * 0.04])
        ys, xs = np.where(keep); bx0, by0, bx1, by1 = xs.min(), ys.min(), xs.max() + 1, ys.max() + 1
        c = a[y0 + by0:y0 + by1, x0 + bx0:x0 + bx1].copy()
        m = keep[by0:by1, bx0:bx1]
        m = ndimage.binary_fill_holes(ndimage.binary_closing(m, iterations=4))
        c[~m] = 255
        c = Image.fromarray(c.astype('uint8')); s = max(c.size); S = int(s * 1.12)
        bg = Image.new('RGB', (S, S), (255, 255, 255)); bg.paste(c, ((S - c.width) // 2, (S - c.height) // 2))
        bg.resize((1024, 1024), Image.LANCZOS).save(os.path.join(out, name + '.png'))
    if 'figs' in spec:
        for band in (spec['figs'] if isinstance(spec['figs'][0], list) else [spec['figs']]):
            x0, y0, x1, y1, names = band[:5]; win = band[5] if len(band) > 5 else spec.get('win', 0.12); X0, Y0, X1, Y1 = x0 * k, y0 * k, x1 * k, y1 * k
            col = fgall[int(Y0):int(Y1), int(X0):int(X1)].sum(0).astype(float)
            col = np.convolve(col, np.ones(9) / 9, 'same'); n = len(names); cuts = [0]
            for j in range(1, n):
                c0 = len(col) * j / n; lo_, hi_ = int(c0 - len(col) * win), int(c0 + len(col) * win)
                cuts.append(lo_ + int(np.argmin(col[lo_:hi_])))
            cuts.append(len(col))
            for j, nm in enumerate(names): save(nm, X0 + cuts[j], Y0, X0 + cuts[j + 1], Y1)
    for nm, b in spec.get('items', {}).items(): save(nm, b[0] * k, b[1] * k, b[2] * k, b[3] * k, b[4] if len(b) > 4 else ())
if __name__ == '__main__':
    run(sys.argv[1], sys.argv[2], json.loads(sys.argv[3]))
