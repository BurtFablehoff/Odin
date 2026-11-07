"""Create diagnostic GIFs from actual C# registered-layer renderer snapshots."""
from pathlib import Path
import json
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2]
frames_dir=ROOT/'local-data/reference-review/registered-variants-v7'
out=Path('C:/Users/hattd/Documents/Codex/2026-11-06/d/outputs')
out.mkdir(parents=True,exist_ok=True)
poses={p['id']:p for p in json.loads((ROOT/'planning/state-head-review.json').read_text())['poses']}
font=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',28)
for kind,ids,phases in [('gaze',[1,2,3],['left','right']),('tail',[1,3,4],['out','in'])]:
 sequence=[(None,0,1000)]
 for phase in phases:
  sequence.extend((phase,step,650 if step==4 else 85) for step in [1,2,3,4])
  sequence.extend((phase,step,85) for step in [3,2,1])
  sequence.append((None,0,700))
 images=[]
 for phase,step,duration in sequence:
  board=Image.new('RGB',(1800,560),'#25313e');draw=ImageDraw.Draw(board)
  for col,pose_id in enumerate(ids):
   pose=poses[pose_id];source=frames_dir/(f'{kind}-{pose_id}-{phase}-{step}.png' if phase else f'base-{pose_id}.png')
   image=Image.open(source).convert('RGBA');base=Image.open(frames_dir/f'base-{pose_id}.png').convert('RGBA')
   bounds=base.getchannel('A').point(lambda a:255 if a>64 else 0).getbbox()
   scale=pose['scaleAtSmall']*1.45
   rendered=image.resize((round(image.width*scale),round(image.height*scale)),Image.Resampling.LANCZOS)
   x=round(col*600+300-(bounds[0]+bounds[2])/2*scale);y=round(470-pose['supportY']*scale)
   draw.rectangle((col*600,470,(col+1)*600,560),fill='#dfe5ee');draw.line((col*600,470,(col+1)*600,470),fill='#5bddd3',width=2)
   board.paste(rendered,(x,y),rendered)
   draw.text((col*600+18,15),pose['label']+' - registered '+kind,font=font,fill='white')
  images.append(board)
 # A fixed palette prevents unrelated fur/background pixels changing through GIF quantization.
 palette=images[0].quantize(colors=256)
 indexed=[image.quantize(palette=palette,dither=Image.Dither.NONE) for image in images]
 destination=out/f'Odin-registered-{kind}-v7.gif'
 indexed[0].save(destination,save_all=True,append_images=indexed[1:],duration=[s[2] for s in sequence],loop=0,optimize=False)
 print(destination)
