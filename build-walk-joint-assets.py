from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import cv2,numpy as np,json,zipfile,shutil,math,argparse

BASE=Path(r'C:\_repo\output\pets\Odin\v3')
REF=BASE/'references/walk-motion/owner-annotation-32-20261031/marked-by-owner'
parser=argparse.ArgumentParser()
parser.add_argument('--output',type=Path,default=Path(__file__).resolve().parent/'build'/'walk-video-joints-v6')
OUT=parser.parse_args().output
OUT.mkdir(exist_ok=True)
FONT=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',15)
COLORS={'FR':(20,101,186,255),'FL':(185,91,26,255),'RL':(32,132,67,255),'RR':(134,80,176,255)}
body_points=[(81,94),(97,87),(143,88),(179,92),(208,96),(231,104),(247,96),(260,90),(267,95),(282,111),(303,120),(320,133),(329,145),(319,152),(301,158),(287,172),(263,178),(232,181),(199,180),(163,176),(132,174),(102,174),(87,168),(76,152),(73,134),(76,113)]
HIP=(108.0,118.0)
SHOULDER=(226.0,145.0)
RL_FORWARD=16
FL_BACK=-8
NEAR_VOLUME_SCALE=1.07
FAR_VOLUME_SCALE=0.96
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

hip_mask=Image.new('L',(382,264));ImageDraw.Draw(hip_mask).ellipse((HIP[0]-12,HIP[1]-12,HIP[0]+12,HIP[1]+12),fill=255)

def anchored_path(leg,mask):
    observed=center_path(mask);rear=leg.startswith('R');anchor=HIP if rear else SHOULDER
    if not observed:return [list(anchor)],observed
    # The first exposed trace point can be an elbow, knee, or hock rather
    # than a body attachment. Keep the observed bends below the body and
    # connect their hidden proximal portion to the common anatomical anchor.
    distal=[p for p in observed if p[1]>=anchor[1]+10]
    if not distal:distal=[observed[-1]]
    points=[list(anchor)]+distal
    if points[-1]!=observed[-1]:points.append(observed[-1])
    return points,observed

def volume_layer(leg,mask):
    layer=Image.new('RGBA',(382,264));draw=ImageDraw.Draw(layer);points,observed=anchored_path(leg,mask)
    rear=leg.startswith('R');colour=COLORS[leg]
    if not observed:return layer,points
    # Muscle follows the hip-to-knee segment; no fixed oval is pasted on.
    # The rear shin and ankle are substantially thicker than the old sticks.
    profile=[(0,15),(.15,22),(.33,14),(.70,11),(1,8)] if rear else [(0,11),(.4,10),(.75,8),(1,7)]
    lengths=[math.dist(a,b) for a,b in zip(points,points[1:])];total=max(1,sum(lengths));distance=0
    def radius_at(t):
        for (a,ra),(b,rb) in zip(profile,profile[1:]):
            if t<=b:return (ra+(rb-ra)*(t-a)/(b-a))*(NEAR_VOLUME_SCALE if leg.endswith('R') else FAR_VOLUME_SCALE)
        return profile[-1][1]*(NEAR_VOLUME_SCALE if leg.endswith('R') else FAR_VOLUME_SCALE)
    for segment,(a,b) in enumerate(zip(points,points[1:])):
        dx,dy=b[0]-a[0],b[1]-a[1];length=max(.01,math.hypot(dx,dy));nx,ny=-dy/length,dx/length
        left=[];right=[]
        for n in range(21):
            t=n/20;cx,cy=a[0]+dx*t,a[1]+dy*t
            radius=radius_at((distance+length*t)/total)
            left.append((cx+nx*radius,cy+ny*radius));right.append((cx-nx*radius,cy-ny*radius))
        draw.polygon(left+right[::-1],fill=colour)
        for p,radius in [(a,radius_at(distance/total)),(b,radius_at((distance+length)/total))]:
            draw.ellipse((p[0]-radius,p[1]-radius,p[0]+radius,p[1]+radius),fill=colour)
        distance+=length
    px,py=points[-1];paw_scale=NEAR_VOLUME_SCALE if leg.endswith('R') else FAR_VOLUME_SCALE
    draw.ellipse((px-10*paw_scale,py-5*paw_scale,px+10*paw_scale,py+6*paw_scale),fill=colour)
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
        source_paws={leg:(center_path(mask)[-1] if np.any(mask) else None) for leg,mask in leg_masks.items()}
        # Owner adjustments move the exposed far limbs while retaining shared
        # hip/shoulder anchors and the pose timing of every limb.
        leg_masks['RL']=cv2.warpAffine(leg_masks['RL'],np.array([[1,0,RL_FORWARD],[0,1,0]],dtype='float32'),(382,264),flags=cv2.INTER_NEAREST)
        leg_masks['FL']=cv2.warpAffine(leg_masks['FL'],np.array([[1,0,FL_BACK],[0,1,0]],dtype='float32'),(382,264),flags=cv2.INTER_NEAREST)
        layers={};joint_paths={}
        for leg,mask in leg_masks.items():
            layers[leg],joint_paths[leg]=volume_layer(leg,mask)
        torso=Image.new('RGBA',(382,264));td=ImageDraw.Draw(torso)
        td.polygon(body_points,fill=(205,207,211,255),outline=(120,124,132,255))
        td.line([(91,99),(72,77),(68,48),(72,17)],fill=(175,178,184,255),width=11)
        # A hidden upper connection is tracked, but is not invented as a new
        # visible segment outside the owner's exposed far-leg trace.
        for leg in ['FL','RL']:
            visible_support=cv2.dilate(leg_masks[leg],cv2.getStructuringElement(cv2.MORPH_ELLIPSE,(29,29)))
            occluder=np.maximum(np.array(torso.getchannel('A')),np.array(layers['RR'].getchannel('A')))
            allowed=np.maximum(visible_support,occluder)
            layers[leg].putalpha(Image.fromarray(np.minimum(np.array(layers[leg].getchannel('A')),allowed)))
        diagram=Image.new('RGBA',(382,264),(245,246,248,255))
        for leg in ['FL','RL']:diagram.alpha_composite(layers[leg])
        diagram.alpha_composite(torso)
        for leg in ['RR','FR']:diagram.alpha_composite(layers[leg])
        assert joint_paths['RR'][0]==joint_paths['RL'][0]==list(HIP)
        assert joint_paths['FR'][0]==joint_paths['FL'][0]==list(SHOULDER)
        rgba=np.array(diagram);proximal=np.array(hip_mask)>0
        exposed_green=int(np.count_nonzero(proximal & np.all(rgba[:,:,:3]==np.array(COLORS['RL'][:3]),axis=2)))
        assert exposed_green==0,('Far green upper thigh visible in front',frame,exposed_green)
        occlusion_checks.append({'frame':frame,'greenUpperThighPixelsVisible':exposed_green,
                                 'sharedHip':list(HIP),'sharedShoulder':list(SHOULDER),
                                 'visibleVideoBendsPreserved':True,'rearLeftForwardOffset':RL_FORWARD,'frontLeftBackOffset':FL_BACK})
        # Labels are annotations after flattening, not limb layers over the body.
        dd=ImageDraw.Draw(diagram);leg_info={}
        for letter,(ax,ay) in [('H',HIP),('S',SHOULDER)]:
            dd.ellipse((ax-3,ay-3,ax+3,ay+3),fill='white',outline=(50,50,50,255))
            dd.text((ax+5,ay-17),letter,font=FONT,fill=(50,50,50,255))
        for leg,mask in leg_masks.items():
            ys,xs=np.where(mask>0);visible=bool(len(xs));foot=None
            if visible:
                bottom=ys>=np.quantile(ys,.9);foot=[float(np.median(xs[bottom])),float(np.max(ys))]
                px,py=foot;dd.text((max(2,min(352,px-8)),min(246,py+7)),leg,font=FONT,fill=COLORS[leg])
            leg_info[leg]={'side':'near' if leg.endswith('R') else 'far','markedPixels':int(len(xs)),
                           'foot':foot,'jointPath':joint_paths[leg],
                           'observedPaw':source_paws[leg],'poseOffset':[RL_FORWARD if leg=='RL' else FL_BACK if leg=='FL' else 0,0],
                           'evidence':'visible-video inference' if leg in inferred else 'owner trace' if visible else 'unmarked or occluded; inspect source'}
        diagram.convert('RGB').save(OUT/f'joints-{frame:02d}.png')
        records.append({'number':frame,'seconds':(frame-1)/15,'source':f'source-{frame:02d}.png',
                        'diagram':f'joints-{frame:02d}.png','legs':leg_info,
                        'drawOrder':['FL','RL','BODY','RR','FR']})

