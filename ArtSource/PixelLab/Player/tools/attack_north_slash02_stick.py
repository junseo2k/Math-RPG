"""North 2nd hit (rising slash) stick draft, 8 frames, 16 fps. Starts on the approved 1st-hit f8, ends with both hands
and the sword above the head (= 3rd-hit start). Same rules/drawing as attack_north_slash01_stick.py (imported).
  python attack_north_slash02_stick.py  -> 34_.../stick/slash02/ + 1->2 chain GIF ; exit 0 = checks passed
Timing from the confirmed East 2nd hit: f1 = 1st-hit end, f2 small gather, f3 hit (2/16 s), f4~f6 rise, f7~f8 settle.
"""
import sys
from PIL import Image
import attack_north_slash01_stick as m

OUT = m.OUT / 'slash02'
EAST2 = m.ROOT / '33_attack_greatsword_combo_east' / '2_rising_slash_east.png'

# Overhead end: blade ~120 deg in 3D (up and a bit back toward camera) -> screen ~92 deg, foreshortened ~0.65, drawn over the head.
# v3 (2026-11-08 user): opposite of the 1st hit, which goes upper left -> top -> down. 2nd hit goes bottom -> top:
# blade dropped to the low centre in front of the body, then swung straight up through the front to overhead.
# v1 (right-side rise) and v2 (loop via the left) are in slash02/history/.
KEYS = [
    dict(m.KEYS[-1], stage='시작 = 1타 8프레임'),
    dict(G=(52, 78), ang=290, f=0.45, twist=0.3, bend=2, depth='behind', stage='힘 모으기: 칼을 몸 앞 아래로'),
    dict(G=(50, 73), ang=90,  f=0.70, twist=0.0, bend=1, depth='behind', stage='올려베기(판정): 아래→위'),
    dict(G=(49, 63), ang=90,  f=1.00, twist=0.0, bend=0, depth='behind', stage='상승: 칼이 곧게 섬'),
    dict(G=(48, 56), ang=91,  f=0.85, twist=0.0, bend=0, lift=1, depth='over', stage='상승: 손이 머리 위로'),
    dict(G=(48, 53), ang=92,  f=0.72, twist=0.0, bend=0, lift=2, depth='over', stage='감속'),
    dict(G=(48, 51), ang=93,  f=0.67, twist=0.0, bend=0, lift=2, depth='over', stage='정착'),
    dict(G=(48, 51), ang=93,  f=0.65, twist=0.0, bend=0, lift=2, depth='over', stage='끝: 양손·검 머리 위'),
]


def chain_gif():
    """1st hit + 2nd hit without the shared frame (1 f8 = 2 f1), 16 fps, no smear."""
    sc, bg = 4, (60, 63, 72, 255)
    frames = []
    for k in m.KEYS + KEYS[1:]:
        c = Image.new('RGBA', (m.S * sc, m.S * sc), bg)
        c.alpha_composite(m.render(k, m.solve(k), sc, True))
        frames.append(c.convert('RGB'))
    frames[0].save(m.OUT / 'stick_north_chain_1to2_16fps_x4.gif', save_all=True, append_images=frames[1:], duration=1000 // m.FPS, loop=0)
    frames[0].save(m.OUT / 'stick_north_chain_1to2_6fps_x4.gif', save_all=True, append_images=frames[1:], duration=160, loop=0)
    return len(frames)


if __name__ == '__main__':
    OUT.mkdir(parents=True, exist_ok=True)
    rc = m.main(KEYS, 'slash02', OUT, EAST2)
    if m.solve(KEYS[0]) != m.solve(m.KEYS[-1]):
        print('FAIL: 2nd-hit f1 != 1st-hit f8'); rc = 1
    print('chain frames:', chain_gif())
    sys.exit(rc)
