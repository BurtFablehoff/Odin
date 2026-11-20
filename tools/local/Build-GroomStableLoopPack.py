"""A30: stable bowed-head lick loop using matching existing whole drawings."""
from pathlib import Path
from zipfile import ZipFile,ZIP_DEFLATED
import copy,hashlib,json
root=Path(__file__).resolve().parents[2];work=root/'build/groom-stable-v3';work.mkdir(exist_ok=True)
base=root/'build/groom-return-v1/candidate.zip';receipt=json.loads((root/'planning/groom-review-history/v1-before-scale.json').read_text())
pin=next(e['sha256'] for e in receipt['artifacts'] if e['path']=='build/groom-return-v1/candidate.zip');assert hashlib.sha256(base.read_bytes()).hexdigest()==pin
with ZipFile(base) as z:payloads={n:z.read(n) for n in z.namelist()}
m=json.loads(payloads['manifest.json']);before=copy.deepcopy(m);changed={'groom','sit_to_groom','groom_to_sit'}
groom=sorted((copy.deepcopy(e) for e in m['actions'] if e['clip']=='groom'),key=lambda e:e['frame'])
entry=sorted((copy.deepcopy(e) for e in m['actions'] if e['clip']=='sit_to_groom'),key=lambda e:e['frame'])
# Keep original whole-pose PNGs. The first two bowed lick poses have matching
# lower-body proportions; upright and differently puffed redraws leave the loop.
loop=[copy.deepcopy(groom[1+i%2]) for i in range(16)]
entry=entry+[copy.deepcopy(groom[1])]
back=[copy.deepcopy(groom[2]),copy.deepcopy(groom[1])]+[copy.deepcopy(e) for e in entry[-2::-1]]
specs=[('sit_to_groom',entry,[.04,.14,.14,.16,.18,.04]),('groom',loop,None),('groom_to_sit',back,[.04,.12,.18,.16,.14,.14,.04])]
actions=[e for e in m['actions'] if e['clip'] not in changed];clips=[c for c in m['actionClips'] if c['clip'] not in changed]
for name,keys,delays in specs:
 for i,e in enumerate(keys):e.update(clip=name,frame=i);actions.append(e)
 timeline=dict(clip=name,frames=len(keys),seconds=sum(delays) if delays else 3.5,loop=name=='groom')
 if delays:timeline['frameSeconds']=delays
 clips.append(timeline)
m.update(actions=actions,actionClips=clips,stableGroomV3=True)
assert [e for e in m['actions'] if e['clip'] not in changed]==[e for e in before['actions'] if e['clip'] not in changed]
payloads['manifest.json']=json.dumps(m,separators=(',',':')).encode()
with ZipFile(work/'candidate.zip','w',ZIP_DEFLATED) as z:
 for key,data in sorted(payloads.items()):z.writestr(key,data)
(work/'selection.json').write_text(json.dumps(dict(sourceBaseline='build/groom-return-v1/candidate.zip',rejectedScaleTweakNotUsed=True,sourceLickFrames=[1,2],playbackSlots=16,cycleSeconds=3.5,originalPngsPreserved=True,entryAndReturn=specs[::2],sourceOrder='One head-lowering entry; alternate matching closed-eye lick poses; one head-raising return.'),indent=2)+'\n')
print('Built stable grooming loop from original whole lick poses; head lowers once and rises once; no scale tweak.')
