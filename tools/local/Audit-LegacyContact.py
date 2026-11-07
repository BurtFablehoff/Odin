"""Audit unchanged legacy resting sprites against taskbar and window edges."""
from pathlib import Path
import json, hashlib
import numpy as np
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'local-data/reference-review/legacy-contact-audit-v1'
OUT.mkdir(parents=True,exist_ok=True)
FONT=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',17)
BOLD=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',22)

def load(path): return Image.open(ROOT/path).convert('RGBA')
def bounds(im): return im.getchannel('A').point(lambda a:255 if a>64 else 0).getbbox()
def panel(pose,context,width=580,height=335):
    tile=Image.new('RGBA',(width,height),'#222a34'); d=ImageDraw.Draw(tile)
    d.text((14,8),pose['label']+' / '+context,font=BOLD,fill='white')
    d.text((14,36),pose['note'],font=FONT,fill='#b9c8d6')
    floor=285
    fill='#151820' if context=='Taskbar' else '#e0e5ed'
    d.rectangle((0,floor,width,height),fill=fill)
    d.line((0,floor,width,floor),fill='#52d8d0',width=2)
    if context=='Window top':
        for dx in (width-88,width-54,width-20): d.rectangle((dx-3,floor+15,dx+3,floor+16),fill='#647080')
    else:
        for x in range(18,width-20,34): d.rounded_rectangle((x,floor+17,x+15,floor+32),radius=3,fill='#536174')
    im=load(pose['source']); b=bounds(im); scale=pose['scale']
    x=round(width/2-(b[0]+b[2])/2*scale); y=round(floor-pose['supportY']*scale)
    rendered=im.resize((round(im.width*scale),round(im.height*scale)),Image.Resampling.LANCZOS)
    tile.alpha_composite(rendered,(x,y))
    return tile

def board(poses,title,name):
    width,height=580,335
    canvas=Image.new('RGBA',(width*2,height*len(poses)+80),'#10161e'); d=ImageDraw.Draw(canvas)
    d.text((15,8),title,font=BOLD,fill='white')
    d.text((15,42),'Unchanged sprites; shadows omitted. Cyan = support edge. Below-edge pixels need visual review.',font=FONT,fill='#b9c8d6')
    for row,p in enumerate(poses):
        for col,context in enumerate(('Taskbar','Window top')):
            canvas.alpha_composite(panel(p,context),(col*width,80+row*height))
    canvas.convert('RGB').save(OUT/name)

manifest=json.loads((ROOT/'art/manifest.json').read_text())
families={'sit','loaf','lay','sleep'}
def family(name):
    if name.startswith('look_'):return name[5:]
    for suffix in ('_eyes','_tail'):
        if name.endswith(suffix):return name[:-len(suffix)]
    return name
records=[]
for name,clip in manifest['clips'].items():
    if family(name) not in families:continue
    for frame,info in enumerate(clip['images']):
        source=f'art/frames/{name}/{frame:03}.png'; im=load(source)
        anchor=info.get('anchor_y') or clip.get('anchor_y') or manifest['foot_anchor'][1]
        a=np.asarray(im.getchannel('A')); yy,xx=np.where(a>64)
        factor=.9 if family(name)=='loaf' else 1
        below=(yy>anchor)
        records.append({'clip':name,'frame':frame,'source':source,'anchorY':anchor,
          'opaquePixelsBelowSupport':int(below.sum()),'maxOpaqueDepthSourcePx':int(max(0,int(yy.max())-anchor)),
          'depthAtDisplaySizes':{str(size):round(max(0,int(yy.max())-anchor)*round(size*factor)/1024,2) for size in (480,640,800)},
          'sourceSHA256':hashlib.sha256((ROOT/source).read_bytes()).hexdigest(),
          'needsVisualClassification':True})
poses=[]
for name,label in [('sit','Current sitting'),('loaf','Current loaf'),('lay','Current awake lying'),('sleep','Current sleeping')]:
    rec=next(x for x in records if x['clip']==name and x['frame']==0)
    poses.append({'label':label,'note':f"Actual runtime anchor {rec['anchorY']}; Small (480)",
      'source':rec['source'],'supportY':rec['anchorY'],'scale':round(480*(.9 if name=='loaf' else 1))/1024})
board(poses,'Existing app artwork - actual anchor placement','runtime-contact.png')
reg=json.loads((ROOT/'planning/rest-pose-registration.json').read_text())
paths={'sit-rear.png':'draft-assets/approved-sitting-v1/rear.png','sit-lookback.png':'draft-assets/approved-sitting-v1/lookback.png','sit-side.png':'draft-assets/approved-sitting-v1/side.png','belly-flat.png':'draft-assets/approved-belly-v1/side.png'}
old=[]
for p in reg['poses']:
    if p['key'] not in paths:continue
    im=load(paths[p['key']]); b=bounds(im)
    scale=.46875*255/(p.get('scaleReference') or p['headWidth'])
    old.append({'label':p['label'],'note':'Not in runtime; provisional bottom alignment only',
                'source':paths[p['key']],'supportY':b[3]-1,'scale':scale})
board(old,'Older approved poses - support anchors still to establish','approved-legacy-contact.png')
(OUT/'audit.json').write_text(json.dumps({'artworkModified':False,'runtimeFramesChecked':len(records),
 'classification':'Geometric diagnostic only. Paws are permitted below support; requires visual review.',
 'frames':records,'unintegratedPoses':old},indent=2)+'\n')
print(f'Audited {len(records)} runtime frames; rendered current and older approved contact boards: {OUT}')
