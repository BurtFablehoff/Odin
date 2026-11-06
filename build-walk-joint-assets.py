from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import cv2,numpy as np,json,zipfile,shutil,math,argparse

BASE=Path(r'C:\_repo\output\pets\Odin\v3')
REF=BASE/'references/walk-motion/owner-annotation-32-20261031/marked-by-owner'
parser=argparse.ArgumentParser()
parser.add_argument('--output',type=Path,default=Path(__file__).resolve().parent/'build'/'walk-video-joints-v2')
OUT=parser.parse_args().output
OUT.mkdir(exist_ok=True)
FONT=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',15)
COLORS={'FR':(20,101,186,255),'FL':(185,91,26,255),'RL':(32,132,67,255),'RR':(134,80,176,255)}
body_points=[(85,91),(97,87),(143,88),(179,92),(208,96),(231,104),(247,96),(260,90),(267,95),(282,111),(303,120),(320,133),(329,145),(319,152),(301,158),(287,172),(263,178),(232,181),(199,180),(163,176),(132,169),(111,161),(96,150),(86,137)]
def center_path(mask):
    ys=np.where(mask.any(axis=1))[0]
    if not len(ys):return []
    points=[[float(np.median(np.where(mask[y]>0)[0])),float(y)] for y in ys]
    # Retain the sideways toe segment instead of ending at the median of a
    # horizontal hand-drawn paw line.
    end_y=int(ys[-1]);tail_y=max(int(ys[0]),end_y-3)
    yy,xx=np.where(mask[tail_y:end_y+1]>0)
    before=points[max(0,len(points)-8)][0]
    end_x=float(np.min(xx) if np.mean(xx)<before else np.max(xx))
    points.append([end_x,float(end_y)])
    return cv2.approxPolyDP(np.array(points,dtype='float32').reshape(-1,1,2),2.2,False).reshape(-1,2).tolist()

THIGH=(66,91,143,198)
thigh_mask=Image.new('L',(382,264));ImageDraw.Draw(thigh_mask).ellipse(THIGH,fill=255)
def radius_at(t,rear):
    profile=[(0,16),(.3,15),(.58,9),(.82,6),(1,5)] if rear else [(0,11),(.35,9),(.75,6),(1,5)]
    for (a,ra),(b,rb) in zip(profile,profile[1:]):
        if t<=b:return ra+(rb-ra)*(t-a)/(b-a)
    return profile[-1][1]

def volume_layer(leg,mask):
    layer=Image.new('RGBA',(382,264));draw=ImageDraw.Draw(layer);points=center_path(mask)
    rear=leg.startswith('R');colour=COLORS[leg]
    # Both rear legs have a full proximal thigh. Far RL is flattened first;
    # the opaque torso and matching near RR haunch hide its proximal mass.
    if rear:draw.ellipse(THIGH,fill=colour)
    if not points:return layer,points
    lengths=[math.dist(a,b) for a,b in zip(points,points[1:])];total=max(1,sum(lengths));distance=0
    for a,b,length in zip(points,points[1:],lengths):
        dx,dy=b[0]-a[0],b[1]-a[1];norm=max(.01,length);nx,ny=-dy/norm,dx/norm
        ra=radius_at(distance/total,rear);rb=radius_at((distance+length)/total,rear)
        draw.polygon([(a[0]+nx*ra,a[1]+ny*ra),(b[0]+nx*rb,b[1]+ny*rb),
                      (b[0]-nx*rb,b[1]-ny*rb),(a[0]-nx*ra,a[1]-ny*ra)],fill=colour)
        draw.ellipse((a[0]-ra,a[1]-ra,a[0]+ra,a[1]+ra),fill=colour)
        draw.ellipse((b[0]-rb,b[1]-rb,b[0]+rb,b[1]+rb),fill=colour)
        distance+=length
    px,py=points[-1];draw.ellipse((px-9,py-4,px+9,py+5),fill=colour)
    # Small landmarks show the bone path within the volume; they are not the
    # limb outline and they are hidden by later anatomical layers as well.
    draw.line([tuple(p) for p in points],fill=(235,235,240,255),width=1)
    for px,py in points[1:-1]:draw.ellipse((px-1.5,py-1.5,px+1.5,py+1.5),fill=(245,245,248,255))
    return layer,points

