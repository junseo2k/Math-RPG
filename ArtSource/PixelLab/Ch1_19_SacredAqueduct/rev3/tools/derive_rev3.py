# rev3 tilesets. Keeps rev2's shared channel structure (PixelLab dry_b) and wall set, and swaps in
# the new PixelLab floor and water from tileset_floorwater:
#   floor: every pixel equal to the rev2 floor tile -> new floor tile (tones pulled toward the mean)
#   dry:   bed specks cleaned, dried water-level line warmed and lifted one step
#   water: bed -> new water (calmer, glints dimmed), inner wall below the line submerged,
#          broken faint ripple where water meets the stone
# usage: python derive_rev3.py <rev3 dir> <rev2 dir>
import json, shutil, sys
from PIL import Image

R3, R2 = sys.argv[1] + '/raw/', sys.argv[2] + '/raw/'
SUBMERGED = (14, 56, 60)
DARK = 80
FLOOR_TONE = 0.55            # slab tone variation kept (1 = as generated)
WATER_VALUE, WATER_SAT = 0.82, 0.8
GLINT_KEEP = 0.45            # glint brightness kept over the water colour
STAIN = (150, 148, 140)      # dried water line on the dry inner wall


def lum(p):
    return 0.299 * p[0] + 0.587 * p[1] + 0.114 * p[2]


def load(path, name):
    m = json.load(open(path + 'tileset_%s.json' % name))
    sheet = Image.open(path + 'tileset_%s.png' % name).convert('RGBA')
    return sheet, {''.join(t['corners'][c][0] for c in ('NW', 'NE', 'SW', 'SE')): t['bounding_box']
                   for t in m['tileset_data']['tiles']}


def crop(sheet, b):
    return sheet.crop((b['x'], b['y'], b['x'] + 32, b['y'] + 32))


def calm(p):
    g = lum(p)
    return tuple(int((g + (c - g) * WATER_SAT) * WATER_VALUE) for c in p[:3]) + (255,)


def mix(a, b, t):
    return tuple(int(x + (y - x) * t) for x, y in zip(a[:3], b[:3])) + (255,)


dry, boxes = load(R2, 'dry_b')
wall_sheet, _ = load(R2, 'wall')
fw, fw_boxes = load(R3, 'floorwater')      # water source
fl2, fl2_boxes = load(R3, 'floor_medium')  # floor source (same water base, medium slabs)
bed = crop(dry, boxes['llll']).load()
old_floor = crop(dry, boxes['uuuu']).load()

# new floor: same slab shapes, tone variation compressed
_f = crop(fl2, fl2_boxes['uuuu'])
_fp = _f.load()
_mean = tuple(sum(_fp[x, y][i] for x in range(32) for y in range(32)) // 1024 for i in range(3))
floor = {(x, y): mix(_mean, _fp[x, y], FLOOR_TONE) for x in range(32) for y in range(32)}
_f.save(R3 + 'floor_generated.png')

# new water: flat background, glint clusters split into groups for the fill variants
_w = crop(fw, fw_boxes['llll']).load()
_bg = max(((_w[x, y][:3]) for x in range(32) for y in range(32)),
          key=lambda c: sum(_w[x, y][:3] == c for x in range(32) for y in range(32)))
BG = calm(_bg)
glint = {(x, y): mix(BG, calm(_w[x, y]), GLINT_KEEP)
         for x in range(32) for y in range(32) if lum(_w[x, y]) > lum(_bg) + 15}
clusters, seen = [], set()
for p in sorted(glint):
    if p in seen:
        continue
    stack, group = [p], []
    seen.add(p)
    while stack:
        x, y = stack.pop()
        group.append((x, y))
        for q in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1), (x + 1, y + 1), (x - 1, y - 1),
                  (x + 1, y - 1), (x - 1, y + 1)):
            if q in glint and q not in seen:
                seen.add(q)
                stack.append(q)
    clusters.append(group)


def water_tile(keep, dx=0, dy=0):
    # glints moved off the generated lattice; a glint that would cross the tile edge is dropped
    t = Image.new('RGBA', (32, 32), BG)
    p = t.load()
    for i, g in enumerate(clusters):
        moved = [(x + dx, y + dy) for x, y in g]
        if keep(i) and all(0 <= x < 32 and 0 <= y < 32 for x, y in moved):
            for q, m in zip(g, moved):
                p[m] = glint[q]
    return t


water_base = water_tile(lambda i: i % 2 == 0).load()             # half the glints
water_tile(lambda i: i % 2 == 1, 5, 9).save(R3 + 'water_fill_a.png')   # other half, shifted
water_tile(lambda i: i % 5 == 2, -7, 3).save(R3 + 'water_fill_b.png')  # nearly calm
EDGE = mix(BG, max(glint.values(), key=lum), 0.3)

_vals = sorted(lum(bed[x, y]) for x in range(32) for y in range(32))
SPECK = {(x, y) for x in range(32) for y in range(32) if lum(bed[x, y]) > _vals[512] + 10}


def bed_fill(x, y):
    for d in range(1, 32):
        for xx in (x - d, x + d):
            if 0 <= xx < 32 and (xx, y) not in SPECK:
                return bed[xx, y]
    return bed[x, y]


out_water, out_dry, out_wall = dry.copy(), dry.copy(), wall_sheet.copy()

