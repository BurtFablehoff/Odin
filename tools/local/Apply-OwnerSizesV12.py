"""V12 DEV owner sizes (planning/sizes-owner-v12.json), replacing Apply-SizeNormalizationV11_1.

States carry absolute renderWidthAtSmall values (and their family action clips follow them).
Rear #6 and side #8 are matched to sitting #1 by seated height (owner exception for 6/8 only).
Animations carry absolute widths set by continuity with sitting (v12 2a, planning/animation-continuity-v12.json):
each clip's first and last frames match sit/000; clips that failed the 5% check keep their v11.3 width.
Changes renderWidthAtSmall, legacyActionWidths and listed supportY values only; no source pixels.

Run directly to print the seated-height table from the built DEV pack."""
import io
import json
import sys
import zipfile
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
POLICY = ROOT / 'planning/sizes-owner-v12.json'
PACK = ROOT / 'local-data/reference-review/dev-latest-art/dev-artwork.zip'
SEATED = (1, 6, 8)
TOLERANCE_SCREEN_PX = 1.0


def ear_tip_row(data, threshold=64):
    with Image.open(io.BytesIO(data)) as image:
        alpha = np.asarray(image.convert('RGBA'))[..., 3]
        size = image.size
    rows = np.nonzero((alpha >= threshold).any(axis=1))[0]
    return int(rows.min()), size


def seated_heights(manifest, payloads):
    """Ear tip (top alpha >= 64 row) to supportY, native and on screen at the small size."""
    poses = {p['id']: p for p in manifest['poses']}
    table = {}
    for pid in SEATED:
        pose = poses[pid]
        top, size = ear_tip_row(payloads[pose['key']])
        assert size[0] == pose['nativeWidth'], (pid, size, pose['nativeWidth'])
        native = float(pose['supportY']) - top
        scale = float(pose['renderWidthAtSmall']) / pose['nativeWidth']
        table[pid] = {'earTipRow': top, 'supportY': pose['supportY'], 'nativePx': native,
                      'renderWidthAtSmall': pose['renderWidthAtSmall'], 'screenPx': round(native * scale, 3)}
    return table


def print_table(table, title):
    print(title)
    print(f"{'id':>3} {'earTip':>6} {'supportY':>8} {'native':>7} {'width':>10} {'screen':>8} {'vs sit':>7}")
    sit = table[1]['screenPx']
    for pid, row in table.items():
        print(f"{pid:>3} {row['earTipRow']:>6} {row['supportY']:>8} {row['nativePx']:>7.1f} "
              f"{float(row['renderWidthAtSmall']):>10.4f} {row['screenPx']:>8.3f} {row['screenPx'] - sit:>+7.3f}")


def set_width(target, key, value):
    # Keep the existing JSON value (and its int/float spelling) when it already matches.
    if float(target[key]) != float(value):
        target[key] = value


