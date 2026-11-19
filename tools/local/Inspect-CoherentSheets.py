"""Measure head and source placement of drawn sheet cells; no artwork writes."""
from pathlib import Path
from zipfile import ZipFile
import json,cv2,numpy as np
root=Path(__file__).resolve().parents[2]
folder=root/'draft-assets/coherent-motion-v11'
def iris(im):
 hsv=cv2.cvtColor(im[:,:,:3],cv2.COLOR_BGR2HSV)
 mask=((hsv[:,:,0]>20)&(hsv[:,:,0]<80)&(hsv[:,:,1]>70)&(im[:,:,3]>180)).astype('uint8')
 n,lab,stats,centers=cv2.connectedComponentsWithStats(mask)
 return centers[max((i for i in range(1,n) if 5<stats[i,4]<700),key=lambda i:stats[i,4])]
def descriptors(im,eye):
 x,y=map(int,eye);mask=np.zeros(im.shape[:2],np.uint8);mask[max(0,y-90):y+30,max(0,x-65):x+125]=255;mask[im[:,:,3]<180]=0
 return cv2.SIFT_create(nfeatures=2000,contrastThreshold=.01,edgeThreshold=15).detectAndCompute(cv2.cvtColor(im[:,:,:3],cv2.COLOR_BGR2GRAY),mask)
with ZipFile(root/'build/drawn-motion-v9/candidate.zip') as z:
 ref=cv2.imdecode(np.frombuffer(z.read('action-sit-000.png'),np.uint8),-1)
 kp0,d0=descriptors(ref,iris(ref))
result={}
for path in folder.glob('cells/*.png'):
 im=cv2.imread(str(path),-1);eye=iris(im);kp,d=descriptors(im,eye)
 matches=[a for a,b in cv2.BFMatcher().knnMatch(d0,d,k=2) if a.distance<.72*b.distance]
 info=dict(eye=eye.tolist(),matches=len(matches))
 if len(matches)>=4:
  mat,inliers=cv2.estimateAffinePartial2D(np.float32([kp0[x.queryIdx].pt for x in matches]),np.float32([kp[x.trainIdx].pt for x in matches]),method=cv2.RANSAC,ransacReprojThreshold=2,maxIters=10000)
  if mat is not None:info.update(inliers=int(inliers.sum()),nativeSkullScale=float(np.hypot(mat[0,0],mat[1,0])))
 alpha=im[:,:,3]>128;ys,xs=np.where(alpha)
 # Recorded as diagnostics, NOT used as a body size estimate.
 info['bounds']=[int(xs.min()),int(ys.min()),int(xs.max()+1),int(ys.max()+1)]
 info['contactSamples']={str(x):int(np.where(alpha[:,x])[0][-1]) if alpha[:,x].any() else None for x in range(180,301,10)}
 result[path.stem]=info
(folder/'measurements.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result))
