"""run_calm skeletons (2026-10-30): poses of the in-game run, moved onto the in-game idle with no body bob.

  python run_skeleton.py estimate      # POST /v2/estimate-skeleton on game idle f0 + game run f0..7 -> 12_run_calm/skeleton/<dir>.json
  python run_skeleton.py keypoints     # -> 12_run_calm/skeleton/<dir>_job.json (first_frame_keypoints + 8 bob-free run poses)
Token: the PixelLab MCP header in ~/.claude.json (never printed).
"""
import base64, json, os, sys, urllib.request
import numpy as np
from pngio import read_png, write_png

HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.join(HERE, '..', '..', '..', '..', 'Assets', 'Resources', 'Art')
OUT = os.path.join(HERE, '..', '12_run_calm', 'skeleton')
DIRS = {'east': 'e', 'north-east': 'ne', 'north': 'n', 'north-west': 'nw',
        'west': 'w', 'south-west': 'sw', 'south': 's', 'south-east': 'se'}
TORSO = ('NECK', 'RIGHT SHOULDER', 'LEFT SHOULDER', 'RIGHT HIP', 'LEFT HIP')


def token():
    cfg = json.load(open(os.path.join(os.path.expanduser('~'), '.claude.json'), encoding='utf-8'))
    for p in cfg.get('projects', {}).values():
        s = p.get('mcpServers', {}).get('pixellab')
        if s:
            return s['headers']['Authorization']
    raise SystemExit('pixellab MCP token not found')


def frames(state, d):
    s = read_png(os.path.join(ART, f'chr_player_{state}_{DIRS[d]}.png'))
    return [s[:, 64 * i:64 * (i + 1)] for i in range(s.shape[1] // 64)]


def png_b64(img):
    p = os.path.join(OUT, '_tmp.png')
    write_png(p, img)
    return base64.b64encode(open(p, 'rb').read()).decode()


def estimate(img, auth):
    body = json.dumps({'image': {'type': 'base64', 'base64': png_b64(img)}}).encode()
    req = urllib.request.Request('https://api.pixellab.ai/v2/estimate-skeleton', body,
                                 {'Authorization': auth, 'Content-Type': 'application/json'})
    return json.load(urllib.request.urlopen(req, timeout=120))['keypoints']


def cmd_estimate(only):
    auth = token()
    os.makedirs(OUT, exist_ok=True)
    for d in DIRS:
        if only and d != only:
            continue
        res = dict(idle=estimate(frames('idle', d)[0], auth), run=[estimate(f, auth) for f in frames('run', d)])
        json.dump(res, open(os.path.join(OUT, f'{d}.json'), 'w'), indent=1)
        print(d, 'ok')


def center(kp):
    pts = np.array([[k['x'], k['y']] for k in kp if k['label'] in TORSO])
    return pts.mean(0)


def cmd_keypoints(only):
    for d in DIRS:
        if only and d != only:
            continue
        sk = json.load(open(os.path.join(OUT, f'{d}.json')))
        home = center(sk['idle'])
        # torso centre of every run pose moves onto the idle's torso centre: in-place, 0 px body bob (spec)
        poses = []
        for kp in sk['run']:
            dx, dy = home - center(kp)
            poses.append([dict(k, x=round(k['x'] + dx, 4), y=round(k['y'] + dy, 4)) for k in kp])
        json.dump(dict(first_frame_keypoints=sk['idle'], keypoints=poses),
                  open(os.path.join(OUT, f'{d}_job.json'), 'w'))
        print(d, 'shifts px', [tuple(np.round((home - center(k)) * 64, 1)) for k in sk['run']])


def cmd_fetch(arg):
    """fetch <dir>=<job_id>[,<dir>=<job_id>...] -> 01_raw/clean/run_calm/<dir>/frame_00 (game idle start) + frame_01..08"""
    auth = token()
    for pair in arg.split(','):
        d, job = pair.split('=')
        out = os.path.join(HERE, '..', '01_raw', 'clean', 'run_calm', d)
        os.makedirs(out, exist_ok=True)
        write_png(os.path.join(out, 'frame_00.png'), frames('idle', d)[0])
        for i in range(8):
            req = urllib.request.Request(f'https://api.pixellab.ai/mcp/images/{job}/download?index={i}', headers={'Authorization': auth})
            open(os.path.join(out, f'frame_{i + 1:02d}.png'), 'wb').write(urllib.request.urlopen(req, timeout=120).read())
        print(d, 'fetched')


if __name__ == '__main__':
    {'estimate': cmd_estimate, 'keypoints': cmd_keypoints, 'fetch': cmd_fetch}[sys.argv[1]](sys.argv[2] if len(sys.argv) > 2 else None)
