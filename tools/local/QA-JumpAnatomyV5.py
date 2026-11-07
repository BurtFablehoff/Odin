"""Independent native-source and placement audit for provisional jump anatomy.

Reads the built pack without starting Odin or creating any native UI. The saved
v4 manifest is an immutable baseline; this check never imports the pack builder.
Visual anatomy approval remains the owner's provisional approval, not a metric.
"""
from pathlib import Path
import argparse
import hashlib
import io
import json
import math
import zipfile
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
BASELINE = 'local-data/reference-review/jump-anatomy-v5/base-v4-manifest.json'


def read(path):
    return json.loads((ROOT/path).read_text())


def sha(data):
    return hashlib.sha256(data).hexdigest()


def require(condition, message):
    if not condition:
        raise AssertionError(message)


def close(a, b, message, tolerance=1e-7):
    require(math.isclose(a, b, rel_tol=1e-9, abs_tol=tolerance), message)


def midpoint(points):
    require(len(points) == 2 and all(len(p) == 2 for p in points), 'Invalid paired-pupil landmarks')
    return [(points[0][axis]+points[1][axis])/2 for axis in (0, 1)]


def source_info(data):
    image = Image.open(io.BytesIO(data)).convert('RGBA')
    bounds = image.getchannel('A').point(lambda a: 255 if a > 8 else 0).getbbox()
    require(bounds is not None, 'Empty native sprite')
    require(bounds[0] > 0 and bounds[1] > 0 and bounds[2] < image.width and bounds[3] < image.height,
            'A visible sprite edge touches the source canvas')
    return image.size, bounds


