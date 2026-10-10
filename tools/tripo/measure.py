# landmarks of a T-pose Tripo suit (Blender z-up, front -y): returns dict
import numpy as np
def measure(P):
    # P: (n,3) world positions
    P = P.copy(); P[:, 2] -= P[:, 2].min(); cx = (P[:, 0].min() + P[:, 0].max()) / 2; P[:, 0] -= cx
    H = P[:, 2].max(); ax = np.abs(P[:, 0]); R = ax.max()
    # arm line: median height of slices across the arm span
    xs, zs = [], []
    for x0 in np.linspace(0.45 * R, 0.85 * R, 9):
        m = (ax > x0 - 0.01) & (ax < x0 + 0.01) & (P[:, 2] > 0.45 * H)
        if m.sum() > 30: xs.append(x0); zs.append(np.median(P[m, 2]))
    xs, zs = np.array(xs), np.array(zs)
    b, a = np.polyfit(xs, zs, 1) if len(xs) > 2 else (0.0, 0.72 * H)
    # the hands (the outermost tips) and the top of the upper arm set the slope: a bell sleeve hanging below the arm
    # would drag a plain middle line down near the body
    mh = (ax > 0.93 * R) & (P[:, 2] > 0.45 * H)
    m1 = (ax > 0.43 * R) & (ax < 0.5 * R) & (P[:, 2] > 0.45 * H)
    if mh.sum() > 20 and m1.sum() > 20:
        zh = float(np.median(P[mh, 2])); z1 = float(np.percentile(P[m1, 2], 80)) - 0.022
        b = (zh - z1) / (0.95 * R - 0.465 * R); a = z1 - b * 0.465 * R
    body_w = 0.0
    # body half width at chest: the narrowest |x| at 0.9*arm height excluding arms... take shoulder joint at 0.14*H
    sx = 0.13 * H
    shoulder = a + b * sx
    # feet
    f = P[P[:, 2] < 0.03 * H]
    fl = np.median(f[f[:, 0] > 0][:, 0]) if (f[:, 0] > 0).sum() > 10 else 0.06
    fr = np.median(f[f[:, 0] < 0][:, 0]) if (f[:, 0] < 0).sum() > 10 else -0.06
    # crotch: highest z (below 0.65 shoulder) with an empty column at the middle, scanning up from the ankles
    crotch = None
    mid = (np.abs(P[:, 0]) < 0.012 * H / 0.98)
    for z in np.arange(0.08 * H, 0.62 * shoulder, 0.004):
        m = mid & (np.abs(P[:, 2] - z) < 0.003)
        if m.sum() == 0: crotch = z
        elif crotch is not None and z - crotch > 0.02: break
    return dict(cx=float(cx), H=float(H), R=float(R), slope=float(b), shoulder=float(shoulder), foot_l=float(fl), foot_r=float(fr),
                crotch=None if crotch is None else float(crotch), armz_at_R=float(a + b * R))