for key, b in boxes.items():
    src = crop(dry, b)
    px = src.load()
    is_floor = [[px[x, y] == old_floor[x, y] and px[x, y] != bed[x, y] for x in range(32)] for y in range(32)]
    is_bed = [[px[x, y] == bed[x, y] and px[x, y] != old_floor[x, y] for x in range(32)] for y in range(32)]
    amb = [(x, y) for y in range(32) for x in range(32) if px[x, y] == bed[x, y] == old_floor[x, y]]
    for _ in range(4):
        for x, y in amb:
            near = [(xx, yy) for yy in range(max(y - 1, 0), min(y + 2, 32)) for xx in range(max(x - 1, 0), min(x + 2, 32))]
            if any(is_bed[yy][xx] for xx, yy in near):
                is_bed[y][x] = True
            elif any(is_floor[yy][xx] for xx, yy in near):
                is_floor[y][x] = True
    for _ in range(3):
        grow = []
        for y in range(32):
            for x in range(32):
                if is_bed[y][x] or is_floor[y][x] or lum(px[x, y]) >= 125:
                    continue
                n = sum(is_bed[yy][xx] for xx, yy in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1))
                        if 0 <= xx < 32 and 0 <= yy < 32)
                if n >= 2:
                    grow.append((x, y))
        for x, y in grow:
            is_bed[y][x] = True
    wall = [[not is_bed[y][x] and lum(px[x, y]) < DARK for x in range(32)] for y in range(32)]
    sub = [[False] * 32 for _ in range(32)]
    stain = [[False] * 32 for _ in range(32)]
    for x in range(32):
        for y in range(1, 32):
            if is_bed[y][x] and wall[y - 1][x]:
                k = 0
                while y - 1 - k >= 0 and wall[y - 1 - k][x]:
                    k += 1
                for i in range(k):
                    sub[y - 1 - i][x] = True
                top = y - 1 - k  # the pale line sits right above the dark inner wall
                if top >= 0 and k and 120 <= lum(px[x, top]) < 160:
                    stain[top][x] = True
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
            if is_floor[y][x]:
                wp[x, y] = dp[x, y] = floor[x, y]
            elif is_bed[y][x]:
                wp[x, y] = water_base[x, y]
                if (x, y) in SPECK:
                    dp[x, y] = bed_fill(x, y)
            elif sub[y][x]:
                wp[x, y] = SUBMERGED + (255,)
            if stain[y][x]:
                dp[x, y] = STAIN + (255,)
    # faint broken ripple on the first water pixel off the stone
    for y in range(32):
        for x in range(32):
            if not is_bed[y][x]:
                continue
            touch = any(0 <= xx < 32 and 0 <= yy < 32 and not is_bed[yy][xx] and not is_floor[yy][xx]
                        for xx, yy in ((x - 1, y), (x + 1, y), (x, y - 1)))
            if touch and (x * 7 + y * 13 + len(key)) % 5 < 3:
                wp[x, y] = EDGE
    out_water.paste(wt, (b['x'], b['y']))
    out_dry.paste(dt, (b['x'], b['y']))

# wall set: 25 tiles, some share corner keys, so walk the metadata list itself.
# The flat cap has the same colour as the old floor joints, so colour alone can't separate them:
# swap only floor-coloured regions connected to a corner that is floor ('l').
_wm = json.load(open(R2 + 'tileset_wall.json'))['tileset_data']['tiles']
CORNER_PX = {'NW': (0, 0), 'NE': (31, 0), 'SW': (0, 31), 'SE': (31, 31)}
for tdef in _wm:
    b = tdef['bounding_box']
    t = crop(wall_sheet, b)
    p = t.load()
    cand = {(x, y) for y in range(32) for x in range(32) if p[x, y] == old_floor[x, y]}
    seeds = [CORNER_PX[c] for c, v in tdef['corners'].items() if v == 'lower' and CORNER_PX[c] in cand]
    region, stack = set(seeds), list(seeds)
    while stack:
        x, y = stack.pop()
        for q in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if q in cand and q not in region:
                region.add(q)
                stack.append(q)
    for q in region:
        p[q] = floor[q]
    out_wall.paste(t, (b['x'], b['y']))

out_dry.save(R3 + 'tileset_dry.png')
out_water.save(R3 + 'tileset_water.png')
out_wall.save(R3 + 'tileset_wall.png')
for n in ('dry', 'water'):
    shutil.copy(R2 + 'tileset_dry_b.json', R3 + 'tileset_%s.json' % n)
shutil.copy(R2 + 'tileset_wall.json', R3 + 'tileset_wall.json')

# sediment and crack decals: candidates pulled toward the dry bed colour so they sit in the stone
_bed_avg = tuple(sum(bed[x, y][i] for x in range(32) for y in range(32)) // 1024 for i in range(3))
for src, dst, t in (('cand_siltband/1.png', 'decal_silt_band_a.png', 0.45),
                    ('cand_siltband/2.png', 'decal_silt_band_b.png', 0.45),
                    ('cand_siltclump/18.png', 'decal_silt_clump_a.png', 0.45),
                    ('cand_siltclump/27.png', 'decal_silt_clump_b.png', 0.45),
                    ('cand_siltclump/44.png', 'decal_silt_clump_c.png', 0.45),
                    ('cand_siltclump/6.png', 'decal_silt_clump_d.png', 0.45),
                    ('cand_hairline/22.png', 'decal_hairline_a.png', 0.35),
                    ('cand_hairline/10.png', 'decal_hairline_b.png', 0.35)):
    im = Image.open(R3 + src).convert('RGBA')
    p = im.load()
    for y in range(im.height):
        for x in range(im.width):
            if p[x, y][3]:
                p[x, y] = mix(p[x, y], _bed_avg, t)[:3] + (p[x, y][3],)
    im.save(sys.argv[1] + '/objects/' + dst)
