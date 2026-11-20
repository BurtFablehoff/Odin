"""A31/A32: preserve motion cadence; trim foreign right-edge sheet content via viewport."""
from pathlib import Path
from zipfile import ZipFile,ZIP_DEFLATED
import copy,hashlib,json
import cv2,numpy as np
root=Path(__file__).resolve().parents[2];work=root/'build/action-review-v4';work.mkdir(exist_ok=True)
snap=root/'draft-assets/approved-groom-motion-v3';approved=json.loads((snap/'approval.json').read_text());source=snap/'build/groom-stable-v3/candidate.zip'
pin=next(f['sha256'] for f in approved['files'] if f['path']=='build/groom-stable-v3/candidate.zip');assert hashlib.sha256(source.read_bytes()).hexdigest()==pin
with ZipFile(source) as z:payloads={n:z.read(n) for n in z.namelist()}
m=json.loads(payloads['manifest.json']);old=copy.deepcopy(m);groom=sorted((e for e in m['actions'] if e['clip']=='groom'),key=lambda e:e['frame'])
m['actions']=[e for e in m['actions'] if e['clip']!='groom']+groom[:2]
c=next(c for c in m['actionClips'] if c['clip']=='groom');oldCadence=c['seconds']/c['frames'];c.update(frames=2,seconds=oldCadence*2)
edges=[]
for e in m['actions']:
 if e['clip'] not in ('sit_to_loaf','loaf_to_sit') or not e['key'].startswith('coherent-v11/'):continue
 im=cv2.imdecode(np.frombuffer(payloads[e['key']],np.uint8),-1)
 if e.get('contentRect'):
  x,y,w,h=e['contentRect'];im=im[y:y+h,x:x+w]
 n,labels,stats,centers=cv2.connectedComponentsWithStats((im[:,:,3]>128).astype('uint8'));main=stats[1+np.argmax(stats[1:,4])];right=min(im.shape[1],int(main[0]+main[2])+32)
 assert not (im[:,right:,3]>128).any(),'Viewport would remove opaque foreground.'
 e['visibleSourceRect']=[0,0,right,im.shape[0]]
 edges.append(dict(clip=e['clip'],frame=e['frame'],key=e['key'],viewport=e['visibleSourceRect'],removedRightColumns=im.shape[1]-right,maxForeignAlpha=int(im[:,right:,3].max()) if right<im.shape[1] else 0))
payloads['manifest.json']=json.dumps(m,separators=(',',':')).encode()
with ZipFile(work/'candidate.zip','w',ZIP_DEFLATED) as z:
 for name,data in sorted(payloads.items()):z.writestr(name,data)
(work/'changes.json').write_text(json.dumps(dict(groomCadenceSeconds=oldCadence,lickPairSeconds=oldCadence*2,completeActionSeconds=[2,4],entrySeconds=next(c['seconds'] for c in m['actionClips'] if c['clip']=='sit_to_groom'),returnSeconds=next(c['seconds'] for c in m['actionClips'] if c['clip']=='groom_to_sit'),rightEdgeViewports=edges,sourcePngsUnchanged=True),indent=2)+'\n')
print('V4: original lick cadence, complete short grooming episodes, conservative right-edge viewports; no PNG changes.')
