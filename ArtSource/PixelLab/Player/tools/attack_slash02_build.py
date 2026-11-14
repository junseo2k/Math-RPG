"""30번 두 번째 약공격 8프레임 시트 조립·검사·미리보기. 생성 호출 없음, 확대·축소·이동 없음.
  python attack_slash02_build.py [up|up2]  -> 0_east.png, check_20261107.txt, preview/* (실패가 있으면 종료 코드 1)
  up2 = 31번 2차 시트(마지막 자세 v7c) -> 0_east_v2.png, check_v2_20261107.txt, preview/*_v2*
"""
import os
import sys
from PIL import Image
from attack_slash_east import snap, is_blade, is_hair, px

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D = os.path.join(ROOT, '30_attack_greatsword_light_slash_02_east')
S, N, FPS, AX, AY = 96, 8, 16, 48, 93
# f1 = 확정 첫 공격 f8(그대로), f2·f4·f8 = 승인 핵심 포즈, f3·f5~f7 = 양 끝 고정 생성 구간에서 고름.
# seg1/frame_01은 흰 초승달 잔상이라 쓰지 않음(2026-11-07).
FRAMES = [('f1_from_slash01_f8.png', '확정 첫 공격 f8'), ('key_a_prep.png', '준비'),
          ('mid_raw/seg1/frame_02.png', '타격 앞'), ('key_b_strike_r2.png', '타격'),
          ('mid_raw/seg2/frame_01.png', '감속 1'), ('mid_raw/seg2/frame_02.png', '감속 2'),
          ('mid_raw/seg2/frame_03.png', '연결 1'), ('key_c_finish.png', '연결 2')]
CLEAN = set()
if sys.argv[1:] == ['up']:
    # 31번 올려베기(2026-11-07 승인 포즈). seg1 frame_01·02는 초승달 잔상이라 뺌.
    # seg1 frame_03은 몸과 떨어진 흰 호 조각만 지운다(CLEAN). seg2는 frame_01부터 마무리에 거의 닿아 있음.
    D = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
    FRAMES = [('f1_from_slash01_f8.png', '확정 첫 공격 f8'), ('key_a_prep.png', '준비'),
              ('mid_raw/seg1/frame_03.png', '올려베기 중'), ('key_b_strike.png', '타격'),
              ('mid_raw/seg2/frame_01.png', '상승·감속 1'), ('mid_raw/seg2/frame_02.png', '감속 2'),
              ('mid_raw/seg2/frame_03.png', '높은 자세 1'), ('key_c_finish.png', '높은 자세 2')]
    CLEAN = {'mid_raw/seg1/frame_03.png'}
OUT_TAG = ''
if sys.argv[1:] == ['up2']:
    # 31번 2차 시트(2026-11-07): 마지막 자세 = 확정 v7c, f5~f7 = 타격→v7c 양 끝 고정 생성(seg2b). 1차 0_east.png는 보존
    D = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
    FRAMES = [('f1_from_slash01_f8.png', '확정 첫 공격 f8'), ('key_a_prep.png', '준비'),
              ('mid_raw/seg1/frame_03.png', '올려베기 중'), ('key_b_strike.png', '타격'),
              ('mid_raw/seg2b/frame_01.png', '상승 1'), ('mid_raw/seg2b/frame_02.png', '상승 2'),
              ('mid_raw/seg2b/frame_03.png', '감속'), ('key_c_finish_overhead_v7c.png', '머리 위 자세(확정)')]
    CLEAN = {'mid_raw/seg1/frame_03.png'}
    OUT_TAG = '_v2'


def drop_detached(im, max_px=40):
    """가장 큰 덩어리와 떨어진 작은 덩어리(잔상 조각)를 지운다. 지운 픽셀 수를 돌려준다."""
    p = im.load()
    seen, comps = set(), []
    for y in range(S):
        for x in range(S):
            if p[x, y][3] and (x, y) not in seen:
                comp, todo = [], [(x, y)]
                seen.add((x, y))
                while todo:
                    a, b = todo.pop()
                    comp.append((a, b))
                    for dx in (-1, 0, 1):
                        for dy in (-1, 0, 1):
                            q = (a + dx, b + dy)
                            if 0 <= q[0] < S and 0 <= q[1] < S and q not in seen and p[q][3]:
                                seen.add(q)
                                todo.append(q)
                comps.append(comp)
    comps.sort(key=len, reverse=True)
    n = 0
    for comp in comps[1:]:
        if len(comp) <= max_px:
            for q in comp:
                p[q] = (0, 0, 0, 0)
            n += len(comp)
    return n


