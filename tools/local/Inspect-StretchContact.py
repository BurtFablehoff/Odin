"""Measure original drawing registration; never changes artwork pixels."""
from pathlib import Path
import json,cv2,numpy as np
root=Path(__file__).resolve().parents[2];work=root/'build/stretch-return-v1';work.mkdir(exist_ok=True)
def contact(im):
 a=im[:,:,3]>128;mask=a[945:956].sum(axis=0)>=2
 runs=[];start=None
 for x,v in enumerate(np.r_[mask,False]):
  if v and start is None:start=x
  if not v and start is not None:
   if x-start>=8:runs.append((start,x))
   start=None
 assert runs
 chosen=[runs[-1]]
 if len(runs)>1 and runs[-1][0]-runs[-2][1]<55:chosen.insert(0,runs[-2])
 columns=np.concatenate([np.arange(a,b) for a,b in chosen])
 return dict(frontContactX=float(columns.mean()),toeGroups=chosen)
def iris(im):
 hsv=cv2.cvtColor(im[:,:,:3],cv2.COLOR_BGR2HSV);m=((hsv[:,:,0]>20)&(hsv[:,:,0]<80)&(hsv[:,:,1]>70)&(im[:,:,3]>200)).astype('uint8');m[:450]=0;m[925:]=0;m[:,:180]=0;m[:,850:]=0
 n,lab,stats,centers=cv2.connectedComponentsWithStats(m);return centers[max((j for j in range(1,n) if 10<stats[j,4]<1000),key=lambda j:stats[j,4])]
def features(im):
 x,y=map(int,iris(im));mask=np.zeros(im.shape[:2],np.uint8);mask[y-100:y+30,x-85:x+145]=255;mask[im[:,:,3]<200]=0
 return cv2.SIFT_create(nfeatures=2000,contrastThreshold=.01,edgeThreshold=15).detectAndCompute(cv2.cvtColor(im[:,:,:3],cv2.COLOR_BGR2GRAY),mask)
sit=cv2.imread(str(root/'art/frames/sit/000.png'),-1);kp0,d0=features(sit);rows=[]
for i in range(28):
 im=cv2.imread(str(root/f'art/frames/stretch/{i:03d}.png'),-1);row=contact(im);row['frame']=i
 if i in (0,27):
  kp,d=features(im);matches=[a for a,b in cv2.BFMatcher().knnMatch(d0,d,k=2) if a.distance<.72*b.distance]
  mat,inliers=cv2.estimateAffinePartial2D(np.float32([kp0[x.queryIdx].pt for x in matches]),np.float32([kp[x.trainIdx].pt for x in matches]),method=cv2.RANSAC,ransacReprojThreshold=2,maxIters=10000)
  row.update(skullScale=float(np.hypot(mat[0,0],mat[1,0])),inliers=int(inliers.sum()))
 rows.append(row)
report=dict(sit=contact(sit),frames=rows)
(work/'contact.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
