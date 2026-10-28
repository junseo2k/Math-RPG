"""Measure final sprites against the pixel-art standard: scale, colour count, outline thickness/corners."""
import glob, os, sys
import numpy as np
from pngio import read_png
from process import FINAL, PREVIEW, SIZE, key, raw_frames, DIRS, CLIPS, RAW

def cells():
    for clip in CLIPS:
        for d in DIRS:
            s = read_png(os.path.join(FINAL, clip, f'{d}.png'))
            for i, c in enumerate(np.split(s, s.shape[1] // SIZE, 1)):
                yield f'{clip}/{d}/f{i}', c

def nbr(m, dy, dx):
    p = np.pad(m, 1)
    return p[1 + dy:1 + dy + m.shape[0], 1 + dx:1 + dx + m.shape[1]]

# 1) scale: raw upscaled? (fraction of opaque 2x2 blocks that are uniform; native art ~ low, 2x-upscaled = 1.0)
def block_uniform(img, k):
    h, w = (img.shape[0] // k) * k, (img.shape[1] // k) * k
    b = key(img[:h, :w]).reshape(h // k, k, w // k, k)
    op = (img[:h, :w, 3] > 0).reshape(h // k, k, w // k, k).all((1, 3))   # fully opaque blocks only
    return float((b == b[:, :1, :, :1]).all((1, 3))[op].mean())

raws = [f for f in glob.glob(os.path.join(RAW, '*', '*', '*.png'))] + glob.glob(os.path.join(RAW, 'rotations', '*.png'))
ru = [block_uniform(read_png(f), 2) for f in raws]
print(f'raw {len(raws)} files: uniform 2x2 blocks mean {np.mean(ru):.2f} (1.00 = upscaled art, low = native pixels)')
pv = [block_uniform(read_png(f), 4) for f in glob.glob(os.path.join(PREVIEW, '*_sheet_x4.png'))]
print(f'previews x4: uniform 4x4 blocks min {min(pv):.3f} (1.000 = exact nearest integer x4)')

# 2) colours
allc, per = set(), []
for name, c in cells():
    k = key(c)[c[..., 3] > 0]
    u = set(np.unique(k).tolist()); allc |= u; per.append((len(u), name))
per.sort()
print(f'colours: total {len(allc)} incl. outline; per frame min {per[0][0]} max {per[-1][0]} ({per[-1][1]})')

# 3) outline
stats = dict(edge_not_black=0, corner_fill=0, double_inner=0, frames_with_corner=0)
worst = []
for name, c in cells():
    a = c[..., 3] > 0
    blk = a & (c[..., :3] == 0).all(-1)
    edge = a & ~(nbr(a, -1, 0) & nbr(a, 1, 0) & nbr(a, 0, -1) & nbr(a, 0, 1))
    body = a & ~blk
    # corner fill: black edge pixel touching coloured body only diagonally
    body4 = nbr(body, -1, 0) | nbr(body, 1, 0) | nbr(body, 0, -1) | nbr(body, 0, 1)
    body8 = body4 | nbr(body, -1, -1) | nbr(body, -1, 1) | nbr(body, 1, -1) | nbr(body, 1, 1)
    corner = blk & edge & ~body4 & body8
    # doubled: black edge pixel whose inward 4-neighbour is also black and not itself edge
    inner_blk = blk & ~edge
    double = blk & edge & (nbr(inner_blk, -1, 0) | nbr(inner_blk, 1, 0) | nbr(inner_blk, 0, -1) | nbr(inner_blk, 0, 1))
    stats['edge_not_black'] += int((edge & ~blk).sum())
    stats['corner_fill'] += int(corner.sum()); stats['double_inner'] += int(double.sum())
    stats['frames_with_corner'] += int(corner.any())
    worst.append((int(double.sum()), int(corner.sum()), int(edge.sum()), name))
n = len(worst)
print(f'outline over {n} frames: edge pixels not black {stats["edge_not_black"]}, '
      f'diagonal corner fills {stats["corner_fill"]} (in {stats["frames_with_corner"]} frames), '
      f'edge pixels with black behind them {stats["double_inner"]}')
print('edge pixels per frame avg %.0f; doubled share %.1f%%' % (np.mean([w[2] for w in worst]), 100 * sum(w[0] for w in worst) / sum(w[2] for w in worst)))
worst.sort(reverse=True); print('most doubled:', worst[:3])