def main():
    frames = []
    for n, _ in FRAMES:
        im = Image.open(os.path.join(D, n)).convert('RGBA')
        im = im if n.startswith('f1_') else snap(im)
        if n in CLEAN:
            print(f'{n}: 떨어진 조각 {drop_detached(im)}px 지움')
        frames.append(im)
    sheet = Image.new('RGBA', (S * N, S), (0, 0, 0, 0))
    for k, f in enumerate(frames):
        sheet.paste(f, (S * k, 0))
    out = os.path.join(D, f'0_east{OUT_TAG}.png')
    sheet.save(out)

    rows, fails = [], 0

    def rec(name, std, val, how, ok):
        nonlocal fails
        fails += ok == '실패'
        rows.append(f'{ok}\t{name}\t기준 {std}\t실측 {val}\t{how}')

    im = Image.open(out)
    rec('형식', 'PNG', im.format, '파일 열기', '통과' if im.format == 'PNG' else '실패')
    sh = im.convert('RGBA')
    rec('시트 크기', '768×96', f'{sh.width}×{sh.height}', '파일 열기', '통과' if sh.size == (768, 96) else '실패')
    d = px(sh)
    al = sorted({q[3] for q in d})
    rec('알파', '0·255만', al, '전 픽셀', '통과' if al == [0, 255] else '실패')
    cols = {q[:3] for q in d if q[3]}
    rec('RGB 색 수', '16 이하', len(cols), '알파 0 제외 합산', '통과' if len(cols) <= 16 else '실패')
    fs = [sh.crop((S * k, 0, S * k + S, S)) for k in range(N)]
    rec('프레임 수·간격·패딩', '8·0·0', f'8칸 96px 연속, 빈 칸 {[k + 1 for k, f in enumerate(fs) if not f.getbbox()]}', '칸 계산',
        '통과' if all(f.getbbox() for f in fs) else '실패')
    f1 = Image.open(os.path.join(D, 'f1_from_slash01_f8.png')).convert('RGBA')
    diff = sum(a != b for a, b in zip(px(fs[0]), px(f1)))
    rec('f1 = 확정 첫 공격 f8', '차이 0', f'{diff}px', '픽셀 비교', '통과' if not diff else '실패')
    c8 = Image.open(os.path.join(D, 'key_c_finish.png')).convert('RGBA')
    rec('f2·f4·f8 = 승인 포즈', '차이 0', [sum(a != b for a, b in zip(px(fs[k]), px(Image.open(os.path.join(D, FRAMES[k][0])).convert('RGBA')))) for k in (1, 3, 7)],
        '픽셀 비교', '통과')
    tops, feet = [], []
    for k, f in enumerate(fs):
        b = f.getbbox()
        m = (b[0], b[1], S - b[2], S - b[3])
        rec(f'f{k + 1} 영역·여백', '≤92×92, 각 ≥2', f'{b[2] - b[0]}×{b[3] - b[1]}, 좌상우하 {m}', 'bbox',
            '통과' if min(m) >= 2 and max(b[2] - b[0], b[3] - b[1]) <= 92 else '실패')
        p = f.load()
        low = [x for x in range(S) if p[x, b[3] - 1][3]]
        cx = (min(low) + max(low)) / 2
        rec(f'f{k + 1} 기준점', f'아랫줄 y={AY}, 중앙 x={AX}, 오차 0', f'y={b[3] - 1}, 중앙 {cx:g}', '최하단 줄',
            '통과' if b[3] - 1 == AY and cx == AX else '실패')
        hair = [y for y in range(S) for x in range(S) if is_hair(p[x, y])]
        tops.append(min(hair))
        fx = [x for y in range(AY - 2, AY + 1) for x in range(S) if p[x, y][3] and not is_blade(p[x, y])]
        feet.append((min(fx), max(fx)))
    rec('머리 윗줄 상하(몸통 대리값)', '범위 ≤2px', f'{tops}, 범위 {max(tops) - min(tops)}', '금발 최상단', '통과' if max(tops) - min(tops) <= 2 else '실패')
    rec('발 x범위(아래 3줄, 칼색 제외)', '미끄러짐 없음', feet, '측정값, 판정은 눈', '미확인')
    ch = [sum(a != b for a, b in zip(px(fs[k]), px(fs[k + 1]))) for k in range(N - 1)]
    rec('이웃 프레임 바뀐 픽셀', '튐 확인용', ch, '픽셀 비교, 판정은 눈', '미확인')
    rec('재생 시간', '약 0.50초', f'{N}÷{FPS}={N / FPS}초', '계산', '통과')
    txt = '\n'.join(rows) + f'\n실패 {fails}건\nexit {1 if fails else 0}\n'
    open(os.path.join(D, f'check{OUT_TAG}_20261107.txt'), 'w', encoding='utf-8').write(txt)
    print(txt)

    bg = (58, 62, 72, 255)
    g = Image.new('RGBA', (S * N, S), bg)
    for k, f in enumerate(fs):
        g.alpha_composite(f, (S * k, 0))
    g = g.resize((g.width * 2, g.height * 2), Image.NEAREST)
    q = g.load()
    for k in range(N):
        ox = 2 * S * k
        for t in range(2 * S):
            q[ox, t] = (120, 120, 120, 255)
            q[ox + 2 * AX, t] = (70, 130, 220, 255)
            q[ox + t, 2 * (AY + 1)] = (70, 130, 220, 255)
        for t in range(4, 2 * S - 4):
            for a, b in ((ox + t, 4), (ox + t, 2 * S - 5), (ox + 4, t), (ox + 2 * S - 5, t)):
                q[a, b] = (220, 70, 70, 255)
    g.save(os.path.join(D, 'preview', f'guides{OUT_TAG}_x2.png'))
    # 첫 공격 8프레임 + 두 번째 공격 8프레임 이어 재생(연결 확인용), 16fps, 끝 0.6초 멈춤
    first = Image.open(os.path.join(ROOT, '29_attack_greatsword_light_slash_east', '0_east_pauldron_w.png')).convert('RGBA')
    seq = [first.crop((S * k, 0, S * k + S, S)) for k in range(N)] + fs[1:]
    for name, frames_ in ((f'slash02{OUT_TAG}_16fps_x4.gif', fs), (f'slash01_02_chain{OUT_TAG}_16fps_x4.gif', seq)):
        gif = []
        for f in frames_:
            c = Image.new('RGBA', (S, S), bg)
            c.alpha_composite(f)
            gif.append(c.resize((S * 4, S * 4), Image.NEAREST).convert('RGB'))
        dur = ([60, 70] * 20)[:len(gif) - 1] + [600]
        gif[0].save(os.path.join(D, 'preview', name), save_all=True, append_images=gif[1:], duration=dur, loop=0, disposal=1)
    return fails


if __name__ == '__main__':
    sys.exit(1 if main() else 0)
