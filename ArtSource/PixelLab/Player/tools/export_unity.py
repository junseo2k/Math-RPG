"""Copy 02_final sheets into the game's naming: Assets/Resources/Art/chr_player_{state}_{dir}.png
(SpriteAnimator reads {BaseName}_{state}_{dir} horizontal strips; dodge is the game's 'dash' state)."""
import os, shutil
import numpy as np
from pngio import read_png, write_png

HERE = os.path.dirname(os.path.abspath(__file__))
FINAL = os.path.join(HERE, '..', '02_final')
ART = os.path.join(HERE, '..', '..', '..', '..', 'Assets', 'Resources', 'Art')
STATES = {'idle_calm': 'idle',   # body-frozen guard idle replaces the breathing idle (user, 2026-10-29)
          'run_calm': 'run',     # in-place run from the in-game idle, cape/blade sway (user, 2026-10-30)
          'attack': 'attack', 'dodge': 'dash',
          'hurt_calm': 'hurt',     # recoil-and-recover from the in-game idle replaces the old hurt (user, 2026-10-29)
          'death_calm': 'death'}   # forward collapse from the in-game idle replaces the 6-frame death (user, 2026-10-29)
DIRS = {'south': 's', 'south-east': 'se', 'east': 'e', 'north-east': 'ne',
        'north': 'n', 'north-west': 'nw', 'west': 'w', 'south-west': 'sw'}
# game copy without the outer 1px silhouette outline; 02_final keeps it (user, 2026-10-29)
NO_OUTLINE = {'idle_calm', 'death_calm', 'hurt_calm', 'run_calm'}


def strip_outline(src, dst):
    oc = np.array([int(open(os.path.join(FINAL, 'palette.hex')).readline()[i:i + 2], 16) for i in (0, 2, 4)])
    img = read_png(src)
    m = img[..., 3] > 0
    p = np.pad(m, 1)
    edge = m & ~(p[:-2, 1:-1] & p[2:, 1:-1] & p[1:-1, :-2] & p[1:-1, 2:]) & (img[..., :3] == oc).all(-1)
    img[edge] = 0
    write_png(dst, img)

if __name__ == '__main__':
    import sys
    only = sys.argv[1:]   # e.g. run_calm: export just these clips (02_final may hold only some of them)
    n = 0
    for clip, state in STATES.items():
        if only and clip not in only:
            continue
        for d, short in DIRS.items():
            src, dst = os.path.join(FINAL, clip, f'{d}.png'), os.path.join(ART, f'chr_player_{state}_{short}.png')
            if clip in NO_OUTLINE:
                strip_outline(src, dst)
            else:
                shutil.copyfile(src, dst)
            n += 1
    print(f'{n} sprites -> {os.path.normpath(ART)}')
