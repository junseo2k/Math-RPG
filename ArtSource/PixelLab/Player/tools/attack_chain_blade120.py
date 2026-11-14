"""2타 f4~f8과 3타 f1~f3의 칼을 120°(화면 왼쪽 위)로 다시 그림(생성 없음, 2026-11-07 사용자:
"검이 이어지지 않는다, 2타 끝났을 때 각도가 120°여야 한다" → 범위 확인: 2타 f4~f8 + 3타 f1~f3).
프레임마다 머리 위(y<60) 칼날색 픽셀에서 칼 뿌리·칼끝을 찾고, 기존 칼날·코등이를 지운 뒤 뿌리에서 120°로
길이 30·폭 6 칼날과 수직 코등이를 그린다. 손·몸·머리는 그대로(빈 칸·옛 칼 자리에만 칠함).
2타 f4의 작은 호 잔상은 칼이 돈 각도만큼 뿌리를 중심으로 같이 돌린다.
  python attack_chain_blade120.py -> 31_.../0_east_v2g.png, 32_.../0_east_v5.png, preview/*blade120*, 이어 보기 GIF
"""
import math
import os
from PIL import Image
from attack_slash_east import px
from attack_slash02_fararm import PAL

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D2 = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
D3 = os.path.join(ROOT, '32_attack_greatsword_heavy_slash_03_east')
S = 96
ANG, LENGTH = 120, 30
CROSS = {-3: '7', -2: 'a', -1: 'a', 0: 'a', 1: 'c', 2: '7'}
BLADE = {PAL[c][:3] for c in '7ac'}
GOLD = {PAL[c][:3] for c in 'b9'}
TOP = 60


def blade_ends(p):
    bl = [(x, y) for y in range(TOP) for x in range(S) if p[x, y][3] and p[x, y][:3] in BLADE]
    tip = max(bl, key=lambda q: q[0] - q[1])                                   # 오른쪽 위 끝
    root = max(bl, key=lambda q: (q[0] - tip[0]) ** 2 + (q[1] - tip[1]) ** 2)  # 반대 끝(코등이 쪽)
    return root, tip


def redraw(f, arc=None):
    """f를 그 자리에서 고친다. 반환: (뿌리, 옛 각도, 지운 칼날 수, 그린 칼날 수)."""
    p = f.load()
    root, tip = blade_ends(p)
    ox, oy = tip[0] - root[0], tip[1] - root[1]
    L = math.hypot(ox, oy)
    old_ang = math.degrees(math.atan2(-oy, ox))
    ux0, uy0 = ox / L, oy / L
    erased = 0
    for y in range(TOP):
        for x in range(S):
            t = ((x - root[0]) * ox + (y - root[1]) * oy) / (L * L)
            d = abs((x - root[0]) * oy - (y - root[1]) * ox) / L
            c = p[x, y]
            if not c[3]:
                continue
            if (-0.02 <= t <= 1.1 and d <= 3.6 and c[:3] in BLADE) or \
               (math.hypot(x - (root[0] - ux0 * 1.5), y - (root[1] - uy0 * 1.5)) <= 4 and c[:3] in GOLD and abs(t * L) <= 3):
                p[x, y] = (0, 0, 0, 0)          # 옛 칼날, 옛 코등이(뿌리 바로 아래 금색)
                erased += 1
    ux, uy = math.cos(math.radians(ANG)), -math.sin(math.radians(ANG))
    nx, ny = -uy, ux
    if nx - ny < 0:                         # +쪽이 화면 오른쪽 위(광원 쪽)
        nx, ny = -nx, -ny
    G = (root[0] - ux * 1, root[1] - uy * 1)  # 코등이 중심 = 뿌리에서 칼 반대쪽 1px
    drawn = 0
    for i in range(2, LENGTH * 2 + 1):
        t = i / 2
        half = 3 if t < LENGTH - 3 else max(1, round(3 * (LENGTH - t) / 3))
        for w, c in CROSS.items():
            if not (-half <= w < half):
                continue
            q = (round(G[0] + ux * t + nx * w), round(G[1] + uy * t + ny * w))
            if 0 <= q[0] < S and 0 <= q[1] < S and not p[q][3]:
                p[q] = PAL[c]
                drawn += 1
    for t in range(-3, 4):
        q = (round(G[0] + nx * t), round(G[1] + ny * t))
        p[q] = PAL['9' if abs(t) == 3 else 'b']
    # 옛 칼날 가장자리(어두운 7 등)가 띠 밖에 남은 것 정리(2026-11-07 1차에서 가는 선이 남았음):
    # 뿌리보다 위·오른쪽의 칼날색 중 새 칼 띠(폭 ±3.6) 밖인 것
    for y in range(min(TOP, root[1] + 2)):
        for x in range(root[0] - 1, S):
            c = p[x, y]
            if not c[3] or c[:3] not in BLADE:
                continue
            t = (x - G[0]) * ux + (y - G[1]) * uy
            d = abs((x - G[0]) * nx + (y - G[1]) * ny)
            if not (0 <= t <= LENGTH + 1 and d <= 3.6):
                p[x, y] = (0, 0, 0, 0)
                erased += 1
    if arc:                                 # 작은 호 잔상: 뿌리 중심으로 (120 − 옛 각도)만큼 회전
        rot = math.radians(ANG - old_ang)
        for (x, y), c in arc.items():
            p[x, y] = (0, 0, 0, 0)
        for (x, y), c in arc.items():
            dx, dy = x - root[0], -(y - root[1])
            rx, ry = dx * math.cos(rot) - dy * math.sin(rot), dx * math.sin(rot) + dy * math.cos(rot)
            q = (round(root[0] + rx), round(root[1] - ry))
            if 0 <= q[0] < S and 0 <= q[1] < S and not p[q][3]:
                p[q] = c
    return root, old_ang, erased, drawn


