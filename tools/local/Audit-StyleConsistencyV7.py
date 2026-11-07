"""Diagnostic sheets and source inventory only; never modifies source artwork."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import hashlib, io, json, math, zipfile

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'local-data/reference-review/style-consistency-v7'
OUT.mkdir(parents=True, exist_ok=True)
FONT=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',17)

def sheet(items, name, cols=4, cell=(400,360)):
    w,h=cell
    canvas=Image.new('RGB',(cols*w,math.ceil(len(items)/cols)*h),(47,51,58))
    draw=ImageDraw.Draw(canvas)
    for i,(label,im) in enumerate(items):
        x=(i%cols)*w; y=(i//cols)*h
        im=im.convert('RGBA')
        alpha=im.getchannel('A')
        box=alpha.point(lambda a:255 if a>=8 else 0).getbbox()
        if box: im=im.crop(box)
        im.thumbnail((w-24,h-60),Image.Resampling.LANCZOS)
        canvas.paste(im,(x+(w-im.width)//2,y+40+(h-60-im.height)//2),im)
        draw.text((x+10,y+10),label,fill=(240,240,240),font=FONT)
    canvas.save(OUT/name)

with zipfile.ZipFile(ROOT/'local-data/reference-review/dev-latest-art/dev-artwork.zip') as z:
    manifest=json.loads(z.read('manifest.json'))
    sources=[]
    for kind in ('poses','blinks','motions','actions'):
        for row in manifest[kind]:
            p=ROOT/row['source']
            raw=p.read_bytes()
            sources.append(dict(kind=kind,source=row['source'],key=row['key'],sha256=hashlib.sha256(raw).hexdigest(),hashMatches=hashlib.sha256(raw).hexdigest()==row['sha256'],size=list(Image.open(io.BytesIO(raw)).size)))
    sheet([(p['label'],Image.open(io.BytesIO(z.read(p['key'])))) for p in manifest['poses']], 'current-stills.png')
    for clip in ('jump_left','jump_right'):
        sheet([(f"{clip} {p['frame']:03d}",Image.open(io.BytesIO(z.read(p['key'])))) for p in manifest['actions'] if p['clip']==clip],f'current-{clip}.png')
    sheet([(p['key'],Image.open(io.BytesIO(z.read(p['key'])))) for p in manifest['motions']], 'current-layers.png',4,(380,300))

legacy=json.loads((ROOT/'art/manifest.json').read_text())
legacy_sources=[]
for clip,data in legacy['clips'].items():
    for f in range(data['frames']):
        path=ROOT/f'art/frames/{clip}/{f:03d}.png'
        raw=path.read_bytes()
        legacy_sources.append(dict(clip=clip,frame=f,source=path.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(raw).hexdigest()))
for clip in ('groom','stretch','run_left','run_right','jump_left','jump_right'):
    samples=range(legacy['clips'][clip]['frames']) if clip in ('jump_left','jump_right','groom') else (0,legacy['clips'][clip]['frames']//3,2*legacy['clips'][clip]['frames']//3,legacy['clips'][clip]['frames']-1)
    sheet([(f'original {clip} {f:03d}',Image.open(ROOT/f'art/frames/{clip}/{f:03d}.png')) for f in samples],f'original-{clip}.png')
refs=json.loads((ROOT/'local-data/reference-review/fur-source-selection-v1/reference-index.json').read_text())
items=[]
for p in refs['photos']:
    im=Image.open(ROOT/p['source']).convert('RGBA').crop(p['previewCropOnly'])
    items.append((f"Photo {p['number']}: {p['label']}",im))
sheet(items,'real-references.png',3,(500,450))
sheet([(f"original {clip}",Image.open(ROOT/f'art/frames/{clip}/{f:03d}.png')) for clip,f in [('sit',0),('groom',5),('stretch',12)]], 'original-style-baselines.png',3,(600,650))
inventory={'headlessOnly':True,'artworkModified':False,'sheetNote':'Sprites are cropped by alpha>=8 and fitted for texture comparison; these sheets are not physical-size approval.','packCounts':{k:len(manifest[k]) for k in ('poses','blinks','motions','actions')},'packSourceCount':len(sources),'packUniqueSourceCount':len({p['source'] for p in sources}),'legacySourceCount':len(legacy_sources),'legacyCounts':{k:v['frames'] for k,v in legacy['clips'].items()},'packSources':sources,'legacySources':legacy_sources,'references':refs['photos'],'allSourceHashesMatch':all(p['hashMatches'] for p in sources)}
(OUT/'inventory.json').write_text(json.dumps(inventory,indent=2))
print(json.dumps({k:v for k,v in inventory.items() if k not in ('packSources','legacySources','references')},indent=2))

