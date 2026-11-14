"""Two-handed greatsword clips (2026-10-31 user). The one-hand standing poses process.py aligns to don't fit this weapon,
so the reference is 13_idle_greatsword/base_out/<dir>.png (edit_image of the game idle frame, same feet).

  python greatsword.py base                 # base_out -> base/ (palette, specks) = standing pose for every greatsword clip
  python greatsword.py build idle_greatsword [dir ...]   # 01_raw/clean/<clip>/<dir> -> 02_final/<clip>/<dir>.png
"""
import os, sys
import numpy as np
import process as P
from pngio import read_png, write_png

ROOT = os.path.join(P.ROOT, '13_idle_greatsword')


def strip(f, oc):   # sheet_clip.strip (importing sheet_clip reads its own argv)
    m = f[..., 3] > 0
    p = np.pad(m, 1)
    f = f.copy()
    f[m & ~(p[:-2, 1:-1] & p[2:, 1:-1] & p[1:-1, :-2] & p[1:-1, 2:]) & (f[..., :3] == oc).all(-1)] = 0
    return f


def head_top(f, pal):
    """body_top of the shipped (outline-free) frame without the blade: the diagonal greatsword is >= 6px wide above the
    head, so plain body_top measured the blade tip, not the head (run S, 2026-10-31)."""
    g = strip(f, pal[0])
    g[P.steel_mask(g)] = 0
    return P.body_top(g)


def drop_specks(f, pal, max_px=3):
    """Islands of <= max_px on the shipped (outline-free) frame, with their outline ring: a speck touching the body only
    through the ring survives drop_fragments (run W f2 1x3 steel bar under the feet, 2026-10-31)."""
    a = strip(f, pal[0])[..., 3] > 0
    lab = np.zeros(a.shape, int)
    out = f.copy()
    for y, x in zip(*np.nonzero(a)):
        if lab[y, x]:
            continue
        lab[y, x] = 1
        st, comp = [(y, x)], []
        while st:
            p, q = st.pop()
            comp.append((p, q))
            for v in range(max(p - 1, 0), min(p + 2, a.shape[0])):
                for u in range(max(q - 1, 0), min(q + 2, a.shape[1])):
                    if a[v, u] and not lab[v, u]:
                        lab[v, u] = 1
                        st.append((v, u))
        if len(comp) <= max_px:
            m = np.zeros(a.shape, bool)
            m[tuple(np.array(comp).T)] = True
            ring = P.grow(m, 1) & (f[..., :3] == pal[0]).all(-1)
            out[m | ring] = 0
    return out


def clean(img, pal):
    # same steps as process.process_frame: the 1px outline ring is drawn here and stripped on delivery like every clip
    f = P.quantize(img, pal)
    f = P.outline(P.drop_fragments(f, max_px=4, gap=0)[0], pal)[0]
    return P.drop_fragments(f, max_px=4, gap=0)[0]


def clean_blade(f, pal):
    """Keep the generated blade's own shape but fill its nicks/holes and repaint even stripes (light edge toward the
    60-degree light, mid, dark edge), so generation specks and scratches disappear (user 2026-10-31: no scratches on
    the sword). Only steel pixels on the silhouette edge (blade_parts) are touched; guard, grip and armour stay."""
    # edge 0.3: the wide blade's middle stripe is 2px from the air, so fewer of its pixels touch it (N: 0.5 found none)
    parts = P.blade_parts(f, edge=0.3)
    if not parts or len(parts[0]) < 40:
        return f
    c = parts[0]
    mean = c.mean(0)
    d = np.linalg.svd(c - mean)[2][0]
    n = np.array([-d[1], d[0]])
    n = n if n @ P.LIGHT_DIR >= 0 else -n
    t, s = (c - mean) @ d, (c - mean) @ n
    k = np.round(t).astype(int)
    lo = {i: s[k == i].min() for i in np.unique(k)}
    hi = {i: s[k == i].max() for i in np.unique(k)}
    y0, x0 = c.min(0) - 1
    y1, x1 = c.max(0) + 2
    yy, xx = np.mgrid[max(y0, 0):min(y1, P.SIZE), max(x0, 0):min(x1, P.SIZE)]
    pts = np.stack([yy.ravel(), xx.ravel()], 1)
    pt, ps = (pts - mean) @ d, (pts - mean) @ n
    pk = np.round(pt).astype(int)
    out = f.copy()
    for (y, x), kk, ss in zip(pts, pk, ps):
        if kk not in lo or not (lo[kk] - .5 <= ss <= hi[kk] + .5):
            continue
        a = out[y, x]
        if a[3] and not P.steel_mask(out[y:y + 1, x:x + 1])[0, 0] and not (a[:3] == pal[0]).all():
            continue   # gold guard / hands crossing the blade stay
        w = hi[kk] - lo[kk]
        r = (ss - lo[kk]) / w if w > 0 else .5
        col = P.LIGHT_STEEL if r > .67 else (P.DARK_STEEL if r < .34 else P.TIP_STEEL)
        out[y, x] = [*col, 255]
    return P.outline(out, pal)[0]