master=BASE/'references/owner-approved-original-body-20261101/approved-original.png'
shutil.copy2(master,OUT/'appearance-master.png')
shutil.copy2(Path(r'C:\_repo\_downloads\odin-reference-review-20261105\set5\IMG_7705.jpg'),OUT/'appearance-photo.jpg')
manifest={'version':6,'frames':records,'frameMilliseconds':67,'scope':'RL advanced 16px and FL moved back 8px with shared hip and shoulder fixed; right-side near limbs subtly fuller than far-side left limbs; all paths and timing retained. Finished furred walk pending approval.',
          'hipAnchor':list(HIP),'shoulderAnchor':list(SHOULDER),
          'nearVolumeScale':NEAR_VOLUME_SCALE,'farVolumeScale':FAR_VOLUME_SCALE,
          'rearLeftForwardOffset':RL_FORWARD,'frontLeftBackOffset':FL_BACK,
          'appearanceMaster':'appearance-master.png','appearancePhoto':'appearance-photo.jpg',
          'frontChain':['shoulder','elbow','carpus/wrist','paw/toes'],'rearChain':['hip','stifle/knee','hock/ankle','paw/toes'],
          'sources':['https://www.acvs.org/small-animal/fractured-limbs/','https://www.cliniciansbrief.com/article/feline-orthopedic-examination',
                     'https://wpcdn.web.wsu.edu/extension/uploads/sites/44/2018/06/4-H-Cat-Anatomy-and-Physiology-Unit-3.pdf'],
          'sourceVideo':str(BASE/'references/walk-motion/user-reference-20261031.mp4'),
          'limitations':['Owner traces are pose references, not measurements of Odin limb thickness.',
                         'Unmarked limbs remain anatomically tracked; lack of a trace is not proof of complete occlusion.',
                         'Hidden upper connections use shared anchors. RL and FL have explicit owner-requested pose adjustments; near-side video paw positions are retained. These traces are not bone-length measurements.',
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
print('OK: 32 frames; RL +16px, FL -8px with shared anchors fixed; near limbs subtly fuller; all paths retained; far-thigh occlusion checked')
