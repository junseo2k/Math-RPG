# 동쪽 베기 핵심 포즈 3장: guide = 생성 입력(칼 뺀 idle + 팔·칼 밑그림), finish = 생성본 + idle 하체 + 정확한 칼, 나란히 보기
# python attack_keys.py guide | finish <take> | connect <take>
import math, os, sys
import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
REF = os.path.join(ROOT, '01_raw/attack_light_01_ref/0_east_f0.png')
OUT = os.path.join(ROOT, '21_attack_greatsword_light_01_keys')
RAW = os.path.join(ROOT, '01_raw/clean/attack_greatsword_light_01_keys')
W, H = 128, 104     # 칼을 줄이지 않도록 넓힌 캔버스 (idle 64칸은 OX, OY에 그대로 놓음)
OX, OY = 32, 32
BLADE_LEN = 32      # idle 칼날 길이(자루 앞 ~ 칼끝)
LEGS_Y = 46         # 이 줄 아래는 idle 그대로 = 발 접지 고정
STEEL = [(107, 115, 107), (165, 168, 167), (187, 190, 190), (165, 168, 167), (107, 115, 107)]  # idle 칼날과 같은 5줄 명암
GOLD, GOLD_D, BLACK, CAPE = (252, 219, 109), (195, 150, 80), (0, 0, 0), (120, 21, 36)
# 손 위치(idle 64칸 좌표), 칼 각도(화면 위 +, 동쪽 0), 어깨에서 손으로 가는 팔꿈치 위치
POSES = {
    'a_windup': dict(hand=(26, 43), ang=-42, near_elbow=(21, 37), far_elbow=None, front=True),
    'b_mid':    dict(hand=(41, 36), ang=-4,  near_elbow=(33, 35), far_elbow=(36, 33), front=True),
    'c_finish': dict(hand=(39, 28), ang=82,  near_elbow=(35, 35), far_elbow=(39, 33), front=True),
}
# 프레임 연결용 중간 포즈. 8번은 마무리 몸 그대로 칼만 더 멈춰 가는 자세라 생성하지 않음
BETWEEN = {
    'i1': dict(hand=(32, 39), ang=8, near_elbow=(26, 37), far_elbow=(32, 35), front=True),
    'ab': dict(hand=(34, 41), ang=-22, near_elbow=(27, 38), far_elbow=(32, 37), front=True),
    'bc': dict(hand=(42, 32), ang=42, near_elbow=(35, 33), far_elbow=(38, 31), front=True),
}
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


def draw_blade(img, hand, ang, length=BLADE_LEN, ox=OX, oy=OY):
    # 칼날은 긴 축으로 한 칸씩 나아가며 짧은 축으로 같은 폭을 찍는다: 울퉁불퉁 없는 곧은 칼
    px = img.load()
    c, s_ = math.cos(math.radians(ang)), -math.sin(math.radians(ang))
    hx, hy = hand[0] + ox, hand[1] + oy
    start = 3
    major = max(abs(c), abs(s_))
    width = round(5 / major)
    steps = round(length * major)
    for i in range(steps):
        t = start + i / major
        cx, cy = hx + c * t, hy + s_ * t
        w = width - 2 if i >= steps - 2 else width
        for k in range(w):
            off = k - (w - 1) / 2
            x, y = (round(cx), round(cy + off)) if abs(c) >= abs(s_) else (round(cx + off), round(cy))
            if 0 <= x < W and 0 <= y < H:
                px[x, y] = STEEL[min(4, int((off + width / 2) * 5 / width))] + (255,)
    d = ImageDraw.Draw(img)
    nx, ny = -s_, c
    gx, gy = hx + c * 3, hy + s_ * 3
    d.line([(hx - c * 3, hy - s_ * 3), (gx, gy)], fill=(54, 46, 37, 255), width=2)  # 자루
    d.line([(gx + nx * 3, gy + ny * 3), (gx - nx * 3, gy - ny * 3)], fill=GOLD_D + (255,), width=2)  # 칼밑


def guide(pose):
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    im.alpha_composite(idle_no_sword(), (OX, OY))
    d = ImageDraw.Draw(im)
    p = {**POSES, **BETWEEN}[pose]
    at = lambda q: (q[0] + OX, q[1] + OY)
    # 먼 팔 -> 칼 -> 가까운 팔 순서
    for sh, el, col in ([(FAR_SHOULDER, p['far_elbow'], GOLD_D)] if p['far_elbow'] else []):
        d.line([at(sh), at(el), at(p['hand'])], fill=BLACK + (255,), width=5)
        d.line([at(sh), at(el), at(p['hand'])], fill=col + (255,), width=3)
    draw_blade(im, p['hand'], p['ang'])
    d.line([at(NEAR_SHOULDER), at(p['near_elbow']), at(p['hand'])], fill=BLACK + (255,), width=6)
    d.line([at(NEAR_SHOULDER), at(p['near_elbow']), at(p['hand'])], fill=GOLD + (255,), width=4)
    hx, hy = at(p['hand'])
    d.rectangle([hx - 2, hy - 2, hx + 2, hy + 2], fill=GOLD_D + (255,), outline=BLACK + (255,))
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
    a = snap(np.array(Image.open(src).convert('RGBA')))
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
    for y0, y1, x0, x1 in [(12, 28, 20, 38), (LEGS_Y, 64, 0, 64)]:
        a[OY + y0:OY + y1, OX + x0:OX + x1] = ids[y0:y1, x0:x1]
    im = Image.fromarray(a).copy()
    draw_blade(im, p['hand'], p['ang'])
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


def connect(take):
    # 8프레임: idle, 뒤로 빼기, 준비, 가속, 타격 중간, 감속, 마무리, 정착
    keys = {k: Image.open(os.path.join(OUT, k + '.png')).convert('RGBA') for k in POSES}
    mid = {k: assemble(p, os.path.join(RAW, take, k + '.png')) for k, p in BETWEEN.items()}
    settle = keys['c_finish'].copy()
    frames = [idle_canvas(), mid['i1'], keys['a_windup'], mid['ab'], keys['b_mid'], mid['bc'], keys['c_finish'], settle]
    strip = Image.new('RGBA', (W * 8, H))
    for n, f in enumerate(frames):
        strip.alpha_composite(f, (n * W, 0))
    strip.save(os.path.join(OUT, '0_east.png'))
    bg = (58, 60, 70, 255)
    def gif(ms, name, scale=3):
        fs = []
        for f in frames:
            c = Image.new('RGBA', (W, H), bg)
            c.alpha_composite(f)
            fs.append(c.resize((W * scale, H * scale), 0).convert('P', palette=Image.ADAPTIVE))
        fs[0].save(os.path.join(OUT, 'preview', name), save_all=True, append_images=fs[1:], duration=ms, loop=0)
    gif(60, 'slash_18fps_x3.gif')
    gif(180, 'slash_slow_x3.gif')
    sheet_rows = Image.new('RGBA', (W * 4 * 3, H * 2 * 3), bg)
    for n, f in enumerate(frames):
        sheet_rows.alpha_composite(f.resize((W * 3, H * 3), 0), ((n % 4) * W * 3, (n // 4) * H * 3))
    sheet_rows.save(os.path.join(OUT, 'preview', 'frames_x3.png'))
    return report(['f%d' % (n + 1) for n in range(8)], frames)


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
