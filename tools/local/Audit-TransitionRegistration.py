"""Read-only sprite measurements; no artwork transforms or writes."""
from pathlib import Path
from io import BytesIO
import json,zipfile,argparse
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
parser=argparse.ArgumentParser();parser.add_argument('--pack',default='build/transitions-v2/candidate.zip');args=parser.parse_args()
with zipfile.ZipFile(ROOT/args.pack) as z:
 m=json.loads(z.read('manifest.json'))
 for clip in ['sit','loaf','lay','sleep','groom','sit_to_groom','sit_to_loaf','sit_to_lay','lay_to_sleep']:
  for e in [e for e in m['actions'] if e['clip']==clip and (clip!='groom' or e['frame'] in [0,7,15])]:
   a=np.array(Image.open(BytesIO(z.read(e['key']))).convert('RGBA'))
   mask=a[:,:,3]>128;ys,xs=np.where(mask);scale=e['renderWidthAtSmall']/a.shape[1]
   lower=[]
   for fraction in [.15,.25,.35,.45,.55,.65,.75,.85]:
    x=int(xs.min()+(xs.max()-xs.min())*fraction)
    yy=np.flatnonzero(mask[:,max(0,x-2):x+3].sum(axis=1)>=3)
    lower.append([fraction,int(yy[-1]) if len(yy) else None])
   eye_y={'sit':618,'loaf':745,'lay':728,'sleep':810,'groom':617}.get(clip)
   if clip=='sit_to_groom':eye_y=[618,252,268,282,617][e['frame']]
   elif clip=='sit_to_loaf':eye_y=[618,258,290,330,745][e['frame']]
   elif clip=='sit_to_lay':eye_y=[618,288,331,386,728][e['frame']]
   elif clip=='lay_to_sleep':eye_y=[728,350,395,445,810][e['frame']]
   row=np.flatnonzero(mask[eye_y])
   centers={'sit':524,'loaf':628,'lay':628,'sleep':660,'groom':455}
   center=centers.get(clip)
   if clip=='sit_to_groom':center=[524,450.5,458.5,450,455][e['frame']]
   if clip=='sit_to_loaf':center=[524,490.5,486,480,628][e['frame']]
   if clip=='sit_to_lay':center=[524,524,523.5,534.5,628][e['frame']]
   if clip=='lay_to_sleep':center=[628,522,520,523,660][e['frame']]
   apices=[]
   for lo,hi in [(int(center-150),int(center-15)),(int(center+15),int(center+150))]:
    crop=mask[:,lo:hi];runs=crop[:,:-2]&crop[:,1:-1]&crop[:,2:]
    yy,xx=np.where(runs)
    y=int(yy.min());x=float(np.median(xx[yy==y]))+lo+1;apices.append([x,y])
   ear_width=(apices[1][0]-apices[0][0])*scale
   band=mask[max(0,ys.max()-5):ys.max()+1].sum(0)
   sole=np.flatnonzero(band>=3);groups=np.split(sole,np.where(np.diff(sole)>1)[0]+1);groups=[g for g in groups if len(g)>5]
   foot=float(np.median(groups[-1])) if groups else None
   roi={'sit':(420,480),'loaf':(470,545),'lay':(450,520),'sleep':(490,570),'groom':(390,450)}.get(clip)
   contact=None
   if roi:
    values=[]
    for x in range(*roi):
     yy=np.flatnonzero(mask[:,x])
     if len(yy):values.append(int(yy[-1]))
    contact=float(np.median(values))
   print(json.dumps(dict(clip=clip,frame=e['frame'],scale=round(scale,4),earWidth=round(ear_width,2),footSoleX=foot,bodyContactMedian=contact,headWidthAtEyeRow=round((row[-1]-row[0]+1)*scale,2) if len(row) else None)))
