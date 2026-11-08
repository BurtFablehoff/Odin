"""V11 DEV override (Hatt 2026-11-07): whole-body original look/eyes/tail frames,
byte-exact original sitting base, approved-v1 rear (#6) and side (#8).

Run directly to (re)compute planning/owner-corrections-v11.json:
    python tools/local/Apply-OwnerCorrectionsV11.py
Build-StyleRecoveryV7.py calls apply_pack() after the V10 override.

Placement rule D2: each look/eyes/tail frame gets an integer (dx, dy) from
lower-body registration against its family base, applied through the frame's
anchorX/supportY so the placed lower body matches the base. Source pixels are
never edited.
"""
from pathlib import Path
import hashlib
import io
import json
import sys

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
POLICY = ROOT / 'planning/owner-corrections-v11.json'
FAMILIES = {'sit': 1, 'loaf': 2, 'lay': 3, 'sleep': 4}
CLIPS = ['look_sit', 'look_loaf', 'look_lay', 'sit_eyes', 'loaf_eyes', 'lay_eyes',
         'sit_tail', 'lay_tail', 'sleep_tail']
# Lower-body band (Step 0 evidence algorithm): alpha>64, rows y0..y1, cols x>=xmin.
# sleep is not in the Step 0 evidence; its tail sweeps x<~490, so its band starts at x 520.
BANDS = {'sit': (780, 950, 440), 'loaf': (820, 950, 420), 'lay': (820, 950, 420), 'sleep': (820, 950, 520)}
ORIGINAL_SIT_SHA256 = 'ab5c4261984e4f252172962198c2d532dffc8b8d269cfe23dd06d000808e7c43'
SEARCH = 8


def family(clip):
    if clip.startswith('look_'):
        return clip[5:]
    return clip[:-5]


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def band_mask(path, name):
    alpha = np.asarray(Image.open(path).convert('RGBA'))[:, :, 3]
    y0, y1, x0 = BANDS[name]
    mask = np.zeros(alpha.shape, bool)
    mask[y0:y1 + 1, x0:] = alpha[y0:y1 + 1, x0:] > 64
    return mask


def centroid(mask):
    ys, xs = np.nonzero(mask)
    return float(xs.mean()), float(ys.mean())


def best_shift(base, frame):
    """Integer (dx, dy) minimising lower-body mask mismatch: frame ~= base shifted by (dx, dy)."""
    costs = {}
    for dy in range(-SEARCH, SEARCH + 1):
        for dx in range(-SEARCH, SEARCH + 1):
            shifted = np.roll(np.roll(base, dy, axis=0), dx, axis=1)
            costs[(dx, dy)] = int(np.count_nonzero(shifted ^ frame))
    low = min(costs.values())
    winners = [k for k, v in costs.items() if v == low]
    dx, dy = min(winners, key=lambda k: abs(k[0]) + abs(k[1]))
    return low, dx, dy, len(winners)


def anchor_y(manifest, clip, frame):
    c = manifest['clips'][clip]
    return c['images'][frame].get('anchor_y', c.get('anchor_y', manifest['foot_anchor'][1]))


def compute():
    manifest = json.loads((ROOT / 'art/manifest.json').read_text())
    frames = []
    for clip in CLIPS:
        name = family(clip)
        base_path = ROOT / f'art/frames/{name}/000.png'
        base = band_mask(base_path, name)
        bcx, bcy = centroid(base)
        base_anchor = anchor_y(manifest, name, 0)
        for frame in range(manifest['clips'][clip]['frames']):
            path = ROOT / f'art/frames/{clip}/{frame:03d}.png'
            mask = band_mask(path, name)
            cx, cy = centroid(mask)
            ex, ey = cx - bcx, cy - bcy
            cost, sx, sy, ties = best_shift(base, mask)
            # Registration = minimum lower-body mask mismatch. Centroid offsets that remain are
            # fur/ruff shape changes, not translation; rounding them would shift the silhouette
            # off its best overlap. Ambiguous (tied minimum) frames fall back to the base anchor.
            ambiguous = ties > 1
            dx, dy = (0, 0) if ambiguous else (sx, sy)
            manifest_anchor = anchor_y(manifest, clip, frame)
            frames.append({
                'clip': clip, 'frame': frame, 'family': name,
                'source': f'art/frames/{clip}/{frame:03d}.png', 'sha256': sha(path),
                'imageCentroidOffset': [round(ex, 3), round(ey, 3)],
                'maskShift': [sx, sy], 'maskMismatchPixels': cost,
                'ambiguous': ambiguous, 'dx': dx, 'dy': dy,
                'anchorX': 512 + dx, 'supportY': base_anchor + dy,
                'manifestAnchorY': manifest_anchor,
                'placedDriftPx': round(float(np.hypot(ex - dx, ey - dy)), 3),
                'productionPlacedDriftPx': round(float(np.hypot(ex, (cy - manifest_anchor) - (bcy - base_anchor))), 3),
            })
    return frames


