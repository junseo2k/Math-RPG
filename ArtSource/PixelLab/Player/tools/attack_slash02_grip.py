"""31번 마지막 자세 v3 → v4 (생성 없음, 2026-11-07 사용자):
  "얼굴 가려도 되니 팔을 더 펴라 / 칼 너무 밑부분을 잡고 있다 / 오른손잡이: 오른손이 손잡이 위쪽, 왼손이 아래쪽 /
   칼을 좀 더 왼쪽 아래로, 머리 근처까지"
  - 칼날(회색)만 v3에서 가져와 각도·길이 그대로 평행 이동, 코등이·손잡이·폼멜은 다시 그림
  - 오른손(가까운 팔) = 코등이 바로 아래, 왼손(먼 팔) = 그 아래, 손잡이 끝에 폼멜
  - 두 팔은 어깨 → 손까지 거의 곧게, 머리(얼굴) 앞을 지나도 됨
  - 어깨 아래(y≥61)는 그대로
  python attack_slash02_grip.py     -> key_c_finish_overhead_v4.png, preview/finish_v3_v4_x5.png·_x8.png
  python attack_slash02_grip.py v5  -> key_c_finish_overhead_v5.png(손 위아래 교체, 오른팔 5px·팔꿈치 1px 아래), preview/finish_v4d_v5_*
"""
import os
import sys
from PIL import Image
from attack_slash_east import is_blade, is_hair, px
from attack_slash02_fararm import PAL
from attack_slash02_plan import ends

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
S = 96
GUARD = (52, 47)                 # 코등이 중심: 머리(정수리 약 x45, y52) 바로 오른쪽 위
R_SHOULDER, R_ELBOW = (52, 61), (53, 55)   # 오른팔(가까운 팔): 거의 곧게
L_SHOULDER, L_ELBOW = (38, 60), (41, 56)   # 왼팔(먼 팔): 거의 곧게, 얼굴 왼쪽 앞을 지남
HEAD = (41, 51, 50, 61)
SKIN = {(0xfd, 0xe6, 0xbd), (0xd9, 0xc6, 0x9f)}


def line(a, b):
    n = max(abs(b[0] - a[0]), abs(b[1] - a[1]), 1)
    return [(round(a[0] + (b[0] - a[0]) * t / n), round(a[1] + (b[1] - a[1]) * t / n)) for t in range(n + 1)]


def arm(w, pts, outer, cross='02b'):
    """팔 단면(안쪽 → 바깥쪽): 기본 3px = 검정 0 / 갑옷 2 / 금색 b. outer = -1이면 바깥쪽이 화면 왼쪽."""
    for a, b in zip(pts, pts[1:]):
        for x, y in line(a, b):
            if y > 60:               # 어깨 아래 몸통은 건드리지 않음
                continue
            for i, c in enumerate(cross):
                w[x + outer * (i - 1), y] = PAL[c]


# v5(2026-11-07 사용자): 머리 왼쪽에 보이는 팔 = 캐릭터 오른팔(메모리 feedback-east-arm-sides).
# 오른팔이 손잡이 위쪽, 왼팔이 아래쪽을 쥐도록 두 팔의 손을 맞바꾸고, 오른팔은 5px, 팔꿈치를 1px 아래로.
V5 = sys.argv[1:] == ['v5']
SCREEN_LEFT_ELBOW_V5 = (41, 57)


