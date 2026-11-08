"""V12 3bR-1 DEV: add the 12-hour clock frames (planning/clock-registration-v12.json, Register-ClockV12.py) to the DEV pack
as whole-frame clips clock_rear / clock_lookback / clock_side: 12 frames each, frame index = hour order 12, 1, 2, ..., 11
(index 0 = 12 o'clock = head up), matching app/ClockGaze.cs ClockAngles {0, 30, ..., 330}. Every frame is the family's
ONE fixed body (the 3R centre frame, which Apply-HeadTurnsV12.py already made the state's DEV still) with only the head and
neck above the seam row changed, so it is placed exactly like the still (same anchorX / supportY / renderWidthAtSmall).

A family with any hour missing is left out of the pack and listed in manifest.clockV12.leftOut; its still then has no head
turns at all, because since 3bR-2 (0e5508d) the 5-frame look_* clips are no longer emitted (Apply-HeadTurnsV12.py
EMIT_LOOK_CLIPS = False; only its centre-as-still step remains) and app/DevArtwork.cs tracks clock_* with 12 frames. Production art is untouched; nothing under art/ is read or written.
Called from Build-DevArtworkPack.py after Apply-HeadTurnsV12.py."""
import hashlib
import io
import json
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
REGISTRATION = ROOT / 'planning/clock-registration-v12.json'
HOURS = [12, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11]
# DevArtwork.cs (action timeline check) accepts seconds == 0 only for discrete look_* gazes; until 3bR-2 teaches it the
# clock_* clips, each clock clip carries the tracker's own stepping time: 12 frames x 65 ms (ClockGaze.StepToFrame).
CLOCK_SECONDS = 12 * 0.065


def decode(data):
    with Image.open(io.BytesIO(data)) as image:
        return np.asarray(image.convert('RGBA'))


def visible_box(rgba):
    ys, xs = np.nonzero(rgba[..., 3] > 8)
    return [int(xs.min()), int(ys.min()), int(xs.max() - xs.min() + 1), int(ys.max() - ys.min() + 1)]


def apply_pack(manifest, payloads):
    registration = json.loads(REGISTRATION.read_text())
    assert registration['hourOrder'] == HOURS, registration['hourOrder']
    poses = {p['id']: p for p in manifest['poses']}
    in_pack, left_out, families = [], [], {}
    for name, family in registration['families'].items():
        clip = family['clip']
        assert clip == f'clock_{name}' and not any(a['clip'] == clip for a in manifest['actions']), clip
        summary = {'id': family['id'], 'clip': clip, 'seamRow': family['seamRow'], 'featherRows': family['featherRows'],
                   'hoursPresent': family['hoursPresent'], 'hoursMissing': family['hoursMissing'],
                   'shelfHours': family['shelfHours'], 'clippedHours': family['clippedHours'],
                   'headRescaled': family['headRescaled'], 'headUnmeasured': family['headUnmeasured'], 'inPack': False}
        families[name] = summary
        if family['hoursMissing']:
            left_out.append({'family': name, 'id': family['id'], 'hoursMissing': family['hoursMissing'],
                             'reason': 'Codex returns missing for these hours; the state keeps its look_* head turns'})
            continue
        pose = poses[family['id']]
        # The still is the 3R centre (Apply-HeadTurnsV12.py), byte-identical to the registration's fixed body.
        assert pose['sha256'] == family['centre']['sha256'], (name, pose['sha256'], family['centre']['sha256'])
        for key in ('anchorX', 'supportY', 'renderWidthAtSmall'):
            assert float(pose[key]) == float(family[key]), (name, key, pose[key], family[key])
        still = decode(payloads[pose['key']])
        below = family['seamRow'] + family['featherRows'] // 2
        for index, hour in enumerate(HOURS):
            frame = family['frames'][f'h{hour:02d}']
            assert frame['index'] == index and frame['hour'] == hour, (name, hour)
            data = (ROOT / frame['file']).read_bytes()
            assert hashlib.sha256(data).hexdigest() == frame['sha256'], frame['file']
            rgba = decode(data)
            assert rgba.shape == (1024, 1024, 4), (frame['file'], rgba.shape)
            # One fixed body: every row from seam + feather/2 down is the still's pixels.
            differing = int((rgba[below:] != still[below:]).any(axis=2).sum())
            assert differing == 0, (name, hour, differing)
            record = {'id': family['id'], 'key': f'action-{clip}-{index:03d}.png',
                      'label': f"{clip} frame {index} ({hour} o'clock)", 'clip': clip, 'frame': index,
                      'source': frame['file'], 'sha256': frame['sha256'], 'bounds': visible_box(rgba),
                      'nativeWidth': 1024, 'nativeHeight': 1024, 'customAnchor': True,
                      'anchorX': pose['anchorX'], 'supportY': pose['supportY'], 'renderWidthAtSmall': pose['renderWidthAtSmall'],
                      'airborne': False, 'grounded': True, 'wholeFrame': True, 'hour': hour,
                      'clockV12': {'codexSource': frame['source'], 'seamRow': frame['seamRow'],
                                   'alignmentScaleVsCentre': frame['alignment']['returnToCentre1254']['scale'],
                                   'headRatio': frame['headRatioBeforeRescale'], 'headRescaled': frame['headRescaled'],
                                   'shelfPx': frame['shelfPx'], 'clipped': frame['clipped'], 'belowSeamDiffPx': differing},
                      'ownerArtworkApproved': False,
                      'notes': 'V12 3bR-1 clock frame: the fixed 3R centre body below the neck seam, the Codex head and neck above it (DEV).'}
            manifest['actions'].append(record)
            payloads[record['key']] = data
        manifest['actionClips'].append({'clip': clip, 'frames': len(HOURS), 'seconds': CLOCK_SECONDS, 'loop': False})
        summary['inPack'] = True
        in_pack.append(name)
    manifest['clockV12'] = {'registration': REGISTRATION.relative_to(ROOT).as_posix(), 'inPack': in_pack,
                            'leftOut': [f['family'] for f in left_out], 'leftOutDetail': left_out,
                            'hourOrder': HOURS, 'framesPerFamily': len(HOURS),
                            'frameIndexMeaning': "index = ClockGaze.ClockAngles index: 0 = 12 o'clock (head up), clockwise",
                            'clipSeconds': CLOCK_SECONDS, 'families': families, 'ownerApproved': False}
    print('Clock v12: in pack', in_pack or 'none', '; left out', [(f['family'], f['hoursMissing']) for f in left_out] or 'none')
    return manifest, payloads
