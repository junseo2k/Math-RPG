"""Left-shoulder pauldron stills (user spec, 2026-11-02): one 64x64 frame per direction, outline-free.

Base = frame 0 of 13_idle_greatsword. NW/W/SW/N/NE get a hand-stamped sun (see STAMP), E has the misplaced
right-shoulder plate covered by the cape. S and SE keep the inpaint results
(01_raw/trials/idle_greatsword_pauldron/<d>_{add,del}_seed1.png), composed so the sword stays in front:
  add  - generated pauldron only where the source was cape or empty
  del  - the misplaced pauldron on the RIGHT shoulder is replaced, sword steel stays
Every off-palette colour then goes to the nearest palette colour.

  python pauldron.py   ->  15_idle_greatsword_pauldron/<n>_<dir>.png
"""
import os
import numpy as np
import process as P
from pngio import read_png, write_png

RAW = os.path.join(P.ROOT, '01_raw', 'trials', 'idle_greatsword_pauldron')
SRC = os.path.join(P.ROOT, '13_idle_greatsword')
OUT = os.path.join(P.ROOT, '15_idle_greatsword_pauldron')
DIRS = ['east', 'north-east', 'north', 'north-west', 'west', 'south-west', 'south', 'south-east']
SHORT = dict(zip(DIRS, ['e', 'ne', 'n', 'nw', 'w', 'sw', 's', 'se']))
# (y0, y1, x0, x1) of the inpaint masks; None = nothing to add (left shoulder hidden behind the torso)
ADD = {'e': None, 'ne': (24, 29, 18, 25), 'n': (26, 32, 21, 29), 'se': (23, 28, 38, 45), 's': (29, 37, 31, 39)}
DEL = {'e': (26, 32, 22, 31), 'ne': (25, 31, 24, 32), 'n': (26, 32, 33, 41), 'se': (23, 30, 24, 32), 's': (29, 33, 19, 27)}
CAPE = {(0x40, 0x0b, 0x18), (0x78, 0x15, 0x24)}
STEEL = {(0x6b, 0x73, 0x6b), (0xa5, 0xa8, 0xa7), (0xbb, 0xbe, 0xbe)}


def compose(d, src):
    out = src.copy()
    if ADD[d]:
        y0, y1, x0, x1 = ADD[d]
        gen = read_png(os.path.join(RAW, f'{d}_add_seed1.png'))
        for y in range(y0, y1):
            for x in range(x0, x1):
                if src[y, x, 3] == 0 or tuple(src[y, x, :3]) in CAPE:
                    out[y, x] = gen[y, x]
    y0, y1, x0, x1 = DEL[d]
    gen = read_png(os.path.join(RAW, f'{d}_del_seed1.png'))
    for y in range(y0, y1):
        for x in range(x0, x1):
            if tuple(src[y, x, :3]) not in STEEL or src[y, x, 3] == 0:
                out[y, x] = gen[y, x]
    return out


# Hand-stamped sun (user 2026-11-02). The pauldron sits on the FRONT of the anatomical left shoulder:
#   NW       big     - 7x7 version of B, only its front 3 columns: the W medallion turned away, bright but
#                      half hidden (user 2026-11-02: epaulette-like, emphasised yet only slightly visible)
#   W/SW     stamp   - whole 5x5 sun on the pauldron
#   N        peek    - seen from behind, only the top two rows rise over the shoulder line
#   E        cover   - the misplaced right-shoulder plate goes under the cape
#   NE       patch   - right-shoulder plate erased and redrawn as a thin sleeve (covering it made the shoulder swell)
# SUN_B is the round medallion the user picked (B): bright disc, dark rim and rays. '.' keeps the pixel.
SUN_B = """. b K b .
b Y Y Y b
K Y C Y K
b Y Y Y b
. b K b ."""
SUN_BIG = """. . b K b . .
. b Y Y Y b .
b Y C C C Y b
K Y C C C Y K
b Y C C C Y b
. b Y Y Y b .
. . b K b . ."""
SUN = {'b': (0x91, 0x51, 0x1b), 'K': (0xae, 0x78, 0x3a), 'Y': (0xfc, 0xdb, 0x6d), 'C': (0xfd, 0xe6, 0xbd),
       'R': (0x78, 0x15, 0x24), 'r': (0x40, 0x0b, 0x18), 'd': (0x36, 0x2e, 0x25), '#': (0, 0, 0)}
