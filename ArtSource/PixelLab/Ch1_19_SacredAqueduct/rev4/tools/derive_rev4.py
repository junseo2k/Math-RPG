# rev4 tiles and decals from rev3 + the PixelLab aging candidates.
#   floor: slab highlight borders folded into the face, joints one step softer; a worn variant
#   rims:  pale rim pixels pulled toward the floor face (lower contrast, same shapes)
#   water: flat base, back-wall band made even (no arches/ticks), ripples only in fill variants
# usage: python derive_rev4.py <rev4 dir> <rev3 dir>
import json, random, shutil, sys
from collections import deque
from PIL import Image

R4, R3 = sys.argv[1], sys.argv[2]
RAW4, RAW3, OBJ = R4 + '/raw/', R3 + '/raw/', R4 + '/objects/'

FACE = (109, 127, 144)
FLOOR_MAP = {(114, 132, 149): FACE,             # bright inner border of every slab
             (122, 140, 157): (113, 131, 148),  # highlight corner
             (129, 147, 164): (113, 131, 148),
             (84, 103, 120): (96, 114, 131)}    # joints
WORN_JOINT = (102, 120, 137)
RIM_PULL = 0.35
BG = (16, 92, 98, 255)
SUBMERGED = (14, 56, 60, 255)


def lum(p):
    return 0.299 * p[0] + 0.587 * p[1] + 0.114 * p[2]


def mix(a, b, t):
    return tuple(int(x + (y - x) * t) for x, y in zip(a[:3], b[:3]))


def tiles_meta(raw, name):
    return json.load(open(raw + 'tileset_%s.json' % name))['tileset_data']['tiles']


def is_teal(p):
    return p[0] < 45 and p[1] > 50 and p[2] > 55


# rev3 floor tile = the 'llll' tile of the wall set (floor is lower there)
wall3 = Image.open(RAW3 + 'tileset_wall.png').convert('RGBA')
_fb = next(t['bounding_box'] for t in tiles_meta(RAW3, 'wall') if set(t['corners'].values()) == {'lower'})
floor3 = wall3.crop((_fb['x'], _fb['y'], _fb['x'] + 32, _fb['y'] + 32)).load()


def floor4(p):
    return FLOOR_MAP.get(p[:3], p[:3]) + (255,)


worn = Image.new('RGBA', (32, 32))
for y in range(32):
    for x in range(32):
        c = floor4(floor3[x, y])
        worn.putpixel((x, y), (WORN_JOINT + (255,)) if c[:3] == FLOOR_MAP[(84, 103, 120)] else c)
worn.save(RAW4 + 'floor_worn.png')


def process(name, water=False):
    sheet = Image.open(RAW3 + 'tileset_%s.png' % name).convert('RGBA')
    for t in tiles_meta(RAW3, name):
        b = t['bounding_box']
        tile = sheet.crop((b['x'], b['y'], b['x'] + 32, b['y'] + 32))
        p = tile.load()
        fl = [[p[x, y] == floor3[x, y] for x in range(32)] for y in range(32)]
        for y in range(32):
            for x in range(32):
                if fl[y][x]:
                    p[x, y] = floor4(p[x, y])
                elif name != 'wall' and lum(p[x, y]) >= 130 and not is_teal(p[x, y]):
                    p[x, y] = mix(p[x, y], FACE, RIM_PULL) + (255,)
        if water:
            # back-wall band: rows that are mostly submerged become one even dark strip
            for y in range(32):
                xs = [x for x in range(32) if p[x, y] == SUBMERGED]
                if len(xs) >= 6:
                    for x in range(min(xs), max(xs) + 1):
                        if not fl[y][x] and (is_teal(p[x, y]) or lum(p[x, y]) < 125):
                            p[x, y] = SUBMERGED
            for y in range(32):
                for x in range(32):
                    if is_teal(p[x, y]) and p[x, y] != SUBMERGED:
                        p[x, y] = BG
            # old rim tick marks left hanging in the water: grey pixels with water on both sides
            # (runs of up to 3 px, across or down)
            for dx, dy in ((1, 0), (0, 1)):
                for y in range(32):
                    for x in range(32):
                        if fl[y][x] or is_teal(p[x, y]):
                            continue
                        # a tile edge counts as water when the pixel beside the run (above it for
                        # a horizontal run, left of it for a vertical one) is water: marks on seams
                        ox, oy = (x, y - 1) if dx else (x - 1, y)
                        side = 0 <= ox < 32 and 0 <= oy < 32 and is_teal(p[ox, oy])

                        def water_at(cx, cy):
                            return is_teal(p[cx, cy]) if 0 <= cx < 32 and 0 <= cy < 32 else side

                        bx, by = x - dx, y - dy
                        if not (0 <= bx < 32 and 0 <= by < 32) and not side:
                            continue
                        if not water_at(bx, by):
                            continue
                        run = []
                        cx, cy = x, y
                        while 0 <= cx < 32 and 0 <= cy < 32 and not is_teal(p[cx, cy]) and not fl[cy][cx] and len(run) < 4:
                            run.append((cx, cy))
                            cx, cy = cx + dx, cy + dy
                        if len(run) <= 3 and water_at(cx, cy):
                            for q in run:
                                p[q] = BG
        sheet.paste(tile, (b['x'], b['y']))
    sheet.save(RAW4 + 'tileset_%s.png' % name)
    shutil.copy(RAW3 + 'tileset_%s.json' % name, RAW4 + 'tileset_%s.json' % name)


