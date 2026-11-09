"""V12 3f DEV: every gaze (look_*) frame of a rest family carries the family base's underside (floor rules F1/F2, planning/RULES.md).

Hatt, 2026-11-08: "he's still floating ... looks like look around is different than the static image". The original look_sit frames
draw the belly's last row 3-5 native rows above the static sit's (body underside 938-941 vs 943 with the same support row 944),
so while Odin looks around his belly lifts off the taskbar line and the black wallpaper letterbox shows under him. The fix uses
only the family base's own original pixels: below a seam row the look frame becomes the base (premultiplied blend over a feather
band above the seam), so the underside and paws are pixel-identical to the static still in every gaze frame. Nothing is
generated, nothing moves (support rows unchanged), the head and upper body are the original look frame. Families whose look
frames already end on or below the base's underside (loaf, lay) are measured and left alone. Entry sha256 values are updated for
the loader's hash check; manifest.floorContactV12 records every measurement. --test-body-stability fails if a fixed frame differs
from its base below the seam."""
import hashlib, io
import numpy as np
from PIL import Image

RULE = "planning/RULES.md F1/F2: the floor line is the body's underside; toes hang over it. Every frame of a family keeps the base's underside."
FAMILIES = {'sit': 'sit', 'loaf': 'loaf', 'lay': 'lay', 'sleep': 'sleep'}
SEAMS = {'sit': (900, 32), 'loaf': (900, 32), 'lay': (880, 32), 'sleep': (880, 32)}   # seam row, feather rows above it
BODY_WIDTH = 150        # a row with more than this many opaque pixels is body, not paws or tail tips
MIN_LIFT_ROWS = 2       # fix a frame whose underside is this many rows or more above the base's


def rgba(data):
    return np.array(Image.open(io.BytesIO(data)).convert('RGBA')).astype(np.float64)


def body_bottom(a):
    rows = [y for y in range(a.shape[0]) if (a[y, :, 3] > 8).sum() > BODY_WIDTH]
    return max(rows) if rows else None


def composite(frame, base, seam, feather):
    w = np.zeros(frame.shape[0]); w[seam:] = 1.0; w[seam - feather:seam] = np.linspace(0, 1, feather, endpoint=False)
    w = w[:, None, None]
    fp = frame.copy(); fp[:, :, :3] *= frame[:, :, 3:4] / 255
    bp = base.copy(); bp[:, :, :3] *= base[:, :, 3:4] / 255
    out = fp * (1 - w) + bp * w
    alpha = out[:, :, 3:4]
    rgb = np.where(alpha > 0, out[:, :, :3] * 255 / np.maximum(alpha, 1e-6), 0)
    result = np.clip(np.concatenate([rgb, alpha], axis=2), 0, 255).astype(np.uint8)
    result[seam:] = base[seam:].astype(np.uint8)   # below the seam: the base's bytes exactly, no blend rounding
    return result


def encode(pixels):
    buffer = io.BytesIO(); Image.fromarray(pixels, 'RGBA').save(buffer, 'PNG'); return buffer.getvalue()


def apply_pack(manifest, payloads):
    by_clip = {}
    for entry in manifest['actions']:
        by_clip.setdefault(entry['clip'], []).append(entry)
    record = {'rule': RULE, 'bodyWidthThreshold': BODY_WIDTH, 'minLiftRows': MIN_LIFT_ROWS, 'families': {}, 'ownerApproved': False,
              'ownerWords': ["he's still floating... look at the black background", "looks like look around is different than the static image"]}
    fixed_total = 0
    for family, base_clip in FAMILIES.items():
        if base_clip not in by_clip:
            continue
        base_entry = by_clip[base_clip][0]; base = rgba(payloads[base_entry['key']]); base_bottom = body_bottom(base)
        seam, feather = SEAMS[family]
        fam = {'baseClip': base_clip, 'baseKey': base_entry['key'], 'baseBodyBottom': base_bottom, 'supportY': base_entry['supportY'],
               'seamRow': seam, 'featherRows': feather, 'fixedClips': [], 'frames': []}
        for clip in ('look_' + family,):
            for entry in by_clip.get(clip, []):
                frame = rgba(payloads[entry['key']]); before = body_bottom(frame)
                item = {'clip': clip, 'frame': entry.get('frame'), 'key': entry['key'], 'bodyBottomBefore': before}
                if before is not None and base_bottom - before >= MIN_LIFT_ROWS:
                    if entry['supportY'] != base_entry['supportY'] or entry['nativeWidth'] != 1024 or entry['nativeHeight'] != 1024:
                        raise SystemExit('Floor contact: frame placed unlike its base: ' + entry['key'])
                    fixed = composite(frame, base, seam, feather)
                    differing = int((fixed[seam:] != base[seam:].astype(np.uint8)).any(axis=2).sum())   # any channel, like the app's check
                    assert differing == 0, (entry['key'], differing)
                    data = encode(fixed); payloads[entry['key']] = data
                    entry['sha256'] = hashlib.sha256(data).hexdigest(); entry['sourcePixelsUnmodified'] = False
                    entry['floorContactV12'] = {'seamRow': seam, 'featherRows': feather, 'bodyBottomBefore': before, 'bodyBottomAfter': body_bottom(fixed.astype(np.float64)),
                                                'basePixels': base_entry['source'], 'generated': False}
                    item.update({'fixed': True, 'bodyBottomAfter': entry['floorContactV12']['bodyBottomAfter'], 'differingBelowSeam': differing})
                    if clip not in fam['fixedClips']:
                        fam['fixedClips'].append(clip)
                    fixed_total += 1
                else:
                    item.update({'fixed': False, 'reason': 'underside on or below the base within %d rows' % MIN_LIFT_ROWS})
                fam['frames'].append(item)
        record['families'][family] = fam
    manifest['floorContactV12'] = record
    print('Floor contact v12-3f: fixed', fixed_total, 'gaze frames;',
          {f: (v['baseBodyBottom'], sorted(set(i['bodyBottomBefore'] for i in v['frames']))) for f, v in record['families'].items()})
    return manifest, payloads
