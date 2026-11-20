"""A28: combine retained stretch with original grooming and drawn entry/return."""
from pathlib import Path
from zipfile import ZipFile,ZIP_DEFLATED
import copy,hashlib,json
root=Path(__file__).resolve().parents[2];work=root/'build/groom-return-v1';work.mkdir(exist_ok=True)
base=root/'build/stretch-return-v1/candidate.zip';receipt=json.loads((root/'planning/current-stretch-review.json').read_text())
pin=next(e['sha256'] for e in receipt['artifacts'] if e['path']=='build/stretch-return-v1/candidate.zip');assert hashlib.sha256(base.read_bytes()).hexdigest()==pin
snapshot=root/'draft-assets/approved-transition-preview-v3';approved=json.loads((snapshot/'approval.json').read_text())
source=snapshot/'build/transitions-v3/candidate.zip';pin=next(e['sha256'] for e in approved['files'] if e['path']=='build/transitions-v3/candidate.zip');assert hashlib.sha256(source.read_bytes()).hexdigest()==pin
with ZipFile(base) as z:payloads={n:z.read(n) for n in z.namelist()}
m=json.loads(payloads['manifest.json']);before=copy.deepcopy(m);changed={'groom','sit_to_groom','groom_to_sit'}
with ZipFile(source) as z:
 old=json.loads(z.read('manifest.json'));keys=[copy.deepcopy(e) for e in old['actions'] if e['clip'] in changed]
 for e in keys:
  data=z.read(e['key'])
  if e['key'] in payloads:assert payloads[e['key']]==data,e['key']
  payloads[e['key']]=data
 for i in range(16):
  prior=next(e for e in before['actions'] if e['clip']=='groom' and e['frame']==i);new=next(e for e in keys if e['clip']=='groom' and e['frame']==i)
  assert prior['sha256']==new['sha256']
 m['actions']=[e for e in m['actions'] if e['clip'] not in changed]+keys
 m['actionClips']=[c for c in m['actionClips'] if c['clip'] not in changed]+[copy.deepcopy(c) for c in old['actionClips'] if c['clip'] in changed]
m['registeredGroomV1']=True
assert [e for e in m['actions'] if e['clip'] not in changed]==[e for e in before['actions'] if e['clip'] not in changed]
assert [c for c in m['actionClips'] if c['clip'] not in changed]==[c for c in before['actionClips'] if c['clip'] not in changed]
payloads['manifest.json']=json.dumps(m,separators=(',',':')).encode()
with ZipFile(work/'candidate.zip','w',ZIP_DEFLATED) as z:
 for k,data in sorted(payloads.items()):z.writestr(k,data)
(work/'registration.json').write_text(json.dumps(dict(originalSixteenGroomPngsPreserved=True,stretchAndRestMetadataUnchanged=True,source='draft-assets/approved-transition-preview-v3/build/transitions-v3/candidate.zip',keys=keys,timelines=[c for c in m['actionClips'] if c['clip'] in changed]),indent=2)+'\n')
print('Built grooming entry/return candidate; original 16 lick images, stretch and passed rest motion retained.')
