"""Check actual rendered iris progression and unchanged baseline payloads."""
import hashlib
import json
import sys
from pathlib import Path
from zipfile import ZipFile
import cv2
import numpy as np

root = Path(__file__).resolve().parents[2]
folder = root / 'build/motion-v8'
snapshot = root / 'draft-assets/approved-transition-preview-v3'
approval = json.loads((snapshot / 'approval.json').read_text(encoding='utf-8-sig'))
for item in approval['files']:
    assert hashlib.sha256((snapshot / item['path']).read_bytes()).hexdigest() == item['sha256'], item['path']
frames = json.loads((folder / 'frames.json').read_text())
report = {}
for clip, rows in frames.items():
    assert all(rows[i]['segment']+rows[i]['t'] < rows[i+1]['segment']+rows[i+1]['t'] for i in range(len(rows)-1)), (clip, 'out of order')
    points = []
    for row in rows:
        image = cv2.imread(row['file'], cv2.IMREAD_UNCHANGED)
        hsv = cv2.cvtColor(image[..., :3], cv2.COLOR_BGR2HSV)
        mask = ((hsv[..., 0] > 20) & (hsv[..., 0] < 80) & (hsv[..., 1] > 70) & (image[..., 3] > 200)).astype(np.uint8)
        mask[:500] = 0; mask[880:] = 0; mask[:, :400] = 0; mask[:, 720:] = 0
        n, labels, stats, centers = cv2.connectedComponentsWithStats(mask)
        candidates = [j for j in range(1, n) if 15 < stats[j, 4] < 700]
        assert candidates, (clip, row['frame'], 'iris missing')
        iris = max(candidates, key=lambda j: stats[j, 4])
        x, y = centers[iris]
        points.append([x+512-row['anchorX'], y+944-row['supportY']])
    points = np.array(points)
    steps = np.linalg.norm(np.diff(points, axis=0), axis=1)
    backwards = np.maximum(0, -np.diff(points[:, 1])) if clip.startswith('sit_') else np.maximum(0, np.diff(points[:, 1]))
    report[clip] = dict(frames=len(rows), maxIrisStepNative=float(steps.max()), maxReverseVerticalStepNative=float(backwards.max()), iris=points.tolist())
    assert steps.max() < 14, (clip, 'jerky iris movement', steps.max())
    assert backwards.max() < 3, (clip, 'backwards posture step', backwards.max())

with ZipFile(root / 'build/tail-v7/candidate.zip') as before, ZipFile(folder / 'candidate.zip') as after:
    for name in before.namelist():
        if name != 'manifest.json':
            assert before.read(name) == after.read(name), name
    a, b = [json.loads(z.read('manifest.json')) for z in (before, after)]
    assert b.pop('smoothRestV8') is True
    changed = {'sit_to_loaf', 'loaf_to_sit', 'sit_to_lay', 'lay_to_sit', 'loaf_to_lay', 'lay_to_loaf'}
    for forward in ('sit_to_loaf', 'sit_to_lay', 'loaf_to_lay'):
        start, end = forward.split('_to_')
        first = sorted((x for x in b['actions'] if x['clip'] == forward), key=lambda x:x['frame'])
        back = sorted((x for x in b['actions'] if x['clip'] == end+'_to_'+start), key=lambda x:x['frame'], reverse=True)
        assert len(first) == len(back)
        for x, y in zip(first, back):
            assert all(x.get(key) == y.get(key) for key in ('key', 'sha256', 'anchorX', 'supportY', 'renderWidthAtSmall')), (forward, 'reverse order mismatch')
    for key in ('actions', 'actionClips'):
        old, new = a.pop(key), b.pop(key)
        assert [x for x in old if x['clip'] not in changed] == [x for x in new if x['clip'] not in changed], key
    assert a == b, 'Other metadata changed'

result = dict(passed=True, motion=report, unchangedV7Payloads=True, unchangedOtherMetadata=True, tailV7Unchanged=True)
(folder / 'motion-audit.json').write_text(json.dumps(result, indent=2))
print(json.dumps({clip: {k:v for k,v in value.items() if k != 'iris'} for clip,value in report.items()}))
