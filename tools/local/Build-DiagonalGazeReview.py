"""Diagnostic head/body sheets for actual diagonal head candidates; no asset edits."""
from pathlib import Path
import hashlib,json
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2]
metadata=json.loads((ROOT/'planning/diagonal-gaze-reconciliation-v4.json').read_text())
assert metadata['generationComplete'] and len(metadata['frames'])==12
poses={p['id']:p for p in json.loads((ROOT/'planning/state-head-review.json').read_text())['poses']}
out=Path('C:/Users/hattd/Documents/Codex/2026-11-06/d/outputs');out.mkdir(parents=True,exist_ok=True)
local=ROOT/'local-data/reference-review/diagonal-gaze-v4';local.mkdir(parents=True,exist_ok=True)
families=['sit','loaf','lay'];phases=['up-right','down-right','down-left','up-left']
ids={'sit':1,'loaf':2,'lay':3}
regions={'sit':(460,590,850,920),'loaf':(585,690,1020,1080),'lay':(585,685,1025,1105)}
font=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',27)
small=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',22)
heads=Image.new('RGBA',(2240,1600),'#1c2631');bodies=Image.new('RGBA',(2400,1450),'#1c2631')
for board,title in [(heads,'Odin - actual diagonal gaze heads'),(bodies,'Odin - diagonal candidates and floor contact')]:
 d=ImageDraw.Draw(board);d.text((20,12),title,font=font,fill='white');d.text((20,54),'12 independently rendered candidate faces; no head crossfades. Body locking is applied separately.',font=small,fill='#b6c8d7')
for row,family in enumerate(families):
 pose=poses[ids[family]];base=Image.open(ROOT/pose['source']).convert('RGBA')
 basebounds=base.getchannel('A').point(lambda a:255 if a>64 else 0).getbbox()
 for col,phase in enumerate(phases):
  record=next(r for r in metadata['frames'] if r['family']==family and r['phase']==phase)
  path=ROOT/record['source'];assert hashlib.sha256(path.read_bytes()).hexdigest()==record['sha256']
  image=Image.open(path).convert('RGBA');assert image.size==(1254,1254)
  head=Image.new('RGBA',(560,500),'#25313e');d=ImageDraw.Draw(head);d.text((15,12),f"{family} - {phase} ({record['angleDegrees']} deg)",font=font,fill='white')
  crop=image.crop(regions[family]);scale=pose['scaleAtSmall']*2.5
  rendered=crop.resize((round(crop.width*scale),round(crop.height*scale)),Image.Resampling.LANCZOS)
  assert rendered.width<540 and rendered.height<435
  head.alpha_composite(rendered,((560-rendered.width)//2,60+(440-rendered.height)//2))
  heads.alpha_composite(head,(col*560,100+row*500))
  tile=Image.new('RGBA',(600,450),'#25313e');d=ImageDraw.Draw(tile);d.text((15,12),family+' - '+phase,font=font,fill='white')
  floor=385;d.rectangle((0,floor,600,450),fill='#dfe5ee');d.line((0,floor,600,floor),fill='#5bddd3',width=2)
  scale=pose['scaleAtSmall']*1.2;rendered=image.resize((round(image.width*scale),round(image.height*scale)),Image.Resampling.LANCZOS)
  x=round(300-(basebounds[0]+basebounds[2])/2*scale);y=round(floor-pose['supportY']*scale)
  bounds=image.getchannel('A').point(lambda a:255 if a>8 else 0).getbbox()
  assert x+bounds[0]*scale>0 and x+bounds[2]*scale<600 and y+bounds[1]*scale>60 and y+bounds[3]*scale<450
  tile.alpha_composite(rendered,(x,y));bodies.alpha_composite(tile,(col*600,100+row*450))
for image,name in [(heads,'Odin-diagonal-gaze-heads-v4.png'),(bodies,'Odin-diagonal-gaze-stills-v4.png')]:
 image.convert('RGB').save(out/name);image.convert('RGB').save(local/name)
 print(out/name)
print('All12 source hashes, native sizes, margins and preview bounds verified. No source pixels modified.')
