"""Compare unretouched source and candidate ear crops; never alter sprite assets."""
from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
DRAFT = ROOT / 'draft-assets/legacy-ear-tufts-v1'
OUT = ROOT / 'local-data/reference-review/legacy-ear-tufts-v1'
OUT.mkdir(parents=True, exist_ok=True)
review = json.loads((DRAFT / 'review.json').read_text())
regions = {
    'sit-front': (485, 610, 800, 835),
    'loaf': (590, 720, 960, 965),
    'lying-awake': (650, 715, 960, 960),
    'sleeping': (695, 840, 975, 1060),
    'sit-rear': (625, 55, 1010, 340),
    'sit-lookback': (730, 40, 1180, 405),
    'sit-side': (770, 10, 1160, 385),
    'belly-flat': (1510, 190, 1983, 625),
}
font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 21)
bold = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 26)
groups = [('resting', list(regions)[:4]), ('other-approved', list(regions)[4:])]
for name, keys in groups:
    for background in ('dark', 'light'):
        bg = '#252d38' if background == 'dark' else '#e1e6ee'
        fg = 'white' if background == 'dark' else '#172332'
        board = Image.new('RGBA', (1200, 1320), bg)
        draw = ImageDraw.Draw(board)
        draw.text((20, 12), 'Odin ear tips - source / new draft', font=bold, fill=fg)
        draw.text((20, 49), 'Original hair/alpha retained. Diagnostic crops only; no changes to saved sprites.', font=font, fill=fg)
        for row, key in enumerate(keys):
            entry = review['poses'][key]
            y = 90 + row * 305
            source_size = Image.open(ROOT / entry['source']).size
            region = regions[key]
            scale = min(540 / (region[2] - region[0]), 245 / (region[3] - region[1]))
            for col, field in enumerate(('source', 'candidate')):
                image = Image.open(ROOT / entry[field]).convert('RGBA')
                # Normalize crop coordinates only if the tool changed the canvas dimensions.
                box = tuple(round(value * image.size[index % 2] / source_size[index % 2])
                            for index, value in enumerate(region))
                crop = image.crop(box)
                crop = crop.resize((round((region[2] - region[0]) * scale),
                                    round((region[3] - region[1]) * scale)), Image.Resampling.LANCZOS)
                x = col * 600 + (600 - crop.width) // 2
                board.alpha_composite(crop, (x, y + 37))
                draw = ImageDraw.Draw(board)
                draw.text((col * 600 + 18, y), key + (' / source' if col == 0 else ' / ear draft'), font=font, fill=fg)
        board.convert('RGB').save(OUT / ('ear-review-' + name + '-' + background + '.png'))
print('Rendered before/after ear comparisons: ' + str(OUT))
