"""Delivery sheets (2026-10-28 spec): 02_final/<clip>/<dir>.png -> <out>/<n>_<dir>.png (1px outer padding, E->SE order),
then re-open the files and check them. Exit 1 on any FAIL; report.json holds per-frame numbers.

  python process.py build death && python sheet_clip.py death 06_death
  python sheet_clip.py idle_alert 07_idle_alert 0 4    # outer padding 0, 4px gap between frames
"""
import json, os, sys
import numpy as np
import process as P
from pngio import read_png, write_png, upscale

CLIP = sys.argv[1] if len(sys.argv) > 1 else 'hurt'
OUT = os.path.join(P.ROOT, sys.argv[2] if len(sys.argv) > 2 else '05_hurt')
DIRS = ['east', 'north-east', 'north', 'north-west', 'west', 'south-west', 'south', 'south-east']
PAD = int(sys.argv[3]) if len(sys.argv) > 3 else 1
GAP = int(sys.argv[4]) if len(sys.argv) > 4 else 0
FPS, MARGIN, MAX_W, MAX_H = P.CLIPS[CLIP][1], 4, 36, 48
NO_OUTLINE = {'death_calm', 'hurt_calm', 'run_calm'}   # delivered like the game idle: outer 1px outline stripped (export_unity.strip_outline)
FALLS = {'death', 'death_calm'}   # only the start pose is anchored; the body moves as it falls
STRIDES = {'run_calm'}   # feet move with the stride: the torso is anchored instead (body_top range must be 0)


def edge_of(f):
    m = f[..., 3] > 0
    p = np.pad(m, 1)
    return m & ~(p[:-2, 1:-1] & p[2:, 1:-1] & p[1:-1, :-2] & p[1:-1, 2:])


def strip(f, oc):
    f = f.copy()
    f[edge_of(f) & (f[..., :3] == oc).all(-1)] = 0
    return f


def outline_ok(f, oc):
    edge = edge_of(f)
    if CLIP in NO_OUTLINE:   # no ring left: the silhouette edge is mostly coloured, not the outline colour
        return float((f[edge, :3] == oc).all(-1).mean()) < 0.5
    return bool((f[edge, :3] == oc).all()) and not P.outline_corners(f, oc).any()


