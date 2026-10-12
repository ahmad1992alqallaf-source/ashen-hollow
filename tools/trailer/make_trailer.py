# Ashen Hollow: put the trailer together from the frames AHTrailer filmed (HeroShots/trailer/<nn>_<shot>/f_*.jpg):
# the shots one after another with short cross-fades, a line of text over some of them, an opening line and the
# closing title, and its own music (made here: drums, a low drone and a rising chord, no samples, so nothing licensed).
#   python make_trailer.py <trailer folder> <out.mp4>
import os, sys, glob, math, subprocess, wave
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

src, out = sys.argv[1], sys.argv[2]
W, H, FPS, XF = 1280, 720, 30, 12          # cross-fade frames
TEXT = {   # a line over a shot (by its name)
    'dawn': 'A LIVING WORLD AWAITS', 'lava': 'LANDS OF ASH AND FIRE', 'abyss': 'BEYOND THE EDGE OF THE WORLD',
    'depths': 'DELVE INTO ANCIENT DEPTHS', 'throne': 'FACE THE EMBER THRONE', 'hero': 'FORGE YOUR LEGEND',
}
FONT = next((f for f in ['/usr/share/fonts/opentype/noto/NotoSerifCJK-Bold.ttc', '/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf'] if os.path.exists(f)), None)
def font(sz): return ImageFont.truetype(FONT, sz) if FONT else ImageFont.load_default()

def caption(img, text, a, size=46, y=None, glow=(255, 140, 50)):
    if a <= 0: return img
    over = Image.new('RGBA', img.size, (0, 0, 0, 0)); d = ImageDraw.Draw(over); f = font(size)
    spaced = ' '.join(text) if size < 80 else text
    w = d.textlength(spaced, font=f); x = (W - w) / 2; y = y if y is not None else H * 0.78
    gl = Image.new('RGBA', img.size, (0, 0, 0, 0)); ImageDraw.Draw(gl).text((x, y), spaced, font=f, fill=glow + (int(200 * a),))
    gl = gl.filter(ImageFilter.GaussianBlur(9))
    d.text((x + 2, y + 3), spaced, font=f, fill=(0, 0, 0, int(160 * a)))
    d.text((x, y), spaced, font=f, fill=(255, 238, 215, int(255 * a)))
    base = img.convert('RGBA'); base.alpha_composite(gl); base.alpha_composite(over); return base.convert('RGB')

def vignette():
    yy, xx = np.mgrid[0:H, 0:W]; r = np.sqrt(((xx - W / 2) / (W / 2)) ** 2 + ((yy - H / 2) / (H / 2)) ** 2)
    return np.clip(1.15 - 0.35 * r ** 2, 0.55, 1.0)[..., None]
VIG = vignette()
def grade(im):
    a = np.asarray(im).astype(np.float32) / 255
    a = a * VIG; a = np.clip((a - 0.5) * 1.08 + 0.5, 0, 1)       # a touch of contrast and a dark edge
    a[..., 0] *= 1.03; a[..., 2] *= 0.97                         # a little warmer
    # letterbox bars: the film look
    bar = int(H * 0.075); a[:bar] = 0; a[-bar:] = 0
    return Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))

shots = sorted(d for d in glob.glob(os.path.join(src, '[0-9][0-9]_*')) if os.path.isdir(d))
frames_out = os.path.join(os.path.dirname(os.path.abspath(out)), 'trailer_frames'); os.makedirs(frames_out, exist_ok=True)
for f in glob.glob(os.path.join(frames_out, '*.jpg')): os.remove(f)
n = 0
def emit(im):
    global n; im.save(os.path.join(frames_out, 'o_%05d.jpg' % n), quality=92); n += 1
black = Image.new('RGB', (W, H), (0, 0, 0))
# the opening: a line out of the dark
for i in range(int(2.8 * FPS)):
    t = i / (2.8 * FPS); a = min(1, t * 3) * min(1, (1 - t) * 4)
    emit(caption(black, 'FROM THE ASHES, A WORLD RISES', a, size=40, y=H * 0.46, glow=(255, 120, 40)))
prev_tail = None
for k, sd in enumerate(shots):
    name = os.path.basename(sd)[3:]
    fs = sorted(glob.glob(os.path.join(sd, 'f_*.jpg')))
    if not fs: continue
    m = len(fs); keep = XF if k < len(shots) - 1 else 0; tail = []
    for i, fp in enumerate(fs):
        im = grade(Image.open(fp).convert('RGB').resize((W, H)))
        if name in TEXT:
            tt = i / max(1, m - 1); a = min(1, max(0, (tt - 0.12) * 4)) * min(1, max(0, (0.92 - tt) * 5))
            im = caption(im, TEXT[name], a)
        if prev_tail is not None and i < len(prev_tail): im = Image.blend(prev_tail[i], im, (i + 1) / (len(prev_tail) + 1))
        elif prev_tail is None and n <= int(2.8 * FPS) + 15 and i < 15: im = Image.blend(black, im, i / 15)
        if i >= m - keep: tail.append(im); continue   # shown blended into the next shot's start
        emit(im)
    prev_tail = tail if tail else None
