# 동쪽 약베기 궤적 설계: 위에서 본 설계도 -> 게임 시점 투영(점·선만, 생성 없음)
# 종료 코드 0 = 칼 길이 일정, 칼 방향·손 방향 모두 한 방향, 칼이 대상을 오른쪽에서 왼쪽으로 통과, 칼이 몸을 지나지 않음, 앞쪽 칼이 머리를 가리지 않음
import math, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont
import attack_keys_pose as K

OUT = os.path.join(K.OUT, 'connect')
BLADE = 3 + K.BLADE_LEN     # 손 -> 칼끝
POMMEL = K.GRIP + 3         # 손 -> 손잡이 끝
TARGET, TARGET_R = (30.0, 0.0), 7.0   # 동쪽 대상(세계 좌표, 칸)
TORSO_R = 6.0
CENTER = (28.0, 36.0)       # 몸 축의 게임 화면 위치(idle 칸, 허리 높이)
CAMERA, TILT = 20.0, 15.0   # 카메라 각도, 칼 궤도면 기울기(먼 쪽이 높고 가까운 쪽이 낮음)
SQUASH = math.sin(math.radians(CAMERA + TILT))
HEAD_X, HEAD_TOP, HEAD_BOTTOM = (24, 37), 15, 29
# 자세표: 이름, 손 방향(도), 손 반지름, 칼 방향(도). 0 = 동쪽, + = 캐릭터 왼쪽(북), - = 오른쪽(남)
TABLE = [
    ('준비',     -128, 7.0, -135),
    ('풀림',      -85, 8.0,  -85),
    ('진입',      -37, 10.0, -35),
    ('타격',       -5, 11.0,   3),
    ('넘어감',     12, 11.0,  48),
    ('마무리',     20, 11.0,  84),   # 84도: 머리 뒤로 숨지 않고 머리 앞쪽에 서지 않는 왼쪽 끝
]
NOW = {'준비': K.POSES['a_windup'], '진입': K.POSES['b_mid'], '마무리': K.POSES['c_finish_d']}


def world(i):
    # i: 0 ~ len(TABLE)-1 사이 실수. 표를 선형으로 잇는다
    u = range(len(TABLE))
    th = math.radians(np.interp(i, u, [t[1] for t in TABLE]))
    r = float(np.interp(i, u, [t[2] for t in TABLE]))
    ph = math.radians(np.interp(i, u, [t[3] for t in TABLE]))
    hand = np.array([r * math.cos(th), r * math.sin(th)])
    d = np.array([math.cos(ph), math.sin(ph)])
    return dict(hand=hand, d=d, phi=math.degrees(ph), theta=math.degrees(th), rear=hand - d * K.GRIP, pommel=hand - d * POMMEL, guard=hand + d * 3, tip=hand + d * BLADE)


def screen(q):
    return np.array([CENTER[0] + q[0], CENTER[1] - SQUASH * q[1]])


def seg_dist(p, a, b):
    ab = b - a
    return float(np.linalg.norm(p - (a + ab * min(1, max(0, float((p - a) @ ab) / float(ab @ ab))))))


