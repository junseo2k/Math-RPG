"""North greatsword combo, dressed: all frames (8/8/10, 16fps) from the approved stick coords. No AI generation.

Same layers as the approved 3-frame trial (attack_north_dress_trial.py, 2026-11-08):
body/cape/medal = north idle minus idle sword+hands (mask), back of head = run_calm north, sword/hands/arms redrawn.
Hit1 f1 and hit3 f10 = the north idle frame itself (exact pixels), so idle <-> attack has no pop.
Arms are drawn under the cape/head, so only the parts outside the body show (user rule: arms hidden from behind).
Hit3 f2 smear = separate layer; sheets with and without it.
Output: 34_attack_greatsword_combo_north/dress/v1/. Exit code 0 = all checks pass.
"""
import json, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont
import attack_north_dress_trial as T

OUT = os.path.join(T.NORTH, 'dress', 'v1')
LIFT = {('2', 5): 1, ('2', 6): 2, ('2', 7): 2, ('2', 8): 2, ('3', 1): 2}   # from attack_north_slash02_stick.py
HITS = [('1', 'stick/coords.json', '1_light_slash_north'),
        ('2', 'stick/slash02/coords.json', '2_rising_slash_north'),
        ('3', 'stick/slash03/coords.json', '3_heavy_chop_north')]
IDLE_ENDS = {('1', 1), ('3', 10)}


def idle96():
    can = np.zeros((96 + 64, 96 + 64, 4), np.uint8)
    can[T.OFF[1]:T.OFF[1] + 64, T.OFF[0]:T.OFF[0] + 64] = np.array(Image.open(T.IDLE).convert('RGBA'))
    return can[:96, :96].copy()


def compose(k, hit, fno, torso64, feet64, head64):
    lift = LIFT.get((hit, fno), 0)
    bend, tw = k['knee_bend'], round(k['twist'])
    L = dict(feet=T.place(feet64), cape=T.place(torso64, 0, bend, lift), head=T.place(head64, tw, bend),
             arms=T.draw_arms(k), hands=T.draw_hands(k), sword=T.draw_sword(k))
    # same order as the approved trial frames; arms over the cape only when raised overhead (lift),
    # otherwise under it (stick rule: arms never show on the back)
    if k['depth'] == 'over':
        # no lift: arms not drawn at all (elbows stuck out of the cape edge as gold dots in hit3 f8-f9)
        order = ['feet', 'cape', 'arms', 'head', 'sword', 'hands'] if lift else ['feet', 'cape', 'head', 'sword', 'hands']
    else:
        order = ['sword', 'hands', 'feet', 'cape', 'head']
    img = np.zeros((96, 96, 4), np.uint8)
    for ln in order:
        T.over(img, L[ln])
    return img, L, order, lift


