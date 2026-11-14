"""North greatsword combo, dressed with the approved whole-body stick v2 (2026-11-08). No AI generation.

Layers as in dress v1 (attack_north_dress_trial.py): cape/medal = idle minus idle sword+hands (mask), head = run_calm north,
sword/hands/arms redrawn. Body motion from stick_v2_body/coords.json:
  head  -> shifted to the stick head centre
  cape  -> per-row warp: top rows follow the shoulders (shift, drop, lift), hem follows the lagging hem (shear + bob)
  feet  -> balls fixed; heel lift removes the heel rows, foot rotation slides the heel rows sideways
Hit1 f1 and hit3 f10 = north idle pixels. Smear comes later (user order).
Output: 34_attack_greatsword_combo_north/dress/v2_body/ ; exit 0 = checks passed.
"""
import json, math, os, sys
import numpy as np
from PIL import Image, ImageDraw
import attack_north_dress_trial as T
import attack_north_slash01_stick as m

STICK = os.path.join(T.NORTH, 'stick_v2_body', 'coords.json')
OUT = os.path.join(T.NORTH, 'dress', 'v2_body')
V1 = os.path.join(T.NORTH, 'dress', 'v1')
NAMES = {1: '1_light_slash_north', 2: '2_rising_slash_north', 3: '3_heavy_chop_north'}
IDLE_CAPE_TOP = (m.SHOULDER_C + (m.CAPE_TOP[0] + m.CAPE_TOP[1]) / 2, m.SHOULDER_Y - 2)   # stick v2 cape top centre at idle
IDLE_CAPE_BOT = (m.SHOULDER_C + (m.CAPE_BOTTOM[0] + m.CAPE_BOTTOM[1]) / 2, 90.0)
CAPE_Y0, CAPE_Y1 = 64, 90          # rows of the dressed cape that map to stick top / hem
FOOT_SPLIT_X = 45                  # 96 coords: left foot x < 45
FEET_Y0 = 91                       # feet rows (96 coords) = idle rows 51+


def warp_cape(cape96, k):
    top = np.mean([q[0] for q in k['cape'][:2]]), k['cape'][0][1]
    bot = np.mean([q[0] for q in k['cape'][2:]]), k['cape'][2][1]
    tdx, tdy = top[0] - IDLE_CAPE_TOP[0], top[1] - IDLE_CAPE_TOP[1]
    bdx, bdy = bot[0] - IDLE_CAPE_BOT[0], bot[1] - IDLE_CAPE_BOT[1]
    out = np.zeros_like(cape96)
    for ys in np.arange(0, 96, 0.25):
        t = min(max((ys - CAPE_Y0) / (CAPE_Y1 - CAPE_Y0), 0), 1)
        dx, dy = tdx + (bdx - tdx) * t, tdy + (bdy - tdy) * t
        sy, yd = int(ys), int(round(ys + dy))
        if not (0 <= yd < 96): continue
        row = cape96[sy]; sh = int(round(dx))
        new = np.zeros_like(row)
        if sh >= 0: new[sh:] = row[:96 - sh]
        else: new[:sh] = row[-sh:]
        mask = new[:, 3] > 0
        out[yd][mask] = new[mask]
    return out


def move_feet(feet96, k):
    out = np.zeros_like(feet96)
    for i, xs in ((0, slice(0, FOOT_SPLIT_X)), (1, slice(FOOT_SPLIT_X, 96))):
        part = np.zeros_like(feet96); part[:, xs] = feet96[:, xs]
        ys = np.where(part[..., 3].any(axis=1))[0]
        if len(ys) == 0: continue
        heel = k['body']['heel'][i]; rot = k['body']['rot'][i]
        y_last = ys.max()
        for y in ys:
            if y > y_last - heel:                 # heel lifted: back rows disappear (seen from behind, above)
                continue
            depth = y - ys.min()                  # 0 at the ball row
            sh = -int(round(math.sin(math.radians(rot)) * depth))   # + rot: heel swings to screen left (stick v2)
            row = np.zeros_like(part[y]); src = part[y]
            if sh >= 0: row[sh:] = src[:96 - sh]
            else: row[:sh] = src[-sh:]
            msk = row[:, 3] > 0; out[y][msk] = row[msk]
    return out


def sword_k(k):
    """Map stick v2 coords to the field names T.draw_* expect."""
    return dict(grip=k['grip'], tip=k['tip'], pommel=k['pommel'], foreshorten=k['foreshorten'],
                hand_A=k['hand_A'], hand_B=k['hand_B'], elbow_A=k['elbow_A'], elbow_B=k['elbow_B'],
                shoulder_A=k['shoulders'][0], shoulder_B=k['shoulders'][1])


