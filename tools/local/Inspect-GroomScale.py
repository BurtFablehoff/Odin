"""Inspect anatomical feature scale and actual display rendering; no artwork edits."""
from pathlib import Path
from zipfile import ZipFile
import json,cv2,numpy as np
from PIL import Image,ImageDraw
from io import BytesIO
root=Path(__file__).resolve().parents[2];work=root/'build/groom-scale-v2';work.mkdir(exist_ok=True)
def eye(im):
 hsv=cv2.cvtColor(im[:,:,:3],cv2.COLOR_BGR2HSV);mask=((hsv[:,:,0]>20)&(hsv[:,:,0]<80)&(hsv[:,:,1]>70)&(im[:,:,3]>200)).astype('uint8');mask[:450]=0;mask[900:]=0;mask[:,:250]=0;mask[:,750:]=0
 n,_,s,c=cv2.connectedComponentsWithStats(mask);valid=[j for j in range(1,n) if 10<s[j,4]<800]
 # Closed-eye licking frames have no green iris. This fallback locates only the
 # diagnostic ROI; scale is independently fitted from forehead/pinna features.
 return c[max(valid,key=lambda j:s[j,4])] if valid else np.array([425.,635.])
def features(im,region):
 x,y=map(int,eye(im));mask=np.zeros(im.shape[:2],np.uint8)
 if region=='forehead':mask[y-85:y+15,x-65:x+120]=255
 else:mask[y-130:y-30,x-90:x+150]=255
 mask[im[:,:,3]<200]=0
 return cv2.SIFT_create(nfeatures=2000,contrastThreshold=.01,edgeThreshold=15).detectAndCompute(cv2.cvtColor(im[:,:,:3],cv2.COLOR_BGR2GRAY),mask)
def match(a,b,region):
 ka,da=features(a,region);kb,db=features(b,region);pairs=[x for x,y in cv2.BFMatcher().knnMatch(da,db,k=2) if x.distance<.72*y.distance]
 if len(pairs)<4:return dict(inliers=0,matches=len(pairs))
 mat,inside=cv2.estimateAffinePartial2D(np.float32([ka[p.queryIdx].pt for p in pairs]),np.float32([kb[p.trainIdx].pt for p in pairs]),method=cv2.RANSAC,ransacReprojThreshold=1.5,maxIters=10000)
 if mat is None:return dict(inliers=0)
 count=int(inside.sum())
 if count<12:return dict(inliers=count,usable=False,reason='Insufficient matching anatomical texture; do not calibrate from this fit.')
 return dict(inliers=count,usable=True,nativeScale=float(np.hypot(mat[0,0],mat[1,0])),rotation=float(np.degrees(np.arctan2(mat[1,0],mat[0,0]))))
rows=[];sheet=Image.new('RGB',(560,520),'#292d35');full=Image.new('RGB',(1040,920),'#292d35')
with ZipFile(root/'build/groom-return-v1/candidate.zip') as z:
 m=json.loads(z.read('manifest.json'));entries=sorted((e for e in m['actions'] if e['clip']=='groom'),key=lambda e:e['frame']);images=[cv2.imdecode(np.frombuffer(z.read(e['key']),np.uint8),-1) for e in entries]
 for i,(e,im) in enumerate(zip(entries,images)):
  row=dict(frame=i,key=e['key'],renderWidth=e['renderWidthAtSmall'])
  for region in ['forehead','pinna']:
   row[region]=match(images[0],im,region)
   if i:row['previous_'+region]=match(images[i-1],im,region)
   if i>=8:row['repeat_'+region]=match(images[i-8],im,region)
  source=Image.open(BytesIO(z.read(e['key']))).convert('RGBA');scale=e['renderWidthAtSmall']/source.width
  sprite=source.transform((260,230),Image.Transform.AFFINE,(1/scale,0,e['anchorX']-130/scale,0,1/scale,e['supportY']-220/scale),Image.Resampling.BICUBIC)
  canvas=Image.new('RGBA',sprite.size,'#292d35');canvas.alpha_composite(sprite);d=ImageDraw.Draw(canvas);d.text((6,5),str(i),fill='white');d.line((0,220,260,220),fill='#777777');full.paste(canvas.convert('RGB'),((i%4)*260,(i//4)*230))
  x,y=eye(im);sx=130+(x-e['anchorX'])*scale;sy=220+(y-e['supportY'])*scale
  crop=canvas.crop((round(sx-60),round(sy-65),round(sx+80),round(sy+65))).convert('RGB');ImageDraw.Draw(crop).text((4,4),str(i),fill='white');sheet.paste(crop,((i%4)*140,(i//4)*130));rows.append(row)
sheet.save(work/'heads-v1.png');full.save(work/'frames-v1.png');(work/'scale-audit-v1.json').write_text(json.dumps(rows,indent=2)+'\n')
print(json.dumps([dict(frame=r['frame'],forehead=r.get('repeat_forehead'),pinna=r.get('repeat_pinna')) for r in rows[8:]]))
