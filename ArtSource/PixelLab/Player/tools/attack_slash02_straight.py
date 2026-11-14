"""31번 마지막 자세 v5 → v6 (생성 없음, 2026-11-07 사용자: "오른팔이 너무 꺾여 있다, 얼굴 가려도 되니 팔꿈치 각도를 크게").
대상 = 머리 왼쪽에 보이는 팔(캐릭터 오른팔, 사용자 확인). 꺾여 보이던 곳은 몸통의 세로 위팔 조각(x37~41, y61~62)과
그 위에서 비스듬히 출발하는 팔이 만나는 어깨 지점이었다. 손 (50,49)에서 어깨 쪽 직선을 몸통 안 y63까지 이어 곧게 편다.
이 팔이 지나는 몸통 줄(y61~63, x≤42)만 예외로 고치고, 그 밖의 몸통은 그대로.
  python attack_slash02_straight.py -> key_c_finish_overhead_v6.png, preview/finish_v5_v6_x5.png·_x8.png
"""
import os
from PIL import Image
from attack_slash_east import px
from attack_slash02_fararm import PAL
from attack_slash02_grip import line

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
S = 96
HAND = (50, 49)                  # 위쪽(오른)손
SHOULDER = (36, 63)              # 손 → v5 어깨 (38,60) 방향을 몸통 안으로 연장한 점
CROSS = '0222b'                  # 안쪽 → 바깥쪽(화면 왼쪽), 5px
STUB = (36, 61, 42, 63)          # 몸통의 세로 위팔 조각: 새 팔 띠 밖은 몸통 옆 어두운 갑옷(0)으로 덮음
HAND_BOX = (49, 48, 51, 50)      # 손은 그대로


def main():
    src = Image.open(os.path.join(D, 'key_c_finish_overhead_v5.png')).convert('RGBA')
    out = src.copy()
    w = out.load()
    band = set()
    for x, y in line(SHOULDER, HAND):
        for i, c in enumerate(CROSS):
            q = (x - (i - 1), y)
            if HAND_BOX[0] <= q[0] <= HAND_BOX[2] and HAND_BOX[1] <= q[1] <= HAND_BOX[3]:
                continue
            w[q] = PAL[c]
            band.add(q)
    a = src.load()
    for y in range(STUB[1], STUB[3] + 1):     # 세로 조각의 띠 밖 금색 테두리만 어둡게(꺾임 선이 남지 않게)
        for x in range(STUB[0], STUB[2] + 1):
            if (x, y) not in band and a[x, y][3] and a[x, y][:3] in (PAL['b'][:3], PAL['e'][:3]):
                w[x, y] = PAL['0']
    out.save(os.path.join(D, 'key_c_finish_overhead_v6.png'))
    ch = [(x, y) for y in range(S) for x in range(S) if a[x, y] != w[x, y]]
    d = px(out)
    b = out.getbbox()
    print('바뀐 픽셀', len(ch), '| 몸통 줄(y≥61) 변경', sum(1 for _, y in ch if y >= 61),
          '| x 범위', (min(x for x, _ in ch), max(x for x, _ in ch)), '| 색 수', len({c[:3] for c in d if c[3]}),
          '| 알파', sorted({c[3] for c in d}), '| 여백', (b[0], b[1], S - b[2], S - b[3]))
    bg = (58, 62, 72, 255)
    for name, crop, z, n in (('finish_v5_v6_x5.png', (18, 2, 78, 94), 5, 60), ('finish_v5_v6_x8.png', (28, 26, 70, 70), 8, 42)):
        W = Image.new('RGBA', (n * 2, crop[3] - crop[1]), bg)
        for i, im in enumerate((src, out)):
            W.alpha_composite(im.crop(crop), (n * i, 0))
        W.resize((W.width * z, W.height * z), Image.NEAREST).save(os.path.join(D, 'preview', name))


if __name__ == '__main__':
    main()
