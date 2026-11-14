"""North 1st hit (light slash) stick-figure draft, 8 frames, 96x96, pivot (48,94), 16 fps.
No generation. Body = simple shapes, arms/sword = sticks. Sword is its own layer.
  python attack_north_slash01_stick.py   -> 34_attack_greatsword_combo_north/stick/ ; exit 0 = checks passed
Screen coords: (0,0) top-left, +x right, +y down. Sword angle = grip->tip, 0 right / 90 up / 180 left / 270 down.
Arm A = character right arm (screen right, back view), arm B = character left arm. Hand A sits on the guard side.
"""
import json, math, sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / '34_attack_greatsword_combo_north' / 'stick'
IDLE = ROOT / '16_idle_greatsword_pauldron_breath' / '2_north_1.png'   # start pose (= 2_north.aseprite frame 1, byte-identical export)
EAST = ROOT / '33_attack_greatsword_combo_east' / '1_light_slash_east.png'
S, N, FPS = 96, 8, 16
PIVOT = (48, 94)
IDLE_OFS = (16, 40)            # 64-px idle placed so its feet bottom row (54) lands on y=94, centre x 32 -> 48
TIP_LEN = 33.3                 # grip centre -> tip in idle (measured: grip (51,69), tip (26,47))
POMMEL_LEN = 6.4
UPPER, FORE = 9, 9
HEAD = ((46.5, 63.5), 8)       # from hair bbox 22..39 x 16..31 (+offset)
SHOULDER_C, SHOULDER_HALF, SHOULDER_Y = 46.5, 6.5, 68
HIP_Y, HIP_HALF = 90, 6        # legs start under the cape hem (idle cape bottom y=50 -> 90)
# cape silhouette (idle cape bbox x 14..44, y 26..50 -> 30..60, 66..90). Arms sit behind it in back view (2026-11-08 user)
CAPE_TOP, CAPE_BOTTOM = (-9, 9.5), (-16.5, 13.5)
FEET = ((39, 93), (50, 93))    # both feet planted for the whole hit (support = both)

# per frame: grip centre G, sword angle, screen foreshortening f, torso twist (-1 left .. +1 right),
# knee bend px, sword depth ('over' = drawn over body, 'behind' = in front of a north-facing body -> hidden by it), stage
# v2 (2026-11-08 user: facing north, so swing mirrored): pull left -> slash left->right -> brake lower right.
# v1 (right->left) is in 34_.../stick/history/v1_right_to_left/
KEYS = [
    dict(G=(51, 69), ang=139, f=1.00, twist=0.0, bend=0, depth='over',   stage='시작 = north idle 1프레임'),
    dict(G=(45, 65), ang=150, f=1.00, twist=-0.4, bend=0, depth='over',  stage='준비: 양손을 왼쪽으로 당김'),
    dict(G=(41, 68), ang=165, f=1.00, twist=-0.8, bend=1, depth='over',  stage='준비 끝: 칼끝 왼쪽'),
    dict(G=(47, 63), ang=88,  f=0.60, twist=0.0, bend=1, depth='behind', stage='베기: 몸 앞 통과(판정)'),
    dict(G=(53, 69), ang=12,  f=0.95, twist=0.7, bend=1, depth='behind', stage='베기: 오른쪽으로'),
    dict(G=(56, 75), ang=340, f=1.00, twist=0.9, bend=1, depth='behind', stage='제동: 오른쪽 아래'),
    dict(G=(57, 76), ang=334, f=1.00, twist=0.9, bend=1, depth='behind', stage='제동'),
    dict(G=(57, 76), ang=332, f=1.00, twist=0.9, bend=1, depth='behind', stage='정착(양손 파지 유지)'),
]


def unit(a):
    r = math.radians(a)
    return math.cos(r), -math.sin(r)


