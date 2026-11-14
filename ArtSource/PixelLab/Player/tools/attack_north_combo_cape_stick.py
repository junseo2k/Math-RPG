"""North 3-hit combo, stick draft v3: cape separated from the body (2026-11-08 user: "everything moves together").

Kept from the approved v2 (attack_north_combo_body_stick.py): sword per frame, body keys (rotation, knees, lean,
heels/feet), 8/8/10 @16fps. New:
  body = visible shapes (torso, legs, arms, head) placed like body_under_cape/ (torso y65-79, hips y80, legs to feet,
         head 1 px left of the dress-v1 head - user pick 2026-11-08)
  cape = its own cloth: top fixed on the shoulders, 5 hem points keyed per frame (swing, side lift, flare, whole-hem lift)
         after the East measurements (cape -13..+12 px off the body, width x2, 6-7 px above the body on the chop).
         The cape is keyed with its lag already in (artist keys), so it reacts a frame after the body and overshoots.
  lining (dark) shows where the hem is lifted.
  python attack_north_combo_cape_stick.py -> 34_attack_greatsword_combo_north/stick_v3_cape/ ; exit 0 = checks passed
"""
import json, math, sys
from PIL import Image, ImageDraw
import attack_north_slash01_stick as m
import attack_north_combo_body_stick as v2

OUT = m.ROOT / '34_attack_greatsword_combo_north' / 'stick_v3_cape'
S, FPS = m.S, m.FPS
HEAD_DX = -1
HIP_Y = 80                                  # body_under_cape: belt y76-77, legs from y80
HEM_X = (-16.0, -8.0, -1.0, 6.0, 13.5)      # idle cape hem x relative to the shoulder centre (cape x30-61 at idle)
HEM_Y = 90.0
TOP = (-10.5, 10.0)                         # cape top corners on the shoulders (pauldrons), y = shoulders - 2

# cape keys per frame: s = swing (+ right, px), L = lift of the leading side, f = flare (extra width), up = whole hem up
C = lambda s=0, L=0, f=0, up=0: dict(s=s, L=L, f=f, up=up)
CAPE = {
    '1': [C(), C(2, 0, 1), C(4, 1, 2),          # body twists left, cape trails right
          C(-3, 2, 2),                          # hit: body whips right, cape is left behind
          C(8, 5, 7), C(11, 6, 9),              # cape whips over to the right and lifts (left side of the body shows)
          C(8, 3, 6), C(5, 1, 3)],              # falls back, still swinging (= 2nd f1)
    '2': [None, C(3, 0, 3, -1),                 # gather: cape pools low
          C(-7, 3, 6), C(-10, 5, 8, 1),         # rising cut: cape flips to the left
          C(-7, 3, 5, 1), C(-4, 1, 3), C(-2, 0, 1), C(-1, 0, 0)],   # settles (= 3rd f1)
    '3': [None, C(0, 0, 4, 6), C(-2, 0, 5, 8),  # body drops fast, cape stays up and opens (legs show)
          C(-1, 0, 4, 6), C(0, 0, 3, 3),        # cape comes down while the body holds low
          C(0, 0, 2, -1),                       # overshoots below rest
          C(1, 0, 1, 1), C(1, 0, 0, 0.5), C(0.5, 0, 0, 0), C()],      # small bounce, idle
}
CAPE['2'][0] = CAPE['1'][-1]
CAPE['3'][0] = CAPE['2'][-1]

COL = dict(v2.COL, torso=(120, 128, 150, 255), leg=(100, 108, 130, 255), arm=(140, 150, 175, 255),
           cape=(175, 70, 85, 230), lining=(95, 30, 45, 255), head=(200, 200, 210, 255))


def body_shapes(b, pose):
    (sx, sy) = pose['sh_c']
    drop = b['bend']
    hc = pose['hip_c'][0]
    hy = HIP_Y + drop
    half_sh = 8.5 - 2 * abs(b['sh'])           # torso narrows when it turns (back view)
    half_hip = 5.5 - 1.5 * abs(b['hip'])
    torso = [(sx - half_sh, sy - 2), (sx + half_sh, sy - 2), (hc + half_hip, hy), (hc - half_hip, hy)]
    legs = []
    for i in (0, 1):
        ball, heel = pose['feet'][i]
        hip = (hc + (-1 if i == 0 else 1) * 3.5, hy)
        ankle = ((ball[0] + heel[0]) / 2, (ball[1] + heel[1]) / 2 - 1)
        kn = v2.knee(hip, ankle, -1 if i == 0 else 1, b['bend'])
        legs.append((hip, kn, ankle, ball, heel))
    (hx, hyy), r = pose['head']
    return dict(torso=torso, legs=legs, head=((hx + HEAD_DX, hyy), r))


