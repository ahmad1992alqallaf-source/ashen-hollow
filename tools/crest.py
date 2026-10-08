# Ashen Hollow crest: a hollow tree inside a ring, an ember glowing in its heart. Drawn entirely in code (our own art).
# usage: crest.py out.png "r,g,b" (metal) "r,g,b" (dark) "r,g,b" (ember) [size]
import sys, math
from PIL import Image, ImageDraw, ImageFilter
def crest(metal, dark, ember, S=1024):
    c = S / 2; k = S / 1024
    base = Image.new('RGBA', (S, S), (0, 0, 0, 0)); d = ImageDraw.Draw(base)
    E = lambda r: [c - r * k, c - r * k, c + r * k, c + r * k]
    d.ellipse(E(470), fill=dark); d.ellipse(E(445), fill=metal); d.ellipse(E(395), fill=dark)
    for a in range(0, 360, 90):
        x = c + 420 * k * math.cos(math.radians(a + 45)); y = c + 420 * k * math.sin(math.radians(a + 45)); d.ellipse([x - 14 * k, y - 14 * k, x + 14 * k, y + 14 * k], fill=dark)
    tree = Image.new('RGBA', (S, S), (0, 0, 0, 0)); t = ImageDraw.Draw(tree)
    P = lambda pts: [(c + x * k, c + y * k) for x, y in pts]
    t.polygon(P([(-100, 330), (-72, 40), (-58, -70), (58, -70), (72, 40), (100, 330)]), fill=metal)
    for sx in (-1, 1): t.polygon(P([(sx * 70, 250), (sx * 250, 330), (sx * 300, 360), (sx * 90, 320)]), fill=metal)
    def br(x0, y0, ang, L, w, depth):
        if depth == 0: t.ellipse([c + (x0 - w * 1.6) * k, c + (y0 - w * 1.6) * k, c + (x0 + w * 1.6) * k, c + (y0 + w * 1.6) * k], fill=metal); return
        x1 = x0 + L * math.cos(ang); y1 = y0 - L * math.sin(ang); nx = math.sin(ang) * w; ny = math.cos(ang) * w
        t.polygon(P([(x0 - nx, y0 - ny), (x1 - nx * 0.6, y1 - ny * 0.6), (x1 + nx * 0.6, y1 + ny * 0.6), (x0 + nx, y0 + ny)]), fill=metal)
        br(x1, y1, ang + 0.45, L * 0.66, w * 0.62, depth - 1); br(x1, y1, ang - 0.4, L * 0.64, w * 0.6, depth - 1)
    br(0, -55, math.pi / 2 + 0.6, 170, 44, 3); br(0, -55, math.pi / 2 - 0.6, 170, 44, 3); br(0, -65, math.pi / 2, 150, 36, 3)
    m = Image.new('L', (S, S), 0); ImageDraw.Draw(m).ellipse(E(392), fill=255)
    clipped = Image.new('RGBA', (S, S), (0, 0, 0, 0)); clipped.paste(tree, (0, 0), Image.composite(tree.split()[3], Image.new('L', (S, S), 0), m))
    im = Image.alpha_composite(base, clipped); d = ImageDraw.Draw(im)
    d.ellipse([c - 44 * k, c + 80 * k, c + 44 * k, c + 200 * k], fill=dark)
    g = Image.new('RGBA', (S, S), (0, 0, 0, 0)); ImageDraw.Draw(g).ellipse([c - 75 * k, c + 65 * k, c + 75 * k, c + 215 * k], fill=ember + (170,)); g = g.filter(ImageFilter.GaussianBlur(26 * k)); im = Image.alpha_composite(im, g); d = ImageDraw.Draw(im)
    d.polygon(P([(0, 98), (27, 148), (11, 186), (-11, 186), (-27, 148)]), fill=ember + (255,))
    d.polygon(P([(0, 128), (10, 158), (0, 178), (-10, 158)]), fill=(255, 235, 170, 255))
    return im
if __name__ == '__main__':
    col = lambda s: tuple(int(v) for v in s.split(','))
    S = int(sys.argv[5]) if len(sys.argv) > 5 else 1024
    crest(col(sys.argv[2]) + (255,), col(sys.argv[3]) + (255,), col(sys.argv[4]), S).save(sys.argv[1])
