"""6방향 대검 3연격 막대기 밑그림 (2026-11-13).

동쪽 확정본(33_attack_greatsword_combo_east)의 박자를 몸 기준 3D 좌표(F 앞, R 오른쪽, U 위)로 적고
방향마다 high top-down으로 투영한다. 완성 그림의 평면 회전·좌우 반전은 쓰지 않는다.
1타 f1·3타 f9~f10은 그 방향 idle에서 잰 자루·검끝을 그대로 쓴다.

python combo6_stick.py   → 35_attack_greatsword_combo_6dir/01_stick/
"""
import json, math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
IDLE = ROOT / '16_idle_greatsword_pauldron_breath'
EAST = ROOT / '33_attack_greatsword_combo_east'
OUT = ROOT / '35_attack_greatsword_combo_6dir' / '01_stick'
NE_STUDY = Path.home() / 'Documents/ChatGPT/시간 게임 아트/outputs/ne-hit1-poses/facing-ne-v2/ne-hit1-study-8f.png'

CELL, PX, PY = 104, 52, 94           # 셀, 피벗
IDLE_OFF = (20, 40)                  # 64 idle → 104 셀 (승인 NE 밑그림과 같은 배치)
K_GROUND, K_UP = 0.5, 1.0            # 지면 깊이 단축, 높이 배율
BLADE = 30                           # 자루 중심 → 검끝 (동쪽 대조·승인 NE f1 32px로 맞춤)
FPS = 16

DIRS = {  # 화면 기준 바라보는 각도(오른쪽 0°, 위 90°)와 idle 파일 번호
    'north-east': (45, 1), 'north-west': (135, 3), 'west': (180, 4),
    'south-west': (225, 5), 'south': (270, 6), 'south-east': (315, 7), 'east': (0, 0),
}
ORDER = ['north-east', 'north-west', 'west', 'south-west', 'south', 'south-east']

# 프레임: grip(F,R,U), 검 방향 D(F,R,U), yaw(+ = 오른 어깨가 앞으로), crouch(px), 발 L·R(F,R), idle 혼합(0~1)
IDLE_FEET = ((2, -3), (-1, 3))
def P(g, d, yaw=0, cr=0, fl=(2, -3), fr=(-1, 3), idle=0.0, hit=False):
    return dict(grip=g, d=d, yaw=yaw, cr=cr, fl=fl, fr=fr, idle=idle, hit=hit)

