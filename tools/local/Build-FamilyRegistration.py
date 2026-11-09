"""Resolve all 73 resting-family slots without flattening vertical gaze directions.

This writes a rendering plan, not raster replacements. Nearest-cardinal previews
preserve the requested angle separately and never claim 45 distinct new poses.
All legacy and approved sources remain untouched. Run --check to validate it.
"""
from pathlib import Path
from collections import Counter
import argparse, hashlib, json, math, re
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
POSE_IDS={'sit':1,'loaf':2,'lay':3,'sleep':4}
CARDINAL_ANGLES={'up':0,'right':90,'down':180,'left':270}
OCTANT_ANGLES={'up':0,'up-right':45,'right':90,'down-right':135,'down':180,'down-left':225,'left':270,'up-left':315}

def read(relative):
    return json.loads((ROOT/relative).read_text())

def digest(relative):
    return hashlib.sha256((ROOT/relative).read_bytes()).hexdigest()

def asset(relative,expected=None,approved=False):
    actual=digest(relative)
    if expected and actual!=expected:
        raise ValueError('Source hash changed: '+relative)
    image=Image.open(ROOT/relative).convert('RGBA')
    bounds=image.getchannel('A').point(lambda a:255 if a>8 else 0).getbbox()
    return {'source':relative,'sha256':actual,'size':list(image.size),
            'visibleBounds':list(bounds),'artApproved':bool(approved)}

def array_from_csharp(relative,name):
    source=(ROOT/relative).read_text()
    match=re.search(r'\b'+name+r'\s*=\s*\{([^}]+)\}',source)
    if not match:
        raise ValueError('Could not read authoritative direction array '+name)
    return [float(value.strip()) for value in match.group(1).split(',')]

