"""North 3rd hit (heavy chop) stick draft, 10 frames (user 2026-11-08: same as East, not 12), 16 fps.
Starts on the approved 2nd-hit f8 (overhead), chops down in FRONT of the body (= screen up / away from camera, so the
blade foreshortens and goes behind the back), brakes low, then returns to the north idle (f10 = 1st-hit f1).
  python attack_north_slash03_stick.py  -> 34_.../stick/slash03/ + 1->2->3 chain GIFs ; exit 0 = checks passed
Timing from the confirmed East 3rd hit: f2 hit (1/16 s, no wind-up), f3~f6 low brake, f7~f9 recover, f10 idle.
"""
import sys
from PIL import Image
import attack_north_slash01_stick as m
import attack_north_slash02_stick as h2

OUT = m.OUT / 'slash03'
EAST3 = m.ROOT / '33_attack_greatsword_combo_east' / '3_heavy_chop_east.png'

# f = screen length / real length. Blade pointing forward-down toward the ground ahead is nearly end-on from behind.
KEYS = [
    dict(h2.KEYS[-1], stage='시작 = 2타 8프레임'),
    dict(G=(49, 64), ang=88, f=0.50, twist=0.0, bend=2, depth='behind', stage='내려치기(판정): 몸 앞으로'),
    dict(G=(50, 74), ang=85, f=0.20, twist=0.0, bend=3, depth='behind', stage='내려치기: 몸 앞 아래'),
    dict(G=(50, 76), ang=85, f=0.10, twist=0.0, bend=3, depth='behind', stage='제동: 가장 낮게'),
    dict(G=(50, 76), ang=86, f=0.12, twist=0.0, bend=3, depth='behind', stage='제동 유지'),
    dict(G=(51, 75), ang=88, f=0.18, twist=0.1, bend=3, depth='behind', stage='제동 끝'),
    dict(G=(53, 70), ang=100, f=0.50, twist=0.3, bend=2, depth='behind', stage='회수: 칼을 들어 올림'),
    dict(G=(53, 68), ang=120, f=0.80, twist=0.2, bend=1, depth='over', stage='회수: 오른쪽 어깨로'),
    dict(G=(52, 68), ang=132, f=0.95, twist=0.1, bend=0, depth='over', stage='회수: idle 직전'),
    dict(m.KEYS[0], stage='끝 = north idle (1타 f1)'),
]


def chain():
    """1->2->3 without the shared frames (1 f8 = 2 f1, 2 f8 = 3 f1), 16 fps and 4 fps, no smear."""
    sc, bg = 4, (60, 63, 72, 255)
    frames = []
    for k in m.KEYS + h2.KEYS[1:] + KEYS[1:]:
        c = Image.new('RGBA', (m.S * sc, m.S * sc), bg)
        c.alpha_composite(m.render(k, m.solve(k), sc, True))
        frames.append(c.convert('RGB'))
    for fps in (16, 4):
        frames[0].save(m.OUT / f'stick_north_chain_1to3_{fps}fps_x4.gif', save_all=True, append_images=frames[1:], duration=1000 // fps, loop=0)
    return len(frames)


if __name__ == '__main__':
    OUT.mkdir(parents=True, exist_ok=True)
    rc = m.main(KEYS, 'slash03', OUT, EAST3)
    if m.solve(KEYS[0]) != m.solve(h2.KEYS[-1]): print('FAIL: 3rd-hit f1 != 2nd-hit f8'); rc = 1
    if m.solve(KEYS[-1]) != m.solve(m.KEYS[0]): print('FAIL: 3rd-hit f10 != north idle'); rc = 1
    print('screen blade length per frame:', [round(m.TIP_LEN * k['f'], 1) for k in KEYS])
    print('chain frames:', chain())
    sys.exit(rc)
