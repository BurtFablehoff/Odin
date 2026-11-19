"""Measure candidate artwork only: no image creation, interpolation or warping."""
import cv2
import json
import sys
import numpy as np
from pathlib import Path

folder = Path(sys.argv[1])
rows = {}
for path in sorted(folder.glob('*.png')):
    im = cv2.imread(str(path), cv2.IMREAD_UNCHANGED)
    scale = 1024/im.shape[1]
    hsv = cv2.cvtColor(im[..., :3], cv2.COLOR_BGR2HSV)
    mask = ((hsv[..., 0]>20)&(hsv[..., 0]<80)&(hsv[..., 1]>70)&(im[..., 3]>200)).astype(np.uint8)
    mask[:int(500/scale)]=0;mask[int(920/scale):]=0;mask[:,:int(400/scale)]=0;mask[:,int(760/scale):]=0
    n,labels,stats,centers=cv2.connectedComponentsWithStats(mask)
    candidates=[i for i in range(1,n) if 15/scale**2<stats[i,4]<700/scale**2]
    if not candidates:raise RuntimeError('Iris missing: '+path.name)
    iris=max(candidates,key=lambda i:stats[i,4]);eye=centers[iris]*scale
    lows=[]
    for x in range(int(430/scale),int(480/scale)):
        ys=np.flatnonzero(im[:,x,3]>128)
        if len(ys):lows.append(ys[-1]*scale)
    support=float(np.median(lows)+1)
    rows[path.stem]=dict(eye=eye.tolist(),supportY=support,worldEyeY=float(eye[1]+944-support),irisArea=float(stats[iris,4]*scale**2))
print(json.dumps(rows))
