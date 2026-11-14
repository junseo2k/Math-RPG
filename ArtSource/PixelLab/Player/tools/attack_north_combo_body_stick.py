"""North greatsword 3-hit combo, stick draft v2: whole-body motion (2026-11-08 user request).

Kept from the approved stick v3 drafts: order, frame counts 8/8/10 @16fps, sword angle + screen length per frame.
Added: pelvis weight shift, hip/shoulder rotation, knee bend, forward lean, heel lift + foot rotation about the ball
(ball points never move = no sliding), cape that lags behind the body and keeps swinging a little after the stop.
Hands follow the body (grip moved by the shoulder offset) - allowed by the user; sword length unchanged.
  python attack_north_combo_body_stick.py -> 34_attack_greatsword_combo_north/stick_v2_body/ ; exit 0 = checks passed
No AI generation. The approved stick/ folder is not touched.
"""
import json, math, sys
from pathlib import Path
from PIL import Image, ImageDraw
import attack_north_slash01_stick as m
import attack_north_slash02_stick as h2
import attack_north_slash03_stick as h3

OUT = m.ROOT / '34_attack_greatsword_combo_north' / 'stick_v2_body'
S, FPS = m.S, m.FPS
BALL = ((39, 92), (50, 92))        # ball of each foot (screen left = character left). Fixed for the whole combo.
FOOT_LEN = 4                       # ball -> heel (back view: heel is toward the camera = screen down)

# Body keys per frame: px = pelvis shift (screen x), sh / hip = rotation -1..1 (+ = character turns to its right),
# bend = knee bend px (hips/torso drop), lean = forward lean px (extra drop of shoulders+head), lift = shoulders up,
# heel = (left, right) heel lift px, rot = (left, right) foot rotation deg about the ball.
B = dict
BODY = {
    '1': [  # light slash, screen left -> right
        B(px=0,    sh=0.0,  hip=0.0,  bend=0, lean=0, heel=(0, 0), rot=(0, 0)),     # idle
        B(px=-1,   sh=-0.5, hip=-0.2, bend=0, lean=0, heel=(0, 0), rot=(0, 0)),     # prep: twist left, weight left
        B(px=-1.5, sh=-0.9, hip=-0.4, bend=1, lean=0, heel=(0, 1), rot=(0, -10)),   # prep end: right heel up
        B(px=0,    sh=0.0,  hip=0.2,  bend=2, lean=1, heel=(0, 0), rot=(0, 0)),     # hit: hips lead, drive down
        B(px=1.5,  sh=0.7,  hip=0.5,  bend=2, lean=1, heel=(1, 0), rot=(15, 0)),    # follow: weight right, left heel pivots
        B(px=2,    sh=1.0,  hip=0.7,  bend=2, lean=1, heel=(1, 0), rot=(25, 0)),    # brake
        B(px=2,    sh=0.9,  hip=0.6,  bend=1, lean=0, heel=(1, 0), rot=(25, 0)),
        B(px=2,    sh=0.9,  hip=0.6,  bend=1, lean=0, heel=(1, 0), rot=(20, 0)),    # settle (= 2nd f1)
    ],
    '2': [  # rising slash: gather low, extend up
        None,                                                                         # = 1st f8
        B(px=1,    sh=0.3,  hip=0.3,  bend=4, lean=1, heel=(0, 0), rot=(10, 0)),    # gather: knees down
        B(px=0.5,  sh=0.0,  hip=0.1,  bend=1, lean=0, heel=(1, 1), rot=(0, 0)),     # hit: drive up onto the balls
        B(px=0,    sh=0.0,  hip=0.0,  bend=-1, lean=0, heel=(2, 2), rot=(0, 0)),    # full extension
        B(px=0,    sh=0.0,  hip=0.0,  bend=-1, lean=0, heel=(1, 1), rot=(0, 0), lift=1),
        B(px=0,    sh=0.0,  hip=0.0,  bend=0, lean=0, heel=(0, 0), rot=(0, 0), lift=2),
        B(px=0,    sh=0.0,  hip=0.0,  bend=0, lean=0, heel=(0, 0), rot=(0, 0), lift=2),
        B(px=0,    sh=0.0,  hip=0.0,  bend=0, lean=0, heel=(0, 0), rot=(0, 0), lift=2),  # (= 3rd f1)
    ],
    '3': [  # heavy chop: knees and torso drop together, brake low, stand up to idle
        None,
        B(px=0,    sh=0.0,  hip=0.0,  bend=3, lean=2, heel=(0, 0), rot=(0, 0)),     # hit
        B(px=0,    sh=0.0,  hip=0.0,  bend=5, lean=3, heel=(0, 0), rot=(0, 0)),
        B(px=0,    sh=0.0,  hip=0.0,  bend=6, lean=4, heel=(0, 0), rot=(0, 0)),     # lowest
        B(px=0,    sh=0.0,  hip=0.0,  bend=6, lean=3, heel=(0, 0), rot=(0, 0)),
        B(px=0,    sh=0.1,  hip=0.0,  bend=5, lean=2, heel=(0, 0), rot=(0, 0)),
        B(px=0.5,  sh=0.3,  hip=0.1,  bend=3, lean=1, heel=(0, 0), rot=(0, 0)),     # recover
        B(px=0.5,  sh=0.2,  hip=0.1,  bend=1, lean=0, heel=(0, 0), rot=(0, 0)),
        B(px=0,    sh=0.1,  hip=0.0,  bend=0, lean=0, heel=(0, 0), rot=(0, 0)),
        B(px=0,    sh=0.0,  hip=0.0,  bend=0, lean=0, heel=(0, 0), rot=(0, 0)),     # idle
    ],
}
BODY['2'][0] = BODY['1'][-1]
BODY['3'][0] = BODY['2'][-1]
SWORD = {'1': m.KEYS, '2': h2.KEYS, '3': h3.KEYS}
HEAD_R = m.HEAD[1]

