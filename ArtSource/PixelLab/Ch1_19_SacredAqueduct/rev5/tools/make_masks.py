# Inpainting masks for the rev5 damage pass. White = PixelLab may repaint, black = frozen.
# Masks stay on narrow crack paths, small spall patches and rim chips so the slab and block
# joints around them are kept pixel-for-pixel.
# usage: python make_masks.py <rev5 dir>
import json, sys
from PIL import Image, ImageDraw

R5 = sys.argv[1]
base = Image.open(R5 + '/work/base_noprops.png').convert('RGB')

CRACK, BLOB, BAND = 'crack', 'blob', 'band'
# (kind, points or centre, width/radius) in 640x360 scene coordinates
DAMAGE = [
    # wall: cracks inside blocks, spalls, chipped corners, deeper joints, damp climbing joints
    (CRACK, [(60, 49), (66, 58), (63, 68)], 3), (CRACK, [(150, 47), (157, 60)], 3),
    (CRACK, [(236, 62), (244, 74), (240, 84)], 3), (CRACK, [(362, 47), (353, 60), (356, 70)], 3),
    (CRACK, [(468, 54), (476, 70)], 3), (CRACK, [(590, 46), (584, 58), (592, 70)], 3),
    (BLOB, (100, 61), 5), (BLOB, (186, 52), 4), (BLOB, (291, 78), 4), (BLOB, (352, 80), 4),
    (BLOB, (455, 77), 5), (BLOB, (540, 52), 5), (BLOB, (618, 67), 4), (BLOB, (16, 74), 4),
    (BAND, [(24, 88), (26, 62)], 3), (BAND, [(88, 88), (86, 70)], 3), (BAND, [(522, 88), (520, 68)], 3),
    (BAND, [(120, 56), (148, 56)], 2), (BAND, [(398, 72), (428, 72)], 2), (BAND, [(560, 64), (588, 64)], 2),
    # water channel rims (most wear: water and feet meet here)
    (BLOB, (30, 112), 4), (BLOB, (58, 111), 3), (BLOB, (96, 113), 5),
    (BLOB, (14, 150), 4), (BLOB, (15, 232), 3), (BLOB, (14, 302), 5),
    (BLOB, (143, 182), 4), (BLOB, (143, 272), 3),
    (BAND, [(21, 116), (70, 116)], 2), (BAND, [(19, 135), (19, 200)], 2), (BAND, [(19, 250), (19, 330)], 2),
    # dry channel rims
    (BLOB, (520, 112), 4), (BLOB, (576, 111), 5), (BLOB, (612, 113), 3),
    (BLOB, (495, 172), 4), (BLOB, (496, 282), 3), (BLOB, (624, 212), 4), (BLOB, (623, 322), 3),
    # dry channel bed: silt along the wall foot and a few joints
    (BAND, [(503, 124), (614, 124)], 3), (BAND, [(503, 135), (503, 250)], 3),
    (BAND, [(552, 162), (552, 214)], 2), (BAND, [(522, 256), (584, 256)], 2),
    # floor: long cracks over several slabs (one through the centre, kept faint later),
    # short cracks, spalls, chipped corners. Edges of the floor get more.
    (CRACK, [(150, 140), (175, 150), (190, 170), (215, 178)], 3),
    (CRACK, [(430, 300), (452, 320), (470, 345)], 3),
    (CRACK, [(300, 250), (330, 262), (352, 258), (372, 272)], 3),
    (CRACK, [(250, 190), (258, 199)], 3), (CRACK, [(400, 160), (411, 168)], 3),
    (CRACK, [(200, 331), (213, 322)], 3), (CRACK, [(460, 200), (468, 213)], 3),
    (BLOB, (180, 250), 5), (BLOB, (440, 250), 6), (BLOB, (262, 320), 4), (BLOB, (332, 172), 4),
    (BLOB, (150, 330), 5), (BLOB, (470, 150), 5),
    (BLOB, (230, 230), 3), (BLOB, (382, 320), 3), (BLOB, (420, 120), 3),
]

CROPS = {
    'wall_l': ((0, 0, 256, 128), 'wall'), 'wall_m': ((256, 0, 512, 128), 'wall'),
    'wall_r': ((512, 0, 640, 128), 'wall'), 'water': ((0, 128, 160, 360), 'water'),
    'floor': ((160, 128, 416, 360), 'floor'), 'floor_dry': ((416, 128, 640, 360), 'floor_dry'),
}

full = Image.new('L', base.size, 0)
d = ImageDraw.Draw(full)
for kind, pts, w in DAMAGE:
    if kind == BLOB:
        x, y = pts
        d.ellipse((x - w, y - w * 0.7, x + w, y + w * 0.7), fill=255)
    else:
        d.line(pts, fill=255, width=w)
full.save(R5 + '/work/mask_full.png')

for name, (box, _) in CROPS.items():
    base.crop(box).save(R5 + '/work/crop_%s.png' % name)
    full.crop(box).convert('RGB').save(R5 + '/work/mask_%s.png' % name)
json.dump({k: v[0] for k, v in CROPS.items()}, open(R5 + '/work/crops.json', 'w'))
