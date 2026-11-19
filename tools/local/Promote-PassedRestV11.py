"""A25: promote only passed awake rest motion, preserving released unrelated artwork."""
from pathlib import Path
from zipfile import ZipFile
import copy,hashlib,json,subprocess
import cv2,numpy as np
root=Path(__file__).resolve().parents[2];work=root/'build/release-3.3.0';snapshot=root/'draft-assets/approved-transition-motion-v11'
approval=json.loads((snapshot/'approval.json').read_text())
for f in approval['files']:assert hashlib.sha256((snapshot/f['path']).read_bytes()).hexdigest()==f['sha256'],f['path']
with ZipFile(root/'build/production-before-v11.zip') as z:previous={n:z.read(n) for n in z.namelist()}
with ZipFile(snapshot/'build/coherent-motion-v11/candidate.zip') as z:passed={n:z.read(n) for n in z.namelist()}
before=json.loads(previous['manifest.json']);v11=json.loads(passed['manifest.json']);manifest=copy.deepcopy(before)
changed={'sit_to_loaf','loaf_to_sit','sit_to_lay','lay_to_sit','loaf_to_lay','lay_to_loaf'}
lookup={(e['clip'],e['frame']):e for e in v11['actions']};bases={p['id']:p for p in before['poses']};newbases={p['id']:p for p in v11['poses']}
manifest['poses']=[copy.deepcopy(newbases[e['id']]) if e['id'] in (1,2,3) else e for e in manifest['poses']]
for e in manifest['poses']:assert e['sha256']==bases[e['id']]['sha256']
actions=[]
awake={'sit','loaf','lay','look_sit','look_loaf','look_lay','sit_eyes','loaf_eyes','lay_eyes'}
for source in before['actions']:
 e=copy.deepcopy(source)
 if e['clip'] in awake:
  new=lookup[(e['clip'],e['frame'])];assert new['sha256']==e['sha256'];e=copy.deepcopy(new)
 elif e['clip'] in ('sit_tail','lay_tail'):
  old=bases[e['id']];new=newbases[e['id']]
  assert old['renderWidthAtSmall']==new['renderWidthAtSmall']
  e['anchorX']+=new['anchorX']-old['anchorX'];e['supportY']+=new['supportY']-old['supportY']
 actions.append(e)
newactions=[copy.deepcopy(e) for e in v11['actions'] if e['clip'] in changed]
padding={}
for e in newactions:
 im=cv2.imdecode(np.frombuffer(passed[e['key']],np.uint8),-1);a=im[:,:,3]
 if np.r_[a[0],a[-1],a[:,0],a[:,-1]].max()>8:
  padding[e['key']]=im.shape[:2]
inputdir=work/'pad-input';outputdir=work/'padded';inputdir.mkdir(exist_ok=True)
for key in padding:(inputdir/Path(key).name).write_bytes(passed[key])
subprocess.run([str(work/'Pad.exe'),str(inputdir),str(outputdir)],check=True,creationflags=0x08000000)
for key,(h,w) in padding.items():
 data=(outputdir/Path(key).name).read_bytes();old=cv2.imdecode(np.frombuffer(passed[key],np.uint8),-1);new=cv2.imdecode(np.frombuffer(data,np.uint8),-1)
 assert np.array_equal(old[:,:,3],new[16:16+h,16:16+w,3]),key
 visible=old[:,:,3]>0;assert np.array_equal(old[:,:,:3][visible],new[16:16+h,16:16+w,:3][visible]),key
 padding[key]=data
for e in newactions:
 if e['key'] in padding:e.update(contentRect=[16,16,e['nativeWidth'],e['nativeHeight']],sha256=hashlib.sha256(padding[e['key']]).hexdigest())
actions+=newactions;manifest.update(actions=actions,coherentRestV11=True,drawnRestV9=True)
manifest['actionClips'] += [copy.deepcopy(c) for c in v11['actionClips'] if c['clip'] in changed]
latest=root/'art/latest';index=json.loads((latest/'index.json').read_text());rows={e['key']:e for e in index['entries']}
for e in newactions:
 key=e['key'];data=padding.get(key,passed[key])
 if key not in rows or key in padding:
  path=latest/'transitions-v11'/key;path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(data)
  rows[key]=dict(key=key,sha256=hashlib.sha256(data).hexdigest(),bytes=len(data),**{'from':path.relative_to(root).as_posix()})
manifest_bytes=(json.dumps(manifest,indent=2)+'\n').encode();(latest/'manifest.json').write_bytes(manifest_bytes)
index.update(manifestSha256=hashlib.sha256(manifest_bytes).hexdigest(),entries=list(rows.values()),promotedFrom=dict(build=approval['exe'],approval='draft-assets/approved-transition-motion-v11/approval.json',date='2026-11-18',rule='A25',scope=sorted(changed),tailCorrectionIncluded=False))
index['counts']=dict(entries=len(rows));(latest/'index.json').write_text(json.dumps(index,indent=2)+'\n')
(work/'promotion.json').write_text(json.dumps(dict(scope=sorted(changed),sourceSnapshotVerified=len(approval['files']),paddedKeys=list(padding),existingTailFramesPreserved=True,unrelatedClipTimelinesPreserved=True),indent=2)+'\n')
print('Promoted six approved awake rest transitions. Four frames have lossless storage margins. Released tail motion retained.')
