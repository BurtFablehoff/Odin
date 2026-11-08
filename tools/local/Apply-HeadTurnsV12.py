"""V12 3R DEV: add the registered Codex head-turn frames (planning/head-turns-registration-v12.json,
Register-HeadTurnsV12.py) to the DEV pack as whole-frame look clips look_rear / look_lookback / look_side (5 frames,
far-left..far-right), placed exactly like their state's still. For a family that passed the 3 px lower-body stability
check, the CENTRE frame replaces that state's still in DEV; a family that failed keeps today's still, is left out of the
pack and is listed in manifest.headTurnsV12.leftOut. Production art is untouched; nothing under art/ is read or written.
Called from Build-DevArtworkPack.py after Apply-RunTweensV12.py."""
import hashlib
import io
import json
import runpy
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
REGISTRATION = ROOT / 'planning/head-turns-registration-v12.json'
DIRECTIONS = ['far-left', 'left', 'centre', 'right', 'far-right']


def visible_box(data):
    with Image.open(io.BytesIO(data)) as image:
        alpha = np.asarray(image.convert('RGBA'))[..., 3]
        size = image.size
    ys, xs = np.nonzero(alpha > 8)
    return [int(xs.min()), int(ys.min()), int(xs.max() - xs.min() + 1), int(ys.max() - ys.min() + 1)], size


def apply_pack(manifest, payloads):
    registration = json.loads(REGISTRATION.read_text())
    poses = {p['id']: p for p in manifest['poses']}
    in_pack, left_out = [], []
    for name, family in registration['families'].items():
        clip = family['clip']
        assert clip == f'look_{name}' and not any(a['clip'] == clip for a in manifest['actions']), clip
        if not family['inPack']:
            left_out.append({'family': name, 'id': family['id'], 'maxDriftNativePx': family['maxDriftNativePx'],
                             'reason': 'lower-body drift above 3 native px; the state keeps its current still'})
            continue
        pose = poses[family['id']]
        width = family['renderWidthAtSmall']
        assert float(pose['renderWidthAtSmall']) == float(width), (name, pose['renderWidthAtSmall'], width)
        assert pose['supportY'] == family['centre']['today']['supportY'] or family['id'] == 7, (name, pose['supportY'])
        datas = {}
        for number, direction in enumerate(DIRECTIONS):
            frame = family['frames'][direction]
            data = (ROOT / frame['file']).read_bytes()
            assert hashlib.sha256(data).hexdigest() == frame['sha256'], frame['file']
            box, size = visible_box(data)
            assert size == (1024, 1024), (frame['file'], size)
            datas[direction] = data
            record = {'id': family['id'], 'key': f'action-{clip}-{number:03d}.png',
                      'label': f'{clip} frame {number} ({direction})', 'clip': clip, 'frame': number,
                      'source': frame['file'], 'sha256': frame['sha256'], 'bounds': box,
                      'nativeWidth': 1024, 'nativeHeight': 1024, 'customAnchor': True,
                      'anchorX': family['anchorX'], 'supportY': family['supportY'], 'renderWidthAtSmall': width,
                      'airborne': False, 'grounded': True, 'wholeFrame': True, 'direction': direction,
                      'headTurnV12': {'codexSource': frame['source'], 'scale': frame['scale'], 'offset': frame['offset'],
                                      'driftNativePx': frame['driftNativePx'], 'silhouetteIoU': frame['silhouetteIoU']},
                      'ownerArtworkApproved': False,
                      'notes': 'Codex head-turn frame (whole cat), uniformly resampled and registered to the centre frame by the lower body.'}
            manifest['actions'].append(record)
            payloads[record['key']] = data
        manifest['actionClips'].append({'clip': clip, 'frames': len(DIRECTIONS), 'seconds': 0, 'loop': False})
        centre = family['frames']['centre']
        box, _ = visible_box(datas['centre'])
        before = {'source': pose.get('source'), 'sha256': pose['sha256'], 'nativeWidth': pose['nativeWidth'], 'supportY': pose['supportY']}
        for stale in ('motionBounds', 'approvedSourceArtwork', 'tailOverhangOwnerApproved'):
            pose.pop(stale, None)
        pose.update({'source': centre['file'], 'sha256': centre['sha256'], 'bounds': box, 'nativeWidth': 1024, 'nativeHeight': 1024,
                     'customAnchor': True, 'anchorX': family['anchorX'], 'supportY': family['supportY'],
                     'headCrop': family['headCrop'], 'label': family['label'], 'ownerArtworkApproved': False,
                     'notes': 'Codex head-turn centre frame, registered into the 1024 canvas by the state size rule (V12 3R, DEV)',
                     'headTurnsV12': {'before': before, 'clip': clip, 'sizeRule': family['centre']['sizeRule']}})
        if family['id'] == 6:
            pose['tailOverhangOwnerApproved'] = True
        payloads[pose['key']] = datas['centre']
        in_pack.append(name)
    # The 6/8 seated-height owner exception still holds on the new stills.
    sizes = runpy.run_path(str(ROOT / 'tools/local/Apply-OwnerSizesV12.py'))
    keys = {p['key'] for p in manifest['poses'] if p['id'] in (1, 6, 8)}
    table = sizes['seated_heights'](manifest, {k: payloads[k] for k in keys})
    sizes['print_table'](table, 'Head turns v12: seated heights of the DEV stills at the small size')
    for pid in (6, 8):
        assert abs(table[pid]['screenPx'] - table[1]['screenPx']) <= 1.0, (pid, table[pid])
    manifest['headTurnsV12'] = {'registration': REGISTRATION.relative_to(ROOT).as_posix(), 'inPack': in_pack,
                                'leftOut': [f['family'] for f in left_out], 'leftOutDetail': left_out,
                                'framesPerFamily': len(DIRECTIONS), 'directions': DIRECTIONS,
                                'seatedHeightsScreenPx': {str(k): v['screenPx'] for k, v in table.items()}, 'ownerApproved': False}
    return manifest, payloads
