"""Explicit final DEV art/registration override for the owner's v10 corrections."""
from pathlib import Path
import hashlib
import io
import json
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]


def settings():
    path=ROOT/'planning/owner-corrections-v10.json'
    value=json.loads(path.read_text()) if path.exists() else {}
    return value if value.get('enabled') else None


def read_source(override):
    data=(ROOT/override['source']).read_bytes()
    assert hashlib.sha256(data).hexdigest()==override['sourceSHA256'],override['source']
    with Image.open(io.BytesIO(data)) as image:
        assert image.mode=='RGBA'
        bounds=image.getchannel('A').point(lambda a:255 if a>8 else 0).getbbox()
        size=image.size
    assert bounds and 0<bounds[0] and 0<bounds[1] and bounds[2]<size[0] and bounds[3]<size[1],override['source']
    return data,size,[bounds[0],bounds[1],bounds[2]-bounds[0],bounds[3]-bounds[1]]


def apply_config(config):
    policy=settings()
    if not policy:return config
    for override in policy['poseOverrides']:
        read_source(override)
        pose=next(p for p in config['poses'] if p['id']==override['id'])
        for key in ('source','sourceSHA256','supportY','scaleAtSmall','headCrop','anchorX'):
            pose[key]=override[key]
        pose['customAnchor']=True
        pose['status']=override['description']+'; development integration requested, owner visual review pending'
    config['ownerCorrections']='planning/owner-corrections-v10.json'
    config['headSizeReference']['rule']='Use solid ear pinna, skull/face core and forward eye/nose/mouth landmarks. Never normalize by full image, tail or ear-to-floor height.'
    return config


def apply_pack(manifest,payloads):
    policy=settings()
    if not policy:return manifest,payloads
    registration=json.loads((ROOT/policy['gazeRegistration']).read_text())
    for override in policy['poseOverrides']:
        data,size,bounds=read_source(override)
        pose=next(p for p in manifest['poses'] if p['id']==override['id'])
        pose.update(source=override['source'],sha256=override['sourceSHA256'],bounds=bounds,
                    nativeWidth=size[0],nativeHeight=size[1],supportY=override['supportY'],
                    renderWidthAtSmall=size[0]*override['scaleAtSmall'],customAnchor=True,
                    anchorX=override['anchorX'],headCrop=override['headCrop'],
                    notes=override['description']+'; owner-requested development integration',
                    ownerArtworkApproved=False,anatomicalScaleGuide=policy['sizingGuides'])
        pose.pop('motionBounds',None)
        pose['label']=override['label']
        payloads[pose['key']]=data
        if override['id']==1:
            neutral=next(p for p in manifest['actions'] if p['clip']=='sit' and p['frame']==0)
            for field in ('source','sha256','bounds','nativeWidth','nativeHeight','supportY','renderWidthAtSmall','customAnchor','anchorX'):
                neutral[field]=pose[field]
            neutral['rightFurEdgeRepair']=True
            payloads[neutral['key']]=data
    for pose in manifest['poses']:
        if pose['id']<=4:
            family={1:'sit',2:'loaf',3:'lay',4:'sleep'}[pose['id']]
            pose['motionBounds']=registration['familyVisibleBounds'][family]
    manifest['ownerCorrectionsV10']=policy
    manifest['headSourceRegistrationVersion']=10
    manifest['completeStyleCorrection']=False
    return manifest,payloads
