"""32번 3타 v3 → v4: 잔상 넣기(생성 없음, 2026-11-07 사용자: "3번째 벨 때도 잔상이 남게").
1·2타처럼 같은 도구 생성본의 잔상을 되살린다. segA/frame_04(시작→타격 구간 마지막, 큰 초승달; 이전엔 잔상이라 뺐음)에서
초승달만 골라(칼날 회색·밝은 회색·크림색이면서 머리 위 y<56 또는 몸 앞 x≥60) 타격 프레임의 빈 칸에만 얹는다.
  f4 = 승인 타격 + 초승달 전체, f5 = 감속 1 + 초승달 바깥 밝은 테두리(y≥50)만(1·2타의 큰 잔상 → 작은 잔상 순서)
  python attack_slash03_smear.py -> 0_east_v4.png, preview/smear3_v3_v4_x3.png, GIF
"""
import os
from PIL import Image
from attack_slash_east import snap, px

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D = os.path.join(ROOT, '32_attack_greatsword_heavy_slash_03_east')
S = 96
SMEAR = {(0xa5, 0xa8, 0xa7), (0xbb, 0xbe, 0xbe), (0xfd, 0xe6, 0xbd)}
TAIL_Y = 50
RIM = {(0xbb, 0xbe, 0xbe), (0xfd, 0xe6, 0xbd)}   # f5: 초승달 바깥 밝은 테두리만(가로로 자르면 윗면이 납작해 보였음, 2026-11-07)


def main():
    sheet = Image.open(os.path.join(D, '0_east_v3.png')).convert('RGBA')
    src = snap(Image.open(os.path.join(D, 'mid_raw', 'segA', 'frame_04.png')).convert('RGBA')).load()
    arc = {(x, y): src[x, y] for y in range(S) for x in range(S)
           if src[x, y][3] and src[x, y][:3] in SMEAR and (y < 56 or x >= 60)}
    out = sheet.copy()
    for k, part in ((4, arc), (5, {q: c for q, c in arc.items() if q[1] >= TAIL_Y and c[:3] in RIM})):
        f = sheet.crop((S * (k - 1), 0, S * k, S))
        p = f.load()
        n = 0
        for q, c in part.items():
            if not p[q][3]:
                p[q] = c
                n += 1
        out.paste(f, (S * (k - 1), 0))
        b = f.getbbox()
        print(f'f{k}: 잔상 {n}px, 여백 좌상우하 {(b[0], b[1], S - b[2], S - b[3])}, 크기 {b[2] - b[0]}×{b[3] - b[1]}')
    out.save(os.path.join(D, '0_east_v4.png'))
    d = px(out)
    a, b2 = sheet.load(), out.load()
    ch = [(x, y) for y in range(S) for x in range(S * 12) if a[x, y] != b2[x, y]]
    print('바뀐 프레임', sorted({x // S + 1 for x, _ in ch}), '| 색 수', len({c[:3] for c in d if c[3]}), '| 알파', sorted({c[3] for c in d}))
    bg = (58, 62, 72, 255)
    W = Image.new('RGBA', (S * 4, S * 2), bg)
    for r, sh in enumerate((sheet, out)):
        for i, k in enumerate(range(2, 6)):
            W.alpha_composite(sh.crop((S * k, 0, S * k + S, S)), (S * i, S * r))
    W.resize((W.width * 3, W.height * 3), Image.NEAREST).save(os.path.join(D, 'preview', 'smear3_v3_v4_x3.png'))
    first = Image.open(os.path.join(ROOT, '29_attack_greatsword_light_slash_east', '0_east_pauldron_w.png')).convert('RGBA')
    second = Image.open(os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east', '0_east_v2d.png')).convert('RGBA')
    fs = [out.crop((S * k, 0, S * k + S, S)) for k in range(12)]
    chain = [first.crop((S * k, 0, S * k + S, S)) for k in range(8)] + [second.crop((S * k, 0, S * k + S, S)) for k in range(1, 8)] + fs[1:]
    for name, fr in (('slash03_v4_16fps_x4.gif', fs), ('slash01_02_03_chain_v4_16fps_x4.gif', chain)):
        g = []
        for f in fr:
            c = Image.new('RGBA', (S, S), bg)
            c.alpha_composite(f)
            g.append(c.resize((S * 4, S * 4), Image.NEAREST).convert('RGB'))
        g[0].save(os.path.join(D, 'preview', name), save_all=True, append_images=g[1:],
                  duration=([60, 70] * 40)[:len(g) - 1] + [600], loop=0, disposal=1)


if __name__ == '__main__':
    main()
