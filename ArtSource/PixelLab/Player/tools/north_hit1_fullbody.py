"""North 1st hit, whole body + cape redrawn per frame from the East motion analysis (2026-11-08 user spec).

Kept: north sword path of the 1st hit (pull left -> slash left->right -> brake lower right), order, two-hand grip,
8 frames @16fps. Grip follows the chest, clamped to 3 px from the existing grip.
Body (per frame, 96 px, +x right, +y down; attack direction = screen right and forward = screen up):
  head / chest / pelvis offsets separately (no whole-sprite shifting), torso twist, knee bend from the pelvis drop,
  support foot (left) fixed on the ground, right foot: heel up in prep, steps out+forward on the hit and stays.
Cape in three zones: A neck/shoulders = idle collar pasted at the shoulders (same frame), B middle band and
  C hem keyed per frame (B ~1 frame late, C 1-2 frames late), silhouette and fold lines redrawn every frame.
Out: 34_attack_greatsword_combo_north/hit1_fullbody_v1/ ; exit 0 = all target-range checks pass.
"""
import json, math, os, sys
import numpy as np
from PIL import Image, ImageDraw
import attack_north_dress_trial as T
import attack_north_slash01_stick as m
import attack_north_combo_body_stick as v2
from north_from_east_hit1 import Fig, collar, catmull, c
from north_chop_keyposes import sword, gauntlet, RED, RDK

OUT = os.path.join(T.NORTH, 'hit1_fullbody_v1')
V3 = os.path.join(T.NORTH, 'dress', 'v3_cape')

# neutral (north idle) landmarks, 96 px
SH_L, SH_R, SH_Y = 37.0, 57.0, 64.0
PELVIS = (47.0, 80.0)
ANKLE_L, ANKLE_R = (39.5, 91.0), (49.5, 91.0)
CL, CR, CY = 35, 57, 73                 # collar bottom corners
B0L, B0R = (32.0, 81.0), (60.0, 81.0)   # cape middle band edges
HEM0 = [(29, 89), (37, 90), (45, 90), (53, 90), (61, 88)]
W0 = HEM0[-1][0] - HEM0[0][0]           # 32
L0 = 90 - 66                            # neck -> hem

F = lambda **k: k
# head/chest/pelvis: (dx, dy). twist: - = turned left (opposite of the slash), + = right.
# ankleR: right ankle, heelR: right heel lift px. B: (left edge, right edge) of the middle band. C: 5 hem points.
FRAMES = [
    F(stage='idle', head=(0, 0), chest=(0, 0), pelvis=(0, 0), twist=0.0, ankleR=ANKLE_R, heelR=0,
      B=(B0L, B0R), C=HEM0, lining=None),
    F(stage='준비: 왼쪽으로 틂, 체중 왼발', head=(-0.5, 0), chest=(-1, 0), pelvis=(-1, 0.5), twist=-0.5, ankleR=ANKLE_R, heelR=0,
      B=((32, 81), (60, 81)), C=[(29, 89), (37, 90), (45, 90), (54, 90), (62, 88)], lining=None),          # hem stays (lag)
    F(stage='준비 끝: 머리·골반 1px 낮음, 오른 뒤꿈치 듦', head=(-1, 1), chest=(-1.5, 1), pelvis=(-1.5, 1), twist=-0.9,
      ankleR=ANKLE_R, heelR=1,
      B=((31, 81), (57, 80)), C=[(29, 88), (37, 89), (45, 89), (54, 89), (62, 87)], lining=None),          # 0.95L, hem +2 right of body
    F(stage='베기 통과: 어깨 먼저, 몸 실음', head=(1, 2), chest=(1.5, 2), pelvis=(-0.5, 1), twist=0.4,
      ankleR=(51.5, 89.0), heelR=0,
      B=((30, 82), (58, 82)), C=[(25, 86), (34, 89), (43, 90), (52, 90), (60, 89)], lining=(0, 1)),       # hem trails left, lifts
    F(stage='베기 통과: 골반 따라옴, 망토 펼침', head=(2, 2), chest=(2.5, 2.5), pelvis=(1, 1.5), twist=0.8,
      ankleR=(51.5, 89.0), heelR=0,
      B=((28, 80), (61, 82)), C=[(23, 86), (31, 88), (41, 89), (51, 90), (63, 90)], lining=(0, 2)),       # +7 out left, 4 higher
    F(stage='제동: 1px 되돌아옴, 무릎 굽힘', head=(1, 2), chest=(1.5, 2), pelvis=(1.5, 2), twist=0.9,
      ankleR=(51.5, 89.0), heelR=0,
      B=((28, 81), (60, 82)), C=[(21, 86), (29, 88), (40, 89), (51, 90), (63, 90)], lining=(0, 2)),       # hem goes 2 px further
    F(stage='제동: 자락 방향 바뀜', head=(1.5, 2), chest=(1.5, 2), pelvis=(1.5, 2), twist=0.8,
      ankleR=(51.5, 89.0), heelR=0,
      B=((33, 81), (61, 81)), C=[(28, 88), (36, 90), (46, 90), (55, 89), (64, 86)], lining=(3, 4)),       # rebound right
    F(stage='정착: 넓은 디딤 유지(2타 시작)', head=(1, 1.5), chest=(1, 1.5), pelvis=(1.5, 1.5), twist=0.8,
      ankleR=(51.5, 89.0), heelR=0,
      B=((33, 81), (61, 81)), C=[(29, 89), (37, 90), (46, 90), (54, 90), (63, 88)], lining=None),
]


