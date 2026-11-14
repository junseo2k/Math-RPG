# 동쪽 약베기 단순 도형 애니메이션(생성 없음). 몸통·골반·손·칼을 3D로 놓고 게임 시점에 투영. 게임 코드·판정·그림은 건드리지 않는다
# 종료 코드 0 = 칼 길이 일정, 회전 한 방향, 대상을 오른쪽에서 왼쪽으로 통과, 0.22초 표시 프레임이 통과 구간 안, 칼이 몸 축을 지나지 않음
import math, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
OUT = os.path.join(ROOT, '27_attack_greatsword_light_01_block')
FPS = 16
HIT_TIME = 0.22             # CombatBalance.sword.hitDelay 값 그대로(읽기만)
CAM = math.radians(20)
CENTER = (28.0, 36.0)       # 몸 축의 화면 위치(idle 칸)
# 원본 측정: 칼밑 중심 (36,36), 칼끝 (57.5,11.5) -> 화면 칼날 32.6칸, 화면 각도 48.7도
BLADE_SCREEN, IDLE_ANG = 32.6, 48.7
IDLE_EL = math.degrees(math.atan(math.tan(math.radians(IDLE_ANG)) / math.cos(CAM)))
BLADE = BLADE_SCREEN / math.hypot(math.cos(math.radians(IDLE_EL)), math.sin(math.radians(IDLE_EL)) * math.cos(CAM))  # 3D 칼날
TO_GUARD, TO_REAR, TO_POMMEL = 2.0, 5.0, 7.0   # 앞손 기준. 자루는 손에 가려 원본에서 잴 수 없어 양손 폭으로 추정
TARGET, TARGET_R = np.array([30.0, 0.0]), 7.0
TORSO_R = 6.0
# 프레임별 자세. theta·r = 손, phi·el = 칼 방향·높이, sh·hip = 어깨·골반 회전, cdx·hdx = 가슴·골반 좌우 이동, cape = 망토 끝
FRAMES = [
    dict(name='idle',     theta=-33, r=8.0,  phi=0,   el=IDLE_EL, sh=0,   hip=0,   cdx=0,  hdx=0,  cape=-10),
    dict(name='당김',     theta=-45, r=8.0,  phi=-30, el=22,      sh=-15, hip=-8,  cdx=-1, hdx=-1, cape=-10),
    dict(name='준비',     theta=-60, r=8.0,  phi=-60, el=-10,     sh=-30, hip=-18, cdx=-3, hdx=-2, cape=-9),
    dict(name='타격',     theta=-8,  r=11.0, phi=-14, el=-2,      sh=-12, hip=0,   cdx=-1, hdx=0,  cape=-8),
    dict(name='통과',     theta=12,  r=11.0, phi=10,  el=4,       sh=8,   hip=12,  cdx=2,  hdx=2,  cape=-11),
    dict(name='넘어감',   theta=24,  r=10.0, phi=30,  el=9,       sh=22,  hip=18,  cdx=3,  hdx=2,  cape=-13),
    dict(name='감속',     theta=33,  r=10.0, phi=44,  el=13,      sh=28,  hip=18,  cdx=2,  hdx=1,  cape=-12),
    dict(name='마무리',   theta=37,  r=9.5,  phi=52,  el=14,      sh=30,  hip=18,  cdx=2,  hdx=1,  cape=-10),
]
STARTS = [n / FPS for n in range(len(FRAMES))]


def sw_dir(phi, el):
    p, e = math.radians(phi), math.radians(el)
    return np.array([math.cos(e) * math.cos(p), math.cos(e) * math.sin(p), math.sin(e)])


def solve(f):
    t = math.radians(f['theta'])
    hand = np.array([f['r'] * math.cos(t), f['r'] * math.sin(t), 0.0])
    d = sw_dir(f['phi'], f['el'])
    return dict(hand=hand, d=d, rear=hand - d * TO_REAR, pommel=hand - d * TO_POMMEL, guard=hand + d * TO_GUARD, tip=hand + d * (TO_GUARD + BLADE))


def scr(q):
    z = q[2] if len(q) > 2 else 0.0
    return np.array([CENTER[0] + q[0], CENTER[1] - (q[1] * math.sin(CAM) + z * math.cos(CAM))])


