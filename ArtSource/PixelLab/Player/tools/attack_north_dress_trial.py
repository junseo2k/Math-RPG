"""North greatsword combo: dress 3 approved stick frames (no AI generation).

Body/cape/medal = 16_idle_greatsword_pauldron_breath/2_north_1.png with the idle sword + hands cut by a geometric mask.
Back of head (hidden by the idle sword) = 12_run_calm/2_north.png frame 2, pasted through a mask.
Sword, hands, arms = redrawn from stick coords as separate layers.
Output: 34_attack_greatsword_combo_north/dress/trial1/ (layers, masks, coords, sheets). Originals untouched.
Exit code 0 = checks pass (96x96, <=16 colours, alpha 0/255).
"""
import json, math, os, sys
import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
NORTH = os.path.join(ROOT, '34_attack_greatsword_combo_north')
OUT = os.path.join(NORTH, 'dress', 'trial1')
IDLE = os.path.join(ROOT, '16_idle_greatsword_pauldron_breath', '2_north_1.png')
RUN = os.path.join(ROOT, '12_run_calm', '2_north.png')
OFF = (16, 40)          # idle 64 cell -> 96 canvas (feet at y=94), same as stick scripts
RUN_SHIFT = (-1, 1)     # run_calm head -> idle head (top row 15->16, right edge 37->36)

C = dict(  # all from the idle palette
    blade_hi=(187, 190, 190), blade=(165, 168, 167), blade_lo=(107, 115, 107),
    gold_hi=(252, 219, 109), gold=(210, 168, 85), gold_mid=(195, 150, 80), gold_lo=(174, 120, 58),
    bronze=(145, 81, 27), grip=(54, 46, 37), black=(0, 0, 0), olive=(91, 86, 66),
)

FRAMES = [  # (name, coords file, frame no., label)
    ('hit1_f5', 'stick/coords.json', 5, '1타 f5 베기 중간'),
    ('hit2_f8', 'stick/slash02/coords.json', 8, '2타 f8 머리 위'),
    ('hit3_f2', 'stick/slash03/coords.json', 2, '3타 f2 내려치기(판정)'),
]


def rgba(c):
    return (*c, 255)