def body_pts(fr):
    tw = fr['twist']; cx, cy = fr['chest']; cy -= fr.get('lift', 0)   # lift: shoulders rise with the elbows (2nd hit)
    # back view twist: the shoulder turning away moves up/in by ~1 px, the one coming toward the camera down/out
    shL = (SH_L + cx - 0.8 * tw, SH_Y + cy + 1.0 * tw)
    shR = (SH_R + cx - 0.8 * tw, SH_Y + cy - 1.0 * tw)
    px, py = fr['pelvis']
    pel = (PELVIS[0] + px, PELVIS[1] + py)
    return shL, shR, pel


def knee(hip, ankle, side, drop):
    mx, my = (hip[0] + ankle[0]) / 2, (hip[1] + ankle[1]) / 2
    return (mx + side * (0.6 + 0.9 * max(drop, 0)), my - 0.3 * drop)


def grip_for(i, fr, hit='1'):
    """Existing north grip/sword for this frame, moved with the chest (max 3 px)."""
    k, b = v2.SWORD[hit][i], v2.BODY[hit][i]
    G, sp = v2.sword_pose(k, b, None)
    old = (b['px'] + 2 * b['sh'], b['bend'] + b['lean'])
    d = (fr['chest'][0] - old[0], fr['chest'][1] - old[1])
    d = tuple(max(-3, min(3, v)) for v in d)
    mv = lambda p: (p[0] + d[0], p[1] + d[1])
    return k, mv(G), mv(sp['tip']), mv(sp['pommel']), mv(sp['hand_a']), mv(sp['hand_b']), d


def draw_cape(f, fr, cdx, cdy):
    BL, BR = fr['B']; C = fr['C']
    pts = [(CL + cdx, CY + cdy - 1), BL] + list(C) + [BR, (CR + cdx, CY + cdy - 1)]
    f.d.polygon(catmull(pts, 6), fill=c(RED), outline=c(RDK))
    # folds: from the collar through the middle band to the hem, redrawn every frame
    top = [(CL + cdx + (CR - CL) * t, CY + cdy) for t in (0.25, 0.5, 0.75)]
    mid = [((BL[0] + (BR[0] - BL[0]) * t), (BL[1] + BR[1]) / 2) for t in (0.25, 0.5, 0.75)]
    for t0, m0, h in zip(top, mid, C[1:4]):
        f.d.line(catmull([t0, m0, (h[0], h[1] - 1)], 4), fill=c(RDK))
    if fr['lining']:
        a, b = fr['lining']
        seg = C[a:b + 1]
        band = seg + [(x + 1, y - 2) for x, y in seg[::-1]]
        f.d.polygon(band, fill=c(RDK))


