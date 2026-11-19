"""Internal contact sheet/playback of unmodified drawings at manifest registration.

This is a diagnostic render, not authored animation artwork or owner approval.
"""
import json
import sys
from io import BytesIO
from pathlib import Path
from zipfile import ZipFile
from PIL import Image, ImageDraw

root=Path(__file__).resolve().parents[2]
work=root/'build'/(sys.argv[1] if len(sys.argv)>1 else 'geometry-motion-v10')
with ZipFile(work/'candidate.zip') as z:
    manifest=json.loads(z.read('manifest.json'))
    sheet=Image.new('RGB',(360*5,260*6),'#292d35')
    draw=ImageDraw.Draw(sheet)
    for route_index,clip in enumerate(('sit_to_loaf','sit_to_lay','loaf_to_lay')):
        frames=[]
        entries=sorted((e for e in manifest['actions'] if e['clip']==clip),key=lambda e:e['frame'])
        for i,e in enumerate(entries):
            source=Image.open(BytesIO(z.read(e['key']))).convert('RGBA')
            scale=e['renderWidthAtSmall']/source.width
            guide_scale=scale/.46875
            guide=source.transform((1024,1024),Image.Transform.AFFINE,(1/guide_scale,0,e['anchorX']-512/guide_scale,0,1/guide_scale,e['supportY']-944/guide_scale),Image.Resampling.BICUBIC)
            guide_dir=work/'guides';guide_dir.mkdir(exist_ok=True)
            guide.save(guide_dir/f'{clip}-{i}.png')
            matrix=(1/scale,0,e['anchorX']-175/scale,0,1/scale,e['supportY']-235/scale)
            sprite=source.transform((360,260),Image.Transform.AFFINE,matrix,Image.Resampling.BICUBIC)
            frame=Image.new('RGB',(360,260),'#292d35')
            frame.paste(sprite,mask=sprite.getchannel('A'))
            d=ImageDraw.Draw(frame);d.line((0,235,360,235),fill='#808080')
            d.text((8,8),f'{clip} {i}: {e["renderWidthAtSmall"]:.1f}',fill='white')
            sheet.paste(frame,((i%5)*360,(route_index*2+i//5)*260))
            frames.append(frame)
        durations=[max(20,int(v*1000)) for v in next(c for c in manifest['actionClips'] if c['clip']==clip)['frameSeconds']]
        durations[0]=700;durations[-1]=700
        cycle=frames+frames[-2:0:-1]
        timing=durations+durations[-2:0:-1]
        cycle[0].save(work/f'{clip}-internal.gif',save_all=True,append_images=cycle[1:],duration=timing,loop=0)
    sheet.save(work/'contact-sheet.png')