def arc_of(f):
    """2타 f4의 몸과 떨어진 작은 호 조각(가장 큰 덩어리가 아닌 작은 덩어리)."""
    p = f.load()
    seen, comps = set(), []
    for y in range(S):
        for x in range(S):
            if p[x, y][3] and (x, y) not in seen:
                comp, todo = [], [(x, y)]
                seen.add((x, y))
                while todo:
                    a, b = todo.pop()
                    comp.append((a, b))
                    for dx in (-1, 0, 1):
                        for dy in (-1, 0, 1):
                            q = (a + dx, b + dy)
                            if 0 <= q[0] < S and 0 <= q[1] < S and q not in seen and p[q][3]:
                                seen.add(q)
                                todo.append(q)
                comps.append(comp)
    comps.sort(key=len, reverse=True)
    return {q: p[q] for comp in comps[1:] if len(comp) <= 60 for q in comp}


def run(src, dst, ks, arc_k=None):
    sheet = Image.open(src).convert('RGBA')
    out = sheet.copy()
    for k in ks:
        f = sheet.crop((S * (k - 1), 0, S * k, S))
        arc = arc_of(f) if k == arc_k else None
        if arc:
            for q in arc:                    # 호 조각은 칼 찾기에서 빼기 위해 잠시 지움
                f.putpixel(q, (0, 0, 0, 0))
        root, old, er, dr = redraw(f, arc)
        out.paste(f, (S * (k - 1), 0))
        b = f.getbbox()
        print(f'{os.path.basename(dst)} f{k}: 뿌리 {root}, 옛 각도 {old:.0f}° → {ANG}°, 지움 {er}, 칼날 {dr}px'
              + (f', 호 {len(arc)}px 회전' if arc else '') + f', 여백 좌상우하 {(b[0], b[1], S - b[2], S - b[3])}')
    out.save(dst)
    d = px(out)
    print('  색 수', len({c[:3] for c in d if c[3]}), '| 알파', sorted({c[3] for c in d}))
    return sheet, out


def main():
    s2, o2 = run(os.path.join(D2, '0_east_v2f.png'), os.path.join(D2, '0_east_v2g.png'), range(4, 9), arc_k=4)
    s3, o3 = run(os.path.join(D3, '0_east_v4.png'), os.path.join(D3, '0_east_v5.png'), range(1, 4))
    same = o2.crop((S * 7, 0, S * 8, S)).tobytes() == o3.crop((0, 0, S, S)).tobytes()
    print('2타 f8 == 3타 f1:', same)
    bg = (58, 62, 72, 255)
    W = Image.new('RGBA', (S * 8, S * 2), bg)
    for r, sh in enumerate((s2, o2)):
        for i, k in enumerate(range(2, 8)):
            W.alpha_composite(sh.crop((S * k, 0, S * k + S, S)), (S * i, S * r))
    for r, sh in enumerate((s3, o3)):
        for i, k in enumerate((1, 2)):
            W.alpha_composite(sh.crop((S * k, 0, S * k + S, S)), (S * (6 + i), S * r))
    W.resize((W.width * 2, W.height * 2), Image.NEAREST).save(os.path.join(D3, 'preview', 'blade120_before_after_x2.png'))
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