HIT1 = [
    P((5, 3, 17), (0.6, 0.1, 0.8), idle=1.0),                         # f1 idle
    P((3, 4, 19), (0.4, 0.2, 0.9), -10, 1, idle=0.5),                 # f2 무게 싣기
    P((0, 6, 22), (0.2, 0.4, 0.9), -25, 1, fr=(-2, 4)),               # f3 준비: 오른쪽에 검 세움
    P((9, 2, 14), (1.0, -0.15, -0.05), 10, 3, fl=(6, -3), hit=True),  # f4 낮은 횡베기
    P((6, -4, 13), (-0.6, -0.7, -0.4), 30, 3, fl=(6, -3)),            # f5 반대편 통과
    P((2, -3, 17), (-0.85, -0.3, 0.25), 25, 2, fl=(5, -3)),           # f6 제동
    P((2, -3, 17.5), (-0.85, -0.3, 0.25), 25, 1.5, fl=(5, -3)),
    P((2, -3, 18), (-0.85, -0.3, 0.25), 25, 1, fl=(5, -3)),           # f8 = 2타 f1
]
HIT2 = [
    HIT1[-1],
    P((1, -3, 12), (-0.7, -0.4, -0.6), 25, 3, fl=(5, -3)),            # f2 낮춤
    P((7, 1, 22), (0.5, 0.3, 0.8), 0, 1, fl=(5, -3), fr=(1, 3), hit=True),  # f3 올려베기
    P((3, 2, 30), (0.3, 0.1, 0.95), -10, 0, fl=(5, -3), fr=(1, 3)),   # f4 머리 위
    P((1, 2, 27), (0.35, 0.1, 0.9), -10, 0, fl=(4, -3), fr=(0, 3)),
    P((0, 2, 28), (0.3, 0.1, 0.95), -10, 0, fl=(4, -3), fr=(0, 3)),
    P((-1, 1, 31), (-0.3, 0, 0.95), -10, 0, fl=(4, -3), fr=(0, 3)),   # f7 높은 검
    P((-1, 1, 31), (-0.3, 0, 0.95), -10, 0, fl=(4, -3), fr=(0, 3)),   # f8 = 3타 f1
]
HIT3 = [
    HIT2[-1],
    P((8, 1, 16), (0.8, 0, -0.6), 5, 4, fl=(7, -3), fr=(0, 3), hit=True),  # f2 내려치기
    P((9, 1, 10), (0.9, 0, -0.35), 5, 5, fl=(7, -3), fr=(0, 3)),      # f3 낮은 자세
    P((8, 1, 10), (0.95, 0, -0.3), 5, 4, fl=(7, -3), fr=(0, 3)),
    P((7, 1, 11), (0.95, 0, -0.2), 5, 4, fl=(6, -3), fr=(0, 3)),
    P((6, 2, 11), (0.98, 0.1, -0.1), 3, 3, fl=(5, -3)),
    P((6, 2, 12), (0.95, 0.1, 0.1), 2, 2.5, fl=(4, -3)),
    P((6, 3, 14), (0.85, 0.1, 0.4), 0, 1.5, idle=0.5),                # f8 회수
    P((5, 3, 17), (0.6, 0.1, 0.8), idle=1.0),                         # f9 idle
    P((5, 3, 17), (0.6, 0.1, 0.8), idle=1.0),                         # f10 idle
]
HITS = [('hit1', HIT1), ('hit2', HIT2), ('hit3', HIT3)]


def proj(phi, f, r, u):
    a = math.radians(phi)
    east = f * math.cos(a) + r * math.sin(a)
    north = f * math.sin(a) - r * math.cos(a)
    return (PX + east, PY - north * K_GROUND - u * K_UP), north


def rot(f, r, yaw):  # 몸 기준 yaw 회전
    a = math.radians(yaw)
    return f * math.cos(a) - r * math.sin(a), f * math.sin(a) + r * math.cos(a)


def idle_sword(file_no, name):
    """idle 그림의 회색 검날에서 자루 중심·검끝(104 셀 좌표)."""
    a = np.array(Image.open(IDLE / f'{file_no}_{name}_1.png').convert('RGBA'))
    rgb = a[..., :3].astype(int)
    gray = (a[..., 3] > 0) & (rgb.max(-1) - rgb.min(-1) < 25) & (rgb.max(-1) > 90)
    ys, xs = np.nonzero(gray)
    pts = np.stack([xs, ys], 1).astype(float)
    c = pts.mean(0)
    v = np.linalg.svd(pts - c)[2][0]
    t = (pts - c) @ v
    e1, e2 = c + v * t.min(), c + v * t.max()
    body = np.array([32, 40])
    guard, tip = (e1, e2) if np.linalg.norm(e1 - body) < np.linalg.norm(e2 - body) else (e2, e1)
    u = (tip - guard) / np.linalg.norm(tip - guard)
    grip = tip - u * 31  # 승인 NE f1 자루(54.5,73.5)와 맞춤.   회색이 몸 쪽에도 있어 guard 끝은 믿지 않음(11/13 남쪽)
    off = np.array(IDLE_OFF)
    return (grip + off).tolist(), (tip + off).tolist()