records=[];occlusion_checks=[]
for page,span in enumerate(['01-08','09-16','17-24','25-32']):
    board=Image.open(REF/f'Owner-marked-walk-{span}.png').convert('RGB')
    sx=board.width/797;sy=board.height/1267
    for cell in range(8):
        frame=page*8+cell+1;col=cell%2;row=cell//2
        x=round((11+399*col)*sx);y=round((90+305*row)*sy)
        tile=board.crop((x,y,min(board.width,x+round(382*sx)),min(board.height,y+round(264*sy))))
        tile=tile.resize((382,264),Image.Resampling.LANCZOS)
        tile.save(OUT/f'source-{frame:02d}.png')
        a=np.array(tile);r,g,b=a[:,:,0].astype(float),a[:,:,1].astype(float),a[:,:,2].astype(float)
        masks={'right':((r>85)&(r>g*1.6)&(r>b*1.3)).astype('uint8')*255,
               'left':((g>85)&(g>r*1.4)&(g>b*1.15)).astype('uint8')*255}
        leg_masks={leg:np.zeros((264,382),dtype='uint8') for leg in COLORS}
        for side,mask in masks.items():
            count,labels,stats,centers=cv2.connectedComponentsWithStats(mask)
            for k in range(1,count):
                if stats[k,cv2.CC_STAT_AREA]<12:continue
                cx,cy=centers[k]
                if cy<90:continue
                leg=('F' if cx>175 else 'R')+('R' if side=='right' else 'L')
                leg_masks[leg][labels==k]=255
        # These two dashed paths were previously inferred from the visible white
        # foreleg in the video, and are kept distinct from owner-drawn evidence.
        inferred=[]
        if frame in (5,7) and np.count_nonzero(leg_masks['FL'])<12:
            pts={5:[[250,175],[242,197],[247,222],[270,232]],7:[[247,172],[234,197],[240,219],[261,226]]}[frame]
            cv2.polylines(leg_masks['FL'],[np.array(pts,dtype='int32')],False,255,3)
            inferred=['FL']
        layers={};joint_paths={}
        for leg,mask in leg_masks.items():
            layers[leg],joint_paths[leg]=volume_layer(leg,mask)
        torso=Image.new('RGBA',(382,264));td=ImageDraw.Draw(torso)
        td.polygon(body_points,fill=(205,207,211,255),outline=(120,124,132,255))
        td.line([(91,99),(72,77),(68,48),(72,17)],fill=(175,178,184,255),width=11)
        diagram=Image.new('RGBA',(382,264),(245,246,248,255))
        for leg in ['FL','RL']:diagram.alpha_composite(layers[leg])
        diagram.alpha_composite(torso)
        for leg in ['RR','FR']:diagram.alpha_composite(layers[leg])
        rgba=np.array(diagram);proximal=np.array(thigh_mask)>0
        exposed_green=int(np.count_nonzero(proximal & np.all(rgba[:,:,:3]==np.array(COLORS['RL'][:3]),axis=2)))
        assert exposed_green==0,('Far green upper thigh visible in front',frame,exposed_green)
        occlusion_checks.append({'frame':frame,'greenUpperThighPixelsVisible':exposed_green,
                                 'nearThighArea':int(np.count_nonzero(proximal))})
        # Labels are annotations after flattening, not limb layers over the body.
        dd=ImageDraw.Draw(diagram);leg_info={}
        for leg,mask in leg_masks.items():
            ys,xs=np.where(mask>0);visible=bool(len(xs));foot=None
            if visible:
                bottom=ys>=np.quantile(ys,.9);foot=[float(np.median(xs[bottom])),float(np.max(ys))]
                px,py=foot;dd.text((max(2,min(352,px-8)),min(246,py+7)),leg,font=FONT,fill=COLORS[leg])
            leg_info[leg]={'side':'near' if leg.endswith('R') else 'far','markedPixels':int(len(xs)),
                           'foot':foot,'jointPath':joint_paths[leg],
                           'evidence':'visible-video inference' if leg in inferred else 'owner trace' if visible else 'unmarked or occluded; inspect source'}
        diagram.convert('RGB').save(OUT/f'joints-{frame:02d}.png')
        records.append({'number':frame,'seconds':(frame-1)/15,'source':f'source-{frame:02d}.png',
                        'diagram':f'joints-{frame:02d}.png','legs':leg_info,
                        'drawOrder':['FL','RL','BODY','RR','FR']})

master=BASE/'references/owner-approved-original-body-20261101/approved-original.png'
shutil.copy2(master,OUT/'appearance-master.png')
shutil.copy2(Path(r'C:\_repo\_downloads\odin-reference-review-20261105\set5\IMG_7705.jpg'),OUT/'appearance-photo.jpg')
manifest={'version':2,'frames':records,'frameMilliseconds':67,'scope':'Filled limb-volume review: far green thigh hidden by body and near purple haunch. Finished furred walk pending approval.',
          'appearanceMaster':'appearance-master.png','appearancePhoto':'appearance-photo.jpg',
          'frontChain':['shoulder','elbow','carpus/wrist','paw/toes'],'rearChain':['hip','stifle/knee','hock/ankle','paw/toes'],
          'sources':['https://www.acvs.org/small-animal/fractured-limbs/','https://www.cliniciansbrief.com/article/feline-orthopedic-examination',
                     'https://wpcdn.web.wsu.edu/extension/uploads/sites/44/2018/06/4-H-Cat-Anatomy-and-Physiology-Unit-3.pdf'],
          'sourceVideo':str(BASE/'references/walk-motion/user-reference-20261031.mp4'),
          'limitations':['Owner traces are pose references, not measurements of Odin limb thickness.',
                         'Unmarked limbs remain anatomically tracked; lack of a trace is not proof of complete occlusion.',
                         'Filled limb masses and gray torso are anatomical drawing guides, not final Odin fur artwork.',
                         'This review does not approve a finished furred animation.']}
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2))
(OUT/'haunch-occlusion-qa.json').write_text(json.dumps({'passed':True,'framesChecked':32,'checks':occlusion_checks},indent=2))
with zipfile.ZipFile(OUT/'walk-review.zip','w',compression=zipfile.ZIP_DEFLATED) as z:
    members=['manifest.json','haunch-occlusion-qa.json','appearance-master.png','appearance-photo.jpg']
    for record in records:members.extend([record['source'],record['diagram']])
    for name in members:z.write(OUT/name,name)
    assert z.testzip() is None
assert len(records)==32 and all(set(r['legs'])==set(COLORS) for r in records)
print('OK: 32 filled-volume guides; far green upper thigh hidden in all 32; near purple haunch and tapered joint chains; walk-review.zip')
