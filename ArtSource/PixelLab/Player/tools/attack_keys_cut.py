# 동쪽 내려베기(2026-11-07, 22번이 검을 돌리는 느낌이라는 사용자 의견): 손목 회전 대신 어깨부터 팔 전체가 크게 내려오고, 칼은 손을 늦게 따라오며, 베는 순간은 1~2프레임에 몰고 잔상을 붙인다. 핵심 포즈 3장: guide = 생성 입력(칼 뺀 idle + 팔·칼 밑그림), finish = 생성본 + idle 하체 + 정확한 칼, 나란히 보기
# python attack_keys.py guide | finish <take> | connect <take>
import math, os, sys
import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
REF = os.path.join(ROOT, '01_raw/attack_light_01_ref/0_east_f0.png')
OUT = os.path.join(ROOT, '23_attack_greatsword_light_01_cut')
RAW = os.path.join(ROOT, '01_raw/clean/attack_greatsword_light_01_cut')
W, H = 128, 108     # 칼을 줄이지 않도록 넓힌 캔버스 (idle 64칸은 OX, OY에 그대로 놓음)
OX, OY = 32, 32
BLADE_LEN = 32      # idle 칼날 길이(자루 앞 ~ 칼끝)
LEGS_Y = 46         # 이 줄 아래는 idle 그대로 = 발 접지 고정
STEEL = [(107, 115, 107), (165, 168, 167), (187, 190, 190), (165, 168, 167), (107, 115, 107)]  # idle 칼날과 같은 5줄 명암
GOLD, GOLD_D, BLACK, CAPE = (252, 219, 109), (195, 150, 80), (0, 0, 0), (120, 21, 36)
# 손 위치(idle 64칸 좌표), 칼 각도(화면 위 +, 동쪽 0), 어깨에서 손으로 가는 팔꿈치 위치
# hand = 앞손(자루의 칼밑 쪽), 뒷손은 자루를 따라 GRIP칸 뒤. 팔꿈치는 어깨에서 각 손으로 가는 중간 점
# hand = 앞손, ang = 칼 각도, body = 허리 위 이동(dx, dy): 내려칠 때 몸무게가 앞으로 실림. 발은 그대로
POSES = {
    'a_windup': dict(hand=(22, 21), ang=112, body=(-1, 0)),
    'b_mid':    dict(hand=(45, 38), ang=-8,  body=(2, 1)),
    'c_finish': dict(hand=(39, 45), ang=-45, body=(2, 1)),
}
BETWEEN = {
    'i1': dict(hand=(27, 25), ang=95,  body=(0, 0)),
    'ab': dict(hand=(38, 26), ang=48,  body=(1, 0)),
    'bc': dict(hand=(41, 45), ang=-38, body=(2, 2)),
}
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


