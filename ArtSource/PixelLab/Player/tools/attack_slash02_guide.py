"""31번 높은 마무리 밑그림(생성 없음): f4 타격 포즈에서 머리 오른쪽의 팔·손·칼만 지우고,
손이 캐릭터 오른쪽 위에 남는 자세를 단순 도형으로 그린다(2026-11-07 사용자: 손은 오른쪽 위, 왼쪽으로 넘어가지 않음).
  python attack_slash02_guide.py -> 31_.../guide_finish_right.png, preview/guide_finish_right_x5.png
"""
import os
from PIL import Image

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
PAL = {k: tuple(int(l.strip().lstrip('#')[i:i + 2], 16) for i in (0, 2, 4)) + (255,)
       for k, l in zip('0123456789abcdef', [l for l in open(os.path.join(ROOT, '02_final', 'palette.hex')) if l.strip()])}
SHOULDER, ELBOW, HANDS = (47, 58), (58, 50), (56, 41)   # 어깨 → 팔꿈치 → 손(오른쪽 위)
TIP = (52, 6)                                          # 칼끝: 손에서 거의 수직, 조금 뒤로
CLEAR_X, CLEAR_Y = 45, 60                              # f4에서 이 범위(x≥45, y<60)의 팔·손·칼을 지움


def line(a, b):
    n = max(abs(b[0] - a[0]), abs(b[1] - a[1]))
    return [(round(a[0] + (b[0] - a[0]) * t / n), round(a[1] + (b[1] - a[1]) * t / n)) for t in range(n + 1)]


def main():
    im = Image.open(os.path.join(D, 'key_b_strike.png')).convert('RGBA')
    p = im.load()
    for y in range(CLEAR_Y):
        for x in range(CLEAR_X, 96):
            p[x, y] = (0, 0, 0, 0)
    # 칼날: 폭 5(어두운 날 7, 중간 a 2줄, 밝은 c 2줄), 손 위에서 칼끝까지
    for i, (x, y) in enumerate(line((HANDS[0] - 1, HANDS[1] - 6), TIP)):
        for dx, c in zip(range(-2, 3), '7aacc'):
            p[x + dx, y] = PAL[c]
    # 코등이(가로 7px 금색)와 손잡이
    for dx in range(-3, 4):
        p[HANDS[0] - 1 + dx, HANDS[1] - 5] = PAL['9' if abs(dx) == 3 else 'b']
    for y in range(HANDS[1] - 4, HANDS[1] + 3):
        p[HANDS[0] - 1, y] = PAL['2']
    # 두 팔: 어두운 갑옷 띠(폭 4), 가장자리 금색 한 줄
    for a, b in ((SHOULDER, ELBOW), (ELBOW, HANDS)):
        for x, y in line(a, b):
            for dx in range(-2, 2):
                p[x + dx, y] = PAL['0']
            p[x + 2, y] = PAL['b']
    # 손(건틀릿 4×4)
    for y in range(HANDS[1] - 2, HANDS[1] + 2):
        for x in range(HANDS[0] - 3, HANDS[0] + 1):
            p[x, y] = PAL['2'] if (x + y) % 3 else PAL['9']
    im.save(os.path.join(D, 'guide_finish_right.png'))

    bg = (58, 62, 72, 255)
    names = ['key_b_strike.png', 'key_c_finish.png', 'guide_finish_right.png']
    W = Image.new('RGBA', (60 * len(names), 92), bg)
    for i, n in enumerate(names):
        W.alpha_composite(Image.open(os.path.join(D, n)).convert('RGBA').crop((20, 2, 80, 94)), (60 * i, 0))
    W = W.resize((W.width * 5, W.height * 5), Image.NEAREST)
    q = W.load()
    for i in range(len(names)):
        for t in range(W.height):
            q[60 * 5 * i + (47 - 20) * 5 + 2, t] = (230, 80, 80, 255)   # 몸 중앙 x47(빨강)
    W.save(os.path.join(D, 'preview', 'guide_finish_right_x5.png'))
    b = im.getbbox()
    print('밑그림 bbox', b, '여백 좌상우하', (b[0], b[1], 96 - b[2], 96 - b[3]), '손', HANDS, '칼끝', TIP)


if __name__ == '__main__':
    main()