def check():
    fine = np.linspace(0, len(TABLE) - 1, 501)
    tgt = np.array(TARGET)
    rc, lines = 0, []
    lens = [float(np.linalg.norm(world(i)['tip'] - world(i)['pommel'])) for i in fine]
    rigid = max(lens) - min(lens) < 1e-6
    th = [t[1] for t in TABLE]
    ph = [t[3] for t in TABLE]
    mono = all(b > a for a, b in zip(th, th[1:])) and all(b > a for a, b in zip(ph, ph[1:]))
    # 대상과 칼이 닿는 구간, 칼이 대상 중심을 지나는 순간
    hits = [i for i in fine if seg_dist(tgt, world(i)['guard'], world(i)['tip']) <= TARGET_R]
    side = []
    for i in fine:
        w = world(i)
        side.append(float(w['d'][0] * (tgt - w['hand'])[1] - w['d'][1] * (tgt - w['hand'])[0]))
    cross = next((fine[n] for n in range(1, len(fine)) if side[n - 1] > 0 >= side[n]), None)
    point = None
    if cross is not None:
        w = world(cross)
        point = w['hand'] + w['d'] * float((tgt - w['hand']) @ w['d'])
    clear = min(seg_dist(np.zeros(2), world(i)['guard'], world(i)['tip']) for i in fine)
    lines.append(f"sword_len {lens[0]:.1f} rigid {rigid} one_direction {mono}")
    if hits:
        a, b = world(hits[0]), world(hits[-1])
        lines.append(f"target contact: blade phi {a['phi']:.0f} -> {b['phi']:.0f}, tip y {a['tip'][1]:.1f} -> {b['tip'][1]:.1f}")
    lines.append(f"crosses target center at phi {world(cross)['phi']:.1f}, point ({point[0]:.1f},{point[1]:.1f})" if cross is not None else 'does not cross target')
    lines.append(f"min blade to body axis {clear:.1f} (torso {TORSO_R})")
    passed = rigid and mono and cross is not None and bool(hits) and world(hits[0])['tip'][1] < 0 < world(hits[-1])['tip'][1] and clear >= TORSO_R
    rc |= not passed
    lines.append(f'camera {CAMERA} tilt {TILT} squash {SQUASH:.3f}')
    for n, t in enumerate(TABLE):
        w = world(n)
        h, tip, g = screen(w['hand']), screen(w['tip']), screen(w['guard'])
        behind = w['d'][1] > 0.35
        over_head = HEAD_X[0] <= tip[0] <= HEAD_X[1] and tip[1] < HEAD_TOP
        im = Image.new('L', (K.W, K.H), 0)
        ImageDraw.Draw(im).line([(g[0] + K.OX, g[1] + K.OY), (tip[0] + K.OX, tip[1] + K.OY)], fill=255, width=5)
        b = np.array(im) > 0
        head = np.zeros((K.H, K.W), bool)
        head[HEAD_TOP + K.OY:HEAD_BOTTOM + K.OY, HEAD_X[0] + K.OX:HEAD_X[1] + K.OX] = True
        on_head = int((b & head).sum())
        lines.append(f"{t[0]}: world hand ({w['hand'][0]:.1f},{w['hand'][1]:.1f}) phi {t[3]} | screen hand ({h[0]:.1f},{h[1]:.1f}) tip ({tip[0]:.1f},{tip[1]:.1f}) len {np.linalg.norm(tip - h):.1f} {'behind' if behind else 'front'} head_box_overlap {on_head} over_head {over_head}")
        rc |= over_head or (on_head > 0 and not behind)
        if t[0] in NOW and t[0] != '마무리':
            p = NOW[t[0]]
            lines.append(f"  vs current pose: hand ({p['hand'][0]},{p['hand'][1]}) screen ang {p['ang']} len {BLADE}")
    text = '\n'.join(lines) + f'\nexit {int(rc)}\n'
    print(text, end='')
    open(os.path.join(OUT, 'check_plan_20261107.txt'), 'w', encoding='utf-8').write(text)
    return int(rc), point


def plan_image(point):
    f = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 18)
    s, ox, oy = 8, 420, 440
    im = Image.new('RGB', (1120, 880), (58, 60, 70))
    d = ImageDraw.Draw(im)
    q = lambda v: (ox + v[0] * s, oy - v[1] * s)
    fine = np.linspace(0, len(TABLE) - 1, 201)
    for gx in range(-50, 81, 10):
        d.line([q((gx, -55)), q((gx, 55))], fill=(70, 72, 84))
    for gy in range(-50, 51, 10):
        d.line([q((-52, gy)), q((85, gy))], fill=(70, 72, 84))
    r = lambda c, rad, **kw: d.ellipse([q((c[0] - rad, c[1] + rad)), q((c[0] + rad, c[1] - rad))], **kw)
    r(TARGET, TARGET_R, fill=(120, 60, 60), outline=(235, 120, 120))
    r((0, 0), TORSO_R, fill=(92, 95, 106), outline=(200, 200, 210))
    for n in range(len(TABLE)):
        w = world(n)
        hit = TABLE[n][0] == '타격'
        d.line([q(w['pommel']), q(w['guard'])], fill=(174, 120, 58), width=4)
        d.line([q(w['guard']), q(w['tip'])], fill=(240, 240, 240) if hit else (150, 152, 160), width=5 if hit else 3)
    d.line([q(world(i)['hand']) for i in fine], fill=(250, 215, 90), width=3)
    d.line([q(world(i)['tip']) for i in fine], fill=(235, 80, 80), width=3)
    for n in range(len(TABLE)):
        w = world(n)
        for key, col, rad in [('hand', (250, 215, 90), 5), ('tip', (235, 80, 80), 6)]:
            cx, cy = q(w[key])
            d.ellipse([cx - rad, cy - rad, cx + rad, cy + rad], fill=col, outline=(0, 0, 0))
        tx, ty = q(w['tip'] + w['d'] * 2.5)
        d.text((tx - 5, ty - 12), str(n + 1), font=f, fill=(255, 255, 255))
    if point is not None:
        x, y = q(point)
        for a, b in [((-9, -9), (9, 9)), ((-9, 9), (9, -9))]:
            d.line([(x + a[0], y + a[1]), (x + b[0], y + b[1])], fill=(120, 240, 150), width=4)
    for i, (text, col) in enumerate([('위에서 본 설계도', (255, 255, 255)), ('회색 원 = 캐릭터, 붉은 원 = 동쪽 대상', (225, 225, 225)), ('노랑 = 손 경로, 빨강 = 칼끝 경로, 초록 X = 타격 지점', (225, 225, 225)),
                                     ('숫자: ' + ' '.join(f'{n + 1} {t[0]}' for n, t in enumerate(TABLE)), (225, 225, 225))]):
        d.text((20, 12 + i * 24), text, font=f, fill=col)
    d.text(q((62, 2)), '동쪽', font=f, fill=(255, 255, 255))
    d.text(q((-8, 50)), '캐릭터 왼쪽(북)', font=f, fill=(255, 255, 255))
    d.text(q((-8, -46)), '캐릭터 오른쪽(남)', font=f, fill=(255, 255, 255))
    im.save(os.path.join(OUT, 'plan_topdown.png'))


