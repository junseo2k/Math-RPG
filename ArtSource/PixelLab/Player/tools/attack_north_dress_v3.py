"""North greatsword combo, dressed with the approved stick v3 (cape separated from the body, 2026-11-08). No AI generation.

Layers:
  legs/torso/head = body_under_cape/ (hand-drawn back view, head 1 px left), moved by the stick body pose;
                    legs are warped per leg between the moving hip and the fixed foot (knees push out when bent)
  feet            = idle feet (balls fixed, heels lift/rotate) - attack_north_dress_body.move_feet
  arms            = drawn from the stick (shoulder -> elbow -> hand), dark sleeve + gold elbow
  cape            = idle cape (with medal) texture-mapped from the idle cape grid onto the stick v3 cape grid
                    (5 columns x top/mid/hem), dark lining band where the hem is lifted
  sword/hands     = redrawn from the stick (attack_north_dress_trial)
Hit1 f1 and hit3 f10 = north idle pixels (game idle stays seamless).
Out: 34_attack_greatsword_combo_north/dress/v3_cape/ ; exit 0 = checks passed.
"""
import json, math, os, sys
import numpy as np
from PIL import Image, ImageDraw
import attack_north_dress_trial as T
import attack_north_dress_body as DB
import attack_north_slash01_stick as m
import attack_north_combo_body_stick as v2
import attack_north_combo_cape_stick as v3

OUT = os.path.join(T.NORTH, 'dress', 'v3_cape')
BODY = os.path.join(T.NORTH, 'body_under_cape')
V2 = os.path.join(T.NORTH, 'dress', 'v2_body')
NAMES = DB.NAMES
LINING = (64, 11, 24)
SLEEVE, SLEEVE_HI = (0, 0, 0), (54, 46, 37)


def load(n):
    return np.array(Image.open(os.path.join(BODY, n + '.png')).convert('RGBA'))


def to96(a64):
    can = np.zeros((160, 160, 4), np.uint8)
    can[T.OFF[1]:T.OFF[1] + 64, T.OFF[0]:T.OFF[0] + 64] = a64
    return can[:96, :96].copy()


def shift(a, dx, dy):
    out = np.zeros_like(a); dx, dy = int(round(dx)), int(round(dy))
    ys, xs = np.where(a[..., 3] > 0)
    ny, nx = ys + dy, xs + dx
    ok = (ny >= 0) & (ny < a.shape[0]) & (nx >= 0) & (nx < a.shape[1])
    out[ny[ok], nx[ok]] = a[ys[ok], xs[ok]]
    return out


def warp_legs(legs96, pose0, pose, bend):
    """Each leg: rows from the hip (moves with the body) to the ankle (fixed); knees bow outward with the bend."""
    out = np.zeros_like(legs96)
    hy0, hy = v3.HIP_Y, v3.HIP_Y + bend
    ank = 90.0
    dxh = pose['hip_c'][0] - pose0['hip_c'][0]
    for side, xs in ((-1, slice(0, 45)), (1, slice(45, 96))):
        src = np.zeros_like(legs96); src[:, xs] = legs96[:, xs]
        for yd in range(int(math.floor(hy)), int(ank) + 1):
            t = (yd - hy) / max(ank - hy, 1)
            ys = int(round(hy0 + t * (ank - hy0)))
            if not (0 <= ys < 96): continue
            dx = int(round(dxh * (1 - t) + side * 0.6 * max(bend, 0) * math.sin(math.pi * t)))
            row = src[ys]; new = np.zeros_like(row)
            if dx >= 0: new[dx:] = row[:96 - dx]
            else: new[:dx] = row[-dx:]
            msk = new[:, 3] > 0; out[yd][msk] = new[msk]
    return out


def grid(c, pose, b):
    """Cape control grid: 5 columns, each (top, mid, hem). Same construction as v3.cape_shape."""
    poly, hem, (tl, tr) = v3.cape_shape(c, pose, b)
    mid_r, mid_l = poly[2], poly[-1]
    cols = []
    for i in range(5):
        t = i / 4
        top = (tl[0] + (tr[0] - tl[0]) * t, tl[1] + (tr[1] - tl[1]) * t)
        if i == 0: mid = mid_l
        elif i == 4: mid = mid_r
        else: mid = ((top[0] + hem[i][0]) / 2, (top[1] + hem[i][1]) / 2)
        cols.append((top, mid, hem[i]))
    return cols


