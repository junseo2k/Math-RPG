"""19h aqueduct background: PixelLab raw -> 16-colour palette, 1px outline (objects) -> Assets/Resources/Art.
Spec: Docs/Art/Ch1_19_SacredAqueduct.md. Reuses PNG I/O and outline-corner rule from the player pipeline.

  python bg_export.py          # needs 01_raw/{water,dry,wall}.{png,json} and 01_raw/{pillar,lamp,exit_locked,exit_open}.png
Strip order for tile_*_ch1: index = NW<<3 | NE<<2 | SW<<1 | SE, bit = corner is the region terrain (water/dry/wall).
"""
import json, os, shutil, sys
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', '..', 'Player', 'tools'))
from pngio import read_png, write_png, upscale            # noqa: E402
from process import remove_corner_outline, outline_corners  # noqa: E402

RAW = os.path.join(HERE, '..', '01_raw')
FINAL = os.path.join(HERE, '..', '02_final')
PREVIEW = os.path.join(HERE, '..', '03_preview')
ART = os.path.join(HERE, '..', '..', '..', '..', 'Assets', 'Resources', 'Art')
PALETTE = ['000000', '141a26', '1e2738', '2b364a', '3c4a60', '55647a', '8a97a8', 'c4cdd8',
           '1a2450', '26357a', '3f56a8', '7f95d6', '2e2a26', '463f37', '625849', '7a7062']
PAL = np.array([[int(h[i:i + 2], 16) for i in (0, 2, 4)] for h in PALETTE], np.uint8)
W = np.array([2.0, 4.0, 3.0])
BITS = {'NW': 8, 'NE': 4, 'SW': 2, 'SE': 1}


def quantize(img):
    out = np.zeros_like(img)
    a = img[..., 3] > 127
    px = img[a][:, :3].astype(float)
    idx = (((px[:, None] - PAL[None].astype(float)) ** 2) * W).sum(-1).argmin(1)
    out[a, :3] = PAL[idx]
    out[a, 3] = 255
    return out


def outline(img):
    """Objects: 1px black outline drawn outside any non-black edge pixel, 4 directions, no diagonal corners."""
    img = img.copy()
    a = img[..., 3] > 0
    blk = a & (img[..., :3] == 0).all(-1)
    H, Wd = a.shape
    for y, x in zip(*np.nonzero(a & ~blk)):
        for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            oy, ox = y + dy, x + dx
            if 0 <= oy < H and 0 <= ox < Wd and not a[oy, ox]:
                img[oy, ox] = [0, 0, 0, 255]
    # canvas border pixels have no room outside: recolour them
    a = img[..., 3] > 0
    for sl in (np.s_[0, :], np.s_[-1, :], np.s_[:, 0], np.s_[:, -1]):
        img[sl][a[sl]] = [0, 0, 0, 255]
    remove_corner_outline(img, PAL[0])
    return img


def wang_strip(name, region):
    """16-tile strip from a PixelLab Wang tileset; region = 'lower' or 'upper' terrain."""
    sheet = read_png(os.path.join(RAW, f'{name}.png'))
    meta = json.load(open(os.path.join(RAW, f'{name}.json'), encoding='utf-8'))
    tiles = [None] * 16
    for t in meta['tileset_data']['tiles']:
        m = sum(b for k, b in BITS.items() if t['corners'][k] == region)
        bb = t['bounding_box']
        if tiles[m] is None:
            tiles[m] = sheet[bb['y']:bb['y'] + bb['height'], bb['x']:bb['x'] + bb['width']]
    assert all(t is not None for t in tiles), f'{name}: missing corner combos'
    return np.concatenate([quantize(t) for t in tiles], 1)


def remap(img, pairs):
    """Swap palette colours (spec: silver only for small accents — enemies are silver)."""
    img = img.copy()
    src = img[..., :3].copy()
    for a, b in pairs.items():
        m = (img[..., 3] > 0) & (src == PAL[PALETTE.index(a)]).all(-1)
        img[m, :3] = PAL[PALETTE.index(b)]
    return img


