"""칼 120° 수정본(2타 v2g, 3타 v5)에서 잘못 지운 손잡이·손 되살리기(생성 없음, 2026-11-07 사용자: "손잡이랑 손이 사라졌다, 2타 마지막에").
원인: attack_chain_blade120.py가 뿌리 근처 금색을 옛 코등이로 보고 지웠는데, 그중 손잡이(금색 감은 대각선)·손마디가 섞여 있었다.
수정 전 그림(2타 v2f, 3타 v4)에서 칼날색(7·a·c)이 아닌데 지금 비어 있는 픽셀만 그대로 되살린다. 결과는 같은 파일에 덮어쓴다
(v2g·v5는 아직 확정 전 수정본).
  python attack_chain_restore_grip.py
"""
import os
from PIL import Image
from attack_slash_east import px
from attack_slash02_fararm import PAL

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D2 = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
D3 = os.path.join(ROOT, '32_attack_greatsword_heavy_slash_03_east')
S = 96
BLADE = {PAL[c][:3] for c in '7ac'}
# 칼 뿌리 6px 안만 되살림(1차 실행 때 f4의 옛 호 잔상 21px·f6의 외딴 점까지 돌아와 다시 지웠음, 2026-11-07)
ROOTS = {('v2g', 4): (51, 53), ('v2g', 5): (52, 57), ('v2g', 6): (52, 54), ('v2g', 7): (53, 48), ('v2g', 8): (53, 48),
         ('v5', 1): (53, 48), ('v5', 2): (53, 48), ('v5', 3): (53, 48)}
NEAR = 6


def restore(before, after, ks):
    tag = 'v2g' if 'v2g' in after else 'v5'
    a = Image.open(before).convert('RGBA')
    b = Image.open(after).convert('RGBA')
    pa, pb = a.load(), b.load()
    for k in ks:
        got = []
        for y in range(S):
            for x in range(S * (k - 1), S * k):
                r = ROOTS[(tag, k)]
                near = (x - S * (k - 1) - r[0]) ** 2 + (y - r[1]) ** 2 <= NEAR * NEAR
                if near and pa[x, y][3] and not pb[x, y][3] and pa[x, y][:3] not in BLADE:
                    pb[x, y] = pa[x, y]
                    got.append((x - S * (k - 1), y))
        print(f'{os.path.basename(after)} f{k}: 되살린 픽셀 {len(got)} {got}')
    b.save(after)
    return b


def main():
    o2 = restore(os.path.join(D2, '0_east_v2f.png'), os.path.join(D2, '0_east_v2g.png'), range(4, 9))
    o3 = restore(os.path.join(D3, '0_east_v4.png'), os.path.join(D3, '0_east_v5.png'), range(1, 4))
    print('2타 f8 == 3타 f1:', o2.crop((S * 7, 0, S * 8, S)).tobytes() == o3.crop((0, 0, S, S)).tobytes())
    for nm, sh, n in (('v2g', o2, 8), ('v5', o3, 12)):
        bad = []
        for k in range(n):
            bb = sh.crop((S * k, 0, S * k + S, S)).getbbox()
            m = (bb[0], bb[1], S - bb[2], S - bb[3])
            if min(m) < 2:
                bad.append((k + 1, m))
        d = px(sh)
        print(nm, '여백 실패', bad, '색 수', len({c[:3] for c in d if c[3]}), '알파', sorted({c[3] for c in d}))
    bg = (58, 62, 72, 255)
    before = Image.open(os.path.join(D2, '0_east_v2f.png')).convert('RGBA').crop((S * 7, 0, S * 8, S))
    W = Image.new('RGBA', (48, 30), bg)
    W.alpha_composite(before.crop((38, 36, 62, 66)), (0, 0))
    W.alpha_composite(o2.crop((S * 7 + 38, 36, S * 7 + 62, 66)), (24, 0))
    W.resize((W.width * 10, W.height * 10), Image.NEAREST).save(os.path.join(D2, 'preview', 'grip_restore_f8_x10.png'))
    first = Image.open(os.path.join(ROOT, '29_attack_greatsword_light_slash_east', '0_east_pauldron_w.png')).convert('RGBA')
    chain = ([first.crop((S * k, 0, S * k + S, S)) for k in range(8)] + [o2.crop((S * k, 0, S * k + S, S)) for k in range(1, 8)]
             + [o3.crop((S * k, 0, S * k + S, S)) for k in range(1, 12)])
    g = []
    for f in chain:
        c = Image.new('RGBA', (S, S), bg)
        c.alpha_composite(f)
        g.append(c.resize((S * 4, S * 4), Image.NEAREST).convert('RGB'))
    g[0].save(os.path.join(D3, 'preview', 'slash01_02g_03v5_chain_16fps_x4.gif'), save_all=True, append_images=g[1:],
              duration=([60, 70] * 40)[:len(g) - 1] + [600], loop=0, disposal=1)


if __name__ == '__main__':
    main()
