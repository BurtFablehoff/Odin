"""V12 2b strip: one full cycle of each DEV run clip in play order, as the app draws it at the small size (480), one cell
per 16 ms tick. The canvases come from `Odin-Dev-Local.exe --test-run-motion <report.json> [frames-dir]` (OdinWindow.DrawScene
during real CatMind zoomies; Verify writes them to build/run-motion-frames). Every cell is pasted at runtime size with the
cat's root (anchor x, floor) at the same point, so a steady head shows as the nose dot staying on the cross-hair.

Marks: red dot = nose (the head-registration landmark), grey cross-hair = the clip's mean nose position, teal line = floor,
magenta bar + label = optical-flow tween (draft), grey label = original frame.
Usage: Build-RunStripV12.py [frames-dir] [out.png] [run-motion report]
Defaults: build/run-motion-frames -> local-data/reference-review/v12/run-strip.png, build/local-test-run-motion.json"""
import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
FRAMES = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / 'build/run-motion-frames'
OUT = Path(sys.argv[2]) if len(sys.argv) > 2 else ROOT / 'local-data/reference-review/v12/run-strip.png'
REPORT = Path(sys.argv[3]) if len(sys.argv) > 3 else ROOT / 'build/local-test-run-motion.json'
COLUMNS = 7
PAD, LABEL_H, GAP = 10, 26, 6
BG, CELL, FLOOR, INK, NOSE, GUIDE, TWEEN = ((238, 241, 246, 255), (226, 232, 240, 255), (0, 150, 140, 255), (20, 24, 32, 255),
                                            (220, 30, 40, 255), (120, 128, 140, 255), (200, 0, 150, 255))


def font(size):
    for name in ('segoeui.ttf', 'arial.ttf'):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            pass
    return ImageFont.load_default()


def visible_box(image):
    alpha = image.getchannel('A').point(lambda a: 255 if a > 8 else 0)
    return alpha.getbbox()


def clip_rows(frames, clip, small, big):
    cells = [f for f in frames if f['clip'] == clip]
    cells.sort(key=lambda f: f['tick'])
    images = [Image.open(FRAMES / f['file']).convert('RGBA') for f in cells]
    # Cell window relative to the root: the union of every visible sprite, plus the floor line.
    left = top = 10 ** 9
    right = bottom = -10 ** 9
    for f, image in zip(cells, images):
        box = visible_box(image)
        rx, ry = f['root']
        left, top = min(left, box[0] - rx), min(top, box[1] - ry)
        right, bottom = max(right, box[2] - rx), max(bottom, box[3] - ry)
    bottom = max(bottom, 4)
    left, top, right, bottom = int(left) - PAD, int(top) - PAD, int(right) + PAD, int(bottom) + PAD
    w, h = right - left, bottom - top
    mean_x = sum(f['nose'][0] - f['root'][0] for f in cells) / len(cells)
    mean_y = sum(f['nose'][1] - f['root'][1] for f in cells) / len(cells)
    tiles = []
    for f, image in zip(cells, images):
        tile = Image.new('RGBA', (w, h + LABEL_H), BG)
        d = ImageDraw.Draw(tile)
        d.rectangle([0, LABEL_H, w - 1, h + LABEL_H - 1], fill=CELL)
        ox, oy = -left, -top + LABEL_H  # root position in the tile
        d.line([(0, oy), (w, oy)], fill=FLOOR, width=2)
        layer = Image.new('RGBA', tile.size, (0, 0, 0, 0))
        layer.alpha_composite(image, (round(ox - f['root'][0]), round(oy - f['root'][1])))
        tile.alpha_composite(layer)
        d = ImageDraw.Draw(tile)
        gx, gy = ox + mean_x, oy + mean_y
        d.line([(gx - 18, gy), (gx + 18, gy)], fill=GUIDE, width=1)
        d.line([(gx, gy - 18), (gx, gy + 18)], fill=GUIDE, width=1)
        nx, ny = ox + f['nose'][0] - f['root'][0], oy + f['nose'][1] - f['root'][1]
        d.ellipse([nx - 3.5, ny - 3.5, nx + 3.5, ny + 3.5], fill=NOSE)
        if f['tween']:
            d.rectangle([0, 0, w - 1, LABEL_H - 4], fill=TWEEN)
            d.text((6, 3), f"tick {f['tick']}  tween {f['name']}", fill=(255, 255, 255, 255), font=small)
        else:
            d.text((6, 3), f"tick {f['tick']}  frame {f['name']}", fill=INK, font=small)
        tiles.append(tile)
    rows = (len(tiles) + COLUMNS - 1) // COLUMNS
    block = Image.new('RGBA', (COLUMNS * (w + GAP) + GAP, rows * (h + LABEL_H + GAP) + GAP + 40), BG)
    tweens = sum(f['tween'] for f in cells)
    ImageDraw.Draw(block).text((GAP, 8), f"{clip}: {len(cells)} frames per cycle ({len(cells) - tweens} original + {tweens} tween), "
                                         f"{len(cells) * 16} ms per cycle at one frame per 16 ms tick", fill=INK, font=big)
    for i, tile in enumerate(tiles):
        block.alpha_composite(tile, (GAP + (i % COLUMNS) * (w + GAP), 40 + GAP + (i // COLUMNS) * (h + LABEL_H + GAP)))
    return block


def main():
    data = json.loads((FRAMES / 'run-motion-frames.json').read_text())
    report = json.loads(REPORT.read_text()) if REPORT.exists() else {}
    small, big, title = font(13), font(18), font(22)
    blocks = [clip_rows(data['frames'], clip, small, big) for clip in ('run_left', 'run_right')]
    header = 96
    width = max(b.width for b in blocks)
    sheet = Image.new('RGBA', (width, header + sum(b.height for b in blocks)), BG)
    d = ImageDraw.Draw(sheet)
    d.text((10, 8), 'Odin v12-2b running (DEV): head-registered frames and optical-flow tweens, size 480, play order', fill=INK, font=title)
    numbers = ''
    if report:
        numbers = (f"Measured by --test-run-motion over {report.get('checkedTicks')} ticks: head step next to a tween "
                   f"{report.get('maxHeadStepNextToTweenNative')} native px, between originals {report.get('maxHeadStepBetweenOriginalsNative')}, "
                   f"bob {report.get('maxBobNative')} (limits 4 / 8 / 20; beyond 1 screen px of rounding).")
    d.text((10, 40), 'Red dot = nose (registration landmark); grey cross = mean nose; teal = floor; magenta bar = tween (draft). '
                     'Each cell keeps the cat\'s root at the same point.', fill=INK, font=small)
    d.text((10, 62), numbers, fill=INK, font=small)
    y = header
    for b in blocks:
        sheet.alpha_composite(b, (0, y))
        y += b.height
    OUT.parent.mkdir(parents=True, exist_ok=True)
    sheet.convert('RGB').save(OUT)
    print('Run strip:', OUT, sheet.size)


if __name__ == '__main__':
    main()