DRY_NO_SILVER = {'c4cdd8': '7a7062', '8a97a8': '625849'}
DARKER_ONE_STEP = {'c4cdd8': '8a97a8', '8a97a8': '55647a'}
# floor sits in the bottom stone shades (spec 1); applied to every strip that shows floor so edges still match
WALL_UP = {'3c4a60': '55647a', '2b364a': '3c4a60', '1e2738': '2b364a', '141a26': '1e2738'}   # walls read lighter than floor
FLOOR_DOWN = {'55647a': '3c4a60', '3c4a60': '2b364a', '2b364a': '1e2738', '1e2738': '141a26'}


def lamp_frames(img):
    """4-frame flicker from one lamp: swap flame colours (original, dim, original, bright). 64x64 square frames."""
    base = outline(quantize(img))
    h, w = base.shape[:2]
    canvas = np.zeros((64, 64, 4), np.uint8)
    canvas[64 - h:, (64 - w) // 2:(64 - w) // 2 + w] = base
    def swap(src, dst):
        f = canvas.copy()
        m = (f[..., 3] > 0) & (f[..., :3] == PAL[PALETTE.index(src)]).all(-1)
        f[m, :3] = PAL[PALETTE.index(dst)]
        return f
    return np.concatenate([canvas, swap('c4cdd8', '8a97a8'), canvas, swap('7f95d6', 'c4cdd8')], 1)


def on_bg(img, bg=(20, 20, 24)):
    m = img[..., 3:4] / 255.0
    return np.dstack([(img[..., :3] * m + np.array(bg) * (1 - m)).astype(np.uint8), np.full(img.shape[:2], 255, np.uint8)])


def main():
    os.makedirs(FINAL, exist_ok=True); os.makedirs(PREVIEW, exist_ok=True)
    out = {
        'tile_water_ch1': remap(wang_strip('water', 'lower'), FLOOR_DOWN),
        'tile_dry_ch1': remap(remap(wang_strip('dry', 'upper'), DRY_NO_SILVER), FLOOR_DOWN),
    }
    out['tile_ground_ch1'] = out['tile_water_ch1'][:, :32]                 # mask 0 = all floor
    out['tile_wall_ch1'] = remap(wang_strip('wall', 'upper')[:, 15 * 32:], WALL_UP)   # mask 15 = all wall top
    out['obj_pillar_ch1'] = outline(remap(quantize(read_png(os.path.join(RAW, 'pillar.png'))), DARKER_ONE_STEP))
    out['obj_lamp_ch1_idle'] = lamp_frames(read_png(os.path.join(RAW, 'lamp.png')))
    out['obj_exit_locked'] = outline(quantize(read_png(os.path.join(RAW, 'exit_locked.png'))))
    out['obj_exit_open'] = outline(quantize(read_png(os.path.join(RAW, 'exit_open.png'))))

    keys = {int(c[0]) << 16 | int(c[1]) << 8 | int(c[2]) for c in PAL}
    for name, img in out.items():
        a = img[..., 3]
        assert set(np.unique(a).tolist()) <= {0, 255}, name
        k = (img[..., 0].astype(int) << 16 | img[..., 1].astype(int) << 8 | img[..., 2])[a > 0]
        assert set(np.unique(k).tolist()) <= keys, f'{name}: colour outside palette'
        if name.startswith('obj_'):
            for cell in np.split(img, img.shape[1] // min(img.shape[:2]), 1) if name.endswith('_idle') else [img]:
                assert not outline_corners(cell, PAL[0]).any(), f'{name}: diagonal outline corner'
        write_png(os.path.join(FINAL, f'{name}.png'), img)
        write_png(os.path.join(PREVIEW, f'{name}_x4.png'), upscale(on_bg(img), 4))
        shutil.copyfile(os.path.join(FINAL, f'{name}.png'), os.path.join(ART, f'{name}.png'))
    print(f'{len(out)} images -> 02_final and Assets/Resources/Art; palette 16, alpha 0/255, object outlines 1px 4-direction')


if __name__ == '__main__':
    main()
