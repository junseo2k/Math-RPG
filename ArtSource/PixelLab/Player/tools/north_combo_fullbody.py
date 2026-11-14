"""North 3-hit combo, whole body + cape redrawn per frame (2026-11-08 user spec), hands hidden (back view).
1st hit = north_hit1_fullbody.FRAMES (approved method). 2nd and 3rd hit keyed here with the same rules:
  2nd (rising): start low (head/pelvis +2.5), knees and torso extend together (head up 3, pelvis up 2),
       hands reach the top first, body settles a frame later; shoulders rise with the elbows (lift); feet stay wide;
       last prep frames drop the pelvis 1 px. Cape hem stays low while the body rises, follows up 1-2 frames later,
       right side lifts more (asymmetric).
  3rd (chop, 10 frames = current north timetable): head/chest drop 4, pelvis 2 below the prep, torso foreshortened
       (pitched forward = toward screen-up), holds low 2 frames, pelvis settles before the chest, right foot steps back
       to the idle spot (heel up, then down), last frame = north idle. Cape: hem stays up as the body drops, spreads
       back at the lowest point (+6..+8 px, asymmetric), comes down after the body: spread -> half -> small rebound -> idle.
Sword path/order/grip from the existing north combo (grip follows the chest, max 3 px). No AI generation.
Out: 34_attack_greatsword_combo_north/combo_fullbody_v1/ ; exit 0 = checks passed.
"""
import os, sys, json
import numpy as np
from PIL import Image, ImageDraw
import attack_north_dress_trial as T
import attack_north_slash01_stick as m
import north_hit1_fullbody as H

OUT = os.path.join(T.NORTH, 'combo_fullbody_v1')
V3 = os.path.join(T.NORTH, 'dress', 'v3_cape')
F = lambda **k: dict(dict(twist=0.0, ankleR=(51.5, 89.0), heelR=0, lining=None, lift=0), **k)
WIDE = (51.5, 89.0)

FR1 = H.FRAMES
FR2 = [FR1[-1],
       F(stage='모으기: 낮게', head=(0.5, 2.5), chest=(0.5, 2.5), pelvis=(1, 2.5), twist=0.3,
         B=((32, 82), (61, 83)), C=[(29, 90), (37, 91), (46, 92), (55, 92), (63, 90)]),
       F(stage='올려베기: 무릎·상체 함께 폄', head=(0, 0.5), chest=(0, 0.5), pelvis=(0.5, 1.5),
         B=((32, 82), (60, 83)), C=[(29, 90), (37, 92), (46, 92), (54, 92), (62, 91)]),          # hem stays low
       F(stage='상승', head=(0, -0.5), chest=(0, -0.5), pelvis=(0, 0.5),
         B=((32, 81), (60, 82)), C=[(29, 90), (37, 91), (45, 91), (54, 91), (62, 90)]),
       F(stage='손 최고점', head=(0, -1), chest=(0, -1), pelvis=(0, 0), lift=1,
         B=((32, 80), (61, 79)), C=[(29, 89), (37, 90), (46, 89), (56, 87), (65, 85)], lining=(3, 4)),   # hem follows up, right higher
       F(stage='몸이 늦게 정리', head=(0, -0.5), chest=(0, -0.5), pelvis=(0, 0), lift=2,
         B=((31, 80), (62, 79)), C=[(28, 89), (36, 90), (46, 89), (56, 86), (66, 84)], lining=(3, 4)),
       F(stage='내려치기 준비: 골반 1px 낮춤', head=(0, -0.5), chest=(0, -0.5), pelvis=(0, 0.5), lift=2,
         B=((32, 81), (61, 80)), C=[(29, 89), (37, 90), (46, 90), (55, 88), (64, 87)]),
       F(stage='내려치기 준비 끝', head=(0, -0.5), chest=(0, -0.5), pelvis=(0, 1), lift=2,
         B=((32, 81), (61, 81)), C=[(29, 89), (37, 90), (46, 90), (54, 90), (63, 88)])]