def shifted_idle(body):
    # 허리 위를 옮기고 허리 아래는 그대로 둔다
    dx, dy = body
    src = np.array(idle_no_sword())
    a = src.copy()
    a[:LEGS_Y] = 0
    for y in range(LEGS_Y):
        a[min(y + dy, 63)] = np.where(a[min(y + dy, 63)][:, 3:] > 0, a[min(y + dy, 63)], np.roll(src[y], dx if y < 42 else dx // 2, axis=0))
    return Image.fromarray(a).copy()


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
    im.alpha_composite(shifted_idle(p['body']), (OX, OY))
    d = ImageDraw.Draw(im)
    dx, dy = p['body']
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
    # 머리와 다리·발은 idle 그대로
    dx, dy = p['body']
    a[OY + 12 + dy:OY + 28 + dy, OX + 20 + dx:OX + 38 + dx] = ids[12:28, 20:38]
    a[OY + LEGS_Y:OY + 64, OX:OX + 64] = ids[LEGS_Y:64, 0:64]
    im = Image.fromarray(a).copy()
    draw_blade(im, p['hand'], p['ang'])
    draw_grip(im, p)
    return im


def idle_canvas():
    idle = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    idle.alpha_composite(Image.open(REF).convert('RGBA'), (OX, OY))
    return idle


def report(names, frames):
    base = np.array(idle_canvas())
    rc = 0
    for k, im in zip(names, frames):
        n = np.array(im)
        cols = len({tuple(v) for v in n[n[:, :, 3] > 0][:, :3]})
        # 칼을 뺀 발 칸(회색 제외)이 idle과 다른 수
        steel = np.isin(n[:, :, :3].astype(int) @ [65536, 256, 1], [(107 << 16) + (115 << 8) + 107, (165 << 16) + (168 << 8) + 167, (187 << 16) + (190 << 8) + 190])
        f = (n != base).any(2) & ~steel
        feet = int(f[OY + 50:OY + 56, OX:OX + 64].sum())
        print(k, 'feet_diff', feet, 'colors', cols, 'alpha', sorted(set(n[:, :, 3].ravel().tolist())))
        rc |= cols > 16 or feet > 8
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


def smear(p0, p1, inner=23, outer=35):
    # 칼끝이 지나간 초승달: 앞 칼 위치에서 뾰족하고 지금 칼 위치에서 가장 두껍다. 칼 뒤에 깐다
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    def pt(u, t):
        ang = math.radians(p0['ang'] + (p1['ang'] - p0['ang']) * u)
        hx = p0['hand'][0] + (p1['hand'][0] - p0['hand'][0]) * u + OX
        hy = p0['hand'][1] + (p1['hand'][1] - p0['hand'][1]) * u + OY
        return (hx + math.cos(ang) * t, hy - math.sin(ang) * t)
    us = [i / 16 for i in range(17)]
    # 낫처럼 보이지 않게: 칼빛 회색 띠, 바깥 가장자리만 크림색
    o = [pt(u, outer) for u in us]
    n = [pt(u, outer - (outer - inner) * u) for u in us]
    d.polygon(o + n[::-1], fill=(187, 190, 190, 255))
    n = [pt(u, outer - 2 * u) for u in us]
    d.polygon(o + n[::-1], fill=(253, 230, 189, 255))
    return im


def connect(take):
    # 8프레임: idle, 들어 올림, 준비(정점), 내려옴(잔상), 타격(잔상), 따라감, 마무리, 멈춤
    A = {**POSES, **BETWEEN}
    keys = {k: Image.open(os.path.join(OUT, k + '.png')).convert('RGBA') for k in POSES}
    mid = {k: assemble(p, os.path.join(RAW, take, k + '.png')) for k, p in BETWEEN.items()}
    plain = [idle_canvas(), mid['i1'], keys['a_windup'], mid['ab'], keys['b_mid'], mid['bc'], keys['c_finish'], keys['c_finish'].copy()]
    smears = {3: smear(A['a_windup'], A['ab']), 4: smear(A['ab'], A['b_mid'])}
    frames = []
    for n, f in enumerate(plain):
        if n in smears:
            c = smears[n].copy()
            c.alpha_composite(f)
            f = c
        frames.append(f)
    def strip(fs, name):
        st = Image.new('RGBA', (W * 8, H))
        for n, f in enumerate(fs):
            st.alpha_composite(f, (n * W, 0))
        st.save(os.path.join(OUT, name))
    strip(frames, '0_east.png')
    strip(plain, '0_east_no_smear.png')
    bg = (58, 60, 70, 255)
    def gif(fs, ms, name, scale=3):
        out = []
        for f, m in zip(fs, ms):
            c = Image.new('RGBA', (W, H), bg)
            c.alpha_composite(f)
            out.append(c.resize((W * scale, H * scale), 0).convert('P', palette=Image.ADAPTIVE))
        out[0].save(os.path.join(OUT, 'preview', name), save_all=True, append_images=out[1:], duration=list(ms), loop=0)
    gif(frames, [60] * 8, 'cut_18fps_x3.gif')
    gif(frames, [180] * 8, 'cut_slow_x3.gif')
    gif(plain, [60] * 8, 'cut_no_smear_18fps_x3.gif')
    rows = Image.new('RGBA', (W * 4 * 3, H * 2 * 3), bg)
    for n, f in enumerate(frames):
        rows.alpha_composite(f.resize((W * 3, H * 3), 0), ((n % 4) * W * 3, (n // 4) * H * 3))
    rows.save(os.path.join(OUT, 'preview', 'frames_x3.png'))
    return report(['f%d' % (n + 1) for n in range(8)], plain)


if __name__ == '__main__':
    os.makedirs(os.path.join(OUT, 'guide'), exist_ok=True)
    if sys.argv[1] == 'finish':
        sys.exit(finish(sys.argv[2]))
    if sys.argv[1] == 'connect':
        sys.exit(connect(sys.argv[2]))
    if sys.argv[1] == 'guide':
        ims = []
        for k in list(POSES) + list(BETWEEN):
            g = guide(k)
            g.save(os.path.join(OUT, 'guide', k + '.png'))
            ims.append(g)
        sheet(ims, os.path.join(OUT, 'preview', 'guides_x4.png'))
