"""Sheet of the v11.3 jump sequences as the DEV pack places them: each frame drawn at its
on-screen small size around one shared anchor/support point, so size and position jumps
between frames are visible. Writes local-data/reference-review/restore-v11/run-style-jump-v11-3.png."""
import io
import json
import zipfile
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
PACK = ROOT / 'local-data/reference-review/dev-latest-art/dev-artwork.zip'
OUT = ROOT / 'local-data/reference-review/restore-v11/run-style-jump-v11-3.png'
CELL_W, CELL_H, ANCHOR = 300, 300, (150, 230)


def main():
    pack = zipfile.ZipFile(PACK)
    manifest = json.loads(pack.read('manifest.json'))
    clips = ['jump_left', 'jump_right']
    sheet = Image.new('RGBA', (16 * CELL_W, len(clips) * CELL_H + 30), (240, 240, 240, 255))
    draw = ImageDraw.Draw(sheet)
    draw.text((6, 6), 'v11.3 jumps: frames 002-012 are original running frames; 000-001 and 013-015 the jump art. '
                      'Red cross = shared anchor/support. DEV candidate.', fill=(0, 0, 0, 255))
    for row, clip in enumerate(clips):
        entries = sorted((a for a in manifest['actions'] if a['clip'] == clip), key=lambda a: a['frame'])
        for entry in entries:
            image = Image.open(io.BytesIO(pack.read(entry['key']))).convert('RGBA')
            scale = entry['renderWidthAtSmall'] / entry['nativeWidth'] * 0.5
            small = image.resize((round(image.width * scale), round(image.height * scale)), Image.LANCZOS)
            ox = entry['frame'] * CELL_W + ANCHOR[0] - round(entry['anchorX'] * scale)
            oy = 30 + row * CELL_H + ANCHOR[1] - round(entry['supportY'] * scale)
            cell = Image.new('RGBA', sheet.size, (0, 0, 0, 0))
            cell.alpha_composite(small, (ox, oy))
            sheet.alpha_composite(cell.crop((entry['frame'] * CELL_W, 30 + row * CELL_H,
                                             (entry['frame'] + 1) * CELL_W, 30 + (row + 1) * CELL_H)),
                                  (entry['frame'] * CELL_W, 30 + row * CELL_H))
            cx, cy = entry['frame'] * CELL_W + ANCHOR[0], 30 + row * CELL_H + ANCHOR[1]
            draw.line((cx - 6, cy, cx + 6, cy), fill=(220, 0, 0, 255))
            draw.line((cx, cy - 6, cx, cy + 6), fill=(220, 0, 0, 255))
            tag = 'run %03d' % entry['runStyleJumpV11_3']['runFrame'] if entry.get('runStyleJumpV11_3') else 'jump'
            draw.text((entry['frame'] * CELL_W + 4, 32 + row * CELL_H), f"{clip[5:]} {entry['frame']:03d} ({tag})", fill=(0, 0, 160, 255))
    OUT.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(OUT)
    print(OUT, sheet.size)


if __name__ == '__main__':
    main()
