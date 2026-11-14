"""31번 마지막 자세 v2c → v3 (생성 없음, 2026-11-07 사용자: "칼이 너무 위라 팔이 길어 보이고 처형인 같다,
왼팔을 좀 펴고 칼을 머리 쪽으로 낮춰라").
  - 칼·코등이·손(머리 위 y≤50)을 K px 아래로 평행 이동(각도·길이 그대로)
  - 가까운 팔(머리 오른쪽 x≥51, y51~60)도 K px 내리고 어깨 아래로 넘치는 줄은 버려 짧게
  - 먼 왼팔(머리 왼쪽)은 지우고 어깨 → 손잡이 아래 손까지 거의 곧게 다시 그림, 가운데는 머리 뒤로 가림
  - 어깨 아래(y≥61)와 머리는 그대로
  python attack_slash02_lower.py -> key_c_finish_overhead_v3.png, preview/finish_v2c_v3_x5.png·_x8.png
"""
import os
from PIL import Image
from attack_slash_east import is_hair, px
from attack_slash02_fararm import in_band, PAL

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
S = 96
K = 4                                   # 내리는 양(px)
HEAD = (41, 51, 50, 61)                 # 머리 상자 x0,y0,x1,y1(포함): 머리카락·얼굴을 맨 위에 다시 칠함
FAR_ELBOW_BOX = (33, 50, 40, 58)        # 지울 먼 팔꿈치
SHOULDER, ELBOW = (38, 60), (39, 51)    # 먼 팔: 어깨 → 팔꿈치(머리 왼쪽 위 가장자리, v2c의 (36,54)보다 안쪽 = 덜 벌림·덜 굽힘) → 손
# 1차(팔꿈치 (42,55))는 팔이 거의 다 머리 뒤에 가려져 머리 위 금색 선만 남았음(2026-11-07)
HAND = (51, 46 + K)                     # 먼 손(손잡이 아래쪽, 내린 위치)
SKIN = {(0xfd, 0xe6, 0xbd), (0xd9, 0xc6, 0x9f)}


def line(a, b):
    n = max(abs(b[0] - a[0]), abs(b[1] - a[1]), 1)
    return [(round(a[0] + (b[0] - a[0]) * t / n), round(a[1] + (b[1] - a[1]) * t / n)) for t in range(n + 1)]


def main():
    src = Image.open(os.path.join(D, 'key_c_finish_overhead_v2c.png')).convert('RGBA')
    a = src.load()
    head = {(x, y): a[x, y] for y in range(HEAD[1], HEAD[3] + 1) for x in range(HEAD[0], HEAD[2] + 1)
            if a[x, y][3] and (is_hair(a[x, y]) or a[x, y][:3] in SKIN)}
    work = src.copy()
    w = work.load()
    for y in range(S):                  # 1) 먼 팔 지우기(앞팔 띠 + 팔꿈치 상자)
        for x in range(S):
            if (x <= 51 and in_band(x, y) and (x, y) not in head) or \
               (FAR_ELBOW_BOX[0] <= x <= FAR_ELBOW_BOX[2] and FAR_ELBOW_BOX[1] <= y <= FAR_ELBOW_BOX[3]):
                w[x, y] = (0, 0, 0, 0)
    upper = {(x, y): w[x, y] for y in range(51) for x in range(S) if w[x, y][3]}
    near = {(x, y): w[x, y] for y in range(51, 61) for x in range(51, S) if w[x, y][3]}
    for (x, y) in list(upper) + list(near):
        w[x, y] = (0, 0, 0, 0)
    # 2) 먼 팔을 곧게: 어깨 → 팔꿈치 → 손, 단면 위 금색 b / 갑옷 2 / 아래 검정 0 (폭 3)
    for x, y in line(SHOULDER, ELBOW):          # 위팔: 머리 왼쪽 옆으로 세로, 바깥(왼쪽) 금색
        w[x - 1, y] = PAL['b']
        w[x, y] = PAL['2']
        w[x + 1, y] = PAL['0']
    for x, y in line(ELBOW, HAND):              # 앞팔: 정수리 바로 위를 지나 손으로, 위 금색
        w[x, y - 2] = PAL['b']
        w[x, y - 1] = PAL['2']
        w[x, y] = PAL['0']
    # 3) 가까운 팔(넘치는 줄은 버림), 칼·손을 K px 아래로
    for (x, y), c in near.items():
        if y + K <= 60:
            w[x, y + K] = c
    for (x, y), c in upper.items():
        w[x, y + K] = c
    # 4) 머리는 맨 앞(먼 팔이 머리 뒤로 가려짐). 칼·손이 내려와 겹친 곳은 손이 앞
    for (x, y), c in head.items():
        if (x, y - K) not in upper:
            w[x, y] = c
    work.save(os.path.join(D, 'key_c_finish_overhead_v3.png'))

    below = sum(a[x, y] != w[x, y] for y in range(61, S) for x in range(S))
    d = px(work)
    b = work.getbbox()
    print('어깨 아래(y≥61) 바뀐 픽셀', below, '| 색 수', len({c[:3] for c in d if c[3]}), '| 알파', sorted({c[3] for c in d}),
          '| 여백 좌상우하', (b[0], b[1], S - b[2], S - b[3]))
    bg = (58, 62, 72, 255)
    for name, crop, z, n in (('finish_v2c_v3_x5.png', (18, 2, 78, 94), 5, 60), ('finish_v2c_v3_x8.png', (28, 30, 70, 70), 8, 42)):
        W = Image.new('RGBA', (n * 2, crop[3] - crop[1]), bg)
        for i, im in enumerate((src, work)):
            W.alpha_composite(im.crop(crop), (n * i, 0))
        W.resize((W.width * z, W.height * z), Image.NEAREST).save(os.path.join(D, 'preview', name))


if __name__ == '__main__':
    main()