def seg_dist(px, py, a, b):
    ax, ay = a; bx, by = b
    dx, dy = bx - ax, by - ay
    t = max(0.0, min(1.0, ((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy)))
    return math.hypot(px - ax - t * dx, py - ay - t * dy), t


def idle_masks():
    """Masks in 64px idle coords: idle sword band, hand/guard cluster, head-donor area."""
    sword = np.zeros((64, 64), bool)
    tip, pom = (9.9, 7.2), (39.8, 33.2)          # stick f1 tip/pommel /96 -> /64
    for y in range(64):
        for x in range(64):
            d, t = seg_dist(x + .5, y + .5, tip, pom)
            if d <= 3.0:
                sword[y, x] = True
    hands = np.zeros((64, 64), bool)
    poly = [(29, 20), (37, 19), (43, 27), (43, 35), (34, 35), (29, 29)]
    m = Image.new('L', (64, 64), 0); ImageDraw.Draw(m).polygon(poly, fill=255)
    hands |= np.array(m) > 0
    head = np.zeros((64, 64), bool); head[12:20, 22:38] = True; head[20:27, 22:36] = True   # back of head + collar; x>=36 below y20 = run_calm sword hilt, excluded
    return sword, hands, head


def build_body():
    idle = np.array(Image.open(IDLE).convert('RGBA'))
    run = np.array(Image.open(RUN).convert('RGBA'))[:, 2 * 68:2 * 68 + 64]
    donor = np.zeros_like(run)
    dx, dy = RUN_SHIFT
    donor[max(0, dy):64 + min(0, dy), max(0, dx):64 + min(0, dx)] = run[max(0, -dy):64 - max(0, dy), max(0, -dx):64 - max(0, dx)]
    sword, hands, headbox = idle_masks()
    cut = sword | hands
    body = idle.copy(); body[cut] = 0
    body[:25, 19:42] = 0   # idle head goes too: the head layer is shifted per frame, leftovers would double the hair edge
    # head layer = donor head (hair + neck) only, inside head box
    head = np.zeros_like(idle)
    hmask = headbox & (donor[..., 3] > 0) & (np.arange(64)[:, None] <= 25)
    head[hmask] = donor[hmask]
    # fill cut pixels below the head from the donor cape (only where idle had something under the cut)
    fill = cut & ~hmask & (donor[..., 3] > 0) & (np.arange(64)[:, None] > 25)
    body[fill] = donor[fill]
    return body, head, dict(sword=sword, hands=hands, head=hmask, cape_fill=fill)


def place(layer64, dx=0, dy=0, lift=0):
    """64px layer -> 96 canvas, shifted; lift raises the upper cape (rows < 34) by stretching."""
    a = layer64.copy()
    if lift:
        b = np.zeros_like(a)
        b[:34 - lift] = a[lift:34]
        b[34 - lift:] = a[34 - lift:]
        a = b
    can = np.zeros((96 + 64, 96 + 64, 4), np.uint8)   # idle rows below the feet fall off the 96 canvas
    can[OFF[1] + dy:OFF[1] + dy + 64, OFF[0] + dx:OFF[0] + dx + 64] = a
    return can[:96, :96].copy()


def split_feet(body64):
    feet = body64.copy(); feet[:51] = 0
    torso = body64.copy(); torso[51:] = 0
    return torso, feet


def draw_sword(k):
    im = Image.new('RGBA', (96, 96)); px = im.load()
    G = np.array(k['grip'], float); T = np.array(k['tip'], float); P = np.array(k['pommel'], float)
    u = (T - G) / np.linalg.norm(T - G); n = np.array([-u[1], u[0]])   # n = screen-left normal of the blade
    guard = G + u * 3.5 * max(k['foreshorten'], .6)
    L = np.linalg.norm(T - guard)
    for y in range(96):
        for x in range(96):
            p = np.array([x + .5, y + .5])
            t = (p - guard) @ u; s = (p - guard) @ n
            if 0 <= t <= L:
                half = 2.5 if t < L - 3 else 2.5 * (L - t) / 3 + .5
                if abs(s) <= half:
                    light_side = s * (1 if n[1] < 0 else -1)   # upper edge catches light (top-left light)
                    px[x, y] = rgba(C['blade_hi'] if light_side > .8 else C['blade'] if light_side > -.8 else C['blade_lo'])
    d = ImageDraw.Draw(im)
    gl = 3.5
    d.line([tuple(guard + n * gl), tuple(guard - n * gl)], fill=rgba(C['gold_lo']), width=2)
    d.line([tuple(guard + n * gl), tuple(guard - n * gl)], fill=rgba(C['gold']), width=1)
    d.line([tuple(guard - u * .5), tuple(P)], fill=rgba(C['grip']), width=2)
    pr = (round(P[0] - 1), round(P[1] - 1), round(P[0] + 1), round(P[1] + 1))
    d.rectangle(pr, fill=rgba(C['gold_mid']))
    return np.array(im)


def draw_hands(k):
    im = Image.new('RGBA', (96, 96)); d = ImageDraw.Draw(im)
    for h in ('hand_B', 'hand_A'):
        x, y = round(k[h][0]), round(k[h][1])
        d.rectangle((x - 1, y - 1, x + 1, y + 1), fill=rgba(C['gold']))
        d.point((x + 1, y + 1), fill=rgba(C['bronze'])); d.point((x - 1, y - 1), fill=rgba(C['gold_hi']))
    return np.array(im)


def draw_arms(k):
    im = Image.new('RGBA', (96, 96)); d = ImageDraw.Draw(im)
    for s in ('B', 'A'):
        pts = [tuple(k['shoulder_' + s]), tuple(k['elbow_' + s]), tuple(k['hand_' + s])]
        d.line(pts, fill=rgba(C['gold_lo']), width=3)
        d.line(pts[1:], fill=rgba(C['gold_mid']), width=1)
    return np.array(im)


def draw_smear():
    """3rd hit f2 smear (separate layer): narrow crescent of the overhead -> forward chop, bowed to screen right.
    Cream outer edge like the East chop smear; hidden by the head/cape like the blade (it is in front of the body)."""
    a = np.zeros((96, 96, 4), np.uint8)
    for y in range(96):
        for x in range(96):
            e1 = ((x + .5 - 47) / 8.0) ** 2 + ((y + .5 - 46) / 19.0) ** 2
            e2 = ((x + .5 - 44) / 6.5) ** 2 + ((y + .5 - 47) / 18.0) ** 2
            if e1 <= 1 and e2 > 1 and 26 <= y <= 60:
                a[y, x] = rgba(C['gold_hi'] if e1 > .62 else C['blade_hi'])   # outer rim brightest
    cream = (253, 230, 189)
    rim = (a[..., 3] > 0) & (a[..., 0] == C['gold_hi'][0])
    a[rim] = rgba(cream)
    return a


def over(dst, src):
    m = src[..., 3] > 0
    dst[m] = src[m]
    return dst


def check(img):
    a = img[..., 3]
    cols = {tuple(c) for c in img[a > 0][:, :3].tolist()}
    return img.shape[:2] == (96, 96) and set(np.unique(a).tolist()) <= {0, 255} and len(cols) <= 16, len(cols)


def main():
    os.makedirs(os.path.join(OUT, 'layers'), exist_ok=True)
    os.makedirs(os.path.join(OUT, 'masks'), exist_ok=True)
    body64, head64, masks = build_body()
    for name, m in masks.items():
        Image.fromarray((m * 255).astype(np.uint8)).save(os.path.join(OUT, 'masks', f'idle64_{name}.png'))
    torso64, feet64 = split_feet(body64)
    ok_all, used, coords, frames = True, set(), {}, []
    for name, cf, fno, label in FRAMES:
        k = json.load(open(os.path.join(NORTH, cf), encoding='utf-8'))['keys'][fno - 1]
        lift = 2 if name == 'hit2_f8' else 0           # stick slash02 uses lift=2 for the overhead pose
        bend, tw = k['knee_bend'], round(k['twist'])
        L = dict(
            feet=place(feet64),
            cape=place(torso64, 0, bend, lift),
            head=place(head64, tw, bend),
            arms=draw_arms(k) if k['depth'] == 'over' else np.zeros((96, 96, 4), np.uint8),
            hands=draw_hands(k), sword=draw_sword(k))
        order = ['feet', 'cape', 'arms', 'head', 'sword', 'hands'] if k['depth'] == 'over' \
            else ['sword', 'hands', 'feet', 'cape', 'head']   # 'behind' = in front of a north-facing body -> hidden
        img = np.zeros((96, 96, 4), np.uint8)
        for ln in order:
            over(img, L[ln])
            Image.fromarray(L[ln]).save(os.path.join(OUT, 'layers', f'{name}_{ln}.png'))
        ok, n = check(img); ok_all &= ok
        used |= {tuple(c) for c in img[img[..., 3] > 0][:, :3].tolist()}
        Image.fromarray(img).save(os.path.join(OUT, f'{name}.png'))
        frames.append((name, label, img))
        coords[name] = dict(k, source=cf, order=order, lift=lift, head_shift=[tw, bend])
        print(name, 'ok' if ok else 'FAIL', 'colours', n)
    json.dump(dict(idle=IDLE, head_donor=RUN + ' frame 2', run_shift=RUN_SHIFT, offset=OFF, frames=coords),
              open(os.path.join(OUT, 'coords.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    # smear version of hit3_f2: smear layer goes under the sword, over nothing else (body/head hide it)
    k3 = coords['hit3_f2']; sm = draw_smear()
    Image.fromarray(sm).save(os.path.join(OUT, 'layers', 'hit3_f2_smear.png'))
    img = np.zeros((96, 96, 4), np.uint8)
    for ln in k3['order']:
        if ln == 'sword':
            over(img, sm)
        over(img, np.array(Image.open(os.path.join(OUT, 'layers', f'hit3_f2_{ln}.png'))))
    ok, n = check(img); ok_all &= ok
    Image.fromarray(img).save(os.path.join(OUT, 'hit3_f2_smear.png'))
    print('hit3_f2_smear', 'ok' if ok else 'FAIL', 'colours', n)
    used |= {tuple(c) for c in img[img[..., 3] > 0][:, :3].tolist()}
    sheet = np.concatenate([f[2] for f in frames], axis=1)
    Image.fromarray(sheet).save(os.path.join(OUT, 'trial_3f.png'))
    print('palette total', len(used))
    ok_all &= len(used) <= 16
    sys.exit(0 if ok_all else 1)


if __name__ == '__main__':
    main()
