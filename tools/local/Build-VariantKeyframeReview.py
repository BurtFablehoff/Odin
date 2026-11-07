"""Copy imagegen keyframes and make diagnostic sheets without editing asset pixels."""
from pathlib import Path
import hashlib,json,re,shutil,sys
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2]
kind=sys.argv[1]
assert kind in ('gaze','tail')
jobs=json.loads((ROOT/f'planning/{kind}-generation-v1.json').read_text())['jobs']
folder=ROOT/f'draft-assets/{kind}-reconciliation-v1';folder.mkdir(parents=True,exist_ok=True)
out=Path('C:/Users/hattd/Documents/Codex/2026-11-06/d/outputs');out.mkdir(parents=True,exist_ok=True)
config=json.loads((ROOT/'planning/state-head-review.json').read_text())
families=['sit','loaf','lay'] if kind=='gaze' else ['sit','lay','sleep']
ids={'sit':1,'loaf':2,'lay':3,'sleep':4}
font=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',27)
small=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',23)
board=Image.new('RGBA',(1800,1460),'#1c2631');d=ImageDraw.Draw(board)
d.text((20,15),f'Odin - {kind} keyframes',font=font,fill='white')
d.text((20,59),'Latest base at left; new candidates at centre/right. Cyan = support plane.',font=small,fill='#b6c8d7')
records=[]
for row,family in enumerate(families):
 pose=next(p for p in config['poses'] if p['id']==ids[family])
 selected=[j for j in jobs if j['family']==family]
 sources=[ROOT/pose['source']]
 for j in selected:
  phase=j.get('direction',j.get('phase'));source=Path(re.search(r'as (C:.*?\.png) by default',j['output_hint']).group(1))
  target=folder/f'{family}-{phase}.png';shutil.copy2(source,target)
  im=Image.open(target).convert('RGBA');bounds=im.getchannel('A').point(lambda a:255 if a>8 else 0).getbbox()
  if im.size!=(1254,1254):raise ValueError('Generator changed canvas dimensions: '+str(target))
  clear=bounds[0]>0 and bounds[1]>0 and bounds[2]<im.width and bounds[3]<im.height
  records.append({'id':('G' if kind=='gaze' else 'T')+str(len(records)+1),'family':family,'phase':phase,'source':str(target.relative_to(ROOT)).replace('\\','/'),'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'supportY':pose['supportY'],'scaleAtSmall':pose['scaleAtSmall'],'boundsAlpha8':bounds,'marginsClear':clear,'approved':False,'runtimeIntegrated':False,'status':'candidate; artwork and exact registration pending'})
  sources.append(target)
 base=Image.open(sources[0]).convert('RGBA');bounds=base.getchannel('A').point(lambda a:255 if a>64 else 0).getbbox()
 for col,source in enumerate(sources):
  tile=Image.new('RGBA',(600,450),'#25313e');td=ImageDraw.Draw(tile)
  label='Latest base' if col==0 else records[-2+col-1]['id']+' '+selected[col-1].get('direction',selected[col-1].get('phase'))
  td.text((15,14),family+' - '+label,font=font,fill='white')
  floor=385;td.rectangle((0,floor,600,450),fill='#dfe5ee');td.line((0,floor,600,floor),fill='#5bddd3',width=2)
  im=Image.open(source).convert('RGBA');scale=pose['scaleAtSmall']*1.2
  rendered=im.resize((round(im.width*scale),round(im.height*scale)),Image.Resampling.LANCZOS)
  x=round(300-(bounds[0]+bounds[2])/2*scale);y=round(floor-pose['supportY']*scale)
  b=im.getchannel('A').point(lambda a:255 if a>8 else 0).getbbox()
  assert x+b[0]*scale>=0 and x+b[2]*scale<=600 and y+b[1]*scale>=65 and y+b[3]*scale<450,(family,col)
  tile.alpha_composite(rendered,(x,y));board.alpha_composite(tile,(col*600,110+row*450))
destination=out/f'Odin-{kind}-keyframes-v1.png';board.convert('RGB').save(destination)
report={'reviewOnly':True,'kind':kind,'keyframeCount':len(records),'frames':records,'originalAssetsPreserved':True}
(ROOT/f'planning/{kind}-reconciliation-v1.json').write_text(json.dumps(report,indent=2)+'\n')
shutil.copy2(ROOT/f'planning/{kind}-generation-v1.json',out/f'Odin-{kind}-edit-prompts.json')
print(destination)
print('Keyframes:',len(records),'with clear margins:',sum(r['marginsClear'] for r in records))
