"""Hair sway numbers per direction from 09_idle_calm sheets: head rows (top..top+10), per frame vs frame 0.
  python hair_motion.py   -> added/removed px, which side of the head moved (screen x of changed px vs head centre)."""
import numpy as np
import process as P
from pngio import read_png

DIRS = ['east', 'north-east', 'north', 'north-west', 'west', 'south-west', 'south', 'south-east']


def motion(path):
    s = read_png(path)
    fr = [s[:, i * 68:i * 68 + 64] for i in range(4)]
    t = P.bbox(fr[0])[1]
    a0 = fr[0][t:t + 11, :, 3] > 0
    xs = np.nonzero(a0.any(0))[0]
    cx = (xs.min() + xs.max()) / 2
    out = []
    for f in fr[1:]:
        a = f[t:t + 11, :, 3] > 0
        ch = np.any(f[t:t + 11] != fr[0][t:t + 11], -1)
        add, rem = int((a & ~a0).sum()), int((a0 & ~a).sum())
        ys, xx = np.nonzero(ch)
        side = float(np.round((xx.mean() - cx), 1)) if len(xx) else 0.0
        top = float(np.round(ys.mean(), 1)) if len(ys) else 0.0
        out.append(dict(changed=int(ch.sum()), add=add, rem=rem, side=side, row=top))
    return out


if __name__ == '__main__':
    import sys
    sub = sys.argv[1] if len(sys.argv) > 1 else ''
    for n, d in enumerate(DIRS):
        m = motion(f'{P.ROOT}/09_idle_calm/{sub}{n}_{d}.png')
        print(f'{d:11s}', ' | '.join(f"ch{x['changed']:3d} +{x['add']:2d}/-{x['rem']:2d} side{x['side']:+5.1f} row{x['row']:4.1f}" for x in m))


def distance(m, ref):
    """How far a direction's hair sway is from the reference (SW) sway: per-frame px counts + side of the head."""
    return sum(abs(a['changed'] - b['changed']) + abs(a['add'] - b['add']) + abs(a['rem'] - b['rem'])
               + 2 * abs(abs(a['side']) - abs(b['side'])) for a, b in zip(m, ref))
