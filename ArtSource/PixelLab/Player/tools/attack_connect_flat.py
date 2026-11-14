# 타격 -> 중간 2장 -> 마무리: 칼을 기울어진 수평면에서 돌린 궤적(점·선만, 생성 없음). 칼끝은 타원 위, 실제 길이 일정, 화면 길이만 변한다
# 종료 코드 0 = 끝 자세 일치, 한 방향 회전, 칼이 몸 축을 지나지 않음, 머리 위로 지나지 않음, 앞쪽 칼이 머리를 가리지 않음
import math, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont
import attack_keys_pose as K

OUT = os.path.join(K.OUT, 'connect')
HIT, END = K.POSES['b_mid'], K.POSES['c_finish_d']
SCREEN_LEN = 3 + K.BLADE_LEN   # 키포즈에서 손~칼끝 화면 길이
AXIS = -7                      # 타원 긴 축(두 끝 자세의 대칭축)
A = 37.0                       # 타원 긴 반지름 = 모형의 실제 손~칼끝 길이
CENTER = (28, 33)              # 몸 회전 축의 화면 위치
HAND_R, HAND_K = 11.0, 0.35    # 손 궤도 반지름과 납작한 정도
TORSO_R = 6.0                  # 몸통 반폭
HEAD_X, HEAD_TOP, HEAD_BOTTOM = (24, 38), 15, 29
FAR = 0.35                     # sin(psi)가 이보다 크면 칼이 화면 안쪽 = 몸 뒤로 그린다
STEEL = [(107, 115, 107), (165, 168, 167), (187, 190, 190)]
NAMES = ['타격', '중간 1', '중간 2', '마무리']
e1 = np.array([math.cos(math.radians(AXIS)), -math.sin(math.radians(AXIS))])
e2 = np.array([math.cos(math.radians(AXIS + 90)), -math.sin(math.radians(AXIS + 90))])


def vec(ang, length):
    return np.array([math.cos(math.radians(ang)), -math.sin(math.radians(ang))]) * length


v0, v1 = vec(HIT['ang'], SCREEN_LEN), vec(END['ang'], SCREEN_LEN)
# 타원 짧은 반지름: 타격 칼끝이 타원 위에 오도록 푼다
B = abs(float(v0 @ e2)) / math.sqrt(1 - (float(v0 @ e1) / A) ** 2)
PSI0 = math.atan2(float(v0 @ e2) / B, float(v0 @ e1) / A)
PSI1 = math.atan2(float(v1 @ e2) / B, float(v1 @ e1) / A)
if PSI1 < PSI0:
    PSI1 += 2 * math.pi


def hand_base(psi):
    return np.array(CENTER, float) + HAND_R * (math.cos(psi) * e1 + HAND_K * math.sin(psi) * e2)


RES0 = np.array(HIT['hand'], float) - hand_base(PSI0)
RES1 = np.array(END['hand'], float) - hand_base(PSI1)


def pose(t):
    psi = PSI0 + (PSI1 - PSI0) * t
    w = A * math.cos(psi) * e1 + B * math.sin(psi) * e2       # 손 -> 칼끝 화면 벡터
    hand = hand_base(psi) + RES0 * (1 - t) + RES1 * t
    unit = w / SCREEN_LEN      # 칼의 비율은 그대로, 화면에서만 줄어든다
    return dict(psi=psi, hand=hand, tip=hand + w, guard=hand + unit * 3, rear=hand - unit * K.GRIP, pommel=hand - unit * (K.GRIP + 3),
                ang=math.degrees(math.atan2(-w[1], w[0])) % 360, screen=float(np.linalg.norm(w)), behind=math.sin(psi) > FAR)


def to_model(q):
    # 몸 축 기준의 평면 좌표로 되돌림: 여기서 칼이 축을 지나면 몸 관통
    d = np.asarray(q, float) - np.array(CENTER, float)
    return np.array([d @ e1, (d @ e2) / HAND_K])


def seg_dist(a, b):
    ab = b - a
    return float(np.linalg.norm(a + ab * min(1, max(0, float(-a @ ab) / float(ab @ ab)))))


def body_mask(name):
    a = np.array(Image.open(os.path.join(K.OUT, name + '.png')).convert('RGBA'))
    gray = np.isin(a[:, :, :3].astype(int) @ [65536, 256, 1], [(r << 16) + (g << 8) + b for r, g, b in STEEL])
    return (a[:, :, 3] > 0) & ~gray


def line_mask(a, b, width):
    im = Image.new('L', (K.W, K.H), 0)
    ImageDraw.Draw(im).line([(a[0] + K.OX, a[1] + K.OY), (b[0] + K.OX, b[1] + K.OY)], fill=255, width=width)
    return np.array(im) > 0


