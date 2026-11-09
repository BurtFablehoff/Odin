"""Diagnostic comparison of anatomy candidates; never modifies source sprites."""
from pathlib import Path
import hashlib, io, json, zipfile
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = Path('C:/Users/hattd/Documents/Codex/2026-11-06/d/outputs')
OUT.mkdir(parents=True, exist_ok=True)
FONT = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 28)
SMALL = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 19)

def bounds(image):
    box = image.getchannel('A').point(lambda a: 255 if a >= 8 else 0).getbbox()
    assert box is not None
    assert box[0] > 0 and box[1] > 0 and box[2] < image.width and box[3] < image.height
    return (max(0, box[0]-8), max(0, box[1]-8),
            min(image.width, box[2]+8), min(image.height, box[3]+8))

records = []
with zipfile.ZipFile(ROOT/'local-data/reference-review/dev-latest-art/dev-artwork.zip') as pack:
    manifest = json.loads(pack.read('manifest.json'))
    for clip in ('jump_left', 'jump_right'):
        corrections = {}
        qa_path = ROOT/f'draft-assets/jump-anatomy-v5/{clip}/qa.json'
        if qa_path.exists():
            corrections = {f['frame']: float(f.get('reviewScaleCorrection', 1))
                           for f in json.loads(qa_path.read_text())['frames']}
        pairs = []
        for frame in range(4, 9):
            entry = next(p for p in manifest['actions'] if p['clip'] == clip and p['frame'] == frame)
            raw = pack.read(entry['key'])
            assert hashlib.sha256(raw).hexdigest() == entry['sha256']
            old = Image.open(io.BytesIO(raw)).convert('RGBA')
            candidate = ROOT/f'draft-assets/jump-anatomy-v5/{clip}/{frame:03}.png'
            new = Image.open(candidate).convert('RGBA')
            # Uniform native-canvas scaling preserves existing sizes when canvases match.
            # Candidates with changed subject scale need explicitly reviewed landmarks first.
            scale = entry['renderWidthAtSmall']/entry['nativeWidth']
            pairs.append((frame, old, new, scale, candidate))
        zoom = min(1.5, min(420/((bounds(im)[2]-bounds(im)[0])*s*c)
                           for f, old, new, s, _ in pairs for im,c in ((old,1),(new,corrections.get(f,1)))),
                   min(340/((bounds(im)[3]-bounds(im)[1])*s*c)
                       for f, old, new, s, _ in pairs for im,c in ((old,1),(new,corrections.get(f,1)))))
        sheet = Image.new('RGB', (2400, 1000), '#202a36')
        draw = ImageDraw.Draw(sheet)
        draw.text((18, 10), 'Odin — '+clip.replace('_', ' ').title()+' — anatomy revision', font=FONT, fill='white')
        draw.text((18, 52), 'Top: current build | Bottom: revised candidates | Numbers match the jump review. Same zoom; no body-width normalization.', font=SMALL, fill='#c2d0dd')
        for col, (frame, old, new, scale, candidate) in enumerate(pairs):
            for row, im in enumerate((old, new)):
                x, y = col*480, 100+row*450
                draw.rectangle((x+4, y+4, x+475, y+445), fill='#273340', outline='#3d4d5e')
                draw.text((x+14, y+10), str(frame+1)+' — '+('current' if row == 0 else 'revised'), font=FONT, fill='#ffb05e')
                box = bounds(im)
                crop = im.crop(box)
                correction = corrections.get(frame, 1) if row else 1
                crop = crop.resize((round(crop.width*scale*zoom*correction), round(crop.height*scale*zoom*correction)), Image.Resampling.LANCZOS)
                assert crop.width <= 420 and crop.height <= 341
                sheet.paste(crop, (x+(480-crop.width)//2, y+70+(350-crop.height)//2), crop)
            records.append({'clip': clip, 'reviewNumber': frame+1, 'sourceFrame': frame,
                            'candidate': str(candidate.relative_to(ROOT)),
                            'sha256': hashlib.sha256(candidate.read_bytes()).hexdigest(),
                            'reviewScaleCorrection': corrections.get(frame, 1),
                            'nativeSize': list(new.size), 'sourcePixelsModifiedByReview': False})
        path = OUT/f'Odin-{clip.replace("_", "-")}-anatomy-v5.png'
        sheet.save(path)
        print(path)
(OUT/'Odin-jump-anatomy-v5-review.json').write_text(json.dumps({'candidateOnly': True, 'runtimeUnchanged': True, 'frames': records}, indent=2)+'\n')