def base(d):
    return read_png(os.path.join(ROOT, 'base', f'{d}.png'))


def align(raw, ref):
    """Integer shift that puts raw frame 0 (v3 start = ref) exactly on ref: fewest differing pixels."""
    best = None
    for dy in range(-(raw.shape[0] - 40), 24):
        for dx in range(-(raw.shape[1] - 40), 24):
            f, _ = P.place(raw, dx, dy)
            n = int(((f[..., 3] > 127) != (ref[..., 3] > 127)).sum())
            if best is None or n < best[0]:
                best = (n, dx, dy)
    return best


def blade_of(f):
    """(steel pixels of the blade, hilt point) or None. edge 0.3 as in clean_blade."""
    parts = P.blade_parts(f, edge=0.3)
    if not parts or len(parts[0]) < 12:
        return None
    c = parts[0]
    body = (f[..., 3] > 0) & ~P.steel_mask(f)
    mean = c.mean(0)
    v = np.linalg.svd(c - mean)[2][0]
    t = (c - mean) @ v
    ends = [c[t.argmin()], c[t.argmax()]]
    near = lambda e: body[max(e[0] - 2, 0):e[0] + 3, max(e[1] - 2, 0):e[1] + 3].sum()
    return c, max(ends, key=near)


def transplant_blade(f, ref, pal):
    """Erase f's blade (and its outline ring) and draw the base pose's blade at the base position. Returns (frame, True
    when f had no usable blade). Following each frame's hilt moved the blade 0-5px frame to frame and it looked like it
    wobbled (user 2026-10-31), so the blade stays where the base pose holds it in every frame."""
    rb = blade_of(ref)
    if rb is None:
        return f, False
    rc, rh = rb
    fb = blade_of(f)
    dy = dx = 0
    out = f.copy()
    line = (f[..., :3] == pal[0]).all(-1)
    a0 = f[..., 3] > 0
    nb4 = lambda m: (lambda q: q[:-2, 1:-1] | q[2:, 1:-1] | q[1:-1, :-2] | q[1:-1, 2:])(np.pad(m, 1))
    # black is also the body's darkest shade (no outline style): only black that touches the blade and nothing coloured
    # is the blade's own edge. The 2px band erase removed up to 39 shade px per frame and holed the torso (user 2026-10-31)
    coloured = a0 & ~line & ~P.steel_mask(f)
    if fb is not None:
        m = np.zeros(f.shape[:2], bool)
        m[tuple(fb[0].T)] = True
        out[m | (line & nb4(m) & ~nb4(coloured))] = 0
    new = np.zeros(f.shape[:2], bool)
    for y, x in rc:
        v, u = y + dy, x + dx
        if 0 <= v < P.SIZE and 0 <= u < P.SIZE:
            out[v, u] = ref[y, x]
            new[v, u] = True
    # stray bits of the generated tip next to the new blade: dark or steel, touching air, with no coloured body pixel
    # beside them (NE f2/f7, S f2)
    near = P.grow(new, 2) & ~new
    for _ in range(2):
        a = out[..., 3] > 0
        col = a & ~(out[..., :3] == pal[0]).all(-1) & ~P.steel_mask(out) & ~new
        stray = near & a & nb4(~a) & ~nb4(col) & ((P.lum(out[..., :3]) < 60) | P.steel_mask(out))
        out[stray] = 0
    # anything that opened a see-through hole inside the figure goes back to the generated pixel
    o = out[..., 3] > 0
    inside = (np.maximum.accumulate(o, 1) & np.maximum.accumulate(o[:, ::-1], 1)[:, ::-1]
              & np.maximum.accumulate(o, 0) & np.maximum.accumulate(o[::-1], 0)[::-1])
    hole = ~o & inside & a0
    out[hole] = f[hole]
    # grey pieces of the generated blade fused with the hands (S f2, SW f1/f5): steel near the new blade that is not the
    # new blade takes the most common colour of its non-steel neighbours (2026-10-31)
    for _ in range(3):
        left = P.steel_mask(out) & P.grow(new, 4) & ~new
        if not left.any():
            break
        for y, x in zip(*np.nonzero(left)):
            nb = out[max(y - 1, 0):y + 2, max(x - 1, 0):x + 2].reshape(-1, 4)
            nb = nb[(nb[:, 3] > 0) & ~P.steel_mask(nb[None])[0]]
            if len(nb):
                cols, cnt = np.unique(nb[:, :3], axis=0, return_counts=True)
                out[y, x, :3] = cols[cnt.argmax()]
    out = P.drop_fragments(P.outline(out, pal)[0], max_px=4, gap=0)[0]
    # the blade is painted last: outline() above blackened blade edge pixels next to the body ("blade painted black",
    # user 2026-10-31)
    out[rc[:, 0], rc[:, 1]] = ref[rc[:, 0], rc[:, 1]]
    return out, fb is None