def contact(f):
    # 위에서 본 칼날 선분이 대상 원에 닿는가. 칼이 높이 들려 있으면(idle) 닿지 않는다
    if f['el'] > 25:
        return False
    s = solve(f)
    a, b = s['guard'][:2], s['tip'][:2]
    ab = b - a
    k = min(1, max(0, float((TARGET - a) @ ab) / float(ab @ ab)))
    return float(np.linalg.norm(TARGET - (a + ab * k))) <= TARGET_R


def at_time(t):
    # 연속 시간의 칼 자세(프레임 시작 시간 사이 선형)
    keys = ['theta', 'r', 'phi', 'el']
    return {k: float(np.interp(t, STARTS, [f[k] for f in FRAMES])) for k in keys}


def seg_axis(s):
    a, b = s['guard'][:2], s['tip'][:2]
    ab = b - a
    k = min(1, max(0, float((-a) @ ab) / float(ab @ ab)))
    return float(np.linalg.norm(a + ab * k))


def check():
    rc, lines = 0, []
    lens = [float(np.linalg.norm(solve(f)['tip'] - solve(f)['pommel'])) for f in FRAMES]
    lines.append(f"reference: blade screen {BLADE_SCREEN} at {IDLE_ANG} deg -> 3D blade {BLADE:.1f}, hilt below guard {TO_GUARD + TO_POMMEL:.1f} (estimated), total {lens[0]:.1f}, rigid {max(lens) - min(lens) < 1e-6}")
    rc |= max(lens) - min(lens) > 1e-6
    ph = [f['phi'] for f in FRAMES[2:]]
    rng = ph[-1] - ph[0]
    mono = all(b > a for a, b in zip(ph, ph[1:]))
    rc |= not mono
    lines.append(f"swing phi {ph[0]} -> {ph[-1]} = {rng} deg (was 219), one direction {mono}")
    fine = np.linspace(STARTS[2], STARTS[-1], 2001)
    win = [t for t in fine if contact(at_time(t))]
    lines.append(f"design reference only, virtual target (not game hit verification): contact {win[0]:.3f} ~ {win[-1]:.3f} s, center {(win[0] + win[-1]) / 2:.3f} s" if win else 'no contact')
    t0 = at_time(win[0]) if win else None
    shown = max(n for n, s in enumerate(STARTS) if s <= HIT_TIME)
    inside = bool(win) and win[0] <= HIT_TIME <= win[-1]
    rc |= not inside or not contact(FRAMES[shown])
    lines.append(f"hit {HIT_TIME} s shows f{shown + 1} ({FRAMES[shown]['name']}, start {STARTS[shown]:.3f}), frame in target {contact(FRAMES[shown])}, continuous inside {inside}")
    if win:
        a, b = solve(at_time(win[0])), solve(at_time(win[-1]))
        crossing = a['tip'][1] < 0 < b['tip'][1]
        rc |= not crossing
        lines.append(f"tip y at contact {a['tip'][1]:.1f} -> {b['tip'][1]:.1f}, right to left {crossing}")
    for n, f in enumerate(FRAMES):
        s = solve(f)
        L = float(np.linalg.norm(scr(s['tip']) - scr(s['guard'])))
        ax = seg_axis(s)
        lines.append(f"f{n + 1} {STARTS[n]:.3f}s {f['name']}: hand screen ({scr(s['hand'])[0]:.1f},{scr(s['hand'])[1]:.1f}) tip ({scr(s['tip'])[0]:.1f},{scr(s['tip'])[1]:.1f}) blade_screen {L:.1f} axis {ax:.1f} target {contact(f)}")
        rc |= n >= 2 and ax < TORSO_R
    text = '\n'.join(lines) + f'\nexit {int(rc)}\n'
    print(text, end='')
    open(os.path.join(OUT, 'check_16fps_20261107.txt'), 'w', encoding='utf-8').write(text)
    return int(rc)


S = 4
W, H = 128, 108
INSET = 250   # 칼을 가리지 않게 평면도는 캔버스 오른쪽 밖에 둔다
OX, OY = 32, 32
COL = dict(bg=(58, 60, 70), cape=(120, 21, 36), cape_d=(84, 16, 28), torso=(70, 66, 58), trim=(210, 168, 85), hip=(54, 46, 37), leg=(46, 42, 38), foot=(195, 150, 80),
           hair=(252, 219, 109), skin=(253, 230, 189), near=(252, 219, 109), far=(174, 120, 58), blade=(187, 190, 190), edge=(107, 115, 107), hilt=(145, 81, 27), guard=(217, 180, 90))


def P(v):
    return (OX * S + v[0] * S, OY * S + v[1] * S)


