# Ashen Hollow: the water's detail maps for AHWater.shader v3 (all made here, nothing downloaded).
#   water_waves  : r, g = slope of a tiling ocean surface (Phillips spectrum, inverse FFT), b = its height (crests)
#   water_detail : r = caustic net (two octaves of cell edges), g = foam (soft tiling noise)
#   python3 make_water_tex.py <out dir>   -> water_waves.png, water_detail.png (copy into Resources/AH/Textures as .bytes)
import sys, numpy as np
from PIL import Image
out = sys.argv[1] if len(sys.argv) > 1 else '.'
N = 512; L = 24.0
rng = np.random.default_rng(11)
kx = np.fft.fftfreq(N, L / N) * 2 * np.pi; KX, KZ = np.meshgrid(kx, kx); K = np.sqrt(KX ** 2 + KZ ** 2) + 1e-6
wind = np.array([1.0, 0.35]); wind /= np.linalg.norm(wind); V = 4.5; Lw = V * V / 9.81
ph = np.exp(-1 / (K * Lw) ** 2) / K ** 4 * (0.35 + 0.65 * np.abs((KX * wind[0] + KZ * wind[1]) / K) ** 1.2) * np.exp(-(K * 0.18) ** 2); ph[0, 0] = 0
H = (rng.normal(size=(N, N)) + 1j * rng.normal(size=(N, N))) * np.sqrt(ph / 2)
h = np.real(np.fft.ifft2(H)); sx = np.real(np.fft.ifft2(1j * KX * H)); sz = np.real(np.fft.ifft2(1j * KZ * H))
s = max(np.percentile(np.abs(sx), 99.7), np.percentile(np.abs(sz), 99.7)); nx = np.clip(sx / s, -1, 1); nz = np.clip(sz / s, -1, 1)
hn = (h - h.min()) / (h.max() - h.min())
Image.fromarray(np.stack([(nx * 0.5 + 0.5) * 255, (nz * 0.5 + 0.5) * 255, hn * 255], -1).astype(np.uint8)).save(out + '/water_waves.png')

def cells(n, c, seed):
    # distance to the second-nearest point minus the nearest, on a tiling grid of points: 0 on the cell edges
    r = np.random.default_rng(seed); pts = r.random((c, c, 2))
    y, x = np.mgrid[0:n, 0:n] / n * c; ix = np.floor(x).astype(int); iy = np.floor(y).astype(int)
    d1 = np.full((n, n), 9.0); d2 = np.full((n, n), 9.0)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            cx = (ix + dx) % c; cy = (iy + dy) % c; px = ix + dx + pts[cy, cx, 0]; py = iy + dy + pts[cy, cx, 1]
            d = np.sqrt((x - px) ** 2 + (y - py) ** 2); m = d < d1; d2 = np.where(m, d1, np.minimum(d2, d)); d1 = np.where(m, d, d1)
    return d2 - d1
caus = np.exp(-cells(N, 8, 3) * 9) * 0.7 + np.exp(-cells(N, 14, 5) * 11) * 0.4; caus = np.clip(caus / caus.max(), 0, 1)
def smooth_noise(width):
    F = np.fft.fft2(rng.normal(size=(N, N))) * np.exp(-(K * width) ** 2); f = np.real(np.fft.ifft2(F)); return (f - f.min()) / (f.max() - f.min())
foam = np.clip((smooth_noise(0.9) * 0.6 + smooth_noise(0.25) * 0.4 - 0.35) * 2.2, 0, 1)
Image.fromarray(np.stack([caus * 255, foam * 255, np.full((N, N), 128.0)], -1).astype(np.uint8)).save(out + '/water_detail.png')
print('written to', out)
