"""attack_greatsword_combo_3hit (2026-11-06, pixminimax 24f, 8 directions, smears kept: user decision).

  python combo_3hit.py          # build from 01_raw/clean + check (exit code 0/1); overwrites sheets
  python combo_3hit.py --check  # check existing sheets only (safe after Aseprite edits)

Sheets: <OUT>/<n>_<dir>.png 1536x64 (brief), <OUT>/uncropped/<n>_<dir>.png 1920x80 (8px pad each side, nothing cut).
"""
import os, sys
import numpy as np
from scipy import ndimage
from pngio import read_png, write_png, upscale
import process as P
from gif_preview import write_gif

OUT = os.path.join(P.ROOT, '17_attack_greatsword_combo_3hit')
REF = os.path.join(P.ROOT, '16_idle_greatsword_pauldron_breath')
RAW = os.path.join(P.ROOT, '01_raw', 'clean')
DIRS = ['east', 'north-east', 'north', 'north-west', 'west', 'south-west', 'south', 'south-east']
NAME = {d: '%d_%s' % (i, d) for i, d in enumerate(DIRS)}
# user-approved south (b1); N and SW passed review on b1; the others faced away or cut 4 times, redone as b2
SRC = {'east': 'b2', 'north-east': 'b2', 'north': 'b1', 'north-west': 'b2', 'west': 'b2', 'south-west': 'b1', 'south': 'b1', 'south-east': 'b2'}
PAD, N, CELL = 8, 24, 64
FOOT_Y = 54   # same as game idle (2026-11-03 decision), brief says 60
STRAY = {('north-west', 10): 30, ('north-east', 2): 10}   # detached blade fragments seen at x5 zoom
GREYS = np.array([[0x6b, 0x73, 0x6b], [0xa5, 0xa8, 0xa7], [0xbb, 0xbe, 0xbe]], np.int32)


def body_bottom(f):
    """Lowest row of coloured pixels, so grey blade and smear don't count as feet."""
    rgb = f[..., :3].astype(np.int32)
    m = (f[..., 3] > 127) & (rgb.max(axis=2) - rgb.min(axis=2) > 25)
    return int(np.nonzero(m.any(axis=1))[0].max())


def quantize(f, pal):
    out = np.zeros(f.shape, np.uint8)
    m = f[..., 3] > 127
    px = f[m][:, :3].astype(np.int32)
    idx = ((px[:, None] - pal[None].astype(np.int32)) ** 2).sum(axis=2).argmin(axis=1)
    q = pal[idx].astype(np.int32)
    # palette has no white: smear stays grey instead of snapping to the cream skin highlight
    neutral = (px.max(axis=1) - px.min(axis=1) < 12) & (px.min(axis=1) > 90)
    q[neutral] = GREYS[((px[neutral][:, None] - GREYS[None]) ** 2).sum(axis=2).argmin(axis=1)]
    out[m, :3] = q
    out[m, 3] = 255
    return out


def drop_specks(f, max_px=6):
    lab, n = ndimage.label(f[..., 3] > 0, structure=np.ones((3, 3)))
    sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
    gone = 0
    for i, s in enumerate(sizes, 1):
        if s <= max_px:
            f[lab == i] = 0
            gone += int(s)
    return gone


def build_dir(d, pal):
    folder = os.path.join(RAW, 'attack_greatsword_combo_3hit_' + SRC[d], d)
    raw = [read_png(os.path.join(folder, 'frame_%02d.png' % i)) for i in range(25)]
    ref = read_png(os.path.join(REF, NAME[d] + '.png'))[:, :64]
    H, W = raw[0].shape[:2]
    a0 = raw[0][..., 3] > 0
    err, dx, dy = min((int((a0[y:y + 64, x:x + 64] != (ref[..., 3] > 0)).sum()), x, y)
                      for y in range(H - 63) for x in range(W - 63))
    assert err == 0, (d, err)
    ref_bottom = body_bottom(ref)
    size = CELL + PAD * 2
    frames, log = [], []
    # 25 stored = reference + 24 generated. Keep 0..22, drop 23, last pose (24) becomes the exact idle.
    for i in list(range(23)) + [24]:
        cell = np.zeros((size, size, 4), np.uint8)
        if i in (0, 24):
            cell[PAD:PAD + 64, PAD:PAD + 64] = ref
        else:
            f = quantize(raw[i], pal)
            specks = drop_specks(f, STRAY.get((d, i), 6))
            # feet back on the ground line, whole pixels only; bottom more than 1px high = smear hides the feet, leave it
            shift = ref_bottom - (body_bottom(f) - dy)
            shift = 0 if shift > 1 else max(shift, -3)
            ys, xs = np.nonzero(f[..., 3])
            ny, nx = ys - dy + shift + PAD, xs - dx + PAD
            keep = (ny >= 0) & (ny < size) & (nx >= 0) & (nx < size)
            cell[ny[keep], nx[keep]] = f[ys[keep], xs[keep]]
            if shift or specks or not keep.all():
                log.append('%s f%02d shift_y=%d specks=%d cut_by_80=%d' % (d, i, shift, specks, int((~keep).sum())))
        frames.append(cell)
    return frames, log


