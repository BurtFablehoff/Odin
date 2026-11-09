"""Read-only QA of actual C# diagonal-head exports; creates comparison diagnostics."""
from pathlib import Path
import hashlib,json
from PIL import Image,ImageDraw,ImageFont,ImageChops
ROOT=Path(__file__).resolve().parents[2]
EXPORT=ROOT/'local-data/reference-review/diagonal-registered-v4'
OUT=ROOT/'local-data/reference-review/diagonal-registered-v4-qa';OUT.mkdir(parents=True,exist_ok=True)
DELIVER=Path('C:/Users/hattd/Documents/Codex/2026-11-06/d/outputs')
metadata=json.loads((ROOT/'planning/diagonal-gaze-reconciliation-v4.json').read_text())
regions={1:(460,590,850,920),2:(585,690,1020,1080),3:(585,685,1025,1105)}
ids={'sit':1,'loaf':2,'lay':3};phases=['up-right','down-right','down-left','up-left']
font=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',24)
small=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',20)
results=[]
for family,pose_id in ids.items():
 board=Image.new('RGBA',(3200,590),'#1c2631');d=ImageDraw.Draw(board)
 d.text((15,10),family+' - raw diagonal head vs actual registered renderer',font=font,fill='white')
 d.text((15,45),'Same coordinates and viewing scale. Head source at left; fixed-body registered frame at right.',font=small,fill='#b8cad9')
 base=Image.open(EXPORT/f'base-{pose_id}.png').convert('RGBA');left,top,right,bottom=regions[pose_id]
 for col,phase in enumerate(phases):
  record=next(r for r in metadata['frames'] if r['family']==family and r['phase']==phase)
  source=ROOT/record['source'];raw=Image.open(source).convert('RGBA');assert hashlib.sha256(source.read_bytes()).hexdigest()==record['sha256']
  registered_path=EXPORT/f'gaze-{pose_id}-{phase}-4.png';registered=Image.open(registered_path).convert('RGBA')
  assert registered.size==base.size==raw.size==(1254,1254)
  diff=ImageChops.difference(base,registered);changed=diff.getbbox(alpha_only=False)
  assert changed and changed[0]>=left and changed[1]>=top and changed[2]<=right and changed[3]<=bottom,(family,phase,changed)
  outside_parts=[(0,0,1254,top),(0,bottom,1254,1254),(0,top,left,bottom),(right,top,1254,bottom)]
  outside_equal=all(base.crop(b).tobytes()==registered.crop(b).tobytes() for b in outside_parts);assert outside_equal
  paws_equal=base.crop((0,1110,1254,1254)).tobytes()==registered.crop((0,1110,1254,1254)).tobytes();assert paws_equal
  a=registered.getchannel('A');bounds=a.point(lambda v:255 if v>8 else 0).getbbox();clear=bounds[0]>0 and bounds[1]>0 and bounds[2]<1254 and bounds[3]<1254;assert clear
  results.append({'family':family,'phase':phase,'rawSource':record['source'],'registeredSource':str(registered_path.relative_to(ROOT)).replace('\\','/'),'registeredSHA256':hashlib.sha256(registered_path.read_bytes()).hexdigest(),'nativeSize':[1254,1254],'changedBounds':list(changed),'allowedHeadRegion':list(regions[pose_id]),'outsideHeadRGBAIdentical':outside_equal,'frontPawsAndFloorRGBAIdentical':paws_equal,'registeredMarginsClear':clear})
  tile=Image.new('RGBA',(800,500),'#25313e');td=ImageDraw.Draw(tile);td.text((15,10),phase+' - raw / registered',font=font,fill='white')
  box=(left-40,top-25,right+40,bottom+70)
  for side,image in enumerate([raw,registered]):
   crop=image.crop(box);crop=crop.resize((round(crop.width*.70),round(crop.height*.70)),Image.Resampling.LANCZOS)
   tile.alpha_composite(crop,(side*400+(400-crop.width)//2,60+(420-crop.height)//2))
  board.alpha_composite(tile,(col*800,90))
 name=f'Odin-{family}-diagonal-registered-comparison-v4.png';board.convert('RGB').save(OUT/name);board.convert('RGB').save(DELIVER/name)
report={'passed':True,'headless':True,'windowsCreated':0,'framesChecked':len(results),'allOutsideHeadRGBAIdentical':True,'allFrontPawsAndFloorRGBAIdentical':True,'allRegisteredMarginsClear':True,'sourcePixelsNotModified':True,'manualSeamInspectionPending':True,'frames':results}
(ROOT/'build/qa-diagonal-v4-independent.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k!='frames'}))
