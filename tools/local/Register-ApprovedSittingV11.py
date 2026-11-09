"""V11: register owner-approved draft-assets/approved-sitting-v1/{rear,side}.png (1254 px)
into the 1024 sit runtime canvas (root x 512, support y 944). Source pixels are only
uniformly resampled; nothing is repainted or retextured.

Two scale candidates per pose, because the manual landmark metrics disagree by more
than planning/anatomical-sizing-v10.json uncertaintyNativePixels (3):
  head  - skull width / ear-apex spacing matched to art/frames/sit/000.png (physicalScaleRule)
  owner - the rear/side-to-front ratio Hatt last requested in the numbered review
          (numbered-sizing-baseline-v2 = v1 x his "+20% bigger"; its sources share
          approved-v1 geometry within ~15 px, and its front is sit/000 x 1.20)
`SELECTED` goes into the DEV build as rear.png / side.png; both are on the review sheet.
Never scaled by canvas, alpha bbox, tail reach or ear-to-floor height.
"""
from pathlib import Path
import hashlib
import json
import math

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'draft-assets/restore-v11'
SIT = ROOT / 'art/frames/sit/000.png'
CANVAS, ROOT_X, SUPPORT_Y, MARGIN = 1024, 512, 944, 4

# Manual landmarks (native px of each file; +/-3 px). Front values for sit/000.
SIT_LANDMARKS = {
    'nearPinnaApex': [447, 529], 'nearPinnaBase': [454, 585],      # anatomical-sizing-v10.json
    'earApexLeft': [450, 527], 'earApexRight': [614, 533],          # solid pinna apexes
    'skullOuterLeft': [447, 580], 'skullOuterRight': [620, 590],    # outer ear-base skull line
    'workingEyeLeft': [482, 615], 'workingEyeRight': [516, 615],    # eye opening width
}
POSES = {
    'rear': {
        'id': 6, 'label': '6. Sitting away (approved v1)',
        'source': 'draft-assets/approved-sitting-v1/rear.png',
        'landmarks': {
            'earApexLeft': [695, 112], 'earApexRight': [942, 110],
            'skullOuterLeft': [690, 180], 'skullOuterRight': [945, 180],
            'nearPinnaApex': [695, 112], 'nearPinnaBase': [690, 180],
        },
        'rootX': 818,       # spine line = ear-apex midpoint
        'supportY': 1172,   # last row whose widest solid (alpha>128) run is >=25% of the max row width
        'headBox': [660, 90, 980, 330],
        'ownerScaleAtSmall': 0.22652027027027027, 'ownerSource': 'draft-assets/numbered-sizing-v2/sit-rear.png',
    },
    'side': {
        'id': 8, 'label': '8. Sitting side (approved v1 Persian)',
        'source': 'draft-assets/approved-sitting-v1/side.png',
        'landmarks': {
            'earApexLeft': [842, 52], 'earApexRight': [1108, 55],
            'workingEyeLeft': [941, 262], 'workingEyeRight': [999, 262],
            'nearPinnaApex': [845, 77], 'nearPinnaBase': [818, 180],
        },
        'rootX': 700,       # between haunch and front paws, tail excluded (manual)
        'supportY': 1172,
        'headBox': [790, 40, 1140, 420],
        'ownerScaleAtSmall': 0.21750648788927335, 'ownerSource': 'draft-assets/numbered-sizing-v2/sit-side.png',
    },
}
OWNER_FRONT_SCALE_AT_SMALL = 0.41339712918660293   # numbered-sizing-baseline-v2 pose 1 (legacy sit-front 1254)
LEGACY_FRONT_PER_SIT000 = 1.20                      # legacy sit-front = sit/000 x 1.20 (ear spacing 198/165, height 510/425)
SELECTED = {'rear': 'owner', 'side': 'owner'}


def dist(a, b):
    return math.hypot(a[0] - b[0], a[1] - b[1])


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def metrics(lm):
    out = {}
    if 'earApexLeft' in lm:
        out['earApexSpacing'] = dist(lm['earApexLeft'], lm['earApexRight'])
    if 'skullOuterLeft' in lm:
        out['skullWidth'] = dist(lm['skullOuterLeft'], lm['skullOuterRight'])
    if 'workingEyeLeft' in lm:
        out['eyeWidth'] = dist(lm['workingEyeLeft'], lm['workingEyeRight'])
    out['pinnaLength'] = dist(lm['nearPinnaApex'], lm['nearPinnaBase'])
    return out


