"""Left-pauldron greatsword breathing idle (2026-11-02 user brief): 8 directions generated directly (no mirroring,
the pauldron would land on the right shoulder), v3 start = end = 15_idle_greatsword_pauldron/<dir>.png.

  python breath_pauldron.py --check   # inspect only (use after Aseprite edits; never rebuild an edited clip)
  python breath_pauldron.py     # 01_raw/clean/idle_greatsword_pauldron_breath -> 16_idle_greatsword_pauldron_breath/<n>_<dir>.png
Sheet: 8 frames x 64px, one row, spacing 0, padding 0 (brief). No outline (same as the reference).
"""
import os
import numpy as np
import process as P
import greatsword as G
from pngio import read_png, write_png

CLIP = 'idle_greatsword_pauldron_breath'
REF = os.path.join(P.ROOT, '15_idle_greatsword_pauldron')
OUT = os.path.join(P.ROOT, '16_idle_greatsword_pauldron_breath')
FILES = ['0_east', '1_north-east', '2_north', '3_north-west', '4_west', '5_south-west', '6_south', '7_south-east']
# E: v3 dropped the blade twice -> 1st take's body/cape; every direction gets the reference blade (paste_blade)
RAW = {'east': os.path.join(P.ROOT, '01_raw', 'trials', 'idle_greatsword_pauldron_breath_sword_lost', 'east')}
MAX_RISE = 1   # brief: torso at most 1px from neutral


def head_top(f):
    g = f.copy()
    g[P.steel_mask(g)] = 0          # the blade rises above the head in most directions
    return P.body_top(g)


def cap_rise(f, top):
    """Head higher than top - MAX_RISE: drop the rows above mid-body down so the rise is MAX_RISE; feet stay."""
    k = top - head_top(f) - MAX_RISE
    if k <= 0:
        return f
    seam = (top + P.ANCHOR[1]) // 2
    out = f.copy()
    out[k:seam] = f[:seam - k]
    out[:k] = 0
    return out


def plant_feet(f, ref, rows=3):
    """Brief: feet at identical ground coordinates. v3 lifted the whole figure 1-2px (E f2-f5) or one foot (SW f7):
    drop the frame back onto the ref's ground row, then take the ref's bottom rows if the contact still differs."""
    low = lambda g: np.nonzero(g[..., 3].any(1))[0].max()
    k = low(ref) - low(f)
    if k > 0:
        f = np.concatenate([np.zeros_like(f[:k]), f[:-k]])
    y = low(ref) - rows + 1
    if not np.array_equal(f[y:] [..., 3] > 0, ref[y:][..., 3] > 0):
        f = f.copy()
        f[y:] = ref[y:]
    return f


HAIR = ['fcdb6d', 'c39650', 'ae783a', '91511b', 'fde6bd']   # palette hair light..shadow, highlight
HEAD_ROWS = 13


def hair_mask(ref):
    """Hair connected to the head top, within HEAD_ROWS rows (gold armour below shares these colours)."""
    hx = np.isin(np.array(['%02x%02x%02x' % tuple(p) for p in ref[..., :3].reshape(-1, 3)]).reshape(ref.shape[:2]), HAIR)
    hx &= ref[..., 3] > 0
    top = head_top(ref)
    hx[top + HEAD_ROWS:] = False
    m = np.zeros_like(hx)
    st = [(top, x) for x in np.nonzero(hx[top])[0]]
    while st:
        y, x = st.pop()
        if 0 <= y < P.SIZE and 0 <= x < P.SIZE and hx[y, x] and not m[y, x]:
            m[y, x] = True
            st += [(y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)]
    return m


def freeze_hair(f, ref, mask):
    """User 2026-11-02: no hair sway. The ref's hair (+1px ring) moves only with the frame's breathing rise; the sword
    in front stays as generated."""
    r = head_top(ref) - head_top(f)
    src = np.roll(ref, -r, 0)
    m = np.roll(P.grow(mask, 1), -r, 0)
    m &= ~P.steel_mask(f)
    out = f.copy()
    out[m] = src[m]
    return out


