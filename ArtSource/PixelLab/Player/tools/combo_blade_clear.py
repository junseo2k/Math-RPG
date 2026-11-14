"""연격 중 칼이 잔상에 묻혀 흐려지는 프레임 고치기 (2026-11-13 사용자 "칼 잘 보이도록 수정한 후에 게임에 넣어줘").

고칠 프레임마다: 회색·크림·흰색 잔상/옛 칼날 중 새 칼 축 가까이(3px)는 지우고, 나머지 잔상은 반투명(알파 110)으로 둔다.
그 위에 idle 규격 3단 검날(redraw_blade._draw)을 손잡이·각도대로 투명 칸에만 그린다(몸·손은 그대로).

python combo_blade_clear.py   -> ../35_attack_greatsword_combo_6dir/06_blade_clear/
"""
import math, os
import numpy as np
from PIL import Image
from redraw_blade import _draw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.join(HERE, '..')
OUT = os.path.join(ROOT, '35_attack_greatsword_combo_6dir', '06_blade_clear')
SMEAR_ALPHA = 110

# 동쪽: 33 확정 시트 (파일, 시트 프레임 0부터) : (손잡이 x,y, 각도, 길이)   [이어 재생 번호]
EAST = {('1_light_slash', 2): ((62, 68), 75, 31),     # #3 칼날이 점선처럼 얇음
        ('1_light_slash', 3): ((36, 76), -155, 31),   # #4 칼이 잔상 속에 없음
        ('1_light_slash', 4): ((28, 71), 160, 31),    # #5
        ('2_rising_slash', 2): ((60, 56), 119, 30),   # #10
        ('3_heavy_chop', 1): ((59, 73), -40, 30)}     # #16
# 북쪽: 이어 재생 프레임(1부터) : 같은 형식. 3번은 손 앞으로 고친 승인본에서 시작(칼은 그대로)
NORTH = {4: ((68, 74), 20, 31), 5: ((70, 82), -30, 30), 10: ((38, 48), 100, 33),
         19: ((62, 70), 69, 29), 20: ((70, 67), 80, 29)}


def smear_mask(a):
    c = a[..., :3].astype(int)
    grey = (c.max(-1) - c.min(-1) < 40) & (c.min(-1) > 60)
    cream = (c[..., 0] > 220) & (c[..., 1] > 200) & (c[..., 2] > 150)
    return (a[..., 3] > 0) & (grey | cream)


def fix(a, guard, angle, length, thick):
    o = a.copy()
    g = np.array(guard, float); r = math.radians(angle); u = np.array([math.cos(r), -math.sin(r)])
    n = np.array([u[1], -u[0]])
    ys, xs = np.nonzero(smear_mask(a))
    for y, x in zip(ys, xs):
        p = np.array([x, y], float) - g
        if -1 <= p @ u <= length + 1 and abs(p @ n) <= 3:
            o[y, x] = 0
        else:
            o[y, x, 3] = SMEAR_ALPHA
    o, _ = _draw(o, [], g, u, g + u * length, length, thick)
    return o


def preview(frames, path, cell, fps=16, z=4):
    ims = []
    for f in frames:
        bg = Image.new('RGBA', (cell, cell), (58, 62, 72, 255)); bg.alpha_composite(Image.fromarray(f))
        ims.append(bg.resize((cell * z, cell * z), Image.NEAREST).convert('P', palette=Image.ADAPTIVE))
    ims[0].save(path, save_all=True, append_images=ims[1:], duration=round(1000 / fps), loop=0)


def east():
    d = os.path.join(OUT, 'east'); os.makedirs(d, exist_ok=True)
    src = os.path.join(ROOT, '33_attack_greatsword_combo_east')
    sheets = {}
    for name in ('1_light_slash', '2_rising_slash', '3_heavy_chop'):
        s = np.array(Image.open(os.path.join(src, f'{name}_east.png')).convert('RGBA'))
        fr = [s[:, i * 96:(i + 1) * 96].copy() for i in range(s.shape[1] // 96)]
        for (nm, i), (gd, ang, ln) in EAST.items():
            if nm == name:
                fr[i] = fix(fr[i], gd, ang, ln, 4.7)
        sheets[name] = fr
        Image.fromarray(np.concatenate(fr, 1)).save(os.path.join(d, f'{name}_east.png'))
    # 이어 재생 23장(2타 f1·3타 f1·3타 f10 빼기) = 사용자 GIF와 같은 순서
    chain = sheets['1_light_slash'] + sheets['2_rising_slash'][1:] + sheets['3_heavy_chop'][1:9]
    preview(chain, os.path.join(d, 'east_chain_16fps_x4.gif'), 96)
    return d


def north():
    d = os.path.join(OUT, 'north'); os.makedirs(d, exist_ok=True)
    ref = os.path.join(ROOT, '35_attack_greatsword_combo_6dir', '00_ref_aseprite', 'north_chain')
    frames = []
    for i in range(1, 27):
        if i == 3:
            a = np.array(Image.open(os.path.join(ROOT, '35_attack_greatsword_combo_6dir', '05_north_f3_hand_fix',
                                                 'f3_fix1_104.png')).convert('RGBA'))
        else:
            a = np.array(Image.open(os.path.join(ref, f'frame_{i:02d}.png')).convert('RGBA'))[::4, ::4].copy()
        a[:4, :12] = 0   # GIF 라벨 칸
        if i in NORTH:
            gd, ang, ln = NORTH[i]
            a = fix(a, gd, ang, ln, 5.4)
        frames.append(a)
        Image.fromarray(a).save(os.path.join(d, f'frame_{i:02d}.png'))
    preview(frames, os.path.join(d, 'north_chain_16fps_x4.gif'), 104)
    return d


if __name__ == '__main__':
    print(east()); print(north())
