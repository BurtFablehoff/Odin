"""V12 5g: every gaze (look_*) frame of a rest family reaches the family base's underside (floor rules F1/F2/F7, planning/RULES.md).

Hatt, 2026-11-08: "looks like look around is different than the static image". The original look_sit frames draw the belly's
last row 3-5 native rows above the static sit's (body underside 938-940 vs 943 with the same support row 944), so while Odin looks
around his belly lifts off the line. v12-3f blended the still's lower body in below row 900; the two drawings' legs do not line up
and Hatt saw the line ("you broke the sit image... look at the line across the legs"). v12-5g instead stretches the frame's OWN
pixels down, column by column, below a top row: each column's shift is the base's alpha bottom minus the frame's (never negative,
capped at MAX_SHIFT), smoothed across columns with a Gaussian, and the column's rows [top, bottom] are resampled linearly so the
bottom lands shift rows lower (rows below the old bottom move down by the shift). Rows above the top row are the original bytes;
nothing is generated, nothing is pasted from another drawing, support rows are unchanged. Toes may hang a few rows lower, which F2
allows. Families whose look frames already end on or below the base's underside (loaf, lay) are measured and left alone. Entry sha256
values are updated for the loader's hash check; manifest.floorContactV12 records every measurement. --test-body-stability checks
each warped frame's body underside against the base."""
import hashlib, io
import numpy as np
from PIL import Image

RULE = "planning/RULES.md F1/F2: the floor line is the body's underside; toes hang over it. Every frame of a family keeps the base's underside."
FAMILIES = {'sit': 'sit', 'loaf': 'loaf', 'lay': 'lay', 'sleep': 'sleep'}
WARP_TOP = {'sit': 820, 'loaf': 820, 'lay': 800, 'sleep': 800}   # rows above this are the original frame, byte for byte
MAX_SHIFT = 8           # native rows a column may move down
SMOOTH_SIGMA = 25       # columns; smooths the per-column shift so stripes and edges bend, never step
ALPHA_EDGE = 128        # a column's bottom is its last row with alpha above this
BODY_WIDTH = 150        # a row with more than this many opaque pixels is body, not paws or tail tips
MIN_LIFT_ROWS = 2       # fix a frame whose underside is this many rows or more above the base's


def rgba(data):
    return np.array(Image.open(io.BytesIO(data)).convert('RGBA')).astype(np.float64)


def body_bottom(a):
    rows = [y for y in range(a.shape[0]) if (a[y, :, 3] > 8).sum() > BODY_WIDTH]
    return max(rows) if rows else None


def column_bottoms(a):
    m = a[:, :, 3] > ALPHA_EDGE
    return np.where(m.any(0), a.shape[0] - 1 - np.argmax(m[::-1], 0), -1)


def smooth(v, sigma):
    r = int(3 * sigma); k = np.exp(-0.5 * (np.arange(-r, r + 1) / sigma) ** 2); k /= k.sum()
    return np.convolve(np.pad(v, r, mode='edge'), k, 'valid')


def warp(frame, base, top):
    """Stretch each column of `frame` below `top` so its alpha bottom moves toward the base's. Premultiplied linear resampling in y only."""
    fb, bb = column_bottoms(frame), column_bottoms(base)
    valid = (fb > top + 40) & (bb > top + 40)
    shift = smooth(np.where(valid, np.clip(bb - fb, 0, MAX_SHIFT), 0).astype(np.float64), SMOOTH_SIGMA)
    h, w = frame.shape[:2]
    yy = np.arange(h, dtype=np.float64)[:, None]
    old_bottom = np.maximum(fb, top + 1).astype(np.float64)[None, :]
    new_bottom = old_bottom + shift[None, :]
    src = np.where(yy > top, top + (yy - top) * (old_bottom - top) / np.maximum(new_bottom - top, 1e-6), yy)
    src = np.where(yy > new_bottom, yy - shift[None, :], src)
    pre = frame.copy(); pre[:, :, :3] *= frame[:, :, 3:4] / 255
    y0 = np.floor(src).astype(int); t = (src - y0)[:, :, None]; xs = np.broadcast_to(np.arange(w), (h, w))

    def at(y):
        ok = ((y >= 0) & (y < h))[:, :, None]
        return np.where(ok, pre[np.clip(y, 0, h - 1), xs], 0)
    out = at(y0) * (1 - t) + at(y0 + 1) * t
    alpha = out[:, :, 3:4]
    rgb = np.where(alpha > 0, out[:, :, :3] * 255 / np.maximum(alpha, 1e-6), 0)
    result = np.clip(np.round(np.concatenate([rgb, alpha], axis=2)), 0, 255).astype(np.uint8)
    result[:top + 1] = frame[:top + 1].astype(np.uint8)   # above the warp: the original bytes exactly
    return result, shift