def paste_blade(f, ref):
    """User 2026-11-02: the blade moves rigidly with the body (up/down by the frame's breathing rise), no redrawing.
    The generated blade is erased; body pixels it covered come back from their neighbours."""
    r = head_top(ref) - head_top(f)
    rb, fb = G.blade_of(ref), G.blade_of(f)
    ys, xs = rb[0][:, 0] - r, rb[0][:, 1]
    new = np.zeros(f.shape[:2], bool)
    new[ys, xs] = True
    out = f.copy()
    if fb is not None:
        gone = np.zeros_like(new)
        gone[tuple(fb[0].T)] = True
        gone &= ~new
        out[gone] = 0
        o = out[..., 3] > 0
        inside = (np.maximum.accumulate(o, 1) & np.maximum.accumulate(o[:, ::-1], 1)[:, ::-1]
                  & np.maximum.accumulate(o, 0) & np.maximum.accumulate(o[::-1], 0)[::-1])
        for _ in range(3):
            for y, x in zip(*np.nonzero(gone & inside & (out[..., 3] == 0))):
                nb = out[max(y - 1, 0):y + 2, max(x - 1, 0):x + 2].reshape(-1, 4)
                nb = nb[(nb[:, 3] > 0) & ~P.steel_mask(nb[None])[0]]
                if len(nb):
                    c, n = np.unique(nb, axis=0, return_counts=True)
                    out[y, x] = c[n.argmax()]
    # guard, grip and the hands on it move with the blade too (user 2026-11-02: hilt moved on its own)
    h = hilt_mask(ref)
    hy, hx = np.nonzero(h)
    keep = (hy - r >= 0) & (hy - r < P.SIZE)
    out[hy[keep] - r, hx[keep]] = ref[hy[keep], hx[keep]]
    out[ys, xs] = ref[rb[0][:, 0], xs]
    return out


HILT_LEN, HILT_HALF = 9, 4   # px past the blade's hilt end along its axis, half width across it


def hilt_mask(ref):
    """Strip of the ref beyond the blade's hilt end: guard, grip, gripping hands (transparent pixels included, so a
    generated hilt that drifted into the air is cleared)."""
    c, hilt = G.blade_of(ref)
    tip = c[np.argmax(((c - hilt) ** 2).sum(1))]
    v = (hilt - tip) / np.linalg.norm(hilt - tip)
    yy, xx = np.mgrid[:P.SIZE, :P.SIZE]
    p = np.stack([yy - hilt[0], xx - hilt[1]], -1)
    t = p @ v
    s = np.abs(p @ np.array([-v[1], v[0]]))
    return (t >= -1) & (t <= HILT_LEN) & (s <= HILT_HALF)


def build(pal):
    os.makedirs(OUT, exist_ok=True)
    for name in FILES:
        d = name.split('_', 1)[1]
        ref = read_png(os.path.join(REF, f'{name}.png'))
        folder = RAW.get(d, os.path.join(P.RAW, CLIP, d))
        raws = [read_png(os.path.join(folder, f)) for f in sorted(os.listdir(folder)) if f.endswith('.png')]
        miss, dx, dy = G.align(raws[0], ref)
        assert miss == 0, (d, miss)
        # frames 0..7: frame 8 is the end pose = frame 0, dropped so the loop does not hold it twice
        gen = [plant_feet(P.quantize(P.place(r, dx, dy)[0], pal), ref) for r in raws[:8]]
        if d in USER_POSE:   # aligned on the untouched ref above; the breath is built from the user's edited pose
            ref = read_png(USER_POSE[d])
        # v4 (generated body + pasted blade/hair) flapped the cape 100-237 px and breathed out of phase per direction
        # (user 2026-11-02 "망토 줄이기"): every frame is now the ref lifted by RISE, only the cape hem follows v3
        fr = [hem_from(g, breathe(ref, r), ref) for g, r in zip(gen, RISE)]
        fr[0] = ref
        write_png(os.path.join(OUT, f'{name}.png'), np.concatenate(fr, 1))
        print(f'{d}: shift {dx},{dy}, px changed vs ref {[int((f != ref).any(-1).sum()) for f in fr]}')


# NE sleeve: user's Aseprite edit of v4 frame 1 (= neutral pose, 15 px: black/cream -> sleeve brown, 2026-11-02)
USER_POSE = {'north-east': os.path.join(P.ROOT, '01_raw', 'trials', 'idle_greatsword_pauldron_breath_v4_cape_flare',
                                        '1_north-east1.png')}