def draw_frame(n, font, small, label):
    f, s = FRAMES[n], solve(FRAMES[n])
    im = Image.new('RGB', (W * S + INSET, H * S), COL['bg'])
    d = ImageDraw.Draw(im)
    d.line([P((-30, 55)), P((95, 55))], fill=(80, 170, 255), width=1)
    # 몸 뼈대: 어깨·골반 선은 3D 회전 후 투영
    def turned(cx, cy, yaw, half, depth=0.0):
        a = math.radians(yaw)
        fw, right = np.array([math.cos(a), math.sin(a)]), np.array([math.sin(a), -math.cos(a)])
        pt = lambda k, g: np.array([cx + (right[0] * k + fw[0] * g), cy - math.sin(CAM) * (right[1] * k + fw[1] * g)])
        return pt(half, 0), pt(-half, 0), pt(0, depth)
    sc = (29 + f['cdx'], 30.0)
    hc = (28 + f['hdx'], 43.0)
    SR, SL, chest = turned(sc[0], sc[1], f['sh'], 8, 6)
    HR, HL, belly = turned(hc[0], hc[1], f['hip'], 6, 5)
    tp = lambda p: [P(q) for q in p]
    # 망토: 어깨에서 늦게 따라오는 자락
    d.polygon(tp([SL, SR, (hc[0] + f['cape'] + 4, 50), (hc[0] + f['cape'] - 2, 48)]), fill=COL['cape_d'])
    # 먼 다리
    d.line(tp([HL, ((HL[0] + 31) / 2 + 1.5, 49), (31, 54)]), fill=COL['leg'], width=4 * S // 1)
    d.rectangle([P((29, 53)), P((35, 55))], fill=COL['foot'])
    far_blade = s['d'][1] > 0.1
    def sword():
        g, t = scr(s['guard']), scr(s['tip'])
        wid = 5 * S
        d.line([P(g), P(t)], fill=COL['edge'], width=wid)
        d.line([P(g), P(t)], fill=COL['blade'], width=wid - 2 * S)
    def hilt():
        p, g = scr(s['pommel']), scr(s['guard'])
        d.line([P(p), P(g)], fill=(0, 0, 0), width=4 * S)
        d.line([P(p), P(g)], fill=COL['hilt'], width=2 * S)
        v = g - p
        v = v / max(1e-6, np.linalg.norm(v))
        nrm = np.array([-v[1], v[0]])
        d.line([P(g + nrm * 4), P(g - nrm * 4)], fill=(0, 0, 0), width=3 * S)
        d.line([P(g + nrm * 3.5), P(g - nrm * 3.5)], fill=COL['guard'], width=2 * S)
    def arm(a, h, col, sign):
        h2 = scr(h)
        el = ((a[0] + h2[0]) / 2 + sign * 1.5, (a[1] + h2[1]) / 2 + 3.5)
        d.line(tp([a, el, h2]), fill=(0, 0, 0), width=5 * S)
        d.line(tp([a, el, h2]), fill=col, width=3 * S)
    def fist(h, col):
        c = P(scr(h))
        r = 2.5 * S
        d.ellipse([c[0] - r, c[1] - r, c[0] + r, c[1] + r], fill=col, outline=(0, 0, 0), width=S)
    # 먼 팔 -> 칼이 안쪽이면 여기서 칼 -> 몸통 -> 앞쪽이면 몸통 뒤에 칼 -> 가까운 팔 -> 손
    arm(SL, s['rear'], COL['far'], 1)
    if far_blade:
        sword()
    d.polygon(tp([SL, SR, HR, HL]), fill=COL['torso'], outline=(0, 0, 0))
    d.line(tp([SR, HR]), fill=(0, 0, 0), width=3 * S)
    d.line(tp([SL, HL]), fill=(0, 0, 0), width=3 * S)
    d.line(tp([(sc[0], sc[1]), chest]), fill=COL['trim'], width=2 * S)
    d.line(tp([HL, HR]), fill=COL['hip'], width=3 * S)
    d.line(tp([(hc[0], hc[1]), belly]), fill=COL['trim'], width=S)
    # 가까운 다리: 뒤꿈치 들기 허용
    lift = 1 if n in (3, 4, 5) else 0
    d.line(tp([HR, ((HR[0] + 25) / 2 - 1.5, 49), (25, 54 - lift)]), fill=COL['leg'], width=4 * S)
    d.rectangle([P((22, 53 - lift)), P((28, 55 - lift))], fill=COL['foot'])
    # 어깨장식(왼쪽 = 먼 쪽), 머리: 몸통보다 작게 돈다
    d.ellipse([P((SL[0] - 3, SL[1] - 2)), P((SL[0] + 3, SL[1] + 2))], fill=COL['trim'], outline=(0, 0, 0))
    hx, hy = sc[0] + 1, 21.0
    yaw = f['sh'] * 0.4
    d.ellipse([P((hx - 7, hy - 7)), P((hx + 7, hy + 7))], fill=COL['hair'], outline=(0, 0, 0))
    fx = hx + 5.5 * math.cos(math.radians(yaw))
    fy = hy + 1 - 1.5 * math.sin(math.radians(yaw))
    d.ellipse([P((fx - 3, fy - 2)), P((fx + 3, fy + 3))], fill=COL['skin'], outline=(0, 0, 0))
    if not far_blade:
        sword()
    hilt()
    arm(SR, s['hand'], COL['near'], -1)
    fist(s['rear'], COL['far'])
    fist(s['hand'], COL['near'])
    # 대상 표시(위치 참고, 그림 효과 아님)
    t = scr(np.array([TARGET[0], TARGET[1], 0.0]))
    hit = contact(f)
    r = TARGET_R * S
    d.ellipse([P(t)[0] - r, P(t)[1] - r * .55, P(t)[0] + r, P(t)[1] + r * .55], outline=(240, 90, 90) if hit else (150, 110, 110), width=4 if hit else 2)
    # 위에서 본 작은 그림
    ix, iy, k = W * S + 100, 130, 1.9
    d.rectangle([ix - 92, iy - 95, ix + 135, iy + 95], fill=(44, 46, 54), outline=(90, 92, 104))
    Q = lambda v: (ix + v[0] * k, iy - v[1] * k)
    d.text((ix - 90, iy - 92), '위에서 본 모습 (가상 대상)', font=small, fill=(220, 220, 220))
    tt = Q(TARGET)
    d.ellipse([tt[0] - TARGET_R * k, tt[1] - TARGET_R * k, tt[0] + TARGET_R * k, tt[1] + TARGET_R * k], fill=(120, 50, 50) if hit else (70, 50, 54), outline=(235, 120, 120))
    d.ellipse([Q((-TORSO_R, TORSO_R)), Q((TORSO_R, -TORSO_R))], fill=(92, 95, 106))
    a = math.radians(f['sh'])
    d.line([Q((8 * math.sin(a), -8 * math.cos(a))), Q((-8 * math.sin(a), 8 * math.cos(a)))], fill=COL['trim'], width=3)
    d.line([Q(s['pommel']), Q(s['guard'])], fill=COL['hilt'], width=3)
    d.line([Q(s['guard']), Q(s['tip'])], fill=(235, 235, 235), width=3)
    c = Q(s['hand'])
    d.ellipse([c[0] - 3, c[1] - 3, c[0] + 3, c[1] + 3], fill=COL['near'])
    # 글자
    shown = max(i for i, x in enumerate(STARTS) if x <= HIT_TIME)
    d.text((10, 8), f"f{n + 1} {f['name']}  시작 {STARTS[n]:.3f}초", font=font, fill=(255, 255, 255))
    d.text((10, 36), label, font=small, fill=(190, 190, 200))
    if n == shown:
        d.text((10, 58), '게임 판정 시각 0.22초가 이 프레임', font=small, fill=(120, 240, 150))
    return im


def main():
    os.makedirs(OUT, exist_ok=True)
    rc = check()
    font = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 22)
    small = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 15)
    real = [draw_frame(n, font, small, '게임 속도 16fps') for n in range(8)]
    slow = [draw_frame(n, font, small, '느린 재생') for n in range(8)]
    frames = real + slow
    # GIF는 10ms 단위라 60·70ms를 섞어 16fps(62.5ms)에 맞춘다. 끝에서 잠깐 멈춘 뒤 느린 재생
    dur = [60, 60, 60, 70, 60, 60, 60, 500] + [220] * 7 + [800]
    pal = [im.convert('P', palette=Image.ADAPTIVE, colors=128) for im in frames]
    pal[0].save(os.path.join(OUT, 'block_anim_16fps.gif'), save_all=True, append_images=pal[1:], duration=dur, loop=0, disposal=1)
    return rc


if __name__ == '__main__':
    sys.exit(main())
