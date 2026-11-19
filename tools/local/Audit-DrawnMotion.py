"""A21: discrete drawing provenance, order, timing and baseline preservation."""
import json
import hashlib
from pathlib import Path
from zipfile import ZipFile
import cv2
import numpy as np

root=Path(__file__).resolve().parents[2]
work=root/'build/drawn-motion-v9'
changed={'sit_to_loaf','loaf_to_sit','sit_to_lay','lay_to_sit','loaf_to_lay','lay_to_loaf'}
snapshot=root/'draft-assets/approved-transition-preview-v3'
for item in json.loads((snapshot/'approval.json').read_text(encoding='utf-8-sig'))['files']:
    assert hashlib.sha256((snapshot/item['path']).read_bytes()).hexdigest()==item['sha256']
def eye(data):
    im=cv2.imdecode(np.frombuffer(data,np.uint8),cv2.IMREAD_UNCHANGED)
    hsv=cv2.cvtColor(im[...,:3],cv2.COLOR_BGR2HSV)
    m=((hsv[...,0]>20)&(hsv[...,0]<80)&(hsv[...,1]>70)&(im[...,3]>200)).astype(np.uint8)
    m[:500]=0;m[920:]=0;m[:,:400]=0;m[:,760:]=0
    n,labels,stats,centers=cv2.connectedComponentsWithStats(m)
    valid=[i for i in range(1,n) if 15<stats[i,4]<700]
    assert valid,'Missing iris in drawing'
    return centers[max(valid,key=lambda i:stats[i,4])]
report={}
with ZipFile(root/'build/tail-v7/candidate.zip') as before,ZipFile(work/'candidate.zip') as after:
    for name in before.namelist():
        if name!='manifest.json':assert before.read(name)==after.read(name),name
    extras=set(after.namelist())-set(before.namelist())
    assert len(extras)==8 and all(x.startswith('drawn-v9/') for x in extras)
    a,b=[json.loads(z.read('manifest.json')) for z in (before,after)]
    assert b.pop('drawnRestV9') is True
    assert not b.get('smoothRestV8',False)
    for clip in ('sit_to_loaf','sit_to_lay','loaf_to_lay'):
        entries=sorted((x for x in b['actions'] if x['clip']==clip),key=lambda x:x['frame'])
        start,end=clip.split('_to_')
        back=sorted((x for x in b['actions'] if x['clip']==end+'_to_'+start),key=lambda x:x['frame'],reverse=True)
        expected=5 if clip=='loaf_to_lay' else 7
        assert len(entries)==len(back)==expected
        timing=next(x for x in b['actionClips'] if x['clip']==clip)
        reverse=next(x for x in b['actionClips'] if x['clip']==end+'_to_'+start)
        assert len(timing['frameSeconds'])==expected and abs(sum(timing['frameSeconds'])-timing['seconds'])<1e-8
        assert .3<=timing['seconds']<=.6
        assert timing['frameSeconds']==reverse['frameSeconds'][::-1]
        points=[]
        for x,y in zip(entries,back):
            assert all(x.get(k)==y.get(k) for k in ('key','sha256','anchorX','supportY','renderWidthAtSmall'))
            assert 'motion-v8' not in x.get('source','')
            assert hashlib.sha256(after.read(x['key'])).hexdigest()==x['sha256']
            p=eye(after.read(x['key']))
            points.append(((p[1]-x['supportY'])*x['renderWidthAtSmall']/1024)+944)
        if clip.startswith('sit_'):assert min(np.diff(points))>-2,(clip,'reversed lowering',points)
        report[clip]=dict(drawings=len(entries)-2,totalSeconds=timing['seconds'],frameSeconds=timing['frameSeconds'],worldIrisHeights=points)
    for field in ('actions','actionClips'):
        old,new=a.pop(field),b.pop(field)
        assert [x for x in old if x['clip'] not in changed]==[x for x in new if x['clip'] not in changed],field
    assert a==b,'Unrelated metadata changed'
result=dict(passed=True,method='Individually drawn poses; discrete timed playback; no optical-flow or generated intermediate rasters',clips=report,originalPayloadsUnchanged=True,tailV7Unchanged=True)
(work/'drawing-audit.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result))