def cape_shape(c, pose, b):
    (sx, sy) = pose['sh_c']
    top_l, top_r = (sx + TOP[0], sy - 2), (sx + TOP[1], sy - 2)
    base_y = HEM_Y + 0.5 * b['bend'] + 0.5 * b['lean']
    hem = []
    for i, hx in enumerate(HEM_X):
        t = i / (len(HEM_X) - 1)                       # 0 left .. 1 right
        lead = t if c['s'] >= 0 else 1 - t             # leading side = the side the cape swings to
        side = (t - 0.5) * 2                           # -1 left .. 1 right
        x = sx + hx + c['s'] * (0.4 + 1.0 * (1 - lead))  # v3b: trailing edge is dragged across more -> the trailing side of the body shows
        x += (side if c['s'] == 0 else (1 if c['s'] > 0 else -1) * lead) * c['f']
        y = base_y - c['up'] - c['L'] * lead * lead
        hem.append((x, y))
    # side edges bulge out a little at mid height with the flare
    mid_l = ((top_l[0] + hem[0][0]) / 2 - (0.5 * c['f'] if c['s'] <= 0 else 0), (top_l[1] + hem[0][1]) / 2)
    mid_r = ((top_r[0] + hem[-1][0]) / 2 + (0.5 * c['f'] if c['s'] >= 0 else 0), (top_r[1] + hem[-1][1]) / 2)
    poly = [top_l, top_r, mid_r] + hem[::-1] + [mid_l]
    return poly, hem, (top_l, top_r)


