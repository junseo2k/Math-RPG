"""Looping GIF previews (stdlib + numpy, no PIL).

  python gif_preview.py compare   # 09_idle_calm/preview/idle_calm_compare_x3.gif: regen/ unfrozen (top) vs body frozen (bottom), E..SE
  python gif_preview.py outline   # 09_idle_calm/preview/idle_calm_outline_x3.gif: with (top) / without (bottom) outer outline, E..SE
  python gif_preview.py all       # 09_idle_calm/preview/idle_calm_all_x4.gif: 8 directions, E NE N NW / W SW S SE
"""
import os, struct, sys
import numpy as np
import process as P
from pngio import read_png, upscale

OUT = os.path.join(P.ROOT, '09_idle_calm')
DIRS = ['east', 'north-east', 'north', 'north-west', 'west', 'south-west', 'south', 'south-east']


def frames(path):
    s = read_png(path)
    return [P.on_bg(s[:, i * 68:i * 68 + 64]) for i in range(4)]


def lzw(idx):
    # ponytail: no compression (clear code every 254 symbols keeps 9-bit codes); fine for previews
    clear, end, bits, n, buf, out = 256, 257, 0, 0, 0, bytearray()
    def put(c):
        nonlocal buf, n
        buf |= c << n; n += 9
        while n >= 8:
            out.append(buf & 255); buf >>= 8; n -= 8
    for i, v in enumerate(idx):
        if i % 254 == 0: put(clear)
        put(int(v))
    put(end)
    if n: out.append(buf & 255)
    return bytes(out)


def write_gif(path, imgs, delay_cs):
    rgb = np.stack([im[..., :3] for im in imgs])
    pal, inv = np.unique(rgb.reshape(-1, 3), axis=0, return_inverse=True)
    assert len(pal) <= 256
    inv = inv.reshape(rgb.shape[:3])
    h, w = inv.shape[1:]
    g = bytearray(b'GIF89a' + struct.pack('<HHBBB', w, h, 0xF7, 0, 0))
    g += pal.astype(np.uint8).tobytes() + bytes(3 * (256 - len(pal)))
    g += b'\x21\xff\x0bNETSCAPE2.0\x03\x01\x00\x00\x00'
    for f in inv:
        g += b'\x21\xf9\x04\x00' + struct.pack('<H', delay_cs) + b'\x00\x00'
        g += b'\x2c' + struct.pack('<HHHHB', 0, 0, w, h, 0) + b'\x08'
        data = lzw(f.ravel())
        for i in range(0, len(data), 255):
            g += bytes([len(data[i:i + 255])]) + data[i:i + 255]
        g += b'\x00'
    g += b'\x3b'
    open(path, 'wb').write(g)
    print(path, f'{w}x{h}', len(imgs), 'frames')


def grid(rows, k=4):
    return [upscale(P.stack_rows([np.concatenate([r[i] for r in row], 1) for row in rows]), k) for i in range(4)]


if __name__ == '__main__':
    delay = round(100 / P.CLIPS['idle_calm'][1])   # 6fps -> 17 cs (GIF steps are 1/100 s)
    os.makedirs(os.path.join(OUT, 'preview'), exist_ok=True)
    if sys.argv[1] == 'compare':
        rows = [[frames(os.path.join(OUT, sub, f'{n}_{d}.png')) for n, d in enumerate(DIRS)] for sub in ('regen', '')]
        write_gif(os.path.join(OUT, 'preview', 'idle_calm_compare_x3.gif'), grid(rows, 3), delay)
    elif sys.argv[1] == 'outline':
        oc = P.load_palette()[0]
        def strip(path):   # outer silhouette outline -> transparent (inner dark lines stay)
            s = read_png(path)
            m = s[..., 3] > 0
            p = np.pad(m, 1)
            edge = m & ~(p[:-2, 1:-1] & p[2:, 1:-1] & p[1:-1, :-2] & p[1:-1, 2:]) & (s[..., :3] == oc).all(-1)
            s[edge] = 0
            return [P.on_bg(s[:, i * 68:i * 68 + 64]) for i in range(4)]
        rows = [[frames(os.path.join(OUT, f'{n}_{d}.png')) for n, d in enumerate(DIRS)],
                [strip(os.path.join(OUT, f'{n}_{d}.png')) for n, d in enumerate(DIRS)]]
        write_gif(os.path.join(OUT, 'preview', 'idle_calm_outline_x3.gif'), grid(rows, 3), delay)
    else:
        cells =[frames(os.path.join(OUT, f'{n}_{d}.png')) for n, d in enumerate(DIRS)]
        rows = [cells[:4], cells[4:]]
        write_gif(os.path.join(OUT, 'preview', 'idle_calm_all_x4.gif'), grid(rows), delay)
