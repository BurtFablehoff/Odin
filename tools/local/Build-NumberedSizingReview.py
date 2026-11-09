"""Render the numbered review at real relative display scales; never alter source art."""
from pathlib import Path
import hashlib
import json
import math
import shutil
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
CONFIG = ROOT / 'planning/state-head-review.json'
FEEDBACK = ROOT / 'planning/numbered-sizing-feedback-v3.json'
if not FEEDBACK.exists():FEEDBACK = ROOT / 'planning/numbered-sizing-feedback-v2.json'
VERSION = FEEDBACK.stem.rsplit('-',1)[-1]
OUTPUT = ROOT / f'local-data/reference-review/numbered-sizing-{VERSION}'
OUTPUT.mkdir(parents=True, exist_ok=True)
config = json.loads(CONFIG.read_text())
feedback = json.loads(FEEDBACK.read_text())
poses = config['poses']
assert [p['id'] for p in poses] == [1,2,3,4,6,7,8,12,13,14,15,16,17]
width, height, header, floor = 760, 500, 120, 415
board = Image.new('RGBA', (width*4, header+height*4), '#131b23')
draw = ImageDraw.Draw(board)
font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 25)
bold = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 32)
title = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 42)
draw.text((24,13), 'Odin - numbered sizing review '+VERSION, font=title, fill='white')
draw.text((24,73), 'Revised relative sizes | Cyan = support plane | #5, #9 and #10 removed | Reply: number + pass / bigger / smaller', font=font, fill='#b7c9d9')
hashes = {}
for index, pose in enumerate(poses):
    path = ROOT / pose['source']
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    assert digest == pose['sourceSHA256'], pose['label']
    hashes[pose['id']] = digest
    image = Image.open(path).convert('RGBA')
    bounds = image.getchannel('A').point(lambda a: 255 if a > 64 else 0).getbbox()
    scale = pose['scaleAtSmall']
    tile = Image.new('RGBA', (width,height), '#25313e')
    text = ImageDraw.Draw(tile)
    text.text((22,17), f"{pose['id']}. {pose['label']}", font=bold, fill='white')
    change = feedback['changes'].get(str(pose['id']))
    note = pose['status'] if config.get('ownerCorrections') else change['caption'] if change else 'Size retained'
    text.text((22,66), note, font=font, fill='#b3c7d9')
    if not pose['airborne']:
        text.rectangle((0,floor,width,height), fill='#dfe5ee')
        text.line((0,floor,width,floor), fill='#54d9d0', width=2)
    rendered = image.resize((round(image.width*scale),round(image.height*scale)), Image.Resampling.LANCZOS)
    x = round(width/2-(bounds[0]+bounds[2])/2*scale)
    y = round(floor-pose['supportY']*scale)
    # Check all visible hair, not just the dense silhouette, against panel boundaries.
    visible = image.getchannel('A').point(lambda a: 255 if a > 8 else 0).getbbox()
    assert x+visible[0]*scale >= 4 and x+visible[2]*scale <= width-4, pose['label']
    assert y+visible[1]*scale >= 105 and y+visible[3]*scale <= height-4, pose['label']
    tile.alpha_composite(rendered,(x,y))
    board.alpha_composite(tile,((index%4)*width,header+(index//4)*height))
destination = OUTPUT / f'Odin-numbered-sizing-sheet-{VERSION}.png'
board.convert('RGB').save(destination)
for pose in poses:
    assert hashlib.sha256((ROOT/pose['source']).read_bytes()).hexdigest() == hashes[pose['id']]
(OUTPUT/'review.json').write_text(json.dumps({'reviewOnly':True,'config':str(CONFIG.relative_to(ROOT)),'feedback':str(FEEDBACK.relative_to(ROOT)),'poses':poses,'sourceHashesVerified':True,'noSourcePixelsModified':True},indent=2)+'\n')
deliverables = Path('C:/Users/hattd/Documents/Codex/2026-11-06/d/outputs')
deliverables.mkdir(parents=True,exist_ok=True)
shutil.copy2(destination,deliverables/destination.name)
print(destination)
print('Verified 13 active IDs, source hashes, transparent image bounds, and no clipping.')
