"""Package complete native jump families without changing source sprite pixels."""
from pathlib import Path
import hashlib,json,math
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
def bounds(image,threshold=64):
    return image.getchannel('A').point(lambda a:255 if a>threshold else 0).getbbox()
def apply_anatomy_overrides(entries):
    """Apply approved action-only anatomy edits after validating the v4 baseline.

    Still008 remains the approved static calibration source. Its newer action
    counterpart preserves the head's world-space placement and reviewed size.
    """
    activation=ROOT/'planning/jump-anatomy-integration-v5.json'
    if not activation.exists():return entries
    settings=json.loads(activation.read_text())
    if not settings.get('enabled',False):return entries
    assert settings.get('approved') and settings.get('provisional'), 'V5 anatomy approval is missing.'
    assert settings.get('ownerApproval')=='approve for now', 'Unexpected V5 anatomy approval scope.'
    overrides={(f['clip'],f['frame']):f for f in settings['frames']}
    expected={(clip,frame) for clip in ('jump_left','jump_right') for frame in range(4,9)}
    assert len(settings['frames'])==len(overrides)==10 and set(overrides)==expected, 'Unexpected anatomy frame scope.'
    result=[];applied=0
    for entry,data in entries:
        override=overrides.get((entry['clip'],entry['frame']))
        if override is None:
            result.append((entry,data));continue
        assert override.get('approved') and override.get('provisional'), 'Unapproved anatomy frame.'
        baseline=override['baseline']
        assert entry==baseline, 'V4 calibration changed; re-review anatomy registration before rebuilding: '+entry['key']
        assert hashlib.sha256(data).hexdigest()==baseline['sha256'], 'V4 source was replaced.'
        candidate=ROOT/override['source'];new_data=candidate.read_bytes()
        assert hashlib.sha256(new_data).hexdigest()==override['sha256'], candidate
        image=Image.open(candidate).convert('RGBA');visible=bounds(image,8)
        assert visible and visible[0]>0 and visible[1]>0 and visible[2]<image.width and visible[3]<image.height, candidate
        correction=float(override['scaleCorrection'])
        assert math.isfinite(correction) and .6<=correction<=1.6, 'Invalid anatomy size correction.'
        old=override['oldPupilCenters'];new=override['newPupilCenters']
        assert len(old)==len(new)==2 and all(len(p)==2 for p in old+new), 'Invalid pupil landmarks.'
        old_center=[sum(p[i] for p in old)/2 for i in range(2)]
        new_center=[sum(p[i] for p in new)/2 for i in range(2)]
        assert all(math.isfinite(value) for value in old_center+new_center), 'Nonfinite pupil landmark.'
        scale=entry['renderWidthAtSmall']/entry['nativeWidth']*correction
        anchor_x=new_center[0]+(entry['anchorX']-old_center[0])/correction
        support_y=new_center[1]+(entry['supportY']-old_center[1])/correction
        assert 0<support_y<image.height and math.isfinite(anchor_x), 'Invalid anatomy root/support.'
        updated=dict(entry)
        updated.update(source=override['source'],sha256=override['sha256'],
                       bounds=[visible[0],visible[1],visible[2]-visible[0],visible[3]-visible[1]],
                       nativeWidth=image.width,nativeHeight=image.height,
                       renderWidthAtSmall=image.width*scale,anchorX=anchor_x,supportY=support_y,
                       anatomyOverride=True,anatomyProvisional=True,anatomyScaleCorrection=correction,
                       sourceV4=baseline['source'],sourceV4SHA256=baseline['sha256'],
                       notes='Owner provisionally approved anatomy revision; v4 originals and static stills preserved. Uniform whole-sprite size and pupil-aligned logical root/support.')
        result.append((updated,new_data));applied+=1
    assert applied==10, 'Incomplete anatomy overrides.'
    return result

