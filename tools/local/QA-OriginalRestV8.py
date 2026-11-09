"""Inspect actual C# fixed-body exports at a common native crop; no sprite edits."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import math

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'local-data/reference-review/original-rest-v8'
FONT=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',17)
for family,box,count in [('sit',(335,480,715,790),19),('loaf',(400,515,850,915),12),('lay',(440,540,850,915),14)]:
    items=[('Original neutral',ROOT/f'art/frames/{family}/000.png')]+[(f'Fixed body {family} {frame:03d}',OUT/f'rendered/look_{family}/{frame:03d}.png') for frame in range(count)]
    w,h=box[2]-box[0],box[3]-box[1]+35
    board=Image.new('RGB',(w*5,h*math.ceil(len(items)/5)),(47,51,58));draw=ImageDraw.Draw(board)
    for i,(label,path) in enumerate(items):
        image=Image.open(path).convert('RGBA').crop(box)
        x,y=i%5*w,i//5*h
        board.paste(image,(x,y+35),image);draw.text((x+5,y+5),label,font=FONT,fill='white')
    board.save(OUT/f'fixed-{family}-heads.png')
print('Rendered original-rest-v8 fixed-body head comparison sheets.')