def render(src, scale, root_x, support_y):
    image = Image.open(src).convert('RGBA')
    w, h = image.size
    size = (round(w * scale), round(h * scale))
    scaled = image.convert('RGBa').resize(size, Image.LANCZOS).convert('RGBA')
    ox = ROOT_X - round(root_x * scale)
    oy = SUPPORT_Y - round(support_y * scale)
    box = scaled.getchannel('A').point(lambda a: 255 if a > 8 else 0).getbbox()
    shift = 0
    if ox + box[0] < MARGIN:
        shift = MARGIN - (ox + box[0])
    elif ox + box[2] > CANVAS - MARGIN:
        shift = CANVAS - MARGIN - (ox + box[2])
    ox += shift
    canvas = Image.new('RGBA', (CANVAS, CANVAS), (0, 0, 0, 0))
    canvas.alpha_composite(scaled, (ox, oy)) if ox >= 0 and oy >= 0 else canvas.paste(scaled, (ox, oy), scaled)
    out_box = canvas.getchannel('A').point(lambda a: 255 if a > 8 else 0).getbbox()
    assert out_box[0] > 0 and out_box[1] > 0 and out_box[2] < CANVAS and out_box[3] < CANVAS, 'fur touches canvas edge'
    return canvas, ox, oy, shift, out_box


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    sit_m = metrics(SIT_LANDMARKS)
    sit_rate = 1 / (OWNER_FRONT_SCALE_AT_SMALL * LEGACY_FRONT_PER_SIT000)   # sit/000 px per small px in that review
    record = {'version': 11, 'date': '2026-11-07', 'reference': 'art/frames/sit/000.png', 'referenceSHA256': sha(SIT),
              'referenceLandmarks': SIT_LANDMARKS, 'referenceMetrics': sit_m,
              'canvas': CANVAS, 'rootX': ROOT_X, 'supportY': SUPPORT_Y, 'uncertaintyNativePixels': 3,
              'landmarksAreManual': True, 'ownerApprovedAnatomicalMeasurement': False,
              'resample': 'uniform LANCZOS on premultiplied alpha; no repaint, no retexture',
              'caveat': 'approved-v1 rear/side fur is longer/wavier than sit/000; Hatt approved them as-is, no retexture attempted',
              'poses': {}, 'poseOverrides': []}
    for name, pose in POSES.items():
        src = ROOT / pose['source']
        m = metrics(pose['landmarks'])
        ratios = {k: sit_m[k] / v for k, v in m.items() if k in sit_m}
        head_keys = [k for k in ('earApexSpacing', 'skullWidth', 'eyeWidth') if k in ratios]
        head_scale = sum(ratios[k] for k in head_keys) / len(head_keys)
        owner_scale = pose['ownerScaleAtSmall'] * sit_rate
        candidates = {}
        for cand, scale in (('head', head_scale), ('owner', owner_scale)):
            canvas, ox, oy, shift, box = render(src, scale, pose['rootX'], pose['supportY'])
            path = OUT / f'{name}-{cand}.png'
            canvas.save(path, optimize=True)
            residual = {k: round(m[k] * scale - sit_m[k], 2) for k in ratios}
            hb = pose['headBox']
            candidates[cand] = {
                'file': path.relative_to(ROOT).as_posix(), 'sha256': sha(path), 'scale': round(scale, 6),
                'offset': [ox, oy], 'horizontalShiftToFit': shift, 'anchorX': ROOT_X + shift,
                'alpha8Bounds': list(box), 'landmarkResidualNativePx': residual,
                'withinUncertainty': {k: abs(v) <= 3 for k, v in residual.items()},
                'headCrop': [round(hb[0] * scale + ox), round(hb[1] * scale + oy),
                             round(hb[2] * scale + ox), round(hb[3] * scale + oy)],
            }
        pick = SELECTED[name]
        chosen = candidates[pick]
        final = OUT / f'{name}.png'
        final.write_bytes((ROOT / chosen['file']).read_bytes())
        record['poses'][name] = {
            'id': pose['id'], 'source': pose['source'], 'sourceSHA256': sha(src), 'landmarks': pose['landmarks'],
            'metrics': m, 'ratiosToSit000': ratios, 'rootX': pose['rootX'], 'supportY': pose['supportY'],
            'ownerSizedFrom': pose['ownerSource'], 'candidates': candidates, 'selected': pick,
            'output': final.relative_to(ROOT).as_posix(), 'outputSHA256': sha(final),
            'ambiguous': not all(all(c['withinUncertainty'].values()) for c in candidates.values()),
        }
        record['poseOverrides'].append({
            'id': pose['id'], 'source': final.relative_to(ROOT).as_posix(), 'sourceSHA256': sha(final),
            'supportY': SUPPORT_Y, 'scaleAtSmall': 0.46875, 'anchorX': chosen['anchorX'], 'headCrop': chosen['headCrop'],
            'label': pose['label'],
            'description': f"Owner-approved approved-sitting-v1 {name}, uniformly registered into the sit 1024 canvas "
                           f"({pick} scale candidate {chosen['scale']:.3f})",
        })
    (OUT / 'registration.json').write_text(json.dumps(record, indent=2) + '\n')
    for name, value in record['poses'].items():
        print(name, 'selected', value['selected'], {k: (v['scale'], v['landmarkResidualNativePx']) for k, v in value['candidates'].items()})


if __name__ == '__main__':
    main()
