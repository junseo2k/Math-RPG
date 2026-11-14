"""NE 1타 검 잔상 (2026-11-13 사용자 "로컬로 그리기").

동쪽 1타 잔상 구조(f4·f5 초승달, 안쪽 S a5a8a7, 휘두르는 쪽 바깥 가장자리 w fde6bd, 안쪽 테두리 G 6b736b)를
NE 검 궤적에 맞춰 별도 투명 레이어로 그린다. 몸 시트는 건드리지 않는다.
  f4: f4 손잡이를 중심으로 f3 검 각도 → f4 검 각도 사이를 쓸고 간 영역
  f5: f4 검끝 → f5 검끝, 몸 앞을 가로지르는 수평 호(탑다운 타원, 세로 0.5). 캐릭터 너머라 몸 뒤에 둔다.

python ne_hit1_trail.py  → 02_gen/ne_hit1/trail_v1/
"""
import math
from pathlib import Path
import numpy as np
from PIL import Image

D = Path(__file__).resolve().parent.parent / '35_attack_greatsword_combo_6dir/02_gen/ne_hit1'
SRC, OUT = D / 'candidate_v11', D / 'trail_v1'
C = 104
S_ = (0xa5, 0xa8, 0xa7); W_ = (0xfd, 0xe6, 0xbd); G_ = (0x6b, 0x73, 0x6b)


def blade_axis(a):
    import sys; sys.path.insert(0, str(Path(__file__).parent))
    from redraw_blade import blade_component, GOLD
    comp = blade_component(a)
    P = np.array([(x, y) for y, x in comp], float)
    m = P.mean(0); v = np.linalg.svd(P - m)[2][0]; t = (P - m) @ v
    e1, e2 = m + v * t.min(), m + v * t.max()
    gold = np.array([(x, y) for y in range(C) for x in range(C) if a[y, x, 3] and tuple(a[y, x, :3]) in GOLD], float)
    d1 = np.min(np.hypot(*(gold - e1).T)); d2 = np.min(np.hypot(*(gold - e2).T))
    g, tip = (e1, e2) if d1 < d2 else (e2, e1)
    return g, tip, math.degrees(math.atan2(-(tip[1] - g[1]), tip[0] - g[0])) % 360


def paint(layer, x, y, col):
    if 0 <= x < C and 0 <= y < C:
        layer[y, x, :3] = col; layer[y, x, 3] = 255


def trail_f4(g, a_from, a_to, L):
    """g 중심, 각도 a_from → a_to(시계 방향) 부채꼴 초승달. 앞쪽(a_to)일수록 두꺼움."""
    lay = np.zeros((C, C, 4), np.uint8)
    span = (a_from - a_to) % 360
    for y in range(C):
        for x in range(C):
            dx, dy = x - g[0], g[1] - y
            r = math.hypot(dx, dy)
            ang = math.degrees(math.atan2(dy, dx)) % 360
            s = ((a_from - ang) % 360) / span          # 0 = 시작(꼬리), 1 = 지금 검
            if not (0 <= s <= 1) or r > L + 1:
                continue
            th = 0.55 * L * s                            # 두께
            if r < L + 1 - th:
                continue
            k = (r - (L + 1 - th)) / max(th, 1e-6)        # 0 안쪽 → 1 바깥
            col = G_ if k < 0.15 else W_ if (k > 0.7 and s > 0.4) else S_
            paint(lay, x, y, col)
    return lay


def trail_f5(c, rx, ry, phi_from, phi_to):
    """타원 호(φ: 오른쪽 0°, 위 90°). 앞쪽(phi_to)일수록 두꺼움."""
    lay = np.zeros((C, C, 4), np.uint8)
    for y in range(C):
        for x in range(C):
            qx, qy = (x - c[0]) / rx, (c[1] - y) / ry
            rho = math.hypot(qx, qy); phi = math.degrees(math.atan2(qy, qx))
            if phi < 0:
                phi += 360
            s = (phi - phi_from) / (phi_to - phi_from)
            if not (0 <= s <= 1):
                continue
            th = 0.32 * s
            if rho > 1.0 or rho < 1.0 - th:
                continue
            k = (rho - (1.0 - th)) / max(th, 1e-6)
            col = G_ if k < 0.15 else W_ if (k > 0.65 and s > 0.35) else S_
            paint(lay, x, y, col)
    return lay


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    fr = [np.array(Image.open(SRC / f'f{j}.png').convert('RGBA')) for j in range(1, 9)]
    g3, t3, a3 = blade_axis(fr[2]); g4, t4, a4 = blade_axis(fr[3]); g5, t5, a5 = blade_axis(fr[4])
    L = 31.0
    layers = [np.zeros((C, C, 4), np.uint8) for _ in range(8)]
    layers[3] = trail_f4(g4, a3, a4, L)
    # f5 타원: 두 검끝을 지나는 수평 타원(가로 반지름 = 검끝 사이 절반, 세로 = 0.5배)
    cx = (t4[0] + t5[0]) / 2; cy = (t4[1] + t5[1]) / 2; rx = (t4[0] - t5[0]) / 2 + 1; ry = rx * 0.5
    layers[4] = trail_f5((cx, cy), rx, ry, 0.0, 180.0)
    info = dict(f3_angle=round(a3, 1), f4=dict(grip=g4.round(1).tolist(), angle=round(a4, 1)),
                f5_ellipse=dict(center=[round(cx, 1), round(cy, 1)], rx=round(rx, 1), ry=round(ry, 1)))
    sheet_t = Image.new('RGBA', (C * 8, C)); sheet_c = Image.new('RGBA', (C * 8, C))
    for j, (lay, f) in enumerate(zip(layers, fr)):
        Image.fromarray(lay).save(OUT / f'trail_f{j + 1}.png')
        sheet_t.alpha_composite(Image.fromarray(lay), (j * C, 0))
        comp = Image.fromarray(lay.copy()); comp.alpha_composite(Image.fromarray(f))   # 잔상은 몸 뒤
        comp.save(OUT / f'preview_f{j + 1}.png'); sheet_c.alpha_composite(comp, (j * C, 0))
    sheet_t.save(OUT / 'ne_hit1_trail_8f_832x104.png'); sheet_c.save(OUT / 'ne_hit1_with_trail_preview_832x104.png')
    E = Image.open(D.parent.parent.parent / '33_attack_greatsword_combo_east/1_light_slash_east.png').convert('RGBA')
    gif = []
    for j in range(8):
        cc = Image.new('RGBA', (C * 2, C), (90, 110, 90, 255)); e = Image.new('RGBA', (C, C))
        e.alpha_composite(E.crop((j * 96, 0, j * 96 + 96, 96)), (4, 1)); cc.alpha_composite(e, (0, 0))
        cc.alpha_composite(sheet_c.crop((j * C, 0, j * C + C, C)), (C, 0))
        gif.append(cc.resize((C * 8, C * 4), Image.NEAREST).convert('P', palette=Image.ADAPTIVE))
    gif[0].save(OUT / 'east_vs_ne_hit1_trail_16fps_x4.gif', save_all=True, append_images=gif[1:], duration=62, loop=0)
    print(info)


if __name__ == '__main__':
    main()
