"""Aseprite 파일을 실행 파일 없이 읽어 프레임 PNG·시간을 꺼낸다 (2026-11-13).

python ase_read.py <file.aseprite> <out_dir>   → out_dir/frame_NN.png, frames.txt(번호·ms)
지원: RGBA·Indexed, 보이는 일반 레이어 합성, raw·linked·compressed cel.
"""
import struct, sys, zlib
from pathlib import Path
from PIL import Image


def read(path):
    b = Path(path).read_bytes()
    _, magic, nframes, W, H, depth = struct.unpack_from('<IHHHHH', b, 0)
    assert magic == 0xA5E0, 'not aseprite'
    transp = b[28]
    pos, palette, layers, frames = 128, [(0, 0, 0, 0)] * 256, [], []
    for fi in range(nframes):
        fsize, fmagic, old_chunks, dur = struct.unpack_from('<IHHH', b, pos)
        new_chunks = struct.unpack_from('<I', b, pos + 12)[0]
        nchunks = new_chunks or old_chunks
        p = pos + 16
        cels = []
        for _ in range(nchunks):
            csize, ctype = struct.unpack_from('<IH', b, p)
            d = p + 6
            if ctype == 0x2004:  # layer
                flags, ltype = struct.unpack_from('<HH', b, d)
                layers.append(dict(visible=bool(flags & 1), type=ltype))
            elif ctype == 0x2019:  # palette
                n, first, last = struct.unpack_from('<III', b, d)
                q = d + 20
                for i in range(first, last + 1):
                    eflags = struct.unpack_from('<H', b, q)[0]
                    r, g, bl, a = b[q + 2:q + 6]
                    palette[i] = (r, g, bl, a)
                    q += 6
                    if eflags & 1:
                        q += 2 + struct.unpack_from('<H', b, q)[0]
            elif ctype in (0x0004, 0x0011) and palette[1] == (0, 0, 0, 0):  # 옛 팔레트(GIF에서 가져온 파일)
                npk = struct.unpack_from('<H', b, d)[0]
                q, idx = d + 2, 0
                for _ in range(npk):
                    idx += b[q]; n = b[q + 1] or 256; q += 2
                    for i in range(n):
                        r, g, bl = b[q:q + 3]
                        if ctype == 0x0011:
                            r, g, bl = r * 4, g * 4, bl * 4
                        palette[idx] = (r, g, bl, 255); idx += 1; q += 3
            elif ctype == 0x2005:  # cel
                li, x, y, op, ctyp = struct.unpack_from('<HhhBH', b, d)
                q = d + 16
                if ctyp == 1:
                    cels.append(dict(layer=li, link=struct.unpack_from('<H', b, q)[0], x=x, y=y, op=op))
                else:
                    w, h = struct.unpack_from('<HH', b, q)
                    raw = b[q + 4:p + csize]
                    if ctyp == 2:
                        raw = zlib.decompress(raw)
                    cels.append(dict(layer=li, w=w, h=h, data=raw, x=x, y=y, op=op))
            p += csize
        frames.append(dict(dur=dur, cels=cels))
        pos += fsize
    bpp = depth // 8
    out = []
    for fi, f in enumerate(frames):
        canvas = Image.new('RGBA', (W, H))
        for c in sorted(f['cels'], key=lambda c: c['layer']):
            if c['layer'] < len(layers) and not layers[c['layer']]['visible']:
                continue
            src = c
            if 'link' in c:
                src = next(k for k in frames[c['link']]['cels'] if k['layer'] == c['layer'])
            w, h, data = src['w'], src['h'], src['data']
            if bpp == 4:
                im = Image.frombytes('RGBA', (w, h), data)
            elif bpp == 1:
                im = Image.new('RGBA', (w, h))
                im.putdata([(0, 0, 0, 0) if v == transp else palette[v] for v in data])
            else:
                g = Image.frombytes('LA', (w, h), data)
                im = g.convert('RGBA')
            canvas.alpha_composite(im, (c['x'], c['y']))
        out.append((canvas, f['dur']))
    return out


if __name__ == '__main__':
    fr = read(sys.argv[1])
    o = Path(sys.argv[2]); o.mkdir(parents=True, exist_ok=True)
    lines = []
    for i, (im, d) in enumerate(fr, 1):
        im.save(o / f'frame_{i:02d}.png'); lines.append(f'{i}\t{d}')
    (o / 'frames.txt').write_text('\n'.join(lines) + '\n', encoding='utf-8')
    print(len(fr), 'frames', fr[0][0].size, 'durations', [d for _, d in fr])
