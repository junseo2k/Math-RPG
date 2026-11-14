# 동쪽 약베기 가로지르기판(2026-11-07 사용자 명세): 옆구리 준비 -> 앞 통과 -> 왼쪽으로 넘어감. 머리 위로 들지 않고, 팔을 곧게 뻗지 않고, 몸 높이를 떨어뜨리지 않는다. 잔상 없음
# python attack_keys.py guide | finish <take> | connect <take>
import math, os, sys
import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
REF = os.path.join(ROOT, '01_raw/attack_light_01_ref/0_east_f0.png')
OUT = os.path.join(ROOT, '25_attack_greatsword_light_01_cross')
RAW = os.path.join(ROOT, '01_raw/clean/attack_greatsword_light_01_cross')
W, H = 128, 108     # 칼을 줄이지 않도록 넓힌 캔버스 (idle 64칸은 OX, OY에 그대로 놓음)
OX, OY = 32, 32
BLADE_LEN = 32      # idle 칼날 길이(자루 앞 ~ 칼끝)
LEGS_Y = 46         # 이 줄 아래는 idle 그대로 = 발 접지 고정
STEEL = [(107, 115, 107), (165, 168, 167), (187, 190, 190), (165, 168, 167), (107, 115, 107)]  # idle 칼날과 같은 5줄 명암
GOLD, GOLD_D, BLACK, CAPE = (252, 219, 109), (195, 150, 80), (0, 0, 0), (120, 21, 36)
# 손 위치(idle 64칸 좌표), 칼 각도(화면 위 +, 동쪽 0), 어깨에서 손으로 가는 팔꿈치 위치
# hand = 앞손(자루의 칼밑 쪽), 뒷손은 자루를 따라 GRIP칸 뒤. 팔꿈치는 어깨에서 각 손으로 가는 중간 점
# hand = 앞손, ang = 칼 각도, body = 허리 위 이동(dx, dy): 내려칠 때 몸무게가 앞으로 실림. 발은 그대로
# hand = 앞손, ang = 칼 각도, hip·chest·head = idle에서 옮기는 칸(dx, dy), cape = 망토 끝이 늦게 따라오는 칸. 발은 옮기지 않는다
# 몸 높이(dy)는 세 포즈 모두 같게 둔다. 힘은 좌우 체중 이동과 회전으로 보여 준다
POSES = {
    'a_windup': dict(hand=(28, 41), ang=-28, hip=(-1, 1), chest=(-2, 1), head=(-1, 1), cape=0),
    'b_mid':    dict(hand=(37, 38), ang=4,   hip=(1, 1),  chest=(1, 1),  head=(1, 1),  cape=-2),
    'c_finish': dict(hand=(38, 31), ang=40,  hip=(1, 1),  chest=(1, 1),  head=(1, 1),  cape=1),
}
BETWEEN = {}  # 중간 프레임은 포즈 승인 뒤에 정한다
GRIP = 6
NEAR_SHOULDER, FAR_SHOULDER = (27, 31), (33, 30)


def idle_no_sword():
    a = np.array(Image.open(REF).convert('RGBA'))
    for y in range(64):
        for x in range(64):
            if x >= 33 and y <= 41 and tuple(a[y, x, :3]) in [(107, 115, 107), (165, 168, 167), (187, 190, 190), (91, 86, 66)]:
                a[y, x] = 0
            if x >= 36 and 28 <= y <= 42:   # 칼밑·손 (위쪽은 머리카락이라 남김)
                a[y, x] = 0
    return Image.fromarray(a)


def lerp_body(p, y):
    # 줄마다 옮길 칸: 발(50 아래) 0 -> 골반(42) -> 가슴(30) -> 머리(24 위)
    pts = [(24, p['head']), (30, p['chest']), (42, p['hip']), (50, (0, 0))]
    if y <= 24:
        return p['head']
    if y >= 50:
        return (0, 0)
    for (y0, a), (y1, b) in zip(pts, pts[1:]):
        if y0 <= y <= y1:
            u = (y - y0) / (y1 - y0)
            return (a[0] + (b[0] - a[0]) * u, a[1] + (b[1] - a[1]) * u)