def build():
    legacy=read('planning/resting-variant-reconciliation.json')
    assert len(legacy['frames'])==73
    poses={p['id']:p for p in read('planning/state-head-review.json')['poses']}
    angles={'sit':array_from_csharp('app/SittingGaze.cs','Angles'),
            'loaf':array_from_csharp('app/ClockGaze.cs','ClockAngles'),
            'lay':array_from_csharp('app/ClockGaze.cs','LayingAngles')}
    assert [len(angles[k]) for k in ('sit','loaf','lay')]==[19,12,14]
    bases={};gazes={};tails={};eyes={}
    for family,pose_id in POSE_IDS.items():
        pose=poses[pose_id]
        entry=asset(pose['source'],pose['sourceSHA256'])
        bounds=entry['visibleBounds']
        entry.update(poseId=pose_id,supportX=(bounds[0]+bounds[2])/2,
                     supportY=pose['supportY'],scaleAtSmall=pose['scaleAtSmall'],
                     renderWidthAtSmall=entry['size'][0]*pose['scaleAtSmall'],
                     allowedBelowSupport=['paws','tail'] if family=='sleep' else ['paws'],
                     authorization='Current review base and owner-requested sizing; no broadened approval claim.')
        bases[family]=entry
    for kind,collection in [('gaze',gazes),('tail',tails)]:
        data=read(f'planning/{kind}-reconciliation-v1.json')
        for record in data['frames']:
            collection[(record['family'],record['phase'])]=asset(record['source'],record['sha256'],record.get('approved',False))
    vertical=ROOT/'planning/vertical-gaze-reconciliation-v1.json'
    if vertical.exists():
        for record in json.loads(vertical.read_text())['frames']:
            gazes[(record['family'],record['phase'])]=asset(record['source'],record['sha256'],record.get('approved',False))
    diagonal=ROOT/'planning/diagonal-gaze-reconciliation-v4.json'
    if diagonal.exists():
        diagonal_data=json.loads(diagonal.read_text())
        if len(diagonal_data.get('frames',[]))==12:
            for record in diagonal_data['frames']:
                gazes[(record['family'],record['phase'])]=asset(record['source'],record['sha256'],record.get('approved',False))
    blink=read('planning/blink-reconciliation-v1.json')
    override_path=ROOT/'planning/queued-art-edits-v3.json'
    if override_path.exists():
        for override in json.loads(override_path.read_text()).get('motionOverrides',[]):
            kind,pose_id,phase=override['key'].removesuffix('.png').split('-',2)
            family=next(k for k,v in POSE_IDS.items() if v==int(pose_id))
            collection=gazes if kind=='gaze' else tails
            collection[(family,phase)]=asset(override['source'],override['sha256'],override.get('approved',False))
    for record in blink['frames']:
        eyes[(record['family'],record['frame'])]=asset(record['candidate'],record['sha256'],record.get('artApproved',blink.get('artApproved',False)))
    frames=[]
    for source in legacy['frames']:
        family=source['family'];clip=source['clip'];number=source['frame'];basis=bases[family]
        assert digest(source['source'])==source['sourceSHA256'],source['source']
        record={'clip':clip,'frame':number,'family':family,'poseId':basis['poseId'],
                'legacySource':source['source'],'legacySHA256':source['sourceSHA256'],
                'base':basis['source'],'supportY':basis['supportY'],'scaleAtSmall':basis['scaleAtSmall'],
                'renderer':'base','parts':[],'previewAvailable':True,'variantArtApproved':True}
        if clip.endswith('_eyes') and number>0:
            record.update(renderer='eyelid',parts=[dict(eyes[(family,number)],weight=1)],
                          rendererEntry='OdinV2.BlinkEyeRenderer.Compose')
            record['variantArtApproved']=eyes[(family,number)]['artApproved']
        elif clip.endswith('_tail') and number in (1,3):
            phase='out' if number==1 else 'in';part=tails[(family,phase)]
            record.update(renderer='tail',phase=phase,parts=[dict(part,weight=1)],
                          rendererEntry='OdinV2.PoseVariantRenderer.Compose')
            record['variantArtApproved']=part['artApproved']
        elif clip.startswith('look_'):
            angle=angles[family][number];radians=math.radians(angle)
            horizontal=math.sin(radians);up=math.cos(radians)
            raw={'right':max(0,horizontal),'left':max(0,-horizontal),'up':max(0,up),'down':max(0,-up)}
            raw={key:value for key,value in raw.items() if value>1e-9};total=sum(raw.values())
            parts=[];missing=[]
            for direction,weight in raw.items():
                key=(family,direction)
                if key not in gazes:missing.append(direction)
                else:parts.append(dict(gazes[key],phase=direction,cardinalAngleDegrees=CARDINAL_ANGLES[direction],weight=round(weight/total,12)))
            record.update(renderer='gaze',angleDegrees=angle,
                          screenDirection={'x':round(horizontal,12),'y':round(-up,12)},
                          parts=parts,missingDirections=missing,previewAvailable=not missing,
                          variantArtApproved=not missing and all(p['artApproved'] for p in parts),
                          rendererEntry='Head-layer cardinal blend; compose one weighted head layer on the fixed base',
                          independentlyRenderedDirection=len(parts)==1 and not missing)
            available=[name for name in OCTANT_ANGLES if (family,name) in gazes]
            actual_phase=min(available,key=lambda name:(min(abs(angle-OCTANT_ANGLES[name]),360-abs(angle-OCTANT_ANGLES[name])),(OCTANT_ANGLES[name]-angle)%360))
            actual_angle=OCTANT_ANGLES[actual_phase]
            difference=abs(angle-actual_angle)%360
            record.update(defaultDirectionMode='nearest-direction',
                          renderedCardinal=actual_phase,renderedAngleDegrees=actual_angle,
                          angularErrorDegrees=min(difference,360-difference),
                          requestedCardinalComponents=raw,
                          renderPolicy='Use the one nearest actual head keyframe. Component weights are direction metadata, not a source-image crossfade.')
            selected=gazes[(family,actual_phase)]
            error=min(difference,360-difference)
            record.update(parts=[dict(selected,phase=actual_phase,cardinalAngleDegrees=actual_angle,weight=1)],
                          missingDirections=[],previewAvailable=True,
                          variantArtApproved=selected['artApproved'],independentlyRenderedDirection=error<1e-9,
                          rendererEntry='Single registered head layer; no source-image crossfade')
            if error>1e-9:record['status']='Nearest actual direction; requested angle is not independently drawn.'
            elif not record['variantArtApproved']:record['status']='New directional keyframe candidate; owner approval not inferred.'
        frames.append(record)
    assert len({(r['clip'],r['frame']) for r in frames})==73
    counts=Counter(r['clip'] for r in frames)
    assert counts=={'sit':1,'loaf':1,'lay':1,'sleep':1,'look_sit':19,'look_loaf':12,'look_lay':14,'sit_eyes':3,'loaf_eyes':3,'lay_eyes':3,'sit_tail':5,'lay_tail':5,'sleep_tail':5}
    for family in ('sit','lay','sleep'):
        cycle=[r for r in frames if r['clip']==family+'_tail']
        assert [r.get('phase','neutral') for r in cycle]==['neutral','out','neutral','in','neutral']
    gaze_frames=[r for r in frames if r['renderer']=='gaze']
    return {'schemaVersion':1,'reviewOnly':True,'runtimeIntegrated':False,
            'frameCount':73,'clipCounts':dict(counts),'bases':bases,
            'sourceAngleArrays':{'app/SittingGaze.cs':digest('app/SittingGaze.cs'),'app/ClockGaze.cs':digest('app/ClockGaze.cs')},
            'angleConvention':'0 up, 90 image-right, 180 down, 270 image-left; screen coordinates y increases downward.',
            'tailCycle':'neutral / out / neutral / in / neutral; 5 frames, non-looping, original timing retained.',
            'defaultDirectionMode':'nearest-direction',
            'directionalPreviewPolicy':'Preserve exact scheduler angle; display the nearest actually drawn head. Eight directions when all diagonal assets exist, otherwise four cardinals. Do not crossfade head poses.',
            'rejectedBlendEvidence':'local-data/reference-review/registered-families-rejected-blend-v1/failed-blend-render-report.json',
            'sourcePreservation':'All73 legacy source hashes checked; no source PNG pixels modified.',
            'summary':{'previewAvailableSlots':sum(r['previewAvailable'] for r in frames),
                       'unavailableSlots':sum(not r['previewAvailable'] for r in frames),
                       'slotsUsingApprovedVariantArt':sum(r['previewAvailable'] and r['variantArtApproved'] for r in frames),
                       'candidateOrQuantizedGazeSlots':sum(not r['variantArtApproved'] or not r.get('independentlyRenderedDirection',False) for r in gaze_frames),
                       'approximatedGazeSlots':sum(r['angularErrorDegrees']>1e-9 for r in gaze_frames),
                       'actualDistinctCardinalKeyframes':12,
                       'actualDistinctDirectionKeyframes':len(gazes),
                       'gazeSlots':45,'exactHorizontalSlots':6},
            'frames':frames}

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--check',action='store_true');parser.add_argument('--output',default='planning/family-registration-v1.json');args=parser.parse_args()
    report=build();destination=ROOT/args.output
    if args.check:
        assert json.loads(destination.read_text())==report,'Stored plan is stale; rebuild after reviewing source changes.'
    else:
        destination.parent.mkdir(parents=True,exist_ok=True);destination.write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({'passed':True,'frameCount':report['frameCount'],'clipCounts':report['clipCounts'],'summary':report['summary'],'plan':str(destination)}))