# cape: damped spring on the hem centre (x) chasing the shoulder centre; also a little vertical bob
CAPE_K, CAPE_C, SUB = 0.22, 0.34, 4


def body_pose(b):
    lift = b.get('lift', 0)
    drop = b['bend']                    # hips drop
    up_drop = b['bend'] + b['lean']     # shoulders/head drop (lean = torso tips forward/down)
    sc = m.SHOULDER_C + b['px'] + 2 * b['sh']
    half = m.SHOULDER_HALF - 1.5 * abs(b['sh'])
    sy = m.SHOULDER_Y + up_drop - lift
    hc = m.SHOULDER_C + b['px'] - 1.0 * b['hip']   # back view: turning right swings the pelvis centre a bit left
    hhalf = m.HIP_HALF - 1.5 * abs(b['hip'])
    hy = m.HIP_Y + drop
    head = (m.HEAD[0][0] + b['px'] + 1.2 * b['sh'], m.HEAD[0][1] + up_drop + 0.5 * b['lean'])
    feet = []
    for i in (0, 1):
        bx, by = BALL[i]
        a = math.radians(b['rot'][i])
        # heel vector from the ball: straight toward camera (down), rotated, shortened by heel lift
        L = FOOT_LEN - b['heel'][i]
        feet.append(((bx, by), (bx - math.sin(a) * L, by + math.cos(a) * L)))   # + rot = turning right: heel swings to screen left
    return dict(sh=((sc + half, sy), (sc - half, sy)), sh_c=(sc, sy), hips=((hc + hhalf - 1, hy), (hc - hhalf + 1, hy)),
                hip_c=(hc, hy), head=(head, HEAD_R), feet=feet)


def knee(hip, foot, side, bend):
    """Knees push outward (side) when bent."""
    mx, my = (hip[0] + foot[0]) / 2, (hip[1] + foot[1]) / 2
    return (mx + side * (0.5 + 0.6 * max(bend, 0)), my)


def spring(targets):
    x = targets[0]; v = 0.0; out = []
    for t in targets:
        for _ in range(SUB):
            a = CAPE_K * (t - x) - CAPE_C * v
            v += a / SUB * 2; x += v / SUB * 2
        out.append(x)
    return out


def simulate_cape(poses):
    """Hem lags the body: x chases the shoulder centre, y chases the hip drop (damped spring, 4 substeps/frame)."""
    return spring([p['sh_c'][0] for p in poses]), spring([p['hip_c'][1] for p in poses])