def gpoint(cols, u, v):
    """u: 0..1 across (extrapolates), v: 0..1 top->hem through mid (extrapolates)."""
    f = u * 4; i = min(max(int(math.floor(f)), 0), 3); a = f - i
    def colpt(cc, v):
        top, mid, hem = cc
        if v <= 0.5:
            s = v / 0.5; return (top[0] + (mid[0] - top[0]) * s, top[1] + (mid[1] - top[1]) * s)
        s = (v - 0.5) / 0.5; return (mid[0] + (hem[0] - mid[0]) * s, mid[1] + (hem[1] - mid[1]) * s)
    p0, p1 = colpt(cols[i], v), colpt(cols[i + 1], v)
    return (p0[0] + (p1[0] - p0[0]) * a, p0[1] + (p1[1] - p0[1]) * a)


def warp_cape(cape96, g0, g1, lift_hem):
    out = np.zeros_like(cape96)
    N = 360
    for vi in range(-40, N + 30):
        v = vi / N
        for ui in range(-60, N + 60):
            u = ui / N
            sx, sy = gpoint(g0, u, v)
            px, py = int(sx), int(sy)
            if not (0 <= px < 96 and 0 <= py < 96) or cape96[py, px, 3] == 0: continue
            dx, dy = gpoint(g1, u, v)
            qx, qy = int(round(dx - 0.5)), int(round(dy - 0.5))
            if 0 <= qx < 96 and 0 <= qy < 96:
                out[qy, qx] = cape96[py, px]
    # lining: underside shows along the lifted part of the hem
    for (p0, l0), (p1, l1) in zip(zip(g1, lift_hem), zip(g1[1:], lift_hem[1:])):
        w = int(min(max(l0, l1), 3))
        if w < 2: continue
        h0, h1 = p0[2], p1[2]
        for s in np.linspace(0, 1, 24):
            x = h0[0] + (h1[0] - h0[0]) * s; y = h0[1] + (h1[1] - h0[1]) * s
            for k in range(w):
                xi, yi = int(round(x - 0.5)), int(round(y - 0.5 - k))
                if 0 <= xi < 96 and 0 <= yi < 96 and out[yi, xi, 3]:
                    out[yi, xi] = (*LINING, 255)
    return out


def draw_arms(sh, el, hands):
    im = Image.new('RGBA', (96, 96)); d = ImageDraw.Draw(im)
    for s, e, h in zip(sh, el, hands):
        d.line([s, e, h], fill=(*SLEEVE, 255), width=3)
        d.line([s, e, h], fill=(*SLEEVE_HI, 255), width=1)
        d.point((round(e[0]), round(e[1])), fill=(*T.C['gold_mid'], 255))
    return np.array(im)


