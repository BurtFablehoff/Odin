"""A35: integrate the exact owner-passed V5 pack into clone-contained production inputs."""
from pathlib import Path
from zipfile import ZipFile
import json,hashlib
root=Path(__file__).resolve().parents[2];snapshot=root/'draft-assets/approved-action-review-v5'
approval=json.loads((snapshot/'approval.json').read_text())
for item in approval['files']:assert hashlib.sha256((snapshot/item['path']).read_bytes()).hexdigest()==item['sha256'],item['path']
source=snapshot/'build/stretch-tail-v5/candidate.zip'
with ZipFile(source) as z:payloads={n:z.read(n) for n in z.namelist()}
manifest=json.loads(payloads['manifest.json']);latest=root/'art/latest';index=json.loads((latest/'index.json').read_text());prior={e['key']:e for e in index['entries']}
entries={}
for group in ['poses','blinks','motions','actions']:
 for e in manifest.get(group,[]):entries[e['key']]=e
rows=[];stored=[]
for key,data in payloads.items():
 if key=='manifest.json':continue
 digest=hashlib.sha256(data).hexdigest();e=entries.get(key,{})
 if e.get('sha256'):assert e['sha256']==digest,key
 previous=prior.get(key);path=root/previous['from'] if previous else None
 if path and path.is_file() and hashlib.sha256(path.read_bytes()).hexdigest()==digest:origin=previous['from']
 else:
  raw=root/e.get('source','__no_source__')
  if raw.is_file() and raw.read_bytes()==data:origin=raw.relative_to(root).as_posix()
  else:
   raw=latest/'actions-v5'/key;raw.parent.mkdir(parents=True,exist_ok=True);raw.write_bytes(data);origin=raw.relative_to(root).as_posix();stored.append(origin)
 rows.append(dict(key=key,sha256=digest,bytes=len(data),**{'from':origin}))
(latest/'manifest.json').write_bytes(payloads['manifest.json'])
index.update(manifestSha256=hashlib.sha256(payloads['manifest.json']).hexdigest(),entries=rows,counts=dict(entries=len(rows)),promotedFrom=dict(date='2026-11-20',rule='A35',approval='draft-assets/approved-action-review-v5/approval.json',reviewedBuild=approval['exe'],reviewedPackSha256=hashlib.sha256(source.read_bytes()).hexdigest(),tailCorrectionIncluded=False))
(latest/'index.json').write_text(json.dumps(index,indent=2)+'\n')
work=root/'build/action-v5-integration';work.mkdir(exist_ok=True);(work/'promotion.json').write_text(json.dumps(dict(verifiedSnapshotFiles=len(approval['files']),manifestBytesMatchApproved=True,sourcePngPayloadsUnchanged=True,entries=len(rows),newTrackedPayloads=stored),indent=2)+'\n')
print(f'Integrated exact approved V5 manifest and {len(rows)} PNG payloads; normal build remains PowerShell/.NET only.')
