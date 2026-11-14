"""32번 3타 강한 내려베기 12프레임 시트 조립·검사·미리보기(16fps). 생성 호출 없음, 확대·축소·이동 없음.
  python attack_slash03_build.py [v2] -> 0_east.png(1152×96) / v2: 0_east_v2.png(f2·f3 허리 위 = f1), check, preview/* (실패가 있으면 종료 코드 1)
"""
import os
import sys
from PIL import Image
from attack_slash_east import snap, is_blade, px

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D = os.path.join(ROOT, '32_attack_greatsword_heavy_slash_03_east')
S, FPS, AY = 96, 16, 93
# 2026-11-07 고른 이유: segA frame_03은 칼을 머리 뒤로 다시 젖힘("다시 들어 올리기" 금지), frame_04는 초승달 잔상(새 효과 금지) → 뺌.
# segB frame_03·04는 마무리와 거의 같아 뺌. segC는 1~4를 모두 써서 마무리 뒤 잠깐 안정 → idle 복귀.
FRAMES = [('f1_from_slash02_f8.png', '시작(2타 f8)'), ('mid_raw/segA/frame_01.png', '힘 모으기 1'),
          ('mid_raw/segA/frame_02.png', '힘 모으기 2(칼 세움)'), ('key_b_strike.png', '타격'),
          ('mid_raw/segB/frame_01.png', '감속 1'), ('mid_raw/segB/frame_02.png', '감속 2'),
          ('key_c_finish.png', '낮은 마무리'), ('mid_raw/segC/frame_01.png', '안정'),
          ('mid_raw/segC/frame_02.png', '복귀 1'), ('mid_raw/segC/frame_03.png', '복귀 2'),
          ('mid_raw/segC/frame_04.png', '복귀 3'), ('idle_96.png', '원본 idle')]
N = len(FRAMES)
# v2(2026-11-07 사용자: "3타 시작 때 칼이 약간 올라간다, 2타 끝 위치와 같게"): f2·f3은 허리 위(y<70)를 f1 그대로,
# 허리 아래(골반·다리)만 segA 생성본 → 칼·손·팔·머리 위치는 f1과 같고 무릎으로 힘 모으는 느낌만 남김
V2 = sys.argv[1:] == ['v2']
CUT = 70
TOP_FROM_F1 = {1, 2} if V2 else set()
OUT_TAG = '_v2' if V2 else ''
EXACT = {0: 'f1_from_slash02_f8.png', 3: 'key_b_strike.png', 6: 'key_c_finish.png', 11: 'idle_96.png'}


def main():
    frames = []
    for n, _ in FRAMES:
        im = Image.open(os.path.join(D, n)).convert('RGBA')
        frames.append(im if n in EXACT.values() else snap(im))
    for k in TOP_FROM_F1:
        f = frames[k].copy()
        f.paste(Image.new('RGBA', (S, CUT), (0, 0, 0, 0)), (0, 0))
        f.alpha_composite(frames[0].crop((0, 0, S, CUT)), (0, 0))
        frames[k] = f
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
    sh = im.convert('RGBA')
    rec('형식·시트 크기', f'PNG {S * N}×{S}', f'{im.format} {sh.width}×{sh.height}', '파일 열기',
        '통과' if im.format == 'PNG' and sh.size == (S * N, S) else '실패')
    d = px(sh)
    al = sorted({q[3] for q in d})
    rec('알파', '0·255만', al, '전 픽셀', '통과' if al == [0, 255] else '실패')
    cols = {q[:3] for q in d if q[3]}
    rec('RGB 색 수', '16 이하', len(cols), '알파 0 제외 합산', '통과' if len(cols) <= 16 else '실패')
    fs = [sh.crop((S * k, 0, S * k + S, S)) for k in range(N)]
    rec('프레임 수·간격·패딩', f'{N}·0·0', f'{N}칸 96px 연속, 빈 칸 {[k + 1 for k, f in enumerate(fs) if not f.getbbox()]}', '칸 계산',
        '통과' if all(f.getbbox() for f in fs) else '실패')
    same = [sum(a != b for a, b in zip(px(fs[k]), px(Image.open(os.path.join(D, n)).convert('RGBA')))) for k, n in EXACT.items()]
    rec('f1·f4·f7·f12 = 시작·승인 포즈·idle', '차이 0', same, '픽셀 비교', '통과' if not any(same) else '실패')
    feet = []
    for k, f in enumerate(fs):
        b = f.getbbox()
        m = (b[0], b[1], S - b[2], S - b[3])
        rec(f'f{k + 1} 영역·여백', '≤92×92, 각 ≥2', f'{b[2] - b[0]}×{b[3] - b[1]}, 좌상우하 {m}', 'bbox',
            '통과' if min(m) >= 2 and max(b[2] - b[0], b[3] - b[1]) <= 92 else '실패')
        p = f.load()
        fx = [x for y in range(AY - 2, AY + 1) for x in range(S) if p[x, y][3] and not is_blade(p[x, y])]
        feet.append((min(fx), max(fx)) if fx else None)
    rec('발 x범위(아래 3줄, 칼색 제외)', '미끄러짐 없음', feet, '측정값, 판정은 눈', '미확인')
    ch = [sum(a != b for a, b in zip(px(fs[k]), px(fs[k + 1]))) for k in range(N - 1)]
    rec('이웃 프레임 바뀐 픽셀', '튐·정지 확인용', ch, '픽셀 비교, 판정은 눈', '미확인')
    rec('재생 시간', '16fps 12프레임', f'{N}÷{FPS}={N / FPS}초', '계산', '통과')
    rec('기준점 X', '48(명세)', '1·2타에서 이어받은 발 위치(시작 f1 발 중앙 47) 유지', '이전 시트와 같은 원인', '미확인')
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
            q[ox + t, 2 * (AY + 1)] = (70, 130, 220, 255)
        for t in range(4, 2 * S - 4):
            for a, b in ((ox + t, 4), (ox + t, 2 * S - 5), (ox + 4, t), (ox + 2 * S - 5, t)):
                q[a, b] = (220, 70, 70, 255)
    g.save(os.path.join(D, 'preview', f'guides{OUT_TAG}_x2.png'))
    first = Image.open(os.path.join(ROOT, '29_attack_greatsword_light_slash_east', '0_east_pauldron_w.png')).convert('RGBA')
    second = Image.open(os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east', '0_east_v2d.png')).convert('RGBA')
    chain = ([first.crop((S * k, 0, S * k + S, S)) for k in range(8)] + [second.crop((S * k, 0, S * k + S, S)) for k in range(1, 8)]
             + fs[1:])
    for name, fr in ((f'slash03{OUT_TAG}_16fps_x4.gif', fs), (f'slash01_02_03_chain{OUT_TAG}_16fps_x4.gif', chain)):
        gif = []
        for f in fr:
            c = Image.new('RGBA', (S, S), bg)
            c.alpha_composite(f)
            gif.append(c.resize((S * 4, S * 4), Image.NEAREST).convert('RGB'))
        gif[0].save(os.path.join(D, 'preview', name), save_all=True, append_images=gif[1:],
                    duration=([60, 70] * 40)[:len(gif) - 1] + [600], loop=0, disposal=1)
    return fails


if __name__ == '__main__':
    sys.exit(1 if main() else 0)
