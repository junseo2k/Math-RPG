"""32번 3타 v9 → v10 수정안: 준비 구간(이어 보기 v9의 GIF 16·17 = 3타 f2·f3)만, 생성 없음(2026-11-08 사용자).
지시: 2타 끝의 손잡이 위치·잡는 구조를 이어받고, 칼을 왼쪽 위 → 수직 → 오른쪽 위로 다시 돌리는 동작을 줄인다.
강공격 준비는 무릎 굽힘, 골반·가슴의 작은 움직임, 짧은 정지로. GIF 14·15(2타 f7·f8)는 두 번째 공격이라 그대로.
  3타 f2: 위쪽(y<70) = 2타 f8 그대로(칼 120°, 짧은 정지), 다리 = segA frame_01(무릎 살짝 굽힘)
  3타 f3: 칼 105°(오른손·앞팔이 함께, attack_slash03_start_v9.pose), 허리 위 1px 내림(골반·가슴이 아래로), 다리 = 기존 f3(더 굽힘)
  python attack_slash03_prep_v10.py -> 0_east_v10.png, preview/prep_v9_v10_x3.png, 이어 보기 GIF
"""
import os
from PIL import Image
from attack_slash_east import snap, px
from attack_chain_hands120 import head_layer
from attack_slash03_start_v9 import pose

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D2 = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
D3 = os.path.join(ROOT, '32_attack_greatsword_heavy_slash_03_east')
S, CUT = 96, 70
F3_ANG, DROP = 105, 1


def main():
    v9 = Image.open(os.path.join(D3, '0_east_v9.png')).convert('RGBA')
    v7 = Image.open(os.path.join(D3, '0_east_v7.png')).convert('RGBA')
    s2 = Image.open(os.path.join(D2, '0_east_v2i.png')).convert('RGBA')
    f8 = s2.crop((S * 7, 0, S * 8, S))
    old = [v9.crop((S * k, 0, S * k + S, S)) for k in range(12)]
    new = list(old)
    legs1 = snap(Image.open(os.path.join(D3, 'mid_raw', 'segA', 'frame_01.png')).convert('RGBA')).crop((0, CUT, S, S))
    legs2 = v7.crop((S * 2, CUT, S * 3, S))                         # 기존 f3 다리(무릎 더 굽힘)
    f2 = f8.copy()
    f2.paste(Image.new('RGBA', (S, S - CUT), (0, 0, 0, 0)), (0, CUT))
    f2.alpha_composite(legs1, (0, CUT))
    new[1] = f2
    top, upper, guard, tip = pose(f8, F3_ANG, head_layer())
    f3 = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    f3.alpha_composite(legs2, (0, CUT))
    f3.alpha_composite(top.crop((0, 0, S, CUT)), (0, DROP))        # 허리 위 1px 내려 다리 위에 얹음
    new[2] = f3
    out = Image.new('RGBA', (S * 12, S), (0, 0, 0, 0))
    for k, f in enumerate(new):
        out.paste(f, (S * k, 0))
    out.save(os.path.join(D3, '0_east_v10.png'))
    diff = lambda a, b: sum(x != y for x, y in zip(px(a), px(b)))
    print(f'3타 f3: 칼 {F3_ANG}°, 오른손 {upper}, 칼끝 {tip}(허리 위 {DROP}px 내림 전 좌표)')
    print('이웃 차이(2타 f8→f2→f3→f4) 전:', [diff(f8, old[1]), diff(old[1], old[2]), diff(old[2], old[3])],
          '후:', [diff(f8, new[1]), diff(new[1], new[2]), diff(new[2], new[3])])
    d = px(out)
    for k in (1, 2):
        b = new[k].getbbox()
        print(f'3타 f{k + 1} 여백 {(b[0], b[1], S - b[2], S - b[3])}')
    print('2타 f8 == 3타 f1:', f8.tobytes() == new[0].tobytes(), '| 색 수', len({c[:3] for c in d if c[3]}), '| 알파', sorted({c[3] for c in d}))
    bg = (58, 62, 72, 255)
    f7 = s2.crop((S * 6, 0, S * 7, S))
    W = Image.new('RGBA', (S * 5, S * 2), bg)
    for r, row in enumerate(([f7, f8] + old[1:4], [f7, f8] + new[1:4])):
        for i, f in enumerate(row):
            W.alpha_composite(f, (S * i, S * r))
    W.resize((W.width * 3, W.height * 3), Image.NEAREST).save(os.path.join(D3, 'preview', 'prep_v9_v10_x3.png'))
    first = Image.open(os.path.join(ROOT, '29_attack_greatsword_light_slash_east', '0_east_pauldron_w.png')).convert('RGBA')
    chain = [first.crop((S * k, 0, S * k + S, S)) for k in range(8)] + [s2.crop((S * k, 0, S * k + S, S)) for k in range(1, 8)] + new[1:]
    g = []
    for f in chain:
        c = Image.new('RGBA', (S, S), bg)
        c.alpha_composite(f)
        g.append(c.resize((S * 4, S * 4), Image.NEAREST).convert('RGB'))
    g[0].save(os.path.join(D3, 'preview', 'slash01_02i_03v10_chain_16fps_x4.gif'), save_all=True, append_images=g[1:],
              duration=([60, 70] * 40)[:len(g) - 1] + [600], loop=0, disposal=1)


if __name__ == '__main__':
    main()
