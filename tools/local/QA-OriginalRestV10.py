"""Inspect actual C# fixed-body exports at a common native crop; no sprite edits."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import math,json,hashlib
import numpy as np
import cv2

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'local-data/reference-review/original-rest-v10'
FONT=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',17)
qa=json.loads((ROOT/'build/qa-original-rest-renderer-v10.json').read_text())
edge=ROOT/'draft-assets/consistency-v10/sit-edge/sit.png'
basis_paths={family:(edge if family=='sit' and edge.exists() else ROOT/f'art/frames/{family}/000.png') for family in ('sit','loaf','lay','sleep')}
for family,box,count in [('sit',(335,480,715,790),19),('loaf',(400,515,850,915),12),('lay',(440,540,850,915),14)]:
    items=[('Fixed neutral basis',basis_paths[family])]+[(f'Registered {family} {frame:03d}',OUT/f'rendered/look_{family}/{frame:03d}.png') for frame in range(count)]
    w,h=box[2]-box[0],box[3]-box[1]+35
    board=Image.new('RGB',(w*5,h*math.ceil(len(items)/5)),(47,51,58));draw=ImageDraw.Draw(board)
    for i,(label,path) in enumerate(items):
        image=Image.open(path).convert('RGBA').crop(box)
        x,y=i%5*w,i//5*h
        board.paste(image,(x,y+35),image);draw.text((x+5,y+5),label,font=FONT,fill='white')
    board.save(OUT/f'fixed-{family}-heads.png')
    # Whole-pose surface review at the actual common480/432 display widths,
    # sharing one root and support plane per family, never fitting cats to tiles.
    cw,ch=400,360;support={'sit':944,'loaf':948,'lay':927}[family]
    scale=(432 if family=='loaf' else 480)/1024
    for surface in ('taskbar','window'):
        sheet=Image.new('RGB',(cw*5,ch*math.ceil(len(items)/5)),(27,30,35));sd=ImageDraw.Draw(sheet)
        for i,(label,path) in enumerate(items):
            im=Image.open(path).convert('RGBA');im=im.resize((round(1024*scale),round(1024*scale)),Image.Resampling.LANCZOS)
            x,y=i%5*cw,i//5*ch;floor=y+310
            sd.text((x+8,y+8),label,font=FONT,fill='white');sd.line((x+5,floor,x+cw-5,floor),fill=(70,200,190),width=2)
            if surface=='window':sd.rectangle((x+5,floor+3,x+cw-5,y+ch-5),fill=(210,218,225))
            else:sd.rectangle((x+5,floor+3,x+cw-5,y+ch-5),fill=(40,45,52))
            sheet.paste(im,(round(x+cw/2-512*scale),round(floor-support*scale)),im)
        sheet.save(OUT/f'fixed-{family}-{surface}.png')

def family_name(clip):
    return clip[5:] if clip.startswith('look_') else clip[:-5] if clip.endswith('_eyes') or clip.endswith('_tail') else clip

# Review remaining eyelid/tail families too: these use the same fixed new basis,
# while only the gaze source sampling is translated in this revision.
for clip in ('sit_eyes','loaf_eyes','lay_eyes','sit_tail','lay_tail','sleep_tail'):
    family=family_name(clip);rows=[row for row in qa['cases'] if row['clip']==clip]
    support={'sit':944,'loaf':948,'lay':927,'sleep':936}[family];scale=(432 if family=='loaf' else 480)/1024
    for surface in ('taskbar','window'):
        cw,ch=400,360;sheet=Image.new('RGB',(cw*5,ch),(27,30,35));sd=ImageDraw.Draw(sheet)
        for i,row in enumerate(rows):
            image=Image.open(OUT/f"rendered/{clip}/{row['frame']:03d}.png").convert('RGBA');image=image.resize((round(1024*scale),round(1024*scale)),Image.Resampling.LANCZOS)
            x=i*cw;floor=310;sd.text((x+8,8),f"{clip} {row['frame']:03d}",font=FONT,fill='white')
            sd.line((x+5,floor,x+cw-5,floor),fill=(70,200,190),width=2)
            sd.rectangle((x+5,floor+3,x+cw-5,ch-5),fill=(210,218,225) if surface=='window' else (40,45,52))
            sheet.paste(image,(round(x+cw/2-512*scale),round(floor-support*scale)),image)
        sheet.save(OUT/f'fixed-{clip}-{surface}.png')

frame_records=[];bounds_by_family={key:[] for key in basis_paths};rim_cases=0
if edge.exists():
    neutral_sit=np.asarray(Image.open(OUT/'rendered/sit/000.png').convert('RGBA'))
    raw_neutral_sit=np.asarray(Image.open(edge).convert('RGBA'))
    original_sit=np.asarray(Image.open(ROOT/'art/frames/sit/000.png').convert('RGBA'))
    rim=np.any(raw_neutral_sit!=original_sit,axis=2)
for row in qa['cases']:
    family=family_name(row['clip']);path=OUT/f"rendered/{row['clip']}/{row['frame']:03d}.png"
    with Image.open(path) as image:
        arr=np.asarray(image.convert('RGBA'));box=image.getchannel('A').point(lambda a:255 if a>8 else 0).getbbox()
    assert box
    rect=[box[0],box[1],box[2]-box[0],box[3]-box[1]];bounds_by_family[family].append(rect)
    if edge.exists() and family=='sit':
        assert np.array_equal(arr[rim],neutral_sit[rim]),'Gaze overwrote corrected sitting edge.'
        rim_cases+=int(rim.sum())
    record={key:row[key] for key in ('clip','frame','sourceOffsetX','sourceOffsetY','basisSHA256','sourceSHA256')}
    record['visibleBounds']=rect;record['rendered']=str(path.relative_to(ROOT)).replace('\\','/')
    if row['clip'].startswith('look_'):
        # Independently recover the translation from unchanged foreleg stripe/
        # shoulder texture. This registers the neck to the neutral torso; it
        # does not infer head size from tail or outer fur dimensions.
        roi={'sit':(445,790,600,930),'loaf':(455,882,705,935),'lay':(450,870,700,910)}[family]
        basis=cv2.cvtColor(np.array(Image.open(ROOT/f'art/frames/{family}/000.png').convert('RGB')),cv2.COLOR_RGB2GRAY)
        source=cv2.cvtColor(np.array(Image.open(ROOT/f"art/frames/{row['clip']}/{row['frame']:03d}.png").convert('RGB')),cv2.COLOR_RGB2GRAY)
        x1,y1,x2,y2=roi;template=basis[y1:y2,x1:x2];search=source[y1-40:y2+40,x1-40:x2+40]
        response=cv2.matchTemplate(search,template,cv2.TM_CCOEFF_NORMED);_,score,_,point=cv2.minMaxLoc(response)
        delta=[point[0]-40,point[1]-40]
        assert delta==[row['sourceOffsetX'],row['sourceOffsetY']]
        record['registrationCorrelation']=score;record['registrationEvidenceROI']=list(roi)
    frame_records.append(record)
unions={}
for family,boxes in bounds_by_family.items():
    left=min(b[0] for b in boxes);top=min(b[1] for b in boxes);right=max(b[0]+b[2] for b in boxes);bottom=max(b[1]+b[3] for b in boxes)
    unions[family]=[left,top,right-left,bottom-top]
metadata={'version':10,'developmentOnly':True,'ownerArtworkApproval':False,
          'method':'Original1024 head source translated onto fixed neutral body using native shoulder/foreleg-core registration. No head scaling, deformation, synthetic fur or face crossfades.',
          'sourceOffsetConvention':'sourcePixel=destinationPixel+offset; negative sourceX moves the head right onto the neutral shoulders.',
          'headScale':1.0,'sourcePNGsEdited':False,'generatedHeadSourcesUsed':False,
          'diagnosis':'V8 did not register original fullframe head sources to the neutral shoulders. Sitting originals are15-22px left and3-6px up; lower-neck mask then exposed a collar mismatch. Perframe translations correct this without moving the body.',
          'protectedRGBAFromY':{'sit':752,'loaf':878,'lay':862},'protectedSilhouetteFromY':{'sit':735,'loaf':865,'lay':850},
          'correctedSitRimPixelCasesExact':rim_cases,'familyVisibleBounds':unions,
          'bases':{f:{'source':str(p.relative_to(ROOT)).replace('\\','/'),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for f,p in basis_paths.items()},
          'frames':frame_records,'standaloneQA':'build/qa-original-rest-renderer-v10.json','actualRuntimeQA':'build/qa-original-rest-runtime-v10.json',
          'visualReview':'45actual rendered head/neck attachments plus73restframes on whole-pose taskbar/window sheets; owner visual approval remains pending.'}
(ROOT/'planning/gaze-registration-v10.json').write_text(json.dumps(metadata,indent=2)+'\n')
runtime=OUT/'runtime-rendered'
if runtime.exists():
    for family,box,count in [('sit',(335,480,715,790),19),('loaf',(400,515,850,915),12),('lay',(440,540,850,915),14)]:
        items=[('Compiled neutral',runtime/f'{family}-neutral.png')]+[(f'Compiled {family} {frame:03d}',runtime/f'look_{family}/{frame:03d}.png') for frame in range(count)]
        if not all(path.exists() for _,path in items):continue
        w,h=box[2]-box[0],box[3]-box[1]+35;board=Image.new('RGB',(w*5,h*math.ceil(len(items)/5)),(47,51,58));draw=ImageDraw.Draw(board)
        for i,(label,path) in enumerate(items):
            image=Image.open(path).convert('RGBA').crop(box);x,y=i%5*w,i//5*h
            board.paste(image,(x,y+35),image);draw.text((x+5,y+5),label,font=FONT,fill='white')
        board.save(OUT/f'compiled-{family}-heads.png')
    down=runtime/'look_sit/011.png'
    if down.exists():
        board=Image.new('RGB',(1800,580),(27,30,35));draw=ImageDraw.Draw(board)
        for i,size in enumerate((480,640,800)):
            scale=size/1024;image=Image.open(down).convert('RGBA').resize((size,size),Image.Resampling.LANCZOS);x=i*600;floor=500
            draw.text((x+12,12),f'Compiled down011 - size{size}',font=FONT,fill='white')
            draw.line((x+5,floor,x+595,floor),fill=(70,200,190),width=2);draw.rectangle((x+5,floor+3,x+595,575),fill=(210,218,225))
            board.paste(image,(round(x+300-512*scale),round(floor-944*scale)),image)
        board.save(OUT/'compiled-sit-down011-all-sizes.png')
print(json.dumps({'passed':True,'frames':len(frame_records),'gazeFrames':45,'familyVisibleBounds':unions,'correctedSitRimPixelCasesExact':rim_cases}))
