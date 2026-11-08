"""Review sheet for a Codex job: base image first, then the returned PNGs in brief order, on a mid-grey card with the alpha
silhouette of the base drawn under each return so body drift is visible. Writes local-data/reference-review/v12/codex-<job>.png.
Usage: Build-CodexJobSheetV12.py <job> <base.png relative to repo> <name.png> [...]
"""
import os
import sys

from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CELL = 480


def fit(im, size):
    w, h = im.size
    s = size / max(w, h)
    return im.resize((max(1, int(w * s)), max(1, int(h * s))), Image.LANCZOS)


def main():
    job, base_rel, names = sys.argv[1], sys.argv[2], sys.argv[3:]
    folder = os.path.join(ROOT, 'draft-assets', 'codex-v12', job)
    base = Image.open(os.path.join(ROOT, base_rel)).convert('RGBA')
    items = [('BASE ' + os.path.basename(base_rel), base)]
    for n in names:
        p = os.path.join(folder, n)
        items.append((n, Image.open(p).convert('RGBA') if os.path.exists(p) else None))
    cols = len(items)
    sheet = Image.new('RGB', (cols * CELL, CELL + 30), (120, 124, 130))
    draw = ImageDraw.Draw(sheet)
    base_fit = fit(base, CELL - 8)
    base_mask = base_fit.getchannel('A').point(lambda v: 90 if v > 32 else 0)
    for i, (label, im) in enumerate(items):
        x = i * CELL
        card = Image.new('RGBA', (CELL, CELL), (120, 124, 130, 255))
        if i > 0 and im is not None and im.size == base.size:
            ghost = Image.new('RGBA', base_fit.size, (255, 80, 80, 0))
            ghost.putalpha(base_mask)
            card.alpha_composite(ghost, ((CELL - base_fit.width) // 2, (CELL - base_fit.height) // 2))
        if im is not None:
            f = fit(im, CELL - 8)
            card.alpha_composite(f, ((CELL - f.width) // 2, (CELL - f.height) // 2))
        else:
            draw_c = ImageDraw.Draw(card)
            draw_c.text((20, CELL // 2), 'MISSING', fill=(255, 60, 60, 255))
        sheet.paste(card.convert('RGB'), (x, 0))
        size = '%dx%d' % im.size if im is not None else ''
        draw.text((x + 6, CELL + 8), '%s  %s' % (label, size), fill=(255, 255, 255))
    out = os.path.join(ROOT, 'local-data', 'reference-review', 'v12', 'codex-%s.png' % job)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    sheet.save(out)
    print(out, sheet.size)


if __name__ == '__main__':
    main()