def render(i, fr, show_sword=True, hit='1'):
    f = Fig()
    shL, shR, pel = body_pts(fr)
    k, G, tip, pom, hA, hB, gd = grip_for(i, fr, hit)
    behind = k['depth'] == 'behind'
    # 2026-11-08 user: seen from behind the hands are hidden -> no gauntlets drawn
    sw = lambda: sword(f, pom, (G[0] + (tip[0] - G[0]) * 0.1, G[1] + (tip[1] - G[1]) * 0.1), tip)
    if behind and show_sword: sw()
    drop = fr['pelvis'][1]
    hipL, hipR = (pel[0] - 4, pel[1]), (pel[0] + 4, pel[1])
    f.leg(hipL, knee(hipL, ANKLE_L, -1, drop), ANKLE_L, 0)
    aR = fr['ankleR']
    f.leg(hipR, knee(hipR, aR, 1, drop), aR, fr['heelR'])
    f.torso(shL, shR, pel[1], sx=1 - 0.12 * abs(fr['twist']), sy=fr.get('torso_sy', 1.0))
    # arms run under the cape (back view)
    cdx, cdy = fr['chest'][0], fr['chest'][1] - fr.get('lift', 0)
    draw_cape(f, fr, round(cdx), round(cdy))
    collar(f, round(cdx), round(cdy))
    f.head(round(fr['head'][0]), round(fr['head'][1]))
    if not behind and show_sword: sw()
    a = np.array(f.im); a[..., 3] = np.where(a[..., 3] > 0, 255, 0)
    return a, dict(grip=G, tip=tip, grip_shift=gd)


def checks():
    """Target ranges from the user spec (px, 96-base)."""
    fails = []
    f = FRAMES
    hem_w = lambda fr: fr['C'][-1][0] - fr['C'][0][0]
    if not (0 <= f[2]['head'][1] <= 1 and 0 <= f[2]['pelvis'][1] <= 1): fails.append('prep head/pelvis 0-1 lower')
    if not (2 <= f[4]['chest'][0] - f[2]['chest'][0] <= 4.5): fails.append('pass chest 2-3 px toward the slash')
    if not (1 <= f[4]['pelvis'][0] - f[2]['pelvis'][0] <= 2.5): fails.append('pass pelvis 1-2 px')
    if not (f[3]['chest'][0] - f[2]['chest'][0] > f[3]['pelvis'][0] - f[2]['pelvis'][0]): fails.append('shoulders lead pelvis')
    gap0 = ANKLE_R[0] - ANKLE_L[0]; gap = f[4]['ankleR'][0] - ANKLE_L[0] + (ANKLE_R[1] - f[4]['ankleR'][1])
    if not (3 <= gap - gap0 + 1 <= 5): fails.append(f'feet wider 3-5 (side+front) got {gap - gap0}')
    if not (1 <= f[5]['pelvis'][1] <= 2): fails.append('brake pelvis 1-2 lower')
    if not (abs(max(fr['chest'][0] for fr in f) - f[5]['chest'][0]) <= 1.01): fails.append('brake back ~1 px')
    for i in (4, 5):
        w = hem_w(f[i]) / W0
        if not (1.2 <= w <= 1.45): fails.append(f'f{i + 1} hem width {w:.2f}W')
        dh = abs(f[i]['C'][0][1] - f[i]['C'][-1][1])
        if not (2 <= dh <= 4): fails.append(f'f{i + 1} hem side height diff {dh}')
    out_left = HEM0[0][0] - f[4]['C'][0][0]
    if not (4 <= out_left <= 7): fails.append(f'pass hem extra spread {out_left}')
    if not (1 <= f[4]['C'][0][0] - f[5]['C'][0][0] <= 2): fails.append('hem keeps going 1-2 px after the stop')
    fling = HEM0[0][0] - f[5]['C'][0][0]; reb = f[6]['C'][-1][0] - HEM0[-1][0]
    if not (0.3 * fling - 0.5 <= reb <= 0.5 * fling + 0.5): fails.append(f'rebound {reb} vs fling {fling}')
    return fails


