"""Preview untouched draft sprites against both support backgrounds."""
import json
import argparse
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser()
parser.add_argument('--ear-drafts', action='store_true')
parser.add_argument('--size', type=int, choices=(480, 640, 800), default=480)
args = parser.parse_args()
DRAFT = ROOT / 'draft-assets/approved-resting-contact-v1'
OUT = ROOT / ('local-data/reference-review/legacy-ear-tufts-v1' if args.ear_drafts
              else 'local-data/reference-review/legacy-floor-smoosh-v1')
OUT.mkdir(parents=True, exist_ok=True)
review = json.loads((DRAFT / 'approval.json').read_text())
if args.ear_drafts:
    ears = json.loads((ROOT / 'draft-assets/legacy-ear-tufts-v1/review.json').read_text())
    for key, ear_key in [('sit', 'sit-front'), ('loaf', 'loaf'),
                         ('lay', 'lying-awake'), ('sleep', 'sleeping')]:
        review['drafts'][key]['source'] = ears['poses'][ear_key]['candidate']
font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 19)
bold = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 25)
labels = {'sit': 'Sitting', 'loaf': 'Loaf', 'lay': 'Lying awake',
          'sleep': 'Sleeping - tail overhang allowed'}

for context in ('taskbar', 'window'):
    tile_height = max(365, round(args.size * .6) + 100)
    board = Image.new('RGBA', (1500, 80 + tile_height * 2), '#121820')
    draw = ImageDraw.Draw(board)
    draw.text((20, 10), 'Odin - ' + ('ear drafts' if args.ear_drafts else 'approved floor contact') + ' / ' + context + ' / size ' + str(args.size), font=bold, fill='white')
    draw.text((20, 46), 'Cyan = support plane. Paws may overhang; sleeping tail may also overhang.',
              font=font, fill='#b9c9d8')
    for index, key in enumerate(('sit', 'loaf', 'lay', 'sleep')):
        entry = review['drafts'][key]
        tile = Image.new('RGBA', (750, tile_height), '#222a34')
        td = ImageDraw.Draw(tile)
        td.text((18, 10), labels[key], font=bold, fill='white')
        floor = tile_height - 58
        td.rectangle((0, floor, 750, tile_height), fill='#151b24' if context == 'taskbar' else '#dfe5ee')
        td.line((0, floor, 750, floor), fill='#5fddd5', width=2)
        if context == 'taskbar':
            for x in range(30, 730, 42):
                td.rounded_rectangle((x, floor + 20, x + 17, floor + 37), radius=4, fill='#53647c')
        else:
            for x in (640, 680, 720):
                td.rectangle((x - 3, floor + 21, x + 3, floor + 22), fill='#5c6c82')
        im = Image.open(ROOT / entry['source']).convert('RGBA')
        bounds = im.getchannel('A').point(lambda a: 255 if a > 64 else 0).getbbox()
        scale = entry['previewRenderSize'] / 480 * args.size / im.width
        x = round(375 - (bounds[0] + bounds[2]) / 2 * scale)
        y = round(floor - entry['supportY'] * scale)
        resized = im.resize((round(im.width * scale), round(im.height * scale)), Image.Resampling.LANCZOS)
        tile.alpha_composite(resized, (x, y))
        board.alpha_composite(tile, ((index % 2) * 750, 80 + (index // 2) * tile_height))
    suffix = '-' + str(args.size) if args.size != 480 else ''
    board.convert('RGB').save(OUT / ('legacy-floor-smoosh-' + context + suffix + '.png'))
print('Rendered draft contact previews: ' + str(OUT))