def warped_idle(p):
    # 전신 자세 밑그림. 줄이 비지 않도록 도착 줄마다 가장 가까운 원본 줄을 찾는다
    src = np.array(idle_no_sword())
    dest = {}
    for y in range(64):
        dest[y] = y + lerp_body(p, y)[1]
    out = np.zeros_like(src)
    for Y in range(64):
        y = min(range(64), key=lambda v: (abs(dest[v] - Y), v))
        dx = round(lerp_body(p, y)[0])
        for x in range(64):
            if not src[y, x, 3]:
                continue
            is_cape = tuple(src[y, x, :3]) in [(64, 11, 24), (120, 21, 36)] and x < 25 and y >= 28
            nx = x + (round(dx + p['cape'] * max(0, y - 28) / 20) if is_cape else dx)
            if 0 <= nx < 64 and not (is_cape and out[Y, nx, 3]):
                out[Y, nx] = src[y, x]
    return Image.fromarray(out).copy()


def draw_blade(img, hand, ang, length=BLADE_LEN, ox=OX, oy=OY):
    # 칼날은 긴 축으로 한 칸씩 나아가며 짧은 축으로 같은 폭을 찍는다: 울퉁불퉁 없는 곧은 칼
    px = img.load()
    c, s_ = math.cos(math.radians(ang)), -math.sin(math.radians(ang))
    hx, hy = hand[0] + ox, hand[1] + oy
    start = 3
    major = max(abs(c), abs(s_))
    width = round(5.7 / major)  # idle 칼날 폭(가로 7~8칸, 48도)에 맞춤
    steps = round(length * major)
    for i in range(steps):
        t = start + i / major
        cx, cy = hx + c * t, hy + s_ * t
        w = width - 2 if i >= steps - 2 else width
        for k in range(w):
            off = k - (w - 1) / 2
            # 짝수 폭에서 .5 반올림이 칸을 건너뛰지 않도록 시작 칸에서 정수로 센다
            x, y = (round(cx), math.floor(cy - (w - 1) / 2 + 0.5) + k) if abs(c) >= abs(s_) else (math.floor(cx - (w - 1) / 2 + 0.5) + k, round(cy))
            if 0 <= x < W and 0 <= y < H:
                px[x, y] = STEEL[min(4, int((off + width / 2) * 5 / width))] + (255,)
    d = ImageDraw.Draw(img)
    nx, ny = -s_, c
    gx, gy = hx + c * 3, hy + s_ * 3
    d.line([(hx - c * (GRIP + 3), hy - s_ * (GRIP + 3)), (gx, gy)], fill=(54, 46, 37, 255), width=2)  # 긴 자루
    d.line([(gx + nx * 3, gy + ny * 3), (gx - nx * 3, gy - ny * 3)], fill=GOLD_D + (255,), width=2)  # 칼밑


def draw_grip(img, p):
    # 두 손을 자루 위에 또렷하게: 앞손과 뒷손
    d = ImageDraw.Draw(img)
    c, s_ = math.cos(math.radians(p['ang'])), -math.sin(math.radians(p['ang']))
    hx, hy = p['hand'][0] + OX, p['hand'][1] + OY
    for q in [(hx - c * GRIP, hy - s_ * GRIP), (hx, hy)]:
        x, y = round(q[0]), round(q[1])
        # 모서리를 깎은 주먹: 밑은 어두운 금, 윗줄만 밝게
        d.rectangle([x - 2, y - 1, x + 2, y + 1], fill=BLACK + (255,))
        d.rectangle([x - 1, y - 2, x + 1, y + 2], fill=BLACK + (255,))
        d.rectangle([x - 1, y - 1, x + 1, y + 1], fill=GOLD_D + (255,))
        d.line([(x - 1, y - 1), (x + 1, y - 1)], fill=GOLD + (255,))


