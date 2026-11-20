"""A29: conservative metadata-only correction of grooming drawing 13 (index 12)."""
from pathlib import Path
from zipfile import ZipFile,ZIP_DEFLATED
import json,copy,hashlib
import cv2,numpy as np
root=Path(__file__).resolve().parents[2];work=root/'build/groom-scale-v2';work.mkdir(exist_ok=True)
receipt=json.loads((root/'planning/groom-review-history/v1-before-scale.json').read_text());base=root/'build/groom-return-v1/candidate.zip'
pin=next(e['sha256'] for e in receipt['artifacts'] if e['path']=='build/groom-return-v1/candidate.zip');assert hashlib.sha256(base.read_bytes()).hexdigest()==pin
with ZipFile(base) as z:payloads={k:z.read(k) for k in z.namelist()}
m=json.loads(payloads['manifest.json']);e=next(e for e in m['actions'] if e['clip']=='groom' and e['frame']==12);before=copy.deepcopy(e)
im=cv2.imdecode(np.frombuffer(payloads[e['key']],np.uint8),-1);ys,xs=np.where(im[:,:,3]>128);rear=float(xs.min());factor=.95
e['renderWidthAtSmall']*=factor;e['anchorX']=rear+(e['anchorX']-rear)/factor
payloads['manifest.json']=json.dumps(m,separators=(',',':')).encode()
with ZipFile(work/'candidate.zip','w',ZIP_DEFLATED) as z:
 for k,data in sorted(payloads.items()):z.writestr(k,data)
(work/'correction.json').write_text(json.dumps(dict(frame=12,humanFrameNumber=13,factor=factor,registrationPinX=rear,original=before,corrected=e,originalPngsPreserved=True,allOtherEntriesAndTimelinesUnchanged=True,basis='Visible skull-width outlier relative to nearby bowed-head drawings; 5% conservative live-review candidate. Sparse texture fits were not used as a calibration.'),indent=2)+'\n')
print('Corrected grooming drawing 13 by 5%; preserved its rear/floor placement, original PNG and all other frames.')
