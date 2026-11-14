"""32번 3타 v7 → v9: 2타 끝 → 3타 시작 칼 연결(생성 없음, 2026-11-08 사용자).
문제: 2타 f8 = 3타 f1·f2·f3 위쪽(칼 120°)이 같아 멈추고, f3 → f4(타격)에서 칼이 머리 뒤 → 몸 앞 아래로 순간이동.
수정: 3타 f1 = 2타 f8 그대로(연결 기준, 이어 보기에서는 합쳐져 빠짐). f2·f3은 왼손(48,53)을 그대로 두고 칼·오른손을
손잡이 축을 따라 돌려 120° → 90°(f2) → 50°(f3) → 타격(f4). 칼 길이 30·폭 6 그대로, 오른팔 앞팔은 팔꿈치 (41,55)에서
각 오른손까지 다시 그림. 다리: f2 = f1, f3 = 지금 f3(무릎 굽힘). 몸 앞뒤 가림: 칼은 빈 칸에만, 손은 맨 앞.
  python attack_slash03_start_v9.py -> 0_east_v9.png, preview/link_v7_v9_x4.png, 이어 보기 GIF
"""
import math
import os
from PIL import Image
from attack_slash_east import px
from attack_slash02_fararm import PAL
from attack_slash02_grip import line
from attack_chain_hands120 import head_layer, LOWER, ELBOW, CROSS

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D2 = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
D3 = os.path.join(ROOT, '32_attack_greatsword_heavy_slash_03_east')
S, LENGTH = 96, 30
CUR_UPPER, CUR_POMMEL, CUR_GUARD = (46, 50), (49, 55), (45, 48)   # 지금(120°) 오른손·폼멜·코등이
ANGLES = {2: 90, 3: 50}                                            # 3타 프레임(1부터) → 칼 각도
LEG_CUT = 70


def axis(ang):
    ux, uy = math.cos(math.radians(ang)), -math.sin(math.radians(ang))
    vx, vy = -ux, -uy
    nx, ny = -uy, ux
    if nx - ny < 0:
        nx, ny = -nx, -ny
    upper = (round(LOWER[0] - 3 * vx), round(LOWER[1] - 3 * vy))
    guard = (upper[0] - 2.5 * vx, upper[1] - 2.5 * vy)
    pommel = (round(LOWER[0] + 2.5 * vx), round(LOWER[1] + 2.5 * vy))
    return (ux, uy), (nx, ny), upper, guard, pommel


def pose(base, ang, head):
    """base(2타 f8 = 3타 f1)에서 칼·오른손·오른 앞팔만 ang으로 다시 그린 그림."""
    f = base.copy()
    p = f.load()
    clear = {(x, y) for y in range(51) for x in range(S)}                 # 머리 위 칼·코등이
    clear |= {(x, y) for y in range(CUR_UPPER[1] - 1, CUR_UPPER[1] + 2) for x in range(CUR_UPPER[0] - 1, CUR_UPPER[0] + 2)}
    for x, y in line(ELBOW, CUR_UPPER):                                   # 지금 오른 앞팔
        for dx in range(-2, 3):
            clear.add((x + dx, y))
    for q in line(CUR_POMMEL, CUR_GUARD):                                 # 지금 손잡이
        clear.add(q)
    keep_lower = {(x, y) for y in range(LOWER[1] - 1, LOWER[1] + 2) for x in range(LOWER[0] - 1, LOWER[0] + 2)}
    for q in clear - keep_lower:
        if q[1] < 56:
            p[q] = head.get(q, (0, 0, 0, 0))
    (ux, uy), (nx, ny), upper, guard, pommel = axis(ang)
    for x, y in line(ELBOW, upper):                                       # 오른 앞팔 '022b', 바깥(화면 왼쪽) 금색
        for i, c in enumerate('022b'):
            p[x - (i - 1), y] = PAL[c]
    for x, y in line(pommel, (round(guard[0]), round(guard[1]))):        # 손잡이
        p[x, y] = PAL['2']
    p[pommel] = PAL['9']
    for i in range(2, LENGTH * 2 + 1):                                    # 칼날(빈 칸에만 → 몸·머리 뒤)
        t = i / 2
        half = 3 if t < LENGTH - 3 else max(1, round(3 * (LENGTH - t) / 3))
        for w, c in CROSS.items():
            if -half <= w < half:
                q = (round(guard[0] + ux * t + nx * w), round(guard[1] + uy * t + ny * w))
                if 0 <= q[0] < S and 0 <= q[1] < S and not p[q][3]:
                    p[q] = PAL[c]
    for t in range(-3, 4):                                                # 코등이
        p[round(guard[0] + nx * t), round(guard[1] + ny * t)] = PAL['9' if abs(t) == 3 else 'b']
    for hx, hy in (LOWER, upper):                                         # 손(오른손이 맨 앞)
        for y in range(hy - 1, hy + 2):
            for x in range(hx - 1, hx + 2):
                p[x, y] = PAL['9'] if y == hy - 1 else PAL['2']
    return f, upper, guard, (round(guard[0] + ux * LENGTH), round(guard[1] + uy * LENGTH))


