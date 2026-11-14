"""31번 마지막 자세 v4 → v4b: 오른팔(가까운 팔)만 굵게(생성 없음, 2026-11-07 사용자: "위치는 적당, 오른팔이 너무 얇다").
v4의 오른팔 3px(검정 0 / 갑옷 2 / 금색 b)를 4px(0 / 2 / 2 / b)로. 손·손잡이·코등이(y≤50)와 어깨 아래(y≥61)는 그대로.
  python attack_slash02_rarm.py       -> v4b(4px)
  python attack_slash02_rarm.py wide  -> v4c(5px)
  python attack_slash02_rarm.py left  -> v4d(화면 왼쪽 팔 = 캐릭터 오른팔 5px, 반대 팔은 v4 그대로)
"""
import os
import sys
from PIL import Image
from attack_slash_east import px
from attack_slash02_fararm import PAL
from attack_slash02_grip import R_SHOULDER, R_ELBOW, L_SHOULDER, L_ELBOW, line

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
S = 96
R_HAND = (50, 49)        # v4 오른손(attack_slash02_grip.py 출력값)
Y0, Y1 = 51, 60          # 고치는 줄 범위
# 단면(안쪽 → 바깥). 기본 4px = v4b, 'wide' 5px = v4c(2026-11-07 사용자 "1px 더 굵게")
WIDE = sys.argv[1:] in (['wide'], ['left'])
CROSS = '0222b' if WIDE else '022b'
TAG = 'v4c' if WIDE else 'v4b'
# 'left' = v4d(2026-11-07 사용자 정정): 화면에서 머리 오른쪽에 보이는 팔이 캐릭터 왼팔이었음.
# 그 팔은 v4 그대로 두고, 머리 왼쪽에 보이는 팔(캐릭터 오른팔)을 5px로. 바깥쪽이 화면 왼쪽이라 단면을 뒤집어 그림.
LEFT_SIDE = sys.argv[1:] == ['left']
L_HAND = (48, 51)        # v4 화면 왼쪽 팔 끝 손
if LEFT_SIDE:
    TAG = 'v4d'
    Y0 = 53              # 손(47~49, 50~52) 위로는 그리지 않음


def main():
    src = Image.open(os.path.join(D, 'key_c_finish_overhead_v4.png')).convert('RGBA')
    out = src.copy()
    w = out.load()
    segs = ((L_SHOULDER, L_ELBOW), (L_ELBOW, L_HAND)) if LEFT_SIDE else ((R_SHOULDER, R_ELBOW), (R_ELBOW, R_HAND))
    step = -1 if LEFT_SIDE else 1   # 안쪽(검정) → 바깥쪽(금색) 방향
    for a, b in segs:
        for x, y in line(a, b):
            if not (Y0 <= y <= Y1):
                continue
            for i, c in enumerate(CROSS):
                w[x + step * (i - 1), y] = PAL[c]
    out.save(os.path.join(D, f'key_c_finish_overhead_{TAG}.png'))
    a = src.load()
    ch = [(x, y) for y in range(S) for x in range(S) if a[x, y] != w[x, y]]
    d = px(out)
    b = out.getbbox()
    print('바뀐 픽셀', len(ch), '| y 범위', (min(y for _, y in ch), max(y for _, y in ch)) if ch else None,
          '| 색 수', len({c[:3] for c in d if c[3]}), '| 알파', sorted({c[3] for c in d}), '| 여백', (b[0], b[1], S - b[2], S - b[3]))
    bg = (58, 62, 72, 255)
    for name, crop, z, n in ((f'finish_v4_{TAG}_x5.png', (18, 2, 78, 94), 5, 60), (f'finish_v4_{TAG}_x8.png', (28, 26, 70, 70), 8, 42)):
        W = Image.new('RGBA', (n * 2, crop[3] - crop[1]), bg)
        for i, im in enumerate((src, out)):
            W.alpha_composite(im.crop(crop), (n * i, 0))
        W.resize((W.width * z, W.height * z), Image.NEAREST).save(os.path.join(D, 'preview', name))


if __name__ == '__main__':
    main()
