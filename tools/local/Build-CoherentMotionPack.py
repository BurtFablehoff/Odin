"""A21/A22: discrete drawing selection and metadata registration, no image synthesis."""
import copy,hashlib,json
from pathlib import Path
from zipfile import ZipFile,ZIP_DEFLATED
import cv2,numpy as np
root=Path(__file__).resolve().parents[2];folder=root/'draft-assets/coherent-motion-v11';work=root/'build/coherent-motion-v11';work.mkdir(exist_ok=True)
measure=json.loads((folder/'measurements.json').read_text());skulls=json.loads((root/'build/geometry-motion-v10/skull-similarity.json').read_text())
with ZipFile(root/'build/drawn-motion-v9/candidate.zip') as z:payloads={n:z.read(n) for n in z.namelist()}
manifest=json.loads(payloads['manifest.json'])
def old(clip,frame):return copy.deepcopy(next(e for e in manifest['actions'] if e['clip']==clip and e['frame']==frame))
def cell(name):
 p=folder/f'cells/{name}.png';data=p.read_bytes();im=cv2.imdecode(np.frombuffer(data,np.uint8),-1);m=measure[name]
 key=f'coherent-v11/{name}.png';payloads[key]=data;left,top,right,bottom=m['bounds'];x=int(m['eye'][0])
 # Rear underside, away from front toes. Explicit recorded anatomical review region.
 cols=[x-115,x-85];bottoms=[np.where(im[:,i,3]>128)[0][-1] for i in range(*cols) if (im[:,i,3]>128).any()]
 return dict(key=key,label=name,sha256=hashlib.sha256(data).hexdigest(),id=0,clip='',frame=0,kind='transition',source=str(p.relative_to(root)).replace('\\','/'),nativeWidth=im.shape[1],nativeHeight=im.shape[0],bounds=[left,top,right-left,bottom-top],anchorX=0,supportY=float(np.median(bottoms))+1,renderWidthAtSmall=480,customAnchor=True,airborne=False,contactColumns=cols,skullNative=m['nativeSkullScale'],skullInliers=m['inliers'])
def prior_geometry(name):
 with ZipFile(root/'build/geometry-motion-v10/candidate.zip') as z:
  m=json.loads(z.read('manifest.json'));e=copy.deepcopy(next(e for e in m['actions'] if e['key']==f'geometry-v10/{name}.png'))
  payloads[e['key']]=z.read(e['key'])
 return e
specs=[('sit_to_loaf',[old('sit_to_loaf',0)]+[cell(f'sit-loaf-sheet-b-{i}') for i in range(1,6)]+[old('sit_to_loaf',6)]),
 ('sit_to_lay',[old('sit_to_lay',0)]+[cell(f'sit-lay-sheet-{i}') for i in range(1,6)]+[old('sit_to_lay',6)]),
 ('loaf_to_lay',[old('loaf_to_lay',0)]+[cell(f'loaf-lay-sheet-{i}') for i in range(1,6)]+[old('loaf_to_lay',4)])]
changed={c for c,keys in specs}|{'loaf_to_sit','lay_to_sit','lay_to_loaf'};actions=[e for e in manifest['actions'] if e['clip'] not in changed];clips=[c for c in manifest['actionClips'] if c['clip'] not in changed];plans=[]
for clip,keys in specs:
 s0=skulls[keys[0]['key']]['physicalScale'];s1=skulls[keys[-1]['key']]['physicalScale']
 for i,e in enumerate(keys[1:-1],1):
  target=s0+(s1-s0)*i/(len(keys)-1)
  if 'skullNative' in e:native=e['skullNative'];inliers=e['skullInliers']
  else:
   k=Path(e['key']).stem if e['key'].startswith('geometry-v10/') else e['key'];native=skulls[k]['physicalScale']/(480/480);inliers=skulls[k]['inliers']
  assert inliers>=12,(clip,i,'Insufficient anatomical feature matches')
  # Native source reference is 1024 with 480 display width. Whole drawing scale only.
  scale=.46875*target/native;e['renderWidthAtSmall']=e['nativeWidth']*scale
  e['anchorX']=e['bounds'][0]+126.09375/scale
  e['measuredSkullScale']=native*scale/.46875;e['targetSkullScale']=target
 delays=[.02]+[.055]*(len(keys)-2)+[.02]
 for reverse in (False,True):
  name='_to_'.join(clip.split('_to_')[::-1]) if reverse else clip
  ordered=keys[::-1] if reverse else keys;timing=delays[::-1] if reverse else delays
  for i,item in enumerate(ordered):
   e=copy.deepcopy(item);e.update(clip=name,frame=i,id=0);actions.append(e)
  clips.append(dict(clip=name,frames=len(keys),seconds=sum(timing),frameSeconds=timing,loop=False));plans.append(dict(clip=name,keys=ordered,frameSeconds=timing))
manifest.update(actions=actions,actionClips=clips,coherentRestV11=True)
payloads['manifest.json']=json.dumps(manifest,separators=(',',':')).encode()
with ZipFile(work/'candidate.zip','w',ZIP_DEFLATED) as z:
 for name,data in sorted(payloads.items()):z.writestr(name,data)
(work/'playback.json').write_text(json.dumps(plans,indent=2))
print('Reproduced FAILED V11 internal experiment. Diagnostic pack only; no EXE handoff.')
