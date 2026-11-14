"""31번 2차 시트 f8만 칼을 2px 낮춤(생성 없음, 2026-11-07 사용자: "칼 위치가 좀 높다, 2px만 낮춰", 범위 = f8만).
f8(= key_c_finish_overhead_v7c_blade.png)에서 머리 위(y≤52)의 칼·코등이·손잡이·두 손·팔 끝을 2px 아래로 옮긴다.
정수리 머리카락(v3 머리 그림의 y51~52)은 제자리. 옮긴 손이 정수리를 조금 덮는 것은 허용(사용자: 얼굴 가려도 됨).
  python attack_slash02_lowsword.py -> key_c_finish_overhead_v7c_blade_low.png, 0_east_v2c.png, preview/finish_low_x6.png, GIF
"""
import os
from PIL import Image
from attack_slash_east import is_hair, px
from attack_slash02_grip import HEAD, SKIN

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
S, DY, CUT = 96, 2, 52


def main():
    src = Image.open(os.path.join(D, 'key_c_finish_overhead_v7c_blade.png')).convert('RGBA')
    v3 = Image.open(os.path.join(D, 'key_c_finish_overhead_v3.png')).convert('RGBA').load()
    hair_top = {(x, y) for y in range(HEAD[1], CUT + 1) for x in range(HEAD[0], HEAD[2] + 1)
                if v3[x, y][3] and (is_hair(v3[x, y]) or v3[x, y][:3] in SKIN)}
    a = src.load()
    moving = {(x, y): a[x, y] for y in range(CUT + 1) for x in range(S) if a[x, y][3] and (x, y) not in hair_top}
    out = src.copy()
    w = out.load()
    for q in moving:
        w[q] = (0, 0, 0, 0)
    for q in hair_top:                       # 정수리는 원래 머리 그림으로
        w[q] = v3[q]
    for (x, y), c in moving.items():
        w[x, y + DY] = c
    out.save(os.path.join(D, 'key_c_finish_overhead_v7c_blade_low.png'))

    sheet = Image.open(os.path.join(D, '0_east_v2b.png')).convert('RGBA')
    new = sheet.copy()
    new.paste(out, (S * 7, 0))
    new.save(os.path.join(D, '0_east_v2c.png'))
    ch = [(x, y) for y in range(S) for x in range(S) if a[x, y] != w[x, y]]
    d = px(new)
    b = out.getbbox()
    print('옮긴 픽셀', len(moving), '| f8 바뀐 픽셀', len(ch), '| y 범위', (min(y for _, y in ch), max(y for _, y in ch)),
          '| 시트 색 수', len({c[:3] for c in d if c[3]}), '| 알파', sorted({c[3] for c in d}), '| f8 여백', (b[0], b[1], S - b[2], S - b[3]))
    bg = (58, 62, 72, 255)
    W = Image.new('RGBA', (60 * 2, 92), bg)
    for i, im in enumerate((src, out)):
        W.alpha_composite(im.crop((18, 2, 78, 94)), (60 * i, 0))
    W.resize((W.width * 5, W.height * 5), Image.NEAREST).save(os.path.join(D, 'preview', 'finish_low_x5.png'))
    first = Image.open(os.path.join(ROOT, '29_attack_greatsword_light_slash_east', '0_east_pauldron_w.png')).convert('RGBA')
    fs = [new.crop((S * k, 0, S * k + S, S)) for k in range(8)]
    for name, fr in (('slash02_v2c_16fps_x4.gif', fs), ('slash01_02_chain_v2c_16fps_x4.gif', [first.crop((S * k, 0, S * k + S, S)) for k in range(8)] + fs[1:])):
        g = []
        for f in fr:
            c = Image.new('RGBA', (S, S), bg)
            c.alpha_composite(f)
            g.append(c.resize((S * 4, S * 4), Image.NEAREST).convert('RGB'))
        g[0].save(os.path.join(D, 'preview', name), save_all=True, append_images=g[1:],
                  duration=([60, 70] * 20)[:len(g) - 1] + [600], loop=0, disposal=1)


if __name__ == '__main__':
    main()
