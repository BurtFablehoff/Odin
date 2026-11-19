"""Measure iris/socket landmarks from normalized artwork, without editing it."""
import cv2
import json
import sys
from pathlib import Path
import numpy as np

folder=Path(sys.argv[1]);rows={}
for path in folder.glob('*.png'):
    im=cv2.imread(str(path),cv2.IMREAD_UNCHANGED)
    hsv=cv2.cvtColor(im[...,:3],cv2.COLOR_BGR2HSV)
    m=((hsv[...,0]>20)&(hsv[...,0]<80)&(hsv[...,1]>70)&(im[...,3]>200)).astype(np.uint8)
    m[:500]=0;m[920:]=0;m[:,:400]=0;m[:,760:]=0
    n,_,stats,centers=cv2.connectedComponentsWithStats(m)
    eye=centers[max((j for j in range(1,n) if 15<stats[j,4]<700),key=lambda j:stats[j,4])]
    x,y=eye;gray=cv2.cvtColor(im[...,:3],cv2.COLOR_BGR2GRAY);mask=((gray<95)&(im[...,3]>200)).astype(np.uint8)
    roi=np.zeros_like(mask);roi[max(0,int(y-16)):int(y+17),int(x+30):int(x+100)]=1
    n,_,stats,centers=cv2.connectedComponentsWithStats(mask*roi)
    candidates=[j for j in range(1,n) if 8<stats[j,4]<700]
    socket=centers[min(candidates,key=lambda j:abs(centers[j,1]-y)+abs(centers[j,0]-x-60)*.4)]
    rows[path.stem]=dict(eye=eye.tolist(),socket=socket.tolist(),spanNative=float(np.linalg.norm(socket-eye)))
(folder.parent/'features.json').write_text(json.dumps(rows,indent=2))
print(json.dumps(rows))