def write_policy():
    frames = compute()
    rear = json.loads((ROOT / 'draft-assets/restore-v11/registration.json').read_text()) \
        if (ROOT / 'draft-assets/restore-v11/registration.json').exists() else None
    summary = {}
    for name in ('sit', 'loaf', 'lay', 'sleep'):
        own = [f for f in frames if f['family'] == name]
        worst = max(own, key=lambda f: f['placedDriftPx'])
        worst_prod = max(own, key=lambda f: f['productionPlacedDriftPx'])
        summary[name] = {'frames': len(own), 'maxPlacedDriftPx': worst['placedDriftPx'],
                         'maxPlacedDriftFrame': f"{worst['clip']}/{worst['frame']:03d}",
                         'productionMaxPlacedDriftPx': worst_prod['productionPlacedDriftPx'],
                         'productionMaxFrame': f"{worst_prod['clip']}/{worst_prod['frame']:03d}",
                         'ambiguousFrames': sum(f['ambiguous'] for f in own)}
    policy = {
        'enabled': True, 'version': 11, 'date': '2026-11-07', 'developmentOnly': True,
        'ownerArtworkApproval': 'rear/side are owner-approved approved-sitting-v1 art ("approved. keep going"); '
                                'their 1024 registration and the v11 build await Hatt pass/fail',
        'ownerVerdicts': [
            'Sitting away #6 not proportionate: use draft-assets/approved-sitting-v1/rear.png',
            'Look-around must be whole-body frames with the body held still, not a head cut-out',
            'Sitting side #8 must be the short-nosed Persian from draft-assets/approved-sitting-v1/side.png',
        ],
        'supersedes': 'AGENTS.md V8 line "Do not return to whole-frame gaze rendering" (owner verdict 2026-11-07)',
        'wholeFrameRestFamilies': True,
        'sitBase': {'source': 'art/frames/sit/000.png', 'sha256': ORIGINAL_SIT_SHA256,
                    'note': 'D3: V10 right-flank rim repair (5128 px, never owner-approved) dropped; look frames do not carry it'},
        'placementRule': 'D2: per-frame integer (dx, dy) = minimum lower-body alpha-mask mismatch vs family base '
                         '(search +/-8 px); anchorX = 512 + dx, supportY = base anchor + dy; tied minima use the base anchor',
        'band': {k: {'y0': v[0], 'y1': v[1], 'xMin': v[2], 'alphaThreshold': 64} for k, v in BANDS.items()},
        'driftThresholdPx': 3,
        'summary': summary,
        'frames': frames,
        'poseOverrides': rear['poseOverrides'] if rear else [],
    }
    POLICY.write_text(json.dumps(policy, indent=2) + '\n')
    return policy


def settings():
    value = json.loads(POLICY.read_text()) if POLICY.exists() else {}
    return value if value.get('enabled') else None


def apply_config(config):
    """Persisted numbered-review config (planning/state-head-review.json)."""
    policy = settings()
    if not policy:
        return config
    sit = next(p for p in config['poses'] if p['id'] == 1)
    sit.update(source=policy['sitBase']['source'], sourceSHA256=policy['sitBase']['sha256'], supportY=944,
               anchorX=512, status='Original sitting, byte-exact (V11 D3)')
    for override in policy['poseOverrides']:
        assert sha(ROOT / override['source']) == override['sourceSHA256'], override['source']
        pose = next(p for p in config['poses'] if p['id'] == override['id'])
        for key in ('source', 'sourceSHA256', 'supportY', 'scaleAtSmall', 'headCrop', 'anchorX'):
            pose[key] = override[key]
        pose['customAnchor'] = True
        pose['status'] = override['description'] + '; v11 DEV registration, owner pass/fail pending'
    config['ownerCorrections'] = 'planning/owner-corrections-v11.json'
    return config