def guide(pose):
    p = {**POSES, **BETWEEN}[pose]
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    im.alpha_composite(warped_idle(p), (OX, OY))
    d = ImageDraw.Draw(im)
    dx, dy = p['chest']
    c, s_ = math.cos(math.radians(p['ang'])), -math.sin(math.radians(p['ang']))
    rear = (p['hand'][0] - c * GRIP, p['hand'][1] - s_ * GRIP)
    at = lambda q: (q[0] + OX, q[1] + OY)
    def arm(sh, hand, col, width):
        sh = (sh[0] + dx, sh[1] + dy)
        el = ((sh[0] + hand[0]) / 2 - 1, (sh[1] + hand[1]) / 2 + 3)
        d.line([at(sh), at(el), at(hand)], fill=BLACK + (255,), width=width + 2)
        d.line([at(sh), at(el), at(hand)], fill=col + (255,), width=width)
    arm(FAR_SHOULDER, rear, GOLD_D, 3)
    draw_blade(im, p['hand'], p['ang'])
    arm(NEAR_SHOULDER, p['hand'], GOLD, 4)
    draw_grip(im, p)
    return im


def sheet(images, path, scale=4):
    bg = (58, 60, 70, 255)
    pad = 8
    s = Image.new('RGBA', ((W * scale + pad) * len(images) + pad, H * scale + pad * 2), bg)
    d = ImageDraw.Draw(s)
    for n, im in enumerate(images):
        x0 = pad + n * (W * scale + pad)
        s.alpha_composite(im.resize((W * scale, H * scale), 0), (x0, pad))
        fy = pad + (OY + 55) * scale   # 발 접지선
        d.line([(x0, fy), (x0 + W * scale, fy)], fill=(80, 170, 255, 255))
    s.save(path)


def snap(a):
    pal = np.array([[int(l.strip()[i:i + 2], 16) for i in (0, 2, 4)] for l in open(os.path.join(ROOT, '02_final/palette.hex')) if l.strip()])
    d = ((a[:, :, :3].astype(int)[:, :, None] - pal) ** 2).sum(3)
    a[:, :, :3] = pal[d.argmin(2)]
    a[:, :, 3] = (a[:, :, 3] > 127) * 255
    return a


def plant_feet(a):
    # 사용자 결정(2026-11-07 A줄 + 뒷발만 수정): 뒷발이 idle 범위 밖이면 무릎 아래를 기울여 되돌리고, 앞다리 뒤로 그린다. 바닥이 뜨면 1칸 내린다
    RED = [(64, 11, 24), (120, 21, 36)]
    top, bot, foot_x = OY + 47, OY + 54, OX + 23
    reg = a[OY + 50:bot + 1, OX + 14:OX + 38]   # 망토 끝이 섞이지 않는 발 줄로 뒷발 끝을 찾는다
    occ = (reg[:, :, 3] > 0) & ~np.isin(reg[:, :, :3].astype(int) @ [65536, 256, 1], [(c[0] << 16) + (c[1] << 8) + c[2] for c in RED])
    cols = np.where(occ.any(0))[0]
    x0 = OX + 14 + int(cols.min())
    dx = foot_x - x0
    if dx > 0:
        lo = x0 + 3
        gap = lo + int(np.argmin((a[top + 2:bot + 1, lo:OX + 30, 3] > 0).sum(0)))  # 두 다리 사이 빈 칸
        src = a.copy()
        for y in range(top, bot + 1):
            sh = round(dx * min(1, (y - top + 1) / 4))   # 무릎에서 기울기 시작해 y50부터 발 전체가 제자리
            a[y, x0:gap][~np.isin(a[y, x0:gap, :3].astype(int) @ [65536, 256, 1], [(c[0] << 16) + (c[1] << 8) + c[2] for c in RED])] = 0
            for x in range(x0, gap):
                if src[y, x, 3] and tuple(src[y, x, :3]) not in RED and not (x + sh >= gap and a[y, x + sh, 3]):
                    a[y, x + sh] = src[y, x]
    # 바닥 줄을 idle과 같은 y54로 맞춘다: 발목 아래만 위아래로 옮긴다
    rows = np.where((a[OY + 48:OY + 60, OX + 14:OX + 38, 3] > 0).any(1))[0]
    d = 54 - (int(rows.max()) + 48)
    if d:
        m = a[OY + 48:OY + 60, OX + 14:OX + 38].copy()
        a[OY + 48:OY + 60, OX + 14:OX + 38] = np.roll(m, d, axis=0)
        if d > 0:
            a[OY + 48:OY + 48 + d, OX + 14:OX + 38] = m[:d]
        else:
            a[OY + 60 + d:OY + 60, OX + 14:OX + 38] = 0   # 위로 올릴 때 아래로 감긴 줄을 지운다


