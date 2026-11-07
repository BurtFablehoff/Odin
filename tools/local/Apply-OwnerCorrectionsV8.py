"""Final explicit owner override after historical sizing/review generation."""
from pathlib import Path
import hashlib
import json

ROOT = Path(__file__).resolve().parents[2]


def apply_config(config):
    policy = json.loads((ROOT / 'planning/owner-corrections-v8.json').read_text())
    if not policy.get('enabled'):
        return config
    removed = {p['id'] for p in policy['removedStates']}
    config['poses'] = [p for p in config['poses'] if p['id'] not in removed]
    config['removedStates'] = policy['removedStates']
    originals = json.loads((ROOT / 'art/manifest.json').read_text())
    baseline = {p['id']: p for p in json.loads((ROOT / policy['supplementalScaleBaseline']).read_text())['poses']}
    sources = {1: ('sit', 0), 2: ('loaf', 0), 3: ('lay', 0), 4: ('sleep', 0), 16: ('jump_left', 8), 17: ('jump_right', 8)}
    for pose in config['poses']:
        pose_id = pose['id']
        if pose_id in sources:
            clip, frame = sources[pose_id]
            source = f'art/frames/{clip}/{frame:03d}.png'
            pose['source'] = source
            pose['sourceSHA256'] = hashlib.sha256((ROOT / source).read_bytes()).hexdigest()
            c = originals['clips'][clip]
            pose['supportY'] = c['images'][frame].get('anchor_y', c.get('anchor_y', originals['foot_anchor'][1]))
            rect = c.get('head_rect')
            if rect:
                pose['headCrop'] = [rect[0], rect[1], rect[0]+rect[2], rect[1]+rect[3]]
            elif pose_id in (16, 17):
                pose['headCrop'] = baseline[pose_id]['headCrop']
            elif pose_id == 4:
                pose['headCrop'] = [round(v*1024/1254) for v in [690,920,965,1120]]
        width = policy['originalSpriteWidthsAtSmall'].get(str(pose_id))
        if width is not None:
            pose['scaleAtSmall'] = width / 1024
            pose['status'] = 'Original artwork and runtime display scale; no accumulated review multipliers'
        elif pose_id in policy['supplementalPoseIds']:
            pose['scaleAtSmall'] = baseline[pose_id]['scaleAtSmall']
            pose['status'] = 'Pre-enlargement display scale restored; shape/fur correction still pending'
    config['ownerCorrections'] = 'planning/owner-corrections-v8.json'
    for override in policy.get('candidatePoseOverrides', []):
        assert not override['ownerApproved'], 'Candidate approval scope changed.'
        pose=next(p for p in config['poses'] if p['id']==override['id'])
        source=ROOT/override['source']
        assert hashlib.sha256(source.read_bytes()).hexdigest()==override['sourceSHA256']
        pose.update({key:override[key] for key in ('source','sourceSHA256','scaleAtSmall','supportY','headCrop')})
        pose['status']='Narrower original-style candidate at original seated height; owner visual review pending'
    config['headSizeReference']['rule'] = 'Pre-enlargement supplemental sizing restored; do not reapply superseded +20% multipliers.'
    v10=ROOT/'planning/owner-corrections-v10.json'
    if v10.exists():
        apply_v10=__import__('runpy').run_path(str(ROOT/'tools/local/Apply-OwnerCorrectionsV10.py'))['apply_config']
        config=apply_v10(config)
    assert [p['id'] for p in config['poses']] == [1,2,3,4,6,7,8,11,12,13,14,15,16,17]
    return config


if __name__ == '__main__':
    path = ROOT / 'planning/state-head-review.json'
    current = json.loads(path.read_text())
    retired = [p for p in current['poses'] if p['id'] in (5, 10)]
    archive = ROOT / 'planning/removed-poses-v8.json'
    if not archive.exists():
        archive.write_text(json.dumps({'ownerRemoved': True, 'sourceFilesPreserved': True, 'previousPoses': retired}, indent=2)+'\n')
    path.write_text(json.dumps(apply_config(current), indent=2)+'\n')
