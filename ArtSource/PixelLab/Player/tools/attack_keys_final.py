# 동쪽 약베기: 16fps 도형 설계(attack_block_anim)의 f3·f4·f8에 원본 외형 적용. 손·칼·어깨·골반·접지는 설계값 그대로
# python attack_keys_final.py guide | finish <take>. 종료 코드 0 = 16색 이하, 알파 0/255, 발이 idle 범위 안
import math, os, sys
import numpy as np
from PIL import Image, ImageDraw
import attack_block_anim as B
import attack_keys_pose as K

OUT = os.path.join(K.ROOT, '28_attack_greatsword_light_01_final')
RAW = os.path.join(K.ROOT, '01_raw/clean/attack_greatsword_light_01_final')
KEYS = {'f3_windup': 2, 'f4_hit': 3, 'f8_finish': 7}
STEEL = [(107, 115, 107), (165, 168, 167), (187, 190, 190), (165, 168, 167), (107, 115, 107)]
GRAY = STEEL[:3] + [(91, 86, 66)]
GOLD, GOLD_D, BLACK = (252, 219, 109), (195, 150, 80), (0, 0, 0)
at = lambda q: (q[0] + K.OX, q[1] + K.OY)


def design(idx):
    f, s = B.FRAMES[idx], B.solve(B.FRAMES[idx])
    pts = {k: B.scr(s[k]) for k in ('hand', 'rear', 'pommel', 'guard', 'tip')}
    a = math.radians(f['sh'])
    cx = 29 + f['cdx']
    k = 8
    near = (cx + k * math.sin(a), 30 + k * math.cos(a) * math.sin(B.CAM))
    far = (cx - k * math.sin(a), 30 - k * math.cos(a) * math.sin(B.CAM))
    # 도형 설계처럼 몸 높이는 그대로, 좌우 이동과 망토 지연만 밑그림에 준다
    warp = dict(hip=(f['hdx'], 0), chest=(f['cdx'], 0), head=(f['cdx'], 0), cape=f['cape'] + 10)
    return dict(f=f, s=s, pts=pts, near=near, far=far, warp=warp, is_far=s['d'][1] > 0.1)


def draw_blade(im, g, t):
    # 곧은 칼: 긴 축으로 한 칸씩 나아가며 같은 폭을 찍는다. 길이는 설계 투영값
    px = im.load()
    x0, y0 = at(g)
    dx, dy = t[0] - g[0], t[1] - g[1]
    L = math.hypot(dx, dy)
    c, s_ = dx / L, dy / L
    major = max(abs(c), abs(s_))
    width = round(5.7 / major)
    steps = round(L * major)
    for i in range(steps):
        cx, cy = x0 + c * i / major, y0 + s_ * i / major
        w = width - 2 if i >= steps - 2 else width
        for k in range(w):
            off = k - (w - 1) / 2
            x, y = (math.floor(cx + .5), math.floor(cy - (w - 1) / 2 + .5) + k) if abs(c) >= abs(s_) else (math.floor(cx - (w - 1) / 2 + .5) + k, math.floor(cy + .5))  # round()는 .5에서 칸이 어긋남
            if 0 <= x < K.W and 0 <= y < K.H:
                px[x, y] = STEEL[min(4, int((off + width / 2) * 5 / width))] + (255,)


def draw_grip(im, D):
    d = ImageDraw.Draw(im)
    p = D['pts']
    v = np.array(p['guard']) - np.array(p['pommel'])
    v = v / max(1e-6, np.linalg.norm(v))
    n = np.array([-v[1], v[0]])
    d.line([at(p['pommel']), at(p['guard'])], fill=(54, 46, 37, 255), width=2)
    d.line([at(p['guard'] + n * 3), at(p['guard'] - n * 3)], fill=GOLD_D + (255,), width=2)
    for q in (p['rear'], p['hand']):
        x, y = round(q[0] + K.OX), round(q[1] + K.OY)
        d.rectangle([x - 2, y - 1, x + 2, y + 1], fill=BLACK + (255,))
        d.rectangle([x - 1, y - 2, x + 1, y + 2], fill=BLACK + (255,))
        d.rectangle([x - 1, y - 1, x + 1, y + 1], fill=GOLD_D + (255,))
        d.line([(x - 1, y - 1), (x + 1, y - 1)], fill=GOLD + (255,))


def turned_body(D):
    # 회전 밑그림: 어깨 회전만큼 가슴 줄을 가로로 넓히고(옆모습 -> 3/4), 아래로 갈수록 골반 회전으로 줄인다
    src = np.array(K.warped_idle(D['warp']))
    out = src.copy()
    f = D['f']
    for y in range(26, 47):
        u = (y - 26) / 20
        yaw = abs(f['sh']) * (1 - u) + abs(f['hip']) * u
        k = 1 + 0.5 * math.sin(math.radians(yaw))
        xs = np.where(src[y, :, 3] > 0)[0]
        if len(xs) < 2:
            continue
        cx = 29 + f['cdx'] * (1 - u) + f['hdx'] * u
        row = np.zeros_like(src[y])
        for x in range(64):
            sx = int(round(cx + (x - cx) / k))
            if 0 <= sx < 64:
                row[x] = src[y, sx]
        out[y] = row
    return Image.fromarray(out)


