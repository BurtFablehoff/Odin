"""Build review GIFs/filmstrips from actual headless CatMind capture PNGs.

This never computes a trajectory or changes source sprites. It only packages the
frames emitted by app/DevJumpPreview.cs, with one fixed GIF palette per direction.
"""
from pathlib import Path
import argparse,hashlib,json,math
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2]
parser=argparse.ArgumentParser()
parser.add_argument('captures',nargs='?',default='local-data/reference-review/jump-motion-v4')
parser.add_argument('--version',default='v4')
args=parser.parse_args()
assert args.version.isalnum(), 'Version must be alphanumeric.'
source=Path(args.captures)
if not source.is_absolute():source=ROOT/source
report=json.loads((source/'motion-report.json').read_text())
assert report['passed'] and report['headless'] and report['windowsCreated']==0 and report['realCatMindPhysics']
out=Path('C:/Users/hattd/Documents/Codex/2026-11-06/d/outputs');out.mkdir(parents=True,exist_ok=True)
font=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',25)
small=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',20)
results=[]
for direction in report['directions']:
 clip=direction['clip'];assert clip in ('jump_left','jump_right')
 assert direction['passed'] and direction['noOldArtworkFallback'] and direction['noCanvasClipping']
 assert direction['jumpFrameIndices']==list(range(16))
 assert direction['landedWindowId']==direction['expectedWindowId']
 captures=direction['captures'];images=[];hashes={}
 for record in captures:
  path=source/record['file'];data=path.read_bytes();hashes[path]=hashlib.sha256(data).hexdigest()
  image=Image.open(path).convert('RGB');assert list(image.size)==report['canvas']
  images.append(image)
 # Build the common palette from actual cat regions across launch, flight and landing.
 samples=[]
 for index in sorted(set([0,len(images)//4,len(images)//2,3*len(images)//4,len(images)-1])):
  x,y,w,h=captures[index]['visibleBounds'];box=(max(0,math.floor(x)-5),max(0,math.floor(y)-5),min(images[index].width,math.ceil(x+w)+5),min(images[index].height,math.ceil(y+h)+5))
  crop=images[index].crop(box);crop.thumbnail((256,256),Image.Resampling.LANCZOS);samples.append(crop)
 palette_source=Image.new('RGB',(256*len(samples),256),'#222a34')
 for index,image in enumerate(samples):palette_source.paste(image,(index*256,0))
 palette=palette_source.quantize(colors=256)
 indexed=[image.quantize(palette=palette,dither=Image.Dither.NONE) for image in images]
 fps=direction['framesPerSecond']
 # GIF supports centiseconds; distribute30/40ms durations to retain real30fps timing.
 durations=[round((i+1)*100/fps)*10-round(i*100/fps)*10 for i in range(len(indexed))]
 stem=clip.replace('_','-')
 gif=out/f'Odin-{stem}-motion-{args.version}.gif'
 indexed[0].save(gif,save_all=True,append_images=indexed[1:],duration=durations,loop=0,optimize=False)
 selected=[next((i for i,r in enumerate(captures) if r['clip']==clip and r['frame']==frame),None) for frame in range(16)]
 assert None not in selected
 tile_width,tile_height,header=384,335,100
 strip=Image.new('RGB',(tile_width*4,header+tile_height*4),'#1c2631');draw=ImageDraw.Draw(strip)
 draw.text((15,12),clip.replace('_',' ')+' - actual CatMind motion / all16latest frames',font=font,fill='white')
 draw.text((15,53),'Headless real behavior + native latest-art placement; cyan marks the floor and synthetic window top.',font=small,fill='#b8cad9')
 for frame,index in enumerate(selected):
  x=(frame%4)*tile_width;y=header+(frame//4)*tile_height
  draw.text((x+10,y+6),f'Frame {frame:02} | t={captures[index]["timeSeconds"]:.3f}s',font=small,fill='white')
  thumb=images[index].resize((368,288),Image.Resampling.LANCZOS)
  strip.paste(thumb,(x+8,y+36))
 filmstrip=out/f'Odin-{stem}-motion-filmstrip-{args.version}.png';strip.save(filmstrip)
 strip.save(source/(clip+'-filmstrip.png'))
 for path,digest in hashes.items():assert hashlib.sha256(path.read_bytes()).hexdigest()==digest
 results.append({'clip':clip,'gif':str(gif),'filmstrip':str(filmstrip),'captureCount':len(captures),'all16LatestJumpIndicesUsed':True,'realCatMindPhysics':True,'landingWindowVerified':True,'sourceCaptureHashesUnchanged':True,'fixedGifPalette':True,'framesPerSecond':fps})
 assert math.isclose(sum(durations)/1000,len(captures)/fps,abs_tol=.011)
summary={'passed':True,'headless':True,'physicsSource':'app/DevJumpPreview.cs / real CatMind.TryPerch and CatMind.Step','noSyntheticPythonTrajectory':True,'sourcePixelsModified':False,'directions':results}
(source/'preview-report.json').write_text(json.dumps(summary,indent=2)+'\n')
print(json.dumps(summary))