def solve(k):
    gx, gy = k['G']
    ux, uy = unit(k['ang'])
    L = TIP_LEN * k['f']
    tip = (gx + ux * L, gy + uy * L)
    pommel = (gx - ux * POMMEL_LEN * k['f'], gy - uy * POMMEL_LEN * k['f'])
    hand_a = (gx + ux * 2, gy + uy * 2)
    hand_b = (gx - ux * 2, gy - uy * 2)
    c = SHOULDER_C + 2 * k['twist']
    half = SHOULDER_HALF - 1.5 * abs(k['twist'])
    y = SHOULDER_Y + k['bend'] - k.get('lift', 0)   # lift: shoulders rise when the arms go overhead (2nd hit)
    sh_a, sh_b = (c + half, y), (c - half, y)
    return dict(tip=tip, pommel=pommel, hand_a=hand_a, hand_b=hand_b, sh_a=sh_a, sh_b=sh_b,
                el_a=elbow(sh_a, hand_a, -1), el_b=elbow(sh_b, hand_b, +1),
                head=((HEAD[0][0] + k['twist'], HEAD[0][1] + k['bend']), HEAD[1]),
                hips=((c - HIP_HALF + 1 - k['twist'], HIP_Y + k['bend']), (c + HIP_HALF - 1 - k['twist'], HIP_Y + k['bend'])),
                cape=((c + CAPE_TOP[0], 66 + k['bend']), (c + CAPE_TOP[1], 66 + k['bend']),
                      (c + CAPE_BOTTOM[1] - k['twist'], 90), (c + CAPE_BOTTOM[0] - k['twist'], 90)),
                knees=tuple((FEET[i][0] + (-1 if i == 0 else 1) * k['bend'], (HIP_Y + k['bend'] + FEET[i][1]) / 2) for i in (0, 1)))


def elbow(s, h, side):
    """2-bone IK; elbow bends toward side (+1 = screen right) and down. Back view: elbows tuck inward so the cape hides them."""
    dx, dy = h[0] - s[0], h[1] - s[1]
    d = max(min(math.hypot(dx, dy), UPPER + FORE - 1e-6), 1e-6)
    a = (UPPER ** 2 - FORE ** 2 + d ** 2) / (2 * d)
    hgt = math.sqrt(max(UPPER ** 2 - a ** 2, 0))
    mx, my = s[0] + dx * a / d, s[1] + dy * a / d
    nx, ny = -dy / d, dx / d
    c1, c2 = (mx + nx * hgt, my + ny * hgt), (mx - nx * hgt, my - ny * hgt)
    score = lambda p: side * (p[0] - s[0]) + 0.5 * (p[1] - s[1])
    return max(c1, c2, key=score)


def body_mask(p, scale=1):
    m = Image.new('L', (S * scale, S * scale), 0)
    d = ImageDraw.Draw(m)
    (hx, hy), r = p['head']
    d.ellipse([(hx - r) * scale, (hy - r) * scale, (hx + r) * scale, (hy + r) * scale], fill=255)
    d.polygon([(x * scale, y * scale) for x, y in p['cape']], fill=255)
    return m


# ---------- drawing ----------
COL = dict(body=(150, 150, 160, 255), head=(190, 190, 200, 255), leg=(120, 120, 130, 255),
           A=(255, 140, 60, 255), B=(80, 200, 255, 255), blade=(245, 245, 245, 255), handle=(240, 200, 60, 255),
           tip=(255, 70, 70, 255), grip=(255, 255, 0, 255), hidden=(255, 255, 255, 150))


