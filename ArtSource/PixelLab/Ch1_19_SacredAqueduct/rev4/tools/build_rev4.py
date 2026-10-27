# rev4 mockups, same layout as rev1-3.
# usage: python build_rev4.py <rev4 dir> <rev3 dir> <rev2 dir> <rev1 dir> <player png>
import json, random, sys
from PIL import Image

REV4, REV3, REV2, REV1, PLAYER = sys.argv[1:6]
T, COLS, ROWS = 32, 20, 12
F, W, D, L = 'floor', 'wall', 'dry', 'water'

V = [[F] * (COLS + 1) for _ in range(ROWS + 1)]
for r in range(ROWS + 1):
    for c in range(COLS + 1):
        if r <= 1:
            V[r][c] = W
        elif r >= 4 and 1 <= c <= 4:
            V[r][c] = L
        elif r >= 4 and 16 <= c <= 19:
            V[r][c] = D


def rgba(p):
    return Image.open(p).convert('RGBA')


def load(raw, name):
    m = json.load(open(raw + '/tileset_%s.json' % name))
    sheet = rgba(raw + '/tileset_%s.png' % name)
    out = []
    for t in m['tileset_data']['tiles']:
        b = t['bounding_box']
        out.append(([t['pattern_4x4']['row_%d' % i] for i in range(4)],
                    sheet.crop((b['x'], b['y'], b['x'] + b['width'], b['y'] + b['height']))))
    return out


def vert(r, c):
    return V[min(max(r, 0), ROWS)][min(max(c, 0), COLS)]


def code(key, terr, below_wall):
    if key == W:
        return 1 if terr == W else (2 if below_wall else 0)
    return 1 if terr == F else 0


def pick(sets, r, c):
    corners = {vert(r, c), vert(r, c + 1), vert(r + 1, c), vert(r + 1, c + 1)}
    others = corners - {F}
    if not others and W in (vert(r - 1, c), vert(r - 1, c + 1)):
        others = {W}
    key = others.pop() if others else L
    win = [[code(key, vert(r - 1 + i, c - 1 + j), vert(r - 2 + i, c - 1 + j) == W)
            for j in range(4)] for i in range(4)]
    best, score = None, -1
    for pat, img in sets[key]:
        s = 0
        for i in range(4):
            for j in range(4):
                if pat[i][j] == 255:
                    continue
                if pat[i][j] != win[i][j]:
                    s = -1
                    break
                s += 1
            if s < 0:
                break
        if s > score:
            best, score = img, s
    return best, corners


def tiles(sets, water_fill=()):
    rng = random.Random(19)
    out = Image.new('RGBA', (COLS * T, ROWS * T))
    for r in range(ROWS):
        for c in range(COLS):
            img, corners = pick(sets, r, c)
            if corners == {L} and water_fill:
                img = rng.choice([img, *water_fill])
            out.alpha_composite(img, (c * T, r * T))
    return out