def pose2d(phi, p, idle_grip_tip):
    yaw, cr = p['yaw'], p['cr']
    pts, depth = {}, {}
    def put(k, f, r, u, turn=True):
        if turn:
            f, r = rot(f, r, yaw)
        pts[k], depth[k] = proj(phi, f, r, u)
    put('hip', 0, 0, 18 - cr)
    put('neck', 0, 0, 31 - cr)
    put('head', 0.5, 0, 37 - cr)
    put('shR', 0, 5, 29 - cr); put('shL', 0, -5, 29 - cr)
    put('hipR', 0, 3, 18 - cr); put('hipL', 0, -3, 18 - cr)
    for s, (f, r) in (('L', p['fl']), ('R', p['fr'])):
        put('foot' + s, f, r, 0, turn=False)
        hf, hr = rot(0, 3 if s == 'R' else -3, yaw)
        put('knee' + s, (f + hf) / 2 + 1.5, (r + hr) / 2, (18 - cr) / 2)
    g = p['grip']
    d = np.array(p['d'], float); d /= np.linalg.norm(d)
    gf, gr = g[0], g[1]
    put('grip', gf, gr, g[2], turn=False)
    put('tip', gf + d[0] * BLADE, gr + d[1] * BLADE, g[2] + d[2] * BLADE, turn=False)
    put('pommel', gf - d[0] * 4, gr - d[1] * 4, g[2] - d[2] * 4, turn=False)
    # 오른손(주손)은 자루 위쪽, 왼손은 아래쪽
    put('handR', gf + d[0] * 1.5, gr + d[1] * 1.5, g[2] + d[2] * 1.5, turn=False)
    put('handL', gf - d[0] * 1.5, gr - d[1] * 1.5, g[2] - d[2] * 1.5, turn=False)
    if p['idle'] > 0:  # idle 자루·검끝으로 당김(화면 좌표)
        ig, it = idle_grip_tip
        w = p['idle']
        sg, st = np.array(pts['grip']), np.array(pts['tip'])
        ng = sg * (1 - w) + np.array(ig) * w
        nt = st * (1 - w) + np.array(it) * w
        u = (nt - ng) / max(np.linalg.norm(nt - ng), 1e-6)
        pts['grip'], pts['tip'] = tuple(ng), tuple(nt)
        pts['pommel'] = tuple(ng - u * 4)
        pts['handR'] = tuple(ng + u * 1.5)
        pts['handL'] = tuple(ng - u * 1.5)
    # 여백 4px: 검끝이 y=99 아래로 가면 자루는 두고 검만 들어 올림(11/13, 남쪽 계열 3타)
    gx, gy = pts['grip']; tx, ty = pts['tip']
    if ty > CELL - 5:
        L = math.hypot(tx - gx, ty - gy)
        ny = CELL - 5
        nx = gx + math.copysign(math.sqrt(max(L * L - (ny - gy) ** 2, 0)), tx - gx if tx != gx else 1)
        pts['tip'] = (nx, ny)
        u = (np.array(pts['tip']) - np.array(pts['grip'])) / L
        pts['pommel'] = tuple(np.array(pts['grip']) - u * 4)
        pts['lifted'] = (1, 1)
    for s in 'RL':
        sh, h = np.array(pts['sh' + s]), np.array(pts['hand' + s])
        m = (sh + h) / 2 + np.array([0, 2])
        pts['elbow' + s] = tuple(m)
        depth['elbow' + s] = (depth['sh' + s] + depth['grip']) / 2
        depth['hand' + s] = depth['grip']
    # 망토: 목·어깨에 붙고 뒤로, 직전 몸 회전의 반대쪽으로 늦게 따라옴
    return pts, depth


def angle(grip, tip):
    return round(math.degrees(math.atan2(-(tip[1] - grip[1]), tip[0] - grip[0])) % 360, 1)


def cape_poly(phi, p, lag):
    yaw, cr = p['yaw'], p['cr']
    pts = []
    for f, r, u in ((-1, 5, 30), (-4, 6, 18), (-7 - lag[0], 5 + lag[1], 2), (-8 - lag[0], 0 + lag[1], 1),
                    (-7 - lag[0], -5 + lag[1], 2), (-4, -6, 18), (-1, -5, 30)):
        ff, rr = rot(f, r, yaw)
        uu = u - (cr if u > 10 else 0)
        pts.append(proj(phi, ff, rr, uu)[0])
    north = proj(phi, *rot(-5, 0, yaw), 15)[1]
    return pts, north