def main():
    os.makedirs(os.path.join(OUT, 'layers'), exist_ok=True)
    keys = json.load(open(STICK, encoding='utf-8'))['keys']
    body64, head64, _ = T.build_body()
    torso64, feet64 = T.split_feet(body64)
    cape0, feet0, head0 = T.place(torso64), T.place(feet64), T.place(head64)
    idle = np.zeros((160, 160, 4), np.uint8)
    idle[T.OFF[1]:T.OFF[1] + 64, T.OFF[0]:T.OFF[0] + 64] = np.array(Image.open(T.IDLE).convert('RGBA'))
    idle = idle[:96, :96].copy()
    seq, ok_all, used, meta = [], True, set(), {}
    for k in keys:
        hit, f, b = k['hit'], k['frame'], k['body']
        tag = f'h{hit}_f{f}'
        if (hit, f) in ((1, 1), (3, 10)):
            img, order = idle.copy(), ['idle']
        else:
            hx = int(round(k['head'][0] - m.HEAD[0][0])); hy = int(round(k['head'][1] - m.HEAD[0][1]))
            head = np.zeros_like(head0)
            src = head0[max(0, -hy):96 - max(0, hy), max(0, -hx):96 - max(0, hx)]
            head[max(0, hy):max(0, hy) + src.shape[0], max(0, hx):max(0, hx) + src.shape[1]] = src
            sk = sword_k(k)
            L = dict(feet=move_feet(feet0, k), cape=warp_cape(cape0, k), head=head,
                     arms=T.draw_arms(sk), hands=T.draw_hands(sk), sword=T.draw_sword(sk))
            lift = b.get('lift', 0)
            if k['depth'] == 'over':
                order = ['feet', 'cape', 'arms', 'head', 'sword', 'hands'] if lift else ['feet', 'cape', 'head', 'sword', 'hands']
            else:
                order = ['sword', 'hands', 'feet', 'cape', 'head']
            img = np.zeros((96, 96, 4), np.uint8)
            for ln in order:
                T.over(img, L[ln])
            ball0 = feet0[..., 3].any(axis=1).argmax()          # top (ball) row of the feet: must never move
            if not np.array_equal(L['feet'][ball0], feet0[ball0]):
                print('FAIL feet slide', tag); ok_all = False
            for ln, a in L.items():
                Image.fromarray(a).save(os.path.join(OUT, 'layers', f'{tag}_{ln}.png'))
        ok, n = T.check(img); ok_all &= ok
        if not ok: print('FAIL', tag, n)
        used |= {tuple(c) for c in img[img[..., 3] > 0][:, :3].tolist()}
        seq.append((hit, f, img)); meta[tag] = dict(order=order, head_shift=None if order == ['idle'] else [hx, hy])
    # sheets (shared frames repeated)
    per = {1: [], 2: [], 3: []}
    for hit, f, img in seq: per[hit].append(img)
    per[2].insert(0, per[1][-1]); per[3].insert(0, per[2][-1])
    for hit, ims in per.items():
        Image.fromarray(np.concatenate(ims, axis=1)).save(os.path.join(OUT, NAMES[hit] + '.png'))
    print('frames', len(seq), 'palette', len(used))
    ok_all &= len(used) <= 16 and len(per[1]) == 8 and len(per[2]) == 8 and len(per[3]) == 10
    json.dump(dict(stick=STICK, frames=meta), open(os.path.join(OUT, 'coords.json'), 'w', encoding='utf-8'), indent=1)
    previews([img for *_, img in seq], per)
    sys.exit(0 if ok_all else 1)


def previews(chain, per, k=4, bg=(52, 54, 64, 255)):
    def up(a):
        b = Image.new('RGBA', (96, 96), bg); b.alpha_composite(Image.fromarray(a)); return b.resize((96 * k, 96 * k), Image.NEAREST)
    ims = [up(a).convert('RGB') for a in chain]
    for fps in (16, 4):
        ims[0].save(os.path.join(OUT, f'chain_north_body_{fps}fps_x4.gif'), save_all=True, append_images=ims[1:], duration=1000 // fps, loop=0)
    # v1 (approved look, still body) vs v2 body, side by side
    v1 = []
    for hit in (1, 2, 3):
        sh = np.array(Image.open(os.path.join(V1, NAMES[hit] + '.png')).convert('RGBA'))
        fr = [sh[:, i * 96:(i + 1) * 96] for i in range(sh.shape[1] // 96)]
        v1 += fr if hit == 1 else fr[1:]
    F = m.font(22); pair = []
    for a, b in zip(v1, chain):
        im = Image.new('RGB', (96 * k * 2 + 8, 96 * k + 34), (40, 42, 50))
        im.paste(up(a).convert('RGB'), (0, 34)); im.paste(up(b).convert('RGB'), (96 * k + 8, 34))
        d = ImageDraw.Draw(im); d.text((8, 4), '외형 v1 (몸 고정)', font=F, fill='white'); d.text((96 * k + 16, 4), '외형 v2 (전신 움직임)', font=F, fill='white')
        pair.append(im)
    pair[0].save(os.path.join(OUT, 'compare_v1_vs_v2_16fps_x4.gif'), save_all=True, append_images=pair[1:], duration=1000 // 16, loop=0)
    # numbered board x3
    s = 3; W = 96 * s; f = m.font(12)
    board = Image.new('RGBA', (W * 10, (W + 16) * 3), bg); d = ImageDraw.Draw(board)
    for r, hit in enumerate((1, 2, 3)):
        for i, a in enumerate(per[hit]):
            board.alpha_composite(Image.fromarray(a).resize((W, W), Image.NEAREST), (i * W, r * (W + 16)))
            d.text((i * W + 4, r * (W + 16) + W + 1), f'{hit}타 f{i + 1}', font=f, fill='white')
            d.line([(0, r * (W + 16) + 94 * s), (W * 10, r * (W + 16) + 94 * s)], fill=(70, 120, 255, 120))
    board.save(os.path.join(OUT, 'board_numbered_x3.png'))


if __name__ == '__main__':
    main()
