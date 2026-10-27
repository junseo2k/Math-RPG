# Turn the PixelLab inpaint + edit results into transparent damage overlays.
# edit_image re-renders the whole crop (every pixel shifts a little), so a pixel is taken only
# when it is near a masked damage spot AND clearly different from the untouched scene.
# In the centre fighting area the damage is blended halfway back toward the stone.
# usage: python extract_damage.py <rev5 dir>
import json, sys
from PIL import Image, ImageFilter

R5 = sys.argv[1]
W = R5 + '/work/'
crops = json.load(open(W + 'crops.json'))
base = Image.open(W + 'base_noprops.png').convert('RGBA')
mask = Image.open(W + 'mask_full.png').convert('L')
NEAR = {'floor': 5, 'floor_dry': 6, 'water': 6, 'wall_l': 7, 'wall_m': 7, 'wall_r': 7}
DIFF = 14                       # max channel difference that counts as a real change
CENTRE = (220, 150, 440, 330)   # fight area: damage kept, contrast halved
CENTRE_KEEP = 0.5

placement = {}
for name, box in crops.items():
    b = base.crop(box)
    e = Image.open(W + 'edit_%s.png' % name).convert('RGBA')
    near = mask.crop(box).filter(ImageFilter.MaxFilter(NEAR[name] * 2 + 1))
    out = Image.new('RGBA', b.size, (0, 0, 0, 0))
    bp, ep, np_, op = b.load(), e.load(), near.load(), out.load()
    for y in range(b.height):
        for x in range(b.width):
            if not np_[x, y]:
                continue
            c0, c1 = bp[x, y], ep[x, y]
            if max(abs(c0[i] - c1[i]) for i in range(3)) <= DIFF:
                continue
            gx, gy = box[0] + x, box[1] + y
            if CENTRE[0] <= gx < CENTRE[2] and CENTRE[1] <= gy < CENTRE[3]:
                c1 = tuple(int(c0[i] + (c1[i] - c0[i]) * CENTRE_KEEP) for i in range(3)) + (255,)
            lum = 0.299 * c1[0] + 0.587 * c1[1] + 0.114 * c1[2]
            # PixelLab drew the drained-channel silt near white (read as glowing cracks): grey-brown it
            if gx >= 496 and gy >= 120 and lum > 120:
                c1 = tuple(int(c1[i] + (t - c1[i]) * 0.65) for i, t in enumerate((122, 112, 98))) + (255,)
            # pale chips on the dark wall scattered like sparks: one step darker
            elif 43 <= gy < 89 and lum > 110:
                c1 = tuple(int(c1[i] + (t - c1[i]) * 0.5) for i, t in enumerate((60, 70, 85))) + (255,)
            op[x, y] = c1[:3] + (255,)
    bb = out.getbbox()
    if not bb:
        continue
    fname = 'damage_%s.png' % name
    out.crop(bb).save(R5 + '/objects/' + fname)
    placement[fname] = [box[0] + bb[0], box[1] + bb[1]]
    print(name, 'pixels', sum(1 for p in out.getdata() if p[3]), 'at', placement[fname])
json.dump(placement, open(R5 + '/objects/damage_placement.json', 'w'), indent=1)
