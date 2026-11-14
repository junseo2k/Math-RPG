"""3타 chop 후보 10프레임 시트(2026-11-08 사용자 "후보로 승인": 준비 없이 바로 내려치기). 생성 호출 없음.
c1~c5 = candidates/hit3_chop_raw_3x2.png(3×2칸, 11/08 fix_v8 raw/chop 생성본, 팔레트 밖 18색 → 16색 snap)
c6~c10 = 교체 전 승인본 history/3_heavy_chop_east_v11_12f.png f8~f12(회수 → idle). c2 초승달 꼭대기 y=1 줄(4px)만 지워 위 여백 2px.
  python attack_slash03_chop_build.py -> candidates/3_heavy_chop_east_chop10.png(960×96), chain GIF, check 출력(실패 시 종료 코드 1)
"""
import os
import sys
from PIL import Image
from attack_slash_east import snap, PAL
from attack_slash_pauldron import GOLD, fill

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D = os.path.join(ROOT, '33_attack_greatsword_combo_east')
C = os.path.join(D, 'candidates')
S, FPS, MARGIN = 96, 16, 2
# 2026-11-08 사용자 "어깨장식이 안 보이게, A~E 모두"(동쪽은 어깨장식 안 보임). 상자(포함) 안 금색만 지우고 주변 색으로 채움.
# 상자는 candidates/chop10_pauldron_marks_x10.png에 표시해 확인받은 위치. 지운 픽셀은 chop10_pauldron_mask.png
PAULDRON = {4: ('D', (42, 66, 46, 70)), 5: ('E', (40, 65, 44, 70)), 6: ('A', (41, 66, 46, 71)),
            7: ('B', (42, 67, 47, 72)), 8: ('C', (40, 67, 45, 73))}
DARK_RING = {8}   # C는 금색만 지우면 원판의 검은 테두리가 네모 구멍처럼 남음(2026-11-08 확대 확인)


def cells(path, n, cols=None):
    s = Image.open(path).convert('RGBA')
    cols = cols or n
    return [s.crop(((k % cols) * S, (k // cols) * S, (k % cols) * S + S, (k // cols) * S + S)) for k in range(n)]


def fill_no_black(p, mask):
    """바깥 이웃 중 가장 많은 색으로 안쪽까지 채움. 검정(외곽선·음영)은 이웃이 검정뿐일 때만 — 검은 네모 구멍 방지(f4·f8)."""
    left = set(mask)
    while left:
        done = {}
        for x, y in left:
            nb = [p[x + dx, y + dy] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                  if (x + dx, y + dy) not in left and p[x + dx, y + dy][3]]
            col = [c for c in nb if c[:3] != (0, 0, 0)] or nb
            if col:
                done[(x, y)] = max(set(col), key=col.count)
        if not done:
            fill(p, left)
            break
        for q, c in done.items():
            p[q] = c
        left -= set(done)


def build():
    raw = cells(os.path.join(C, 'hit3_chop_raw_3x2.png'), 5, 3)
    appr = cells(os.path.join(D, 'history', '3_heavy_chop_east_v11_12f.png'), 12)   # 교체 전 12f 승인본
    chop = [raw[0]] + [snap(f) for f in raw[1:]]           # c1은 승인본 f1과 픽셀 같음 → 그대로
    p = chop[1].load()
    for x in range(S):
        for y in range(MARGIN):
            p[x, y] = (0, 0, 0, 0)
    frames = chop + appr[7:]
    mask = Image.new('L', (S * len(frames), S), 0)
    for k, (_, box) in PAULDRON.items():
        p, m = frames[k - 1].load(), mask.load()
        x0, y0, x1, y1 = box
        hole = {(x, y) for y in range(y0, y1 + 1) for x in range(x0, x1 + 1) if p[x, y][3] and p[x, y][:3] in GOLD}
        if k in DARK_RING:                       # 원판의 검은 테두리까지(몸 외곽선 열 x1은 남김)
            hole |= {(x, y) for y in range(y0, y1 + 1) for x in range(x0, x1) if p[x, y][3] and p[x, y][:3] == (0, 0, 0)}
        fill_no_black(p, hole)
        for x, y in hole:
            m[S * (k - 1) + x, y] = 255
    mask.save(os.path.join(C, 'chop10_pauldron_mask.png'))
    sheet = Image.new('RGBA', (S * len(frames), S), (0, 0, 0, 0))
    for k, f in enumerate(frames):
        sheet.paste(f, (S * k, 0))
    sheet.save(os.path.join(C, '3_heavy_chop_east_chop10.png'))
    return frames


def check(frames):
    s1 = cells(os.path.join(D, '1_light_slash_east.png'), 8)
    s2 = cells(os.path.join(D, '2_rising_slash_east.png'), 8)
    fails = []
    pal = {c[:3] for c in PAL}
    for k, f in enumerate(frames):
        d = list(f.get_flattened_data())
        if {c[3] for c in d} - {0, 255}:
            fails.append('f%d 반투명' % (k + 1))
        if {c[:3] for c in d if c[3]} - pal:
            fails.append('f%d 팔레트 밖 색' % (k + 1))
        b = f.getbbox()
        m = (b[0], b[1], S - b[2], S - b[3])
        if min(m) < MARGIN:
            fails.append('f%d 여백 %s' % (k + 1, m))
    colors = {c[:3] for f in frames for c in f.get_flattened_data() if c[3]}
    if len(colors) > 16:
        fails.append('색 %d개' % len(colors))
    if s2[7].tobytes() != frames[0].tobytes():
        fails.append('2타 f8 != 3타 f1')
    old = cells(os.path.join(C, 'history', '3_heavy_chop_east_chop10_v1_pauldron.png'), 10)
    for k, f in enumerate(frames):
        a, b = old[k].load(), f.load()
        diff = {(x, y) for y in range(S) for x in range(S) if a[x, y] != b[x, y]}
        box = PAULDRON.get(k + 1, ('', (0, 0, -1, -1)))[1]
        out = {q for q in diff if not (box[0] <= q[0] <= box[2] and box[1] <= q[1] <= box[3])}
        if diff:
            print('f%d 장식 %s: %dpx 바뀜, 상자 밖 %d' % (k + 1, PAULDRON[k + 1][0] if k + 1 in PAULDRON else '-', len(diff), len(out)))
        if out:
            fails.append('f%d 상자 밖 변경 %d' % (k + 1, len(out)))
    print('프레임 %d, %.2f초, 색 %d, 2타 f8 == 3타 f1: %s' % (len(frames), len(frames) / FPS, len(colors), s2[7].tobytes() == frames[0].tobytes()))
    chain = s1 + s2[1:] + frames[1:]                       # 연결 프레임은 한 번만
    bg = (58, 62, 72, 255)
    g = []
    for f in chain:
        c = Image.new('RGBA', (S, S), bg)
        c.alpha_composite(f)
        g.append(c.resize((S * 4, S * 4), Image.NEAREST).convert('RGB'))
    g[0].save(os.path.join(C, 'combo_chain_chop10_16fps_x4.gif'), save_all=True, append_images=g[1:],
              duration=[1000 // FPS] * (len(g) - 1) + [600], loop=0)
    print('이어 보기 %d장' % len(chain))
    for x in fails:
        print('실패:', x)
    return 1 if fails else 0


if __name__ == '__main__':
    sys.exit(check(build()))
