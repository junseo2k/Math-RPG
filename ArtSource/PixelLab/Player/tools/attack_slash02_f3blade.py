"""31번 2타 f3의 칼을 120°(화면 왼쪽 위)로 다시 그림(생성 없음, 2026-11-07 사용자: "2번째 베기 때 칼이 수평면과 약 60°,
양의 방향 120°로 기울면 잔상과 맞을 듯" → 사용자 확인: f3만, 화면 왼쪽 위).
기존 칼날은 초승달 잔상 앞(x≤72)만 지우고(그 뒤는 잔상과 합쳐져 있어 둠), 코등이 중심에서 120° 방향으로
같은 길이·두께의 칼날을 새로 그린다. 손·머리·잔상은 그대로. 확정본 0_east_v2d.png는 수정하지 않는다.
  python attack_slash02_f3blade.py -> 0_east_v2e.png, preview/f3_blade120_x6.png, GIF
"""
import math
import os
from PIL import Image
from attack_slash_east import px
from attack_slash02_fararm import PAL

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
S, K = 96, 3                         # K = 고칠 프레임(1부터)
GUARD = (62, 55)                     # 코등이 중심(픽셀 지도에서 읽음)
OLD_TIP = (80, 40)                   # 기존 칼날 방향 끝(잔상과 합쳐지기 전)
ERASE_MAX_X = 72                     # 이 오른쪽은 잔상과 합쳐져 있어 지우지 않음
ANG, LENGTH = 120, 30                # 화면 기준(오른쪽 0°, 위 90°), 다른 프레임 칼날 길이 약 30px
CROSS = {-3: '7', -2: 'a', -1: 'a', 0: 'a', 1: 'c', 2: '7'}   # 폭 6px, +쪽(오른쪽 위 = 광원 쪽)이 밝음
BLADE = {PAL[c][:3] for c in '7ac'}


def main():
    sheet = Image.open(os.path.join(D, '0_east_v2d.png')).convert('RGBA')
    f = sheet.crop((S * (K - 1), 0, S * K, S))
    p = f.load()
    ox, oy = OLD_TIP[0] - GUARD[0], OLD_TIP[1] - GUARD[1]
    L = math.hypot(ox, oy)
    erased = 0
    for y in range(S):                   # 1) 기존 칼날 지우기(칼 방향 띠 안의 칼날색, x≤72)
        for x in range(GUARD[0] + 1, ERASE_MAX_X + 1):
            t = ((x - GUARD[0]) * ox + (y - GUARD[1]) * oy) / (L * L)
            d = abs((x - GUARD[0]) * oy - (y - GUARD[1]) * ox) / L
            if 0.05 <= t <= 1.1 and d <= 3.6 and p[x, y][3] and p[x, y][:3] in BLADE:
                p[x, y] = (0, 0, 0, 0)
                erased += 1
    ux, uy = math.cos(math.radians(ANG)), -math.sin(math.radians(ANG))   # 화면 y는 아래가 +
    nx, ny = -uy, ux                                                      # 칼날 단면 방향(+ = 오른쪽 위)
    if nx * 1 + ny * -1 < 0:
        nx, ny = -nx, -ny
    drawn = 0
    for i in range(2, LENGTH * 2 + 1):   # 2) 새 칼날(반 픽셀 간격으로 찍어 빈틈 없게), 끝 3px은 좁힘
        t = i / 2
        half = 3 if t < LENGTH - 3 else max(1, round(3 * (LENGTH - t) / 3))
        for w, c in CROSS.items():
            if not (-half <= w < half):
                continue
            q = (round(GUARD[0] + ux * t + nx * w), round(GUARD[1] + uy * t + ny * w))
            if 0 <= q[0] < S and 0 <= q[1] < S and (not p[q][3] or p[q][:3] in BLADE):
                if p[q] != PAL[c]:
                    p[q] = PAL[c]
                    drawn += 1
    for t in range(-3, 4):               # 3) 코등이: 새 칼날에 수직, 7px 금색
        q = (round(GUARD[0] + nx * t), round(GUARD[1] + ny * t))
        p[q] = PAL['9' if abs(t) == 3 else 'b']
    out = sheet.copy()
    out.paste(f, (S * (K - 1), 0))
    out.save(os.path.join(D, '0_east_v2e.png'))
    tip = (round(GUARD[0] + ux * LENGTH), round(GUARD[1] + uy * LENGTH))
    d = px(out)
    b = f.getbbox()
    a, b2 = sheet.load(), out.load()
    ch = [(x, y) for y in range(S) for x in range(S * 8) if a[x, y] != b2[x, y]]
    print(f'f{K}: 기존 칼날 {erased}px 지움, 새 칼날 {drawn}px, 칼끝 {tip}, 여백 좌상우하 {(b[0], b[1], S - b[2], S - b[3])}')
    print('바뀐 프레임', sorted({x // S + 1 for x, _ in ch}), '| 색 수', len({c[:3] for c in d if c[3]}), '| 알파', sorted({c[3] for c in d}))
    bg = (58, 62, 72, 255)
    W = Image.new('RGBA', (S * 2, S), bg)
    W.alpha_composite(sheet.crop((S * (K - 1), 0, S * K, S)), (0, 0))
    W.alpha_composite(f, (S, 0))
    W.resize((W.width * 5, W.height * 5), Image.NEAREST).save(os.path.join(D, 'preview', 'f3_blade120_x5.png'))
    first = Image.open(os.path.join(ROOT, '29_attack_greatsword_light_slash_east', '0_east_pauldron_w.png')).convert('RGBA')
    fs = [out.crop((S * k, 0, S * k + S, S)) for k in range(8)]
    for name, fr in (('slash02_v2e_16fps_x4.gif', fs), ('slash01_02_chain_v2e_16fps_x4.gif', [first.crop((S * k, 0, S * k + S, S)) for k in range(8)] + fs[1:])):
        g = []
        for fr_ in fr:
            c = Image.new('RGBA', (S, S), bg)
            c.alpha_composite(fr_)
            g.append(c.resize((S * 4, S * 4), Image.NEAREST).convert('RGB'))
        g[0].save(os.path.join(D, 'preview', name), save_all=True, append_images=g[1:],
                  duration=([60, 70] * 20)[:len(g) - 1] + [600], loop=0, disposal=1)


if __name__ == '__main__':
    main()
