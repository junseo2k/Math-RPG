"""Copy 02_final sheets into the game's naming: Assets/Resources/Art/chr_player_{state}_{dir}.png
(SpriteAnimator reads {BaseName}_{state}_{dir} horizontal strips; dodge is the game's 'dash' state)."""
import os, shutil

HERE = os.path.dirname(os.path.abspath(__file__))
FINAL = os.path.join(HERE, '..', '02_final')
ART = os.path.join(HERE, '..', '..', '..', '..', 'Assets', 'Resources', 'Art')
STATES = {'idle_alert': 'idle',   # alert idle replaces the game idle (user, 2026-10-28)
          'run': 'run', 'attack': 'attack', 'dodge': 'dash', 'hurt': 'hurt'}
DIRS = {'south': 's', 'south-east': 'se', 'east': 'e', 'north-east': 'ne',
        'north': 'n', 'north-west': 'nw', 'west': 'w', 'south-west': 'sw'}

if __name__ == '__main__':
    n = 0
    for clip, state in STATES.items():
        for d, short in DIRS.items():
            shutil.copyfile(os.path.join(FINAL, clip, f'{d}.png'), os.path.join(ART, f'chr_player_{state}_{short}.png'))
            n += 1
    print(f'{n} sprites -> {os.path.normpath(ART)}')
