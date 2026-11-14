"""1st hit: the confirmed East motion seen from behind (north). 2026-11-08 user: keep the East motion and feel,
do not design a new north attack. Three moments only: prep (East f3), mid-slash (East f4), brake (East f7).

How each East moment maps to the back view (camera behind, character faces screen-up):
  East front (+x) -> north screen up (away), East back (-x) -> screen down (toward camera, foreshortened),
  character right -> screen right, up -> screen up.
  prep  : sword held upright on the right side, torso twisted right, weight back, cape swung back-left
  slash : deep squat, torso pitched forward and turning left, blade sweeping low across the front to the left,
          cape bunched up over the shoulders (back, legs, belt show)
  brake : lunge, left foot forward, torso turned left, arms extended to the left side, blade back-left and a bit
          up (reads near-horizontal from behind), cape thrown forward-right (left side of the body shows)
Appearance from the north idle: head (body_under_cape/head.png), back-plate texture (body_under_cape/torso.png),
palette and cape colours. Limbs/cape/sword drawn here. No AI generation.
Out: 34_attack_greatsword_combo_north/east_to_north_hit1/ ; exit 0 = palette <=16, alpha 0/255.
"""
import math, os, sys
import numpy as np
from PIL import Image, ImageDraw
import attack_north_dress_trial as T
from north_chop_keyposes import (K, D, O, GOLD, GHI, GMID, GLO, BRZ, BEIGE, CREAM, RED, RDK, sword, gauntlet, pauldron)

OUT = os.path.join(T.NORTH, 'east_to_north_hit1')
BODY = os.path.join(T.NORTH, 'body_under_cape')
EAST = os.path.join(T.ROOT, '33_attack_greatsword_combo_east', '1_light_slash_east.png')


def c(col): return (*col, 255)


def catmull(pts, n=8):
    out = []
    P = [pts[0]] + list(pts) + [pts[-1]]
    for i in range(1, len(P) - 2):
        p0, p1, p2, p3 = P[i - 1], P[i], P[i + 1], P[i + 2]
        for k in range(n):
            t = k / n; t2, t3 = t * t, t * t * t
            out.append(tuple(0.5 * ((2 * p1[j]) + (-p0[j] + p2[j]) * t + (2 * p0[j] - 5 * p1[j] + 4 * p2[j] - p3[j]) * t2
                                   + (-p0[j] + 3 * p1[j] - 3 * p2[j] + p3[j]) * t3) for j in (0, 1)))
    out.append(pts[-1])
    return out


