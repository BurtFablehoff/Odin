"""Restore original jump artwork without recoloring or deforming source pixels.

This is an explicit v7 development override; historical v4/v5 source and approvals
remain intact. Generated anatomy revisions, when present, stay separate candidates.
"""
from pathlib import Path
from PIL import Image
import hashlib,json

ROOT=Path(__file__).resolve().parents[2]
# V6 review scale mapped from accepted static008 to original dense silhouette.
# Persist once rather than deriving from a future v7 pose and compounding scale.
V6_LEGACY_SCALE={'jump_left':0.5881444209039549,'jump_right':0.5701455041821561}

def alpha_bounds(image,threshold=8):
    return image.getchannel('A').point(lambda a:255 if a>threshold else 0).getbbox()

def build_actions(current=None):
    manifest=json.loads((ROOT/'art/manifest.json').read_text())
    actions=[];clips=[]
    for clip,pose_id in [('jump_left',16),('jump_right',17)]:
        historical=json.loads((ROOT/f'planning/jump-{clip.split("_")[1]}-family-v4.json').read_text())
        old_scale=V6_LEGACY_SCALE[clip]
        v8_path=ROOT/'planning/owner-corrections-v8.json'
        if v8_path.exists():
            v8=json.loads(v8_path.read_text())
            if v8.get('enabled'):old_scale=v8['originalSpriteWidthsAtSmall'][str(pose_id)]/1024
        assert .3<old_scale<1.0
        original_clip=manifest['clips'][clip]
        assert len(original_clip['images'])==16
        for frame in range(16):
            source=f'art/frames/{clip}/{frame:03}.png'
            path=ROOT/source;data=path.read_bytes();digest=hashlib.sha256(data).hexdigest()
            hist=next(f for f in historical['frames'] if f['frame']==frame)
            assert digest==hist.get('oldSHA256',hist.get('sourceSHA256')),source
            image=Image.open(path).convert('RGBA')
            visible=alpha_bounds(image);dense=alpha_bounds(image,64)
            assert image.size==(1024,1024) and visible
            assert visible[0]>0 and visible[1]>0 and visible[2]<image.width and visible[3]<image.height,source
            grounded=bool(hist.get('grounded',False))
            legacy_anchor=original_clip['images'][frame].get('anchor_y',original_clip.get('anchor_y',620))
            # Airborne root/support never follows a changing fur bounding box.
            # Grounded frames sit on actual original paw soles, with a narrow
            # antialiased fringe allowed below the dense support measurement.
            support=float(dense[3]-2 if grounded else legacy_anchor)
            actions.append(({
                'id':pose_id,'key':f'action-{clip}-{frame:03}.png',
                'label':f'{clip} frame {frame}','clip':clip,'frame':frame,
                'source':source,'sha256':digest,'sourceLegacySHA256':digest,
                'bounds':[visible[0],visible[1],visible[2]-visible[0],visible[3]-visible[1]],
                'customAnchor':True,'anchorX':float(manifest['foot_anchor'][0]),
                'supportY':support,'renderWidthAtSmall':image.width*old_scale,
                'nativeWidth':image.width,'nativeHeight':image.height,
                'grounded':grounded,'airborne':not grounded,'headScaleCorrection':1.0,
                'styleOriginalRestored':True,'sourcePixelsEdited':False,
                'legacyAnchorY':legacy_anchor,
                'notes':'V7 original style restoration: source PNG byte-exact; original warm coat, fine straight fur and smaller paws restored. Original timing/root; actual grounded paw support.'
            },data))
        clips.append({'clip':clip,'frames':16,'seconds':original_clip['seconds'],'loop':original_clip.get('loop',False)})
    assert len(actions)==32
    return actions,clips

if __name__=='__main__':
    actions,clips=build_actions()
    report={'candidateVersion':7,'developmentOnly':True,'originalFramesRestored':32,
            'sourcePixelsEdited':False,'runtimeIntegrated':False,
            'generatorUsedForRestoration':False,
            'styleReferences':['art/frames/sit/000.png','art/frames/groom/000.png','art/frames/stretch/000.png'],
            'photoReferences':['local-data/Odin-original/references/reference-01.jpg','local-data/reference-review/set1/IMG_7680.jpg'],
            'anatomyCandidatesSeparate':True,
            'caveat':'Original restoration removes v4/v5 style drift; original midflight contour is still fairly straight. Separate precise anatomy drafts require visual review.',
            'clips':clips,'frames':[entry for entry,data in actions]}
    dest=ROOT/'planning/jump-style-consistency-v7.json'
    dest.write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({'passed':True,'originalFrames':len(actions),'report':str(dest)}))
