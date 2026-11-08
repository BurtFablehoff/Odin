"""V12 states sheet: the 7 active states from the built DEV pack at their small size, all standing
on one floor line (each pose's supportY on the line), left to right 1,2,3,4,6,7,8, with a guide at
sitting's ear-tip height so the rear/side seated-height match is visible.
Writes local-data/reference-review/v12/states.png."""
import io
import json
import zipfile
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
PACK = ROOT / 'local-data/reference-review/dev-latest-art/dev-artwork.zip'
OUT = ROOT / 'local-data/reference-review/v12/states.png'
ORDER = [1, 2, 3, 4, 6, 7, 8]
NAMES = {1: 'sit', 2: 'loaf', 3: 'lay', 4: 'sleep', 6: 'sitting away', 7: 'look-back', 8: 'sitting side'}
SEATED = (1, 6, 8)


def font(size):
    for name in ('segoeui.ttf', 'arial.ttf'):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            pass
    return ImageFont.load_default()


def main():
    pack = zipfile.ZipFile(PACK)
    manifest = json.loads(pack.read('manifest.json'))
    poses = {p['id']: p for p in manifest['poses']}
    tiles = []
    for pid in ORDER:
        pose = poses[pid]
        image = Image.open(io.BytesIO(pack.read(pose['key']))).convert('RGBA')
        scale = float(pose['renderWidthAtSmall']) / pose['nativeWidth']
        small = image.resize((round(image.width * scale), round(image.height * scale)), Image.LANCZOS)
        left, top, right, bottom = small.getbbox()
        alpha = np.asarray(image)[..., 3]
        ear_tip = int(np.nonzero((alpha >= 64).any(axis=1))[0].min())
        support = float(pose['supportY'])
        tiles.append({'id': pid, 'image': small.crop((left, top, right, bottom)), 'scale': scale,
                      'supportScreen': support * scale - top, 'width': pose['renderWidthAtSmall'],
                      'height': (support - ear_tip) * scale if pid in SEATED else None})
    gap, margin = 40, 40
    above = max(t['supportScreen'] for t in tiles)
    below = max(t['image'].height - t['supportScreen'] for t in tiles)
    width = margin * 2 + sum(t['image'].width for t in tiles) + gap * (len(tiles) - 1)
    floor = round(70 + above)
    sheet = Image.new('RGBA', (width, round(floor + below + 110)), (246, 244, 240, 255))
    draw = ImageDraw.Draw(sheet)
    title, label, small_font = font(22), font(17), font(14)
    draw.text((margin, 14), 'Odin v12 states at the small size - one floor line; rear #6 and side #8 matched '
                            'to sitting #1 by seated height (DEV, not approved)', fill=(20, 20, 20, 255), font=title)
    sit = next(t for t in tiles if t['id'] == 1)
    guide = floor - sit['height']
    for x in range(0, width, 12):
        draw.line((x, round(guide), x + 6, round(guide)), fill=(40, 110, 200, 255), width=1)
    draw.text((width - 270, round(guide) - 20), f"sit ear tip ({sit['height']:.1f} px above floor)",
              fill=(40, 110, 200, 255), font=small_font)
    x = margin
    for t in tiles:
        image = t['image']
        sheet.alpha_composite(image, (x, round(floor - t['supportScreen'])))
        lines = [f"{t['id']}. {NAMES[t['id']]}", f"width {float(t['width']):.2f}"]
        if t['height'] is not None:
            lines.append(f"seated h {t['height']:.1f}")
        for row, text in enumerate(lines):
            draw.text((x, floor + 12 + row * 22), text, fill=(20, 20, 20, 255), font=label)
        x += image.width + gap
    draw.line((0, floor, width, floor), fill=(200, 30, 30, 255), width=2)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    sheet.convert('RGB').save(OUT)
    print(OUT, sheet.size)


if __name__ == '__main__':
    main()
