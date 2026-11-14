"""2타 f8·3타 f1~f3: 두 손을 120° 칼의 손잡이 축 위에 다시 배치(생성 없음, 2026-11-07 사용자: "손 다시 배치").
지금 손 배치(위 (50,51), 아래 (48,53))는 원래 40° 칼 기준이라, 120° 칼이 손 옆에서 나와 보였다.
아래 손(왼손, 머리 오른쪽 팔) (48,53)은 그대로 두고, 위 손(오른손, 머리 왼쪽 팔)을 손잡이 축(칼 반대 방향 = 오른쪽 아래)을 따라
왼손에서 3px 위인 (46,50)으로 옮긴다. 코등이 = 오른손에서 칼 쪽 2.5px, 칼날 길이 30·폭 6, 폼멜 = 왼손에서 2.5px 아래.
오른팔 앞팔은 팔꿈치 (41,55)에서 새 오른손까지 다시 그림(가려졌던 머리는 v3 머리 그림으로 되살림).
  python attack_chain_hands120.py -> 31_.../0_east_v2h.png(f8), 32_.../0_east_v5.png 기준 0_east_v6.png(f1~f3), 비교·이어 보기
"""
import math
import os
from PIL import Image
from attack_slash_east import is_hair, px
from attack_slash02_fararm import PAL
from attack_slash02_grip import HEAD, SKIN, line

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D2 = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
D3 = os.path.join(ROOT, '32_attack_greatsword_heavy_slash_03_east')
S = 96
ANG, LENGTH = 120, 30
UX, UY = math.cos(math.radians(ANG)), -math.sin(math.radians(ANG))   # 칼 방향(화면 y 아래가 +)
VX, VY = -UX, -UY                                                     # 손잡이 방향(오른쪽 아래)
NX, NY = (-UY, UX) if -UY - UX > 0 else (UY, -UX)                    # 칼 단면 + = 오른쪽 위
LOWER = (48, 53)                                                      # 왼손(그대로)
UPPER = (round(LOWER[0] - 3 * VX), round(LOWER[1] - 3 * VY))          # 오른손
GUARD = (UPPER[0] - 2.5 * VX, UPPER[1] - 2.5 * VY)
POMMEL = (round(LOWER[0] + 2.5 * VX), round(LOWER[1] + 2.5 * VY))
ELBOW = (41, 55)                                                      # 오른팔 팔꿈치(v7c 그대로)
OLD_HAND = (50, 51)                                                   # 옛 오른손(칼 2px 내린 뒤 위치)
CROSS = {-3: '7', -2: 'a', -1: 'a', 0: 'a', 1: 'c', 2: '7'}


def head_layer():
    v3 = Image.open(os.path.join(D2, 'key_c_finish_overhead_v3.png')).convert('RGBA').load()
    return {(x, y): v3[x, y] for y in range(HEAD[1], HEAD[3] + 1) for x in range(HEAD[0], HEAD[2] + 1)
            if v3[x, y][3] and (is_hair(v3[x, y]) or v3[x, y][:3] in SKIN)}


def fix(f, head):
    p = f.load()
    clear = set()
    for y in range(S):                                   # 1) 머리 위 칼·코등이·옛 손잡이·옛 오른손
        for x in range(S):
            if y <= 50 or (51 <= y <= 52 and x >= 52) or (49 <= x <= 51 and 50 <= y <= 52):
                clear.add((x, y))
    for x, y in line(ELBOW, OLD_HAND):                   # 2) 옛 오른팔 앞팔(폭: 단면 x−2~+1)
        for dx in range(-2, 2):
            clear.add((x + dx, y))
    clear = {q for q in clear if q[1] < 56}              # 팔꿈치 아래 몸은 건드리지 않음
    for q in clear:
        p[q] = head.get(q, (0, 0, 0, 0))
    for x, y in line(ELBOW, UPPER):                      # 3) 새 오른팔 앞팔: 단면 '022b', 바깥(화면 왼쪽) 금색
        for i, c in enumerate('022b'):
            p[x - (i - 1), y] = PAL[c]
    for x, y in line(POMMEL, (round(GUARD[0]), round(GUARD[1]))):   # 4) 손잡이(어두운 2), 폼멜 금색
        p[x, y] = PAL['2']
    p[POMMEL] = PAL['9']
    for i in range(2, LENGTH * 2 + 1):                   # 5) 칼날(빈 칸에만, 머리 위)
        t = i / 2
        half = 3 if t < LENGTH - 3 else max(1, round(3 * (LENGTH - t) / 3))
        for w, c in CROSS.items():
            if -half <= w < half:
                q = (round(GUARD[0] + UX * t + NX * w), round(GUARD[1] + UY * t + NY * w))
                if not p[q][3]:
                    p[q] = PAL[c]
    for t in range(-3, 4):                               # 6) 코등이: 칼에 수직 7px
        p[round(GUARD[0] + NX * t), round(GUARD[1] + NY * t)] = PAL['9' if abs(t) == 3 else 'b']
    for hx, hy in (LOWER, UPPER):                        # 7) 손: 3×3 건틀릿, 위 줄 금색 손마디(오른손이 맨 앞)
        for y in range(hy - 1, hy + 2):
            for x in range(hx - 1, hx + 2):
                p[x, y] = PAL['9'] if y == hy - 1 else PAL['2']


