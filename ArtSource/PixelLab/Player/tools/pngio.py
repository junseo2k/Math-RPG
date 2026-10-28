"""PNG read/write with stdlib zlib + numpy (no PIL; new package installs were declined)."""
import struct, zlib
import numpy as np


def _paeth(a, b, c):
    p = a + b - c
    pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
    return a if pa <= pb and pa <= pc else (b if pb <= pc else c)


def read_png(path):
    """Returns HxWx4 uint8 RGBA. Supports 8-bit RGB/RGBA/gray/palette, non-interlaced."""
    data = open(path, 'rb').read()
    assert data[:8] == b'\x89PNG\r\n\x1a\n', path
    pos, idat, plte, trns = 8, b'', None, None
    while pos < len(data):
        n, typ = struct.unpack('>I4s', data[pos:pos + 8])
        body = data[pos + 8:pos + 8 + n]
        if typ == b'IHDR':
            w, h, depth, ctype, _, _, inter = struct.unpack('>IIBBBBB', body)
        elif typ == b'PLTE':
            plte = np.frombuffer(body, np.uint8).reshape(-1, 3)
        elif typ == b'tRNS':
            trns = np.frombuffer(body, np.uint8)
        elif typ == b'IDAT':
            idat += body
        pos += 12 + n
    assert depth == 8 and inter == 0, (path, depth, inter)
    ch = {0: 1, 2: 3, 3: 1, 4: 2, 6: 4}[ctype]
    raw = zlib.decompress(idat)
    stride = w * ch
    out = np.zeros((h, stride), np.int32)
    prev = np.zeros(stride, np.int32)
    for y in range(h):
        f = raw[y * (stride + 1)]
        line = np.frombuffer(raw, np.uint8, stride, y * (stride + 1) + 1).astype(np.int32)
        if f == 0:
            cur = line
        elif f == 2:
            cur = (line + prev) & 255
        else:
            cur = line.copy()
            for x in range(stride):
                a = cur[x - ch] if x >= ch else 0
                b = prev[x]
                c = prev[x - ch] if x >= ch else 0
                pred = a if f == 1 else ((a + b) >> 1 if f == 3 else _paeth(a, b, c))
                cur[x] = (cur[x] + pred) & 255
        out[y] = cur
        prev = cur
    px = out.astype(np.uint8).reshape(h, w, ch)
    if ctype == 6:
        return px
    rgba = np.full((h, w, 4), 255, np.uint8)
    if ctype == 2:
        rgba[..., :3] = px
    elif ctype == 0:
        rgba[..., :3] = px
    elif ctype == 4:
        rgba[..., :3] = px[..., :1]; rgba[..., 3] = px[..., 1]
    elif ctype == 3:
        idx = px[..., 0]
        rgba[..., :3] = plte[idx]
        if trns is not None:
            a = np.full(256, 255, np.uint8); a[:len(trns)] = trns
            rgba[..., 3] = a[idx]
    return rgba


def write_png(path, rgba):
    rgba = np.ascontiguousarray(rgba, np.uint8)
    h, w, _ = rgba.shape
    raw = b''.join(b'\x00' + rgba[y].tobytes() for y in range(h))
    def chunk(t, b):
        return struct.pack('>I', len(b)) + t + b + struct.pack('>I', zlib.crc32(t + b) & 0xffffffff)
    with open(path, 'wb') as f:
        f.write(b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', w, h, 8, 6, 0, 0, 0))
                + chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b''))


def upscale(rgba, k):
    return rgba.repeat(k, 0).repeat(k, 1)


if __name__ == '__main__':
    import tempfile, os
    img = np.random.randint(0, 256, (7, 5, 4), np.uint8)
    p = os.path.join(tempfile.gettempdir(), 'pngio_selftest.png')
    write_png(p, img)
    assert (read_png(p) == img).all()
    print('pngio ok')