FR3 = [FR2[-1],
       F(stage='내려치기 통과: 몸 떨어짐, 자락 남음', head=(0, 3.5), chest=(0, 3), pelvis=(0, 2), torso_sy=0.9,
         B=((32, 83), (61, 83)), C=[(29, 89), (37, 90), (46, 90), (54, 90), (63, 88)]),
       F(stage='가장 낮게: 망토 뒤로 펼침', head=(0, 4), chest=(0, 3.5), pelvis=(0, 3), torso_sy=0.85,
         B=((30, 84), (63, 84)), C=[(23, 92), (32, 93), (46, 93), (58, 93), (68, 91)]),
       F(stage='낮게 유지', head=(0, 4), chest=(0, 3.5), pelvis=(0, 3), torso_sy=0.85,
         B=((29, 84), (64, 84)), C=[(22, 92), (31, 93), (46, 93), (59, 93), (69, 90)]),
       F(stage='골반 먼저 안정', head=(0, 4), chest=(0, 3.5), pelvis=(0, 2.5), torso_sy=0.85,
         B=((31, 84), (62, 84)), C=[(25, 92), (33, 93), (46, 93), (57, 92), (66, 90)]),
       F(stage='상체 따라 정리', head=(0, 3.5), chest=(0, 3), pelvis=(0, 2), torso_sy=0.9,
         B=((32, 83), (61, 83)), C=[(27, 91), (35, 92), (46, 92), (55, 92), (64, 89)]),
       F(stage='복귀: 오른 뒤꿈치 듦, 작은 반동', head=(0, 2.5), chest=(0, 2), pelvis=(0, 1.5), torso_sy=0.95, heelR=1,
         B=((32, 82), (61, 82)), C=[(29, 90), (37, 91), (46, 91), (55, 90), (65, 87)]),
       F(stage='복귀: 오른발 원래 자리', head=(0, 1.5), chest=(0, 1.5), pelvis=(0, 1), ankleR=H.ANKLE_R,
         B=((32, 81), (60, 81)), C=[(28, 89), (36, 90), (45, 90), (53, 90), (61, 89)]),
       F(stage='거의 중립', head=(0, 0.5), chest=(0, 0.5), pelvis=(0, 0.5), ankleR=H.ANKLE_R,
         B=(H.B0L, H.B0R), C=H.HEM0),
       None]   # f10 = north idle pixels
HITS = {'1': FR1, '2': FR2, '3': FR3}
NAMES = {'1': '1_light_slash_north', '2': '2_rising_slash_north', '3': '3_heavy_chop_north'}


def idle96(no_sword=False):
    if no_sword:
        body64, head64, _ = T.build_body(); a = T.place(body64); T.over(a, T.place(head64)); return a
    a = np.zeros((160, 160, 4), np.uint8)
    a[T.OFF[1]:T.OFF[1] + 64, T.OFF[0]:T.OFF[0] + 64] = np.array(Image.open(T.IDLE).convert('RGBA'))
    return a[:96, :96].copy()


def build():
    seq, seq_ns = {}, {}
    for hit, frs in HITS.items():
        seq[hit], seq_ns[hit] = [], []
        for i, fr in enumerate(frs):
            if (hit, i) in (('1', 0), ('3', 9)):          # both ends = north idle pixels
                seq[hit].append(idle96()); seq_ns[hit].append(idle96(True)); continue
            seq[hit].append(H.render(i, fr, True, hit)[0]); seq_ns[hit].append(H.render(i, fr, False, hit)[0])
    return seq, seq_ns


def checks(seq):
    fails = H.checks()
    if not np.array_equal(seq['1'][-1], seq['2'][0]): fails.append('1f8 != 2f1')
    if not np.array_equal(seq['2'][-1], seq['3'][0]): fails.append('2f8 != 3f1')
    s, e = FR2[1], FR2[3]
    if not (1 <= s['head'][1] <= 2.5 and 1 <= s['pelvis'][1] <= 2.5): fails.append('2nd start 1-2 low')
    if not (2 <= s['head'][1] - e['head'][1] <= 3): fails.append('2nd head up 2-3')
    if not (1 <= s['pelvis'][1] - e['pelvis'][1] <= 2): fails.append('2nd pelvis up 1-2')
    top, low = FR2[-1], FR3[2]
    if not (3 <= low['head'][1] - top['head'][1] <= 4.5): fails.append('3rd head down 3-4')
    if not (2 <= low['pelvis'][1] - top['pelvis'][1] <= 3): fails.append('3rd pelvis down 2-3')
    for fr in (FR3[2], FR3[3]):
        ex_l, ex_r = H.HEM0[0][0] - fr['C'][0][0], fr['C'][-1][0] - H.HEM0[-1][0]
        if not (5 <= max(ex_l, ex_r) <= 8): fails.append(f'3rd spread {ex_l},{ex_r}')
    if FR3[4]['pelvis'][1] >= FR3[3]['pelvis'][1] or FR3[4]['chest'][1] != FR3[3]['chest'][1]: fails.append('pelvis settles before chest')
    used = set()
    for hit in seq:
        if len(seq[hit]) != {'1': 8, '2': 8, '3': 10}[hit]: fails.append(f'{hit} frame count')
        for a in seq[hit]:
            ok, _ = T.check(a); fails += [] if ok else [f'{hit} format']
            used |= {tuple(x) for x in a[a[..., 3] > 0][:, :3].tolist()}
    if len(used) > 16: fails.append(f'palette {len(used)}')
    return fails, len(used)


