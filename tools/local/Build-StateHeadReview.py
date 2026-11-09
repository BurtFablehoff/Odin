"""Read-only atlas of current state sizes and head/face crops. No sprite edits."""
from pathlib import Path
import hashlib,json,math,textwrap
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'local-data/reference-review/all-states-heads-v1';OUT.mkdir(parents=True,exist_ok=True)
old_config=json.loads((ROOT/'planning/state-head-review.json').read_text())
old_ids={p['label']:p['id'] for p in old_config['poses']}
poses=[]
def add(label,source,scale,support,head,status,page,frame=None,airborne=False):
 p=ROOT/source;poses.append({'id':old_ids.get(label,len(poses)+1),'label':label,'source':source,'sourceSHA256':hashlib.sha256(p.read_bytes()).hexdigest(),'scaleAtSmall':scale,'supportY':support,'headCrop':head,'status':status,'page':page,'frame':frame,'airborne':airborne})
base=json.loads((ROOT/'draft-assets/approved-resting-contact-v1/approval.json').read_text());ears=json.loads((ROOT/'draft-assets/legacy-ear-tufts-v1/review.json').read_text())
head={'sit':[510,695,800,865],'loaf':[625,805,970,1065],'lay':[650,810,950,1060],'sleep':[690,920,965,1120]}
for key,ek,label in [('sit','sit-front','Sitting'),('loaf','loaf','Lying loaf'),('lay','lying-awake','Lying awake'),('sleep','sleeping','Sleeping')]:
 p=base['drafts'][key];add(label,ears['poses'][ek]['candidate'],p['previewRenderSize']/1254,p['supportY'],head[key],'Latest ear revision; floor contact approved','resting')
reg=json.loads((ROOT/'planning/rest-pose-registration.json').read_text())
for label,ek,regkey,headbox in [('Sitting away','sit-rear','sit-rear.png',[650,160,1000,380]),('Sitting look-back','sit-lookback','sit-lookback.png',[770,180,1160,460]),('Sitting side','sit-side','sit-side.png',[760,170,1160,490])]:
 old=next(x for x in reg['poses'] if x['key']==regkey);source=ears['poses'][ek]['candidate'];im=Image.open(ROOT/source);bounds=im.getchannel('A').point(lambda a:255 if a>64 else 0).getbbox()
 add(label,source,480/1024*255/old['scaleReference'],bounds[3]-1,headbox,'Latest ears; prior size estimate; contact provisional','resting')
sleep=json.loads((ROOT/'planning/sleeping-contact-review.json').read_text())
def area(points):return abs(sum(a[0]*b[1]-b[0]*a[1] for a,b in zip(points,points[1:]+points[:1])))/2
refarea=area(sleep['poses'][0]['headOutline'])
for key,label in [('tucked','Tucked sleep')]:
 p=next(x for x in sleep['poses'] if x['key']==key);xs=[x[0] for x in p['headOutline']];ys=[x[1] for x in p['headOutline']]
 add(label,p['source'],480/1024*math.sqrt(refarea/area(p['headOutline'])),p['supportY'],[min(xs),min(ys),max(xs),max(ys)],'Fur/contact approved; size pending' if key=='side' else 'Art approved; size pending','resting')
m=json.loads((ROOT/'art/manifest.json').read_text())
for key,frame,label,factor,headbox in [('groom',8,'Grooming',1,[345,550,565,715]),('stretch',14,'Stretching',1,[388,816,558,911]),('run_left',9,'Running left',1.5,[246,710,401,855]),('run_right',8,'Running right',1.88,[605,740,705,850]),('jump_left',8,'Jumping left',1,[340,250,515,390]),('jump_right',8,'Jumping right',1,[500,310,665,435])]:
 c=m['clips'][key];anchor=c['images'][frame].get('anchor_y') or c.get('anchor_y') or 950
 add(label,f'art/frames/{key}/{frame:03}.png',480*factor/1024,anchor,headbox,'Existing runtime art; ears/fur not reconciled','actions',frame=frame,airborne=key.startswith('jump'))
