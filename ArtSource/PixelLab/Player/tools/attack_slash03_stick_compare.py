"""3타 승인본 vs chop 후보 막대기 밑그림 비교(2026-11-08 사용자 "후보로 밑그림 비교"). 생성 호출 없음.
승인본 = 33번 3_heavy_chop_east.png(12f). 후보 = candidates/hit3_chop_raw_3x2.png(3×2칸, 5f) + 승인본 f8~f12(회수).
몸 = 상자, 머리 = 원, 팔 = 어깨→손 막대, 칼 = 손잡이→칼끝 막대. 각도는 손잡이→칼끝, 오른쪽 0°·위 90°.
  python attack_slash03_stick_compare.py -> candidates/stick_compare_x3.png, stick_*_16fps_x4.gif, stick_measure.txt
"""
import math
import os
from PIL import Image, ImageDraw, ImageFont
from attack_slash_east import snap, is_blade, is_hair

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D = os.path.join(ROOT, '33_attack_greatsword_combo_east')
C = os.path.join(D, 'candidates')
S, FPS = 96, 16
EDGE = (0x6b, 0x73, 0x6b)   # 칼날 어두운 테두리. 밝은 회색 2색은 잔상과 같아 색으로 못 나눔


def approved():
    s = Image.open(os.path.join(D, 'history', '3_heavy_chop_east_v11_12f.png')).convert('RGBA')
    return [s.crop((S * k, 0, S * k + S, S)) for k in range(12)]


