"""V11.3 DEV: draw original running frames for the middle of each jump
(planning/run-style-jump-v11-3.json). No pixels are generated or edited."""
import hashlib
import io
import json
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
POLICY = ROOT / 'planning/run-style-jump-v11-3.json'


def body(data):
    with Image.open(io.BytesIO(data)) as image:
        alpha = np.asarray(image.convert('RGBA'))[..., 3]
    ys, xs = np.nonzero(alpha > 64)
    loose = image_box(alpha)
    return float(xs.mean()), float(ys.mean()), int(ys.max()), loose


def image_box(alpha):
    ys, xs = np.nonzero(alpha >= 8)
    return [int(xs.min()), int(ys.min()), int(xs.max() - xs.min() + 1), int(ys.max() - ys.min() + 1)]


def apply_pack(manifest, payloads):
    if not POLICY.exists():
        return manifest, payloads
    policy = json.loads(POLICY.read_text())
    widths = manifest['legacyActionWidths']
    applied = 0
    for entry in manifest['actions']:
        mapping = policy['map'].get(entry['clip'])
        if not mapping or str(entry['frame']) not in mapping:
            continue
        run_clip = policy['runClip'][entry['clip']]
        run_frame = mapping[str(entry['frame'])]
        source = f'art/frames/{run_clip}/{run_frame:03d}.png'
        data = (ROOT / source).read_bytes()
        old = payloads[entry['key']]
        jcx, jcy, _, _ = body(old)
        rcx, rcy, rbottom, box = body(data)
        js = entry['renderWidthAtSmall'] / entry['nativeWidth']
        rs = widths[run_clip] / 1024.0
        anchor_x = rcx - (jcx - entry['anchorX']) * js / rs
        if entry.get('grounded'):
            support_y = float(rbottom)
        else:
            support_y = rcy - (jcy - entry['supportY']) * js / rs
        entry.update(source=source, sha256=hashlib.sha256(data).hexdigest(), bounds=box,
                     anchorX=round(anchor_x, 2), supportY=round(support_y, 2), customAnchor=True,
                     renderWidthAtSmall=widths[run_clip], nativeWidth=1024, nativeHeight=1024,
                     runStyleJumpV11_3={'runClip': run_clip, 'runFrame': run_frame},
                     sourcePixelsEdited=False, jumpRuffV11_2=False)
        entry.pop('originalSource', None)
        entry.pop('originalSHA256', None)
        payloads[entry['key']] = data
        applied += 1
    expected = sum(len(v) for v in policy['map'].values())
    assert applied == expected, (applied, expected)
    poses = {p['id']: p for p in manifest['poses']}
    for pid, clip in ((16, 'jump_left'), (17, 'jump_right')):
        action = next(a for a in manifest['actions'] if a['clip'] == clip and a['frame'] == 8)
        pose = poses[pid]
        pose.update({k: action[k] for k in ('source', 'sha256', 'bounds', 'anchorX', 'supportY',
                                            'renderWidthAtSmall', 'nativeWidth', 'nativeHeight')})
        pose['label'] = f"{pid}. Jumping {'left' if pid == 16 else 'right'} (run-style v11.3)"
        payloads[pose['key']] = payloads[action['key']]
    manifest['runStyleJumpV11_3'] = {'policy': 'planning/run-style-jump-v11-3.json', 'frames': applied,
                                     'ownerApproved': False}
    return manifest, payloads
