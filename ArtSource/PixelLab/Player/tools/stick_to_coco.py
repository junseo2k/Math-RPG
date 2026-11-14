"""승인 막대기(stick_poses.json) 한 타 → animate_with_skeleton_v3 keypoints (2026-11-13 서쪽 1타 시험).
쉬는 자세 관절(estimate_skeleton 결과)에 막대기 f1 대비 이동량을 더한다. 팔꿈치·무릎은 어깨-손목, 엉덩이-발목
중간점에 쉬는 자세의 굽힘 벡터를 더한다. python stick_to_coco.py <dir> <hit> <rest.json> <out.json>"""
import json, sys
import numpy as np
D, HIT, REST, OUT = sys.argv[1:5]
st = json.load(open('../35_attack_greatsword_combo_6dir/01_stick/stick_poses.json', encoding='utf-8'))
fr = [p for p in st[D] if p['hit'] == HIT]
rest = json.load(open(REST, encoding='utf-8')); W = rest['width']
R = {k['label']: np.array([k['x'], k['y']]) * W for k in rest['keypoints']}
Z = {k['label']: k['z_index'] for k in rest['keypoints']}
P = lambda f, k: np.array(f[k], float)
f1 = fr[0]
out = []
for f in fr:
    d = {k: P(f, k) - P(f1, k) for k in ('hip', 'head', 'shL', 'handR', 'handL', 'footR', 'footL')}
    J = {}
    for k in ('NOSE', 'NECK', 'RIGHT EYE', 'LEFT EYE', 'RIGHT EAR', 'LEFT EAR'): J[k] = R[k] + d['head']
    J['LEFT SHOULDER'] = R['LEFT SHOULDER'] + d['shL']
    J['RIGHT SHOULDER'] = R['RIGHT SHOULDER'] + (d['head'] + d['hip']) / 2
    J['RIGHT HIP'] = R['RIGHT HIP'] + d['hip']; J['LEFT HIP'] = R['LEFT HIP'] + d['hip']
    J['RIGHT ARM'] = R['RIGHT ARM'] + d['handR']; J['LEFT ARM'] = R['LEFT ARM'] + d['handL']
    J['RIGHT LEG'] = R['RIGHT LEG'] + d['footR']; J['LEFT LEG'] = R['LEFT LEG'] + d['footL']
    for mid, a, b in (('RIGHT ELBOW', 'RIGHT SHOULDER', 'RIGHT ARM'), ('LEFT ELBOW', 'LEFT SHOULDER', 'LEFT ARM'),
                      ('RIGHT KNEE', 'RIGHT HIP', 'RIGHT LEG'), ('LEFT KNEE', 'LEFT HIP', 'LEFT LEG')):
        J[mid] = (J[a] + J[b]) / 2 + (R[mid] - (R[a] + R[b]) / 2)
    out.append([{'label': k, 'x': round(float(J[k][0] / W), 4), 'y': round(float(J[k][1] / W), 4), 'z_index': Z[k]}
                for k in R])
json.dump(out, open(OUT, 'w', encoding='utf-8'), indent=0)
print(len(out), 'frames')