def old_chain():
    out, ns = [], []
    for j, hit in enumerate('123'):
        sh = np.array(Image.open(os.path.join(V3, NAMES[hit] + '.png')).convert('RGBA'))
        n = sh.shape[1] // 96
        for i in range(n):
            if j and i == 0: continue
            out.append(sh[:, i * 96:(i + 1) * 96])
            img = np.zeros((96, 96, 4), np.uint8); found = False
            for ln in ('feet', 'legs', 'torso', 'cape', 'head'):
                p = os.path.join(V3, 'layers', f'h{hit}_f{i + 1}_{ln}.png')
                if os.path.exists(p): T.over(img, np.array(Image.open(p).convert('RGBA'))); found = True
            ns.append(img if found else idle96(True))
    return out, ns


def main():
    os.makedirs(OUT, exist_ok=True)
    seq, seq_ns = build()
    for hit, ims in seq.items():
        Image.fromarray(np.concatenate(ims, axis=1)).save(os.path.join(OUT, NAMES[hit] + '.png'))
    fails, n = checks(seq)
    chain = seq['1'] + seq['2'][1:] + seq['3'][1:]
    chain_ns = seq_ns['1'] + seq_ns['2'][1:] + seq_ns['3'][1:]
    old, old_ns = old_chain()
    k, bg = 4, (52, 54, 64, 255); Fnt = m.font(20)
    labels = [f'1타 f{i + 1}' for i in range(8)] + [f'2타 f{i + 1}' for i in range(1, 8)] + [f'3타 f{i + 1}' for i in range(1, 10)]
    def cell(a):
        c = Image.new('RGBA', (96, 96), bg); c.alpha_composite(Image.fromarray(np.ascontiguousarray(a)))
        ImageDraw.Draw(c).line([(0, 94), (96, 94)], fill=(70, 120, 255, 120)); return c.resize((96 * k, 96 * k), Image.NEAREST)
    for tag, A, B in (('', old, chain), ('_no_sword', old_ns, chain_ns)):
        for fps in (16, 4):
            ims = []
            for a, b, lab in zip(A, B, labels):
                im = Image.new('RGB', (96 * k * 2 + 8, 96 * k + 30), (40, 42, 50)); d = ImageDraw.Draw(im)
                im.paste(cell(a).convert('RGB'), (0, 30)); im.paste(cell(b).convert('RGB'), (96 * k + 8, 30))
                d.text((8, 4), '기존 (외형 v3)', font=Fnt, fill='white'); d.text((96 * k + 16, 4), f'수정  {lab}', font=Fnt, fill='white')
                ims.append(im)
            ims[0].save(os.path.join(OUT, f'compare_chain{tag}_{fps}fps_x4.gif'), save_all=True, append_images=ims[1:], duration=1000 // fps, loop=0)
    s = 3; W = 96 * s
    board = Image.new('RGBA', (W * 10, (W + 20) * 6), bg); d = ImageDraw.Draw(board); f12 = m.font(13)
    for r, (lab, ims) in enumerate((('1타', seq['1']), ('2타', seq['2']), ('3타', seq['3']),
                                    ('1타 검 숨김', seq_ns['1']), ('2타 검 숨김', seq_ns['2']), ('3타 검 숨김', seq_ns['3']))):
        for i, a in enumerate(ims):
            board.alpha_composite(Image.fromarray(np.ascontiguousarray(a)).resize((W, W), Image.NEAREST), (i * W, r * (W + 20)))
            d.text((i * W + 4, r * (W + 20) + W + 2), f'{lab} f{i + 1}', font=f12, fill='white')
    board.save(os.path.join(OUT, 'board_x3.png'))
    meta = {h: [dict({k2: v for k2, v in fr.items()}) if fr else 'north idle' for fr in frs] for h, frs in HITS.items()}
    json.dump(dict(frames=meta, checks_failed=fails), open(os.path.join(OUT, 'coords.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1, default=str)
    print('palette', n); print('FAIL' if fails else 'OK', fails)
    sys.exit(1 if fails else 0)


if __name__ == '__main__':
    main()
