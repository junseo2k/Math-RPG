"""31번 2차 시트에 잔상 넣기(생성 없음, 2026-11-07 사용자: "1회 벨 때 잔상이 남잖아, 2회 벨 때도 남게 해 줘").
첫 공격은 타격 두 프레임(f4 큰 초승달, f5 작은 초승달+칼)에 잔상이 있다. 2타도 같은 도구로 만든 잔상을 되살린다:
  f3 = seg1/frame_02(준비→타격 구간 생성본, 회색 초승달; 이전엔 잔상이라 뺐음)
  f4 = 승인 타격 포즈 + seg1/frame_03에서 지웠던 떨어진 호 조각(빈 칸에만)
색은 팔레트 16색으로 맞춤. 나머지 프레임은 0_east_v2c.png 그대로.
  python attack_slash02_smear.py -> 0_east_v2d.png, preview/smear_v2c_v2d_x3.png, GIF
"""
import os
from PIL import Image
from attack_slash_east import snap, px
from attack_slash02_build import drop_detached

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
S = 96


def main():
    sheet = Image.open(os.path.join(D, '0_east_v2c.png')).convert('RGBA')
    new = sheet.copy()
    f3 = snap(Image.open(os.path.join(D, 'mid_raw', 'seg1', 'frame_02.png')).convert('RGBA'))
    raw3 = snap(Image.open(os.path.join(D, 'mid_raw', 'seg1', 'frame_03.png')).convert('RGBA'))
    clean3 = raw3.copy()
    drop_detached(clean3)
    r, c = raw3.load(), clean3.load()
    arc = {(x, y): r[x, y] for y in range(S) for x in range(S) if r[x, y][3] and not c[x, y][3]}
    f4 = sheet.crop((S * 3, 0, S * 4, S))
    p = f4.load()
    n = 0
    for q, col in arc.items():
        if not p[q][3]:
            p[q] = col
            n += 1
    new.paste(f3, (S * 2, 0))
    new.paste(f4, (S * 3, 0))
    new.save(os.path.join(D, '0_east_v2d.png'))
    d = px(new)
    a, b = sheet.load(), new.load()
    ch = [(x, y) for y in range(S) for x in range(S * 8) if a[x, y] != b[x, y]]
    for k, f in ((3, f3), (4, f4)):
        bb = f.getbbox()
        print(f'f{k} 여백 좌상우하', (bb[0], bb[1], S - bb[2], S - bb[3]))
    print('f4에 얹은 호 조각', n, 'px | 바뀐 프레임', sorted({x // S + 1 for x, _ in ch}), '| 색 수', len({c[:3] for c in d if c[3]}),
          '| 알파', sorted({c[3] for c in d}))
    bg = (58, 62, 72, 255)
    first = Image.open(os.path.join(ROOT, '29_attack_greatsword_light_slash_east', '0_east_pauldron_w.png')).convert('RGBA')
    W = Image.new('RGBA', (S * 4, S * 3), bg)
    for row, (sh, ks) in enumerate(((first, (2, 3, 4, 5)), (sheet, (1, 2, 3, 4)), (new, (1, 2, 3, 4)))):
        for i, k in enumerate(ks):
            W.alpha_composite(sh.crop((S * k, 0, S * k + S, S)), (S * i, S * row))
    W.resize((W.width * 3, W.height * 3), Image.NEAREST).save(os.path.join(D, 'preview', 'smear_v2c_v2d_x3.png'))
    fs = [new.crop((S * k, 0, S * k + S, S)) for k in range(8)]
    for name, fr in (('slash02_v2d_16fps_x4.gif', fs), ('slash01_02_chain_v2d_16fps_x4.gif', [first.crop((S * k, 0, S * k + S, S)) for k in range(8)] + fs[1:])):
        g = []
        for f in fr:
            cc = Image.new('RGBA', (S, S), bg)
            cc.alpha_composite(f)
            g.append(cc.resize((S * 4, S * 4), Image.NEAREST).convert('RGB'))
        g[0].save(os.path.join(D, 'preview', name), save_all=True, append_images=g[1:],
                  duration=([60, 70] * 20)[:len(g) - 1] + [600], loop=0, disposal=1)


if __name__ == '__main__':
    main()