def draw_body(d, p, sc):
    P = lambda q: (q[0] * sc, q[1] * sc)
    for i in (0, 1):
        d.line([P(p['hips'][i]), P(p['knees'][i]), P(FEET[i])], fill=COL['leg'], width=max(1, 2 * sc // 2))
        d.rectangle([FEET[i][0] * sc - 2 * sc, FEET[i][1] * sc - sc, FEET[i][0] * sc + 2 * sc, FEET[i][1] * sc], fill=COL['leg'])
    d.polygon([P(q) for q in p['cape']], fill=COL['body'])
    (hx, hy), r = p['head']
    d.ellipse([(hx - r) * sc, (hy - r) * sc, (hx + r) * sc, (hy + r) * sc], fill=COL['head'])


def draw_arm(d, p, which, sc, dashed=False):
    s, e, h = p['sh_' + which], p['el_' + which], p['hand_' + which]
    pts = [(s[0] * sc, s[1] * sc), (e[0] * sc, e[1] * sc), (h[0] * sc, h[1] * sc)]
    if dashed:
        dash(d, pts, fill=COL[which.upper()][:3] + (150,), width=max(1, sc // 2))
    else:
        d.line(pts, fill=COL[which.upper()], width=max(1, sc))


def draw_sword(d, p, sc, dashed=False):
    P = lambda q: (q[0] * sc, q[1] * sc)
    grip = ((p['hand_a'][0] + p['hand_b'][0]) / 2, (p['hand_a'][1] + p['hand_b'][1]) / 2)
    if dashed:
        dash(d, [P(p['pommel']), P(p['tip'])], fill=COL['hidden'], width=max(1, sc // 2))
        return
    d.line([P(p['pommel']), P(grip)], fill=COL['handle'], width=max(1, sc))
    d.line([P(grip), P(p['tip'])], fill=COL['blade'], width=max(1, sc))


def dash(d, pts, fill, width, on=6, off=4):
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        L = math.hypot(x1 - x0, y1 - y0)
        t = 0
        while t < L:
            t1 = min(t + on, L)
            d.line([(x0 + (x1 - x0) * t / L, y0 + (y1 - y0) * t / L), (x0 + (x1 - x0) * t1 / L, y0 + (y1 - y0) * t1 / L)], fill=fill, width=width)
            t += on + off


def render(k, p, sc, guides):
    """Back view: arms always behind the cape/head (only hands show, over the shoulder like idle or outside the cape).
    Sword 'over' = resting over the shoulder (drawn on top), 'behind' = in front of the body (cape hides it)."""
    im = Image.new('RGBA', (S * sc, S * sc), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    behind = k['depth'] == 'behind'
    draw_arm(d, p, 'a', sc); draw_arm(d, p, 'b', sc)
    if behind:
        draw_sword(d, p, sc)
    draw_body(d, p, sc)
    if not behind:
        draw_sword(d, p, sc)
    mask = body_mask(p)
    for w in 'ab':
        h = p['hand_' + w]
        if not behind or mask.getpixel((int(round(h[0])), int(round(h[1])))) == 0:
            d.rectangle([(h[0] - 1) * sc, (h[1] - 1) * sc, (h[0] + 1) * sc, (h[1] + 1) * sc], fill=COL[w.upper()])
    if guides:   # hidden by the body, not gone: full shape as dashed guide
        if behind: draw_sword(d, p, sc, dashed=True)
        draw_arm(d, p, 'a', sc, dashed=True); draw_arm(d, p, 'b', sc, dashed=True)
    for q, c in ((p['tip'], 'tip'), (((p['hand_a'][0] + p['hand_b'][0]) / 2, (p['hand_a'][1] + p['hand_b'][1]) / 2), 'grip')):
        r = max(1, sc * 0.8)
        d.ellipse([q[0] * sc - r, q[1] * sc - r, q[0] * sc + r, q[1] * sc + r], fill=COL[c])
    return im


def arm_visible_px(k, p):
    """Arm-stick pixels (hands excluded) left uncovered by cape/head at 1x."""
    ar = Image.new('L', (S, S)); da = ImageDraw.Draw(ar)
    for w in 'ab':
        da.line([p['sh_' + w], p['el_' + w], p['hand_' + w]], fill=255)
        h = p['hand_' + w]; da.rectangle([h[0] - 1.5, h[1] - 1.5, h[0] + 1.5, h[1] + 1.5], fill=0)
    m = body_mask(p)
    return sum(1 for y in range(S) for x in range(S) if ar.getpixel((x, y)) and not m.getpixel((x, y)))


def font(n):
    for f in ('C:/Windows/Fonts/malgun.ttf',):
        try:
            return ImageFont.truetype(f, n)
        except OSError:
            pass
    return ImageFont.load_default()


def main(KEYS=KEYS, tag='slash01', OUT=OUT, EAST=EAST):
    N = len(KEYS)
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / 'layers').mkdir(exist_ok=True)
    P = [solve(k) for k in KEYS]
    bg = (60, 63, 72, 255)
    fails = []

    # checks
    for i, (k, p) in enumerate(zip(KEYS, P), 1):
        L = math.dist(p['tip'], ((p['hand_a'][0] + p['hand_b'][0]) / 2, (p['hand_a'][1] + p['hand_b'][1]) / 2))
        if abs(L - TIP_LEN * k['f']) > 0.01: fails.append(f'f{i} sword length {L:.1f}')
        for w in 'ab':
            reach = math.dist(p['sh_' + w], p['hand_' + w])
            if reach > UPPER + FORE + 0.01: fails.append(f'f{i} arm {w.upper()} too short ({reach:.1f})')
        for q in (p['tip'], p['pommel']):
            if not (2 <= q[0] <= S - 3 and 2 <= q[1] <= S - 3): fails.append(f'f{i} out of canvas margin {q}')
    moves = [math.dist(P[i]['tip'], P[i + 1]['tip']) for i in range(N - 1)]
    grip_moves = [math.dist(KEYS[i]['G'], KEYS[i + 1]['G']) for i in range(N - 1)]
    vis = [arm_visible_px(k, p) for k, p in zip(KEYS, P)]
    print('arm px outside cape/head per frame:', vis)
    dup = [i + 1 for i in range(N - 1) if KEYS[i] == KEYS[i + 1]]
    if dup: fails.append(f'duplicate frames {dup}')

    # 1x frames + layers (sword separate)
    sheet = Image.new('RGBA', (S * N, S), (0, 0, 0, 0))
    for i, (k, p) in enumerate(zip(KEYS, P)):
        fr = render(k, p, 1, False)
        sheet.alpha_composite(fr, (i * S, 0))
        sw = Image.new('RGBA', (S, S)); draw_sword(ImageDraw.Draw(sw), p, 1); sw.save(OUT / 'layers' / f'sword_f{i + 1}.png')
        bd = Image.new('RGBA', (S, S)); draw_body(ImageDraw.Draw(bd), p, 1); bd.save(OUT / 'layers' / f'body_f{i + 1}.png')
        ar = Image.new('RGBA', (S, S)); da = ImageDraw.Draw(ar); draw_arm(da, p, 'a', 1); draw_arm(da, p, 'b', 1); ar.save(OUT / 'layers' / f'arms_f{i + 1}.png')
    sheet.save(OUT / f'stick_north_{tag}_{N}f.png')

    # numbered review sheet x4 with hidden guides, pivot, start-pose ghost on f1
    sc = 4
    idle = Image.open(IDLE).convert('RGBA')
    ghost = Image.new('RGBA', (S, S)); ghost.alpha_composite(idle, IDLE_OFS)
    ghost = ghost.resize((S * sc, S * sc), Image.NEAREST)
    ghost.putalpha(ghost.getchannel('A').point(lambda a: a * 90 // 255))
    cols, lab = 4, 70
    rows = (N + cols - 1) // cols
    rev = Image.new('RGBA', (S * sc * cols, (S * sc + lab) * rows), bg)
    dr = ImageDraw.Draw(rev)
    F, f2 = font(22), font(15)
    for i, (k, p) in enumerate(zip(KEYS, P)):
        x0, y0 = (i % cols) * S * sc, (i // cols) * (S * sc + lab)
        cell = Image.new('RGBA', (S * sc, S * sc), bg)
        cd = ImageDraw.Draw(cell)
        for g in range(0, S * sc, 16 * sc): cd.line([(g, 0), (g, S * sc)], fill=(70, 74, 84, 255)); cd.line([(0, g), (S * sc, g)], fill=(70, 74, 84, 255))
        if i == 0 and tag == 'slash01': cell.alpha_composite(ghost)
        cd.line([(0, PIVOT[1] * sc), (S * sc, PIVOT[1] * sc)], fill=(70, 120, 255, 255))
        cd.line([(PIVOT[0] * sc, 0), (PIVOT[0] * sc, S * sc)], fill=(70, 120, 255, 120))
        if i: dash(cd, [(q['tip'][0] * sc, q['tip'][1] * sc) for q in P[:i + 1]], fill=(255, 70, 70, 140), width=2)
        cell.alpha_composite(render(k, p, sc, True))
        rev.alpha_composite(cell, (x0, y0))
        g = k['G']
        dr.text((x0 + 8, y0 + 6), f'{i + 1}', font=font(30), fill=(255, 255, 255, 255))
        dr.text((x0 + 8, y0 + S * sc + 4), f"{i + 1}. {k['stage']}", font=F, fill=(255, 255, 255, 255))
        dr.text((x0 + 8, y0 + S * sc + 34), f"손잡이 ({g[0]},{g[1]})  칼 {k['ang']}°  끝 ({p['tip'][0]:.0f},{p['tip'][1]:.0f})"
                + ('  몸 뒤' if k['depth'] == 'behind' else '') + (f"  화면 길이 {TIP_LEN * k['f']:.0f}px" if k['f'] < 1 else ''),
                font=f2, fill=(220, 220, 220, 255))
    rev.save(OUT / f'stick_north_{tag}_numbered_x4.png')

    # chain GIF 16fps (8 frames only, no smear, no hold)
    gif = []
    for k, p in zip(KEYS, P):
        c = Image.new('RGBA', (S * sc, S * sc), bg)
        c.alpha_composite(render(k, p, sc, True))
        gif.append(c.convert('RGB'))
    gif[0].save(OUT / f'stick_north_{tag}_16fps_x4.gif', save_all=True, append_images=gif[1:], duration=1000 // FPS, loop=0)
    slow = [g.copy() for g in gif]
    slow[0].save(OUT / f'stick_north_{tag}_4fps_x4.gif', save_all=True, append_images=slow[1:], duration=250, loop=0)

    # east 1st hit timing reference (frames side by side, scaled x2; timing only)
    e = Image.open(EAST).convert('RGBA')
    eb = Image.new('RGBA', e.size, bg); eb.alpha_composite(e)
    eb.resize((e.width * 2, e.height * 2), Image.NEAREST).save(OUT / f'ref_east_{tag}_x2.png')

    coords = []
    for i, (k, p) in enumerate(zip(KEYS, P), 1):
        coords.append(dict(frame=i, stage=k['stage'], grip=k['G'], sword_angle=k['ang'], foreshorten=k['f'], depth=k['depth'],
                           tip=[round(v, 1) for v in p['tip']], pommel=[round(v, 1) for v in p['pommel']],
                           hand_A=[round(v, 1) for v in p['hand_a']], hand_B=[round(v, 1) for v in p['hand_b']],
                           elbow_A=[round(v, 1) for v in p['el_a']], elbow_B=[round(v, 1) for v in p['el_b']],
                           shoulder_A=[round(v, 1) for v in p['sh_a']], shoulder_B=[round(v, 1) for v in p['sh_b']],
                           twist=k['twist'], knee_bend=k['bend'], feet=FEET, foot_centre=[(FEET[0][0] + FEET[1][0]) / 2, 93]))
    json.dump(dict(canvas=S, pivot=PIVOT, fps=FPS, frames=N, seconds=N / FPS, tip_len=TIP_LEN, support='both feet planted',
                   tip_moves=[round(m, 1) for m in moves], grip_moves=[round(m, 1) for m in grip_moves], keys=coords),
              open(OUT / 'coords.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=1)

    print('tip moves per frame:', [round(m, 1) for m in moves])
    print('grip moves per frame:', [round(m, 1) for m in grip_moves])
    print('FAIL' if fails else 'OK', fails)
    return 1 if fails else 0


if __name__ == '__main__':
    sys.exit(main())
