"""North 3rd hit: two key poses drawn new (2026-11-08 user: stop moving old parts around; draw the attack pose itself).
  A = just before the chop (wind-up): stance open, left foot forward, chest up, shoulders raised, elbows high,
      hands over the head, blade cocked back over the right shoulder, cape pulled up by the arms.
  B = just after the chop (impact): wide low stance, left knee deep and out, right leg pushed back, hips dropped,
      torso pitched forward (short), head low, arms driven forward (hidden), cape still flying up behind (lining shows).
Stick drafts were used only for direction/order/timing. Hair = north head (identity). Everything else drawn here.
96x96, pivot (48,94), palette = north idle palette. No AI generation.
Out: 34_attack_greatsword_combo_north/keyposes_v1/ ; exit 0 = palette <=16, alpha 0/255.
"""
import math, os, sys
import numpy as np
from PIL import Image, ImageDraw
import attack_north_dress_trial as T

OUT = os.path.join(T.NORTH, 'keyposes_v1')
K, D, O = (0, 0, 0), (54, 46, 37), (91, 86, 66)
GOLD, GHI, GMID, GLO, BRZ = (210, 168, 85), (252, 219, 109), (195, 150, 80), (174, 120, 58), (145, 81, 27)
BEIGE, CREAM = (217, 198, 159), (253, 230, 189)
RED, RDK = (120, 21, 36), (64, 11, 24)
BL_HI, BL, BL_LO = (187, 190, 190), (165, 168, 167), (107, 115, 107)


class Canvas:
    def __init__(self):
        self.im = Image.new('RGBA', (96, 96)); self.d = ImageDraw.Draw(self.im)

    def c(self, col): return (*col, 255)

    def poly(self, pts, fill, outline=K):
        self.d.polygon(pts, fill=self.c(fill), outline=self.c(outline) if outline else None)

    def limb(self, pts, w, fill, edge=K):
        self.d.line(pts, fill=self.c(edge), width=w + 2, joint='curve')
        self.d.line(pts, fill=self.c(fill), width=w, joint='curve')

    def dot(self, p, col, r=1):
        x, y = p; self.d.rectangle((x - r, y - r, x + r, y + r), fill=self.c(col))

    def line(self, pts, col, w=1):
        self.d.line(pts, fill=self.c(col), width=w)

    def paste(self, a, dx, dy):
        sub = Image.fromarray(a); self.im.alpha_composite(sub, (dx, dy)) if dx >= 0 and dy >= 0 else self._paste_neg(sub, dx, dy)

    def _paste_neg(self, sub, dx, dy):
        tmp = Image.new('RGBA', (96, 96)); tmp.paste(sub, (dx, dy), sub); self.im.alpha_composite(tmp)


def head_sprite():
    a = np.array(Image.open(os.path.join(T.NORTH, 'body_under_cape', 'head.png')).convert('RGBA'))
    ys, xs = np.where(a[..., 3] > 0)
    return a[ys.min():ys.max() + 1, xs.min():xs.max() + 1], (xs.min() + 16, ys.min() + 40)   # crop + idle 96 pos


def boot(cv, ankle, toe_up, flip=1):
    """Back view boot: shaft + heel; toe_up >0 = foot further forward (smaller, heel raised)."""
    x, y = ankle
    cv.poly([(x - 2, y - 1), (x + 2, y - 1), (x + 2, y + 2 - toe_up), (x - 2, y + 2 - toe_up)], GLO)
    cv.line([(x - 1, y - 1), (x + 1, y - 1)], GOLD)
    if not toe_up:
        cv.line([(x - 1, y + 2), (x + 1, y + 2)], BRZ)


def leg(cv, hip, knee, ankle, light_left=True):
    cv.limb([hip, knee, ankle], 3, D)
    # greave highlight + knee cop
    hx = -1 if light_left else 1
    cv.line([(knee[0] + hx, knee[1] + 1), (ankle[0] + hx, ankle[1] - 2)], BEIGE)
    cv.dot(knee, GMID, 1); cv.d.point(knee, fill=cv.c(GHI))