def apply_pack(manifest, payloads):
    policy = json.loads(POLICY.read_text())
    poses = {p['id']: p for p in manifest['poses']}
    covered = {}

    for row in policy['states']:
        pose = poses[row['id']]
        before = pose['renderWidthAtSmall']
        width = row['renderWidthAtSmall']
        clips = set(row.get('clips', []))
        touched = 0
        for entry in manifest['actions']:
            if entry['clip'] in clips:
                assert float(entry['renderWidthAtSmall']) == float(before), (row['id'], entry['key'])
                set_width(entry, 'renderWidthAtSmall', width)
                touched += 1
        assert not clips or touched, f"state {row['id']}: no action entries for {sorted(clips)}"
        set_width(pose, 'renderWidthAtSmall', width)
        pose['ownerSizesV12'] = {'section': 'states', 'before': before, 'after': pose['renderWidthAtSmall']}
        covered[row['id']] = 'states'

    for override in policy['supportOverrides']:
        pose = poses[override['id']]
        assert pose['supportY'] == override['previousSupportY'], (override['id'], pose['supportY'])
        pose['supportY'] = override['supportY']
        pose['tailOverhangOwnerApproved'] = True

    # Animations: absolute widths from continuity with sitting (v12 2a). Every frame of a clip shares
    # one width, so its first/last frames match sit. Run-style jump frames 002-012 are redrawn later by
    # Apply-RunStyleJumpV11_3 from the run legacy width set here, placed from this jump width.
    animations = policy['animations']
    legacy_seen = set()
    for group in animations['groups']:
        width = group['renderWidthAtSmall']
        clips = set(group.get('clips', []))
        touched = 0
        for entry in manifest['actions']:
            if entry['clip'] in clips:
                set_width(entry, 'renderWidthAtSmall', width)
                touched += 1
        assert not clips or touched, f"{group['name']}: no action entries for {sorted(clips)}"
        for pid in group.get('poseIds', []):
            pose = poses[pid]
            before = pose['renderWidthAtSmall']
            set_width(pose, 'renderWidthAtSmall', width)
            # Run-style jump frames (Apply-RunStyleJumpV11_3, later) replace stills 16/17 with frame 008.
            pose['ownerSizesV12'] = {'section': 'animations', 'basis': group['basis'], 'before': before,
                                     'afterThisStep': pose['renderWidthAtSmall'],
                                     'continuityPass': group['continuityPass']}
            covered[pid] = 'animations'
        for clip in group.get('legacyClips', []):
            assert clip in manifest['legacyActionWidths'], clip
            manifest['legacyActionWidths'][clip] = width
            legacy_seen.add(clip)
    missing_legacy = sorted(set(manifest['legacyActionWidths']) - legacy_seen)
    assert not missing_legacy, f'legacyActionWidths not covered by animations: {missing_legacy}'

    for row in policy['parked']:
        pose = poses.get(row['id'])
        if pose is None:  # a later step removes parked poses from the pack
            continue
        before = pose['renderWidthAtSmall']
        set_width(pose, 'renderWidthAtSmall', row['renderWidthAtSmall'])
        pose['ownerSizesV12'] = {'section': 'parked', 'before': before, 'after': pose['renderWidthAtSmall']}
        covered[row['id']] = 'parked'

    missing = sorted(set(poses) - set(covered))
    assert not missing, f'poses not covered by planning/sizes-owner-v12.json: {missing}'

    table = seated_heights(manifest, payloads)
    print_table(table, 'Owner sizes v12: seated heights at the small size (ear tip to supportY)')
    for pid in (6, 8):
        diff = abs(table[pid]['screenPx'] - table[1]['screenPx'])
        assert diff <= TOLERANCE_SCREEN_PX, f'pose {pid} seated height differs from sit by {diff:.3f} screen px'

    manifest['ownerSizesV12'] = {'policy': str(POLICY.relative_to(ROOT)).replace('\\', '/'),
                                 'version': policy['version'], 'covered': {str(k): v for k, v in sorted(covered.items())},
                                 'seatedHeightsScreenPx': {str(k): v['screenPx'] for k, v in table.items()},
                                 'supportOverrides': policy['supportOverrides'],
                                 'animationsInterim': False, 'animationContinuity': 'planning/animation-continuity-v12.json',
                                 'animationContinuityFailed': sorted(g['name'] for g in animations['groups'] if not g['continuityPass']),
                                 'ownerApproved': False}
    return manifest, payloads


def main():
    pack = zipfile.ZipFile(sys.argv[1] if len(sys.argv) > 1 else PACK)
    manifest = json.loads(pack.read('manifest.json'))
    keys = {p['key'] for p in manifest['poses'] if p['id'] in SEATED}
    table = seated_heights(manifest, {key: pack.read(key) for key in keys})
    print_table(table, f'Seated heights from {pack.filename}')
    sit = table[1]['screenPx']
    native = {p['id']: p['nativeWidth'] for p in manifest['poses']}
    for pid in (6, 8):
        exact = sit / table[pid]['nativePx'] * native[pid]
        print(f'  {pid}: renderWidthAtSmall giving exactly sit height = {exact:.4f} '
              f'(x{exact / float(table[pid]["renderWidthAtSmall"]):.4f} of current)')
    worst = max(abs(table[pid]['screenPx'] - sit) for pid in (6, 8))
    print(f'max |seated - sit| = {worst:.3f} screen px ({"OK" if worst <= TOLERANCE_SCREEN_PX else "FAIL"})')


if __name__ == '__main__':
    main()
