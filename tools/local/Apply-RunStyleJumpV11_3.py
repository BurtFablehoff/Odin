"""V11.3 / V12 2c DEV: draw original running frames for the middle of each jump
(planning/run-style-jump-v11-3.json). No pixels are generated or edited.

V12 2c: the policy's `slots` say which original run frame each jump slot draws and its flight role
(push-off, rising, apex, descending). Every run frame puts its body centroid at one fixed offset from
the anchor (x: the seated takeoff crouch's centroid; y: the push-off frame standing on its lowest paw),
so the body follows CatMind's arc and the DEV lean pivots about a fixed point. The flight roles go into
the pack as `jumpFlight` for the DEV runtime (app/JumpTilt.cs)."""
import hashlib
import io
import json
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
POLICY = ROOT / 'planning/run-style-jump-v11-3.json'
FLIGHT = ('rising', 'apex', 'descending')


def body(data):
    with Image.open(io.BytesIO(data)) as image:
        alpha = np.asarray(image.convert('RGBA'))[..., 3]
    ys, xs = np.nonzero(alpha > 64)
    loose = image_box(alpha)
    return float(xs.mean()), float(ys.mean()), int(ys.max()), loose


def image_box(alpha):
    ys, xs = np.nonzero(alpha >= 8)
    return [int(xs.min()), int(ys.min()), int(xs.max() - xs.min() + 1), int(ys.max() - ys.min() + 1)]


def check_roles(roles, frames):
    slots = sorted(s for key in ('preFlight', 'rising', 'apex', 'descending', 'landing') for s in roles[key])
    assert slots == list(range(frames)), ('flight roles must cover every jump slot once', slots)
    assert len(roles['apex']) == 1 and 0 < roles['apexHold'] < 0.5, roles


def apply_pack(manifest, payloads):
    if not POLICY.exists():
        return manifest, payloads
    policy = json.loads(POLICY.read_text())
    roles = policy['flightRoles']
    widths = manifest['legacyActionWidths']
    applied = 0
    for clip, slots in policy['slots'].items():
        entries = {a['frame']: a for a in manifest['actions'] if a['clip'] == clip}
        check_roles(roles, len(entries))
        run_clip = policy['runClip'][clip]
        rs = widths[run_clip] / 1024.0
        # Fixed body offset (screen px at the small size) shared by every run frame of this jump.
        crouch = [s['slot'] for s in slots if s['role'] == 'preFlight' and 'runFrame' not in s]
        offsets = []
        for number in crouch:
            entry = entries[number]
            cx, _, _, _ = body(payloads[entry['key']])
            offsets.append((cx - entry['anchorX']) * entry['renderWidthAtSmall'] / entry['nativeWidth'])
        dx = sum(offsets) / len(offsets)
        push = [s for s in slots if s['role'] == 'preFlight' and 'runFrame' in s]
        assert len(push) == 1, ('exactly one push-off run frame', push)
        ordered = push + [s for s in slots if 'runFrame' in s and s['role'] != 'preFlight']
        dy = None
        for spec in ordered:
            assert spec['role'] == 'preFlight' or spec['role'] in FLIGHT, spec
            entry = entries[spec['slot']]
            source = f"art/frames/{run_clip}/{spec['runFrame']:03d}.png"
            data = (ROOT / source).read_bytes()
            rcx, rcy, rbottom, box = body(data)
            anchor_x = rcx - dx / rs
            if spec['role'] == 'preFlight':
                support_y = float(rbottom)
                dy = (rcy - support_y) * rs
            else:
                support_y = rcy - dy / rs
            airborne = spec['role'] in FLIGHT
            entry.update(source=source, sha256=hashlib.sha256(data).hexdigest(), bounds=box,
                         anchorX=round(anchor_x, 2), supportY=round(support_y, 2), customAnchor=True,
                         renderWidthAtSmall=widths[run_clip], nativeWidth=1024, nativeHeight=1024,
                         airborne=airborne, grounded=not airborne,
                         runStyleJumpV11_3={'runClip': run_clip, 'runFrame': spec['runFrame'], 'role': spec['role'],
                                            'why': spec['why']},
                         sourcePixelsEdited=False, jumpRuffV11_2=False)
            entry.pop('originalSource', None)
            entry.pop('originalSHA256', None)
            payloads[entry['key']] = data
            applied += 1
    expected = sum(1 for slots in policy['slots'].values() for s in slots if 'runFrame' in s)
    assert applied == expected, (applied, expected)
    poses = {p['id']: p for p in manifest['poses']}
    for pid, clip in ((16, 'jump_left'), (17, 'jump_right')):
        action = next(a for a in manifest['actions'] if a['clip'] == clip and a['frame'] == 8)
        pose = poses[pid]
        pose.update({k: action[k] for k in ('source', 'sha256', 'bounds', 'anchorX', 'supportY',
                                            'renderWidthAtSmall', 'nativeWidth', 'nativeHeight')})
        pose['label'] = f"{pid}. Jumping {'left' if pid == 16 else 'right'} (run-style v11.3)"
        payloads[pose['key']] = payloads[action['key']]
    manifest['jumpFlight'] = {key: roles[key] for key in ('preFlight', 'rising', 'apex', 'descending', 'landing', 'apexHold')}
    manifest['runStyleJumpV11_3'] = {'policy': 'planning/run-style-jump-v11-3.json', 'version': policy['version'],
                                     'frames': applied, 'flightRoles': True, 'ownerApproved': False}
    return manifest, payloads