def main():
    os.makedirs(os.path.join(OUT, 'layers'), exist_ok=True)
    body64, head64, _ = T.build_body()
    torso64, feet64 = T.split_feet(body64)
    idle = idle96()
    ok_all, used, sheets, meta = True, set(), {}, {}
    for hit, cf, name in HITS:
        keys = json.load(open(os.path.join(T.NORTH, cf), encoding='utf-8'))['keys']
        frames = []
        for k in keys:
            f = k['frame']
            if (hit, f) in IDLE_ENDS:
                img, order, lift = idle.copy(), ['idle'], 0
            else:
                img, L, order, lift = compose(k, hit, f, torso64, feet64, head64)
                for ln, a in L.items():
                    Image.fromarray(a).save(os.path.join(OUT, 'layers', f'h{hit}_f{f}_{ln}.png'))
            ok, n = T.check(img); ok_all &= ok
            if not ok:
                print('FAIL', hit, f, n)
            used |= {tuple(c) for c in img[img[..., 3] > 0][:, :3].tolist()}
            frames.append(img)
            meta[f'h{hit}_f{f}'] = dict(k, source=cf, order=order, lift=lift)
        sheets[hit] = frames
        Image.fromarray(np.concatenate(frames, axis=1)).save(os.path.join(OUT, name + '.png'))
    # hit3 f2 smear version
    sm = T.draw_smear()
    Image.fromarray(sm).save(os.path.join(OUT, 'layers', 'h3_f2_smear.png'))
    k = meta['h3_f2']; L = {ln: np.array(Image.open(os.path.join(OUT, 'layers', f'h3_f2_{ln}.png'))) for ln in k['order']}
    img = np.zeros((96, 96, 4), np.uint8)
    for ln in k['order']:
        if ln == 'sword':
            T.over(img, sm)
        T.over(img, L[ln])
    used |= {tuple(c) for c in img[img[..., 3] > 0][:, :3].tolist()}
    smear3 = list(sheets['3']); smear3[1] = img
    Image.fromarray(np.concatenate(smear3, axis=1)).save(os.path.join(OUT, '3_heavy_chop_north_smear.png'))
    # links: hit1 f8 == hit2 f1, hit2 f8 == hit3 f1, hit3 f10 == hit1 f1 (idle)
    links = {'1f8=2f1': np.array_equal(sheets['1'][-1], sheets['2'][0]),
             '2f8=3f1': np.array_equal(sheets['2'][-1], sheets['3'][0]),
             '3f10=1f1': np.array_equal(sheets['3'][-1], sheets['1'][0])}
    # 2f8 vs 3f1 differ only if lift differs; both lift 2
    print('links', links, 'palette', len(used))
    ok_all &= all(links.values()) and len(used) <= 16
    json.dump(dict(idle=T.IDLE, head_donor=T.RUN + ' frame 2', lift=str(LIFT), frames=meta),
              open(os.path.join(OUT, 'coords.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    previews(sheets, smear3)
    sys.exit(0 if ok_all else 1)


def previews(sheets, smear3, k=4, bg=(52, 54, 64, 255)):
    def gif(frames, path, fps):
        ims = []
        for f in frames:
            b = Image.new('RGBA', (96, 96), bg); b.alpha_composite(Image.fromarray(f))
            ims.append(b.resize((96 * k, 96 * k), Image.NEAREST).convert('P', palette=Image.ADAPTIVE))
        ims[0].save(path, save_all=True, append_images=ims[1:], duration=int(1000 / fps), loop=0, disposal=2)
    chain = sheets['1'] + sheets['2'][1:] + sheets['3'][1:]
    chain_s = sheets['1'] + sheets['2'][1:] + smear3[1:]
    gif(chain, os.path.join(OUT, 'chain_north_16fps_x4.gif'), 16)
    gif(chain_s, os.path.join(OUT, 'chain_north_smear_16fps_x4.gif'), 16)
    gif(chain_s, os.path.join(OUT, 'chain_north_smear_4fps_x4.gif'), 4)
    # numbered board: rows = hit1, hit2, hit3, hit3+smear
    f = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 11)
    rows = [('1타', sheets['1']), ('2타', sheets['2']), ('3타', sheets['3']), ('3타+잔상', smear3)]
    s = 3; W = 96 * s
    board = Image.new('RGBA', (W * 10, (W + 16) * 4), bg); d = ImageDraw.Draw(board)
    for r, (lab, fr) in enumerate(rows):
        for i, a in enumerate(fr):
            board.alpha_composite(Image.fromarray(a).resize((W, W), Image.NEAREST), (i * W, r * (W + 16)))
            d.text((i * W + 4, r * (W + 16) + W + 1), f'{lab} f{i + 1}', font=f, fill='white')
            d.rectangle((i * W, r * (W + 16), i * W + W - 1, r * (W + 16) + W - 1), outline=(80, 84, 100, 255))
    board.save(os.path.join(OUT, 'board_numbered_x3.png'))


if __name__ == '__main__':
    main()