def render(k, b, c, sc, guides):
    P = lambda q: (q[0] * sc, q[1] * sc)
    pose = v2.body_pose(b)
    im = Image.new('RGBA', (S * sc, S * sc)); d = ImageDraw.Draw(im)
    G, sp = v2.sword_pose(k, b, None)
    sh_a, sh_b = pose['sh']
    el_a, el_b = m.elbow(sh_a, sp['hand_a'], -1), m.elbow(sh_b, sp['hand_b'], +1)
    behind = k['depth'] == 'behind'
    B = body_shapes(b, pose)
    poly, hem, top = cape_shape(c, pose, b)
    if behind:
        m.draw_sword(d, sp, sc)
    for hip, kn, ankle, ball, heel in B['legs']:
        d.line([P(hip), P(kn), P(ankle)], fill=COL['leg'], width=3 * sc)
        d.line([P(ball), P(heel)], fill=COL['leg'], width=3 * sc)
    d.polygon([P(q) for q in B['torso']], fill=COL['torso'])
    for s, e, h in ((sh_a, el_a, sp['hand_a']), (sh_b, el_b, sp['hand_b'])):
        d.line([P(s), P(e), P(h)], fill=COL['arm'], width=2 * sc)
    d.polygon([P(q) for q in poly], fill=COL['cape'])
    lift = [HEM_Y + 0.5 * b['bend'] + 0.5 * b['lean'] - y for _, y in hem]
    for (p0, l0), (p1, l1) in zip(zip(hem, lift), zip(hem[1:], lift[1:])):
        w = min(max(l0, l1), 4)
        if w > 1.5:   # underside shows where the hem is lifted
            d.polygon([P(p0), P(p1), P((p1[0], p1[1] - w)), P((p0[0], p0[1] - w))], fill=COL['lining'])
    (hx, hy), r = B['head']
    d.ellipse([(hx - r) * sc, (hy - r) * sc, (hx + r) * sc, (hy + r) * sc], fill=COL['head'])
    if not behind:
        m.draw_sword(d, sp, sc)
    for w in 'ab':
        h = sp['hand_' + w]
        d.rectangle([(h[0] - 1) * sc, (h[1] - 1) * sc, (h[0] + 1) * sc, (h[1] + 1) * sc], fill=COL[w.upper()])
    if guides:
        if behind:
            m.draw_sword(d, sp, sc, dashed=True)
        d.line([P(pose['sh'][0]), P(pose['sh'][1])], fill=COL['sh'], width=max(1, sc // 2))
        for q in hem:
            d.ellipse([q[0] * sc - sc, q[1] * sc - sc, q[0] * sc + sc, q[1] * sc + sc], fill=(255, 230, 120, 255))
    for q, cc in ((sp['tip'], 'tip'), (G, 'grip')):
        rr = max(1, sc * 0.8)
        d.ellipse([q[0] * sc - rr, q[1] * sc - rr, q[0] * sc + rr, q[1] * sc + rr], fill=COL[cc])
    return im, dict(cape=poly, hem=hem, torso=B['torso'], head=B['head'][0], sword=sp, grip=G, pose=pose)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    rows = []
    for hit in '123':
        for i, (k, b, c) in enumerate(zip(v2.SWORD[hit], v2.BODY[hit], CAPE[hit])):
            if hit != '1' and i == 0:
                continue
            rows.append((hit, i + 1, k, b, c))
    sc, bg = 4, (60, 63, 72, 255)
    fails, cells, x1, meta = [], [], [], []
    for hit, f, k, b, c in rows:
        im, info = render(k, b, c, sc, True)
        cell = Image.new('RGBA', (S * sc, S * sc), bg); cd = ImageDraw.Draw(cell)
        for g in range(0, S * sc, 16 * sc):
            cd.line([(g, 0), (g, S * sc)], fill=(70, 74, 84, 255)); cd.line([(0, g), (S * sc, g)], fill=(70, 74, 84, 255))
        cd.line([(0, m.PIVOT[1] * sc), (S * sc, m.PIVOT[1] * sc)], fill=(70, 120, 255, 255))
        cell.alpha_composite(im); cells.append((hit, f, k, cell))
        x1.append(render(k, b, c, 1, False)[0])
        xs = [q[0] for q in info['cape']]
        tc = (info['torso'][0][0] + info['torso'][1][0]) / 2
        meta.append(dict(hit=int(hit), frame=f, stage=k['stage'], cape_keys=c,
                         cape=[[round(v, 1) for v in q] for q in info['cape']], hem=[[round(v, 1) for v in q] for q in info['hem']],
                         torso=[[round(v, 1) for v in q] for q in info['torso']], head=[round(v, 1) for v in info['head']],
                         cape_minus_body_x=round((min(xs) + max(xs)) / 2 - tc, 1), cape_width=round(max(xs) - min(xs), 1)))
        for q in info['cape']:
            if not (0 <= q[0] < S and 0 <= q[1] < S): fails.append(f'{hit}-{f} cape out of canvas')
    if rows[-1][4] != CAPE['1'][0]: fails.append('3rd f10 cape != idle')
    # GIFs
    gif = [c.convert('RGB') for *_, c in cells]
    for fps in (16, 4):
        gif[0].save(OUT / f'stick_cape_chain_{fps}fps_x4.gif', save_all=True, append_images=gif[1:], duration=1000 // fps, loop=0)
    v2cells = []
    rows2 = v2.frame_data(); poses = [v2.body_pose(b) for *_, b in rows2]
    hx_, hy_ = v2.simulate_cape(poses); hx_[-1], hy_[-1] = poses[-1]['sh_c'][0], poses[-1]['hip_c'][1]
    for (h, f, k, b), p, hx, hy in zip(rows2, poses, hx_, hy_):
        c = Image.new('RGBA', (S * sc, S * sc), bg); c.alpha_composite(v2.render(k, b, p, (hx, hy), sc, True)[0]); v2cells.append(c)
    F = m.font(22); pair = []
    for o, (hit, f, k, n) in zip(v2cells, cells):
        im = Image.new('RGB', (S * sc * 2 + 8, S * sc + 34), (40, 42, 50))
        im.paste(o.convert('RGB'), (0, 34)); im.paste(n.convert('RGB'), (S * sc + 8, 34))
        dd = ImageDraw.Draw(im)
        dd.text((8, 4), '승인 v2 (망토 고정)', font=F, fill='white'); dd.text((S * sc + 16, 4), f'v3 망토 분리   {hit}타 f{f}', font=F, fill='white')
        pair.append(im)
    pair[0].save(OUT / 'compare_v2_vs_v3_16fps_x4.gif', save_all=True, append_images=pair[1:], duration=1000 // FPS, loop=0)
    # board
    cols, W = 10, S * 2
    board = Image.new('RGBA', (W * cols, (W + 30) * 3), bg); bd = ImageDraw.Draw(board)
    cnt = {'1': 0, '2': 1, '3': 1}; F2 = m.font(13)
    for hit, f, k, cell in cells:
        r = int(hit) - 1; ccol = cnt[hit]; cnt[hit] += 1
        x0, y0 = ccol * W, r * (W + 30)
        board.alpha_composite(cell.resize((W, W), Image.LANCZOS), (x0, y0))
        bd.text((x0 + 4, y0 + W + 2), f'{hit}타 f{f}', font=F2, fill='white')
        bd.text((x0 + 4, y0 + W + 16), k['stage'][:13], font=m.font(10), fill=(200, 200, 200, 255))
    board.save(OUT / 'stick_cape_board_x2.png')
    json.dump(dict(fps=FPS, frames=len(rows), keys=meta), open(OUT / 'coords.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    print('cape - body x:', [d['cape_minus_body_x'] for d in meta])
    print('cape width   :', [d['cape_width'] for d in meta])
    print('FAIL' if fails else 'OK', fails)
    return 1 if fails else 0


if __name__ == '__main__':
    sys.exit(main())