config={'title':'Odin - all current states and head sizing','reviewOnly':True,'displaySize':'Small / 480, current per-pose scale estimates retained','headLineup':'Every head crop shares the same extra 2x viewing zoom; no independent head normalization. Crop boxes are provisional visual selections, not approved anatomical area measurements.','breathing':'Removed','headSizeReference':{'id':6,'label':'Sitting away','rule':'Smallest head-size reference; keep its current scale fixed while reviewing others'},'removedStates':[{'id':5,'label':'Belly-up','reason':'Owner removed lying on back entirely'},{'id':9,'label':'Curled sleep','reason':'Owner rejected duplicate of tucked sleep'}],'poses':poses,'approvalPending':True}
feedback_path=ROOT/'planning/numbered-sizing-feedback-v2.json'
if feedback_path.exists():
 feedback=json.loads(feedback_path.read_text())
 baseline={p['id']:p for p in json.loads((ROOT/feedback['baseline']).read_text())['poses']}
 for p in poses:
  change=feedback['changes'].get(str(p['id']))
  if not change:continue
  p['scaleAtSmall']=baseline[p['id']]['scaleAtSmall']*change['multiplier']*change.get('sourceNormalization',1)
  for field in ['source','supportY','headCrop']:
   if field in change:p[field]=change[field]
  p['sourceSHA256']=hashlib.sha256((ROOT/p['source']).read_bytes()).hexdigest()
  p['status']=change['caption']+'; owner review pending'
 config['sizingFeedback']='planning/numbered-sizing-feedback-v2.json'
 config['headSizeReference']['rule']='Owner enlarged #6 in v2; earlier fixed-scale note superseded. Keep proportional whole-sprite scaling.'
new_feedback_path=ROOT/'planning/numbered-sizing-feedback-v3.json'
if new_feedback_path.exists():
 new_feedback=json.loads(new_feedback_path.read_text())
 new_baseline={p['id']:p for p in json.loads((ROOT/new_feedback['baseline']).read_text())['poses']}
 for p in poses:
  change=new_feedback['changes'].get(str(p['id']))
  if not change:continue
  p['scaleAtSmall']=new_baseline[p['id']]['scaleAtSmall']*change.get('multiplier',1)*change.get('sourceNormalization',1)
  for field in ['source','supportY','headCrop']:
   if field in change:p[field]=change[field]
  p['sourceSHA256']=hashlib.sha256((ROOT/p['source']).read_bytes()).hexdigest()
  p['status']=change['caption']+'; owner review pending'
 config['sizingFeedback']='planning/numbered-sizing-feedback-v3.json'
restore_path=ROOT/'planning/sitting-original-restore-v6.json'
restore=json.loads(restore_path.read_text()) if restore_path.exists() else {}
if restore.get('enabled'):
 replacement=restore['pose']
 assert replacement['id']==1 and replacement['source']=='art/frames/sit/000.png'
 assert hashlib.sha256((ROOT/replacement['source']).read_bytes()).hexdigest()==replacement['sourceSHA256']
 poses[poses.index(next(p for p in poses if p['id']==1))]=dict(replacement)
 config['sittingRestoration']='planning/sitting-original-restore-v6.json'
v8_path=ROOT/'planning/owner-corrections-v8.json'
if v8_path.exists():
 v8=json.loads(v8_path.read_text())
 if v8.get('enabled'):
  apply_v8=__import__('runpy').run_path(str(ROOT/'tools/local/Apply-OwnerCorrectionsV8.py'))['apply_config']
  config=apply_v8(config);poses=config['poses']
