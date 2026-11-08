"""Explicit DEV-only recovery of original coherent artwork; never edit sources."""
from pathlib import Path
import hashlib
import json
import runpy

ROOT = Path(__file__).resolve().parents[2]


def apply_recovery(manifest, payloads):
    settings_path = ROOT / 'planning/style-recovery-v7.json'
    if not settings_path.exists():
        return manifest, payloads
    settings = json.loads(settings_path.read_text())
    if not settings.get('enabled'):
        return manifest, payloads
    assert settings['developmentOnly'] and not settings['completeStyleCorrection']
    rest = runpy.run_path(str(ROOT / 'tools/local/Build-RestStyleV7.py'))
    manifest, payloads = rest['apply_rest'](manifest, payloads)
    manifest['restoredOriginalRestFamilies'] = True
    manifest['restoredRestFrameCount'] = 73

    jumps = runpy.run_path(str(ROOT / 'tools/local/Build-JumpStyleV7.py'))
    entries, clips = jumps['build_actions']()
    replaced = {'jump_left', 'jump_right'}
    manifest['actions'] = [p for p in manifest['actions'] if p['clip'] not in replaced]
    manifest['actions'] += [entry for entry, data in entries]
    manifest['actionClips'] = [p for p in manifest['actionClips'] if p['clip'] not in replaced] + clips
    for entry, data in entries:
        payloads[entry['key']] = data
    # The menu still uses the same restored source and registration as frame008.
    for clip, pose_id in [('jump_left', 16), ('jump_right', 17)]:
        original = next(p for p, data in entries if p['clip'] == clip and p['frame'] == 8)
        pose = next(p for p in manifest['poses'] if p['id'] == pose_id)
        with_source = {key: value for key, value in original.items() if key not in ('key', 'label', 'clip', 'frame')}
        pose.update(with_source)
        pose.pop('motionBounds', None)
        baseline = json.loads((ROOT / 'planning/numbered-sizing-baseline-v1.json').read_text())
        pose['headCrop'] = next(p['headCrop'] for p in baseline['poses'] if p['id'] == pose_id)
        pose['label'] = f'{pose_id}. Original jumping {clip.split("_")[1]}'
        payloads[pose['key']] = payloads[original['key']]

    if settings.get('includeGroomPawCandidate'):
        candidate = json.loads((ROOT / 'draft-assets/style-consistency-v7/groom/integration-candidate.json').read_text())
        assert candidate['candidateOnly'] and not candidate['ownerApproved']
        assert len(candidate['frames']) == 16
        for entry in candidate['frames']:
            data = (ROOT / entry['source']).read_bytes()
            assert hashlib.sha256(data).hexdigest() == entry['sha256'], entry['source']
            manifest['actions'].append(entry)
            payloads[entry['key']] = data
        manifest['actionClips'].append(candidate['actionClip'])
        source = candidate['frames'][8]
        pose = next(p for p in manifest['poses'] if p['id'] == 12)
        pose.update({key: value for key, value in source.items() if key not in ('key', 'label', 'clip', 'frame')})
        pose['label'] = '12. Grooming (local paw correction candidate)'
        payloads[pose['key']] = payloads[source['key']]

    # Five extra poses have no faithful original animation-family counterpart.
    # Keep their existing checkpoint, explicitly pending, rather than promoting
    # any of the failed style-transfer drafts displayed during this task.
    for pose in manifest['poses']:
        if pose['id'] in settings['extraPoseIdsStillPending']:
            pose['label'] += ' (style correction pending)'
    manifest['styleRecoveryV7'] = settings
    v8_path=ROOT/'planning/owner-corrections-v8.json'
    if v8_path.exists():
        v8=json.loads(v8_path.read_text())
        if v8.get('enabled'):
            manifest['ownerCorrectionsV8']=v8
            manifest['legacyActionWidths']={'groom':480,'stretch':480,'run_left':720,'run_right':902.4,'jump_left':480,'jump_right':480}
            for pose in manifest['poses']:
                width=v8['originalSpriteWidthsAtSmall'].get(str(pose['id']))
                if width is not None:pose['renderWidthAtSmall']=width
    manifest['completeStyleCorrection'] = False
    v10=ROOT/'planning/owner-corrections-v10.json'
    if v10.exists():
        final=runpy.run_path(str(ROOT/'tools/local/Apply-OwnerCorrectionsV10.py'))
        manifest,payloads=final['apply_pack'](manifest,payloads)
    v11=ROOT/'planning/owner-corrections-v11.json'
    if v11.exists():
        manifest,payloads=runpy.run_path(str(ROOT/'tools/local/Apply-OwnerCorrectionsV11.py'))['apply_pack'](manifest,payloads)
        manifest,payloads=runpy.run_path(str(ROOT/'tools/local/Apply-OwnerSizesV12.py'))['apply_pack'](manifest,payloads)
        manifest,payloads=runpy.run_path(str(ROOT/'tools/local/Slim-JumpRuffV11_2.py'))['apply_pack'](manifest,payloads)
        manifest,payloads=runpy.run_path(str(ROOT/'tools/local/Apply-RunStyleJumpV11_3.py'))['apply_pack'](manifest,payloads)
    used = {entry['key'] for kind in ('poses', 'actions', 'blinks', 'motions') for entry in manifest.get(kind, [])}
    payloads = {key: data for key, data in payloads.items() if key in used}
    assert len(manifest['actions']) == 105 + (16 if settings.get('includeGroomPawCandidate') else 0)
    assert len(manifest['poses']) == 14
    assert not any(p['id'] in (5, 9, 10) for p in manifest['poses']), 'Retired pose returned.'
    return manifest, payloads
