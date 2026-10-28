"""Player sprite post-process (SPEC.md). No PIL: pngio (zlib+numpy).

  python process.py palette            # lock palette.hex from base rotations (once)
  python process.py build              # all clips: place -> quantize -> clean -> outline -> checks -> sheets/previews
Raw layout: 01_raw/base/rotations/<dir>.png, 01_raw/base/<clip>/<dir>/frame_XX.png
"""
import json, os, struct, sys, zlib
import numpy as np
from pngio import read_png, write_png, upscale

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RAW = os.path.join(ROOT, '01_raw', 'clean')   # 2026-10-28 state: no ahoge, intact cape, gold sun
FINAL = os.path.join(ROOT, '02_final')
PREVIEW = os.path.join(ROOT, '03_preview')
DIRS = ['south', 'south-east', 'east', 'north-east', 'north', 'north-west', 'west', 'south-west']
CLIPS = {  # name: (frames, fps, loop, frames dropped from raw start)
    'idle': (4, 6, True, 1), 'run': (8, 12, True, 1),
    'attack': (6, 12, False, 1), 'dodge': (6, 12, False, 1),
    'hurt': (4, 16, False, 1),   # spec asked 3f@12fps; v3 needs even >= 4 -> 4f@16fps = 0.25 s (user, 2026-10-28)
    'death': (6, 10, False, 1),  # one-shot, ends lying down (game: PlayHold)
    'idle_alert': (4, 6, True, 1),    # sword-ready guard idle, delivery only (07_idle_alert, 2026-10-28)
    'idle_breath': (4, 6, True, 1),   # breathing idle from right-hand start poses (08_idle_breath, 2026-10-29)
    'idle_calm': (4, 6, True, 1)}     # still guard idle, only hair tips + cape hem sway (09_idle_calm, 2026-10-29)
# standing poses that clips are aligned to: right-hand sword versions (W/NW/SE edited, S flipped; user, 2026-10-29)
ROTATIONS = 'idle_breath_start'
SIZE, ANCHOR = 64, (32, 56)       # anchor = ground contact; feet bottom pixel row = 55
MAX_COLORS = 16                   # incl. black outline (pixel-art standard, 2026-10-27)
W = np.array([2.0, 4.0, 3.0])    # perceptual-ish RGB weights for nearest colour
BG = (62, 78, 70)


def lum(c):
    c = np.asarray(c, float)
    return c[..., 0] * .299 + c[..., 1] * .587 + c[..., 2] * .114


# ---------- placement (fixed per-direction offset, scale 1, no per-frame crop) ----------
def feet_anchor(img):
    a = img[..., 3] > 127
    ys = np.nonzero(a.any(1))[0]
    bot = ys.max()
    xs = np.nonzero(a[bot - 1:bot + 1].any(0))[0]
    return (xs.min() + xs.max() + 1) // 2, bot


