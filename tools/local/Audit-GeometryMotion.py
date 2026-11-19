"""Measure displayed geometry, including silhouette/body changes, before a build handoff."""
import json
import sys
from pathlib import Path
from zipfile import ZipFile
import cv2
import numpy as np

root=Path(__file__).resolve().parents[2]
version=sys.argv[1] if len(sys.argv)>1 else 'geometry-motion-v10'
work=root/'build'/version
results={}
with ZipFile(work/'candidate.zip') as z:
    manifest=json.loads(z.read('manifest.json'))
    for clip in ('sit_to_loaf','sit_to_lay','loaf_to_lay'):
        entries=sorted((e for e in manifest['actions'] if e['clip']==clip),key=lambda e:e['frame'])
        points=[];spans=[];areas=[];masks=[];rows=[]
        for e in entries:
            im=cv2.imdecode(np.frombuffer(z.read(e['key']),np.uint8),cv2.IMREAD_UNCHANGED)
            hsv=cv2.cvtColor(im[...,:3],cv2.COLOR_BGR2HSV)
            m=((hsv[...,0]>20)&(hsv[...,0]<80)&(hsv[...,1]>70)&(im[...,3]>200)).astype(np.uint8)
            if im.shape[:2]==(1024,1024):
                m[:500]=0;m[920:]=0;m[:,:400]=0;m[:,760:]=0
            n,_,stats,centers=cv2.connectedComponentsWithStats(m)
            eye=centers[max((j for j in range(1,n) if 5<stats[j,4]<700),key=lambda j:stats[j,4])]
            x,y=eye;gray=cv2.cvtColor(im[...,:3],cv2.COLOR_BGR2GRAY)
            dark=((gray<95)&(im[...,3]>200)).astype(np.uint8);roi=np.zeros_like(dark);roi[int(y-16):int(y+17),int(x+30):int(x+100)]=1
            n,_,stats,centers=cv2.connectedComponentsWithStats(dark*roi)
            socket=centers[min((j for j in range(1,n) if 8<stats[j,4]<700),key=lambda j:abs(centers[j,1]-y)+abs(centers[j,0]-x-60)*.4)]
            scale=e['renderWidthAtSmall']/im.shape[1]
            point=np.array([(x-e['anchorX'])*scale,(y-e['supportY'])*scale])
            span=float(np.linalg.norm(socket-eye)*scale)
            matrix=np.array([[scale,0,260-e['anchorX']*scale],[0,scale,280-e['supportY']*scale]],np.float32)
            alpha=cv2.warpAffine(im[...,3],matrix,(560,330),flags=cv2.INTER_LINEAR)
            mask=(alpha>128).astype(np.uint8);area=int(mask.sum())
            points.append(point);spans.append(span);areas.append(area);masks.append(mask)
            rows.append(dict(frame=e['frame'],key=e['key'],head=point.tolist(),faceSpan=span,opaqueArea=area,renderWidth=e['renderWidthAtSmall'],opaquePixelsOnCellSides=int(mask[:,0].sum()+mask[:,-1].sum())))
        steps=np.linalg.norm(np.diff(points,axis=0),axis=1)
        size_changes=abs(np.diff(spans)/np.array(spans[:-1])*100)
        area_changes=abs(np.diff(areas)/np.array(areas[:-1])*100)
        edges=[]
        for a,b in zip(masks,masks[1:]):
            edge_a=a-cv2.erode(a,np.ones((3,3),np.uint8));edge_b=b-cv2.erode(b,np.ones((3,3),np.uint8))
            da=cv2.distanceTransform(1-edge_a,cv2.DIST_L2,5);db=cv2.distanceTransform(1-edge_b,cv2.DIST_L2,5)
            edges.append(float(np.percentile(np.concatenate((da[edge_b>0],db[edge_a>0])),95)))
        results[clip]=dict(frames=rows,maxHeadStepPx=float(steps.max()),headSteps=steps.tolist(),maxFaceSpanChangePct=float(size_changes.max()),maxOpaqueAreaChangePct=float(area_changes.max()),areaChanges=area_changes.tolist(),maxSilhouetteEdgeStep95Px=max(edges),silhouetteEdges=edges)
(work/'displayed-geometry.json').write_text(json.dumps(results,indent=2))
print(json.dumps({k:{f:v[f] for f in ('maxHeadStepPx','maxFaceSpanChangePct','maxOpaqueAreaChangePct','maxSilhouetteEdgeStep95Px')} for k,v in results.items()}))
