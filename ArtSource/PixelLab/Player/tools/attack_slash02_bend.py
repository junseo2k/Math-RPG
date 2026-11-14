"""31번 마지막 자세 v7 (생성 없음, 2026-11-07 사용자: "오른팔이 너무 딱딱하다, 이두 쪽과 팔꿈치 각도를 넓혀라").
대상 = 머리 왼쪽에 보이는 팔(캐릭터 오른팔). v5(너무 꺾임)와 v6(일직선, 딱딱함) 사이:
어깨 (36,63) → 팔꿈치를 직선에서 바깥(왼쪽 위)으로 약 2.5px 내민 (41,54) → 위쪽 손 (50,49). 팔꿈치 각도 약 150°.
위팔(이두) 6px, 앞팔 5px. v5의 이 팔을 지우고 머리는 v3 머리 그림으로 되살린 뒤 다시 그린다. 손·칼·반대 팔은 그대로.
  python attack_slash02_bend.py      -> key_c_finish_overhead_v7.png, preview/finish_v6_v7_x5.png·_x8.png
  python attack_slash02_bend.py low  -> v7b(어깨·팔꿈치 1px 아래), preview/finish_v7_v7b_*
  python attack_slash02_bend.py thin -> v7c(v7b 위치, 위팔 5px·앞팔 4px), preview/finish_v7b_v7c_*
"""
import math
import os
import sys
from PIL import Image
from attack_slash_east import is_hair, px
from attack_slash02_fararm import PAL
from attack_slash02_grip import L_SHOULDER, HEAD, SKIN, line

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
S = 96
SHOULDER, ELBOW, HAND = (36, 63), (41, 54), (50, 49)
# 'low' = v7b(2026-11-07 사용자 "위로만 두꺼워 보인다, 1px만 낮춰"): 손은 그대로, 어깨·팔꿈치만 1px 아래
LOW = sys.argv[1:] in (['low'], ['thin'])
THIN = sys.argv[1:] == ['thin']   # v7c(2026-11-07 사용자 "굵기 1px만 낮게"): v7b 위치 그대로, 위팔 6→5px·앞팔 5→4px
TAG = 'v7c' if THIN else ('v7b' if LOW else 'v7')
if LOW:
    SHOULDER, ELBOW = (36, 64), (41, 55)
UPPER, FORE = '02222b', '0222b'          # 단면 안쪽 → 바깥쪽(화면 왼쪽)
if sys.argv[1:] == ['thin']:
    UPPER, FORE = '0222b', '022b'
V5_ELBOW = (41, 57)                      # v5 이 팔의 팔꿈치(지울 띠 계산용)
HANDS = [(49, 48, 51, 50), (47, 50, 49, 52)]   # 위쪽 손, 아래쪽 손(그대로 둠)
STUB = (36, 61, 42, 64)


def band(pts, cross, ymax=S):
    out = {}
    for a, b in zip(pts, pts[1:]):
        for x, y in line(a, b):
            if y > ymax:
                continue
            for i, c in enumerate(cross):
                out[(x - (i - 1), y)] = c
    return out


def in_hands(q):
    return any(x0 <= q[0] <= x1 and y0 <= q[1] <= y1 for x0, y0, x1, y1 in HANDS)


def main():
    v5 = Image.open(os.path.join(D, 'key_c_finish_overhead_v5.png')).convert('RGBA')
    v3 = Image.open(os.path.join(D, 'key_c_finish_overhead_v3.png')).convert('RGBA').load()
    v6 = Image.open(os.path.join(D, 'key_c_finish_overhead_v6.png')).convert('RGBA')
    head = {(x, y): v3[x, y] for y in range(HEAD[1], HEAD[3] + 1) for x in range(HEAD[0], HEAD[2] + 1)
            if v3[x, y][3] and (is_hair(v3[x, y]) or v3[x, y][:3] in SKIN)}
    out = v5.copy()
    w = out.load()
    for q in band([L_SHOULDER, V5_ELBOW, HAND], '0222b', ymax=60):      # v5의 이 팔 지우기
        if not in_hands(q):
            w[q] = head.get(q, (0, 0, 0, 0))
    a5 = v5.load()
    new = band([SHOULDER, ELBOW], UPPER)
    new.update(band([ELBOW, HAND], FORE))
    for y in range(STUB[1], STUB[3] + 1):                              # 몸통 세로 위팔 조각의 띠 밖 금색은 어둡게(v6와 같음)
        for x in range(STUB[0], STUB[2] + 1):
            if (x, y) not in new and a5[x, y][3] and a5[x, y][:3] in (PAL['b'][:3], PAL['e'][:3]):
                w[x, y] = PAL['0']
    for q, c in new.items():
        if not in_hands(q):
            w[q] = PAL[c]
    out.save(os.path.join(D, f'key_c_finish_overhead_{TAG}.png'))

    va, vb = (ELBOW[0] - SHOULDER[0], ELBOW[1] - SHOULDER[1]), (HAND[0] - ELBOW[0], HAND[1] - ELBOW[1])
    ang = 180 - math.degrees(math.acos((va[0] * vb[0] + va[1] * vb[1]) / (math.hypot(*va) * math.hypot(*vb))))
    ch = [(x, y) for y in range(S) for x in range(S) if a5[x, y] != w[x, y]]
    d = px(out)
    b = out.getbbox()
    print(f'팔꿈치 각도 {ang:.0f}°(180 = 일직선) | v5 대비 바뀐 픽셀 {len(ch)} | 몸통 줄(y≥61) {sum(1 for _, y in ch if y >= 61)}',
          '| x 범위', (min(x for x, _ in ch), max(x for x, _ in ch)), '| 색 수', len({c[:3] for c in d if c[3]}),
          '| 알파', sorted({c[3] for c in d}), '| 여백', (b[0], b[1], S - b[2], S - b[3]))
    bg = (58, 62, 72, 255)
    pair = 'v7b_v7c' if THIN else ('v7_v7b' if LOW else 'v6_v7')
    if LOW:
        base = 'v7b' if THIN else 'v7'
        v6 = Image.open(os.path.join(D, f'key_c_finish_overhead_{base}.png')).convert('RGBA')   # 비교 기준
    for name, crop, z, n in ((f'finish_{pair}_x5.png', (18, 2, 78, 94), 5, 60), (f'finish_{pair}_x8.png', (26, 26, 68, 70), 8, 42)):
        W = Image.new('RGBA', (n * 2, crop[3] - crop[1]), bg)
        for i, im in enumerate((v6, out)):
            W.alpha_composite(im.crop(crop), (n * i, 0))
        W.resize((W.width * z, W.height * z), Image.NEAREST).save(os.path.join(D, 'preview', name))


if __name__ == '__main__':
    main()