def torso(cv, shL, shR, wL, wR, belt_y):
    cv.poly([shL, shR, wR, wL], K)
    inner = [(shL[0] + 2, shL[1] + 1), (shR[0] - 2, shR[1] + 1), (wR[0] - 1, wR[1] - 2), (wL[0] + 1, wL[1] - 2)]
    cv.poly(inner, D, outline=None)
    mx = (shL[0] + shR[0]) / 2
    cv.line([(mx, shL[1] + 2), (mx, belt_y - 1)], K)                 # spine groove
    cv.line([(shL[0] + 3, shL[1] + 3), (mx - 2, belt_y - 3)], GOLD)  # shoulder-blade trims
    cv.line([(shR[0] - 3, shR[1] + 3), (mx + 2, belt_y - 3)], GOLD)
    cv.line([(mx - 4, shL[1] + 3), (mx - 2, belt_y - 5)], O); cv.line([(mx + 4, shR[1] + 3), (mx + 2, belt_y - 5)], O)
    cv.line([(wL[0] + 1, belt_y), (wR[0] - 1, belt_y)], GHI, 2)        # belt
    cv.line([(wL[0] + 2, belt_y + 2), (wR[0] - 2, belt_y + 2)], GMID)
    for x0 in (wL[0] + 1, (wL[0] + wR[0]) / 2 + 1):                    # tassets
        cv.poly([(x0, belt_y + 3), (x0 + 4, belt_y + 3), (x0 + 4, belt_y + 6), (x0, belt_y + 6)], BEIGE)
        cv.d.point((x0 + 1, belt_y + 4), fill=cv.c(CREAM))


def pauldron(cv, c, w=5, h=3, medal=False):
    x, y = c
    cv.d.ellipse((x - w, y - h, x + w, y + h), fill=cv.c(GMID), outline=cv.c(K))
    cv.line([(x - w + 2, y - 1), (x + w - 2, y - 1)], GHI)
    if medal:
        cv.dot((x, y), GOLD, 1); cv.d.point((x, y), fill=cv.c(GHI)); cv.d.point((x + 1, y + 1), fill=cv.c(BRZ))


def arm(cv, sh, el, hand, w=2):
    cv.limb([sh, el, hand], w, D)
    cv.d.point(el, fill=cv.c(GMID))


def gauntlet(cv, p):
    cv.dot(p, GOLD, 1); cv.d.point((p[0] - 1, p[1] - 1), fill=cv.c(GHI)); cv.d.point((p[0] + 1, p[1] + 1), fill=cv.c(BRZ))


def sword(cv, pommel, guard, tip):
    gx, gy = guard; tx, ty = tip
    L = math.hypot(tx - gx, ty - gy); ux, uy = (tx - gx) / L, (ty - gy) / L; nx, ny = -uy, ux
    for i in range(int(L) + 1):
        t = i / L; half = 2.0 if i < L - 3 else 2.0 * (L - i) / 3 + 0.4
        for s in np.arange(-half, half + 0.01, 0.5):
            x, y = gx + ux * i + nx * s, gy + uy * i + ny * s
            light = s * (1 if ny < 0 else -1)
            col = BL_HI if light > 0.6 else BL if light > -0.6 else BL_LO
            cv.d.point((round(x), round(y)), fill=cv.c(col))
    cv.line([(gx + nx * 4, gy + ny * 4), (gx - nx * 4, gy - ny * 4)], GLO, 2)
    cv.line([(gx + nx * 4, gy + ny * 4), (gx - nx * 4, gy - ny * 4)], GOLD, 1)
    cv.line([guard, pommel], D, 2)
    cv.dot(pommel, GMID, 1)


def cape_hanging(cv, top_l, top_r, hem):
    """Hanging cape: top on the shoulders, hem points (left->right); vertical fold lines."""
    pts = [top_l, top_r] + hem[::-1]
    cv.poly(pts, RED, outline=RDK)
    n = len(hem)
    for i in range(1, n - 1, 1):
        t = i / (n - 1)
        x0 = top_l[0] + (top_r[0] - top_l[0]) * t
        cv.line([(x0, top_l[1] + 4), (hem[i][0], hem[i][1] - 1)], RDK)


def cape_flying(cv, neck_l, neck_r, lobe_l, lobe_r, edge):
    """Cape flung up behind the back: attached at the neck, two lobes rising above the shoulders, underside band."""
    top = [(lobe_l[0] + 4, lobe_l[1] - 3), ((lobe_l[0] + lobe_r[0]) / 2, lobe_l[1] - 5), (lobe_r[0] - 4, lobe_r[1] - 3)]
    outer = [lobe_l] + top + [lobe_r] + edge[::-1]
    cv.poly(outer, RED, outline=RDK)
    # underside (lining) along the lower edge
    for (x0, y0), (x1, y1) in zip(edge, edge[1:]):
        cv.d.polygon([(x0, y0), (x1, y1), (x1, y1 - 3), (x0, y0 - 3)], fill=cv.c(RDK))
    for t in (0.3, 0.55, 0.8):   # fold lines radiating from the neck
        e = edge[int(t * (len(edge) - 1))]
        cv.line([((neck_l[0] + neck_r[0]) / 2, neck_l[1] - 1), (e[0], e[1] - 4)], RDK)


