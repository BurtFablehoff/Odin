"""A33: remove redundant seated redraws, preserving original movement/timing."""
from pathlib import Path
from zipfile import ZipFile,ZIP_DEFLATED
import copy,hashlib,json
root=Path(__file__).resolve().parents[2];work=root/'build/stretch-tail-v5';work.mkdir(exist_ok=True)
base=root/'build/action-review-v4/candidate.zip';receipt=json.loads((root/'planning/action-review-history/v4-before-stretch-tail.json').read_text())
pin=next(f['sha256'] for f in receipt['artifacts'] if f['path']=='build/action-review-v4/candidate.zip');assert hashlib.sha256(base.read_bytes()).hexdigest()==pin
with ZipFile(base) as z:payloads={n:z.read(n) for n in z.namelist()}
m=json.loads(payloads['manifest.json']);before=copy.deepcopy(m);clip=next(c for c in m['actionClips'] if c['clip']=='stretch');keys=sorted((e for e in m['actions'] if e['clip']=='stretch'),key=lambda e:e['frame'])
omitted={'stretch-original/025.png','stretch-original/026.png','stretch-original/027.png'};kept=[];timing=[];removed=0
for e,seconds in zip(keys,clip['frameSeconds']):
 if e['key'] in omitted:removed+=seconds;continue
 kept.append(e);timing.append(seconds)
timing[-1]+=removed
for i,e in enumerate(kept):e['frame']=i
m['actions']=[e for e in m['actions'] if e['clip']!='stretch']+kept;clip.update(frames=len(kept),frameSeconds=timing)
assert abs(sum(timing)-4.6)<1e-9
for group in ['actions','actionClips']:assert [e for e in m[group] if e['clip']!='stretch']==[e for e in before[group] if e['clip']!='stretch']
payloads['manifest.json']=json.dumps(m,separators=(',',':')).encode()
with ZipFile(work/'candidate.zip','w',ZIP_DEFLATED) as z:
 for name,data in sorted(payloads.items()):z.writestr(name,data)
(work/'tail-change.json').write_text(json.dumps(dict(omittedFromPlayback=sorted(omitted),sourcePngsRetained=True,selectedOriginalStretchDrawings=25,frames=len(kept),seconds=4.6,finalExactSitHold=timing[-1],otherActionsUnchanged=True,keys=kept,frameSeconds=timing),indent=2)+'\n')
print('V5: removed three redundant seated variants; exact final sitting held for their duration; other motion unchanged.')