class Fig:
    def __init__(self):
        self.im = Image.new('RGBA', (96, 96)); self.d = ImageDraw.Draw(self.im)

    def seg(self, a, b, w0, w1, fill=D, hi=None):
        """Tapered limb segment with black outline; hi = highlight colour on the screen-left edge."""
        dx, dy = b[0] - a[0], b[1] - a[1]; L = math.hypot(dx, dy) or 1; nx, ny = -dy / L, dx / L
        poly = [(a[0] + nx * w0 / 2, a[1] + ny * w0 / 2), (b[0] + nx * w1 / 2, b[1] + ny * w1 / 2),
                (b[0] - nx * w1 / 2, b[1] - ny * w1 / 2), (a[0] - nx * w0 / 2, a[1] - ny * w0 / 2)]
        grow = lambda p, q, g: (p[0] + (p[0] - q[0]) * 0 + g * np.sign(p[0] - (a[0] + b[0]) / 2), p[1])
        self.d.polygon([(x, y) for x, y in poly], fill=c(K))
        inner = [(a[0] + nx * (w0 / 2 - 1), a[1] + ny * (w0 / 2 - 1)), (b[0] + nx * (w1 / 2 - 1), b[1] + ny * (w1 / 2 - 1)),
                 (b[0] - nx * (w1 / 2 - 1), b[1] - ny * (w1 / 2 - 1)), (a[0] - nx * (w0 / 2 - 1), a[1] - ny * (w0 / 2 - 1))]
        self.d.polygon(inner, fill=c(fill))
        if hi:
            side = 1 if nx < 0 else -1
            self.d.line([(a[0] + side * nx * (w0 / 2 - 1), a[1] + side * ny * (w0 / 2 - 1)),
                         (b[0] + side * nx * (w1 / 2 - 1), b[1] + side * ny * (w1 / 2 - 1))], fill=c(hi))

    def leg(self, hip, knee, ankle, foot_fwd=0):
        self.seg(hip, knee, 6, 5, D, BEIGE)
        self.seg(knee, ankle, 5, 4, D, BEIGE)
        self.d.rectangle((knee[0] - 1, knee[1] - 1, knee[0] + 1, knee[1]), fill=c(GMID)); self.d.point(knee, fill=c(GHI))
        x, y = ankle
        self.d.rectangle((x - 2, y - 1, x + 2, y + 3 - foot_fwd), fill=c(K))
        self.d.rectangle((x - 1, y, x + 1, y + 2 - foot_fwd), fill=c(GLO))
        self.d.line([(x - 1, y), (x + 1, y)], fill=c(GOLD))

    def arm(self, sh, el, hand):
        self.seg(sh, el, 4, 4, D, O); self.seg(el, hand, 4, 3, D, O)
        self.d.point(el, fill=c(GMID))

    def torso(self, shL, shR, waist_y, sx=1.0, sy=1.0):
        """Back plate from the drawn north texture, squashed for turn (sx) / lean (sy), placed by the shoulders."""
        t = Image.open(os.path.join(BODY, 'torso.png')).convert('RGBA')
        a = np.array(t); ys, xs = np.where(a[..., 3] > 0)
        crop = t.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
        w = max(4, round(crop.width * sx)); h = max(4, round(crop.height * sy))
        crop = crop.resize((w, h), Image.NEAREST)
        cx = (shL[0] + shR[0]) / 2; top = min(shL[1], shR[1]) - 1
        self.im.alpha_composite(crop, (int(round(cx - w / 2)), int(round(top))))
        return (int(round(cx - w / 2)), int(round(top)), w, h)

    def head(self, dx, dy):
        a = Image.open(os.path.join(BODY, 'head.png')).convert('RGBA')
        tmp = Image.new('RGBA', (96, 96)); tmp.paste(a, (16 + dx, 40 + dy), a); self.im.alpha_composite(tmp)

    def cape(self, outline, folds=(), lining=None):
        pts = catmull(outline, 6)
        self.d.polygon(pts, fill=c(RED), outline=c(RDK))
        for f in folds:
            self.d.line(catmull(f, 4), fill=c(RDK))
        if lining:
            self.d.polygon(catmull(lining, 4), fill=c(RDK))

    def medal(self, p):
        pauldron(self, p, w=3, h=2, medal=True)


# pauldron() / gauntlet() / sword() from north_chop_keyposes expect .d and .c(col)
Fig.c = lambda self, col: (*col, 255)
Fig.dot = lambda self, p, col, r=1: self.d.rectangle((p[0] - r, p[1] - r, p[0] + r, p[1] + r), fill=(*col, 255))
Fig.line = lambda self, pts, col, w=1: self.d.line(pts, fill=(*col, 255), width=w)


def collar(f, dx, dy):
    """2026-11-08 user: the cape top stays hung on the neck. Idle cape rows 26-33 (64 px; neck, both shoulders,
    medal on the left) pasted as-is on the shoulders; only the cape below this moves."""
    body64, _, _ = T.build_body()
    piece = np.zeros_like(body64); piece[26:34, 18:44] = body64[26:34, 18:44]
    tmp = Image.new('RGBA', (96, 96)); src = Image.fromarray(piece)
    tmp.paste(src, (16 + dx, 40 + dy), src); f.im.alpha_composite(tmp)


CL, CR, CY = 35, 57, 73   # collar bottom corners (96 px, idle) - the lower cape starts here


def lower_cape(f, dx, dy, side_l, hem, side_r, folds=(), lining=None):
    f.cape([(CL + dx, CY + dy - 1)] + side_l + hem + side_r + [(CR + dx, CY + dy - 1)], folds=folds, lining=lining)


def prep():
    f = Fig()
    # weight back on the right foot, feet close, small knee give; torso twisted right (right shoulder toward camera)
    f.leg((43, 80), (42, 86), (41, 90), 1); f.leg((51, 80), (52, 86), (53, 91))
    f.torso((39, 64), (57, 65), 79, sx=0.95)
    # lower cape swings a little back-left with the twist (hem left side flares out)
    lower_cape(f, 1, 0, [(31, 79)], [(28, 87), (35, 89), (44, 88), (52, 88), (58, 85)], [(59, 78)],
               folds=[[(42, 74), (39, 80), (36, 88)], [(49, 74), (48, 81), (47, 87)], [(38, 74), (33, 80), (30, 86)]])
    collar(f, 1, 0)
    f.head(1, 0)
    # sword held upright on the right side, hands at chest height just outside the right shoulder
    sword(f, (58, 70), (58, 61), (60, 31))
    gauntlet(f, (58, 63)); gauntlet(f, (58, 66))
    return f.im