def build():
    os.makedirs(os.path.join(OUT, 'uncropped'), exist_ok=True)
    pal = P.load_palette()
    log = []
    for d in DIRS:
        frames, l = build_dir(d, pal)
        log += l
        write_png(os.path.join(OUT, 'uncropped', NAME[d] + '.png'), np.concatenate(frames, axis=1))
        write_png(os.path.join(OUT, NAME[d] + '.png'), np.concatenate([f[PAD:PAD + 64, PAD:PAD + 64] for f in frames], axis=1))
    open(os.path.join(OUT, 'build_log.txt'), 'w', encoding='utf8').write('\n'.join(log) + '\n')


def on_bg(c, guides=False):
    o = np.zeros(c.shape, np.uint8)
    o[..., :3] = (60, 60, 70)
    o[..., 3] = 255
    if guides:
        p = (c.shape[0] - 64) // 2
        o[p + FOOT_Y + 1, :, :3] = (70, 110, 220)
        o[:, p + 32, :3] = (70, 110, 220)
        for a, b in ((p - 1, p + 64), (p - 1, p + 64)):
            o[a, p - 1:p + 65, :3] = (220, 60, 60); o[b, p - 1:p + 65, :3] = (220, 60, 60)
            o[p - 1:p + 65, a, :3] = (220, 60, 60); o[p - 1:p + 65, b, :3] = (220, 60, 60)
    m = c[..., 3] > 0
    o[m, :3] = c[m, :3]
    return o


def previews():
    os.makedirs(os.path.join(OUT, 'preview'), exist_ok=True)
    sheets = {d: read_png(os.path.join(OUT, 'uncropped', NAME[d] + '.png')) for d in DIRS}
    write_png(os.path.join(OUT, 'preview', 'all_guides_x2.png'),
              upscale(np.concatenate([on_bg_row(sheets[d]) for d in DIRS], axis=0), 2))
    order = [['north-west', 'north', 'north-east'], ['west', None, 'east'], ['south-west', 'south', 'south-east']]
    imgs = []
    for i in range(N):
        rows = []
        for r in order:
            cells = []
            for d in r:
                cells.append(on_bg(sheets[d][:, i * 80:(i + 1) * 80]) if d else on_bg(np.zeros((80, 80, 4), np.uint8)))
            rows.append(np.concatenate(cells, axis=1))
        imgs.append(upscale(np.concatenate(rows, axis=0), 3))
    write_gif(os.path.join(OUT, 'preview', 'all_16fps_x3.gif'), imgs, 6)


def on_bg_row(s):
    return np.concatenate([on_bg(s[:, i * 80:(i + 1) * 80], True) for i in range(N)], axis=1)


def check():
    pal = {tuple(c) for c in P.load_palette()}
    colors, fail, lines = set(), [], []
    total_cut = 0
    for d in DIRS:
        s = read_png(os.path.join(OUT, NAME[d] + '.png'))
        if s.shape[:2] != (64, 64 * N):
            fail.append('%s size %s' % (d, s.shape))
        bad_a = int(((s[..., 3] > 0) & (s[..., 3] < 255)).sum())
        if bad_a:
            fail.append('%s semi-transparent %d' % (d, bad_a))
        colors |= set(map(tuple, s[s[..., 3] > 0][:, :3]))
        ref = read_png(os.path.join(REF, NAME[d] + '.png'))[:, :64]
        d0, d1 = [int((s[:, i * 64:(i + 1) * 64] != ref).any(axis=2).sum()) for i in (0, N - 1)]
        if d0 or d1:
            fail.append('%s start/end differ from idle %d/%d' % (d, d0, d1))
        un = read_png(os.path.join(OUT, 'uncropped', NAME[d] + '.png'))
        cut = [int((un[:, i * 80:(i + 1) * 80, 3] > 0).sum() - (s[:, i * 64:(i + 1) * 64, 3] > 0).sum()) for i in range(N)]
        total_cut += sum(cut)
        rb = body_bottom(ref)
        feet = [body_bottom(s[:, i * 64:(i + 1) * 64]) - rb for i in range(N)]
        margin = []
        for i in range(N):
            ys, xs = np.nonzero(s[:, i * 64:(i + 1) * 64, 3])
            margin.append(min(xs.min(), 63 - xs.max(), ys.min(), 63 - ys.max()))
        ys, xs = np.nonzero(ref[..., 3])
        lines.append('%-11s %s start/end idle diff %d/%d  cut by 64 cell: %d px in %d frames  min margin %d  frames with margin<2: %d  feet offset %d..%d'
                     % (d, SRC[d], d0, d1, sum(cut), sum(1 for c in cut if c), min(margin), sum(1 for m in margin if m < 2), min(feet), max(feet)))
    lines.append('visible RGB colors, all directions: %d (limit 16), outside palette: %d' % (len(colors), len(colors - pal)))
    if len(colors) > 16:
        fail.append('colors %d' % len(colors))
    lines.append('total px cut by the 64 cell: %d' % total_cut)
    lines += ['FAIL ' + f for f in fail] or ['hard checks: PASS']
    txt = '\n'.join(lines) + '\n'
    open(os.path.join(OUT, 'check_20261106.txt'), 'w', encoding='utf8').write(txt)
    sys.stdout.buffer.write(txt.encode('utf8'))
    return 1 if fail else 0


if __name__ == '__main__':
    if '--check' not in sys.argv:
        build()
        previews()
    sys.exit(check())
