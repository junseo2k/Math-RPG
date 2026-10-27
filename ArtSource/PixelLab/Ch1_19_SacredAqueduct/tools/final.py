# Layout mockup + readability mockup for the 19시 지하수로 test area.
import sys, os
from PIL import Image, ImageDraw, ImageEnhance
sys.path.insert(0, os.path.dirname(__file__))
import compose

ROOT = sys.argv[1]
OBJ = ROOT + '/objects/'
PLAYER = sys.argv[2]

base, used = compose.build()
for k, v in used.items():
    print(k, len(v), sorted(v))


def obj(name):
    return Image.open(OBJ + name).convert('RGBA')


def bottom_place(canvas, im, cx, foot_y):
    bb = im.getbbox()
    canvas.alpha_composite(im, (cx - im.width // 2, foot_y - bb[3]))


scene = base.copy()
crack = obj('decal_aqueduct_drycrack.png')
scene.alpha_composite(crack, (520, 150))
scene.alpha_composite(crack.transpose(Image.FLIP_LEFT_RIGHT), (530, 270))
banner, altar, urns = obj('obj_aqueduct_banner.png'), obj('obj_aqueduct_altar.png'), obj('obj_aqueduct_urns.png')
for cx in (208, 432):
    scene.alpha_composite(banner, (cx - 16, 40))
bottom_place(scene, altar, 320, 118)
bottom_place(scene, urns, 184, 124)
scene.save(ROOT + '/mockup_layout_640x360.png')
scene.resize((1920, 1080), Image.NEAREST).save(ROOT + '/mockup_layout_x3.png')

# readability: player frames at true game scale (48px frame, PPU 32) + placeholder danger zone
p = Image.open(PLAYER).convert('RGBA').crop((0, 0, 48, 48))


def readability(bg):
    s = bg.copy()
    zone = Image.new('RGBA', s.size)
    d = ImageDraw.Draw(zone)
    d.ellipse((392, 196, 488, 260), fill=(220, 40, 40, 90), outline=(255, 240, 220, 255))
    s.alpha_composite(zone)
    for cx, fy in ((320, 210), (96, 250), (560, 250), (440, 230)):  # floor, water, dry, inside zone
        bottom_place(s, p, cx, fy)
    return s


r1 = readability(scene)
dark = ImageEnhance.Brightness(scene.convert('RGB')).enhance(0.78)
dark = ImageEnhance.Color(dark).enhance(0.85).convert('RGBA')
r2 = readability(dark)
pair = Image.new('RGBA', (640, 730), (0, 0, 0, 255))
pair.alpha_composite(r1, (0, 0)); pair.alpha_composite(r2, (0, 370))
pair.resize((1280, 1460), Image.NEAREST).save(ROOT + '/mockup_readability_x2.png')
r1.resize((1920, 1080), Image.NEAREST).save(ROOT + '/mockup_readability_x3.png')