def main():
    os.makedirs(OUT, exist_ok=True)
    ps = [pose(t) for t in [0, 1 / 3, 2 / 3, 1]]
    masks = [body_mask('b_mid'), body_mask('b_mid'), body_mask('c_finish_d'), body_mask('c_finish_d')]
    rows = np.zeros((K.H, K.W), bool)
    rows[:HEAD_BOTTOM + K.OY] = True
    lines, rc = [f'ellipse A {A:.1f} B {B:.1f} psi {math.degrees(PSI0):.0f} -> {math.degrees(PSI1):.0f}'], 0
    for n, (name, p, m) in enumerate(zip(NAMES, ps, masks)):
        step = 0 if n == 0 else p['ang'] - ps[n - 1]['ang']
        step = (step + 180) % 360 - 180
        b = line_mask(p['guard'], p['tip'], 5)
        on_head, on_body = int((b & m & rows).sum()), int((b & m & ~rows).sum())
        axis = seg_dist(to_model(p['guard']), to_model(p['tip']))
        over = HEAD_X[0] <= p['tip'][0] <= HEAD_X[1] and p['tip'][1] < HEAD_TOP
        lines.append(f"{name}: hand ({p['hand'][0]:.1f},{p['hand'][1]:.1f}) ang {p['ang']:.0f} step {step:.0f} screen_len {p['screen']:.1f} tip ({p['tip'][0]:.1f},{p['tip'][1]:.1f}) {'behind' if p['behind'] else 'front'} blade_on_head {on_head} blade_on_body {on_body} axis_dist {axis:.1f} over_head {over}")
        rc |= step < 0 or over or axis < TORSO_R or (on_head > 0 and not p['behind'])
    rc |= float(np.abs(ps[0]['hand'] - np.array(HIT['hand'])).max()) > 0.01 or float(np.abs(ps[-1]['tip'] - (np.array(END['hand']) + v1)).max()) > 0.01
    text = '\n'.join(lines) + f'\nexit {int(rc)}\n'
    print(text, end='')
    open(os.path.join(OUT, 'check_flat_20261107.txt'), 'w', encoding='utf-8').write(text)

    # 도식: 회색 = 포즈 윤곽(참고), 흰 = 칼날(몸 뒤면 윤곽에 가림), 갈색 = 손잡이, 노랑·청록 = 앞손·뒷손, 빨강 = 칼끝, 가는 선 = 손·칼끝 경로
    font = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 20)
    s, cw, ch, y0c = 5, K.W, 90, 8
    sheet = Image.new('RGB', (cw * s * 4 + 50, ch * s + 60), (58, 60, 70))
    d = ImageDraw.Draw(sheet)
    path = [pose(i / 40) for i in range(41)]
    for n, (name, p, m) in enumerate(zip(NAMES, ps, masks)):
        ox = 10 + n * (cw * s + 10)
        q = lambda v: (ox + (v[0] + K.OX) * s, 40 + (v[1] + K.OY - y0c) * s)
        def sword():
            d.line([q(p['guard']), q(p['tip'])], fill=(235, 235, 235), width=12)
            d.line([q(p['pommel']), q(p['guard'])], fill=(174, 120, 58), width=8)
        def body():
            for y in range(y0c, min(K.H, y0c + ch)):
                for x in range(K.W):
                    if m[y, x]:
                        d.rectangle([ox + x * s, 40 + (y - y0c) * s, ox + x * s + s - 1, 40 + (y - y0c) * s + s - 1], fill=(92, 95, 106))
        for key, col in [('hand', (150, 130, 75)), ('tip', (140, 75, 75))]:
            for a, b in zip(path, path[1:]):
                d.line([q(a[key]), q(b[key])], fill=col, width=1)
        if p['behind']:
            sword()
            body()
        else:
            body()
            sword()
        for key, col in [('rear', (90, 220, 220)), ('hand', (250, 215, 90)), ('tip', (235, 80, 80))]:
            cx, cy = q(p[key])
            d.ellipse([cx - 7, cy - 7, cx + 7, cy + 7], fill=col, outline=(0, 0, 0))
        d.text((ox + 6, 6), f"{name}  칼 {p['ang']:.0f}°  화면 길이 {p['screen']:.0f}칸{'  몸 뒤' if p['behind'] else ''}", font=font, fill=(255, 255, 255))
    sheet.save(os.path.join(OUT, 'connect_flat_x5.png'))
    return int(rc)


if __name__ == '__main__':
    sys.exit(main())
