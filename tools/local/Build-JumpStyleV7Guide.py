"""Diagnostic only: mark surgical anatomy region; never edit source artwork."""
from pathlib import Path
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'local-data/reference-review/style-consistency-v7'
OUT.mkdir(parents=True,exist_ok=True)
im=Image.new('RGBA',(1024,1024),(220,220,220,255));im.alpha_composite(Image.open(ROOT/'art/frames/jump_left/006.png').convert('RGBA'))
d=ImageDraw.Draw(im)
roi=[(468,423),(562,402),(650,445),(726,516),(762,630),(704,645),(622,602),(520,540),(462,485)]
d.line(roi+[roi[0]],fill=(224,130,0,255),width=3)
line=[(450,474),(488,488),(530,490),(573,482),(609,485),(639,509),(664,554),(703,605)]
d.line(line,fill=(0,145,170,255),width=5)
d.text((360,740),'ORANGE: only this anatomy patch may change',fill=(0,0,0,255))
d.text((360,760),'TEAL: desired natural concave waist / separate thigh',fill=(0,0,0,255))
im.convert('RGB').save(OUT/'jump-left006-surgical-guide.png')