NW_BEFORE_SUN = os.path.expanduser('~/Desktop/Math_RPG_승인_스프라이트/2026-10-31_idle_greatsword_mirror/sheets/3_north-west.png')
# direction -> (mode, base sheet or None, centre x, centre y); NW starts from the 10/31 pauldron before the hand-drawn sun
STAMP = {'nw': ('big', NW_BEFORE_SUN, 35, 28, 3), 'w': ('stamp', None, 37, 29), 'sw': ('stamp', None, 37, 26),
         'n': ('peek', None, 23, 28)}
# (y0, y1, x0, x1) of the right-shoulder plate, covered by the cape
COVER = {'e': (26, 32, 22, 31)}
# NE right shoulder, rows 26-30 from x=27: ' ' = empty, letters = SUN colours. Cape edge, then a 2px dark sleeve
# running down to the gauntlet at row 31 (kept), so the hand does not float.
PATCH = {'ne': (27, 26, ["r      ",
                         "rd     ",
                         "dd     ",
                         "#dd    ",
                         "##dd   "])}
GOLD = {(0xfc, 0xdb, 0x6d), (0xc3, 0x96, 0x50), (0xae, 0x78, 0x3a), (0xd2, 0xa8, 0x55), (0x91, 0x51, 0x1b), (0xfd, 0xe6, 0xbd)}
LIGHT = {(0xfc, 0xdb, 0x6d), (0xfd, 0xe6, 0xbd), (0xd2, 0xa8, 0x55)}


def stamp(f, mode, cx, cy, cols=None):
    """stamp: opaque, non-steel pixels only (nothing floats, the sword stays in front).
    peek: rows 0-1 only, on empty or cape pixels (rises over the shoulder line, never over hair or sword)."""
    f = f.copy()
    rows = [r.split() for r in (SUN_BIG if mode == 'big' else SUN_B).splitlines()]
    if mode == 'peek': rows = rows[:2]
    h = len(rows[0]) // 2
    for j, r in enumerate(rows):
        for i, ch in enumerate(r[:cols]):
            y, x = cy - h + j, cx - h + i
            if ch == '.': continue
            c = tuple(f[y, x, :3])
            ok = (f[y, x, 3] and c not in STEEL) if mode in ('stamp', 'big') else (not f[y, x, 3] or c in CAPE)
            if ok:
                f[y, x, :3] = SUN[ch]; f[y, x, 3] = 255
    return f


def cover(f, box):
    f = f.copy()
    y0, y1, x0, x1 = box
    for y in range(y0, y1):
        for x in range(x0, x1):
            c = tuple(f[y, x, :3])
            if f[y, x, 3] and c in GOLD:
                f[y, x, :3] = SUN['R' if c in LIGHT else 'r']
    return f


def patch(f, x0, y0, rows):
    f = f.copy()
    for j, r in enumerate(rows):
        for i, ch in enumerate(r):
            y, x = y0 + j, x0 + i
            if ch == ' ': f[y, x] = 0
            else: f[y, x, :3] = SUN[ch]; f[y, x, 3] = 255
    return f


def to_palette(f, pal):
    op = f[..., 3] > 0
    rgb = f[..., :3].astype(int)
    idx = ((rgb[..., None, :] - pal[None, None]) ** 2).sum(-1).argmin(-1)
    f = f.copy()
    f[op, :3] = pal[idx][op].astype(np.uint8)
    f[..., 3] = np.where(op, 255, 0)
    return f


def main():
    os.makedirs(OUT, exist_ok=True)
    pal = np.array([[int(h[i:i + 2], 16) for i in (0, 2, 4)] for h in open(os.path.join(P.FINAL, 'palette.hex')).read().split() if len(h) >= 6])
    for n, d in enumerate(DIRS):
        s = SHORT[d]
        src = read_png(os.path.join(SRC, f'{n}_{d}.png'))[:, :64]
        if s in STAMP:
            mode, base, *at = STAMP[s]
            out = stamp(read_png(base)[:, :64] if base else src, mode, *at)
        elif s in COVER:
            out = cover(src, COVER[s])
        elif s in PATCH:
            out = patch(src, *PATCH[s])
        else:   # S, SE: inpaint results the user kept
            out = compose(s, src)
        # every direction goes through the palette: W keeps f7dd88 from the 11/02 hand edit (user: fit to 16 colours here)
        out = to_palette(out, pal)
        write_png(os.path.join(OUT, f'{n}_{d}.png'), out)
        print(d, 'changed px', int(np.any(out != src, axis=2).sum()))


if __name__ == '__main__':
    main()