process('wall')
process('dry')
process('water', water=True)

# ripple shapes (PixelLab candidates), dimmed toward the water colour
RIPPLES = []
for i in (2, 12, 33, 5, 58):
    im = Image.open(RAW4 + 'cand_ripple/%d.png' % i).convert('RGBA')
    im = im.crop(im.getbbox())
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            if px[x, y][3]:
                px[x, y] = mix(px[x, y], BG, 0.35) + (255,)
            else:
                px[x, y] = (0, 0, 0, 0)
    RIPPLES.append(im)


def water_fill(n, seed):
    t = Image.new('RGBA', (32, 32), BG)
    rng = random.Random(seed)
    for _ in range(n):
        r = rng.choice(RIPPLES)
        t.alpha_composite(r, (rng.randint(1, 31 - r.width), rng.randint(2, 30 - r.height)))
    return t


# a: one ripple, b: one other ripple, c: two far apart. The flat base is used most often.
water_fill(1, 3).save(RAW4 + 'water_fill_a.png')
water_fill(1, 8).save(RAW4 + 'water_fill_b.png')
water_fill(2, 21).save(RAW4 + 'water_fill_c.png')
for i, r in enumerate(RIPPLES):
    r.save(OBJ + 'decal_water_ripple_%d.png' % i)


# ---- decals ----
def save_decal(src, dst, pull=0.0, toward=FACE, crop=None, keep=None):
    im = Image.open(RAW4 + src).convert('RGBA')
    if crop:
        im = im.crop(crop)
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            a = px[x, y]
            if a[3] and (keep is None or keep(a)):
                px[x, y] = mix(a, toward, pull) + (a[3],)
            else:
                px[x, y] = (0, 0, 0, 0)
    bb = im.getbbox()
    im.crop(bb).save(OBJ + dst)


save_decal('cand_wetband/0.png', 'decal_water_wetline.png', 0.15, crop=(0, 10, 96, 16))
save_decal('cand_damp/3.png', 'decal_wall_damp_a.png')
save_decal('cand_damp/2.png', 'decal_wall_damp_b.png')
for j, i in enumerate((4, 13, 21, 29, 44)):
    save_decal('cand_chip/%d.png' % i, 'decal_stone_chip_%d.png' % j)
for j, i in enumerate((3, 33)):
    save_decal('cand_calcite/%d.png' % i, 'decal_calcite_%d.png' % j, 0.6, toward=(110, 120, 126))
green = lambda c: c[1] > c[0] + 8 and c[1] > c[2] - 4
for j, i in enumerate((6, 27)):
    save_decal('cand_moss/%d.png' % i, 'decal_wall_moss_%d.png' % j, keep=green)

# large floor slabs: PixelLab drew the joint frame only; fill the face inside it
for j, i in enumerate((1, 57)):
    im = Image.open(RAW4 + 'cand_slab/%d.png' % i).convert('RGBA')
    im = im.crop(im.getbbox())
    px = im.load()
    w, h = im.size
    q, seen = deque([(w // 2, h // 2)]), set()
    while q:
        x, y = q.popleft()
        if (x, y) in seen or not (0 <= x < w and 0 <= y < h) or px[x, y][3]:
            continue
        seen.add((x, y))
        q.extend(((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)))
    for x, y in seen:
        px[x, y] = FACE + (255,)
    for y in range(h):
        for x in range(w):
            c = px[x, y]
            if c[3] and (x, y) not in seen:
                # frame drawn no darker than the softened floor joints
                px[x, y] = mix(FLOOR_MAP.get(c[:3], c[:3]), FLOOR_MAP[(84, 103, 120)], 0.7) + (255,)
    im.save(OBJ + 'decal_floor_slab_%d.png' % j)

# repaired wall stones: one real block lifted from the wall face, a touch lighter and crisper
wall_face = Image.open(RAW4 + 'tileset_wall.png').convert('RGBA')
_wb = next(t['bounding_box'] for t in tiles_meta(RAW3, 'wall')
           if t['corners'] == {'NW': 'upper', 'NE': 'upper', 'SW': 'transition', 'SE': 'transition'})
for j, (dx, dy, lift) in enumerate(((0, 17, 0.10), (16, 25, 0.06))):
    blk = wall_face.crop((_wb['x'] + dx, _wb['y'] + dy, _wb['x'] + dx + 16, _wb['y'] + dy + 7))
    px = blk.load()
    for y in range(blk.height):
        for x in range(blk.width):
            c = px[x, y]
            edge = x == 0 or y == 0
            px[x, y] = mix(c, (150, 158, 170) if edge else (92, 102, 118), lift * (2 if edge else 1)) + (255,)
    blk.save(OBJ + 'decal_wall_repair_%d.png' % j)
