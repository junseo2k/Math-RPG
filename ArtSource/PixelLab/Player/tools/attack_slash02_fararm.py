"""31번 마지막 자세 v2의 먼 팔(머리 왼쪽 앞팔)만 다시 그리기 위한 마스크·합성·검사(2026-11-07 사용자 결정).
  python attack_slash02_fararm.py mask     -> fararm_mask.png (흰색 = 다시 그림, 검정 = 유지)
  python attack_slash02_fararm.py compose  -> key_c_finish_overhead_v2b.png + 전후 비교, 마스크 밖 변경 검사
"""
import os
import sys
from PIL import Image
from attack_slash_east import snap, is_hair, px

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
SRC = os.path.join(D, 'key_c_finish_overhead_v2.png')
S = 96
ELBOW, HAND = (36, 54), (50, 46)   # 먼 팔꿈치(머리 왼쪽) → 먼 손(손잡이 아래쪽)
R = 2.6                            # 띠 반폭(px)
MAX_X = 51                         # x≥52는 손잡이·가까운 손·칼이라 유지
# 앞팔 단면 범위(선에서 수직 거리). 아래는 정수리 머리카락이라 위쪽으로 굵힘(2026-11-07 1차 -1.9~1.6은 34px만 바뀌어 가늘었음)
D0, D1 = -3.6, 1.2


def in_band(x, y):
    ax, ay = ELBOW
    bx, by = HAND
    vx, vy = bx - ax, by - ay
    t = max(0, min(1, ((x - ax) * vx + (y - ay) * vy) / (vx * vx + vy * vy)))
    return ((x - ax - t * vx) ** 2 + (y - ay - t * vy) ** 2) ** 0.5 <= R


def mask():
    p = Image.open(SRC).convert('RGBA').load()
    m = Image.new('RGB', (S, S), (0, 0, 0))
    q = m.load()
    n = 0
    for y in range(S):
        for x in range(S):
            if x <= MAX_X and in_band(x, y) and not is_hair(p[x, y]):
                q[x, y] = (255, 255, 255)
                n += 1
    m.save(os.path.join(D, 'fararm_mask.png'))
    print('마스크 흰 픽셀', n)


def compose():
    src = Image.open(SRC).convert('RGBA')
    gen = snap(Image.open(os.path.join(D, 'keys_raw', 'c_finish_overhead_v2b.png')).convert('RGBA'))
    m = Image.open(os.path.join(D, 'fararm_mask.png')).convert('RGB').load()
    out = src.copy()
    o, g, a = out.load(), gen.load(), src.load()
    for y in range(S):
        for x in range(S):
            if m[x, y] == (255, 255, 255):
                o[x, y] = g[x, y]
    out.save(os.path.join(D, 'key_c_finish_overhead_v2b.png'))
    outside_gen = sum(g[x, y] != a[x, y] for y in range(S) for x in range(S) if m[x, y] != (255, 255, 255))
    changed = sum(o[x, y] != a[x, y] for y in range(S) for x in range(S))
    d = px(out)
    b = out.getbbox()
    print('생성본의 마스크 밖 차이(팔레트 맞춤 후)', outside_gen, '| 납품본 바뀐 픽셀', changed, '(모두 마스크 안)',
          '| 색 수', len({c[:3] for c in d if c[3]}), '| 알파', sorted({c[3] for c in d}), '| 여백', (b[0], b[1], S - b[2], S - b[3]))
    bg = (58, 62, 72, 255)
    W = Image.new('RGBA', (40 * 2, 36), bg)
    for i, im in enumerate((src, out)):
        W.alpha_composite(im.crop((30, 34, 70, 70)), (40 * i, 0))
    W.resize((W.width * 8, W.height * 8), Image.NEAREST).save(os.path.join(D, 'preview', 'fararm_v2_v2b_x8.png'))
    W = Image.new('RGBA', (60 * 2, 92), bg)
    for i, im in enumerate((src, out)):
        W.alpha_composite(im.crop((18, 2, 78, 94)), (60 * i, 0))
    W.resize((W.width * 5, W.height * 5), Image.NEAREST).save(os.path.join(D, 'preview', 'fararm_v2_v2b_x5.png'))


PAL = {k: tuple(int(l.strip().lstrip('#')[i:i + 2], 16) for i in (0, 2, 4)) + (255,)
       for k, l in zip('0123456789abcdef', [l for l in open(os.path.join(ROOT, '02_final', 'palette.hex')) if l.strip()])}


def draw():
    """inpaint가 4px만 바꿔 실패(2026-11-07) → 먼 앞팔을 직접 그린다(생성 없음). 결과 key_c_finish_overhead_v2c.png.
    단면(위→아래): 금색 b(빛 받는 위쪽, 광원 오른쪽 위) / 어두운 갑옷 2 / 검정 0. 손목 앞 금색 팔찌 9.
    머리카락 픽셀, 손 끝(t>0.9), x≥52는 그대로 둔다(머리는 팔 앞, 손은 손잡이를 쥔 원래 그림 유지)."""
    src = Image.open(SRC).convert('RGBA')
    out = src.copy()
    p = out.load()
    ax, ay = ELBOW
    vx, vy = HAND[0] - ax, HAND[1] - ay
    L = (vx * vx + vy * vy) ** 0.5
    nx, ny = -vy / L, vx / L          # (+)쪽 = 아래쪽 단면
    changed = []
    for y in range(S):
        for x in range(MAX_X + 1):
            t = ((x - ax) * vx + (y - ay) * vy) / (L * L)
            d = (x - ax) * nx + (y - ay) * ny
            if not (0 <= t <= 0.9 and D0 <= d <= D1) or is_hair(p[x, y]):
                continue
            c = 'b' if d < D0 + 0.9 else ('0' if d > D1 - 0.8 else '2')
            if 0.68 <= t <= 0.76:
                c = '9' if d <= D1 - 0.8 else '0'
            if p[x, y] != PAL[c]:
                p[x, y] = PAL[c]
                changed.append((x, y))
    out.save(os.path.join(D, 'key_c_finish_overhead_v2c.png'))
    band = {(x, y) for y in range(S) for x in range(MAX_X + 1)
            if 0 <= ((x - ax) * vx + (y - ay) * vy) / (L * L) <= 0.9 and D0 <= (x - ax) * nx + (y - ay) * ny <= D1}
    a = src.load()
    outside = sum(a[x, y] != p[x, y] for y in range(S) for x in range(S) if (x, y) not in band)
    d_ = px(out)
    b = out.getbbox()
    print('바뀐 픽셀', len(changed), '| 앞팔 띠 밖 변경', outside, '| 색 수', len({c[:3] for c in d_ if c[3]}),
          '| 알파', sorted({c[3] for c in d_}), '| 여백', (b[0], b[1], S - b[2], S - b[3]))
    bg = (58, 62, 72, 255)
    for name, crop, z, n in (('fararm_v2_v2c_x8.png', (30, 34, 70, 70), 8, 40), ('fararm_v2_v2c_x5.png', (18, 2, 78, 94), 5, 60)):
        W = Image.new('RGBA', (n * 2, crop[3] - crop[1]), bg)
        for i, im in enumerate((src, out)):
            W.alpha_composite(im.crop(crop), (n * i, 0))
        W.resize((W.width * z, W.height * z), Image.NEAREST).save(os.path.join(D, 'preview', name))


if __name__ == '__main__':
    {'mask': mask, 'compose': compose, 'draw': draw}[sys.argv[1]]()