# the close: the last shot fades to the title
last = Image.open(os.path.join(frames_out, 'o_%05d.jpg' % (n - 1))) if n else black
for i in range(int(1.0 * FPS)): emit(Image.blend(last, black, (i + 1) / FPS))
for i in range(int(4.5 * FPS)):
    t = i / (4.5 * FPS); a = min(1, t * 2.5); b = min(1, max(0, (t - 0.35) * 3))
    im = caption(black, 'ASHEN HOLLOW', a, size=104, y=H * 0.36, glow=(255, 110, 30))
    im = caption(im, 'COMING SOON', b, size=30, y=H * 0.6, glow=(255, 160, 80))
    emit(im)
for i in range(int(0.8 * FPS)): emit(Image.blend(im, black, (i + 1) / (0.8 * FPS)))
secs = n / FPS
print('frames', n, 'seconds', round(secs, 1))

# ---- the music ----
SR = 44100; N = int(secs * SR) + SR; t = np.arange(N) / SR; mix = np.zeros(N)
def env(a, d, length):
    e = np.ones(length); ai = int(a * SR); e[:ai] = np.linspace(0, 1, ai) if ai > 0 else 1
    e[ai:] = np.exp(-np.arange(length - ai) / (d * SR)); return e
def drum(at, gain=1.0, low=48):
    i = int(at * SR); L = int(1.6 * SR); tt = np.arange(L) / SR
    f = low * (1 + 1.8 * np.exp(-tt * 18)); ph = 2 * np.pi * np.cumsum(f) / SR
    s = np.sin(ph) * np.exp(-tt * 3.2) + np.random.randn(L) * 0.25 * np.exp(-tt * 28)
    j = min(N, i + L); mix[i:j] += gain * s[:j - i]
def note(f0, at, dur, gain, bright=3):
    i = int(at * SR); L = int(dur * SR); tt = np.arange(L) / SR
    s = sum(np.sin(2 * np.pi * f0 * h * tt + h) / h ** 1.3 for h in range(1, bright + 1)) + 0.5 * np.sin(2 * np.pi * f0 * 1.003 * tt)
    e = np.minimum(1, tt / (dur * 0.35)) * np.minimum(1, (dur - tt) / (dur * 0.4))
    j = min(N, i + L); mix[i:j] += gain * (s * e)[:j - i]
# the chords: D minor, B flat, F, C, slow, a bar of four beats at 80 bpm, rising in strength
beat = 60 / 80; bar = 4 * beat
chords = [(146.8, 174.6, 220.0), (116.5, 146.8, 174.6), (174.6, 220.0, 261.6), (130.8, 164.8, 196.0)]
end_title = secs - 5.3
k = 0; at = 0.0
while at < end_title:
    c = chords[k % 4]; g = 0.05 + 0.05 * min(1, at / end_title)
    for f0 in c: note(f0 / 2, at, bar + 0.6, g * 1.2, 2); note(f0, at, bar + 0.6, g, 4)
    at += bar; k += 1
# the drums: few at first, then on every beat, a roll before the title
at = 2.8
while at < end_title:
    phase = at / end_title
    drum(at, 0.55 + 0.4 * phase)
    if phase > 0.45: drum(at + beat * 0.5, 0.25 + 0.3 * phase, 62)
    at += beat * (2 if phase < 0.3 else 1)
for r in range(8): drum(end_title - 1.0 + r * 0.12, 0.25 + r * 0.06, 70)
drum(end_title, 1.3, 40)   # the title hit
for f0 in (73.4, 146.8, 220.0, 293.7): note(f0, end_title, 5.0, 0.11, 5)
# a little room: a short noise-tail reverb
ir = np.random.randn(int(1.8 * SR)) * np.exp(-np.arange(int(1.8 * SR)) / (0.45 * SR)) * 0.02
wet = np.convolve(mix, ir)[:N]; mix = mix + wet
mix = mix[:int(secs * SR)]; fade = int(0.8 * SR); mix[-fade:] *= np.linspace(1, 0, fade)
mix = mix / (np.abs(mix).max() + 1e-9) * 0.85
stereo = np.stack([mix, np.roll(mix, 220)], 1)
wav_path = os.path.join(os.path.dirname(os.path.abspath(out)), 'trailer_music.wav')
with wave.open(wav_path, 'wb') as w:
    w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR); w.writeframes((stereo * 32767).astype('<i2').tobytes())
subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-framerate', str(FPS), '-i', os.path.join(frames_out, 'o_%05d.jpg'), '-i', wav_path,
                '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-crf', '19', '-preset', 'medium', '-c:a', 'aac', '-b:a', '192k', '-shortest', out], check=True)
print('out', out)