def bottom_place(canvas, im, cx, foot_y):
    canvas.alpha_composite(im, (cx - im.width // 2, foot_y - im.getbbox()[3]))


def props(s):
    o = REV1 + '/objects/'
    banner = rgba(o + 'obj_aqueduct_banner.png')
    for cx in (208, 432):
        s.alpha_composite(banner, (cx - 16, 40))
    bottom_place(s, rgba(o + 'obj_aqueduct_altar.png'), 320, 118)
    bottom_place(s, rgba(o + 'obj_aqueduct_urns.png'), 184, 124)
    relief = rgba(REV2 + '/objects/obj_aqueduct_relief.png')
    s.alpha_composite(relief, (320 - relief.width // 2, 40))


def rev3_scene():
    raw = REV3 + '/raw'
    fill = [rgba(raw + '/water_fill_%s.png' % n) for n in 'ab']
    s = tiles({L: load(raw, 'water'), D: load(raw, 'dry'), W: load(raw, 'wall')}, fill)
    dry_decals(s, REV3 + '/objects/', moved=False)
    props(s)
    return s.crop((0, 0, 640, 360))


def dry_decals(s, o, moved=True):
    s.alpha_composite(rgba(o + 'decal_silt_band_a.png'), (502, 117))
    s.alpha_composite(rgba(o + 'decal_silt_band_b.png').rotate(90, expand=True), (499, 196))
    # rev4: the clump that sat mid-bed moves to the right wall foot
    spots = (('clump_a', (586, 262)), ('clump_b', (590, 318) if moved else (538, 206)),
             ('clump_c', (514, 316)), ('clump_d', (584, 150)))
    for name, xy in spots:
        s.alpha_composite(rgba(o + 'decal_silt_%s.png' % name), xy)
    s.alpha_composite(rgba(o + 'decal_hairline_a.png'), (556, 232))
    s.alpha_composite(rgba(o + 'decal_hairline_b.png').transpose(Image.FLIP_LEFT_RIGHT), (522, 270))


def put(s, im, cx, cy):
    s.alpha_composite(im, (cx - im.width // 2, cy - im.height // 2))


def rev4_scene():
    raw = REV4 + '/raw'
    fill = [rgba(raw + '/water_fill_%s.png' % n) for n in 'abc']
    sets = {L: load(raw, 'water'), D: load(raw, 'dry'), W: load(raw, 'wall')}
    # worn floor: the approach to the altar and the spot in front of the urns (people stop there)
    worn_cells = {(9, 3), (10, 3), (9, 4), (5, 3)}
    s = Image.new('RGBA', (COLS * T, ROWS * T))
    rng = random.Random(19)
    for r in range(ROWS):
        for c in range(COLS):
            img, corners = pick(sets, r, c)
            if corners == {L}:
                img = rng.choices([img, *fill], [5, 1, 1, 1])[0]
            elif corners == {F} and (c, r) in worn_cells:
                img = rgba(raw + '/floor_worn.png')
            s.alpha_composite(img, (c * T, r * T))
    o, o3 = REV4 + '/objects/', REV3 + '/objects/'
    # floor: two larger slabs set into the small-slab rhythm, a couple of chipped corners
    s.alpha_composite(rgba(o + 'decal_floor_slab_0.png'), (352, 238))
    s.alpha_composite(rgba(o + 'decal_floor_slab_1.png'), (190, 302))
    put(s, rgba(o + 'decal_stone_chip_3.png'), 271, 287)
    put(s, rgba(o + 'decal_stone_chip_4.png'), 417, 175)
    # water channel: wet line on the exposed rim face, limescale at the waterline,
    # wear on the rim where water and people meet it
    s.alpha_composite(rgba(o + 'decal_water_wetline.png'), (22, 112))
    put(s, rgba(o + 'decal_calcite_0.png'), 74, 114)
    put(s, rgba(o + 'decal_calcite_1.png'), 15, 214)
    for i, xy in ((0, (34, 110)), (1, (99, 110)), (2, (13, 158)), (3, (13, 287)), (4, (127, 236))):
        put(s, rgba(o + 'decal_stone_chip_%d.png' % i), *xy)
    # dry channel: deposits kept at the wall foot and corners, one rim chip
    dry_decals(s, o3)
    put(s, rgba(o + 'decal_stone_chip_1.png'), 541, 110)
    # wall foot: damp above the water channel and above the drained one, moss only by the water
    s.alpha_composite(rgba(o + 'decal_wall_damp_a.png'), (6, 81))
    s.alpha_composite(rgba(o + 'decal_wall_damp_b.png').transpose(Image.FLIP_LEFT_RIGHT), (478, 77))
    put(s, rgba(o + 'decal_wall_moss_0.png'), 41, 86)
    put(s, rgba(o + 'decal_wall_moss_1.png'), 101, 85)
    # repaired blocks
    s.alpha_composite(rgba(o + 'decal_wall_repair_0.png'), (264, 57))
    s.alpha_composite(rgba(o + 'decal_wall_repair_1.png'), (552, 65))
    props(s)
    return s.crop((0, 0, 640, 360))


def check(scene):
    # hero only, same frame and size on water, centre floor and dry channel
    s = scene.copy()
    p = rgba(PLAYER).crop((0, 0, 48, 48))
    for cx in (96, 320, 560):
        bottom_place(s, p, cx, 250)
    return s


def x(im, k):
    return im.resize((im.width * k, im.height * k), Image.NEAREST)


if __name__ == '__main__':
    before, after = rev3_scene(), rev4_scene()
    after.save(REV4 + '/mockup_layout_640x360.png')
    x(after, 3).save(REV4 + '/mockup_layout_x3.png')
    pair = Image.new('RGBA', (640, 730), (0, 0, 0, 255))
    pair.alpha_composite(before, (0, 0)); pair.alpha_composite(after, (0, 370))
    x(pair, 2).save(REV4 + '/compare_rev3_rev4_x2.png')
    x(check(after), 3).save(REV4 + '/check_readability_x3.png')
    # close-ups: centre floor, water rim + wet line, wall foot over the water, dry deposits
    zoom = Image.new('RGBA', (96 * 4 + 24, 96), (0, 0, 0, 255))
    for i, (x0, y0) in enumerate(((300, 150), (0, 96), (0, 40), (496, 100))):
        zoom.alpha_composite(after.crop((x0, y0, x0 + 96, y0 + 96)), (i * 104, 0))
    x(zoom, 4).save(REV4 + '/check_details_zoom_x4.png')
