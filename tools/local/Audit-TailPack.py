"""A16: ensure the tail candidate preserves the approved transition evidence."""
import hashlib
import json
import sys
from pathlib import Path
from zipfile import ZipFile

root = Path(__file__).resolve().parents[2]
drawn = '--drawn' in sys.argv
tip = '--tip' in sys.argv
tip_revision = int(sys.argv[sys.argv.index('--tip') + 1]) if tip else 6
version = f'tail-v{tip_revision}' if tip else 'tail-v5' if drawn else 'tail-v4'
frame_count = 7 if drawn else 17
snapshot = root / 'draft-assets/approved-transition-preview-v3'
approval = json.loads((snapshot / 'approval.json').read_text(encoding='utf-8-sig'))
for item in approval['files']:
    assert hashlib.sha256((snapshot / item['path']).read_bytes()).hexdigest() == item['sha256'], item['path']

with ZipFile(snapshot / 'build/transitions-v3/candidate.zip') as before, ZipFile(root / f'build/{version}/candidate.zip') as after:
    assert set(before.namelist()).issubset(after.namelist())
    extras = set(after.namelist()) - set(before.namelist())
    assert extras == ({f'tail-v5/{family}-{key}.png' for family in ('sit', 'lay', 'sleep') for key in ('low', 'mid', 'peak')} if drawn else set())
    payloads = 0
    for name in before.namelist():
        if name != 'manifest.json':
            assert before.read(name) == after.read(name), name
            payloads += 1
    a, b = (json.loads(z.read('manifest.json')) for z in (before, after))
    assert b.pop('subtleTailTipV6' if tip else 'drawnTailFlipV5' if drawn else 'repairedTailFlipV4') is True
    if tip and tip_revision >= 7:
        assert b.pop('tailTipRevision') == tip_revision
    if tip or drawn:
        assert not b.get('repairedTailFlipV4', False)
    if tip:
        assert not b.get('drawnTailFlipV5', False)
    assert b.pop('restoredRestFrameCount') == 58 + 3 * frame_count
    a.pop('restoredRestFrameCount')
    tail = lambda x: x['clip'] in ('sit_tail', 'lay_tail', 'sleep_tail')
    for field in ('actions', 'actionClips'):
        original = a.pop(field)
        candidate = b.pop(field)
        assert [x for x in original if not tail(x)] == [x for x in candidate if not tail(x)], field
        if field == 'actions':
            for family in ('sit', 'lay', 'sleep'):
                held = next(x for x in original if x['clip'] == family and x['frame'] == 0)
                frames = [x for x in candidate if x['clip'] == family + '_tail']
                assert len(frames) == frame_count
                for i, entry in enumerate(frames):
                    expected = dict(held, clip=family + '_tail', frame=i)
                    if drawn and i not in (0, 6):
                        name = ('neutral', 'low', 'mid', 'peak', 'mid', 'low', 'neutral')[i]
                        key = f'tail-v5/{family}-{name}.png'
                        expected.update(key=key, source=f'draft-assets/tail-v5/frames/{family}-{name}.png', sha256=hashlib.sha256(after.read(key)).hexdigest())
                    assert entry == expected, (family, i)
        else:
            for entry in original:
                if tail(entry):
                    assert dict(entry, frames=frame_count) in candidate
    assert a == b, 'Unexpected non-tail metadata change'

report = dict(passed=True, unchangedPayloads=payloads, frozenApprovalFiles=len(approval['files']), nonTailMetadataUnchanged=True)
(root / f'build/{version}/preservation.json').write_text(json.dumps(report, indent=2))
print(json.dumps(report))