def run(exe_path, skip_exe=False):
    baseline = read(BASELINE)
    base = {(p['clip'], p['frame']): p for p in baseline['actions']}
    require(len(base) == 32, 'Saved v4 baseline is incomplete')
    configuration = read('planning/jump-anatomy-integration-v5.json')
    require(configuration.get('enabled') and configuration.get('approved') and configuration.get('provisional'),
            'Provisional anatomy approval must be explicit')
    require(configuration.get('ownerApproval') == 'approve for now', 'Owner approval wording changed')
    overrides = {(p['clip'], p['frame']): p for p in configuration['frames']}
    expected_keys = {(clip, f) for clip in ('jump_left', 'jump_right') for f in range(4, 9)}
    require(set(overrides) == expected_keys and len(configuration['frames']) == 10,
            'Only owner-reviewed source frames004–008 in each direction may be overridden')
    left_landmarks = {p['frame']: p for p in read('draft-assets/jump-anatomy-v5/left-candidates.json')['frames']}
    right_landmarks = {p['frame']: p for p in read('draft-assets/jump-anatomy-v5/jump_right/qa.json')['frames']}
    original = read('art/manifest.json')
    pack_bytes = (ROOT/'local-data/reference-review/dev-latest-art/dev-artwork.zip').read_bytes()
    results = []
    placement_cases = 0
    with zipfile.ZipFile(io.BytesIO(pack_bytes)) as pack:
        manifest = json.loads(pack.read('manifest.json'))
        actions = {(p['clip'], p['frame']): p for p in manifest['actions']}
        require(len(manifest['actions']) == len(actions) == 32 and set(actions) == set(base),
                'Jump family coverage changed or duplicate frames appeared')
        require(manifest['actionClips'] == baseline['actionClips'], 'Jump timings or loop policy changed')
        for clip in ('jump_left', 'jump_right'):
            metadata = next(p for p in manifest['actionClips'] if p['clip'] == clip)
            require(metadata['frames'] == original['clips'][clip]['frames'] == 16, 'Jump count changed')
            close(metadata['seconds'], original['clips'][clip]['seconds'], 'Original jump duration changed')
            close(metadata['seconds'], 1.5, 'Expected1.5second jump duration')
        # The action correction must not silently replace the separately accepted
        # still samples or disturb any other resting state or registered layer.
        for section in ('poses', 'blinks', 'motions', 'legacyActionWidths'):
            require(manifest[section] == baseline[section], 'Unrelated static/layer metadata changed: '+section)
        for pose in baseline['poses']:
            require(sha(pack.read(pose['key'])) == pose['sha256'], 'Static pose PNG changed: '+pose['key'])
        require({p['id'] for p in baseline['poses']} >= {16, 17}, 'Missing separate jump still samples')
        for key in sorted(base):
            old, entry = base[key], actions[key]
            clip, number = key
            label = f'{clip}/{number:03}'
            # Both generations and the original atlas files remain available.
            require(sha((ROOT/old['source']).read_bytes()) == old['sha256'], 'V4 source changed: '+label)
            legacy_data = (ROOT/f'art/frames/{clip}/{number:03}.png').read_bytes()
            require(sha(legacy_data) == old['sourceLegacySHA256'] == entry['sourceLegacySHA256'],
                    'Original atlas source changed: '+label)
            data = pack.read(entry['key'])
            require(data == (ROOT/entry['source']).read_bytes(), 'Packed source pixels/encoding changed: '+label)
            require(sha(data) == entry['sha256'], 'Packed native source hash mismatch: '+label)
            size, visible = source_info(data)
            require(list(size) == [entry['nativeWidth'], entry['nativeHeight']], 'Wrong native dimensions: '+label)
            require(entry['bounds'] == [visible[0], visible[1], visible[2]-visible[0], visible[3]-visible[1]],
                    'Wrong packed alpha bounds: '+label)
            require(entry['grounded'] == old['grounded'] and entry['airborne'] == old['airborne'],
                    'Original action phase changed: '+label)
            if key not in overrides:
                require(entry == old, 'Unreviewed frame metadata changed: '+label)
                results.append({'clip': clip, 'frame': number, 'unchangedV4EntryExact': True,
                                'sha256': entry['sha256']})
                continue
            override = overrides[key]
            require(override['baseline'] == old, 'Override geometry baseline differs from preserved v4: '+label)
            require(override.get('approved') and override.get('provisional'), 'Missing per-frame approval: '+label)
            require(override['reviewNumber'] == number+1, 'Owner review numbering changed: '+label)
            candidate_path = f'draft-assets/jump-anatomy-v5/{clip}/{number:03}.png'
            require(entry['source'] == override['source'] == candidate_path, 'Wrong anatomy candidate: '+label)
            require(entry['sha256'] == override['sha256'], 'Approved candidate hash changed: '+label)
            require(entry.get('anatomyOverride') and entry.get('anatomyProvisional'), 'Missing runtime provenance: '+label)
            require(entry.get('sourceV4') == old['source'] and entry.get('sourceV4SHA256') == old['sha256'],
                    'Missing preserved v4 provenance: '+label)
            if clip == 'jump_left':
                landmarks = left_landmarks[number]['headLandmarks']
                old_pupils = landmarks['oldPupilCentersApprox']
                new_pupils = landmarks['newPupilCentersApprox']
                correction = landmarks['headUniformScaleCorrectionSuggested']
            else:
                landmarks = right_landmarks[number]
                old_pupils, new_pupils = landmarks['oldPupilCenters'], landmarks['newPupilCenters']
                # Derive from recorded eye geometry instead of trusting the builder's ratio.
                correction = abs(old_pupils[1][0]-old_pupils[0][0])/abs(new_pupils[1][0]-new_pupils[0][0])
            require(override['oldPupilCenters'] == old_pupils and override['newPupilCenters'] == new_pupils,
                    'Override landmarks differ from reviewed geometry: '+label)
            close(override['scaleCorrection'], correction, 'Wrong uniform anatomy scale correction: '+label)
            close(entry['anatomyScaleCorrection'], correction, 'Packed anatomy scale differs: '+label)
            old_center, new_center = midpoint(old_pupils), midpoint(new_pupils)
            old_scale = old['renderWidthAtSmall']/old['nativeWidth']
            new_scale = entry['renderWidthAtSmall']/entry['nativeWidth']
            close(new_scale, old_scale*correction, 'Anatomy whole-sprite scale changed: '+label)
            close(entry['anchorX'], new_center[0]+(old['anchorX']-old_center[0])/correction,
                  'Head/root horizontal registration changed: '+label)
            close(entry['supportY'], new_center[1]+(old['supportY']-old_center[1])/correction,
                  'Head/support vertical registration changed: '+label)
            require(0 < entry['supportY'] < size[1], 'Support falls outside native source: '+label)
            for display_size in (480, 640, 800):
                factor = display_size/480
                # Multiple anchors ensure geometry does not accidentally rely on root0.
                for root_x, floor_y in ((0, 0), (480, 900), (950, 550)):
                    old_x = root_x+(old_center[0]-old['anchorX'])*old_scale*factor
                    new_x = root_x+(new_center[0]-entry['anchorX'])*new_scale*factor
                    old_y = floor_y+(old_center[1]-old['supportY'])*old_scale*factor
                    new_y = floor_y+(new_center[1]-entry['supportY'])*new_scale*factor
                    close(old_x, new_x, 'World head X jumped after registration: '+label)
                    close(old_y, new_y, 'World head Y jumped after registration: '+label)
                    placement_cases += 1
            results.append({'clip': clip, 'frame': number, 'reviewNumber': number+1,
                            'sha256': entry['sha256'], 'uniformScaleCorrection': correction,
                            'sourceV4Preserved': True, 'headWorldPositionPreserved': True,
                            'transparentMarginsClear': True, 'provisionallyApproved': True})
    executable = {}
    if not skip_exe:
        exe_data = (ROOT/exe_path).read_bytes()
        require(pack_bytes in exe_data, 'EXE embeds a stale or different artwork pack')
        executable = {'path': exe_path, 'sha256': sha(exe_data), 'currentArtworkPackEmbedded': True}
    return {'passed': True, 'headless': True, 'windowsCreated': 0,
            'approvedAnatomyFrames': 10, 'unchangedActionEntries': 22,
            'allV4SourcesPreserved': True, 'allOriginalAtlasSourcesPreserved': True,
            'staticStill16And17Preserved': True, 'allRestingLayersUnchanged': True,
            'nativeSourceHashesAndMarginsVerified': True, 'frameCountsAndTimingPreserved': True,
            'headLandmarkPlacementCases': placement_cases, 'wholeSpriteScalingOnly': True,
            'visualApproval': 'provisional owner approval: approve for now',
            'artifact': executable, 'frames': results}


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--dev-exe', default='test-exes/Odin-Dev-Review-v5.exe')
    parser.add_argument('--skip-exe', action='store_true')
    args = parser.parse_args()
    report_path = ROOT/'build/qa-jump-anatomy-v5.json'
    report_path.parent.mkdir(parents=True, exist_ok=True)
    try:
        report = run(args.dev_exe, args.skip_exe)
        report_path.write_text(json.dumps(report, indent=2)+'\n')
        print(json.dumps({k: v for k, v in report.items() if k != 'frames'}))
    except Exception as error:
        report_path.write_text(json.dumps({'passed': False, 'headless': True, 'error': str(error)}, indent=2)+'\n')
        print(str(error))
        raise SystemExit(1)