def pose_A():
    cv = Canvas()
    hips = (47, 80)
    # legs: left foot forward (higher, smaller), right foot back; slight bend
    leg(cv, (44, 80), (40, 85), (39, 89)); boot(cv, (39, 89), 1)
    leg(cv, (51, 80), (54, 86), (56, 91)); boot(cv, (56, 91), 0)
    torso(cv, (37, 63), (58, 63), (42, 78), (53, 78), 76)
    # cape: pulled up by the raised arms, swings a little to the left (body winding to the right)
    cape_hanging(cv, (37, 64), (58, 64), [(32, 80), (38, 83), (45, 82), (52, 83), (59, 80)])
    pauldron(cv, (37, 63), medal=True); pauldron(cv, (58, 63))
    # arms up: elbows high and out, hands together over the head
    arm(cv, (39, 62), (37, 54), (45, 48)); arm(cv, (56, 62), (58, 54), (50, 49))
    head, (hx, hy) = head_sprite()
    cv.paste(head, hx, hy)
    # blade cocked back over the right shoulder (behind the head, nearer the camera)
    sword(cv, (44, 46), (50, 52), (68, 78))
    gauntlet(cv, (46, 49)); gauntlet(cv, (49, 51))
    return cv.im


def pose_B():
    cv = Canvas()
    # sword driven forward-down in front (away from camera): mostly hidden; blade end peeks above the head
    sword(cv, (48, 77), (48, 73), (49, 58))
    # legs: wide and low; left knee deep and out, right leg pushed back
    leg(cv, (43, 84), (37, 87), (36, 90)); boot(cv, (36, 90), 1)
    leg(cv, (52, 84), (57, 88), (60, 92)); boot(cv, (60, 92), 0)
    torso(cv, (37, 70), (59, 70), (42, 84), (54, 84), 81)
    # arms driven forward: only the upper arms show, angled in toward the hidden hands
    arm(cv, (38, 70), (40, 67), (45, 66)); arm(cv, (58, 70), (56, 67), (51, 66))
    pauldron(cv, (38, 70), medal=True); pauldron(cv, (58, 70))
    head, (hx, hy) = head_sprite()
    # cape flung up behind the back (lags the drop), wider than the shoulders
    cape_flying(cv, (41, 69), (55, 69), (31, 58), (65, 58),
                [(29, 64), (33, 70), (40, 73), (48, 72), (56, 73), (63, 70), (67, 64)])
    cv.paste(head, hx, hy + 8)          # head dropped low between the shoulders
    return cv.im


def main():
    os.makedirs(OUT, exist_ok=True)
    ok = True
    frames = {}
    for name, fn in (('A_before_chop', pose_A), ('B_after_chop', pose_B)):
        a = np.array(fn())
        a[..., 3] = np.where(a[..., 3] > 0, 255, 0)
        good, n = T.check(a); ok &= good
        print(name, 'colours', n, 'ok' if good else 'FAIL')
        Image.fromarray(a).save(os.path.join(OUT, name + '.png')); frames[name] = a
    preview(frames)
    sys.exit(0 if ok else 1)


def preview(frames, k=5, bg=(52, 54, 64, 255)):
    from attack_north_slash01_stick import font
    idle = np.zeros((160, 160, 4), np.uint8)
    idle[T.OFF[1]:T.OFF[1] + 64, T.OFF[0]:T.OFF[0] + 64] = np.array(Image.open(T.IDLE).convert('RGBA'))
    idle = idle[:96, :96]
    stick = Image.open(os.path.join(T.NORTH, 'stick', 'slash03', 'stick_north_slash03_10f.png')).convert('RGBA')
    panels = [('north idle', idle), ('A 내려치기 직전', frames['A_before_chop']), ('B 내려친 직후', frames['B_after_chop']),
              ('밑그림 3타 f1', np.array(stick.crop((0, 0, 96, 96)))), ('밑그림 3타 f3', np.array(stick.crop((192, 0, 288, 96))))]
    W = 96 * k; out = Image.new('RGBA', (W * len(panels), W + 34), bg); d = ImageDraw.Draw(out); F = font(20)
    for i, (lab, a) in enumerate(panels):
        cell = Image.new('RGBA', (96, 96), bg); cell.alpha_composite(Image.fromarray(np.ascontiguousarray(a)))
        cd = ImageDraw.Draw(cell); cd.line([(0, 94), (96, 94)], fill=(70, 120, 255, 160))
        out.alpha_composite(cell.resize((W, W), Image.NEAREST), (i * W, 34)); d.text((i * W + 8, 6), lab, font=F, fill='white')
    out.save(os.path.join(OUT, 'keyposes_compare_x5.png'))


if __name__ == '__main__':
    main()