def build_actions(current):
    activation=ROOT/'planning/action-art-integration-v4.json'
    if not activation.exists():return [],[]
    settings=json.loads(activation.read_text())
    if not settings.get('enabled',False):return [],[]
    original=json.loads((ROOT/'art/manifest.json').read_text())
    entries=[];clips=[]
    for clip,pose_id in [('jump_left',16),('jump_right',17)]:
        metadata=ROOT/settings['families'][clip]
        family=json.loads(metadata.read_text());frames=family['frames']
        assert family.get('complete') and len(frames)==16, 'Incomplete jump family: '+clip
        assert sorted(f['frame'] for f in frames)==list(range(16)),clip
        ref=next(f for f in frames if f['frame']==8)
        ref_image=Image.open(ROOT/ref['source']).convert('RGBA');ref_bounds=bounds(ref_image)
        old_ref=Image.open(ROOT/f'art/frames/{clip}/008.png').convert('RGBA');old_ref_bounds=bounds(old_ref)
        accepted=current[pose_id]
        assert hashlib.sha256((ROOT/ref['source']).read_bytes()).hexdigest()==accepted['sourceSHA256'], 'Approved frame8 changed.'
        old_scale=accepted['scaleAtSmall']*(ref_bounds[2]-ref_bounds[0])/(old_ref_bounds[2]-old_ref_bounds[0])
        for frame in sorted(frames,key=lambda f:f['frame']):
            number=frame['frame'];path=ROOT/frame['source'];data=path.read_bytes();digest=hashlib.sha256(data).hexdigest()
            assert digest==frame['sha256'],path
            old_path=ROOT/f'art/frames/{clip}/{number:03}.png'
            old_data=old_path.read_bytes();expected_old=frame.get('oldSHA256',frame.get('sourceSHA256'))
            assert hashlib.sha256(old_data).hexdigest()==expected_old,old_path
            image=Image.open(path).convert('RGBA');dense=bounds(image);visible=bounds(image,8)
            old_image=Image.open(old_path).convert('RGBA');old_bounds=bounds(old_image)
            assert visible[0]>0 and visible[1]>0 and visible[2]<image.width and visible[3]<image.height,path
            scale=old_scale*(old_bounds[2]-old_bounds[0])/(dense[2]-dense[0])
            anchor_x=dense[0]+(original['foot_anchor'][0]-old_bounds[0])*old_scale/scale
            old_clip=original['clips'][clip]
            anchor_y=old_clip['images'][number].get('anchor_y',old_clip.get('anchor_y',620))
            grounded=bool(frame.get('grounded',False))
            native_support=dense[3]-(old_bounds[3]-anchor_y)*old_scale/scale
            if grounded:
                native_support=frame.get('proposedSupportY',frame.get('supportY',dense[3]-3))
            # Keep the approved sample's geometry exactly, including its airborne clearance.
            if number==8:scale=accepted['scaleAtSmall'];native_support=accepted['supportY']
            correction=float(frame.get('headScaleCorrection',1))
            assert .6<=correction<=1.6,(clip,number,correction)
            if number==8:assert abs(correction-1)<1e-9,'Approved frame8 sizing must remain fixed.'
            scale*=correction
            assert 0<native_support<image.height,(clip,number,native_support)
            record={'id':pose_id,'key':f'action-{clip}-{number:03}.png','label':f'{clip} frame {number}',
                    'clip':clip,'frame':number,'source':frame['source'],'sha256':digest,
                    'bounds':[visible[0],visible[1],visible[2]-visible[0],visible[3]-visible[1]],
                    'customAnchor':True,'anchorX':anchor_x,'supportY':native_support,
                    'renderWidthAtSmall':image.width*scale,'nativeWidth':image.width,'nativeHeight':image.height,
                    'airborne':not grounded,'grounded':grounded,'sourceLegacySHA256':expected_old,'headScaleCorrection':correction,
                    'notes':'Complete latest-fur jump family; original timing and original sources retained.'}
            entries.append((record,data))
        clips.append({'clip':clip,'frames':16,'seconds':original['clips'][clip]['seconds'],'loop':original['clips'][clip].get('loop',False)})
    assert len(entries)==32
    return apply_anatomy_overrides(entries),clips
