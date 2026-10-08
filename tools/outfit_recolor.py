# recolour the greens (hue 55-170 deg) of a texture to another hue; leather, metal and skin tones stay
import sys
import numpy as np
from PIL import Image
src, out, hue, sat_k, val_k = sys.argv[1], sys.argv[2], float(sys.argv[3]), float(sys.argv[4]), float(sys.argv[5])
im = Image.open(src).convert('RGBA'); a = im.split()[3]
hsv = np.array(im.convert('RGB').convert('HSV')).astype(np.float32)
h = hsv[..., 0] * 360 / 255
m = (h >= 55) & (h <= 170) & (hsv[..., 1] > 25)
hsv[..., 0][m] = hue * 255 / 360
hsv[..., 1][m] = np.clip(hsv[..., 1][m] * sat_k, 0, 255)
hsv[..., 2][m] = np.clip(hsv[..., 2][m] * val_k, 0, 255)
o = Image.fromarray(hsv.astype(np.uint8), 'HSV').convert('RGB'); o.putalpha(a); o.save(out)
print(out, int(m.sum()), 'px recoloured')
