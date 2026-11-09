"""V11.x DEV size normalization: uniform whole-sprite width factors so every state's head
matches sit/000 (planning/size-normalization-v11-1.json), plus owner support overrides.
Changes renderWidthAtSmall, legacyActionWidths and listed supportY values only; no source pixels."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
POLICY = ROOT / 'planning/size-normalization-v11-1.json'


def apply_pack(manifest, payloads):
    if not POLICY.exists():
        return manifest, payloads
    policy = json.loads(POLICY.read_text())
    poses = {p['id']: p for p in manifest['poses']}
    report = []
    for group in policy['groups']:
        factor = float(group['factor'])
        clips = set(group.get('clips', []))
        touched = 0
        for entry in manifest['actions']:
            if entry['clip'] in clips:
                entry['renderWidthAtSmall'] = round(entry['renderWidthAtSmall'] * factor, 4)
                touched += 1
        assert not clips or touched, f"{group['name']}: no action entries for {sorted(clips)}"
        for pid in group.get('poseIds', []):
            pose = poses[pid]
            before = pose['renderWidthAtSmall']
            pose['renderWidthAtSmall'] = round(before * factor, 4)
            pose['sizeNormalizationV11_1'] = {'factor': factor, 'before': before}
        for clip in group.get('legacyClips', []):
            manifest['legacyActionWidths'][clip] = round(manifest['legacyActionWidths'][clip] * factor, 4)
        report.append({'group': group['name'], 'factor': factor, 'actionEntries': touched})
    # Owner support overrides (rear #6: butt on the line, tail may overhang).
    for override in policy.get('supportOverrides', []):
        pose = poses[override['id']]
        assert pose['supportY'] == override['previousSupportY'], (override['id'], pose['supportY'])
        pose['supportY'] = override['supportY']
        pose['tailOverhangOwnerApproved'] = True
    manifest['sizeNormalizationV11_1'] = {'policy': str(POLICY.relative_to(ROOT)).replace('\\', '/'),
                                          'version': policy.get('version'), 'groups': report,
                                          'supportOverrides': policy.get('supportOverrides', []),
                                          'ownerApproved': False}
    return manifest, payloads
