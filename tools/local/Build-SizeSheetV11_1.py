"""Before/after review sheet for the v11.1 size normalization.
Row 1: the 14 states at v11 sizes; row 2: at v11.1 sizes. Each still is drawn at its on-screen
small size, standing on one floor line (airborne jumps share the line for comparison only).
Writes local-data/reference-review/restore-v11/sizes-v11-1.png."""
import io
import json
import zipfile
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
PACK = ROOT / 'local-data/reference-review/dev-latest-art/dev-artwork.zip'
OUT = ROOT / 'local-data/reference-review/restore-v11/sizes-v11-1.png'
SHORT = {1: 'sit', 2: 'loaf', 3: 'lay', 4: 'sleep', 6: 'rear', 7: 'lookback', 8: 'side', 11: 'tucked',
         12: 'groom', 13: 'stretch', 14: 'run L', 15: 'run R', 16: 'jump L', 17: 'jump R'}


def tile(image, width, native):
    scale = width / native
    small = image.resize((max(1, round(image.width * scale)), max(1, round(image.height * scale))), Image.LANCZOS)
    return small.crop(small.getbbox())


def main():
    pack = zipfile.ZipFile(PACK)
    manifest = json.loads(pack.read('manifest.json'))
    rows = [[], []]
    for pose in manifest['poses']:
        image = Image.open(io.BytesIO(pack.read(pose['key']))).convert('RGBA')
        after = pose['renderWidthAtSmall']
        before = (pose.get('ownerSizesV12') or pose.get('sizeNormalizationV11_1') or {}).get('before', after)
        rows[0].append((pose['id'], tile(image, before, pose['nativeWidth']), 1.0))
        rows[1].append((pose['id'], tile(image, after, pose['nativeWidth']), after / before))
    gap = 18
    width = max(sum(t.width + gap for _, t, _ in row) for row in rows) + 120
    heights = [max(t.height for _, t, _ in row) + 50 for row in rows]
    sheet = Image.new('RGBA', (width, sum(heights) + 40), (238, 238, 238, 255))
    draw = ImageDraw.Draw(sheet)
    draw.text((10, 8), 'Odin v11 -> v11.1 sizes at the small size (head size matched to sitting by eye + ear landmarks). '
                       'DEV candidate, not approved.', fill=(0, 0, 0, 255))
    top = 30
    for label, row, height in zip(('v11 (now)', 'v11.1'), rows, heights):
        floor = top + height - 30
        draw.text((10, floor - 20), label, fill=(0, 0, 0, 255))
        x = 110
        for pid, image, factor in row:
            sheet.alpha_composite(image, (x, floor - image.height))
            text = f'{pid} {SHORT[pid]}' + (f' x{factor:.2f}' if abs(factor - 1) > 0.005 else '')
            draw.text((x, floor + 6), text, fill=(0, 0, 0, 255))
            x += image.width + gap
        draw.line((100, floor, width, floor), fill=(200, 0, 0, 255))
        top += height
    OUT.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(OUT)
    print(OUT, sheet.size)


if __name__ == '__main__':
    main()
