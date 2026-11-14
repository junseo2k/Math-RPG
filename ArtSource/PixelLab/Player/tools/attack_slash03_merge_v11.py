"""32번 3타 v10 + fix_v8 제동·회수 → v11(생성 없음, 2026-11-08 사용자 "v10에 합치기").
v10의 f1~f4(준비·타격+초승달)와 f10~f12는 그대로, f5~f9만 fix_v8 수정안(seg 2·3·4·6·7번째)으로 바꾼다.
fix_v8의 f3(칼 40°)은 v10 준비 구간과 겹쳐 쓰지 않음. f5의 기존 테두리 잔상은 원래 픽셀을 새 그림 빈 칸에 그대로 얹음.
  python attack_slash03_merge_v11.py -> 0_east_v11.png, preview/merge_v10_v11_x2.png, 이어 보기 GIF
"""
import os
from PIL import Image
from attack_slash_east import is_blade, px
from attack_slash03_fix_v8 import sheets, new_clean

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D2 = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
D3 = os.path.join(ROOT, '32_attack_greatsword_heavy_slash_03_east')
S = 96
FIX = range(5, 10)


def main():
    v10 = Image.open(os.path.join(D3, '0_east_v10.png')).convert('RGBA')
    v7 = Image.open(os.path.join(D3, '0_east_v7.png')).convert('RGBA')
    v3 = Image.open(os.path.join(D3, '0_east_v3.png')).convert('RGBA')
    _, clean = sheets()
    after = new_clean(clean)
    a, b = v7.load(), v3.load()
    rim = {(x - S * 4, y): a[x, y] for y in range(S) for x in range(S * 4, S * 5) if a[x, y] != b[x, y]}   # f5 테두리 잔상
    old = [v10.crop((S * k, 0, S * k + S, S)) for k in range(12)]
    new = list(old)
    for k in FIX:
        new[k - 1] = after[k - 1].copy()
    p = new[4].load()
    n = 0
    for q, c in rim.items():
        if not p[q][3]:
            p[q] = c
            n += 1
    out = Image.new('RGBA', (S * 12, S), (0, 0, 0, 0))
    for k, f in enumerate(new):
        out.paste(f, (S * k, 0))
    out.save(os.path.join(D3, '0_east_v11.png'))
    same = [k + 1 for k in range(12) if new[k].tobytes() == old[k].tobytes()]
    print('잔상 테두리', n, 'px 얹음 | v10과 같은 프레임', same)
    for k in FIX:
        bb = new[k - 1].getbbox()
        pp = new[k - 1].load()
        fx = [x for y in range(91, 94) for x in range(S) if pp[x, y][3] and not is_blade(pp[x, y])]
        print(f'f{k} 여백 {(bb[0], bb[1], S - bb[2], S - bb[3])} 발 x {(min(fx), max(fx))}')
    d = px(out)
    s2 = Image.open(os.path.join(D2, '0_east_v2i.png')).convert('RGBA')
    print('2타 f8 == 3타 f1:', s2.crop((S * 7, 0, S * 8, S)).tobytes() == new[0].tobytes(),
          '| 색 수', len({c[:3] for c in d if c[3]}), '| 알파', sorted({c[3] for c in d}))
    bg = (58, 62, 72, 255)
    W = Image.new('RGBA', (S * 7, S * 2), bg)
    for r, row in enumerate((old, new)):
        for i, k in enumerate(range(3, 10)):
            W.alpha_composite(row[k], (S * i, S * r))
    W.resize((W.width * 2, W.height * 2), Image.NEAREST).save(os.path.join(D3, 'preview', 'merge_v10_v11_x2.png'))
    first = Image.open(os.path.join(ROOT, '29_attack_greatsword_light_slash_east', '0_east_pauldron_w.png')).convert('RGBA')
    chain = [first.crop((S * k, 0, S * k + S, S)) for k in range(8)] + [s2.crop((S * k, 0, S * k + S, S)) for k in range(1, 8)] + new[1:]
    g = []
    for f in chain:
        c = Image.new('RGBA', (S, S), bg)
        c.alpha_composite(f)
        g.append(c.resize((S * 4, S * 4), Image.NEAREST).convert('RGB'))
    g[0].save(os.path.join(D3, 'preview', 'slash01_02i_03v11_chain_16fps_x4.gif'), save_all=True, append_images=g[1:],
              duration=([60, 70] * 40)[:len(g) - 1] + [600], loop=0, disposal=1)
    print('이어 보기', len(g), '장')


if __name__ == '__main__':
    main()
