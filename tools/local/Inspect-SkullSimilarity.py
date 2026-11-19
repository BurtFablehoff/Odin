"""Compare forehead/skull feature geometry, independent of projected eye spacing."""
import cv2
import json
import numpy as np
from pathlib import Path
from zipfile import ZipFile

root=Path(__file__).resolve().parents[2]
def descriptors(im,eye):
    x,y=map(int,eye);mask=np.zeros(im.shape[:2],np.uint8);mask[max(0,y-100):y+32,max(0,x-85):x+145]=255;mask[im[...,3]<200]=0
    gray=cv2.cvtColor(im[...,:3],cv2.COLOR_BGR2GRAY)
    return cv2.SIFT_create(nfeatures=2000,contrastThreshold=.01,edgeThreshold=15).detectAndCompute(gray,mask)
g=json.loads((root/'build/drawn-motion-v9/head-geometry.json').read_text())
with ZipFile(root/'build/drawn-motion-v9/candidate.zip') as z:
    m=json.loads(z.read('manifest.json'))
    first=g['sit_to_loaf'][0];ref=cv2.imdecode(np.frombuffer(z.read(first['key']),np.uint8),-1);kp0,d0=descriptors(ref,first['eye'])
    targets={}
    for clip,rows in g.items():
        for row in rows:
            e=next(e for e in m['actions'] if e['clip']==clip and e['frame']==row['frame'])
            targets[e['key']]=(cv2.imdecode(np.frombuffer(z.read(e['key']),np.uint8),-1),row['eye'],e['renderWidthAtSmall'])
    features=json.loads((root/'draft-assets/geometry-motion-v10/features.json').read_text())
    for name in ('loaf-gap-low','loaf-gap-a','lay-shift-clean','lay-shift-mid','direct-gap-a','direct-gap-b','lay-shift-b'):
        targets[name]=(cv2.imread(str(root/f'draft-assets/geometry-motion-v10/frames/{name}.png'),-1),features[name]['eye'],480)
    result={}
    for name,(im,eye,width) in targets.items():
        kp,d=descriptors(im,eye)
        matches=[a for a,b in cv2.BFMatcher().knnMatch(d0,d,k=2) if a.distance<.72*b.distance]
        item=dict(matches=len(matches))
        if len(matches)>=4:
            src=np.float32([kp0[x.queryIdx].pt for x in matches]);dest=np.float32([kp[x.trainIdx].pt for x in matches])
            matrix,inliers=cv2.estimateAffinePartial2D(src,dest,method=cv2.RANSAC,ransacReprojThreshold=2,maxIters=10000)
            if matrix is not None:item.update(inliers=int(inliers.sum()),physicalScale=float(np.hypot(matrix[0,0],matrix[1,0])*width/480))
        result[name]=item
(root/'build/geometry-motion-v10/skull-similarity.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result))
