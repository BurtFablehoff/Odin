"""V12 2b DEV: run_left / run_right become DEV action clips, registered by the head and played one frame per 16 ms
render tick. Each clip holds the original frames (byte-exact art/frames/run_* PNGs) and the kept optical-flow tweens
(draft-assets/run-tweens-v12, Tween-RunFramesV12.py) in play order, as listed in planning/run-registration-v12.json
(Measure-RunHeadsV12.py). Every entry's anchorX / supportY carry that frame's head registration:
  anchorX  = foot_anchor x - dx      supportY = clip anchor_y - dy
so DevArtFrame.Destination draws exactly what RunningMotion / RunningRegistration describe (checked by
--test-run-motion). Production keeps its 18 / 16 atlas frames and v11 timing; nothing under art/ is written."""
import hashlib
import io
import json
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
REGISTRATION = ROOT / 'planning/run-registration-v12.json'
ART = ROOT / 'art/manifest.json'
FRAME_SECONDS = 0.016  # one frame per render tick (Desktop.cs timer interval); RunningMotion.FrameSeconds
IDS = {'run_left': 14, 'run_right': 15}


def visible_box(data):
    with Image.open(io.BytesIO(data)) as image:
        alpha = np.asarray(image.convert('RGBA'))[..., 3]
        size = image.size
    ys, xs = np.nonzero(alpha > 8)
    return [int(xs.min()), int(ys.min()), int(xs.max() - xs.min() + 1), int(ys.max() - ys.min() + 1)], size


def apply_pack(manifest, payloads):
    registration = json.loads(REGISTRATION.read_text())
    art = json.loads(ART.read_text())
    foot_x = art['foot_anchor'][0]
    widths = manifest['legacyActionWidths']
    assert not any(a['clip'] in IDS for a in manifest['actions']), 'run clips are already DEV actions'
    summary = {}
    for clip, pose_id in IDS.items():
        info = art['clips'][clip]
        anchors = {f['anchor_y'] for f in info['images']}
        assert anchors == {info['anchor_y']}, (clip, 'per-frame anchor_y differs; a tween would need its own', anchors)
        entry = registration['clips'][clip]
        frames = (entry.get('dev') or {'frames': entry['native']})['frames']
        originals = [r for r in frames if not r['tween']]
        assert [r['frame'] for r in originals] == [f'{i:03d}' for i in range(info['frames'])], (clip, 'originals out of order')
        for number, row in enumerate(frames):
            data = (ROOT / row['source']).read_bytes()
            box, size = visible_box(data)
            assert size == (1024, 1024), (row['source'], size)
            record = {'id': pose_id, 'key': f'action-{clip}-{number:03d}.png',
                      'label': f"{clip} frame {number} ({'tween ' + row['frame'] if row['tween'] else 'original ' + row['frame']})",
                      'clip': clip, 'frame': number, 'source': row['source'], 'sha256': hashlib.sha256(data).hexdigest(),
                      'kind': 'tween' if row['tween'] else 'original', 'phase': row['frame'], 'bounds': box,
                      'nativeWidth': 1024, 'nativeHeight': 1024, 'customAnchor': True,
                      'anchorX': round(foot_x - row['dx'], 2), 'supportY': round(info['anchor_y'] - row['dy'], 2),
                      'renderWidthAtSmall': widths[clip], 'airborne': False, 'grounded': True,
                      'headRegistrationV12': {'noseX': row['x'], 'noseY': row['y'], 'dx': row['dx'], 'dy': row['dy']},
                      'sourcePixelsUnmodified' if not row['tween'] else 'draftTween': True,
                      'notes':('Original run frame, byte-exact; placed by its head (nose) registration.' if not row['tween'] else
                                f"Optical-flow tween between {row['between'][0]} and {row['between'][1]} (draft, ghost gate passed); "
                                'placed midway between their heads.')}
            manifest['actions'].append(record)
            payloads[record['key']] = data
        manifest['actionClips'].append({'clip': clip, 'frames': len(frames), 'seconds': round(len(frames) * FRAME_SECONDS, 6), 'loop': True})
        summary[clip] = {'frames': len(frames), 'tweens': sum(r['tween'] for r in frames), 'seconds': round(len(frames) * FRAME_SECONDS, 6)}
    manifest['runHeadRegistrationV12'] = {'registration': 'planning/run-registration-v12.json', 'tweens': 'draft-assets/run-tweens-v12',
                                          'frameSeconds': FRAME_SECONDS, 'clips': summary, 'ownerApproved': False}
    return manifest, payloads