def guide(idx):
    D = design(idx)
    im = Image.new('RGBA', (K.W, K.H), (0, 0, 0, 0))
    if D['is_far']:
        draw_blade(im, D['pts']['guard'], D['pts']['tip'])
    im.alpha_composite(turned_body(D), (K.OX, K.OY))
    d = ImageDraw.Draw(im)
    def arm(sh, h, col, w, sign):
        # 납작한 띠로 남지 않도록 어두운 갑옷 + 어깨·팔꿈치·손목의 금색 마디만 표시한다
        el = ((sh[0] + h[0]) / 2 + sign * 1.5, (sh[1] + h[1]) / 2 + 3.5)
        d.line([at(sh), at(el), at(h)], fill=BLACK + (255,), width=w + 2)
        d.line([at(sh), at(el), at(h)], fill=(54, 46, 37, 255), width=w)
        for q in (sh, el, ((el[0] + h[0]) / 2, (el[1] + h[1]) / 2)):
            x, y = at(q)
            d.rectangle([x - 1, y - 1, x + 1, y + 1], fill=col + (255,))
    arm(D['far'], D['pts']['rear'], GOLD_D, 3, 1)
    if not D['is_far']:
        draw_blade(im, D['pts']['guard'], D['pts']['tip'])
    arm(D['near'], D['pts']['hand'], GOLD, 4, -1)
    draw_grip(im, D)
    return im


def body_only(idx, src):
    D = design(idx)
    g = K.snap(np.array(Image.open(src).convert('RGBA')))
    a = np.zeros((K.H, K.W, 4), g.dtype)
    a[:g.shape[0]] = g
    # 생성본의 칼을 지운다: 설계 칼 선 주변의 회색
    hx, hy = at(D['pts']['hand'])
    t = np.array(D['pts']['tip']) - np.array(D['pts']['hand'])
    L = float(np.linalg.norm(t))
    c, s_ = t / L
    for y in range(K.H):
        for x in range(K.W):
            if not a[y, x, 3] or tuple(a[y, x, :3]) not in GRAY:
                continue
            u = (x - hx) * c + (y - hy) * s_
            w = abs(-(x - hx) * s_ + (y - hy) * c)
            if u > 3 and w < 13:
                a[y, x] = 0
    K.keep_largest(a)
    K.plant_feet(a)
    return Image.fromarray(a).copy()


def compose(idx, body):
    # 몸 그림 위에 설계 위치의 칼·손을 놓는다(칼이 안쪽이면 몸 뒤로)
    D = design(idx)
    im = Image.new('RGBA', (K.W, K.H), (0, 0, 0, 0))
    if D['is_far']:
        draw_blade(im, D['pts']['guard'], D['pts']['tip'])
        im.alpha_composite(body)
    else:
        im = body
        draw_blade(im, D['pts']['guard'], D['pts']['tip'])
    draw_grip(im, D)
    return im


def assemble(idx, src):
    return compose(idx, body_only(idx, src))


def main():
    os.makedirs(os.path.join(OUT, 'guide'), exist_ok=True)
    os.makedirs(os.path.join(OUT, 'preview'), exist_ok=True)
    if sys.argv[1] == 'guide':
        for k, i in KEYS.items():
            guide(i).save(os.path.join(OUT, 'guide', k + '.png'))
        fs = [Image.open(os.path.join(OUT, 'guide', k + '.png')) for k in KEYS]
    else:
        fs = [assemble(i, os.path.join(RAW, sys.argv[2], k + '.png')) for k, i in KEYS.items()]
        for k, f in zip(KEYS, fs):
            f.save(os.path.join(OUT, k + '.png'))
    idle = K.idle_canvas()
    x0, y0, cw, ch = 8, 16, 112, 92
    s = 4
    row = Image.new('RGBA', ((cw * s + 10) * 4 + 10, ch * s + 20), (58, 60, 70, 255))
    d = ImageDraw.Draw(row)
    for n, f in enumerate([idle] + fs):
        row.alpha_composite(f.crop((x0, y0, x0 + cw, y0 + ch)).resize((cw * s, ch * s), 0), (10 + n * (cw * s + 10), 10))
    fy = 10 + (K.OY + 55 - y0) * s
    d.line([(0, fy), (row.width, fy)], fill=(80, 170, 255, 255))
    row.save(os.path.join(OUT, 'preview', ('guides' if sys.argv[1] == 'guide' else 'keys') + '_x4.png'))
    if sys.argv[1] == 'guide':
        return 0
    return K.report(list(KEYS), fs)


if __name__ == '__main__':
    sys.exit(main())