def candidate(appr):
    s = Image.open(os.path.join(C, 'hit3_chop_raw_3x2.png')).convert('RGBA')
    chop = [snap(s.crop(((k % 3) * S, (k // 3) * S, (k % 3) * S + S, (k // 3) * S + S))) for k in range(5)]
    return chop + appr[7:], ['c%d' % (k + 1) for k in range(5)] + ['f%d' % (k + 1) for k in range(7, 12)]


def measure(f):
    p = f.load()
    blade, body, hair = [], [], []
    for y in range(S):
        for x in range(S):
            c = p[x, y]
            if not c[3]:
                continue
            if is_blade(c):
                if c[:3] == EDGE:
                    blade.append((x, y))
            else:
                body.append((x, y))
                if is_hair(c):
                    hair.append((x, y))
    ys = sorted(y for _, y in body)
    top, bot = ys[0], ys[-1]
    htop = min(y for _, y in hair)
    hair = [(x, y) for x, y in hair if y < htop + 10]    # 금색 갑옷 빼고 정수리 머리카락만
    hx = sum(x for x, _ in hair) / len(hair)
    hy = sum(y for _, y in hair) / len(hair)
    # 몸통 중심: 머리 아래~무릎 위 몸 픽셀(망토 포함)의 중앙값
    torso = [(x, y) for x, y in body if hy + 6 < y < bot - 14]
    tx = sorted(x for x, _ in torso)[len(torso) // 2]
    # 칼: 칼날 어두운 테두리 점이 가장 많이 놓이는 직선(잔상 테두리는 곡선이라 덜 맞음). 몸통에 가까운 끝이 손잡이
    best = (0, None)
    for i, p1 in enumerate(blade):
        for p2 in blade[i + 1:]:
            dx, dy = p2[0] - p1[0], p2[1] - p1[1]
            n = math.hypot(dx, dy)
            if n < 8:
                continue
            on = [q for q in blade if abs((q[0] - p1[0]) * dy - (q[1] - p1[1]) * dx) / n <= 1.2]
            if len(on) > best[0]:
                best = (len(on), on, (dx / n, dy / n))
    _, on, (ux, uy) = best
    on.sort(key=lambda q: q[0] * ux + q[1] * uy)
    e1, e2 = on[0], on[-1]
    near = lambda q: (q[0] - tx) ** 2 + (q[1] - hy - 14) ** 2
    grip, tip = (e1, e2) if near(e1) < near(e2) else (e2, e1)
    ang = math.degrees(math.atan2(grip[1] - tip[1], tip[0] - grip[0])) % 360
    return dict(head=(hx, hy), top=top, bot=bot, tx=tx, grip=grip, tip=tip, ang=ang,
                length=math.dist(grip, tip), shoulder=(tx, hy + 9))


def stick(m, ghost=None):
    im = Image.new('RGBA', (S, S), (250, 250, 248, 255))
    d = ImageDraw.Draw(im)
    d.line([(0, 94), (S, 94)], fill=(150, 180, 230), width=1)
    if ghost:                                            # 이전 프레임 칼(흐리게): 튀는지 보기
        d.line([ghost['grip'], ghost['tip']], fill=(215, 215, 215), width=2)
    hx, hy = m['head']
    d.rectangle([m['tx'] - 6, hy + 6, m['tx'] + 6, m['bot'] - 14], outline=(90, 90, 90), width=1)
    d.line([(m['tx'] - 3, m['bot'] - 14), (m['tx'] - 7, m['bot'])], fill=(90, 90, 90), width=2)
    d.line([(m['tx'] + 3, m['bot'] - 14), (m['tx'] + 7, m['bot'])], fill=(90, 90, 90), width=2)
    d.ellipse([hx - 5, hy - 5, hx + 5, hy + 5], outline=(90, 90, 90), width=1)
    d.line([m['shoulder'], m['grip']], fill=(210, 120, 40), width=2)
    d.line([m['grip'], m['tip']], fill=(40, 90, 200), width=2)
    d.ellipse([m['grip'][0] - 2, m['grip'][1] - 2, m['grip'][0] + 2, m['grip'][1] + 2], fill=(210, 60, 40))
    return im


def row(frames):
    ms = [measure(f) for f in frames]
    return ms, [stick(m, ms[i - 1] if i else None) for i, m in enumerate(ms)]


FONT = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 15)


def label(img, text, xy):
    ImageDraw.Draw(img).text(xy, text, fill=(30, 30, 30), font=FONT)


def main():
    appr = approved()
    cand, cnames = candidate(appr)
    am, ast = row(appr)
    cm, cst = row(cand)
    Z = 3
    cols = 12
    W = Image.new('RGBA', (S * Z * cols, (S * Z) * 4 + 2 * 24 + 12), (255, 255, 255, 255))
    blocks = [('승인본 3타 (12f, %.2f초)' % (12 / FPS), appr, ast, ['f%d' % (k + 1) for k in range(12)], am),
              ('후보 chop c1~c5 + 승인본 f8~f12 (10f, %.2f초)' % (10 / FPS), cand, cst, cnames, cm)]
    y = 0
    for title, frames, sticks, names, ms in blocks:
        label(W, title, (4, y + 2))
        y += 24
        for r, imgs in enumerate((sticks, frames)):
            for i, f in enumerate(imgs):
                bg = Image.new('RGBA', (S, S), (255, 255, 255, 255))
                bg.alpha_composite(f)
                W.paste(bg.resize((S * Z, S * Z), Image.NEAREST), (S * Z * i, y))
                if r == 0:
                    label(W, '%s  %d°' % (names[i], round(ms[i]['ang'])), (S * Z * i + 4, y + 4))
            y += S * Z
        y += 4
    for i in range(1, cols):
        ImageDraw.Draw(W).line([(S * Z * i, 0), (S * Z * i, W.height)], fill=(200, 200, 200))
    W.save(os.path.join(C, 'stick_compare_x3.png'))
    for name, sticks in (('approved', ast), ('candidate', cst)):
        g = [s.resize((S * 4, S * 4), Image.NEAREST).convert('RGB') for s in sticks]
        g[0].save(os.path.join(C, 'stick_%s_16fps_x4.gif' % name), save_all=True, append_images=g[1:],
                  duration=[1000 // FPS] * (len(g) - 1) + [500], loop=0)
    lines = ['프레임 | 손잡이(x,y) | 칼끝(x,y) | 각도° | 길이px | 앞 프레임 대비 손 이동px']
    for title, names, ms in (('승인본', blocks[0][3], am), ('후보', cnames, cm)):
        lines.append('[%s]' % title)
        for i, m in enumerate(ms):
            mv = math.dist(m['grip'], ms[i - 1]['grip']) if i else 0
            lines.append('%s | %s | %s | %d | %.1f | %.1f' % (names[i], m['grip'], m['tip'], round(m['ang']), m['length'], mv))
    open(os.path.join(C, 'stick_measure.txt'), 'w', encoding='utf-8').write('\n'.join(lines) + '\n')
    print('\n'.join(lines))


if __name__ == '__main__':
    main()