def cmd_base(pal):
    # base/ = standing pose with outline ring (built like every frame); start/ = the same outline-free, given to v3 as
    # start and end frame so generation begins from the clean blade
    for sub in ('base', 'start'):
        os.makedirs(os.path.join(ROOT, sub), exist_ok=True)
    for d in P.DIRS:
        b = clean_blade(clean(read_png(os.path.join(ROOT, 'base_out', f'{d}.png')), pal), pal)
        write_png(os.path.join(ROOT, 'base', f'{d}.png'), b)
        write_png(os.path.join(ROOT, 'start', f'{d}.png'), strip(b, pal[0]))
    print('base: 8 standing poses')


def cmd_build(clip, dirs, pal):
    n, fps, loop, drop = P.CLIPS[clip]
    os.makedirs(os.path.join(P.FINAL, clip), exist_ok=True)
    for d in dirs:
        raws = P.raw_frames(clip, d)
        ref = base(d)
        miss, dx, dy = align(raws[0], read_png(os.path.join(ROOT, 'start', f'{d}.png')))
        out = [clean(P.place(r, dx, dy)[0], pal) for r in raws[drop:drop + n]]
        # spec: torso 0px up/down. v3 breathing lifts the head up to 4px -> pin to the base pose (outline is stripped on delivery)
        # measured on the outline-free frame (what ships): with the ring the hair top reads 1-3px off (run S f5, 2026-10-31)
        top = head_top(ref, pal)
        out = [clean_blade(P.pin_head(f, P.body_top(f) - (head_top(f, pal) - top), pal), pal) for f in out]
        # one blade design in every frame (user 2026-10-31: blade broke mid-way, NW worst; v3 also thins or drops it):
        # the generated blade is erased and the base pose's blade is drawn at the base position
        out, lost = zip(*[transplant_blade(f, ref, pal) for f in out])
        out, lost = list(out), [i for i, l in enumerate(lost) if l]
        # specks with their outline ring (~12px) left by a dropped blade (run W f1/f2 3px bar under the feet, 2026-10-31)
        out = [drop_specks(P.drop_fragments(f, max_px=12, gap=0)[0], pal) for f in out]
        if loop:   # v3 ends on the start pose: that frame is the exact standing pose
            out[-1] = ref
        write_png(os.path.join(P.FINAL, clip, f'{d}.png'), np.concatenate(out, 1))
        print(f'{clip}/{d}: shift {dx},{dy} (frame0 vs base: {miss} px differ), blade pasted in f{lost}')


POWER = 0.7