COL = dict(body='#3a3440', bodyo='#8a8090', head='#e8c860', legL='#5a5468', legR='#9a90a8',
           armL='#7a6fb0', armR='#e9d7b4', handL=(172, 153, 255), handR=(255, 244, 168),
           blade=(145, 241, 233), pommel=(244, 188, 96), cape=(112, 30, 40), capeo=(160, 60, 70),
           medal=(240, 200, 60), hit=(255, 80, 80))


def draw_frame(phi, p, prev, idle_gt):
    pts, depth = pose2d(phi, p, idle_gt)
    lag = (0.0, 0.0)
    if prev is not None:
        dy = p['yaw'] - prev['yaw']
        lag = (min(abs(dy) / 10, 3), -dy / 8)
    cape, cape_n = cape_poly(phi, p, lag)
    im = Image.new('RGBA', (CELL, CELL))
    d = ImageDraw.Draw(im)
    torso = [pts['shL'], pts['shR'], pts['hipR'], pts['hipL']]
    items = [
        (cape_n, 0, lambda: d.polygon(cape, fill=COL['cape'], outline=COL['capeo'])),
        (depth['footL'], 1, lambda: d.line([pts['hipL'], pts['kneeL'], pts['footL']], fill=COL['legL'], width=2)),
        (depth['footR'], 1, lambda: d.line([pts['hipR'], pts['kneeR'], pts['footR']], fill=COL['legR'], width=2)),
        (depth['hip'], 2, lambda: d.polygon(torso, fill=COL['body'], outline=COL['bodyo'])),
        (depth['head'] - 0.01, 3, lambda: d.ellipse([pts['head'][0] - 4, pts['head'][1] - 4, pts['head'][0] + 4, pts['head'][1] + 4], fill=COL['head'])),
        (depth['shL'], 4, lambda: d.rectangle([pts['shL'][0] - 1, pts['shL'][1] - 1, pts['shL'][0] + 1, pts['shL'][1] + 1], fill=COL['medal'])),
        (depth['elbowL'], 5, lambda: d.line([pts['shL'], pts['elbowL'], pts['handL']], fill=COL['armL'], width=2)),
        (depth['grip'], 6, lambda: (d.line([pts['pommel'], pts['tip']], fill=COL['blade'], width=1),
                                    d.point(pts['pommel'], fill=COL['pommel']))),
        (depth['elbowR'], 7, lambda: d.line([pts['shR'], pts['elbowR'], pts['handR']], fill=COL['armR'], width=2)),
        (depth['grip'] - 0.02, 8, lambda: (d.point(pts['handL'], fill=COL['handL']), d.point(pts['handR'], fill=COL['handR']))),
    ]
    for _, _, fn in sorted(items, key=lambda t: (-t[0], t[1])):
        fn()
    if p['hit']:
        d.rectangle([1, 1, 3, 3], fill=COL['hit'])
    rec = {k: [round(v[0], 1), round(v[1], 1)] for k, v in pts.items()
           if k in ('grip', 'tip', 'handR', 'handL', 'head', 'hip', 'footL', 'footR', 'shL')}
    rec['angle'] = angle(pts['grip'], pts['tip'])
    rec['tip_lifted_for_margin'] = 'lifted' in pts
    order = [n for _, n in sorted([(depth['elbowL'], 'armL'), (depth['grip'], 'sword'), (depth['elbowR'], 'armR'),
                                   (depth['hip'], 'torso'), (cape_n, 'cape'), (depth['shL'], 'medal')], key=lambda t: -t[0])]
    rec['draw_order_far_to_near'] = order
    return im, rec


def render_dir(name):
    phi, no = DIRS[name]
    idle_gt = idle_sword(no, name)
    frames, recs, prev = [], [], None
    for hit, seq in HITS:
        for i, p in enumerate(seq, 1):
            im, rec = draw_frame(phi, p, prev, idle_gt)
            rec.update(hit=hit, frame=i, visual_hit=p['hit'])
            frames.append(im); recs.append(rec); prev = p
    return frames, recs