def main():
    head = head_layer()
    v7 = Image.open(os.path.join(D3, '0_east_v7.png')).convert('RGBA')
    s2 = Image.open(os.path.join(D2, '0_east_v2i.png')).convert('RGBA')
    f8 = s2.crop((S * 7, 0, S * 8, S))
    frames = [v7.crop((S * k, 0, S * k + S, S)) for k in range(12)]
    new = list(frames)
    for k, ang in ANGLES.items():
        f, upper, guard, tip = pose(f8, ang, head)
        if k == 3:                                                        # 다리는 지금 f3(무릎 굽힘)
            f.paste(frames[2].crop((0, LEG_CUT, S, S)), (0, LEG_CUT))
        new[k - 1] = f
        b = f.getbbox()
        print(f'3타 f{k}: 칼 {ang}°, 오른손 {upper}, 코등이 {tuple(round(v) for v in guard)}, 칼끝 {tip}, 여백 {(b[0], b[1], S - b[2], S - b[3])}')
    out = Image.new('RGBA', (S * 12, S), (0, 0, 0, 0))
    for k, f in enumerate(new):
        out.paste(f, (S * k, 0))
    out.save(os.path.join(D3, '0_east_v9.png'))
    d = px(out)
    print('2타 f8 == 3타 f1:', f8.tobytes() == new[0].tobytes(), '| 색 수', len({c[:3] for c in d if c[3]}), '| 알파', sorted({c[3] for c in d}))
    diff = lambda a, b: sum(x != y for x, y in zip(px(a), px(b)))
    print('이웃 차이 전(2타f8→f2→f3→f4):', [diff(f8, frames[1]), diff(frames[1], frames[2]), diff(frames[2], frames[3])],
          '| 후:', [diff(f8, new[1]), diff(new[1], new[2]), diff(new[2], new[3])])
    bg = (58, 62, 72, 255)
    W = Image.new('RGBA', (S * 4, S * 2), bg)
    for r, row in enumerate(([f8] + frames[1:4], [f8] + new[1:4])):
        for i, f in enumerate(row):
            W.alpha_composite(f, (S * i, S * r))
    W = W.resize((W.width * 3, W.height * 3), Image.NEAREST)
    W.save(os.path.join(D3, 'preview', 'link_v7_v9_x3.png'))
    first = Image.open(os.path.join(ROOT, '29_attack_greatsword_light_slash_east', '0_east_pauldron_w.png')).convert('RGBA')
    chain = [first.crop((S * k, 0, S * k + S, S)) for k in range(8)] + [s2.crop((S * k, 0, S * k + S, S)) for k in range(1, 8)] + new[1:]
    g = []
    for f in chain:
        c = Image.new('RGBA', (S, S), bg)
        c.alpha_composite(f)
        g.append(c.resize((S * 4, S * 4), Image.NEAREST).convert('RGB'))
    g[0].save(os.path.join(D3, 'preview', 'slash01_02i_03v9_chain_16fps_x4.gif'), save_all=True, append_images=g[1:],
              duration=([60, 70] * 40)[:len(g) - 1] + [600], loop=0, disposal=1)
    print('이어 보기 프레임', len(g))


if __name__ == '__main__':
    main()
