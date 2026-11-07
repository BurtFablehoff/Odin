"""Diagnostic measurements/sheets only; never modifies source or runtime artwork."""
from pathlib import Path
from PIL import Image,ImageDraw
import json,math,hashlib
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'local-data/reference-review/consistency-v10';OUT.mkdir(parents=True,exist_ok=True)
FOLDER=ROOT/'draft-assets/consistency-v10/rear'
faces={
 'original-front':{'source':'art/frames/sit/000.png','eye':[496,615],'missingEye':[552,619],'nose':[525,642],'mouth':[526,659]},
 'jump-right000':{'source':'art/frames/jump_right/000.png','eye':[507,310],'nose':[539,328],'mouth':[539,347]},
 'jump-right013':{'source':'art/frames/jump_right/013.png','eye':[522,306],'nose':[554,319],'mouth':[557,336]},
 'jump-right014':{'source':'art/frames/jump_right/014.png','eye':[516,303],'nose':[550,319],'mouth':[550,335]},
 'jump-right015':{'source':'art/frames/jump_right/015.png','eye':[508,305],'nose':[539,321],'mouth':[538,337]},
 'side-v9-Persian':{'source':'draft-assets/style-consistency-v9/side/side-fuller-persian.png','eye':[754,748],'nose':[789,762],'mouth':[790,788]}}
for data in faces.values():
 data['eyeNoseDistance']=math.dist(data['eye'],data['nose'])
 data['noseMouthDistance']=math.dist(data['nose'],data['mouth'])
 data['noseMouthToEyeNoseRatio']=data['noseMouthDistance']/data['eyeNoseDistance']
face_report={'manualLandmarks':True,'uncertaintyNativePixels':2,'ownerApprovedAnatomicalMeasurement':False,
 'notForWholeImageNormalization':True,'sourceCoordinates':faces,
 'bestModerateFlatOriginalReferences':['art/frames/jump_right/013.png','art/frames/jump_right/014.png'],
 'profileCaveat':'Original left000 has stronger side angle but functional eye is farther away; its eye-to-nose distance is perspective-foreshortened and unsuitable for scale calibration.',
 'v9Assessment':'Eye/nose distance remains similar to original rightprep faces, while nose-to-mouth is~26px versus16-19px in originals. Match a coherent skull/eye/nose/mouth core; do not rescale by tail, ear tufts, total image height or iris-color pixel area alone.'}
(ROOT/'planning/face-core-landmarks-v10.json').write_text(json.dumps(face_report,indent=2)+'\n')
# A common pinna/skull-core viewing scale, with no ear-to-floor normalization.
board=Image.new('RGBA',(1500,1000),(224,224,224,255));d=ImageDraw.Draw(board)
items=[('ORIGINAL FRONT - body master','art/frames/sit/000.png',1,0,0),
       ('CURRENT V8 REAR - pinna/core match','draft-assets/style-consistency-v7/focused-rear/rear-attempt2.png',.933,-57.5,-7.65),
       ('REAR BODY FIX - same front contour','draft-assets/consistency-v10/rear/rear.png',1,0,0)]
for i,(label,path,s,tx,ty) in enumerate(items):
 im=Image.open(ROOT/path).convert('RGBA')
 # Inverse affine is display-only; candidate PNGs are never written by this audit.
 fitted=im.transform((1024,1200),Image.Transform.AFFINE,(1/s,0,-tx/s,0,1/s,-ty/s),resample=Image.Resampling.BICUBIC)
 crop=fitted.crop((370,490,700,1120));crop=crop.resize((462,882),Image.Resampling.LANCZOS)
 board.alpha_composite(crop,(i*500+19,75));d.text((i*500+12,20),label,fill=(0,0,0,255))
 y=75+round((944-490)*1.4);d.line((i*500+15,y,i*500+485,y),fill=(140,60,140,255),width=1)
 d.text((i*500+15,y+4),'Original support944 (not realigned)',fill=(100,30,100,255))
d.text((15,970),'Diagnostic manual solid-pinna/skull-core registration. Body/tail/totalheight never used to match scale; tail mostly outside crop.',fill=(0,0,0,255))
board.convert('RGB').save(OUT/'front-rear-body-core-comparison.png')
front=Image.open(ROOT/'art/frames/sit/000.png').convert('RGBA');candidate=Image.open(FOLDER/'rear.png').convert('RGBA')
body_alpha_changes=sum(front.getpixel((x,y))[3]!=candidate.getpixel((x,y))[3] for y in range(700,960) for x in range(390,700))
tail_changes=sum(front.getpixel((x,y))!=candidate.getpixel((x,y)) for y in range(790,959) for x in range(220,390))
def right_flank(im,y):
 values=[x for x in range(500,700) if im.getpixel((x,y))[3]>64]
 return max(values) if values else None
width_samples=[{'y':y,'frontRightFlank':right_flank(front,y),'candidateRightFlank':right_flank(candidate,y),'bodyAxisApprox':527,'leftFlankNote':'Below tail overlap the left body edge is occluded; do not use total alpha bounding width.'} for y in [760,800,850,900]]
report={'passed':body_alpha_changes==0 and tail_changes==0,'bodyShapeMaster':'art/frames/sit/000.png',
 'candidate':'draft-assets/consistency-v10/rear/rear.png',
 'sha256':hashlib.sha256((FOLDER/'rear.png').read_bytes()).hexdigest(),
 'manualPinnaRegistration':{'uniformScale':.933,'generatedRearSourceTranslation':[-54.692,-37.579],
  'basis':'Solid-pinna/base and skull core, not fur-tip overall height','uncertaintyPixels':4,
  'frontPinnaBaseSelections':[[445,586],[486,575],[571,579],[610,591]],
  'currentV8RearPinnaBaseSelections':[[546,639],[594,616],[666,618],[699,639]],
  'scope':'Provisional visual landmarks, not owner-approved skeletal measurements'},
 'actualBodyVerification':{'exactOriginalAlphaRows':[700,959],'alphaChanges':body_alpha_changes,
   'freeTailChanges':tail_changes,'rightFlankSamples':width_samples,'contactExceptionRows':None},
 'floorSupportY':944,'badContactPatchRemoved':True,'roundedPawHockTipsRetained':True,
 'sourcePNGsUnmodified':True,'runtimeIntegrated':False,'ownerApproved':False,
 'comparison':'local-data/reference-review/consistency-v10/front-rear-body-core-comparison.png'}
(FOLDER/'sizing-landmarks-and-qa.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({'passed':report['passed'],'bodyAlphaChanges':body_alpha_changes,'freeTailChanges':tail_changes,'comparison':report['comparison']}))