def main():
    head = head_layer()
    s2 = Image.open(os.path.join(D2, '0_east_v2g.png')).convert('RGBA')
    s3 = Image.open(os.path.join(D3, '0_east_v5.png')).convert('RGBA')
    o2, o3 = s2.copy(), s3.copy()
    for sh, out, ks in ((s2, o2, (8,)), (s3, o3, (1, 2, 3))):
        for k in ks:
            f = sh.crop((S * (k - 1), 0, S * k, S))
            fix(f, head)
            out.paste(f, (S * (k - 1), 0))
    o2.save(os.path.join(D2, '0_east_v2h.png'))
    o3.save(os.path.join(D3, '0_east_v6.png'))
    print('오른손', UPPER, '왼손', LOWER, '코등이', tuple(round(v, 1) for v in GUARD), '폼멜', POMMEL,
          '칼끝', (round(GUARD[0] + UX * LENGTH), round(GUARD[1] + UY * LENGTH)))
    print('2타 f8 == 3타 f1:', o2.crop((S * 7, 0, S * 8, S)).tobytes() == o3.crop((0, 0, S, S)).tobytes())
    for nm, sh, n in (('v2h', o2, 8), ('v6', o3, 12)):
        bad = []
        for k in range(n):
            b = sh.crop((S * k, 0, S * k + S, S)).getbbox()
            m = (b[0], b[1], S - b[2], S - b[3])
            if min(m) < 2:
                bad.append((k + 1, m))
        d = px(sh)
        print(nm, '여백 실패', bad, '색 수', len({c[:3] for c in d if c[3]}), '알파', sorted({c[3] for c in d}))
    bg = (58, 62, 72, 255)
    before = s2.crop((S * 7, 0, S * 8, S))
    after = o2.crop((S * 7, 0, S * 8, S))
    W = Image.new('RGBA', (40 * 2, 48), bg)
    W.alpha_composite(before.crop((26, 18, 66, 66)), (0, 0))
    W.alpha_composite(after.crop((26, 18, 66, 66)), (40, 0))
    W.resize((W.width * 8, W.height * 8), Image.NEAREST).save(os.path.join(D2, 'preview', 'hands120_f8_x8.png'))
    W = Image.new('RGBA', (60 * 2, 92), bg)
    W.alpha_composite(before.crop((18, 2, 78, 94)), (0, 0))
    W.alpha_composite(after.crop((18, 2, 78, 94)), (60, 0))
    W.resize((W.width * 5, W.height * 5), Image.NEAREST).save(os.path.join(D2, 'preview', 'hands120_f8_x5.png'))
    first = Image.open(os.path.join(ROOT, '29_attack_greatsword_light_slash_east', '0_east_pauldron_w.png')).convert('RGBA')
    chain = ([first.crop((S * k, 0, S * k + S, S)) for k in range(8)] + [o2.crop((S * k, 0, S * k + S, S)) for k in range(1, 8)]
             + [o3.crop((S * k, 0, S * k + S, S)) for k in range(1, 12)])
    g = []
    for fr in chain:
        c = Image.new('RGBA', (S, S), bg)
        c.alpha_composite(fr)
        g.append(c.resize((S * 4, S * 4), Image.NEAREST).convert('RGB'))
    g[0].save(os.path.join(D3, 'preview', 'slash01_02h_03v6_chain_16fps_x4.gif'), save_all=True, append_images=g[1:],
              duration=([60, 70] * 40)[:len(g) - 1] + [600], loop=0, disposal=1)


if __name__ == '__main__':
    main()