def game_image():
    f = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 18)
    idle = np.array(Image.open(os.path.join(K.ROOT, '01_raw/attack_light_01_ref/0_east_f0.png')).convert('RGBA'))
    a = idle.astype(int)
    gray = np.isin(a[:, :, :3] @ [65536, 256, 1], [(107 << 16) + (115 << 8) + 107, (165 << 16) + (168 << 8) + 167, (187 << 16) + (190 << 8) + 190, (91 << 16) + (86 << 8) + 66]) & (np.arange(64)[None, :] >= 33)
    m = (idle[:, :, 3] > 0) & ~gray
    s, cw, ch, x0, y0 = 4, 110, 96, -24, -22
    im = Image.new('RGB', (cw * s * 3 + 40, (ch * s + 34) * 2 + 10), (58, 60, 70))
    d = ImageDraw.Draw(im)
    fine = np.linspace(0, len(TABLE) - 1, 201)
    for n, t in enumerate(TABLE):
        ox, oy = 10 + (n % 3) * (cw * s + 10), 30 + (n // 3) * (ch * s + 34)
        q = lambda v: (ox + (v[0] - x0) * s, oy + (v[1] - y0) * s)
        w = world(n)
        P = {k: screen(w[k]) for k in ('hand', 'rear', 'pommel', 'guard', 'tip')}
        behind = w['d'][1] > 0.35
        def sword():
            d.line([q(P['guard']), q(P['tip'])], fill=(235, 235, 235), width=12)
            d.line([q(P['pommel']), q(P['guard'])], fill=(174, 120, 58), width=8)
        def body():
            for y in range(64):
                for x in range(64):
                    if m[y, x]:
                        d.rectangle([q((x, y)), (q((x, y))[0] + s - 1, q((x, y))[1] + s - 1)], fill=(92, 95, 106))
        if behind:
            sword()
            body()
        else:
            body()
            sword()
        for key, col in [('hand', (250, 215, 90)), ('tip', (170, 85, 85))]:   # 경로는 윤곽 위에
            d.line([q(screen(world(i)[key])) for i in fine], fill=col, width=2)
        for key, col in [('rear', (90, 220, 220)), ('hand', (250, 215, 90)), ('tip', (235, 80, 80))]:
            cx, cy = q(P[key])
            d.ellipse([cx - 6, cy - 6, cx + 6, cy + 6], fill=col, outline=(0, 0, 0))
        length = float(np.linalg.norm(P['tip'] - P['hand']))
        d.text((ox + 4, oy - 26), f"{n + 1} {t[0]}  화면 길이 {length:.0f}칸{'  몸 뒤' if behind else ''}", font=f, fill=(255, 255, 255))
    im.save(os.path.join(OUT, 'plan_game.png'))


if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    code, pt = check()
    plan_image(pt)
    game_image()
    sys.exit(code)
