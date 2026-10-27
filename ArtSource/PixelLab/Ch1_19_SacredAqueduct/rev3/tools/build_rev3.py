# rev3 mockups, same layout as rev1/rev2.
# usage: python build_rev3.py <rev3 dir> <rev2 dir> <rev1 dir> <player png>
import json, random, sys
from PIL import Image

REV3, REV2, REV1, PLAYER = sys.argv[1:5]
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


def rev2_scene():
    raw = REV2 + '/raw'
    fill = [rgba(raw + '/water_fill_%s.png' % n) for n in 'ab']
    s = tiles({L: load(raw, 'water_final'), D: load(raw, 'dry_final'), W: load(raw, 'wall')}, fill)
    silt = rgba(REV2 + '/objects/decal_aqueduct_silt_low.png')
    s.alpha_composite(silt, (522, 146))
    s.alpha_composite(silt.transpose(Image.FLIP_LEFT_RIGHT), (536, 300))
    props(s)
    return s.crop((0, 0, 640, 360))


def rev3_scene():
    raw = REV3 + '/raw'
    fill = [rgba(raw + '/water_fill_%s.png' % n) for n in 'ab']
    s = tiles({L: load(raw, 'water'), D: load(raw, 'dry'), W: load(raw, 'wall')}, fill)
    o = REV3 + '/objects/'
    # deposits gather at the back wall foot, along one side wall, in corners and joints;
    # every piece is used once, so nothing repeats at a fixed spacing
    s.alpha_composite(rgba(o + 'decal_silt_band_a.png'), (502, 117))
    s.alpha_composite(rgba(o + 'decal_silt_band_b.png').rotate(90, expand=True), (499, 196))
    for name, xy in (('clump_a', (586, 262)), ('clump_b', (538, 206)),
                     ('clump_c', (514, 316)), ('clump_d', (584, 150))):
        s.alpha_composite(rgba(o + 'decal_silt_%s.png' % name), xy)
    s.alpha_composite(rgba(o + 'decal_hairline_a.png'), (556, 232))
    s.alpha_composite(rgba(o + 'decal_hairline_b.png').transpose(Image.FLIP_LEFT_RIGHT), (522, 270))
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
    before, after = rev2_scene(), rev3_scene()
    after.save(REV3 + '/mockup_layout_640x360.png')
    x(after, 3).save(REV3 + '/mockup_layout_x3.png')
    pair = Image.new('RGBA', (640, 730), (0, 0, 0, 255))
    pair.alpha_composite(before, (0, 0)); pair.alpha_composite(after, (0, 370))
    x(pair, 2).save(REV3 + '/compare_rev2_rev3_x2.png')
    x(check(after), 3).save(REV3 + '/check_readability_x3.png')
    # connected tiles close-up: water corner, centre floor, dry corner (same 96x96 window each)
    zoom = Image.new('RGBA', (96 * 3 + 16, 96), (0, 0, 0, 255))
    for i, (x0, y0) in enumerate(((16, 96), (240, 150), (528, 96))):
        zoom.alpha_composite(after.crop((x0, y0, x0 + 96, y0 + 96)), (i * 104, 0))
    x(zoom, 4).save(REV3 + '/check_tiles_zoom_x4.png')
