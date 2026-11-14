"""30번 두 번째 약공격 핵심 포즈 3장: 팔레트 맞춤·검사·미리보기. 생성 호출 없음, 확대·축소·이동 없음.
  python attack_slash02_keys.py      -> 30번(수평 되베기)
  python attack_slash02_keys.py up   -> 31번(올려베기, 2026-11-07)
  python attack_slash02_keys.py heavy -> 32번(3타 내려베기)
  결과: key_*.png, check_keys_20261107.txt, preview/keys_x4.png (실패가 있으면 종료 코드 1)
"""
import os
import sys
from PIL import Image
from attack_slash_east import snap, is_blade, px

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
F1 = 'f1_from_slash01_f8.png'
if sys.argv[1:] == ['heavy']:
    # 32번 3타 강한 내려베기 핵심 포즈(2026-11-07). 시작 = 2타 확정 f8
    D = os.path.join(ROOT, '32_attack_greatsword_heavy_slash_03_east')
    KEYS = [('b_strike', '내려치기 타격'), ('c_finish', '낮은 마무리')]
    F1 = 'f1_from_slash02_f8.png'
elif sys.argv[1:] == ['up']:
    D = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
    KEYS = [('a_prep', '준비'), ('b_strike', '타격'), ('c_finish', '높은 마무리'), ('c_finish_r2', '높은 마무리 r2'), ('c_finish_r3', '높은 마무리 r3')]
else:
    D = os.path.join(ROOT, '30_attack_greatsword_light_slash_02_east')
    KEYS = [('a_prep', 'f2 준비'), ('b_strike', 'f3~4 타격'), ('b_strike_r2', 'f3~4 타격 r2'), ('c_finish', 'f7~8 마무리')]
S, AX, AY = 96, 48, 93


def feet(im):
    p = im.load()
    b = im.getbbox()
    low = [x for x in range(S) if p[x, b[3] - 1][3]]
    fx = [x for y in range(b[3] - 3, b[3]) for x in range(S) if p[x, y][3] and not is_blade(p[x, y])]
    return b[3] - 1, (min(low) + max(low)) / 2, (min(fx), max(fx))


def blade_rows(im):
    """칼날 색(회색) 픽셀 수와 열마다 세로 두께 중앙값: 칼 두께 비교용."""
    p = im.load()
    cols = {}
    for y in range(S):
        for x in range(S):
            if is_blade(p[x, y]):
                cols.setdefault(x, []).append(y)
    th = sorted(len(v) for v in cols.values())
    return sum(th), th[len(th) // 2] if th else 0


def main():
    f1 = Image.open(os.path.join(D, F1)).convert('RGBA')
    ims = [('시작(2타 확정 f8)' if F1 != 'f1_from_slash01_f8.png' else 'f1 연결(확정 f8)', f1)]
    for n, label in KEYS:
        k = snap(Image.open(os.path.join(D, 'keys_raw', n + '.png')).convert('RGBA'))
        # pro_flash는 바닥을 y=94까지 그린다(원본 f8과 같은 줄). 이동하지 않고 그대로 둔다.
        k.save(os.path.join(D, f'key_{n}.png'))
        ims.append((label, k))
    rows, fails = [], 0

    def rec(name, std, val, how, ok):
        nonlocal fails
        fails += ok == '실패'
        rows.append(f'{ok}\t{name}\t기준 {std}\t실측 {val}\t{how}')

    allc = set()
    for label, im in ims:
        d = px(im)
        allc |= {q[:3] for q in d if q[3]}
        b = im.getbbox()
        m = (b[0], b[1], S - b[2], S - b[3])
        rec(f'{label} 영역·여백', '≤92×92, 각 ≥2', f'{b[2] - b[0]}×{b[3] - b[1]}, 좌상우하 {m}', 'bbox',
            '통과' if min(m) >= 2 and max(b[2] - b[0], b[3] - b[1]) <= 92 else '실패')
        y, cx, fx = feet(im)
        rec(f'{label} 발', f'f1과 같은 줄·같은 x범위 (미끄러짐 없음)', f'아랫줄 y={y}, 중앙 {cx:g}, 발 x {fx}', '아래 3줄, 칼색 제외', '미확인')
        n, th = blade_rows(im)
        rec(f'{label} 칼날 회색 픽셀', 'f1과 비슷(축소·변형 없음)', f'{n}px, 열 두께 중앙값 {th}', '회색 픽셀 수', '미확인')
        rec(f'{label} 알파', '0·255만', sorted({q[3] for q in d}), '전 픽셀', '통과' if {q[3] for q in d} <= {0, 255} else '실패')
    rec('전체 합산 RGB 색 수', '16 이하', len(allc), '알파 0 제외(팔레트 맞춤 후)', '통과' if len(allc) <= 16 else '실패')
    txt = '\n'.join(rows) + f'\n실패 {fails}건\nexit {1 if fails else 0}\n'
    open(os.path.join(D, 'check_keys_20261107.txt'), 'w', encoding='utf-8').write(txt)
    print(txt)

    bg = (58, 62, 72, 255)
    W = Image.new('RGBA', (S * len(ims), S), bg)
    for k, (_, im) in enumerate(ims):
        W.alpha_composite(im, (S * k, 0))
    W = W.resize((W.width * 4, W.height * 4), Image.NEAREST)
    q = W.load()
    _, cx0, _ = feet(f1)
    for k in range(len(ims)):
        for t in range(S * 4):
            q[S * 4 * k, t] = (120, 120, 120, 255)                    # 칸 경계
            q[S * 4 * k + int(cx0 * 4) + 2, t] = (220, 70, 70, 255)   # f1 발 중앙(빨강)
            q[S * 4 * k + t, 4 * (AY + 1)] = (70, 130, 220, 255)      # 바닥선 y=94(파랑)
    W.save(os.path.join(D, 'preview', 'keys_x4.png'))
    return fails


if __name__ == '__main__':
    sys.exit(1 if main() else 0)
