"""Audit imagegen jump-left family; never change source or candidate PNG pixels."""
from pathlib import Path
import hashlib,json
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'draft-assets/jump-family-v4/jump_left'
REVIEW=ROOT/'local-data/reference-review/jump-left-family-v4';REVIEW.mkdir(parents=True,exist_ok=True)
config=json.loads((ROOT/'planning/state-head-review.json').read_text())
reference=next(p for p in config['poses'] if p['id']==16)
def bounds(image,threshold):return image.getchannel('A').point(lambda a:255 if a>threshold else 0).getbbox()
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
old_ref=Image.open(ROOT/'art/frames/jump_left/008.png').convert('RGBA');old_ref_box=bounds(old_ref,64)
new_ref=Image.open(ROOT/reference['source']).convert('RGBA');new_ref_box=bounds(new_ref,64)
legacy_scale=reference['scaleAtSmall']*(new_ref_box[2]-new_ref_box[0])/(old_ref_box[2]-old_ref_box[0])
manifest_data=json.loads((ROOT/'art/manifest.json').read_text())
manifest=manifest_data['clips']['jump_left'];original_anchor_x=manifest_data['foot_anchor'][0]
records=[]
for frame in range(16):
    original=ROOT/f'art/frames/jump_left/{frame:03}.png';source=OUT/f'{frame:03}.png'
    old=Image.open(original).convert('RGBA');old_box=bounds(old,64)
    record={'frame':frame,'source':str(source.relative_to(ROOT)).replace('\\','/'),'sourceOriginal':str(original.relative_to(ROOT)).replace('\\','/'),'sourceSHA256':sha(original),'originalDenseBounds':old_box,'originalAnchorX':original_anchor_x,'originalAnchorY':manifest['images'][frame].get('anchor_y',manifest['anchor_y']),'grounded':frame<=3 or frame>=12,'available':source.exists(),'approved':frame==8}
    if source.exists():
        im=Image.open(source).convert('RGBA');box=bounds(im,64);visible=bounds(im,8)
        norm=(old_box[2]-old_box[0])/(box[2]-box[0])
        anchor=box[3]-(old_box[3]-record['originalAnchorY'])/norm
        if record['grounded']:anchor=box[3]-1
        if frame==8:anchor=reference['supportY']
        record.update(sha256=sha(source),nativeSize=list(im.size),denseBounds=box,boundsAlpha8=visible,sourceNormalization=norm,scaleAtSmall=legacy_scale*norm,anchorX=box[0]+(original_anchor_x-old_box[0])/norm,supportY=anchor,anchorMethod='Grounded dense paw-sole/contact line; native fine fringe may extend below, inspect contact' if record['grounded'] else 'Dense-bottom clearance matched to original source at approved008 display scale',marginsClear=bool(visible[0]>0 and visible[1]>0 and visible[2]<im.width and visible[3]<im.height))
        if frame==8:
            assert record['sha256']==sha(ROOT/reference['source']),'Approved008 changed'
            record['scaleAtSmall']=reference['scaleAtSmall'];record['anchorMethod']='Exact approved current still008 support and scale'
    records.append(record)
report={'clip':'jump_left','frameCount':16,'seconds':manifest['seconds'],'loop':False,'boundsFormat':'LTRB','complete':all(r['available'] for r in records),'availableFrames':sum(r['available'] for r in records),'referenceFrame':8,'referenceSource':reference['source'],'legacyPixelScaleAtSmall':legacy_scale,'originalSourcesPreserved':True,'frames':records}
(ROOT/'planning/jump-left-family-v4.json').write_text(json.dumps(report,indent=2)+'\n')
board=Image.new('RGBA',(2000,1760),'#25313e');draw=ImageDraw.Draw(board);font=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',25)
for record in records:
    i=record['frame'];x=(i%4)*500;y=(i//4)*440;draw.text((x+12,y+6),f"{i:02}"+(' approved' if i==8 else ''),font=font,fill='white')
    if not record['available']:continue
    im=Image.open(ROOT/record['source']).convert('RGBA');s=record['scaleAtSmall'];b=record['denseBounds'];im=im.resize((round(im.width*s),round(im.height*s)),Image.Resampling.LANCZOS)
    px=round(x+250-(b[0]+b[2])/2*s);py=round(y+370-record['supportY']*s)
    if record['grounded']:draw.line((x,y+370,x+500,y+370),fill='#5bddd3',width=2)
    board.alpha_composite(im,(px,py))
board.convert('RGB').save(REVIEW/'current-contact.png')
print(json.dumps({'complete':report['complete'],'availableFrames':report['availableFrames'],'allMarginsClear':all(r.get('marginsClear',False) for r in records if r['available']),'metadata':'planning/jump-left-family-v4.json'}))
