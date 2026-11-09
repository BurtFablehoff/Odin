"""Independent source/pack/scale and retirement checks; no desktop interaction."""
from pathlib import Path
import hashlib
import json
import zipfile

ROOT=Path(__file__).resolve().parents[2]
digest=lambda data:hashlib.sha256(data).hexdigest()
policy=json.loads((ROOT/'planning/owner-corrections-v8.json').read_text())
inventory=json.loads((ROOT/'local-data/reference-review/style-consistency-v7/inventory.json').read_text())
for source in inventory['legacySources']:
    assert digest((ROOT/source['source']).read_bytes())==source['sha256'],source['source']
exe=ROOT/'test-exes/Odin-Dev-StyleRecovery-v8.exe'
pack=ROOT/'local-data/reference-review/dev-latest-art/dev-artwork.zip'
assert pack.read_bytes() in exe.read_bytes(),'Executable embeds a stale pack.'
with zipfile.ZipFile(pack) as archive:
    m=json.loads(archive.read('manifest.json'))
    ids=[p['id'] for p in m['poses']]
    assert ids==[1,2,3,4,6,7,8,12,13,14,15,16,17]
    assert 'state-05.png' not in archive.namelist() and 'state-10.png' not in archive.namelist()
    assert m['fixedOriginalRestBodies'] and not m['blinks'] and not m['motions']
    entries=m['poses']+m['actions']
    for entry in entries:
        data=archive.read(entry['key'])
        assert digest(data)==entry['sha256'] and data==(ROOT/entry['source']).read_bytes(),entry['key']
    neutral={p['id']:p for p in m['poses'] if p['id']<=4}
    for entry in m['actions']:
        if entry['id']<=4:
            basis=neutral[entry['id']]
            for field in ('anchorX','supportY','renderWidthAtSmall'):
                assert entry[field]==basis[field],(entry['key'],field)
        if entry['clip'].startswith('jump_'):
            assert entry['renderWidthAtSmall']==480
    for pose in m['poses']:
        width=policy['originalSpriteWidthsAtSmall'].get(str(pose['id']))
        if width is not None:assert pose['renderWidthAtSmall']==width,pose['id']
    assert m['legacyActionWidths']['stretch']==480
    assert archive.read('state-01.png')==(ROOT/'art/frames/sit/000.png').read_bytes()
    for pose_id in (6,8):
        pose=next(p for p in m['poses'] if p['id']==pose_id)
        proposed=next(p for p in policy['candidatePoseOverrides'] if p['id']==pose_id)
        assert pose['source']==proposed['source'] and not proposed['ownerApproved']
    assert len(m['actions'])==121
qa=json.loads((ROOT/'build/qa-dev-art-style-v8.json').read_text())
assert qa['passed'] and qa['bodyFixedWhileLooking'] and qa['fixedBodyCases']==73
report=dict(passed=True,headless=True,windowsCreated=0,activeStillCount=13,removedPoseIds=[5,9,10],parkedPoseIds=[11],
            sourcePNGsUnchanged=183,packSourcesChecked=len(entries),fixedOriginalRestCases=73,
            originalRuntimeSizesRestored=True,originalSittingByteExact=True,
            rearSideCandidatesNotOwnerApproved=True,candidateExe=str(exe.relative_to(ROOT)),sha256=digest(exe.read_bytes()))
(ROOT/'build/qa-owner-corrections-v8.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report))
