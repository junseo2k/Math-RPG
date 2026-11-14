"""31번 2차 시트 칼날만 보정(생성 없음, 2026-11-07 사용자: "칼이 약간 날이 빠진 것처럼 보인다, 이것만 수정").
측정: f4~f6 칼날 단면 5~6px로 매끈, f7은 3~4px·가장자리 들쭉날쭉(118px), f8은 칼끝 쪽 계단 모양.
f5 칼날(각도 40°, 167px, 단면 6px)을 f7·f8의 칼 뿌리에 맞춰 평행 이동해 넣는다. 칼날은 손·코등이 뒤(빈 칸·원래 칼날 자리에만 칠함).
  python attack_slash02_blade.py -> 0_east_v2b.png (0_east_v2.png는 그대로), preview/blade_v2_v2b_x6.png
"""
import os
from PIL import Image
from attack_slash_east import is_blade, px

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
S, TOP = 96, 56            # 칼날은 머리 위(y<56)에만 있음
SRC_F, FIX = 5, (7, 8)     # 기준 칼날 프레임, 고칠 프레임(1부터)


def blade(f):
    p = f.load()
    bl = {(x, y): p[x, y] for y in range(TOP) for x in range(S) if is_blade(p[x, y])}
    tip = max(bl, key=lambda q: q[0] - q[1])                                   # 오른쪽 위 끝
    root = max(bl, key=lambda q: (q[0] - tip[0]) ** 2 + (q[1] - tip[1]) ** 2)  # 반대 끝
    return bl, root


def main():
    sheet = Image.open(os.path.join(D, '0_east_v2.png')).convert('RGBA')
    frames = [sheet.crop((S * k, 0, S * k + S, S)) for k in range(8)]
    src_bl, src_root = blade(frames[SRC_F - 1])
    out = sheet.copy()
    for k in FIX:
        f = frames[k - 1].copy()
        p = f.load()
        bl, root = blade(f)
        for q in bl:
            p[q] = (0, 0, 0, 0)
        dx, dy = root[0] - src_root[0], root[1] - src_root[1]
        n = 0
        for (x, y), c in src_bl.items():
            q = (x + dx, y + dy)
            if 0 <= q[0] < S and 0 <= q[1] < S and (p[q][3] == 0 or q in bl):
                p[q] = c
                n += 1
        out.paste(f, (S * (k - 1), 0))
        print(f'f{k}: 원래 칼날 {len(bl)}px → f{SRC_F} 칼날 {n}px, 이동 {(dx, dy)}, 뿌리 {root}')
    out.save(os.path.join(D, '0_east_v2b.png'))
    a, b = sheet.load(), out.load()
    ch = [(x, y) for y in range(S) for x in range(S * 8) if a[x, y] != b[x, y]]
    d = px(out)
    print('바뀐 픽셀', len(ch), '| 바뀐 프레임', sorted({x // S + 1 for x, _ in ch}), '| y 최대', max(y for _, y in ch),
          '| 색 수', len({c[:3] for c in d if c[3]}), '| 알파', sorted({c[3] for c in d}))
    bg = (58, 62, 72, 255)
    W = Image.new('RGBA', (40 * 5, 64), bg)
    for r, sh in enumerate((sheet, out)):
        for i, k in enumerate(range(3, 8)):
            W.alpha_composite(sh.crop((S * k + 44, 24, S * k + 84, 56)), (40 * i, 32 * r))
    W.resize((W.width * 6, W.height * 6), Image.NEAREST).save(os.path.join(D, 'preview', 'blade_v2_v2b_x6.png'))


if __name__ == '__main__':
    main()