def main():
    os.makedirs(os.path.join(OUT, 'layers'), exist_ok=True)
    legs96, torso96, head96 = to96(load('legs')), to96(load('torso')), to96(load('head'))
    body64, _, _ = T.build_body()
    cape64 = body64.copy(); cape64[51:] = 0
    feet64 = body64.copy(); feet64[:51] = 0
    cape96, feet96 = to96(cape64), to96(feet64)
    idle = to96(np.array(Image.open(T.IDLE).convert('RGBA')))
    b0 = v2.BODY['1'][0]; pose0 = v2.body_pose(b0)
    g0 = grid(v3.CAPE['1'][0], pose0, b0)
    head0 = v3.body_shapes(b0, pose0)['head'][0]
    seq, ok_all, used = [], True, set()
    for hit in '123':
        for i, (k, b, c) in enumerate(zip(v2.SWORD[hit], v2.BODY[hit], v3.CAPE[hit])):
            if hit != '1' and i == 0: continue
            f = i + 1; tag = f'h{hit}_f{f}'
            if (hit, f) in (('1', 1), ('3', 10)):
                seq.append((hit, f, idle.copy())); continue
            pose = v2.body_pose(b)
            G, sp = v2.sword_pose(k, b, None)
            shp = v3.body_shapes(b, pose)
            g1 = grid(c, pose, b)
            base_y = v3.HEM_Y + 0.5 * b['bend'] + 0.5 * b['lean']
            lift = [base_y - col[2][1] for col in g1]
            dx_t = pose['sh_c'][0] - pose0['sh_c'][0]; dy_t = pose['sh_c'][1] - pose0['sh_c'][1]
            hd = (shp['head'][0][0] - head0[0], shp['head'][0][1] - head0[1])
            sh = pose['sh']; el = (m.elbow(sh[0], sp['hand_a'], -1), m.elbow(sh[1], sp['hand_b'], +1))
            sk = dict(grip=G, tip=sp['tip'], pommel=sp['pommel'], foreshorten=k['f'],
                      hand_A=sp['hand_a'], hand_B=sp['hand_b'])
            L = dict(feet=DB.move_feet(feet96, dict(body=b)), legs=warp_legs(legs96, pose0, pose, b['bend']),
                     torso=shift(torso96, dx_t, dy_t), arms=draw_arms(sh, el, (sp['hand_a'], sp['hand_b'])),
                     cape=warp_cape(cape96, g0, g1, lift), head=shift(head96, hd[0], hd[1]),
                     sword=T.draw_sword(sk), hands=T.draw_hands(sk))
            ball0 = feet96[..., 3].any(axis=1).argmax()
            if not np.array_equal(L['feet'][ball0], feet96[ball0]):
                print('FAIL feet slide', tag); ok_all = False
            lifted = b.get('lift', 0)
            if k['depth'] == 'behind':
                order = ['sword', 'hands', 'feet', 'legs', 'torso', 'arms', 'cape', 'head']
            elif lifted:
                order = ['feet', 'legs', 'torso', 'cape', 'arms', 'head', 'sword', 'hands']
            else:
                order = ['feet', 'legs', 'torso', 'arms', 'cape', 'head', 'sword', 'hands']
            img = np.zeros((96, 96, 4), np.uint8)
            for ln in order:
                T.over(img, L[ln])
                Image.fromarray(L[ln]).save(os.path.join(OUT, 'layers', f'{tag}_{ln}.png'))
            seq.append((hit, f, img))
    per = {1: [], 2: [], 3: []}
    for hit, f, img in seq:
        ok, n = T.check(img); ok_all &= ok
        if not ok: print('FAIL', hit, f, n)
        used |= {tuple(c) for c in img[img[..., 3] > 0][:, :3].tolist()}
        per[int(hit)].append(img)
    per[2].insert(0, per[1][-1]); per[3].insert(0, per[2][-1])
    for hit, ims in per.items():
        Image.fromarray(np.concatenate(ims, axis=1)).save(os.path.join(OUT, NAMES[hit] + '.png'))
    print('frames', len(seq), 'palette', len(used))
    ok_all &= len(used) <= 16 and [len(per[h]) for h in (1, 2, 3)] == [8, 8, 10]
    previews([img for *_, img in seq], per)
    sys.exit(0 if ok_all else 1)


def previews(chain, per, k=4, bg=(52, 54, 64, 255)):
    def up(a):
        b = Image.new('RGBA', (96, 96), bg); b.alpha_composite(Image.fromarray(a)); return b.resize((96 * k, 96 * k), Image.NEAREST)
    ims = [up(a).convert('RGB') for a in chain]
    for fps in (16, 4):
        ims[0].save(os.path.join(OUT, f'chain_north_cape_{fps}fps_x4.gif'), save_all=True, append_images=ims[1:], duration=1000 // fps, loop=0)
    v2f = []
    for hit in (1, 2, 3):
        sh = np.array(Image.open(os.path.join(V2, NAMES[hit] + '.png')).convert('RGBA'))
        fr = [sh[:, i * 96:(i + 1) * 96] for i in range(sh.shape[1] // 96)]
        v2f += fr if hit == 1 else fr[1:]
    F = m.font(22); pair = []
    for a, b in zip(v2f, chain):
        im = Image.new('RGB', (96 * k * 2 + 8, 96 * k + 34), (40, 42, 50))
        im.paste(up(a).convert('RGB'), (0, 34)); im.paste(up(b).convert('RGB'), (96 * k + 8, 34))
        d = ImageDraw.Draw(im); d.text((8, 4), '외형 v2 (망토 고정)', font=F, fill='white'); d.text((96 * k + 16, 4), '외형 v3 (망토 분리)', font=F, fill='white')
        pair.append(im)
    pair[0].save(os.path.join(OUT, 'compare_v2_vs_v3_16fps_x4.gif'), save_all=True, append_images=pair[1:], duration=1000 // 16, loop=0)
    s = 3; W = 96 * s; f = m.font(12)
    board = Image.new('RGBA', (W * 10, (W + 16) * 3), bg); d = ImageDraw.Draw(board)
    for r, hit in enumerate((1, 2, 3)):
        for i, a in enumerate(per[hit]):
            board.alpha_composite(Image.fromarray(a).resize((W, W), Image.NEAREST), (i * W, r * (W + 16)))
            d.text((i * W + 4, r * (W + 16) + W + 1), f'{hit}타 f{i + 1}', font=f, fill='white')
    board.save(os.path.join(OUT, 'board_numbered_x3.png'))


if __name__ == '__main__':
    main()