def assemble(p, src):
    ids = np.array(idle_no_sword())
    g = snap(np.array(Image.open(src).convert('RGBA')))
    a = np.zeros((H, W, 4), g.dtype)  # 생성본(104줄)보다 넓힌 캔버스: 칼끝이 바닥에 닿지 않게
    a[:g.shape[0]] = g
    # 생성 칼을 지운다: 손에서 칼끝까지 폭 ±4칸의 회색
    c, s_ = math.cos(math.radians(p['ang'])), -math.sin(math.radians(p['ang']))
    hx, hy = p['hand'][0] + OX, p['hand'][1] + OY
    for y in range(H):
        for x in range(W):
            t = (x - hx) * c + (y - hy) * s_
            w = abs(-(x - hx) * s_ + (y - hy) * c)
            if t > 4 and w < 5 and a[y, x, 3] and tuple(a[y, x, :3]) in [(107, 115, 107), (165, 168, 167), (187, 190, 190), (91, 86, 66)]:
                a[y, x] = 0
    plant_feet(a)
    im = Image.fromarray(a).copy()
    draw_blade(im, p['hand'], p['ang'])
    draw_grip(im, p)
    return im


def idle_canvas():
    idle = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    idle.alpha_composite(Image.open(REF).convert('RGBA'), (OX, OY))
    return idle


def report(names, frames):
    # 발: 바닥 줄 위치와 발끝 가로 범위가 idle에서 몇 칸 벗어나는지. 칼은 빼고 잰다
    GRAY = [(107 << 16) + (115 << 8) + 107, (165 << 16) + (168 << 8) + 167, (187 << 16) + (190 << 8) + 190]
    def feet(n):
        m = (n[:, :, 3] > 0) & ~np.isin(n[:, :, :3].astype(int) @ [65536, 256, 1], GRAY)
        r = m[OY + 50:OY + 58, OX + 14:OX + 42]
        ys = np.where(r.any(1))[0]
        xs = np.where(r.any(0))[0]
        return int(ys.max()) + 50, int(xs.min()) + 14, int(xs.max()) + 14
    base = feet(np.array(idle_canvas()))
    rc = 0
    print('idle feet bottom,x0,x1', base)
    for k, im in zip(names, frames):
        n = np.array(im)
        cols = len({tuple(v) for v in n[n[:, :, 3] > 0][:, :3]})
        f = feet(n)
        print(k, 'feet', f, 'colors', cols, 'alpha', sorted(set(n[:, :, 3].ravel().tolist())))
        rc |= cols > 16 or abs(f[0] - base[0]) > 1 or abs(f[1] - base[1]) > 2 or abs(f[2] - base[2]) > 2
    return int(rc)


def finish(take):
    out = [assemble(p, os.path.join(RAW, take, k + '.png')) for k, p in POSES.items()]
    for k, im in zip(POSES, out):
        im.save(os.path.join(OUT, k + '.png'))
    idle = idle_canvas()
    arc = idle.copy()
    arc.putalpha(arc.getchannel('A').point(lambda v: v // 3))
    for im in out:
        arc.alpha_composite(Image.fromarray(np.where(np.array(im)[:, :, 3:] > 0, np.array(im) * [1, 1, 1, 0] + [0, 0, 0, 150], 0).astype('uint8')))
    sheet([idle] + out + [arc], os.path.join(OUT, 'preview', 'keys_x3.png'), 3)
    return report(list(POSES), out)


if __name__ == '__main__':
    os.makedirs(os.path.join(OUT, 'guide'), exist_ok=True)
    if sys.argv[1] == 'finish':
        sys.exit(finish(sys.argv[2]))
    if sys.argv[1] == 'guide':
        ims = []
        for k in list(POSES) + list(BETWEEN):
            g = guide(k)
            g.save(os.path.join(OUT, 'guide', k + '.png'))
            ims.append(g)
        sheet(ims, os.path.join(OUT, 'preview', 'guides_x4.png'))
