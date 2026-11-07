from pathlib import Path
from PIL import Image,ImageDraw
import json
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'local-data/reference-review/style-consistency-v7'
OUT.mkdir(parents=True,exist_ok=True)
overrides={(f['clip'],f['frame']):f for f in json.loads((ROOT/'planning/jump-anatomy-integration-v5.json').read_text())['frames']}
def tile(path,size):
 im=Image.open(path).convert('RGBA'); b=im.getchannel('A').point(lambda x:255 if x>8 else 0).getbbox();im=im.crop(b);im.thumbnail(size,Image.Resampling.LANCZOS);return im
for clip in ['jump_left','jump_right']:
 sheet=Image.new('RGB',(1600,1700),(224,224,224));d=ImageDraw.Draw(sheet)
 for i in range(16):
  x=(i%4)*400;y=(i//4)*420
  for j,kind in enumerate(['original','current']):
   p=ROOT/f'art/frames/{clip}/{i:03}.png' if j==0 else ROOT/overrides.get((clip,i),{}).get('source',f'draft-assets/jump-family-v4/{clip}/{i:03}.png')
   im=tile(p,(190,355));sheet.paste(im,(x+j*200+(190-im.width)//2,y+45+(355-im.height)//2),im)
   d.text((x+j*200+7,y+12),f'{i:03} {kind}',fill=(0,0,0))
 sheet.save(OUT/f'{clip}-original-current.png')
refs=['art/frames/sit/000.png','art/frames/groom/000.png','art/frames/stretch/000.png']
sheet=Image.new('RGB',(1500,650),(224,224,224));d=ImageDraw.Draw(sheet)
for i,p in enumerate(refs):
 im=tile(ROOT/p,(480,580));sheet.paste(im,(i*500+(480-im.width)//2,45+(580-im.height)//2),im);d.text((i*500+10,10),p,fill=(0,0,0))
sheet.save(OUT/'ideal-originals-jump-audit.png')
print(OUT)