def planted_feet(img):
    """Feet contact point: bottom row, runs of >= 3 px (sword tip / cape specks ignored). Returns (x, ground y)."""
    a = img[..., 3] > 0
    bot = np.nonzero(a.any(1))[0].max()
    row = np.r_[0, a[bot].astype(int), 0]
    st, en = np.nonzero(np.diff(row) == 1)[0], np.nonzero(np.diff(row) == -1)[0]
    runs = [(s, e) for s, e in zip(st, en) if e - s >= 3] or list(zip(st, en))
    return int((runs[0][0] + runs[-1][1]) // 2), int(bot + 1)


# frames whose feet must stay where the standing pose has them (ArtSpecification 1-6): clip -> frame indices (None = all)
PLANTED = {'hurt': None, 'death': [0], 'idle_alert': None, 'idle_breath': None, 'idle_calm': None}
SNAP_MAX = 2   # px; larger drift is a real pose change, not generation noise

# every generated idle/N take bobbed the head 2-5 px (2026-10-28): rotation pose held, only the cape hem sways (user)
STILL = {'idle': ['north'], 'idle_alert': ['north']}
# the whole cape swings like a pendulum; 8 small steps instead of 4 so it doesn't stutter (user, 2026-10-29)
STILL_FRAMES = 8

# sword belongs in the right hand; W-side art mirrors E so these held it in the left (user, 2026-10-29).
# S faces the camera, so a horizontal flip puts the sword in the right hand without regenerating.
MIRROR = {'idle_alert': ['south'], 'run': ['south'], 'attack': ['south'], 'dodge': ['south'],
          'hurt': ['south'], 'death': ['south']}   # all clips right-handed (user, 2026-10-29)

# head/torso height pinned to the standing pose; dodge/death drop the body on purpose, run keeps its stride bob (user, 2026-10-29)
STABLE = ('idle_breath', 'idle_calm', 'attack', 'hurt')
STABLE_MAX = 4   # px; a larger difference is a real pose change (raised arm, lean), not generation bob


# v3 drops the NW idle sword in some frames (every take, 2026-10-29): paint the start pose's blade onto each frame
SWORD_FROM_REF = {('idle_breath', 'north-west')}

# body, head and sword stay exactly the start pose; only the silhouette edge of the cape hem / hair tips
# follows the generated frame, at most cape/hair px in or out (user, 2026-10-29)
FREEZE = {'idle_calm': dict(cape=2, hair=1)}


def steel_mask(img):
    c = img[..., :3].astype(int)
    return (img[..., 3] > 0) & (c.max(-1) - c.min(-1) < 25) & (lum(c) > 60)


def lost_sword(f, ref):
    """Near-start pose whose blade is (mostly) gone: v3 sometimes draws it as a 1px dark line that cleanup erases."""
    a, b = f[..., 3] > 0, ref[..., 3] > 0
    return steel_mask(f).sum() < 0.4 * steel_mask(ref).sum() and (a & b).sum() / max(1, (a | b).sum()) >= 0.7


def paste_sword(frames, ref, pal):
    """Copy the grey blade (and its 1px outline) of the reference pose onto every frame."""
    steel = steel_mask(ref)
    m = np.pad(steel, 1)
    near = m[:-2, 1:-1] | m[2:, 1:-1] | m[1:-1, :-2] | m[1:-1, 2:] | steel
    mask = near & (ref[..., 3] > 0) & (steel | (ref[..., :3] == pal[0]).all(-1))
    out = []
    for f in frames:
        f = f.copy()
        f[mask] = ref[mask]
        out.append(outline(f, pal)[0])
    return out


def body_top(img):
    return int(np.nonzero((img[..., 3] > 0).sum(1) >= 6)[0][0])   # first row >= 6 px wide (hair spikes excluded)


def pin_head(f, top, pal):
    """Move everything above mid-body so body_top == top: the torso stretches/compresses by the difference
    at the seam while the legs and feet stay put; then redraw the outline across the seam."""
    k = body_top(f) - top
    if k == 0 or abs(k) > STABLE_MAX:
        return f
    seam = (body_top(f) + ANCHOR[1]) // 2
    out = f.copy()
    if k > 0:                                    # head too low: lift, repeating the k rows above the seam
        out[:seam - k] = f[k:seam]
    else:                                        # head too high: lower, dropping |k| rows above the seam
        out[-k:seam] = f[:seam + k]
        out[:-k] = 0
    return outline(out, pal)[0]


def grow(m, r):
    for _ in range(r):
        p = np.pad(m, 1)
        m = m | p[:-2, 1:-1] | p[2:, 1:-1] | p[1:-1, :-2] | p[1:-1, 2:]
    return m


def freeze_body(f, ref, pal, cape, hair):
    """Start pose everywhere except an edge band (cape lower half: cape px, hair rows: hair px) where the
    generated frame's cape/hair/outline/empty pixels are taken; legs, skin, armour and sword never move."""
    a = ref[..., 3] > 0
    c, cf = ref[..., :3].astype(int), f[..., :3].astype(int)
    red = lambda c, al: al & (c[..., 0] > c[..., 1] + 40) & (c[..., 0] > 60)
    gold = lambda c, al: al & (c[..., 0] > 150) & (c[..., 1] > 110) & (c[..., 2] < c[..., 1] - 30)
    line = lambda img: (img[..., 3] > 0) & (img[..., :3] == pal[0]).all(-1)
    soft_ref = ~a | line(ref)
    soft_f = (f[..., 3] == 0) | line(f)
    rows = np.nonzero(red(c, a).sum(1) >= 4)[0]
    top = body_top(ref)
    y = np.arange(SIZE)[:, None]
    zones = []
    if len(rows):
        mid = (rows.min() + rows.max()) // 2
        band = grow(a, cape) & grow(~a, cape) & (y >= mid)
        zones.append((band, soft_ref | red(c, a), soft_f | red(cf, f[..., 3] > 0)))
    band = grow(a, hair) & grow(~a, hair) & (y <= top + 10)
    zones.append((band, soft_ref | gold(c, a), soft_f | gold(cf, f[..., 3] > 0)))
    out = ref.copy()
    for band, ok_ref, ok_f in zones:
        take = band & ok_ref & ok_f
        out[take] = f[take]
    out = outline(out, pal)[0]
    return drop_fragments(out, max_px=4, gap=0)[0]


def mirror_frames(frames):
    """Flip frames horizontally, shifted so the flipped legs (bottom 8 rows) overlap the original best."""
    flipped = [np.flip(f, 1) for f in frames]
    a, b = frames[0][-16:-8, :, 3] > 0, flipped[0][-16:-8, :, 3] > 0
    s = max(range(-4, 5), key=lambda s: (a & np.roll(b, s, 1)).sum())
    return [np.roll(f, s, 1) for f in flipped]


def cape_flutter(img, th, amp=2.0, spread=0.0, lag=0.6):
    """Bend the cape below the shoulders at phase th: rows move sideways by sin*amp and widen by cos*spread,
    both growing toward the hem (0 at the shoulders), resampled nearest so the palette and outline survive.
    lag (radians at the hem) delays lower rows so the hem trails the shoulders."""
    c = img[..., :3].astype(int)
    red = (img[..., 3] > 0) & (c[..., 0] > c[..., 1] + 40) & (c[..., 0] > 60)
    rows = np.nonzero(red.sum(1) >= 10)[0]
    top, hem = rows.min(), rows.max() + 1          # +1: outline row under the hem
    out = img.copy()
    xs = np.arange(img.shape[1])
    for y in range(top, hem + 1):
        w = ((y - top) / (hem - top)) ** 1.5
        cols = np.nonzero(img[y, :, 3] > 0)[0]
        cx = (cols.min() + cols.max()) / 2
        s, f = np.sin(th - lag * w), np.cos(th - lag * w)
        src = np.round(cx + (xs - cx - s * amp * w) / (1 + f * spread * w)).astype(int)
        ok = (src >= 0) & (src < img.shape[1])
        row = np.zeros_like(img[y])
        row[ok] = img[y, src[ok]]
        out[y] = row
    return out


def place(img, dx, dy):
    out = np.zeros((SIZE, SIZE, 4), np.uint8)
    h, w = img.shape[:2]
    y0, x0 = max(0, dy), max(0, dx)
    y1, x1 = min(SIZE, dy + h), min(SIZE, dx + w)
    out[y0:y1, x0:x1] = img[y0 - dy:y1 - dy, x0 - dx:x1 - dx]
    clipped = int((img[..., 3] > 127).sum() - (out[..., 3] > 127).sum())
    return out, clipped


def rotation_offset(d):
    rot = read_png(os.path.join(RAW, ROTATIONS, f'{d}.png'))
    fx, fy = feet_anchor(rot)
    return rot, ANCHOR[0] - fx, ANCHOR[1] - 1 - fy


def clip_offset(rot, rdx, rdy, raws):
    """One offset per clip. Same canvas (template clips): rotation x, and the clip's lowest foot on the ground row.
    Grown canvas (v3 clips keep the rotation as raw frame 0): best alpha overlap of frame 0 with the rotation."""
    frame0 = raws[0]
    if frame0.shape == rot.shape:
        low = max(feet_anchor(r)[1] for r in raws)
        return rdx, ANCHOR[1] - 1 - low, 'same-canvas, ground row'
    ref = np.zeros((SIZE + 64, SIZE + 64), bool)
    ref[32 + rdy:32 + rdy + rot.shape[0], 32 + rdx:32 + rdx + rot.shape[1]] = rot[..., 3] > 127
    a = frame0[..., 3] > 127
    h, w = a.shape
    best = None
    for dy in range(-h + 8, SIZE - 8):
        for dx in range(-w + 8, SIZE - 8):
            if not (0 <= dy + 32 and dy + 32 + h <= ref.shape[0] and 0 <= dx + 32 and dx + 32 + w <= ref.shape[1]):
                continue
            win = ref[dy + 32:dy + 32 + h, dx + 32:dx + 32 + w]
            s = (win & a).sum() / max(1, (win | a).sum())
            if best is None or s > best[0]:
                best = (s, dx, dy)
    return best[1], best[2], f'IoU {best[0]:.2f}'


# ---------- palette ----------
def build_palette(pixels, k):
    cols, counts = np.unique(pixels, axis=0, return_counts=True)
    order = np.argsort(-counts)
    cols, counts = cols[order].astype(float), counts[order].astype(float)
    if len(cols) <= k:
        return cols.astype(np.uint8)
    # max-min init from the darkest colour: rare but distinct hues (sword steel, emblem core) get a slot
    wts = np.sqrt(counts)
    cent = [cols[lum(cols).argmin()]]
    dmin = (((cols - cent[0]) ** 2) * W).sum(-1)
    while len(cent) < k:
        i = int((dmin * (wts > 1.5)).argmax())
        cent.append(cols[i])
        dmin = np.minimum(dmin, (((cols - cols[i]) ** 2) * W).sum(-1))
    cent = np.array(cent)
    for _ in range(50):
        d = (((cols[:, None] - cent[None]) ** 2) * W).sum(-1)
        lab = d.argmin(1)
        new = np.array([np.average(cols[lab == i], 0, wts[lab == i]) if (lab == i).any() else cent[i] for i in range(k)])
        if np.allclose(new, cent):
            break
        cent = new
    # snap each centre to its most-used member so palette colours are real drawn colours
    d = (((cols[:, None] - cent[None]) ** 2) * W).sum(-1)
    lab = d.argmin(1)
    pal = np.array([cols[lab == i][0] if (lab == i).any() else cent[i] for i in range(k)])
    pal = np.unique(pal.round().astype(np.uint8), axis=0)
    return pal[np.argsort(lum(pal))]   # index 0 = darkest = outline


def to_steel(c):
    """Blue-tinted colours (PixelLab's sword highlight in some directions) -> neutral steel grey of equal luminance."""
    c = np.asarray(c, float)
    blue = c[..., 2] > c[..., 0] + 15
    L = lum(c)[..., None]
    grey = np.clip(np.concatenate([L - 2, L + 1, L + 1], -1), 0, 255)
    return np.where(blue[..., None], grey, c)


def quantize(img, pal):
    out = img.copy()
    a = img[..., 3] > 127
    px = to_steel(img[a][:, :3])
    d = (((px[:, None] - pal[None].astype(float)) ** 2) * W).sum(-1)
    # neutral (steel) pixels may only become neutral palette greys, never ivory cloth or gold
    pf = pal.astype(float)
    pal_neutral = (np.ptp(pf, 1) <= 12) & (lum(pf) > 60)
    px_neutral = (np.ptp(px, 1) <= 12) & (lum(px) > 60)
    d[np.ix_(px_neutral, ~pal_neutral)] = np.inf
    idx = d.argmin(1)
    out[a, :3] = pal[idx]
    out[..., 3] = np.where(a, 255, 0)
    out[~a, :3] = 0
    return out


# ---------- cleanup ----------
N4 = [(-1, 0), (1, 0), (0, -1), (0, 1)]
N8 = N4 + [(-1, -1), (-1, 1), (1, -1), (1, 1)]


def key(img):
    return (img[..., 0].astype(np.int32) << 16) | (img[..., 1].astype(np.int32) << 8) | img[..., 2]


def drop_fragments(img, max_px=30, gap=3):
    """Remove small opaque islands floating >= gap px away from the body (AI debris).
    Islands close to the body (sword piece behind a hand) are kept. Returns img, removed px."""
    a = img[..., 3] > 0
    lab = np.zeros(a.shape, np.int32)
    sizes, n = [0], 0
    for y, x in zip(*np.nonzero(a)):
        if lab[y, x]:
            continue
        n += 1; lab[y, x] = n; stack, cnt = [(y, x)], 0
        while stack:
            cy, cx = stack.pop(); cnt += 1
            for dy, dx in N8:
                ny, nx = cy + dy, cx + dx
                if 0 <= ny < a.shape[0] and 0 <= nx < a.shape[1] and a[ny, nx] and not lab[ny, nx]:
                    lab[ny, nx] = n; stack.append((ny, nx))
        sizes.append(cnt)
    sizes = np.array(sizes)
    body = lab == int(sizes.argmax())
    near = body.copy()
    for _ in range(gap):
        p = np.pad(near, 1)
        near = p[1:-1, 1:-1] | p[:-2, 1:-1] | p[2:, 1:-1] | p[1:-1, :-2] | p[1:-1, 2:] | p[:-2, :-2] | p[:-2, 2:] | p[2:, :-2] | p[2:, 2:]
    c = img[..., :3].astype(int)
    steel = (c.max(-1) - c.min(-1) < 25) & (lum(c) > 60)
    # a blade drawn apart from the hidden hand (NW back views) is an island too: keep mostly-steel islands (2026-10-29)
    small = [i for i in range(1, n + 1) if sizes[i] <= max_px and i != sizes.argmax() and not (near & (lab == i)).any()
             and not (sizes[i] >= 6 and steel[lab == i].mean() >= 0.4)]
    kill = np.isin(lab, small)
    img = img.copy(); img[kill] = 0
    return img, int(kill.sum())


def cleanup(img, pal):
    """Isolated pixels / checker -> neighbour colour. Keeps darkest + 2 brightest (eyes, metal glints)."""
    img, fixed = drop_fragments(img)
    keep = {int(key(pal[None, [i]])[0, 0]) for i in (0, -1, -2)}
    a = img[..., 3] > 0
    k = np.where(a, key(img), -1)
    H, Wd = a.shape
    for y in range(H):
        for x in range(Wd):
            n4 = [k[y + dy, x + dx] if 0 <= y + dy < H and 0 <= x + dx < Wd else -1 for dy, dx in N4]
            if not a[y, x]:
                if all(v >= 0 for v in n4):          # 1px hole
                    vals, c = np.unique(n4, return_counts=True)
                    v = int(vals[c.argmax()])
                    img[y, x] = [(v >> 16) & 255, (v >> 8) & 255, v & 255, 255]; fixed += 1
                continue
            if all(v < 0 for v in n4):               # stray opaque speck
                img[y, x] = 0; fixed += 1
                continue
            c = k[y, x]
            if c in keep or c in n4:
                continue
            n8 = [k[y + dy, x + dx] if 0 <= y + dy < H and 0 <= x + dx < Wd else -1 for dy, dx in N8]
            uniform4 = len(set(n4)) == 1
            if c in n8 and not uniform4:
                continue
            nb = [v for v in n8 if v >= 0]
            vals, cnt = np.unique(nb, return_counts=True)
            v = int(vals[cnt.argmax()])
            rep = np.array([(v >> 16) & 255, (v >> 8) & 255, v & 255])
            # only near-shade noise; a contrasting single pixel is deliberate detail (eye, glint, emblem core)
            if np.sqrt((((rep - img[y, x, :3]) ** 2) * W).sum()) > 70:
                continue
            img[y, x, :3] = rep; fixed += 1
    return img, fixed


SUN_DARK = (0x36, 0x2e, 0x25)
SUN_RING = {(0xae, 0x78, 0x3a), (0xc3, 0x96, 0x50), (0xd2, 0xa8, 0x55), (0xfc, 0xdb, 0x6d), (0x91, 0x51, 0x1b)}
SUN_FILL, SUN_CORE = (0xd2, 0xa8, 0x55), (0xfc, 0xdb, 0x6d)


def gold_sun(img):
    """Sun ornament centre is gold (2026-10-28). PixelLab left a dark #362e25 disc in some directions (SE, NE):
    recolour dark clusters of 4-16 px that are mostly ringed by gold and sit at shoulder height. Returns img, px."""
    a = img[..., 3] > 0
    if not a.any():
        return img, 0
    top = np.nonzero(a.any(1))[0][0]
    dark = a & (img[..., :3] == SUN_DARK).all(-1)
    seen, fixed = np.zeros_like(dark), 0
    img = img.copy()
    for y, x in zip(*np.nonzero(dark)):
        if seen[y, x]:
            continue
        seen[y, x] = True; stack, comp = [(y, x)], []
        while stack:
            cy, cx = stack.pop(); comp.append((cy, cx))
            for dy, dx in N8:
                ny, nx = cy + dy, cx + dx
                if 0 <= ny < dark.shape[0] and 0 <= nx < dark.shape[1] and dark[ny, nx] and not seen[ny, nx]:
                    seen[ny, nx] = True; stack.append((ny, nx))
        ring = {(cy + dy, cx + dx) for cy, cx in comp for dy, dx in N8} - set(comp)
        gold = sum(1 for r in ring if 0 <= r[0] < a.shape[0] and 0 <= r[1] < a.shape[1] and a[r] and tuple(img[r][:3]) in SUN_RING)
        cy = np.mean([p[0] for p in comp])
        # shoulder only: eyes under gold hair sit 8-9.5 px below the head top, the ornament 12-16.5 px
        if 4 <= len(comp) <= 16 and gold >= 0.5 * len(ring) and top + 12 <= cy <= top + 18:
            my, mx = np.mean(comp, 0)
            core = min(comp, key=lambda p: (p[0] - my) ** 2 + (p[1] - mx) ** 2)
            for p in comp:
                img[p][:3] = SUN_CORE if p == core else SUN_FILL
            fixed += len(comp)
    return img, fixed


def outline_corners(img, oc):
    """Outline pixels on the silhouette that touch coloured body only diagonally (4-direction outline rule)."""
    a = img[..., 3] > 0
    blk = a & (img[..., :3] == oc).all(-1)
    body = a & ~blk
    p, q = np.pad(a, 1), np.pad(body, 1)
    edge = a & ~(p[:-2, 1:-1] & p[2:, 1:-1] & p[1:-1, :-2] & p[1:-1, 2:])
    body4 = q[:-2, 1:-1] | q[2:, 1:-1] | q[1:-1, :-2] | q[1:-1, 2:]
    body8 = body4 | q[:-2, :-2] | q[:-2, 2:] | q[2:, :-2] | q[2:, 2:]
    return blk & edge & ~body4 & body8


def remove_corner_outline(img, oc):
    """In place: clear diagonal-only outline corners until none remain. Returns removed px."""
    n = 0
    while True:
        c = outline_corners(img, oc)
        if not c.any():
            return n
        img[c] = 0
        n += int(c.sum())


def outline(img, pal):
    """Edge pixels (4-adjacent to transparency) must be the darkest colour. Returns img, stats, debug mask."""
    img = img.copy()
    oc = pal[0]
    a = img[..., 3] > 0
    pad = np.pad(a, 1)
    edge = a & ~(pad[:-2, 1:-1] & pad[2:, 1:-1] & pad[1:-1, :-2] & pad[1:-1, 2:])
    diag_only = a & ~edge & ~(pad[:-2, :-2] & pad[:-2, 2:] & pad[2:, :-2] & pad[2:, 2:])
    is_oc = a & (img[..., :3] == oc).all(-1)
    missing = edge & ~is_oc
    # draw the missing outline OUTSIDE (keeps edge detail like emblem rays); recolour only at ground/canvas limit
    H0, W0 = a.shape
    for y, x in zip(*np.nonzero(missing)):
        for dy, dx in N4:
            oy, ox = y + dy, x + dx
            if 0 <= oy < min(H0, ANCHOR[1]) and 0 <= ox < W0 and not a[oy, ox]:
                img[oy, ox] = [*oc, 255]
    a = img[..., 3] > 0
    pad = np.pad(a, 1)
    edge = a & ~(pad[:-2, 1:-1] & pad[2:, 1:-1] & pad[1:-1, :-2] & pad[1:-1, 2:])
    diag_only = a & ~edge & ~(pad[:-2, :-2] & pad[:-2, 2:] & pad[2:, :-2] & pad[2:, 2:])
    is_oc = a & (img[..., :3] == oc).all(-1)
    img[edge & ~is_oc, :3] = oc
    is_oc = a & (img[..., :3] == oc).all(-1)
    thick_fixed, thick_left = 0, np.zeros_like(a)
    H, Wd = a.shape
    for y, x in zip(*np.nonzero(is_oc & ~edge)):
        for dy, dx in N4:
            oy, ox, iy, ix = y + dy, x + dx, y - dy, x - dx
            if not (0 <= oy < H and 0 <= ox < Wd and 0 <= iy < H and 0 <= ix < Wd):
                continue
            if edge[oy, ox] and is_oc[oy, ox] and a[iy, ix] and not is_oc[iy, ix]:
                if lum(img[iy, ix, :3]) > 70:        # bright inner side: clearly a doubled outline
                    img[y, x, :3] = img[iy, ix, :3]; thick_fixed += 1
                else:                                # dark cloth/glove: ambiguous, mark only
                    thick_left[y, x] = True
                break
    corners = remove_corner_outline(img, oc)
    stats = dict(missing_fixed=int(missing.sum()), thick_fixed=thick_fixed,
                 thick_marked=int(thick_left.sum()), corners_removed=corners)
    return img, stats, missing | thick_left


# ---------- checks ----------
def bbox(img):
    a = img[..., 3] > 0
    ys, xs = np.nonzero(a.any(1))[0], np.nonzero(a.any(0))[0]
    return xs.min(), ys.min(), xs.max(), ys.max()


def gold_centroid(img, top):
    c = img[..., :3].astype(float)
    a = img[..., 3] > 0
    g = a & (c[..., 0] > 150) & (c[..., 1] > 110) & (c[..., 2] < c[..., 1] - 30)
    g[top + 22:] = False                      # head + shoulders only
    ys, xs = np.nonzero(g)
    return (xs.mean(), ys.mean()) if len(xs) else (np.nan, np.nan)


def diff(a, b):
    return float(((a[..., 3] > 0) != (b[..., 3] > 0)).sum() + (a[..., :3] != b[..., :3]).any(-1).sum())


def check_clip(name, d, frames, base_h):
    loop = CLIPS[name][2]
    issues = []
    for i, f in enumerate(frames):
        a = f[..., 3] > 0
        if a[0].any() or a[-1].any() or a[:, 0].any() or a[:, -1].any():
            issues.append(f'f{i}: touches canvas edge (possible clipping)')
        x0, y0, x1, y1 = bbox(f)
        if y1 > ANCHOR[1] - 1:
            issues.append(f'f{i}: pixels below ground line (bottom row {y1})')
        if name in ('idle', 'run') and abs((y1 - y0 + 1) - base_h) > {'idle': 2, 'run': 3}[name]:
            issues.append(f'f{i}: height {y1 - y0 + 1} vs base {base_h}')
    if name in ('idle', 'run'):
        bots = [bbox(f)[3] for f in frames]
        if ANCHOR[1] - 1 not in bots:
            issues.append(f'feet never reach ground row {ANCHOR[1] - 1}: bottoms {bots}')
    cents = [gold_centroid(f, bbox(f)[1]) for f in frames]
    for i in range(1, len(cents)):
        j = np.hypot(cents[i][0] - cents[i - 1][0], cents[i][1] - cents[i - 1][1])
        if j > 4 and name in ('idle', 'run'):
            issues.append(f'f{i - 1}->f{i}: hair/emblem gold centroid jumps {j:.1f}px')
    if loop and len(frames) > 2:
        steps = [diff(frames[i], frames[i + 1]) for i in range(len(frames) - 1)]
        seam = diff(frames[-1], frames[0])
        if seam > 1.5 * max(steps):
            issues.append(f'loop seam diff {seam:.0f} > 1.5x max step {max(steps):.0f}')
        if seam == 0:
            issues.append('last frame duplicates first')
    return issues


# ---------- outputs ----------
def stack_rows(rows):
    """Stack sheet rows vertically, padding shorter rows (fewer frames) with transparency on the right."""
    w = max(r.shape[1] for r in rows)
    return np.concatenate([np.pad(r, ((0, 0), (0, w - r.shape[1]), (0, 0))) for r in rows], 0)


def on_bg(img):
    m = img[..., 3:4] / 255.0
    out = (img[..., :3] * m + np.array(BG) * (1 - m)).astype(np.uint8)
    return np.dstack([out, np.full(out.shape[:2], 255, np.uint8)])


def write_apng(path, frames, fps):
    h, w = frames[0].shape[:2]
    def chunk(t, b):
        return struct.pack('>I', len(b)) + t + b + struct.pack('>I', zlib.crc32(t + b) & 0xffffffff)
    out = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', w, h, 8, 6, 0, 0, 0))
    out += chunk(b'acTL', struct.pack('>II', len(frames), 0))
    seq = 0
    for i, f in enumerate(frames):
        out += chunk(b'fcTL', struct.pack('>IIIIIHHBB', seq, w, h, 0, 0, 1, fps, 0, 0)); seq += 1
        data = zlib.compress(b''.join(b'\x00' + f[y].tobytes() for y in range(h)), 9)
        if i == 0:
            out += chunk(b'IDAT', data)
        else:
            out += chunk(b'fdAT', struct.pack('>I', seq) + data); seq += 1
    out += chunk(b'IEND', b'')
    open(path, 'wb').write(out)


def load_palette():
    p = [l.strip() for l in open(os.path.join(FINAL, 'palette.hex')) if l.strip()]
    return np.array([[int(h[i:i + 2], 16) for i in (0, 2, 4)] for h in p], np.uint8)


def raw_frames(clip, d):
    folder = os.path.join(RAW, clip, d)
    files = sorted(f for f in os.listdir(folder) if f.endswith('.png'))
    return [read_png(os.path.join(folder, f)) for f in files]


def cmd_palette():
    px = []
    for d in DIRS:
        rot, dx, dy = rotation_offset(d)
        px.append(rot[rot[..., 3] > 127][:, :3])
    # sword is grey steel in every direction (2026-10-27): no blue slots in the palette
    pal = build_palette(to_steel(np.concatenate(px)).round().astype(np.uint8), MAX_COLORS)
    os.makedirs(FINAL, exist_ok=True)
    open(os.path.join(FINAL, 'palette.hex'), 'w').write('\n'.join('%02x%02x%02x' % tuple(c) for c in pal) + '\n')
    sw = np.zeros((8, 8 * len(pal), 4), np.uint8)
    for i, c in enumerate(pal):
        sw[:, i * 8:(i + 1) * 8, :3] = c; sw[:, i * 8:(i + 1) * 8, 3] = 255
    write_png(os.path.join(FINAL, 'palette.png'), sw)
    print(len(pal), 'colours locked')


def process_frame(img, dx, dy, pal):
    f, clipped = place(img, dx, dy)
    f = quantize(f, pal)
    f, cleaned = cleanup(f, pal)
    f, sun = gold_sun(f)
    f, ostats, mark = outline(f, pal)
    f, specks = drop_fragments(f, max_px=4, gap=0)   # islands left after outline-corner cleanup are generation specks
    return f, dict(clipped_px=clipped, cleaned_px=cleaned, sun_gold_px=sun, specks_px=specks, **ostats), mark


def cmd_build(only=None):
    pal = load_palette()
    os.makedirs(PREVIEW, exist_ok=True)
    report, log = [], {}
    base = {}
    for d in DIRS:
        rot, dx, dy = rotation_offset(d)
        f, st, _ = process_frame(rot, dx, dy, pal)
        base[d] = (rot, dx, dy, f)
        os.makedirs(os.path.join(FINAL, 'rotations'), exist_ok=True)
        write_png(os.path.join(FINAL, 'rotations', f'{d}.png'), f)
        log[f'rotation/{d}'] = dict(offset=[dx, dy], **st)
    for clip, (n, fps, loop, drop) in CLIPS.items():
        if only and clip != only:
            continue
        rows, prev_rows = [], []
        for d in DIRS:
            if not os.path.isdir(os.path.join(RAW, clip, d)):
                report.append(f'{clip}/{d}: raw missing'); continue
            rot, rdx, rdy, bf = base[d]
            raws = raw_frames(clip, d)
            if d in STILL.get(clip, ()):
                raws = [raws[0]] + [cape_flutter(raws[0], 2 * np.pi * t / STILL_FRAMES) for t in range(STILL_FRAMES)]
            dx, dy, how = clip_offset(rot, rdx, rdy, raws)
            ref = raws[0]   # v3 keeps the start pose as raw frame 0
            raws = raws[drop:]
            if len(raws) != n and d not in STILL.get(clip, ()):
                report.append(f'{clip}/{d}: {len(raws)} raw frames, spec {n} (not padded)')
            out, marks = [], []
            home = planted_feet(bf)
            for i, r in enumerate(raws):
                f, st, m = process_frame(r, dx, dy, pal)
                snap = [0, 0]
                if clip in PLANTED and (PLANTED[clip] is None or i in PLANTED[clip]):
                    fx, fy = planted_feet(f)
                    sx, sy = home[0] - fx, home[1] - fy
                    if (sx or sy) and abs(sx) <= SNAP_MAX and abs(sy) <= SNAP_MAX:
                        f, st, m = process_frame(r, dx + sx, dy + sy, pal)
                        snap = [sx, sy]
                out.append(f); marks.append(m)
                log[f'{clip}/{d}/f{i}'] = dict(offset=[dx, dy], snap=snap, align=how, **st)
            if d in MIRROR.get(clip, ()):
                out, marks = mirror_frames(out), [np.flip(m, 1) for m in marks]
            reff = process_frame(ref, dx, dy, pal)[0]
            if (clip, d) in SWORD_FROM_REF:
                out = paste_sword(out, reff, pal)
            else:
                lost = [i for i, f in enumerate(out) if d == 'north-west' and lost_sword(f, reff)]   # only NW loses the blade
                for i in lost:
                    out[i] = paste_sword([out[i]], reff, pal)[0]
                if lost:
                    report.append(f'{clip}/{d}: sword restored from start pose in f{lost}')
            if clip in STABLE:
                out = [pin_head(f, body_top(bf), pal) for f in out]
            if clip in FREEZE:
                out = [freeze_body(f, reff, pal, **FREEZE[clip]) for f in out]
            bh = bbox(bf)[3] - bbox(bf)[1] + 1
            report += [f'{clip}/{d}: {s}' for s in check_clip(clip, d, out, bh)]
            sheet = np.concatenate(out, 1)
            os.makedirs(os.path.join(FINAL, clip), exist_ok=True)
            write_png(os.path.join(FINAL, clip, f'{d}.png'), sheet)
            rows.append(sheet)
            write_apng(os.path.join(PREVIEW, f'{clip}_{d}_x4.png'), [upscale(on_bg(f), 4) for f in out], fps)
            dbg = [on_bg(f) for f in out]
            for f, m in zip(dbg, marks):
                f[m] = [255, 0, 255, 255]
            prev_rows.append(np.concatenate(dbg, 1))
        if rows:
            write_png(os.path.join(FINAL, f'{clip}_sheet.png'), stack_rows(rows))
            write_png(os.path.join(PREVIEW, f'{clip}_sheet_x4.png'), upscale(on_bg(stack_rows(rows)), 4))
            write_png(os.path.join(PREVIEW, f'{clip}_outline_marks_x4.png'), upscale(stack_rows(prev_rows), 4))
    write_png(os.path.join(PREVIEW, 'rotations_x4.png'), upscale(on_bg(np.concatenate([base[d][3] for d in DIRS], 1)), 4))
    # floor shadow: separate asset, not baked
    sh = np.zeros((SIZE, SIZE, 4), np.uint8)
    yy, xx = np.mgrid[:SIZE, :SIZE]
    e = ((xx + .5 - ANCHOR[0]) / 9) ** 2 + ((yy + .5 - (ANCHOR[1] - 1)) / 3) ** 2 <= 1
    sh[e] = [*pal[0], 255]
    write_png(os.path.join(FINAL, 'shadow.png'), sh)
    spec = dict(frame_size=[SIZE, SIZE], anchor_px=list(ANCHOR), anchor_note='ground contact between feet, y from top',
                unity_pivot=[ANCHOR[0] / SIZE, (SIZE - ANCHOR[1]) / SIZE], ppu=32, scale=1,
                direction_order=DIRS, palette='palette.hex', palette_colors=len(pal), outline_color='#%02x%02x%02x' % tuple(pal[0]),
                sheet_layout='per clip: <clip>/<dir>.png = frames left->right; <clip>_sheet.png = rows in direction_order',
                shadow=dict(file='shadow.png', note='separate layer, draw under sprite at ~40% alpha'),
                clips={c: dict(frames=v[0], fps=v[1], loop=v[2]) for c, v in CLIPS.items()},
                attack_phases=dict(windup=[0, 1], strike=[2, 3], recover=[4, 5]),
                attack_trail='separate asset (not in sprite)')
    json.dump(spec, open(os.path.join(FINAL, 'sprite_spec.json'), 'w'), indent=2)
    json.dump(log, open(os.path.join(FINAL, 'process_log.json'), 'w'), indent=1, default=int)
    open(os.path.join(FINAL, 'issues.txt'), 'w').write('\n'.join(report) + '\n')
    print('\n'.join(report) or 'no issues')
    verify_outputs(pal)


def verify_outputs(pal):
    """Hard guarantees on everything written to 02_final (fails loudly)."""
    pk = set(key(pal[None])[0].tolist())
    n = 0
    for dirpath, _, files in os.walk(FINAL):
        for f in files:
            if not f.endswith('.png') or f == 'palette.png':
                continue
            img = read_png(os.path.join(dirpath, f))
            a = img[..., 3]
            assert set(np.unique(a).tolist()) <= {0, 255}, f'{f}: semi-transparent alpha'
            assert set(np.unique(key(img)[a > 0]).tolist()) <= pk, f'{f}: colour outside palette'
            if f != 'shadow.png':
                for cell in np.split(img, img.shape[1] // SIZE, 1):
                    m = cell[..., 3] > 0
                    pad = np.pad(m, 1)
                    edge = m & ~(pad[:-2, 1:-1] & pad[2:, 1:-1] & pad[1:-1, :-2] & pad[1:-1, 2:])
                    assert (cell[edge, :3] == pal[0]).all(), f'{f}: edge pixel not outline colour'
                    assert not outline_corners(cell, pal[0]).any(), f'{f}: diagonal outline corner left'
            n += 1
    assert len(pal) <= MAX_COLORS, f'palette {len(pal)} > {MAX_COLORS}'
    print(f'verified {n} files: palette {len(pal)} colours, alpha 0/255, 1px 4-direction outline, no diagonal corners')


if __name__ == '__main__':
    {'palette': cmd_palette, 'build': lambda: cmd_build(sys.argv[2] if len(sys.argv) > 2 else None)}[sys.argv[1]]()
