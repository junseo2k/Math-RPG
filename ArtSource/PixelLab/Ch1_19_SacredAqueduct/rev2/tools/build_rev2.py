# rev2 mockups for the 19시 지하수로 test area. Same layout as rev1 (tools/compose.py).
# usage: python build_rev2.py <rev2 dir> <rev1 dir> <player png>
import json, random, sys
from PIL import Image

REV2, REV1, PLAYER = sys.argv[1:4]
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
    assert not others
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
            # full-water cells: base tile or one of the calm fill variants
            if corners == {L} and water_fill:
                img = rng.choice([img, *water_fill])
            out.alpha_composite(img, (c * T, r * T))
    return out


def bottom_place(canvas, im, cx, foot_y):
    canvas.alpha_composite(im, (cx - im.width // 2, foot_y - im.getbbox()[3]))


def rev1_scene():
    raw = REV1 + '/raw'
    s = tiles({L: load(raw, 'water'), D: load(raw, 'dry_v4'), W: load(raw, 'wall')})
    o = REV1 + '/objects/'
    crack = rgba(o + 'decal_aqueduct_drycrack.png')
    s.alpha_composite(crack, (520, 150))
    s.alpha_composite(crack.transpose(Image.FLIP_LEFT_RIGHT), (530, 270))
    props(s, o)
    return s.crop((0, 0, 640, 360))


def props(s, o):
    banner = rgba(o + 'obj_aqueduct_banner.png')
    for cx in (208, 432):
        s.alpha_composite(banner, (cx - 16, 40))
    bottom_place(s, rgba(o + 'obj_aqueduct_altar.png'), 320, 118)
    bottom_place(s, rgba(o + 'obj_aqueduct_urns.png'), 184, 124)


def rev2_scene():
    raw = REV2 + '/raw'
    fill = [rgba(raw + '/water_fill_%s.png' % n) for n in 'ab']
    s = tiles({L: load(raw, 'water_final'), D: load(raw, 'dry_final'), W: load(raw, 'wall')}, fill)
    o2 = REV2 + '/objects/'
    silt = rgba(o2 + 'decal_aqueduct_silt_low.png')
    # irregular spacing, one mirrored, so no visible rhythm
    s.alpha_composite(silt, (522, 146))
    s.alpha_composite(silt.transpose(Image.FLIP_LEFT_RIGHT), (536, 300))
    relief = rgba(o2 + 'obj_aqueduct_relief.png')
    s.alpha_composite(relief, (320 - relief.width // 2, 40))
    props(s, REV1 + '/objects/')
    return s.crop((0, 0, 640, 360))


def telegraph(s, box, k=0.6):
    # SliceBoss: Square sprite tinted telegraphOuterColor, fill scaled by progress k
    x0, y0, x1, y1 = box
    outer = Image.new('RGBA', (x1 - x0, y1 - y0), (255, 64, 51, int(0.15 * 255)))
    s.alpha_composite(outer, (x0, y0))
    w, h = int((x1 - x0) * k), int((y1 - y0) * k)
    s.alpha_composite(Image.new('RGBA', (w, h), (255, 64, 51, int(0.35 * 255))),
                      ((x0 + x1 - w) // 2, (y0 + y1 - h) // 2))


def check(scene):
    s = scene.copy()
    p = rgba(PLAYER).crop((0, 0, 48, 48))
    for cx, fy, box in ((96, 260, (48, 196, 144, 260)),
                        (320, 230, (272, 166, 368, 230)),
                        (560, 260, (512, 196, 608, 260))):
        telegraph(s, box)
        bottom_place(s, p, cx, fy)
    bottom_place(s, p, 400, 300)  # plain floor, no telegraph
    return s


def x(im, k):
    return im.resize((im.width * k, im.height * k), Image.NEAREST)


if __name__ == '__main__':
    before, after = rev1_scene(), rev2_scene()
    after.save(REV2 + '/mockup_layout_640x360.png')
    x(after, 3).save(REV2 + '/mockup_layout_x3.png')
    pair = Image.new('RGBA', (640, 730), (0, 0, 0, 255))
    pair.alpha_composite(before, (0, 0)); pair.alpha_composite(after, (0, 370))
    x(pair, 2).save(REV2 + '/compare_before_after_x2.png')
    x(check(after), 3).save(REV2 + '/check_readability_x3.png')
    # seam/repeat check: 6x6 block of each fill tile, plus the water area with variants
    sheet = Image.new('RGBA', (3 * 6 * T + 2 * 8, 6 * T), (0, 0, 0, 255))
    raw = REV2 + '/raw'
    fills = []
    for name in ('water_final', 'dry_final'):
        for pat, img in load(raw, name):
            if all(v == 0 for v in pat[1][1:3] + pat[2][1:3]):
                fills.append(img)
    for pat, img in load(raw, 'wall'):
        if all(v == 0 for v in pat[1][1:3] + pat[2][1:3]):
            fills.insert(1, img)
            break
    for i, img in enumerate(fills[:3]):  # water, floor, dry bed
        for r in range(6):
            for c in range(6):
                sheet.alpha_composite(img, (i * (6 * T + 8) + c * T, r * T))
    x(sheet, 2).save(REV2 + '/check_tile_repeat_x2.png')