def main():
    src = Image.open(os.path.join(D, 'key_c_finish_overhead_v3.png')).convert('RGBA')
    a = src.load()
    blade = {(x, y): a[x, y] for y in range(52) for x in range(S) if is_blade(a[x, y])}
    root, tip = ends(Image.open(os.path.join(D, 'key_c_finish_overhead_v3.png')).convert('RGBA'))
    L = ((tip[0] - root[0]) ** 2 + (tip[1] - root[1]) ** 2) ** 0.5
    dx, dy = (tip[0] - root[0]) / L, (tip[1] - root[1]) / L          # 칼날 방향(단위)
    at = lambda k: (round(GUARD[0] + k * dx), round(GUARD[1] + k * dy))
    shift = (at(1)[0] - root[0], at(1)[1] - root[1])                  # 칼 뿌리를 코등이 바로 위로
    R_HAND, L_HAND, POMMEL = at(-2.5), at(-5.5), at(-8)
    head = {(x, y): a[x, y] for y in range(HEAD[1], HEAD[3] + 1) for x in range(HEAD[0], HEAD[2] + 1)
            if a[x, y][3] and (is_hair(a[x, y]) or a[x, y][:3] in SKIN)}

    out = src.copy()
    w = out.load()
    for y in range(61):              # 머리 위·팔 자리 지우기(머리카락·얼굴은 아래에서 다시 칠함)
        for x in range(S):
            if y <= 51 or (x >= 51 and y <= 60) or (x <= 40 and 48 <= y <= 59):
                w[x, y] = (0, 0, 0, 0)
    for q, c in head.items():
        w[q] = c
    for (x, y), c in blade.items():  # 칼날 평행 이동
        w[x + shift[0], y + shift[1]] = c
    for x, y in line(POMMEL, at(-1)):                                # 손잡이(어두운 2), 폼멜 금색
        w[x, y] = PAL['2']
    w[POMMEL] = PAL['9']
    for t in range(-3, 4):                                           # 코등이: 칼날에 수직, 7px
        w[GUARD[0] + round(-t * dy), GUARD[1] + round(t * dx)] = PAL['9' if abs(t) == 3 else 'b']
    if V5:
        arm(w, [R_SHOULDER, R_ELBOW, L_HAND], +1)                    # 머리 오른쪽 팔(캐릭터 왼팔) → 아래쪽 손, 3px
        arm(w, [L_SHOULDER, SCREEN_LEFT_ELBOW_V5, R_HAND], -1, '0222b')  # 머리 왼쪽 팔(캐릭터 오른팔) → 위쪽 손, 5px
    else:
        arm(w, [L_SHOULDER, L_ELBOW, L_HAND], -1)                    # v4: 머리 왼쪽 팔 먼저
        arm(w, [R_SHOULDER, R_ELBOW, R_HAND], +1)
    for hx, hy in (L_HAND, R_HAND):                                  # 손: 3×3 건틀릿, 위 줄 금색 손마디(위쪽 손이 맨 앞)
        for y in range(hy - 1, hy + 2):
            for x in range(hx - 1, hx + 2):
                w[x, y] = PAL['9'] if y == hy - 1 else PAL['2']
    tag = 'v5' if V5 else 'v4'
    out.save(os.path.join(D, f'key_c_finish_overhead_{tag}.png'))
    if V5:
        src = Image.open(os.path.join(D, 'key_c_finish_overhead_v4d.png')).convert('RGBA')   # 비교 기준 = v4d

    below = sum(a[x, y] != w[x, y] for y in range(61, S) for x in range(S))
    d = px(out)
    b = out.getbbox()
    print('칼 이동', shift, '| 코등이', GUARD, '오른손', R_HAND, '왼손', L_HAND, '폼멜', POMMEL)
    print('어깨 아래(y≥61) 바뀐 픽셀', below, '| 칼날 회색', len(blade), '| 색 수', len({c[:3] for c in d if c[3]}),
          '| 알파', sorted({c[3] for c in d}), '| 여백 좌상우하', (b[0], b[1], S - b[2], S - b[3]))
    bg = (58, 62, 72, 255)
    pair = 'v4d_v5' if V5 else 'v3_v4'
    for name, crop, z, n in ((f'finish_{pair}_x5.png', (18, 2, 78, 94), 5, 60), (f'finish_{pair}_x8.png', (28, 26, 70, 70), 8, 42)):
        W = Image.new('RGBA', (n * 2, crop[3] - crop[1]), bg)
        for i, im in enumerate((src, out)):
            W.alpha_composite(im.crop(crop), (n * i, 0))
        W.resize((W.width * z, W.height * z), Image.NEAREST).save(os.path.join(D, 'preview', name))


if __name__ == '__main__':
    main()
