"""31번 2타 v2h → v2i: f7의 위쪽(y<66: 손·팔·칼·머리)을 f8과 같게(생성 없음, 2026-11-08 사용자 "f7도 맞추기").
f8만 손·칼을 120° 손잡이 축에 다시 배치해 f7→f8에서 칼이 약 8px 옮겨졌다(655px 차이). 아래쪽(다리·망토)은 f7 그대로.
  python attack_slash02_f7link.py -> 0_east_v2i.png, preview/f6_f8_v2h_v2i_x3.png, 1·2·3타 이어 보기(3타 v7)
"""
import os
from PIL import Image
from attack_slash_east import px

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D2 = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
D3 = os.path.join(ROOT, '32_attack_greatsword_heavy_slash_03_east')
S, CUT = 96, 66


def main():
    s2 = Image.open(os.path.join(D2, '0_east_v2h.png')).convert('RGBA')
    f8 = s2.crop((S * 7, 0, S * 8, S))
    f7 = s2.crop((S * 6, 0, S * 7, S))
    new7 = f7.copy()
    new7.paste(Image.new('RGBA', (S, CUT), (0, 0, 0, 0)), (0, 0))
    new7.alpha_composite(f8.crop((0, 0, S, CUT)), (0, 0))
    out = s2.copy()
    out.paste(new7, (S * 6, 0))
    out.save(os.path.join(D2, '0_east_v2i.png'))
    fs = [out.crop((S * k, 0, S * k + S, S)) for k in range(8)]
    old = [s2.crop((S * k, 0, S * k + S, S)) for k in range(8)]
    diff = lambda a, b: sum(x != y for x, y in zip(px(a), px(b)))
    print('이전 f6→f7, f7→f8:', diff(old[5], old[6]), diff(old[6], old[7]), '| 수정 f6→f7, f7→f8:', diff(fs[5], fs[6]), diff(fs[6], fs[7]))
    b = new7.getbbox()
    d = px(out)
    print('f7 여백 좌상우하', (b[0], b[1], S - b[2], S - b[3]), '| 색 수', len({c[:3] for c in d if c[3]}), '| 알파', sorted({c[3] for c in d}))
    bg = (58, 62, 72, 255)
    W = Image.new('RGBA', (S * 3, S * 2), bg)
    for r, row in enumerate((old, fs)):
        for i, k in enumerate((5, 6, 7)):
            W.alpha_composite(row[k], (S * i, S * r))
    W.resize((W.width * 3, W.height * 3), Image.NEAREST).save(os.path.join(D2, 'preview', 'f6_f8_v2h_v2i_x3.png'))
    s3 = Image.open(os.path.join(D3, '0_east_v7.png')).convert('RGBA')
    print('2타 f8 == 3타 f1:', fs[7].tobytes() == s3.crop((0, 0, S, S)).tobytes())
    first = Image.open(os.path.join(ROOT, '29_attack_greatsword_light_slash_east', '0_east_pauldron_w.png')).convert('RGBA')
    chain = [first.crop((S * k, 0, S * k + S, S)) for k in range(8)] + fs[1:] + [s3.crop((S * k, 0, S * k + S, S)) for k in range(1, 12)]
    g = []
    for fr in chain:
        c = Image.new('RGBA', (S, S), bg)
        c.alpha_composite(fr)
        g.append(c.resize((S * 4, S * 4), Image.NEAREST).convert('RGB'))
    g[0].save(os.path.join(D3, 'preview', 'slash01_02i_03v7_chain_16fps_x4.gif'), save_all=True, append_images=g[1:],
              duration=([60, 70] * 40)[:len(g) - 1] + [600], loop=0, disposal=1)


if __name__ == '__main__':
    main()
