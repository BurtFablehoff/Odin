"""Record failed internal artwork experiments and verify protected provenance.

No visual approval is inferred from these mechanical preservation checks.
"""
import hashlib
import json
from pathlib import Path
from zipfile import ZipFile

root=Path(__file__).resolve().parents[2]
snapshot=root/'draft-assets/approved-transition-preview-v3'
approval=json.loads((snapshot/'approval.json').read_text(encoding='utf-8-sig'))
for item in approval['files']:
    assert hashlib.sha256((snapshot/item['path']).read_bytes()).hexdigest()==item['sha256'],item['path']
work=root/'build/geometry-motion-v10'
changed={'sit_to_loaf','loaf_to_sit','sit_to_lay','lay_to_sit','loaf_to_lay','lay_to_loaf'}
with ZipFile(root/'build/drawn-motion-v9/candidate.zip') as baseline,ZipFile(work/'candidate.zip') as current:
    before=json.loads(baseline.read('manifest.json'));after=json.loads(current.read('manifest.json'))
    for name in baseline.namelist():
        if name!='manifest.json':assert baseline.read(name)==current.read(name),name
    for field in ('actions','actionClips'):
        assert [e for e in before[field] if e['clip'] not in changed]==[e for e in after[field] if e['clip'] not in changed],field
    for clip in changed:
        a=sorted((e for e in before['actions'] if e['clip']==clip),key=lambda e:e['frame'])
        b=sorted((e for e in after['actions'] if e['clip']==clip),key=lambda e:e['frame'])
        for old,new in ((a[0],b[0]),(a[-1],b[-1])):
            for field in ('key','sha256','anchorX','supportY','renderWidthAtSmall'):
                assert old[field]==new[field],(clip,field)
report=dict(
    date='2026-11-18',revision=10,status='FAILED INTERNAL REVIEW; DO NOT HAND OFF',
    ownerFeedback='All three V9 routes are equally bad; jerky with apparent growth.',
    ownerApproval=False,exe=None,productionPromotion=False,
    findings=[
        'V9 fixed render width did not imply fixed anatomical size or continuous body registration.',
        'Eye/socket spacing confounds head yaw. The discarded eye-span calibration enlarged a bridge and produced about 34% adjacent opaque-area growth.',
        'Forehead SIFT matches are a secondary diagnostic, not anatomical ground truth. Low inlier counts and pose projection limit confidence.',
        'Tail/alpha-left anchoring can move the whole cat when tail shape changes; fixed floor columns can sample different anatomy.',
        'The revised drawn sit/lay bridge still reverses head height and changes rear-body/limb outline abruptly.',
        'Additional drawings and scale calibration reduced some numerical jumps but did not establish smooth coherent animation.',
    ],
    diagnostics=json.loads((work/'displayed-geometry.json').read_text()),
    files=dict(prompts='draft-assets/geometry-motion-v10/prompts.json',raw='draft-assets/geometry-motion-v10/raw',
               registration='draft-assets/geometry-motion-v10/registration.json',skullMatches='build/geometry-motion-v10/skull-similarity.json',
               sheet='build/geometry-motion-v10/contact-sheet.png'),
    preservation=dict(frozenSnapshotFilesVerified=len(approval['files']),baselinePngBytesUnchanged=True,heldEndpointRegistrationUnchanged=True,unrelatedManifestEntriesUnchanged=True),
    nextAcceptance='Inspect consistent skull/chest volume, planted paws/pelvis, monotonically progressing joints and clean fur/alpha in complete forward/reverse playback at actual display size before another EXE handoff. Numbers alone cannot pass the motion.'
)
path=root/'planning/transition-review-history/v10-internal-failed.json'
path.write_text(json.dumps(report,indent=2)+'\n')
review_path=root/'planning/current-transition-review.json'
review=json.loads(review_path.read_text(encoding='utf-8-sig'))
review['ownerRejectionScope']='All three routes equally bad: sit/loaf, sit/lay, direct loaf/lay.'
review['latestInvestigation']='planning/transition-review-history/v10-internal-failed.json'
review_path.write_text(json.dumps(review,indent=2)+'\n')
print(json.dumps(dict(status=report['status'],preservation=report['preservation'],exe=report['exe'])))
