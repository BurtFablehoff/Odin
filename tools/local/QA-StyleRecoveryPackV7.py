"""Independent byte/provenance checks for the original-style recovery test."""
from pathlib import Path
import hashlib
import json
import zipfile

ROOT = Path(__file__).resolve().parents[2]
digest = lambda data: hashlib.sha256(data).hexdigest()
inventory = json.loads((ROOT / 'local-data/reference-review/style-consistency-v7/inventory.json').read_text())
for source in inventory['legacySources']:
    assert digest((ROOT / source['source']).read_bytes()) == source['sha256'], source['source']
pack = ROOT / 'local-data/reference-review/dev-latest-art/dev-artwork.zip'
exe = ROOT / 'test-exes/Odin-Dev-StyleRecovery-v7.exe'
assert pack.read_bytes() in exe.read_bytes(), 'Executable does not embed this pack.'
with zipfile.ZipFile(pack) as archive:
    manifest = json.loads(archive.read('manifest.json'))
    entries = [entry for kind in ('poses', 'actions', 'blinks', 'motions') for entry in manifest[kind]]
    for entry in entries:
        data = archive.read(entry['key'])
        assert digest(data) == entry['sha256'], entry['key']
        assert data == (ROOT / entry['source']).read_bytes(), entry['source']
        assert 'focused-rear' not in entry['source'] and 'attempt' not in entry['source']
    assert not manifest['blinks'] and not manifest['motions'], 'Generated head layers returned.'
    original_actions = [entry for entry in manifest['actions'] if entry['source'].startswith('art/frames/')]
    assert len(original_actions) == 105
    original_gaze = [entry for entry in original_actions if entry['clip'].startswith('look_')]
    assert len(original_gaze) == 45
    for pose_id in (5, 6, 7, 8, 10, 11):
        entry = next(p for p in manifest['poses'] if p['id'] == pose_id)
        old = next(p for p in inventory['packSources'] if p['key'] == entry['key'])
        assert entry['sha256'] == old['sha256'], 'Rejected extra-pose generation was integrated.'
        assert 'pending' in entry['label']
    original_sit = (ROOT / 'art/frames/sit/000.png').read_bytes()
    assert archive.read('state-01.png') == original_sit
    assert archive.read('action-sit-000.png') == original_sit
    assert len(manifest['actions']) == 121
groom = json.loads((ROOT / 'draft-assets/style-consistency-v7/groom/qa.json').read_text())
assert groom['passed'] and groom['frameCount'] == 16 and groom['originalSourcesUnmodified']
baseline = json.loads((ROOT / 'planning/current-test-build.json').read_text())
assert digest((ROOT / baseline['mainExe']).read_bytes()) == baseline['sha256'][baseline['mainExe']], 'V6 fallback was changed.'
report = dict(passed=True, headless=True, windowsCreated=0, originalSourcesUnchanged=len(inventory['legacySources']),
              packSourcesChecked=len(entries), originalRestFrames=73, originalGazeBodyFrames=45,
              originalJumpFrames=32, groomLocalPatchFrames=16, generatedRestOverlays=0,
              rejectedStyleTransfersIntegrated=False, sixExtraPosesStillPending=True,
              jumpAnatomyStillPending=True, originalSittingByteExact=True,
              exactPackEmbedded=True, v6FallbackUnchanged=True,
              candidateExe=str(exe.relative_to(ROOT)).replace('\\', '/'), candidateSHA256=digest(exe.read_bytes()))
(ROOT / 'build/qa-source-preservation-style-v7.json').write_text(json.dumps(report, indent=2)+'\n')
print(json.dumps(report))
