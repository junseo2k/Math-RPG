"""Copy 02_final sheets into the game's naming: Assets/Resources/Art/chr_player_{state}_{dir}.png
(SpriteAnimator reads {BaseName}_{state}_{dir} horizontal strips; dodge is the game's 'dash' state)."""
import os, shutil
import numpy as np
from pngio import read_png, write_png

HERE = os.path.dirname(os.path.abspath(__file__))
FINAL = os.path.join(HERE, '..', '02_final')
ART = os.path.join(HERE, '..', '..', '..', '..', 'Assets', 'Resources', 'Art')
STATES = {'idle_greatsword': 'idle',   # two-handed greatsword idle replaces idle_calm (user, 2026-10-31)
          'idle_greatsword_pauldron_breath': 'idle',   # left-pauldron breathing idle, 8f, Aseprite-edited (user, 2026-11-03)
          'run_calm': 'run',     # in-place run from the in-game idle, cape/blade sway (user, 2026-10-30)
          'run_greatsword': 'run',   # two-handed greatsword run (user, 2026-10-31); export with --sheet after Aseprite edits
          'attack': 'attack', 'dodge': 'dash',
          'hurt_calm': 'hurt',     # recoil-and-recover from the in-game idle replaces the old hurt (user, 2026-10-29)
          'death_calm': 'death'}   # forward collapse from the in-game idle replaces the 6-frame death (user, 2026-10-29)
DIRS = {'south': 's', 'south-east': 'se', 'east': 'e', 'north-east': 'ne',
        'north': 'n', 'north-west': 'nw', 'west': 'w', 'south-west': 'sw'}
# game copy without the outer 1px silhouette outline; 02_final keeps it (user, 2026-10-29)
NO_OUTLINE = {'idle_greatsword', 'death_calm', 'hurt_calm', 'run_calm', 'attack', 'dodge'}   # attack/dodge added 2026-10-31 (user)


def strip_outline(src, dst):
    oc = np.array([int(open(os.path.join(FINAL, 'palette.hex')).readline()[i:i + 2], 16) for i in (0, 2, 4)])
    img = read_png(src)
    m = img[..., 3] > 0
    p = np.pad(m, 1)
    edge = m & ~(p[:-2, 1:-1] & p[2:, 1:-1] & p[1:-1, :-2] & p[1:-1, 2:]) & (img[..., :3] == oc).all(-1)
    img[edge] = 0
    write_png(dst, img)

def from_sheets(clip, folder, gap=4):
    """Delivery sheets <folder>/<n>_<dir>.png (E..SE order, outline-free, 0 padding, `gap` px between frames; the files
    the user edits in Aseprite, 2026-10-31) -> game strips with the gaps removed. No outline step: they ship as is."""
    order = ['east', 'north-east', 'north', 'north-west', 'west', 'south-west', 'south', 'south-east']
    for n, d in enumerate(order):
        s = read_png(os.path.join(HERE, '..', folder, f'{n}_{d}.png'))
        k = (s.shape[1] + gap) // (64 + gap)
        write_png(os.path.join(ART, f'chr_player_{STATES[clip]}_{DIRS[d]}.png'),
                  np.concatenate([s[:, i * (64 + gap):i * (64 + gap) + 64] for i in range(k)], 1))
    print(f'8 sprites from {folder} -> {os.path.normpath(ART)}')


COMBO = ['1_light_slash', '2_rising_slash', '3_heavy_chop']   # 33_attack_greatsword_combo_east (2026-11-08 확정)
# 96칸 시트의 발(가로 중앙 50, 맨 아래 줄 93)을 게임 idle 64칸의 발(30, 54)과 같은 자리에 두려고 144칸에 덧댐.
# 스프라이트 기준점이 칸 가운데라서 idle은 발이 중앙에서 (-2, +22), 144칸도 (70, 94) = 중앙 72에서 (-2, +22).
COMBO_CELL, COMBO_PAD = 144, (20, 1)


def combo(direction='east', folder='33_attack_greatsword_combo_east'):
    """3연격 시트 -> chr_player_attack1~3_<dir>.png (144×144 칸 가로 스트립, 2026-11-08 사용자 "유니티에 넣어 줘")."""
    for k, name in enumerate(COMBO, 1):
        s = read_png(os.path.join(HERE, '..', folder, f'{name}_{direction}.png'))
        n = s.shape[1] // 96
        out = np.zeros((COMBO_CELL, COMBO_CELL * n, 4), np.uint8)
        x0, y0 = COMBO_PAD
        for i in range(n):
            out[y0:y0 + 96, i * COMBO_CELL + x0:i * COMBO_CELL + x0 + 96] = s[:, i * 96:(i + 1) * 96]
        write_png(os.path.join(ART, f'chr_player_attack{k}_{DIRS[direction]}.png'), out)
        print(f'attack{k}_{DIRS[direction]}: {n} frames')


def chain(frames_dir, d='n', counts=(8, 6, 12), cell=104, foot=(52, 94), scale=4, label=(4, 12)):
    """사용자 Aseprite 이어 재생(x4 GIF에서 읽은 frame_NN.png) -> attack1~3_<d> 144칸 스트립 (2026-11-13 사용자 "북쪽 넣어 줘").
    x4를 1배로 줄이고, GIF 라벨("hit1 f1") 칸 label=(줄, 칸)을 지운다. 발 foot을 144칸 (70, 94)에 맞춘다."""
    frames = []
    for i in range(sum(counts)):
        f = read_png(os.path.join(frames_dir, f'frame_{i + 1:02d}.png'))[::scale, ::scale].copy()
        f[:label[0], :label[1]] = 0
        frames.append(f)
    x0, y0 = 70 - foot[0], 94 - foot[1]
    start = 0
    for k, n in enumerate(counts, 1):
        out = np.zeros((COMBO_CELL, COMBO_CELL * n, 4), np.uint8)
        for i in range(n):
            out[y0:y0 + cell, i * COMBO_CELL + x0:i * COMBO_CELL + x0 + cell] = frames[start + i]
        start += n
        write_png(os.path.join(ART, f'chr_player_attack{k}_{d}.png'), out)
        print(f'attack{k}_{d}: {n} frames')


def strip(sheet, name, cell=64, gap=4):
    """가로 시트(칸 사이 gap) -> Resources/Art/<name>.png, 틈 제거 (2026-11-13 동쪽 걷기 v3)."""
    s = read_png(sheet)
    k = (s.shape[1] + gap) // (cell + gap)
    write_png(os.path.join(ART, f'{name}.png'), np.concatenate([s[:, i * (cell + gap):i * (cell + gap) + cell] for i in range(k)], 1))
    print(f'{name}: {k} frames')


if __name__ == '__main__':
    import sys
    if sys.argv[1:2] == ['--chain']:   # python export_unity.py --chain <frames_dir> n
        chain(*sys.argv[2:4])
        sys.exit(0)
    if sys.argv[1:2] == ['--strip']:   # python export_unity.py --strip <sheet.png> chr_player_run_e
        strip(*sys.argv[2:4])
        sys.exit(0)
    if sys.argv[1:2] == ['--combo']:   # python export_unity.py --combo [east]
        combo(*sys.argv[2:3])
        sys.exit(0)
    if sys.argv[1:2] == ['--sheet']:   # python export_unity.py --sheet run_greatsword 14_run_greatsword [gap]
        from_sheets(sys.argv[2], sys.argv[3], *map(int, sys.argv[4:5]))   # 16_ sheets have gap 0 (brief)
        sys.exit(0)
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