def main():
    os.makedirs(os.path.join(OUT, 'preview'), exist_ok=True)
    oc = P.load_palette()[0]
    for n, d in enumerate(DIRS):
        src = read_png(os.path.join(P.FINAL, CLIP, f'{d}.png'))
        FRAMES = src.shape[1] // 64   # per direction: STILL directions may have more frames
        sheet = np.zeros((64 + 2 * PAD, 64 * FRAMES + GAP * (FRAMES - 1) + 2 * PAD, 4), np.uint8)
        for i in range(FRAMES):
            x = PAD + i * (64 + GAP)
            cell = src[:, 64 * i:64 * (i + 1)]
            sheet[PAD:PAD + 64, x:x + 64] = strip(cell, oc) if CLIP in NO_OUTLINE else cell
        write_png(os.path.join(OUT, f'{n}_{d}.png'), sheet)
    fails, notes, rep, allpx, alpha, rows = [], [], {}, [], set(), []
    for n, d in enumerate(DIRS):
        path = os.path.join(OUT, f'{n}_{d}.png')
        assert open(path, 'rb').read(8) == b'\x89PNG\r\n\x1a\n', path
        s = read_png(path)
        FRAMES = (s.shape[1] - 2 * PAD + GAP) // (64 + GAP)
        alpha |= set(np.unique(s[..., 3]).tolist())
        want = (64 + 2 * PAD, 64 * FRAMES + GAP * (FRAMES - 1) + 2 * PAD)
        if s.shape[:2] != want: fails.append(f'{d}: size {s.shape[1]}x{s.shape[0]} != {want[1]}x{want[0]}')
        cells = [(PAD, PAD + i * (64 + GAP)) for i in range(FRAMES)]
        border = np.ones(s.shape[:2], bool)
        for y, x in cells:
            border[y:y + 64, x:x + 64] = False
        if s[border, 3].any(): fails.append(f'{d}: padding/gap not transparent')
        frames = [s[y:y + 64, x:x + 64] for y, x in cells]
        home = P.planted_feet(read_png(os.path.join(P.FINAL, 'rotations', f'{d}.png')))
        fr = []
        for i, f in enumerate(frames):
            a = f[..., 3] > 0
            ys, xs = np.nonzero(a.any(1))[0], np.nonzero(a.any(0))[0]
            w, h = int(xs[-1] - xs[0] + 1), int(ys[-1] - ys[0] + 1)
            m = dict(l=int(xs[0]), t=int(ys[0]), r=int(63 - xs[-1]), b=int(63 - ys[-1]))
            anc = P.planted_feet(f)
            body_top = int(np.nonzero(a.sum(1) >= 6)[0][0])     # first row >= 6 px wide (hair spikes excluded)
            ok = outline_ok(f, oc)
            # 2026-10-28: action clips may grow with the motion -> box/margin are notes; touching the canvas edge is a fail
            if w > MAX_W or h > MAX_H: notes.append(f'{d} f{i}: {w}x{h} > {MAX_W}x{MAX_H}')
            if min(m.values()) == 0: fails.append(f'{d} f{i}: touches canvas edge (clipped?)')
            elif min(m.values()) < MARGIN: notes.append(f'{d} f{i}: margin {min(m.values())} < {MARGIN}')
            # feet stay where the standing pose (same direction) has them; in death only the start pose is anchored
            if CLIP in NO_OUTLINE:   # the standing pose is compared outline-free too
                home = P.planted_feet(strip(read_png(os.path.join(P.FINAL, 'rotations', f'{d}.png')), oc))
            if anc != home and CLIP not in STRIDES and (CLIP not in FALLS or i == 0): fails.append(f'{d} f{i}: anchor {anc} != standing {home}')
            if not ok: fails.append(f'{d} f{i}: silhouette edge not 1px outline colour')
            fr.append(dict(w=w, h=h, margin=m, anchor=list(anc), body_top=body_top, outline_ok=ok))
            allpx.append(f[a][:, :3])
        bt = [x['body_top'] for x in fr]
        if CLIP in STRIDES and max(bt) != min(bt): fails.append(f'{d}: body_top range {max(bt) - min(bt)} px (spec 0)')
        rep[d] = dict(file=f'{n}_{d}.png', size=[s.shape[1], s.shape[0]], frames=FRAMES, frame=fr,
                      body_top_range=max(bt) - min(bt))
        P.write_apng(os.path.join(OUT, 'preview', f'{CLIP}_{d}_x4.png'), [upscale(P.on_bg(f), 4) for f in frames], FPS)
        rows.append(np.concatenate([P.on_bg(f) for f in frames], 1))
    write_png(os.path.join(OUT, 'preview', f'{CLIP}_all_x4.png'), upscale(P.stack_rows(rows), 4))
    # guide overlay (x4): grey = 4px margin, blue = ground line y=56 and centre x=32, red = MAX_W x MAX_H box on the anchor
    g = upscale(P.stack_rows(rows), 4).copy()
    for r in range(len(rows)):
        for c in range(rows[r].shape[1] // 64):
            y0, x0 = r * 64 * 4, c * 64 * 4
            for v in (MARGIN, 63 - MARGIN):
                g[y0 + v * 4, x0:x0 + 256] = g[y0:y0 + 256, x0 + v * 4] = [120, 120, 120, 255]
            g[y0 + 56 * 4, x0:x0 + 256] = g[y0:y0 + 256, x0 + 32 * 4] = [60, 120, 255, 255]
            bx, by = x0 + (32 - MAX_W // 2) * 4, y0 + (56 - MAX_H) * 4
            g[by:by + MAX_H * 4, bx] = g[by:by + MAX_H * 4, bx + MAX_W * 4] = [230, 60, 60, 255]
            g[by, bx:bx + MAX_W * 4] = g[by + MAX_H * 4 - 1, bx:bx + MAX_W * 4] = [230, 60, 60, 255]
    write_png(os.path.join(OUT, 'preview', f'{CLIP}_guides_x4.png'), g)
    ncol = len(np.unique(np.concatenate(allpx), axis=0))
    if ncol > 16: fails.append(f'{ncol} colours > 16')
    if not alpha <= {0, 255}: fails.append(f'alpha values {sorted(alpha)}')
    json.dump(dict(unique_rgb=ncol, alpha=sorted(alpha), fps=FPS, dirs=rep, fails=fails, notes=notes),
              open(os.path.join(OUT, 'report.json'), 'w'), indent=1)
    print(f'sheets {len(DIRS)}, colours {ncol}, alpha {sorted(alpha)}')
    for d, v in rep.items():
        print(d, 'WxH max', max(f['w'] for f in v['frame']), max(f['h'] for f in v['frame']), 'body_top range', v['body_top_range'])
    print(f'notes (size/margin, not failures): {len(notes)}')
    print('\n'.join('FAIL ' + f for f in fails) or 'ALL PASS')
    sys.exit(1 if fails else 0)


if __name__ == '__main__':
    main()
