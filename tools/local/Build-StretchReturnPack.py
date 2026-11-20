"""A27: register original stretch drawings; preserve all original PNG pixels."""
from pathlib import Path
from zipfile import ZipFile,ZIP_DEFLATED
import copy,hashlib,json
import cv2,numpy as np
root=Path(__file__).resolve().parents[2];work=root/'build/stretch-return-v1'
with ZipFile(root/'build/Odin.latest.zip') as z:payloads={n:z.read(n) for n in z.namelist()}
m=json.loads(payloads['manifest.json']);contact=json.loads((work/'contact.json').read_text());sit=copy.deepcopy(next(e for e in m['actions'] if e['clip']=='sit' and e['frame']==0))
width=m['legacyActionWidths']['stretch'];scale=round(width)/1024
target=(contact['sit']['frontContactX']-sit['anchorX'])*sit['renderWidthAtSmall']/1024
keys=[copy.deepcopy(sit)]
for row in contact['frames']:
 i=row['frame'];p=root/f'art/frames/stretch/{i:03d}.png';data=p.read_bytes();im=cv2.imdecode(np.frombuffer(data,np.uint8),-1);ys,xs=np.where(im[:,:,3]>128)
 x=row['frontContactX']
 if len(row['toeGroups'])==1:x+=26
 key=f'stretch-original/{i:03d}.png';payloads[key]=data
 keys.append(dict(key=key,sha256=hashlib.sha256(data).hexdigest(),id=0,clip='stretch',frame=i+1,kind='action',source=p.relative_to(root).as_posix(),nativeWidth=1024,nativeHeight=1024,bounds=[int(xs.min()),int(ys.min()),int(xs.max()-xs.min()+1),int(ys.max()-ys.min()+1)],customAnchor=True,anchorX=x-target/scale,supportY=944 if i in (0,26,27) else 951,renderWidthAtSmall=round(width),airborne=False))
keys.append(copy.deepcopy(sit))
for i,e in enumerate(keys):e.update(id=0,clip='stretch',frame=i)
m['actions']=[e for e in m['actions'] if e['clip']!='stretch']+keys
delays=[.055]+[(4.6-.11)/28]*28+[.055]
m['actionClips']=[c for c in m['actionClips'] if c['clip']!='stretch']+[dict(clip='stretch',frames=30,seconds=4.6,frameSeconds=delays,loop=False)]
m['registeredStretchV1']=True;payloads['manifest.json']=json.dumps(m,separators=(',',':')).encode()
with ZipFile(work/'candidate.zip','w',ZIP_DEFLATED) as z:
 for k,v in sorted(payloads.items()):z.writestr(k,v)
(work/'registration.json').write_text(json.dumps(dict(originalPixelsPreserved=True,unchangedStretchDuration=4.6,firstAndLastExactSit=True,keys=keys,frameSeconds=delays),indent=2)+'\n')
print('Built stretch review: all 28 original drawings, exact sitting endpoints, placement metadata only.')