RISE = [0, 0, 1, 1, 1, 1, 0, 0]   # one breathing phase for all directions (N's curve, user 2026-11-02)
CAPE = np.array([[0x40, 0x0b, 0x18], [0x78, 0x15, 0x24]])   # palette cape reds
HEM = 1   # px the cape edge may move


def lift_line(ref):
    """Rows above this rise with the breath: mid-body, or below the weapon so blade, hilt and hands move as one."""
    c = G.blade_of(ref)[0]
    w = hilt_mask(ref) & (ref[..., 3] > 0)
    w[c[:, 0], c[:, 1]] = True
    return max((head_top(ref) + P.ANCHOR[1]) // 2, int(np.nonzero(w.any(1))[0].max()) + 1)


def breathe(ref, r):
    """Upper body + weapon + hair lifted r px; the row at the lift line repeats (1px stretch), legs and feet stay."""
    out = ref.copy()
    if r:
        y = lift_line(ref)
        out[:y - r] = ref[r:y]
    return out


def hem_from(g, f, ref):
    """Cape edge band (+-HEM px around f's cape, below the lift line, above the feet) takes the generated frame's cape
    or air; generated black edges and everything else stay as f. Gaps that open inside the figure are refilled."""
    cape = lambda img: (img[..., 3] > 0) & (img[..., None, :3] == CAPE).all(-1).any(-1)
    cf = cape(f)
    band = P.grow(cf, HEM) & P.grow(~cf, HEM)
    band[:lift_line(ref)] = False
    band[P.ANCHOR[1] - 4:] = False
    out = f.copy()
    take = band & cape(g) & (cf | (f[..., 3] == 0))   # never paint over body, legs or weapon
    out[take] = g[take]
    drop = band & (g[..., 3] == 0) & cf
    out[drop] = 0
    o = out[..., 3] > 0
    inside = (np.maximum.accumulate(o, 1) & np.maximum.accumulate(o[:, ::-1], 1)[:, ::-1]
              & np.maximum.accumulate(o, 0) & np.maximum.accumulate(o[::-1], 0)[::-1])
    hole = ~o & inside & (f[..., 3] > 0)
    out[hole] = f[hole]
    return out


def check():
    """Delivery checks on the sheets as they are (also after Aseprite edits). Exit code 1 on failure."""
    cols, fail = set(), False
    for name in FILES:
        s, ref = read_png(os.path.join(OUT, f'{name}.png')), read_png(os.path.join(REF, f'{name}.png'))
        fr = [s[:, i * 64:(i + 1) * 64] for i in range(s.shape[1] // 64)]
        al = set(int(v) for v in np.unique(s[..., 3]))
        bbs = []
        for f in fr:
            a = f[..., 3] > 0
            ys, xs = np.nonzero(a)
            bbs.append((xs.min(), ys.min(), xs.max(), ys.max()))
            cols |= {tuple(c) for c in f[a][:, :3]}
        feet = max(int(((f[52:, ..., 3] > 0) != (ref[52:, ..., 3] > 0)).sum()) for f in fr)
        rise = [head_top(ref) - head_top(f) for f in fr]
        w = max(b[2] - b[0] + 1 for b in bbs)
        h = max(b[3] - b[1] + 1 for b in bbs)
        mg = min(min(b[0], b[1], 63 - b[2], 63 - b[3]) for b in bbs)
        print(f'{name:14s} sheet {s.shape[1]}x{s.shape[0]} frames {len(fr)} alpha {sorted(al)} f0=ref {np.array_equal(fr[0], ref)} '
              f'bbox max {w}x{h} margin min {mg} feet(rows52-54) diff {feet} head rise {rise}')
        fail |= s.shape[:2] != (64, 512) or bool(al - {0, 255}) or feet > 0 or max(map(abs, rise)) > MAX_RISE
    print('colors total', len(cols))
    fail |= len(cols) > 16
    print('exit', int(fail))
    return int(fail)


if __name__ == '__main__':
    import sys
    if sys.argv[1:] == ['--check']:
        sys.exit(check())
    build(P.load_palette())
