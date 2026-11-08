"""Before/after sheet for the v11.2 jump ruff slimming: top row original jump_left frames,
bottom row the slimmed frames (unchanged frames repeat the original).
Writes local-data/reference-review/restore-v11/jump-ruff-v11-2.png."""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'local-data/reference-review/restore-v11/jump-ruff-v11-2.png'
T = 260


def tile(path):
    im = Image.open(path).convert('RGBA')
    im = im.crop(im.getbbox())
    im.thumbnail((T, T), Image.LANCZOS)
    bg = Image.new('RGBA', (T, T), (255, 255, 255, 255))
    bg.alpha_composite(im, ((T - im.width) // 2, T - im.height))
    return bg


def main():
    frames = [0, 1, 2, 3, 12, 13, 14, 15]
    sheet = Image.new('RGBA', (len(frames) * T + 120, 2 * T + 40), (255, 255, 255, 255))
    draw = ImageDraw.Draw(sheet)
    draw.text((8, 6), 'jump_left: original (top) vs v11.2 slimmed throat ruff (bottom). Original pixels warped, none generated. DEV candidate.', fill=(0, 0, 0, 255))
    for row, label in enumerate(('original', 'v11.2')):
        draw.text((8, 30 + row * T + T // 2), label, fill=(0, 0, 0, 255))
        for i, f in enumerate(frames):
            src = ROOT / f'art/frames/jump_left/{f:03d}.png'
            new = ROOT / f'draft-assets/jump-ruff-v11-2/jump_left/{f:03d}.png'
            sheet.alpha_composite(tile(new if row and new.exists() else src), (120 + i * T, 30 + row * T))
            if row == 0:
                draw.text((120 + i * T + 4, 30), f'{f:03d}', fill=(200, 0, 0, 255))
    OUT.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(OUT)
    print(OUT, sheet.size)


if __name__ == '__main__':
    main()