def sway(f, th, pal, amp):
    """process.sway_cape; side views (E/W) have no wide free drape it accepts, so there the cape below the shoulders
    moves. power 0.7: the whole cape swings, not just the hem (user 2026-10-31, like run NW 2026-10-30)."""
    out = P.sway_cape(f, th, pal, amp, 0.0, 0.6, power=POWER)
    if (out != f).any():
        return out
    red = P.cape_px(f)
    rows = np.nonzero(red.any(1))[0]
    if not len(rows):
        return f
    red[:rows.min() + (rows.max() - rows.min()) // 4] = False   # the top quarter is worn on the shoulders
    layer = np.zeros_like(f)
    layer[red] = f[red]
    moved = P.cape_flutter(layer, th, amp=amp, power=POWER, min_row=1)
    out = f.copy()
    out[red] = 0
    under = (moved[..., 3] > 0) & (out[..., 3] == 0)
    out[under] = moved[under]
    o = out[..., 3] > 0
    hole = ~o & (f[..., 3] > 0) & np.maximum.accumulate(o, 1) & np.maximum.accumulate(o[:, ::-1], 1)[:, ::-1]
    out[hole] = f[hole]
    return P.drop_fragments(P.outline(out, pal)[0], max_px=4, gap=0)[0]


def cmd_still(clip, dirs, pal, amp=1.5):
    """Body and sword are the base pose in every frame; only the free-hanging cape sways amp px (process.sway_cape).
    v3 re-draws armour and hands every frame and spreads the cape (south x2, 2026-10-31), so the idle is built like
    the game's idle_calm (body frozen, user 2026-10-29). Last frame = base pose so the loop closes."""
    n = P.CLIPS[clip][0]
    os.makedirs(os.path.join(P.FINAL, clip), exist_ok=True)
    for d in dirs:
        ref = base(d)
        out = [sway(ref, 2 * np.pi * (i + 1) / n, pal, amp) for i in range(n - 1)] + [ref]
        write_png(os.path.join(P.FINAL, clip, f'{d}.png'), np.concatenate(out, 1))
        print(f'{clip}/{d}: still, cape px changed per frame', [int((f != ref).any(-1).sum()) for f in out])


def cape_from(f, ref, pal, r=2):
    """Whole cape follows the generated frame: inside the base cape (grown r px) every cape/outline/empty pixel is taken
    from f, so folds inside the cape and its upper part move too, not only the hem edge (user 2026-10-31: E, NE, N, NW,
    SW capes stood still; freeze_body moved 11-18% of the N/NE/NW cape). Body, hair and sword pixels never change."""
    line = lambda img: (img[..., 3] > 0) & (img[..., :3] == pal[0]).all(-1)
    cr = P.cape_px(ref)
    zone = P.grow(cr, r) & (np.arange(P.SIZE)[:, None] < P.ANCHOR[1] - 3)   # ground rows stay: feet anchor
    take = zone & ((ref[..., 3] == 0) | line(ref) | cr) & ((f[..., 3] == 0) | line(f) | P.cape_px(f))
    out = ref.copy()
    out[take] = f[take]
    # the generated frame's own dark edge would stack on the redrawn outline as a 2px black seam (NW f2, 2026-10-31):
    # take only its cape colour and empty space; outline() draws a single ring afterwards
    out[take & line(f)] = 0
    # a pixel the cape left that still has the figure on both sides (row and column) is a see-through gap between cape
    # and body: keep the base pixel (E f1 / NW f2 opened 6-9 px gaps with an outline ring, 2026-10-31)
    o = out[..., 3] > 0
    inside = (np.maximum.accumulate(o, 1) & np.maximum.accumulate(o[:, ::-1], 1)[:, ::-1]
              & np.maximum.accumulate(o, 0) & np.maximum.accumulate(o[::-1], 0)[::-1])
    hole = ~o & inside & (ref[..., 3] > 0)
    out[hole] = ref[hole]
    return P.drop_fragments(P.outline(out, pal)[0], max_px=4, gap=0)[0]


def cmd_freeze(clip, dirs, pal, cape=2):
    """Like the game idle (idle_calm FREEZE, 2026-10-29): v3 "almost still" frames, body/head/sword = base pose, only the
    cape's lower-half edge band (+-cape px) follows the generated frame, so the folds shift smoothly (user 2026-10-31:
    the row-shift sway looked stiff; reference = game idle W). Hair stays (hair=0)."""
    n, fps, loop, drop = P.CLIPS[clip]
    os.makedirs(os.path.join(P.FINAL, clip), exist_ok=True)
    for d in dirs:
        raws = P.raw_frames(clip, d)
        ref = base(d)
        miss, dx, dy = align(raws[0], read_png(os.path.join(ROOT, 'start', f'{d}.png')))
        out = [cape_from(clean(P.place(r, dx, dy)[0], pal), ref, pal, cape)
               for r in raws[drop:drop + n]]
        if loop:
            out[-1] = ref
        write_png(os.path.join(P.FINAL, clip, f'{d}.png'), np.concatenate(out, 1))
        print(f'{clip}/{d}: shift {dx},{dy} (frame0 vs start {miss} px), changed px', [int((f != ref).any(-1).sum()) for f in out])


if __name__ == '__main__':
    pal = P.load_palette()
    if sys.argv[1] == 'base':
        cmd_base(pal)
    elif sys.argv[1] == 'freeze':
        cmd_freeze(sys.argv[2], sys.argv[3:] or P.DIRS, pal)
    elif sys.argv[1] == 'still':
        cmd_still(sys.argv[2], sys.argv[3:] or P.DIRS, pal)
    else:
        cmd_build(sys.argv[2], sys.argv[3:] or P.DIRS, pal)