def slash():
    f = Fig()
    # blade sweeping low across the front to the left (in front of the body -> drawn first, body covers the middle)
    sword(f, (50, 81), (44, 80), (15, 74))
    # deep squat, wide feet, knees out
    f.leg((42, 85), (36, 88), (36, 90), 0); f.leg((51, 85), (57, 88), (57, 91))
    # torso pitched forward (short) and turning left
    f.torso((38, 71), (56, 70), 84, sx=0.95, sy=0.8)
    f.arm((40, 72), (39, 77), (43, 80))
    # body drops, cape lags: the lower cape lifts and flips up toward the trailing left (East f4), still hung on the neck;
    # hem short, underside shows, belt and legs below it
    lower_cape(f, 0, 7, [(31, 80), (25, 74)], [(27, 81), (34, 85), (43, 86), (51, 84)], [(58, 81)],
               folds=[[(44, 80), (36, 82), (30, 80)], [(50, 80), (47, 84)]],
               lining=[(25, 74), (27, 81), (34, 85), (43, 86), (51, 84), (44, 83), (34, 82), (28, 78)])
    collar(f, 0, 7)
    f.head(0, 9)
    return f.im


def brake():
    f = Fig()
    # lunge: left foot forward (higher on screen), right leg pushed back
    f.leg((44, 81), (39, 85), (38, 89), 1); f.leg((51, 81), (55, 87), (57, 91))
    f.torso((38, 66), (55, 64), 80, sx=0.88)
    f.arm((39, 67), (34, 70), (31, 72))
    # lower cape thrown forward-right, top still on both shoulders; lower-left of the body shows
    lower_cape(f, -1, 0, [(38, 79), (43, 86)], [(50, 88), (58, 88), (65, 84)], [(66, 78), (60, 74)],
               folds=[[(46, 74), (50, 81), (53, 87)], [(52, 74), (58, 80), (62, 85)]],
               lining=[(58, 88), (65, 84), (63, 83), (57, 86)])
    collar(f, -1, 0)
    f.head(-1, 0)
    # blade back-left, a little up (back + up cancel on screen), held at the left side
    sword(f, (36, 74), (30, 71), (5, 65))
    gauntlet(f, (31, 72)); gauntlet(f, (33, 73))
    return f.im


def main():
    os.makedirs(OUT, exist_ok=True)
    ok = True; res = {}
    for name, fn in (('1_prep', prep), ('2_slash_mid', slash), ('3_brake', brake)):
        a = np.array(fn()); a[..., 3] = np.where(a[..., 3] > 0, 255, 0)
        good, n = T.check(a); ok &= good; print(name, n, 'ok' if good else 'FAIL')
        Image.fromarray(a).save(os.path.join(OUT, name + '_north.png')); res[name] = a
    board(res)
    sys.exit(0 if ok else 1)


def board(res, k=5, bg=(52, 54, 64, 255)):
    from attack_north_slash01_stick import font
    east = Image.open(EAST).convert('RGBA')
    pairs = [('준비 (East f3)', 2, '1_prep'), ('베기 중간 (East f4)', 3, '2_slash_mid'), ('제동 (East f7)', 6, '3_brake')]
    W = 96 * k; out = Image.new('RGBA', (W * 2 + 12, (W + 40) * 3), bg); d = ImageDraw.Draw(out); F = font(20)
    for r, (lab, ef, nk) in enumerate(pairs):
        y0 = r * (W + 40)
        for j, im in enumerate((east.crop((ef * 96, 0, ef * 96 + 96, 96)), Image.fromarray(res[nk]))):
            cell = Image.new('RGBA', (96, 96), bg); cell.alpha_composite(im)
            ImageDraw.Draw(cell).line([(0, 94), (96, 94)], fill=(70, 120, 255, 150))
            out.alpha_composite(cell.resize((W, W), Image.NEAREST), (j * (W + 12), y0 + 34))
        d.text((8, y0 + 6), f'{lab}', font=F, fill='white'); d.text((W + 20, y0 + 6), '같은 순간, 북쪽', font=F, fill='white')
    out.save(os.path.join(OUT, 'east_vs_north_hit1_x5.png'))


if __name__ == '__main__':
    main()
