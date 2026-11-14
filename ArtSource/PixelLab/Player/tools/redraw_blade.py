"""검날만 idle 검 규격으로 다시 그리기 (2026-11-13 사용자 승인: "로컬로 다시 그리기").

손잡이(코등이) 위치와 검 각도는 프레임의 현재 검에서 재고, 옛 검날 픽셀을 지운 뒤
같은 축에 곧은 3단 검날(위 L·가운데 S·아래 G, 끝은 짧게 뾰족)을 투명 칸에만 그린다.
손·몸·망토·손잡이는 건드리지 않는다.

python redraw_blade.py <in.png> <out.png> [--length 31] [--thick 4.7] [--guard x y --angle deg]
"""
import argparse, math
import numpy as np
from PIL import Image

L_ = (0xbb, 0xbe, 0xbe); S_ = (0xa5, 0xa8, 0xa7); G_ = (0x6b, 0x73, 0x6b)
GOLD = {(0xd2, 0xa8, 0x55), (0xfc, 0xdb, 0x6d), (0xae, 0x78, 0x3a), (0xc3, 0x96, 0x50), (0x91, 0x51, 0x1b)}


def blade_component(a):
    c = a[..., :3].astype(int)
    g = (a[..., 3] > 0) & (c.max(-1) - c.min(-1) < 40) & (c.min(-1) > 60)
    seen = np.zeros(g.shape, bool); best = []
    for y, x in zip(*np.nonzero(g)):
        if seen[y, x]:
            continue
        st, comp = [(y, x)], []; seen[y, x] = True
        while st:
            p = st.pop(); comp.append(p)
            for dy in (-1, 0, 1):
                for dx in (-1, 0, 1):
                    q = (p[0] + dy, p[1] + dx)
                    if 0 <= q[0] < g.shape[0] and 0 <= q[1] < g.shape[1] and g[q] and not seen[q]:
                        seen[q] = True; st.append(q)
        if len(comp) > len(best):
            best = comp
    return best


def redraw(a, length=31.0, thick=4.7, guard_xy=None, angle=None):
    comp = blade_component(a)
    if guard_xy is not None:   # 칼날이 빠진 프레임(2026-11-13 run f2·f6): 앞뒤 프레임에서 정한 손잡이·각도
        guard = np.array(guard_xy, float); r = math.radians(angle)
        u = np.array([math.cos(r), -math.sin(r)]); tip = guard + u * length
        return _draw(a, comp, guard, u, tip, length, thick)
    P = np.array([(x, y) for y, x in comp], float)
    m = P.mean(0); v = np.linalg.svd(P - m)[2][0]; t = (P - m) @ v
    e1, e2 = m + v * t.min(), m + v * t.max()
    gold = np.array([(x, y) for y in range(a.shape[0]) for x in range(a.shape[1])
                     if a[y, x, 3] and tuple(a[y, x, :3]) in GOLD], float)
    d1 = np.min(np.hypot(*(gold - e1).T)); d2 = np.min(np.hypot(*(gold - e2).T))
    guard, tip = (e1, e2) if d1 < d2 else (e2, e1)
    u = (tip - guard) / np.linalg.norm(tip - guard)
    return _draw(a, comp, guard, u, tip, length, thick)


def _draw(a, comp, guard, u, tip, length, thick):
    n = np.array([u[1], -u[0]])
    if n[1] > 0:           # n = 화면 위쪽(빛 받는 쪽)
        n = -n
    o = a.copy()
    for y, x in comp:      # 옛 검날 지우기
        if (np.array([x, y]) - guard) @ u > -0.5:
            o[y, x] = 0
    hw = thick / 2; taper = 4.0
    for y in range(a.shape[0]):
        for x in range(a.shape[1]):
            p = np.array([x, y], float) - guard
            tt, dd = p @ u, p @ n
            if tt < 0 or tt > length:
                continue
            w = hw if tt < length - taper else hw * (length - tt) / taper + 0.5
            if abs(dd) > w or o[y, x, 3]:
                continue
            f = (dd + hw) / (2 * hw)   # 0 = 아래, 1 = 위
            col = L_ if f > 0.7 else S_ if f > 0.4 else G_
            o[y, x, :3] = col; o[y, x, 3] = 255
    # 떨어진 1px 점 제거
    al = o[..., 3] > 0
    pad = np.pad(al, 1)
    nb = sum(np.roll(np.roll(pad, dy, 0), dx, 1)[1:-1, 1:-1] for dy in (-1, 0, 1) for dx in (-1, 0, 1)) - al
    o[al & (nb == 0)] = 0
    ang = math.degrees(math.atan2(-u[1], u[0])) % 360
    return o, dict(guard=guard.round(1).tolist(), angle=round(ang, 1), old_len=round(float(np.linalg.norm(tip - guard)), 1),
                   isolated_removed=int((al & (nb == 0)).sum()))


if __name__ == '__main__':
    ap = argparse.ArgumentParser(); ap.add_argument('src'); ap.add_argument('dst')
    ap.add_argument('--length', type=float, default=31.0); ap.add_argument('--thick', type=float, default=4.7)
    ap.add_argument('--guard', type=float, nargs=2); ap.add_argument('--angle', type=float)
    A = ap.parse_args()
    a = np.array(Image.open(A.src).convert('RGBA'))
    o, info = redraw(a, A.length, A.thick, A.guard, A.angle)
    Image.fromarray(o).save(A.dst); print(info)
