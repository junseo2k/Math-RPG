"""32번 3타 v2 → v3: f6·f7 칼끝이 바닥선에 닿아 아래 여백 0·1px → 칼끝만 들어 올려 맨 아랫줄 y93(여백 2px).
자르지 않고(메모리: 잘라내지 말 것) 칼 뿌리는 그대로, 칼끝 쪽으로 갈수록 위로 옮기는 기울이기(열마다 정수 이동)라
칼 길이·두께는 유지된다. 생성 없음(2026-11-07 사용자 "칼끝 여백 맞추기").
  python attack_slash03_tip.py -> 0_east_v3.png, preview/tip_v2_v3_x6.png, GIF
"""
import os
from PIL import Image
from attack_slash_east import is_blade, px

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D = os.path.join(ROOT, '32_attack_greatsword_heavy_slash_03_east')
S, LAST = 96, 93
FIX = (6, 7)          # 1부터
Y0 = 66               # 이 줄 아래의 칼날만(손·몸 위쪽 회색은 건드리지 않음)


def main():
    sheet = Image.open(os.path.join(D, '0_east_v2.png')).convert('RGBA')
    out = sheet.copy()
    for k in FIX:
        f = sheet.crop((S * (k - 1), 0, S * k, S))
        p = f.load()
        bl = {(x, y): p[x, y] for y in range(Y0, S) for x in range(S) if is_blade(p[x, y])}
        lift = max(y for _, y in bl) - LAST
        if lift <= 0:
            continue
        x0 = min(x for x, _ in bl)                   # 칼 뿌리 쪽 끝
        x1 = max(x for x, _ in bl)                   # 칼끝 쪽 끝
        for q in bl:
            p[q] = (0, 0, 0, 0)
        for (x, y), c in bl.items():
            dy = round(lift * (x - x0) / (x1 - x0))
            if not p[x, y - dy][3]:
                p[x, y - dy] = c
        out.paste(f, (S * (k - 1), 0))
        b = f.getbbox()
        print(f'f{k}: 칼날 {len(bl)}px, 칼끝 {lift}px 올림(x{x0}→{x1}에 걸쳐), 여백 좌상우하 {(b[0], b[1], S - b[2], S - b[3])},',
              f'칼날 회색 {sum(1 for y in range(Y0, S) for x in range(S) if is_blade(p[x, y]))}px')
    out.save(os.path.join(D, '0_east_v3.png'))
    a, b2 = sheet.load(), out.load()
    ch = [(x, y) for y in range(S) for x in range(S * 12) if a[x, y] != b2[x, y]]
    d = px(out)
    print('바뀐 픽셀', len(ch), '| 바뀐 프레임', sorted({x // S + 1 for x, _ in ch}), '| 색 수', len({c[:3] for c in d if c[3]}),
          '| 알파', sorted({c[3] for c in d}))
    bg = (58, 62, 72, 255)
    W = Image.new('RGBA', (64 * 2, 40 * 2), bg)
    for r, sh in enumerate((sheet, out)):
        for i, k in enumerate(FIX):
            W.alpha_composite(sh.crop((S * (k - 1) + 30, 56, S * (k - 1) + 94, 96)), (64 * i, 40 * r))
    W.resize((W.width * 6, W.height * 6), Image.NEAREST).save(os.path.join(D, 'preview', 'tip_v2_v3_x6.png'))
    first = Image.open(os.path.join(ROOT, '29_attack_greatsword_light_slash_east', '0_east_pauldron_w.png')).convert('RGBA')
    second = Image.open(os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east', '0_east_v2d.png')).convert('RGBA')
    fs = [out.crop((S * k, 0, S * k + S, S)) for k in range(12)]
    chain = [first.crop((S * k, 0, S * k + S, S)) for k in range(8)] + [second.crop((S * k, 0, S * k + S, S)) for k in range(1, 8)] + fs[1:]
    for name, fr in (('slash03_v3_16fps_x4.gif', fs), ('slash01_02_03_chain_v3_16fps_x4.gif', chain)):
        g = []
        for f in fr:
            c = Image.new('RGBA', (S, S), bg)
            c.alpha_composite(f)
            g.append(c.resize((S * 4, S * 4), Image.NEAREST).convert('RGB'))
        g[0].save(os.path.join(D, 'preview', name), save_all=True, append_images=g[1:],
                  duration=([60, 70] * 40)[:len(g) - 1] + [600], loop=0, disposal=1)


if __name__ == '__main__':
    main()