def encode(pixels):
    buffer = io.BytesIO(); Image.fromarray(pixels, 'RGBA').save(buffer, 'PNG'); return buffer.getvalue()


def apply_pack(manifest, payloads):
    by_clip = {}
    for entry in manifest['actions']:
        by_clip.setdefault(entry['clip'], []).append(entry)
    record = {'rule': RULE, 'bodyWidthThreshold': BODY_WIDTH, 'minLiftRows': MIN_LIFT_ROWS, 'families': {}, 'ownerApproved': False,
              'ownerWords': ["he's still floating... look at the black background", "looks like look around is different than the static image",
                            "you broke the sit image... look at the line across the legs", "any except 2. thats the only one that is broken. look at the vertical leg lines"]}
    fixed_total = 0
    for family, base_clip in FAMILIES.items():
        if base_clip not in by_clip:
            continue
        base_entry = by_clip[base_clip][0]; base = rgba(payloads[base_entry['key']]); base_bottom = body_bottom(base)
        top = WARP_TOP[family]
        fam = {'baseClip': base_clip, 'baseKey': base_entry['key'], 'baseBodyBottom': base_bottom, 'supportY': base_entry['supportY'],
               'method': 'warp', 'warpTopRow': top, 'maxShiftRows': MAX_SHIFT, 'smoothSigmaColumns': SMOOTH_SIGMA, 'fixedClips': [], 'frames': []}
        for clip in ('look_' + family,):
            for entry in by_clip.get(clip, []):
                frame = rgba(payloads[entry['key']]); before = body_bottom(frame)
                item = {'clip': clip, 'frame': entry.get('frame'), 'key': entry['key'], 'bodyBottomBefore': before}
                if before is not None and base_bottom - before >= MIN_LIFT_ROWS:
                    if entry['supportY'] != base_entry['supportY'] or entry['nativeWidth'] != 1024 or entry['nativeHeight'] != 1024:
                        raise SystemExit('Floor contact: frame placed unlike its base: ' + entry['key'])
                    fixed, shift = warp(frame, base, top)
                    assert (fixed[:top + 1] == frame[:top + 1].astype(np.uint8)).all(), entry['key']
                    after = body_bottom(fixed.astype(np.float64))
                    assert abs(after - base_bottom) <= 1, (entry['key'], after, base_bottom)
                    data = encode(fixed); payloads[entry['key']] = data
                    entry['sha256'] = hashlib.sha256(data).hexdigest(); entry['sourcePixelsUnmodified'] = False
                    entry['floorContactV12'] = {'method': 'warp', 'warpTopRow': top, 'bodyBottomBefore': before, 'bodyBottomAfter': after,
                                                'maxShiftRows': round(float(shift.max()), 2), 'ownPixelsOnly': True, 'generated': False}
                    item.update({'fixed': True, 'bodyBottomAfter': after, 'maxShiftRows': round(float(shift.max()), 2)})
                    if clip not in fam['fixedClips']:
                        fam['fixedClips'].append(clip)
                    fixed_total += 1
                else:
                    item.update({'fixed': False, 'reason': 'underside on or below the base within %d rows' % MIN_LIFT_ROWS})
                fam['frames'].append(item)
        record['families'][family] = fam
    manifest['floorContactV12'] = record
    print('Floor contact v12-5g (warp): fixed', fixed_total, 'gaze frames;',
          {f: (v['baseBodyBottom'], sorted(set(i['bodyBottomBefore'] for i in v['frames']))) for f, v in record['families'].items()})
    return manifest, payloads