def apply_pack(manifest, payloads):
    policy = settings()
    if not policy:
        return manifest, payloads
    # Whole-frame mode: draw original look/eyes/tail frames whole (no head layer on a frozen body).
    manifest['fixedOriginalRestBodies'] = False
    manifest['wholeFrameRestFamilies'] = True
    sit_data = (ROOT / policy['sitBase']['source']).read_bytes()
    assert hashlib.sha256(sit_data).hexdigest() == policy['sitBase']['sha256'] == ORIGINAL_SIT_SHA256
    pose = next(p for p in manifest['poses'] if p['id'] == 1)
    neutral = next(p for p in manifest['actions'] if p['clip'] == 'sit' and p['frame'] == 0)
    for entry in (pose, neutral):
        entry.update(source=policy['sitBase']['source'], sha256=ORIGINAL_SIT_SHA256, nativeWidth=1024,
                     nativeHeight=1024, supportY=944, anchorX=512, customAnchor=True,
                     renderWidthAtSmall=neutral['renderWidthAtSmall'])
        entry.pop('rightFurEdgeRepair', None)
        payloads[entry['key']] = sit_data
    pose['label'] = '1. Sitting (original)'
    with Image.open(io.BytesIO(sit_data)) as image:
        box = image.getchannel('A').point(lambda a: 255 if a >= 8 else 0).getbbox()
    pose['bounds'] = neutral['bounds'] = [box[0], box[1], box[2] - box[0], box[3] - box[1]]
    table = {(f['clip'], f['frame']): f for f in policy['frames']}
    applied = 0
    for entry in manifest['actions']:
        key = (entry['clip'], entry['frame'])
        if key not in table:
            continue
        row = table[key]
        data = (ROOT / row['source']).read_bytes()
        assert hashlib.sha256(data).hexdigest() == row['sha256'] == entry['sha256'], row['source']
        assert entry['source'] == row['source']
        payloads[entry['key']] = data
        entry.update(anchorX=row['anchorX'], supportY=row['supportY'], customAnchor=True,
                     registrationDx=row['dx'], registrationDy=row['dy'], wholeFrame=True)
        applied += 1
    assert applied == 69, f'expected 69 look/eyes/tail slots, applied {applied}'
    for override in policy['poseOverrides']:
        data = (ROOT / override['source']).read_bytes()
        assert hashlib.sha256(data).hexdigest() == override['sourceSHA256'], override['source']
        with Image.open(io.BytesIO(data)) as image:
            size = image.size
            box = image.getchannel('A').point(lambda a: 255 if a > 8 else 0).getbbox()
        target = next(p for p in manifest['poses'] if p['id'] == override['id'])
        target.update(source=override['source'], sha256=override['sourceSHA256'],
                      bounds=[box[0], box[1], box[2] - box[0], box[3] - box[1]],
                      nativeWidth=size[0], nativeHeight=size[1], supportY=override['supportY'],
                      renderWidthAtSmall=size[0] * override['scaleAtSmall'], customAnchor=True,
                      anchorX=override['anchorX'], headCrop=override['headCrop'],
                      label=override['label'], notes=override['description'],
                      ownerArtworkApproved=False, approvedSourceArtwork=True)
        target.pop('motionBounds', None)
        payloads[target['key']] = data
    manifest['ownerCorrectionsV11'] = {k: v for k, v in policy.items() if k != 'frames'}
    manifest['headSourceRegistrationVersion'] = 11
    manifest['completeStyleCorrection'] = False
    return manifest, payloads


if __name__ == '__main__':
    result = write_policy()
    print(json.dumps(result['summary'], indent=2))
    if '--update-config' in sys.argv:
        path = ROOT / 'planning/state-head-review.json'
        path.write_text(json.dumps(apply_config(json.loads(path.read_text())), indent=2) + '\n')
        print('Updated', path)
