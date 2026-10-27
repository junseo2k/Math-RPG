# Builds the 19시 지하수로 test-area mockup from the three PixelLab Wang tilesets.
import json, os, sys
from PIL import Image

ROOT = sys.argv[1]  # ArtSource/PixelLab/Ch1_19_SacredAqueduct
RAW = ROOT + '/raw/'
T = 32
COLS, ROWS = 20, 12          # 640x384 cells, cropped to 640x360 later
F, W, D, L = 'floor', 'wall', 'dry', 'water'

# vertex terrain grid (COLS+1 x ROWS+1)
V = [[F] * (COLS + 1) for _ in range(ROWS + 1)]
for r in range(ROWS + 1):
    for c in range(COLS + 1):
        if r <= 1:
            V[r][c] = W
        elif r >= 4 and 1 <= c <= 4:
            V[r][c] = L
        elif r >= 4 and 16 <= c <= 19:
            V[r][c] = D


def load(name):
    m = json.load(open(RAW + 'tileset_%s.json' % name))
    sheet = Image.open(RAW + 'tileset_%s.png' % name).convert('RGBA')
    tiles = []
    for t in m['tileset_data']['tiles']:
        b = t['bounding_box']
        img = sheet.crop((b['x'], b['y'], b['x'] + b['width'], b['y'] + b['height']))
        pat = [t['pattern_4x4']['row_%d' % i] for i in range(4)]
        tiles.append((pat, img, (b['x'] // T, b['y'] // T)))
    return tiles


SETS = {L: load('water'), D: load(os.environ.get('DRY', 'dry_v4')), W: load('wall')}


def code(set_key, terr, below_upper):
    # 0 lower, 1 upper, 2 transition (lower vertex right under a wall vertex)
    if set_key == W:
        if terr == W:
            return 1
        return 2 if below_upper else 0
    return 1 if terr == F else 0  # water/dry sets: floor is upper


def vert(r, c):
    r = min(max(r, 0), ROWS); c = min(max(c, 0), COLS)
    return V[r][c]


def pick(r, c):
    corners = {vert(r, c), vert(r, c + 1), vert(r + 1, c), vert(r + 1, c + 1)}
    others = corners - {F}
    # a cell under a wall face still belongs to the wall set
    if not others and (vert(r - 1, c) == W or vert(r - 1, c + 1) == W):
        others = {W}
    key = others.pop() if others else L  # pure floor: any set's floor tile
    assert not others, 'cell %d,%d mixes terrains %s' % (r, c, corners)
    win = [[code(key, vert(r - 1 + i, c - 1 + j), vert(r - 2 + i, c - 1 + j) == W)
            for j in range(4)] for i in range(4)]
    best, score = None, -1
    for pat, img, pos in SETS[key]:
        s = 0
        for i in range(4):
            for j in range(4):
                p = pat[i][j]
                if p == 255:
                    continue
                if p != win[i][j]:
                    s = -1; break
                s += 1
            if s < 0:
                break
        if s > score:
            best, score = (img, key, pos), s
    assert best, 'no tile for %d,%d (%s) %s' % (r, c, key, win)
    return best


def build():
    out = Image.new('RGBA', (COLS * T, ROWS * T))
    used = {}
    for r in range(ROWS):
        for c in range(COLS):
            img, key, pos = pick(r, c)
            out.alpha_composite(img, (c * T, r * T))
            used.setdefault(key, set()).add(pos)
    return out.crop((0, 0, 640, 360)), used


if __name__ == '__main__':
    img, used = build()
    img.save(ROOT + '/mockup_tiles_only.png')
    for k, v in used.items():
        print(k, len(v), sorted(v))