def main():
    os.makedirs(os.path.join(OUT, 'frames'), exist_ok=True)
    new, new_ns, meta = [], [], []
    for i, fr in enumerate(FRAMES):
        a, info = render(i, fr); b, _ = render(i, fr, show_sword=False)
        Image.fromarray(a).save(os.path.join(OUT, 'frames', f'f{i + 1}.png')); new.append(a); new_ns.append(b)
        ok, n = T.check(a)
        meta.append(dict(frame=i + 1, stage=fr['stage'], head=fr['head'], chest=fr['chest'], pelvis=fr['pelvis'],
                         twist=fr['twist'], ankleR=fr['ankleR'], heelR=fr['heelR'], cape_B=fr['B'], cape_C=fr['C'],
                         hem_width_W=round((fr['C'][-1][0] - fr['C'][0][0]) / W0, 2),
                         grip=[round(v, 1) for v in info['grip']], grip_shift=[round(v, 1) for v in info['grip_shift']], colours=n))
    Image.fromarray(np.concatenate(new, axis=1)).save(os.path.join(OUT, '1_light_slash_north.png'))
    fails = checks()
    used = set()
    for a in new:
        used |= {tuple(x) for x in a[a[..., 3] > 0][:, :3].tolist()}
        ok, _ = T.check(a)
        if not ok: fails.append('format')
    if len(used) > 16: fails.append(f'palette {len(used)}')
    json.dump(dict(frames=meta, checks_failed=fails), open(os.path.join(OUT, 'coords.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    previews(new, new_ns)
    print('palette', len(used)); print('FAIL' if fails else 'OK', fails)
    sys.exit(1 if fails else 0)


def existing():
    sh = np.array(Image.open(os.path.join(V3, '1_light_slash_north.png')).convert('RGBA'))
    old = [sh[:, i * 96:(i + 1) * 96] for i in range(8)]
    ns = []
    body64, head64, _ = T.build_body()
    for i in range(8):
        if i == 0:
            a = T.place(body64); T.over(a, T.place(head64))   # idle with sword/hands removed
            ns.append(a); continue
        img = np.zeros((96, 96, 4), np.uint8)
        for ln in ('feet', 'legs', 'torso', 'cape', 'head'):
            p = os.path.join(V3, 'layers', f'h1_f{i + 1}_{ln}.png')
            if os.path.exists(p): T.over(img, np.array(Image.open(p).convert('RGBA')))
        ns.append(img)
    return old, ns


def previews(new, new_ns, k=4, bg=(52, 54, 64, 255)):
    old, old_ns = existing()
    Fnt = m.font(20)
    def pair(a, b, la, lb, i):
        im = Image.new('RGB', (96 * k * 2 + 8, 96 * k + 30), (40, 42, 50)); d = ImageDraw.Draw(im)
        for j, x in enumerate((a, b)):
            cell = Image.new('RGBA', (96, 96), bg); cell.alpha_composite(Image.fromarray(np.ascontiguousarray(x)))
            ImageDraw.Draw(cell).line([(0, 94), (96, 94)], fill=(70, 120, 255, 120))
            im.paste(cell.resize((96 * k, 96 * k), Image.NEAREST).convert('RGB'), (j * (96 * k + 8), 30))
        d.text((8, 4), la, font=Fnt, fill='white'); d.text((96 * k + 16, 4), f'{lb}  f{i + 1}', font=Fnt, fill='white')
        return im
    for tag, A, B in (('', old, new), ('_no_sword', old_ns, new_ns)):
        for fps in (16, 4):
            ims = [pair(a, b, '기존 (외형 v3)', '수정', i) for i, (a, b) in enumerate(zip(A, B))]
            ims[0].save(os.path.join(OUT, f'compare_old_vs_new{tag}_{fps}fps_x4.gif'), save_all=True, append_images=ims[1:],
                        duration=1000 // fps, loop=0)
    s = 3; W = 96 * s
    board = Image.new('RGBA', (W * 8, (W + 20) * 4), bg); d = ImageDraw.Draw(board); f12 = m.font(13)
    for r, (lab, row) in enumerate((('기존', old), ('수정', new), ('기존 검 숨김', old_ns), ('수정 검 숨김', new_ns))):
        for i, a in enumerate(row):
            board.alpha_composite(Image.fromarray(np.ascontiguousarray(a)).resize((W, W), Image.NEAREST), (i * W, r * (W + 20)))
            d.text((i * W + 4, r * (W + 20) + W + 2), f'{lab} f{i + 1}', font=f12, fill='white')
    board.save(os.path.join(OUT, 'board_old_new_x3.png'))


if __name__ == '__main__':
    main()
