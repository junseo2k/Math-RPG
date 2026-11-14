# 타격 -> 중간 2장 -> 마무리 연결 도식(점·선만, 생성 없음)과 강체·충돌 검사. 종료 코드 0 = 칼 길이·회전 방향 통과이고 머리와 겹침 없음
import math, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont
import attack_keys_pose as K

OUT = os.path.join(K.OUT, 'connect')
HIT, END = K.POSES['b_mid'], K.POSES['c_finish_d']
CTRL = (32, 22)     # 손 호의 조절점: 손이 가슴 앞을 지나 왼쪽으로 간다
TS = [0, 1 / 3, 2 / 3, 1]
NAMES = ['타격', '중간 1', '중간 2', '마무리']
TIP = 3 + K.BLADE_LEN   # 손에서 칼끝까지
POMMEL = K.GRIP + 3     # 손에서 손잡이 끝까지
HEAD_BOTTOM = 29        # idle 칸에서 머리 아래 줄
SCALE = 5
STEEL = [(107, 115, 107), (165, 168, 167), (187, 190, 190)]


def pose(t):
    # 각도는 -22도에서 188도로 한 방향(오른쪽 -> 앞 -> 왼쪽)으로만 움직인다
    u = 1 - t
    hand = (u * u * HIT['hand'][0] + 2 * u * t * CTRL[0] + t * t * END['hand'][0],
            u * u * HIT['hand'][1] + 2 * u * t * CTRL[1] + t * t * END['hand'][1])
    ang = HIT['ang'] + (END['ang'] - HIT['ang']) * t
    c, s = math.cos(math.radians(ang)), -math.sin(math.radians(ang))
    at = lambda d: (hand[0] + c * d, hand[1] + s * d)
    return dict(hand=hand, ang=ang, rear=at(-K.GRIP), pommel=at(-POMMEL), guard=at(3), tip=at(TIP))


def body_mask(name):
    a = np.array(Image.open(os.path.join(K.OUT, name + '.png')).convert('RGBA'))
    gray = np.isin(a[:, :, :3].astype(int) @ [65536, 256, 1], [(r << 16) + (g << 8) + b for r, g, b in STEEL])
    return (a[:, :, 3] > 0) & ~gray


def blade_mask(p):
    im = Image.new('L', (K.W, K.H), 0)
    ImageDraw.Draw(im).line([(p['guard'][0] + K.OX, p['guard'][1] + K.OY), (p['tip'][0] + K.OX, p['tip'][1] + K.OY)], fill=255, width=5)
    return np.array(im) > 0


def main():
    os.makedirs(OUT, exist_ok=True)
    ps = [pose(t) for t in TS]
    masks = [body_mask('b_mid'), body_mask('b_mid'), body_mask('c_finish_d'), body_mask('c_finish_d')]
    head = np.zeros((K.H, K.W), bool)
    head[:HEAD_BOTTOM + K.OY] = True   # 머리는 포즈 윤곽의 머리 줄로 잰다(몸이 옮겨져도 맞게)
    lines, rc = [], 0
    for n, (name, p, m) in enumerate(zip(NAMES, ps, masks)):
        length = math.dist(p['pommel'], p['tip'])
        b = blade_mask(p)
        over_body, over_head = int((b & m & ~head).sum()), int((b & m & head).sum())
        step = 0 if n == 0 else p['ang'] - ps[n - 1]['ang']
        hand_step = 0 if n == 0 else math.dist(p['hand'], ps[n - 1]['hand'])
        lines.append(f"{name}: hand ({p['hand'][0]:.1f},{p['hand'][1]:.1f}) ang {p['ang']:.0f} step {step:.0f} hand_step {hand_step:.1f} sword_len {length:.1f} tip ({p['tip'][0]:.1f},{p['tip'][1]:.1f}) blade_on_body {over_body} blade_on_head {over_head}")
        rc |= abs(length - (TIP + POMMEL)) > 0.01 or step < 0 or step >= 180 or over_head > 0
    text = '\n'.join(lines) + f'\nexit {int(rc)}\n'
    print(text, end='')
    open(os.path.join(OUT, 'check_20261107.txt'), 'w', encoding='utf-8').write(text)

    # 도식: 회색 = 포즈 윤곽(참고), 흰 = 칼날, 갈색 = 손잡이, 노랑·청록 = 앞손·뒷손, 빨강 = 칼끝, 점선 = 손·칼끝 경로
    font = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 20)
    x0c, y0c, cw, ch = 0, 8, K.W, 90
    s = SCALE
    sheet = Image.new('RGB', (cw * s * 4 + 50, ch * s + 90), (58, 60, 70))
    d = ImageDraw.Draw(sheet)
    path = [pose(i / 40) for i in range(41)]
    for n, (name, p, m) in enumerate(zip(NAMES, ps, masks)):
        ox = 10 + n * (cw * s + 10)
        q = lambda v: (ox + (v[0] + K.OX - x0c) * s, 40 + (v[1] + K.OY - y0c) * s)
        for y in range(K.H):
            for x in range(K.W):
                if m[y, x] and y >= y0c and y < y0c + ch:
                    d.rectangle([ox + (x - x0c) * s, 40 + (y - y0c) * s, ox + (x - x0c) * s + s - 1, 40 + (y - y0c) * s + s - 1], fill=(92, 95, 106))
        for key, col in [('hand', (240, 200, 90)), ('tip', (225, 90, 90))]:
            for a, b in zip(path[::2], path[2::2]):
                d.line([q(a[key]), q(b[key])], fill=tuple(c // 2 + 30 for c in col), width=1)
        d.line([q(p['guard']), q(p['tip'])], fill=(235, 235, 235), width=5 * s // 2)
        d.line([q(p['pommel']), q(p['guard'])], fill=(174, 120, 58), width=8)
        r = 7
        for key, col in [('rear', (90, 220, 220)), ('hand', (250, 215, 90)), ('tip', (235, 80, 80))]:
            cx, cy = q(p[key])
            d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=col, outline=(0, 0, 0))
        d.text((ox + 6, 6), f"{name}  칼 {p['ang']:.0f}°", font=font, fill=(255, 255, 255))
    sheet.save(os.path.join(OUT, 'connect_x5.png'))
    return int(rc)


if __name__ == '__main__':
    sys.exit(main())
