"""Diagnostic gallery only: actual final PNGs at supplied physical scales and common2x viewing zoom."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,hashlib
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'local-data/reference-review/consistency-v10';OUT.mkdir(parents=True,exist_ok=True)
ZOOM=2.0;TILE=550;WIDTH=1650;HEIGHT=1240
FONT=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',18)
SMALL=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',15)
BOLD=ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf',20)
board=Image.new('RGBA',(WIDTH,HEIGHT),(224,224,224,255));draw=ImageDraw.Draw(board)
draw.text((18,15),'Odin v10 — actual final assets / DEV candidates for owner review',font=BOLD,fill=(35,35,35,255))
draw.text((18,44),'Supplied physical scales; common2x viewing zoom only. Heads and bodies are not independently normalized.',font=SMALL,fill=(55,55,55,255))
items=[
 {'label':'Original sitting — reference','source':'art/frames/sit/000.png','scale':.46875,'root':512,'support':944,'tile':[0,0],'rootDisplay':295,'plane':555},
 {'label':'Right-edge repair — DEV candidate','source':'draft-assets/consistency-v10/sit-edge/sit.png','scale':.46875,'root':512,'support':944,'tile':[1,0],'rootDisplay':295,'plane':555,'expected':'63a101e99163ffab8c99b9a093611bf9607a5834645cb3f15a5625c72bb069d2'},
 {'label':'Down gaze011 — actual runtime render','source':'local-data/reference-review/original-rest-v10/runtime-rendered/look_sit/011.png','scale':.46875,'root':512,'support':944,'tile':[2,0],'rootDisplay':295,'plane':555},
 {'label':'Rear sitting — DEV candidate','source':'draft-assets/consistency-v10/rear/rear.png','scale':.46875,'root':512,'support':944,'tile':[0,1],'rootDisplay':295,'plane':1125,'expected':'83be1425f413398847060637fc5a6ef9b50e67832b00cc847865f49d1eb6e2c3'},
 {'label':'Side / moderate Persian — DEV candidate','source':'draft-assets/consistency-v10/side/side.png','scale':.39923597194388777,'root':519,'support':1151,'tile':[1,1],'rootDisplay':262,'plane':1125,'expected':'055b3850a7eef9c1220badf95bee15781e5369652d4fd6f46c5f89c34e6a2183'}]
for row,plane in [(0,555),(1,1125)]:
 draw.rectangle((0,plane,WIDTH if row==0 else 1100,plane+35),fill=(65,65,65,255))
 draw.line((0,plane,WIDTH if row==0 else 1100,plane),fill=(135,70,135,255),width=1)
for item in items:
 path=ROOT/item['source'];digest=hashlib.sha256(path.read_bytes()).hexdigest();item['sha256']=digest
 assert not item.get('expected') or digest==item['expected'],item['source']
 im=Image.open(path).convert('RGBA');factor=item['scale']*ZOOM
 scaled=im.resize((round(im.width*factor),round(im.height*factor)),Image.Resampling.LANCZOS)
 x=round(item['tile'][0]*TILE+item['rootDisplay']-item['root']*factor);y=round(item['plane']-item['support']*factor)
 board.alpha_composite(scaled,(x,y));labelY=95 if item['tile'][1]==0 else 650
 draw.text((item['tile'][0]*TILE+14,labelY),item['label'],font=FONT,fill=(25,25,25,255))
 draw.text((item['tile'][0]*TILE+14,labelY+28),'scale %.8f / support%d'%(item['scale'],item['support']),font=SMALL,fill=(65,65,65,255))
draw.text((1118,650),'Face detail at the SAME2x view',font=FONT,fill=(25,25,25,255))
face_items=[('Original front','art/frames/sit/000.png',(390,510,670,735),.46875,(1118,730)),
            ('Moderate Persian','draft-assets/consistency-v10/side/side.png',(600,650,875,885),.39923597194388777,(1390,730))]
for label,path,crop,scale,pos in face_items:
 im=Image.open(ROOT/path).convert('RGBA').crop(crop);factor=scale*ZOOM
 im=im.resize((round(im.width*factor),round(im.height*factor)),Image.Resampling.LANCZOS)
 board.alpha_composite(im,pos);draw.text((pos[0],pos[1]-29),label,font=SMALL,fill=(45,45,45,255))
draw.text((1118,998),'Face crops only; no extra enlargement',font=SMALL,fill=(65,65,65,255))
draw.text((1118,1022),'or face-size normalization.',font=SMALL,fill=(65,65,65,255))
draw.text((18,1194),'Original files remain intact. DEV candidates shown here are not owner-approved artwork.',font=SMALL,fill=(50,50,50,255))
board.convert('RGB').save(OUT/'final-comparison.png')
(OUT/'final-comparison-sources.json').write_text(json.dumps({'diagnosticOnly':True,'commonViewingZoom':ZOOM,'bodyOrHeadIndependentNormalization':False,'ownerArtworkApproved':False,'sources':items},indent=2)+'\n')
print(OUT/'final-comparison.png')
