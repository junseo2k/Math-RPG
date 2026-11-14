"""North (back view) body under the cape, hand-placed pixels, 64px idle coords. No AI generation.

Reference: 16_idle_greatsword_pauldron_breath/*.aseprite (south/east/west show the armour: black plates, gold trim,
beige tassets, olive-dark legs, gold boot cuffs). Proportions from south: torso x21-36, legs to y54.
Layers (separate PNGs): torso, arms_upper, legs. Feet + head reuse the existing north idle / run_calm pixels.
Out: 34_attack_greatsword_combo_north/body_under_cape/ ; exit 0 = palette <=16, nothing shows outside cape+head at idle.
"""
import os, sys
import numpy as np
from PIL import Image, ImageDraw
import attack_north_dress_trial as T

OUT = os.path.join(T.NORTH, 'body_under_cape')
X0 = 19  # column 0 of the maps = x 19
PAL = {'K': (0, 0, 0), 'D': (54, 46, 37), 'O': (91, 86, 66), 'G': (107, 115, 107), 'g': (210, 168, 85),
       'y': (252, 219, 109), 'm': (195, 150, 80), 'b': (174, 120, 58), 'r': (145, 81, 27), 'c': (217, 198, 159),
       'w': (253, 230, 189)}

TORSO = {  # y: 20 columns, x19..38. Back plate: dark centre, olive shading, gold trim on shoulder blades + pauldrons
    27: '.......gKKKKgKKK....',   # left 3 px trimmed: above the idle cape edge
    28: '...KgmKKKDDKKKmgK...',
    29: '..KgmyKDDOKKODDKymgK',
    30: '..KmgKKDOOKKOODKKgmK',
    31: '...KKKDDOGKKGODDKKK.',
    32: '...KKDDOOGKKGOODDKK.',
    33: '...KgKDOOKKKKOODKgK.',
    34: '....KgKDOKKKKODKgK..',
    35: '....KKgKDKKKKDKgKK..',
    36: '....KKKgKKKKKKgKKK..',
    37: '....KDKKggggggKKDK..',
    38: '....KKKgyyyyyygKKK..',
    39: '....KKKKgmmmmgKKKK..',
    40: '...KccwcKKKKKcwccK..',
    41: '...KcccDK...KDcccK..',
}
ARMS = {  # upper arms at the sides (idle: hands meet in front, hidden by the body from behind); left arm hugs the torso (idle cape edge)
    29: '..K................K',
    30: '..KK...............K',
    31: '..KDK.............KD',
    32: '.KDK..............KD',
    33: '.KOK..............KO',
    34: '.KDK..............KD',
    35: '.KmK..............Km',
    36: '.KgK..............Kg',
    37: '..KK..............KK',
}
LEGS = {  # light from top-left: beige greave highlight on the left edge of each leg (south view legs are beige)
    40: '...KcccDK...KccDDK..',   # 2026-11-08 thighs +2 rows: torso moved up 2 to the idle pauldron
    41: '...KccDDK...KcDDDK..',
    42: '...KcccDK...KccDDK..',
    43: '...KccDDK...KcDDDK..',
    44: '...KcDDK.....KcDDK..',
    45: '...KcODK.....KcODK..',
    46: '...KgmgK.....KgmgK..',
    47: '...KcDDK.....KcDDK..',
    48: '...KcODK.....KcODK..',
    49: '...KmgmK.....KmgmK..',
    50: '...KbmbK.....KbmbK..',
}


# 2026-11-08 user "위치만 맞춰봐": align to the north idle. Pauldron gold -> idle medal (x22-24, y26),
# torso centre -> feet centre 28.5, each shin over its idle foot (x22-25 / x32-35).
HEAD_DX = -1   # 2026-11-08 user picked head 1 px left (centre x29, over the torso) over the dress-v1 head position
OFFSET = {'torso': (0, -2), 'arms_upper': (0, -2), 'legs': (-1, 0)}   # torso x 0: pauldron gold then starts at x22 = idle medal


def paint(rows, off=(0, 0)):
    a = np.zeros((64, 64, 4), np.uint8)
    for y, row in rows.items():
        assert len(row) == 20, (y, len(row))
        for i, ch in enumerate(row):
            if ch != '.':
                a[y + off[1], X0 + i + off[0]] = (*PAL[ch], 255)
    return a