def bg(im, k):
    b = Image.new('RGBA', im.size, (90, 110, 90, 255)); b.alpha_composite(im)
    return b.resize((im.width * k, im.height * k), Image.NEAREST)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    all_frames, data = {}, {'cell': CELL, 'pivot': [PX, PY], 'fps': FPS, 'frames': [8, 8, 10],
                            'note': '좌표는 104 셀 화면 좌표. angle: 자루→검끝, 오른쪽 0° 위 90°'}
    for name in ORDER + ['east']:
        frames, recs = render_dir(name)
        all_frames[name] = frames
        data[name] = recs
        sheet = Image.new('RGBA', (CELL * 26, CELL))
        for i, f in enumerate(frames):
            sheet.alpha_composite(f, (i * CELL, 0))
        sheet.save(OUT / f'stick_{name}_26f.png')
    (OUT / 'stick_poses.json').write_text(json.dumps(data, ensure_ascii=False, indent=1), encoding='utf-8')

    # 6방향 묶음 GIF (3×2, 3배, 16fps). 위 줄 NE·NW·W, 아래 줄 SW·S·SE
    k = 3
    gif = []
    for i in range(26):
        c = Image.new('RGBA', (CELL * 3, CELL * 2))
        for j, n in enumerate(ORDER):
            c.alpha_composite(all_frames[n][i], ((j % 3) * CELL, (j // 3) * CELL))
        gif.append(bg(c, k).convert('P', palette=Image.ADAPTIVE))
    gif[0].save(OUT / 'stick_6dir_16fps_x3.gif', save_all=True, append_images=gif[1:],
                duration=int(1000 / FPS), loop=0, disposal=2)

    # 동쪽 대조: 동쪽 투영 막대기 위에 확정본(96→104, +4,+1)
    rows = []
    for hit, n, cnt in (('1', '1_light_slash_east', 8), ('2', '2_rising_slash_east', 8), ('3', '3_heavy_chop_east', 10)):
        src = Image.open(EAST / f'{n}.png').convert('RGBA')
        rows.append([(src.crop((i * 96, 0, i * 96 + 96, 96)), i) for i in range(cnt)])
    cmp_ = Image.new('RGBA', (CELL * 10, CELL * 3))
    idx = 0
    for r, row in enumerate(rows):
        for img, i in row:
            cell = Image.new('RGBA', (CELL, CELL))
            ghost = img.copy(); ghost.putalpha(ghost.getchannel('A').point(lambda a: 110 if a else 0))
            cell.alpha_composite(ghost, (4, 1))
            cell.alpha_composite(all_frames['east'][idx])
            cmp_.alpha_composite(cell, (i * CELL, r * CELL)); idx += 1
    bg(cmp_, 3).save(OUT / 'check_east_projection_x3.png')

    # NE 1타: 승인 밑그림 vs 투영
    if NE_STUDY.exists():
        st = Image.open(NE_STUDY).convert('RGBA')
        c = Image.new('RGBA', (CELL * 8, CELL * 2))
        c.alpha_composite(st, (0, 0))
        for i in range(8):
            c.alpha_composite(all_frames['north-east'][i], (i * CELL, CELL))
        bg(c, 3).save(OUT / 'check_ne_hit1_study_vs_stick_x3.png')

    # 6방향 핵심 자세(1타 f3·f4·f6, 2타 f3·f7, 3타 f2·f3) 한 장
    keys = [(0, 3), (0, 4), (0, 6), (1, 3), (1, 7), (2, 2), (2, 3), (2, 10)]
    base = {0: 0, 1: 8, 2: 16}
    c = Image.new('RGBA', (CELL * len(keys), CELL * 6))
    for r, n in enumerate(ORDER):
        for j, (h, f) in enumerate(keys):
            c.alpha_composite(all_frames[n][base[h] + f - 1], (j * CELL, r * CELL))
    bg(c, 2).save(OUT / 'keyposes_6dir_x2.png')
    print('ok', OUT)


if __name__ == '__main__':
    main()
