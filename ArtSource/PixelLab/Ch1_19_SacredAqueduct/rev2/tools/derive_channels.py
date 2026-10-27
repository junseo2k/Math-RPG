# Derive both channel tilesets from ONE rim/inner-wall structure (PixelLab dry_b set):
#   dry  = dry_b with the bed's pale dash specks cleaned (its pale line above the dark inner wall
#          is the old water level and stays)
#   water = dry_b with bed pixels -> water, the inner wall below that line -> submerged
# usage: python derive_channels.py <rev2 dir>
import json, shutil, sys
from PIL import Image

R = sys.argv[1] + '/raw/'
SUBMERGED = (14, 56, 60)      # darkest PixelLab teal after calm()
DARK = 80                     # luminance below which a non-bed pixel counts as inner wall/shadow


def lum(p):
    return 0.299 * p[0] + 0.587 * p[1] + 0.114 * p[2]


def tiles(name):
    m = json.load(open(R + 'tileset_%s.json' % name))
    sheet = Image.open(R + 'tileset_%s.png' % name).convert('RGBA')
    out = {}
    for t in m['tileset_data']['tiles']:
        k = ''.join(t['corners'][c][0] for c in ('NW', 'NE', 'SW', 'SE'))
        out[k] = t['bounding_box']
    return m, sheet, out


meta, dry, boxes = tiles('dry_b')
_, wsheet, wboxes = tiles('water')


def crop(sheet, b):
    return sheet.crop((b['x'], b['y'], b['x'] + 32, b['y'] + 32))


bed = crop(dry, boxes['llll']).load()
floor = crop(dry, boxes['uuuu']).load()
WATER_VALUE, WATER_SAT = 0.82, 0.8  # PixelLab teal muddied the red telegraph; keep hue, drop value/sat


def calm(p):
    g = lum(p)
    return tuple(int((g + (c - g) * WATER_SAT) * WATER_VALUE) for c in p[:3]) + (255,)


_w = crop(wsheet, wboxes['llll']).load()
water = {(x, y): calm(_w[x, y]) for x in range(32) for y in range(32)}

out_water, out_dry = dry.copy(), dry.copy()

# pale dash specks of the bed reference tile: same positions in every bed pixel
_vals = sorted(lum(bed[x, y]) for x in range(32) for y in range(32))
_med = _vals[len(_vals) // 2]
SPECK = {(x, y) for x in range(32) for y in range(32) if lum(bed[x, y]) > _med + 10}


def bed_fill(x, y):
    # nearest non-speck bed pixel on the same row
    for d in range(1, 32):
        for xx in (x - d, x + d):
            if 0 <= xx < 32 and (xx, y) not in SPECK:
                return bed[xx, y]
    return bed[x, y]

for key, b in boxes.items():
    src = crop(dry, b)
    px = src.load()
    is_bed = [[px[x, y] == bed[x, y] and px[x, y] != floor[x, y] for x in range(32)] for y in range(32)]
    # pixels equal to both references (shared specks): bed if they touch bed, spreading along a speck
    amb = [(x, y) for y in range(32) for x in range(32) if px[x, y] == bed[x, y] == floor[x, y]]
    for _ in range(4):
        for x, y in amb:
            if any(is_bed[yy][xx] for yy in range(max(y - 1, 0), min(y + 2, 32))
                   for xx in range(max(x - 1, 0), min(x + 2, 32))):
                is_bed[y][x] = True
    # bed joints/specks redrawn near the rim: grow into non-floor, non-pale pixels
    # wedged between bed pixels (a 1-sided wall band never has 2 bed neighbours)
    for _ in range(3):
        grow = []
        for y in range(32):
            for x in range(32):
                if is_bed[y][x] or px[x, y] == floor[x, y] or lum(px[x, y]) >= 125:
                    continue
                n = sum(is_bed[yy][xx] for xx, yy in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1))
                        if 0 <= xx < 32 and 0 <= yy < 32)
                if n >= 2:
                    grow.append((x, y))
        for x, y in grow:
            is_bed[y][x] = True
    wall = [[not is_bed[y][x] and lum(px[x, y]) < DARK for x in range(32)] for y in range(32)]
    sub = [[False] * 32 for _ in range(32)]
    # back wall (top bands): everything below the water-level line goes under
    for x in range(32):
        for y in range(1, 32):
            if is_bed[y][x] and wall[y - 1][x]:
                k = 0
                while y - 1 - k >= 0 and wall[y - 1 - k][x]:
                    k += 1
                for i in range(k):
                    sub[y - 1 - i][x] = True
    # side walls: the half nearest the bed goes under water
    for y in range(32):
        for x in range(32):
            if not is_bed[y][x]:
                continue
            for d in (-1, 1):
                k = 0
                while 0 <= x + d * (k + 1) < 32 and wall[y][x + d * (k + 1)]:
                    k += 1
                for i in range((k + 1) // 2):
                    sub[y][x + d * (i + 1)] = True
    wt, dt = src.copy(), src.copy()
    wp, dp = wt.load(), dt.load()
    for y in range(32):
        for x in range(32):
            if is_bed[y][x]:
                wp[x, y] = water[x, y]
                if (x, y) in SPECK:
                    dp[x, y] = bed_fill(x, y)
            elif sub[y][x]:
                wp[x, y] = SUBMERGED + (255,)
    out_water.paste(wt, (b['x'], b['y']))
    out_dry.paste(dt, (b['x'], b['y']))

out_dry.save(R + 'tileset_dry_final.png')
out_water.save(R + 'tileset_water_final.png')
for n in ('dry_final', 'water_final'):
    shutil.copy(R + 'tileset_dry_b.json', R + 'tileset_%s.json' % n)

# calm water fill variants (create_tiles_pro, style = water base tile): their background is one
# shade darker and they carry a 1px joint border, so both are mapped to the base water colour
_base_main = (1, 113, 116)
for i, name in ((12, 'a'), (10, 'b')):
    v = Image.open(R + 'cand_waterfill/%d.png' % i).convert('RGBA')
    p = v.load()
    for y in range(32):
        for x in range(32):
            c = p[x, y][:3]
            if c in ((1, 101, 106), (10, 73, 76)):
                c = _base_main
            p[x, y] = calm(c)
    v.save(R + 'water_fill_%s.png' % name)