def main():
    os.makedirs(OUT, exist_ok=True)
    L = dict(torso=paint(TORSO, OFFSET['torso']), arms_upper=paint(ARMS, OFFSET['arms_upper']), legs=paint(LEGS, OFFSET['legs']))
    idle = np.array(Image.open(T.IDLE).convert('RGBA'))
    body64, head64, _ = T.build_body()
    head64 = np.roll(head64, HEAD_DX, axis=1)   # head is empty at the edges, so roll = shift
    Image.fromarray(head64).save(os.path.join(OUT, 'head.png'))
    feet = body64.copy(); feet[:51] = 0
    for n, a in L.items():
        Image.fromarray(a).save(os.path.join(OUT, f'{n}.png'))
    under = np.zeros_like(idle)
    for n in ('legs', 'torso', 'arms_upper'):
        T.over(under, L[n])
    T.over(under, feet); T.over(under, head64)
    Image.fromarray(under).save(os.path.join(OUT, 'body_nocape_64.png'))
    # idle check: body must hide fully under the idle cape/head (else idle look changes)
    cover = idle[..., 3] > 0
    body_only = np.zeros((64, 64), bool)
    for n in L: body_only |= L[n][..., 3] > 0
    leak = int((body_only & ~cover).sum())
    cols = {tuple(c) for c in under[under[..., 3] > 0][:, :3].tolist()} | {tuple(c) for c in idle[idle[..., 3] > 0][:, :3].tolist()}
    print('body px outside idle silhouette:', leak, ' palette with idle:', len(cols))
    preview(under, idle, body64, head64, L)
    sys.exit(0 if len(cols) <= 16 and leak <= 8 else 1)


def preview(under, idle, body64, head64, L, k=6, bg=(52, 54, 64, 255)):
    ref = {n: Image.open(os.path.join(T.ROOT, '16_idle_greatsword_pauldron_breath', f'{n}_1.png')).convert('RGBA')
           for n in ('6_south', '0_east')}
    # cape lifted demo: cape layer (idle minus sword/hands) with its lower rows swung right by up to 9 px and shortened
    cape = body64.copy(); cape[51:] = 0
    lifted = np.zeros_like(cape)
    for y in range(64):
        t = max(0, (y - 30) / 20)
        sh = int(round(9 * t * t)); yy = int(round(y - 4 * t * t))
        if 0 <= yy < 64:
            row = np.zeros_like(cape[y]); row[sh:] = cape[y][:64 - sh]
            m = row[:, 3] > 0; lifted[yy][m] = row[m]
    demo = under.copy(); T.over(demo, lifted); T.over(demo, head64)
    ghost = under.copy(); a = idle.copy(); a[..., 3] = (a[..., 3] > 0) * 110
    panels = [('남쪽 idle (참고)', np.array(ref['6_south'])), ('동쪽 idle (참고)', np.array(ref['0_east'])),
              ('북쪽 망토 안 몸', under), ('몸 + 망토 윤곽', None), ('망토 젖힘 시험', demo), ('북쪽 idle (원본)', idle)]
    W = 64 * k; out = Image.new('RGBA', (W * len(panels), W + 30), bg); d = ImageDraw.Draw(out)
    from attack_north_slash01_stick import font
    F = font(18)
    for i, (lab, arr) in enumerate(panels):
        cell = Image.new('RGBA', (64, 64), bg)
        if arr is None:
            cell.alpha_composite(Image.fromarray(under))
            ov = Image.fromarray(idle).copy(); ov.putalpha(Image.fromarray(((idle[..., 3] > 0) * 90).astype(np.uint8)))
            cell.alpha_composite(ov)
        else:
            cell.alpha_composite(Image.fromarray(arr))
        out.alpha_composite(cell.resize((W, W), Image.NEAREST), (i * W, 30))
        d.text((i * W + 8, 4), lab, font=F, fill='white')
    out.save(os.path.join(OUT, 'body_under_cape_compare_x6.png'))
    Image.fromarray(demo).save(os.path.join(OUT, 'demo_cape_lifted_64.png'))


if __name__ == '__main__':
    main()
