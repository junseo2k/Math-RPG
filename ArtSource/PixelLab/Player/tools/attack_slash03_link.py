"""32번 3타 v6 → v7: 3타 f2를 f1(= 2타 f8)과 완전히 같게(생성 없음, 2026-11-08 사용자: "2타 끝 프레임과 3타 시작 프레임이 살짝 안 맞는다").
이어 보기에서는 3타 f1이 2타 f8과 같아 빠지므로 2타 f8 다음이 3타 f2다. f2는 허리 아래(y≥70)가 힘 모으기용 생성본 다리라
223px 달랐다. f2 = f1로 바꿔 2타 끝 자세가 한 프레임 더 이어지고, 다리 변화는 f3부터 시작.
  python attack_slash03_link.py -> 0_east_v7.png, preview/slash01_02h_03v7_chain_16fps_x4.gif
"""
import os
from PIL import Image
from attack_slash_east import px

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D2 = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
D3 = os.path.join(ROOT, '32_attack_greatsword_heavy_slash_03_east')
S = 96


def main():
    s3 = Image.open(os.path.join(D3, '0_east_v6.png')).convert('RGBA')
    out = s3.copy()
    out.paste(s3.crop((0, 0, S, S)), (S, 0))
    out.save(os.path.join(D3, '0_east_v7.png'))
    s2 = Image.open(os.path.join(D2, '0_east_v2h.png')).convert('RGBA')
    f8 = s2.crop((S * 7, 0, S * 8, S)).tobytes()
    print('2타 f8 == 3타 f1:', f8 == out.crop((0, 0, S, S)).tobytes(), '| == 3타 f2:', f8 == out.crop((S, 0, 2 * S, S)).tobytes())
    fs = [out.crop((S * k, 0, S * k + S, S)) for k in range(12)]
    ch = [sum(a != b for a, b in zip(px(fs[k]), px(fs[k + 1]))) for k in range(11)]
    d = px(out)
    print('3타 이웃 프레임 바뀐 픽셀', ch, '| 색 수', len({c[:3] for c in d if c[3]}), '| 알파', sorted({c[3] for c in d}))
    bg = (58, 62, 72, 255)
    first = Image.open(os.path.join(ROOT, '29_attack_greatsword_light_slash_east', '0_east_pauldron_w.png')).convert('RGBA')
    chain = ([first.crop((S * k, 0, S * k + S, S)) for k in range(8)] + [s2.crop((S * k, 0, S * k + S, S)) for k in range(1, 8)]
             + fs[1:])
    g = []
    for fr in chain:
        c = Image.new('RGBA', (S, S), bg)
        c.alpha_composite(fr)
        g.append(c.resize((S * 4, S * 4), Image.NEAREST).convert('RGB'))
    g[0].save(os.path.join(D3, 'preview', 'slash01_02h_03v7_chain_16fps_x4.gif'), save_all=True, append_images=g[1:],
              duration=([60, 70] * 40)[:len(g) - 1] + [600], loop=0, disposal=1)


if __name__ == '__main__':
    main()
