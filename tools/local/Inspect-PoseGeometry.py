"""Inspect actual displayed heads and facial landmarks; never changes source art."""
import json
import sys
from pathlib import Path
from zipfile import ZipFile
import cv2
import numpy as np
from PIL import Image, ImageDraw

root=Path(__file__).resolve().parents[2]
version=sys.argv[1] if len(sys.argv)>1 else 'drawn-motion-v9'
work=root/'build'/version
rows={}
sheet=Image.new('RGB',(7*240,3*275),(27,29,34))
draw=ImageDraw.Draw(sheet)
with ZipFile(work/'candidate.zip') as archive:
    manifest=json.loads(archive.read('manifest.json'))
    for row,clip in enumerate(('sit_to_loaf','sit_to_lay','loaf_to_lay')):
        entries=sorted((e for e in manifest['actions'] if e['clip']==clip),key=lambda e:e['frame'])
        report=[]
        for i,e in enumerate(entries):
            im=cv2.imdecode(np.frombuffer(archive.read(e['key']),np.uint8),cv2.IMREAD_UNCHANGED)
            hsv=cv2.cvtColor(im[...,:3],cv2.COLOR_BGR2HSV)
            m=((hsv[...,0]>20)&(hsv[...,0]<80)&(hsv[...,1]>70)&(im[...,3]>200)).astype(np.uint8)
            m[:500]=0;m[920:]=0;m[:,:400]=0;m[:,760:]=0
            n,labels,stats,centers=cv2.connectedComponentsWithStats(m)
            eye=centers[max((j for j in range(1,n) if 15<stats[j,4]<700),key=lambda j:stats[j,4])]
            x,y=eye
            gray=cv2.cvtColor(im[...,:3],cv2.COLOR_BGR2GRAY)
            dark=((gray<95)&(im[...,3]>200)).astype(np.uint8)
            roi=np.zeros_like(dark);roi[max(0,int(y-16)):int(y+17),int(x+30):int(x+100)]=1
            n,labels,stats,centers=cv2.connectedComponentsWithStats(dark*roi)
            candidates=[j for j in range(1,n) if 8<stats[j,4]<700]
            socket=centers[min(candidates,key=lambda j:abs(centers[j,1]-y)+abs(centers[j,0]-x-60)*.4)] if candidates else None
            scale=e['renderWidthAtSmall']/1024
            span=float(np.linalg.norm(socket-eye)*scale) if socket is not None else None
            report.append(dict(frame=i,key=e['key'],scale=scale,eye=eye.tolist(),socket=None if socket is None else socket.tolist(),displayEyeSpan=span,displayEye=[(x-e['anchorX'])*scale,(y-e['supportY'])*scale]))
            rendered=Image.open(work/'review'/(clip+'-dark')/(str(i).zfill(2)+'.png')).convert('RGB')
            rx=round(260+(x-e['anchorX'])*scale);ry=round(280+(y-e['supportY'])*scale)
            crop=rendered.crop((rx-35,ry-55,rx+85,ry+65)).resize((240,240))
            sheet.paste(crop,(i*240,row*275));draw.text((i*240+4,row*275+242),f'{clip}/{i} span={span:.1f}' if span else f'{clip}/{i}',fill='white')
        rows[clip]=report
sheet.save(work/'head-geometry.png')
(work/'head-geometry.json').write_text(json.dumps(rows,indent=2))
print(json.dumps({c:[round(e['displayEyeSpan'],2) if e['displayEyeSpan'] else None for e in r] for c,r in rows.items()}))
