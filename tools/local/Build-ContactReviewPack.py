"""Package numbered review sources byte-for-byte at the current display scales."""
from pathlib import Path
import hashlib,json,math,zipfile
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'local-data/reference-review/approved-contact-v1';OUT.mkdir(parents=True,exist_ok=True)
config=json.loads((ROOT/'planning/state-head-review.json').read_text())
override_path=ROOT/'planning/queued-art-edits-v3.json'
queued_overrides=json.loads(override_path.read_text()) if override_path.exists() else {}
motion_overrides={r['key']:r for r in queued_overrides.get('motionOverrides',[])}
assert [p['id'] for p in config['poses']]==[1,2,3,4,6,7,8,12,13,14,15,16,17]
def entry(pose,key):
 data=(ROOT/pose['source']).read_bytes();digest=hashlib.sha256(data).hexdigest()
 assert digest==pose['sourceSHA256'],pose['source']
 image=Image.open(ROOT/pose['source']).convert('RGBA');bounds=image.getchannel('A').point(lambda a:255 if a>8 else 0).getbbox()
 assert bounds and 0<pose['supportY']<image.height
 record={'id':pose['id'],'key':key+'.png','label':f"{pose['id']}. {pose['label']}",'sha256':digest,'source':pose['source'],'bounds':[bounds[0],bounds[1],bounds[2]-bounds[0],bounds[3]-bounds[1]],'supportY':pose['supportY'],'renderWidthAtSmall':image.width*pose['scaleAtSmall'],'airborne':pose['airborne'],'notes':pose['status'],'nativeWidth':image.width,'nativeHeight':image.height}
 for field in ('customAnchor','anchorX'):
  if field in pose:record[field]=pose[field]
 return record,data
entries=[entry(p,f"state-{p['id']:02}") for p in config['poses']]
reference,reference_data=entry(config['poses'][0],'reference')
blink_config=json.loads((ROOT/'planning/blink-reconciliation-v1.json').read_text())
assert blink_config['artApproved']
blink_entries=[]
for frame in blink_config['frames']:
 pose_id={'sit':1,'loaf':2,'lay':3}[frame['family']]
 pose=dict(next(p for p in config['poses'] if p['id']==pose_id))
 pose.update(source=frame['candidate'],sourceSHA256=frame['sha256'])
 record,data=entry(pose,f"blink-{pose_id}-{frame['frame']}")
 record['eyeFrame']=frame['frame'];blink_entries.append((record,data))
motion_entries=[]
motion_configs=[('gaze',ROOT/'planning/gaze-reconciliation-v1.json',True),('tail',ROOT/'planning/tail-reconciliation-v1.json',True)]
vertical=ROOT/'planning/vertical-gaze-reconciliation-v1.json'
if vertical.exists():motion_configs.append(('gaze',vertical,False))
diagonal=ROOT/'planning/diagonal-gaze-reconciliation-v4.json'
if diagonal.exists():
 diagonal_config=json.loads(diagonal.read_text())
 if len(diagonal_config.get('frames',[]))==12:motion_configs.append(('gaze',diagonal,False))
for kind,config_path,approval_required in motion_configs:
 motion_config=json.loads(config_path.read_text())
 if approval_required:assert motion_config['artApproved']
 for frame in motion_config['frames']:
  pose_id={'sit':1,'loaf':2,'lay':3,'sleep':4}[frame['family']]
  pose=dict(next(p for p in config['poses'] if p['id']==pose_id))
  pose.update(source=frame['source'],sourceSHA256=frame['sha256'])
  key=f"{kind}-{pose_id}-{frame['phase']}"
  override=motion_overrides.get(key+'.png')
  if override:pose.update(source=override['source'],sourceSHA256=override['sha256'])
  record,data=entry(pose,key)
  approved=bool(frame.get('approved',motion_config.get('artApproved',False)))
  record.update(kind=kind,phase=frame['phase'],label=f"{kind.title()}: {frame['phase']}"+('' if approved else ' (candidate)'))
  if override:record['label']+=' (muzzle revision)'
  motion_entries.append((record,data))
v8_path=ROOT/'planning/owner-corrections-v8.json'
if v8_path.exists() and json.loads(v8_path.read_text()).get('enabled'):
 # The separate review is static. DEV uses registered ORIGINAL frame layers;
 # never composite the rejected1254-canvas drafts onto original1024 bodies.
 blink_entries=[];motion_entries=[]
for record,data in entries:
 boxes=[record['bounds']]
 for layer,layer_data in motion_entries:
  if layer['id']!=record['id']:continue
  b=layer['bounds']
  sx=record['nativeWidth']/layer['nativeWidth'];sy=record['nativeHeight']/layer['nativeHeight']
  assert record['id']==1 or (sx==1 and sy==1),'Unexpected motion canvas mismatch'
  # Runtime normalizes sitting drafts to the native original base; preserve a
  # small interpolation margin when mapping their conservative union bounds.
  if sx!=1 or sy!=1:
   left=max(0,math.floor(b[0]*sx)-2);top=max(0,math.floor(b[1]*sy)-2)
   right=min(record['nativeWidth'],math.ceil((b[0]+b[2])*sx)+2)
   bottom=min(record['nativeHeight'],math.ceil((b[1]+b[3])*sy)+2)
   b=[left,top,right-left,bottom-top]
  boxes.append(b)
 left=min(b[0] for b in boxes);top=min(b[1] for b in boxes)
 right=max(b[0]+b[2] for b in boxes);bottom=max(b[1]+b[3] for b in boxes)
 record['motionBounds']=[left,top,right-left,bottom-top]
manifest={'reference':reference,'poses':[r for r,data in entries],'blinks':[r for r,data in blink_entries],'motions':[r for r,data in motion_entries],'runtimeIntegrated':False,'sizeApprovalPending':True,'reviewVersion':10 if len(motion_entries)==30 else (9 if override_path.exists() else (8 if vertical.exists() else 7)),'animation':False,'sourceConfig':'planning/state-head-review.json'}
with zipfile.ZipFile(OUT/'contact-review.zip','w',zipfile.ZIP_DEFLATED) as archive:
 archive.writestr('manifest.json',json.dumps(manifest));archive.writestr(reference['key'],reference_data)
 for record,data in entries+blink_entries+motion_entries:archive.writestr(record['key'],data)
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('Packaged 13 active numbered states; belly-up, side sleep, curled sleep excluded and tucked sleep parked; PNG hashes verified.')