def sword_pose(k, b, b0):
    """Approved sword (angle, screen length); grip moved with the shoulders relative to the approved stick body."""
    old = dict(px=0, sh=k['twist'], bend=k['bend'], lean=0, lift=k.get('lift', 0))
    dx = (b['px'] + 2 * b['sh']) - (old['px'] + 2 * old['sh'])
    dy = (b['bend'] + b['lean'] - b.get('lift', 0)) - (old['bend'] - old['lift'])
    G = (k['G'][0] + dx, k['G'][1] + dy)
    kk = dict(k, G=G)
    p = m.solve(dict(kk, twist=0, bend=0))   # tip/pommel/hands only (shoulders recomputed below)
    return G, p


def frame_data():
    rows = []
    for hit in '123':
        for i, (k, b) in enumerate(zip(SWORD[hit], BODY[hit])):
            if hit != '1' and i == 0:
                continue                         # shared frame
            rows.append((hit, i + 1, k, b))
    return rows


COL = dict(m.COL, sh=(255, 220, 120, 255), hip=(120, 255, 170, 255), cape=(150, 150, 160, 255),
           cape_lag=(200, 120, 140, 255), foot=(120, 120, 130, 255), heel=(255, 120, 120, 255))


def render(k, b, pose, hem, sc, guides):
    hem_x, hem_dy = hem[0], hem[1] - pose['hip_c'][1]   # hem_dy < 0: hem has not dropped as far as the hips yet
    P = lambda q: (q[0] * sc, q[1] * sc)
    im = Image.new('RGBA', (S * sc, S * sc)); d = ImageDraw.Draw(im)
    G, sp = sword_pose(k, b, None)
    sh_a, sh_b = pose['sh']
    el_a, el_b = m.elbow(sh_a, sp['hand_a'], -1), m.elbow(sh_b, sp['hand_b'], +1)
    behind = k['depth'] == 'behind'
    # arms (always under cape/head)
    for s, e, h, c in ((sh_a, el_a, sp['hand_a'], 'A'), (sh_b, el_b, sp['hand_b'], 'B')):
        d.line([P(s), P(e), P(h)], fill=COL[c], width=max(1, sc))
    if behind:
        m.draw_sword(d, sp, sc)
    # legs + feet
    for i in (0, 1):
        ball, heel = pose['feet'][i]
        hip = pose['hips'][1] if i == 0 else pose['hips'][0]          # hips[1] = screen-left end
        kn = knee(hip, ball, -1 if i == 0 else 1, b['bend'])
        d.line([P(hip), P(kn), P(((ball[0] + heel[0]) / 2, (ball[1] + heel[1]) / 2))], fill=COL['foot'], width=max(1, 2 * sc // 2))
        d.line([P(ball), P(heel)], fill=COL['foot'], width=3 * sc)
        if b['heel'][i]:
            d.ellipse([heel[0] * sc - sc, heel[1] * sc - sc, heel[0] * sc + sc, heel[1] * sc + sc], fill=COL['heel'])
    # cape: shoulders -> hem; hem centre lags
    (sx, sy) = pose['sh_c']; hy = min(90 + 0.5 * (pose['hip_c'][1] - m.HIP_Y) + hem_dy, 91.5)
    sway = hem_x - sx
    top_l, top_r = (sx + m.CAPE_TOP[0], sy - 2), (sx + m.CAPE_TOP[1], sy - 2)
    bot_r = (hem_x + m.CAPE_BOTTOM[1] + 0.5 * sway, hy)
    bot_l = (hem_x + m.CAPE_BOTTOM[0] + 0.5 * sway, hy)
    cape = [top_l, top_r, bot_r, bot_l]
    d.polygon([P(q) for q in cape], fill=COL['cape'])
    (hx, hy2), r = pose['head']
    d.ellipse([(hx - r) * sc, (hy2 - r) * sc, (hx + r) * sc, (hy2 + r) * sc], fill=COL['head'])
    if not behind:
        m.draw_sword(d, sp, sc)
    mask = Image.new('L', (S, S)); dm = ImageDraw.Draw(mask)
    dm.polygon(cape, fill=255); dm.ellipse([hx - r, hy2 - r, hx + r, hy2 + r], fill=255)
    for w in 'ab':
        h = sp['hand_' + w]
        inside = 0 <= h[0] < S and 0 <= h[1] < S and mask.getpixel((int(round(h[0])), int(round(h[1]))))
        if not behind or not inside:
            d.rectangle([(h[0] - 1) * sc, (h[1] - 1) * sc, (h[0] + 1) * sc, (h[1] + 1) * sc], fill=COL[w.upper()])
    if guides:
        # shoulder + hip lines (rotation), hidden sword dashed
        d.line([P(pose['sh'][0]), P(pose['sh'][1])], fill=COL['sh'], width=max(1, sc // 2))
        d.line([P(pose['hips'][0]), P(pose['hips'][1])], fill=COL['hip'], width=max(1, sc // 2))
        if behind:
            m.draw_sword(d, sp, sc, dashed=True)
        d.line([P((sx, hy)), P((hem_x, hy))], fill=COL['cape_lag'], width=max(1, sc // 2))
    for q, c in ((sp['tip'], 'tip'), (G, 'grip')):
        rr = max(1, sc * 0.8)
        d.ellipse([q[0] * sc - rr, q[1] * sc - rr, q[0] * sc + rr, q[1] * sc + rr], fill=COL[c])
    return im, dict(grip=G, tip=sp['tip'], pommel=sp['pommel'], hand_A=sp['hand_a'], hand_B=sp['hand_b'],
                    elbow_A=el_a, elbow_B=el_b, cape=cape)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    rows = frame_data()
    poses = [body_pose(b) for _, _, _, b in rows]
    hx_, hy_ = simulate_cape(poses)
    hx_[-1], hy_[-1] = poses[-1]['sh_c'][0], poses[-1]['hip_c'][1]   # last frame must be exactly idle
    hems = list(zip(hx_, hy_))
    fails = []
    sc, bg = 4, (60, 63, 72, 255)
    # checks
    for (hit, f, k, b), p, hx in zip(rows, poses, hems):
        _, sp = sword_pose(k, b, None)
        G = sword_pose(k, b, None)[0]
        L = math.dist(sp['tip'], G)
        if abs(L - m.TIP_LEN * k['f']) > 0.05: fails.append(f'{hit}-{f} sword length {L:.1f}')
        for w, s in (('a', p['sh'][0]), ('b', p['sh'][1])):
            if math.dist(s, sp['hand_' + w]) > m.UPPER + m.FORE + 0.01: fails.append(f'{hit}-{f} arm {w} too short')
        for i in (0, 1):
            if p['feet'][i][0] != BALL[i]: fails.append(f'{hit}-{f} foot {i} ball moved')
    if body_pose(BODY['3'][-1]) != body_pose(BODY['1'][0]): fails.append('3rd f10 != idle')
    # frames
    frames, coords, x1 = [], [], []
    for (hit, f, k, b), p, hx in zip(rows, poses, hems):
        im, c = render(k, b, p, hx, sc, True)
        cell = Image.new('RGBA', (S * sc, S * sc), bg)
        cd = ImageDraw.Draw(cell)
        for g in range(0, S * sc, 16 * sc):
            cd.line([(g, 0), (g, S * sc)], fill=(70, 74, 84, 255)); cd.line([(0, g), (S * sc, g)], fill=(70, 74, 84, 255))
        cd.line([(0, m.PIVOT[1] * sc), (S * sc, m.PIVOT[1] * sc)], fill=(70, 120, 255, 255))
        cd.line([(m.PIVOT[0] * sc, 0), (m.PIVOT[0] * sc, S * sc)], fill=(70, 120, 255, 120))
        cell.alpha_composite(im)
        frames.append((hit, f, k, cell))
        x1.append(render(k, b, p, hx, 1, False)[0])
        coords.append(dict(hit=int(hit), frame=f, stage=k['stage'], sword_angle=k['ang'], foreshorten=k['f'], depth=k['depth'],
                           **{n: [round(v, 1) for v in val] for n, val in c.items() if n != 'cape'},
                           cape=[[round(v, 1) for v in q] for q in c['cape']], body=b,
                           shoulders=[[round(v, 1) for v in q] for q in p['sh']], hips=[[round(v, 1) for v in q] for q in p['hips']],
                           head=[round(v, 1) for v in p['head'][0]], feet=[[list(map(lambda v: round(v, 1), q)) for q in ft] for ft in p['feet']],
                           cape_hem=[round(hx[0], 2), round(hx[1], 2)]))
    # 1x sheets per hit (with shared frames repeated, like the approved sheets)
    per = {'1': [], '2': [], '3': []}
    for (hit, f, _, _), im in zip(rows, x1):
        per[hit].append(im)
    per['2'].insert(0, per['1'][-1]); per['3'].insert(0, per['2'][-1])
    for hit, ims in per.items():
        sh = Image.new('RGBA', (S * len(ims), S))
        for i, im in enumerate(ims): sh.alpha_composite(im, (i * S, 0))
        sh.save(OUT / f'stick_body_hit{hit}_{len(ims)}f.png')
    # GIFs: new chain, and approved vs new side by side
    gif = [c.convert('RGB') for *_, c in frames]
    for fps in (16, 4):
        gif[0].save(OUT / f'stick_body_chain_{fps}fps_x4.gif', save_all=True, append_images=gif[1:], duration=1000 // fps, loop=0)
    old = []
    for k in m.KEYS + h2.KEYS[1:] + h3.KEYS[1:]:
        c = Image.new('RGBA', (S * sc, S * sc), bg); c.alpha_composite(m.render(k, m.solve(k), sc, True)); old.append(c)
    pair = []
    F = m.font(22)
    for o, (hit, f, k, n) in zip(old, frames):
        im = Image.new('RGB', (S * sc * 2 + 8, S * sc + 34), (40, 42, 50))
        im.paste(o.convert('RGB'), (0, 34)); im.paste(n.convert('RGB'), (S * sc + 8, 34))
        dd = ImageDraw.Draw(im)
        dd.text((8, 4), '승인본(검만)', font=F, fill='white'); dd.text((S * sc + 16, 4), f'전신 보완 후보   {hit}타 f{f}', font=F, fill='white')
        pair.append(im)
    pair[0].save(OUT / 'compare_approved_vs_body_16fps_x4.gif', save_all=True, append_images=pair[1:], duration=1000 // FPS, loop=0)
    # numbered board
    cols, lab = 8, 34
    nrow = 4
    order = [r for r in range(len(frames))]
    board = Image.new('RGBA', (S * 2 * cols, (S * 2 + lab) * nrow), bg); bd = ImageDraw.Draw(board)
    F2 = m.font(14)
    # rows: hit1 (8), hit2 f2-8 (7), hit3 f2-9 (8) + f10 on its own
    pos, r, cidx = [], 0, 0
    layout = {'1': 0, '2': 1, '3': 2}
    count = {'1': 0, '2': 1, '3': 1}
    for (hit, f, k, cell) in frames:
        row = layout[hit]; col = count[hit]; count[hit] += 1
        if col >= cols: row, col = 3, col - cols
        x0, y0 = col * S * 2, row * (S * 2 + lab)
        board.alpha_composite(cell.resize((S * 2, S * 2), Image.LANCZOS), (x0, y0))
        bd.text((x0 + 4, y0 + S * 2 + 2), f'{hit}타 f{f}', font=F2, fill='white')
        bd.text((x0 + 4, y0 + S * 2 + 17), k['stage'][:14], font=m.font(11), fill=(200, 200, 200, 255))
    board.save(OUT / 'stick_body_board_x2.png')
    json.dump(dict(canvas=S, pivot=m.PIVOT, fps=FPS, frames=len(rows), seconds=len(rows) / FPS, balls=BALL,
                   cape_spring=dict(k=CAPE_K, c=CAPE_C, substeps=SUB), keys=coords),
              open(OUT / 'coords.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    print('cape lag x:', [round(h[0] - p['sh_c'][0], 1) for h, p in zip(hems, poses)])
    print('cape lag y:', [round(h[1] - p['hip_c'][1], 1) for h, p in zip(hems, poses)])
    print('FAIL' if fails else 'OK', fails)
    return 1 if fails else 0


if __name__ == '__main__':
    sys.exit(main())