(ROOT/'planning/state-head-review.json').write_text(json.dumps(config,indent=2)+'\n')
bold=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',24);font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',17)
def load(p):return Image.open(ROOT/p['source']).convert('RGBA')
for page,title in [('resting','Resting poses: belly-up (#5), curled (#9) and side sleep (#10) removed'),('actions','12-17: Runtime action samples - existing artwork')]:
 selected=[p for p in poses if p['page']==page];cols=3 if page=='resting' else 2;w,h=(760 if page=='resting' else 600),500
 board=Image.new('RGBA',(cols*w,100+math.ceil(len(selected)/cols)*h),'#10161e');d=ImageDraw.Draw(board)
 d.text((18,10),title,fill='white',font=bold)
 d.text((18,47),'Current Small-size scales. Pose sizes are NOT fitted independently to their panels.',fill='#bbcbd8',font=font)
 d.text((18,72),'Cyan = proposed support; provisional contacts labelled. No floor line for airborne samples.',fill='#6ee0d5',font=font)
 for i,p in enumerate(selected):
  tile=Image.new('RGBA',(w,h),'#242d38');td=ImageDraw.Draw(tile);td.text((15,10),f"{p['id']}. {p['label']}",font=bold,fill='white')
  for row,line in enumerate(textwrap.wrap(p['status'],58)):td.text((15,44+row*20),line,font=font,fill='#b5c6d7')
  td.text((15,90),f"scale {p['scaleAtSmall']:.3f} px/source px"+(f" | frame {p['frame']}" if p['frame'] is not None else ''),font=font,fill='#91a7bb')
  floor=420
  if not p['airborne']:
   td.rectangle((0,floor,w,h),fill='#dfe5ee');td.line((0,floor,w,floor),fill='#5bddd3',width=2)
  im=load(p);b=im.getchannel('A').point(lambda a:255 if a>64 else 0).getbbox();s=p['scaleAtSmall']
  resized=im.resize((round(im.width*s),round(im.height*s)),Image.Resampling.LANCZOS)
  x=round(w/2-(b[0]+b[2])/2*s);y=round(floor-p['supportY']*s)
  if x+b[0]*s<0 or x+b[2]*s>w or y+b[1]*s<112 or y+b[3]*s>h:raise ValueError('Panel clips source or label: '+p['label'])
  tile.alpha_composite(resized,(x,y));board.alpha_composite(tile,((i%cols)*w,100+(i//cols)*h))
 board.convert('RGB').save(OUT/f'all-states-{page}.png')
# Keep one common viewing zoom, including complete ear/head context in the main lineup.
whole_heads={1:[480,620,820,900],2:[590,720,995,1065],3:[635,710,980,1050],4:[675,830,990,1135],5:[1500,245,1970,705],6:[615,55,1020,390],7:[730,40,1185,500],8:[750,10,1180,520],10:[1680,95,2169,550],11:[1050,250,1510,700],12:[330,510,590,725],13:[365,765,570,915],14:[235,650,420,845],15:[595,685,715,825],16:[325,205,525,385],17:[490,260,675,430]}
if feedback_path.exists():
 for key,change in feedback['changes'].items():
  if 'wholeHeadCrop' in change:whole_heads[int(key)]=change['wholeHeadCrop']
if new_feedback_path.exists():
 for key,change in new_feedback['changes'].items():
  if 'wholeHeadCrop' in change:whole_heads[int(key)]=change['wholeHeadCrop']
if restore.get('enabled'):whole_heads[1]=restore['wholeHeadCrop']
if config.get('ownerCorrections'):
 for p in poses:whole_heads[p['id']]=p['headCrop']

def head_board(name,whole):
 cols,w,h=4,340,340
 board=Image.new('RGBA',(cols*w,105+math.ceil(len(poses)/cols)*h),'#10161e');d=ImageDraw.Draw(board)
 d.text((18,10),('Whole heads' if whole else 'Facial crops')+' - existing relative sizes, common 2x viewing zoom',font=bold,fill='white')
 d.text((18,48),'Relative display sizes include the latest numbered feedback; heads share the same viewing zoom.',font=font,fill='#bbcbd8')
 d.text((18,73),'Selections show visual context; they are not approved anatomical head-area measurements.',font=font,fill='#bbcbd8')
 for i,p in enumerate(poses):
  tile=Image.new('RGBA',(w,h),'#242d38');td=ImageDraw.Draw(tile)
  td.text((12,8),f"{p['id']}. {p['label']}",font=font,fill='white')
  im=load(p);region=whole_heads[p['id']] if whole else p['headCrop'];crop=im.crop(tuple(region));scale=p['scaleAtSmall']
  td.text((12,33),'Same 2x viewing zoom',font=font,fill='#aebfd0')
  rendered=crop.resize((round(crop.width*scale*2),round(crop.height*scale*2)),Image.Resampling.LANCZOS)
  if rendered.width>w-12 or rendered.height>h-65:raise ValueError('Head card clips '+p['label'])
  tile.alpha_composite(rendered,((w-rendered.width)//2,65+(h-65-rendered.height)//2));board.alpha_composite(tile,((i%cols)*w,105+(i//cols)*h))
 board.convert('RGB').save(OUT/name)
head_board('all-heads-current-scale.png',True)
head_board('all-faces-current-scale.png',False)
for p in poses:assert hashlib.sha256((ROOT/p['source']).read_bytes()).hexdigest()==p['sourceSHA256']
print('Rendered',len(poses),'active states and common-scale head/face lineups; source hashes unchanged.')
