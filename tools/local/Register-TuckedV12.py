"""V12 3R (tucked): register the Codex tucked-sleep redraw (draft-assets/codex-v12/tucked/tucked.png, 1254 px) into the
1024 runtime canvas for review. Tucked #11 stays PARKED (plan A5): this writes draft-assets/tucked-v12/tucked.png, a
registration record and a review sheet; nothing enters the DEV pack until Hatt passes it at G3.

Size rule: the new cat's on-screen body width at the Small size equals the parked tucked's (v11.3, width 318.3 for its 1774
canvas = scale 0.17937), so the review compares fur, not size. Support line y 944 like sit; body/belly/tail bottoms must lie
within 6 native px of one line, only paws below (floor rule). Uniform LANCZOS on premultiplied alpha; no repaint.
"""
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / 'draft-assets/codex-v12/tucked/tucked.png'
PARKED = ROOT / 'draft-assets/approved-tucked-sleep-v1/tucked.png'
OUT_DIR = ROOT / 'draft-assets/tucked-v12'
SHEET = ROOT / 'local-data/reference-review/v12/tucked.png'
CANVAS, ROOT_X, SUPPORT_Y, MARGIN = 1024, 512, 944, 8
PARKED_WIDTH_AT_SMALL = 318.3       # BACKLOG v11.3 width for the 1774-wide parked canvas
STANDARD_WIDTH_AT_SMALL = 480.0     # 1024-canvas states
LAY, LOAF = ROOT / 'art/frames/lay/000.png', ROOT / 'art/frames/loaf/000.png'


def sha(p):
    return hashlib.sha256(Path(p).read_bytes()).hexdigest()


def alpha(im):
    return np.asarray(im.convert('RGBA'))[..., 3]


def bbox(a, t=32):
    ys, xs = np.nonzero(a >= t)
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def bottom_profile(a, t=128):
    """Lowest solid row per column, for columns that have solid pixels."""
    cols = np.nonzero((a >= t).any(axis=0))[0]
    rows = np.array([np.nonzero(a[:, c] >= t)[0].max() for c in cols])
    return cols, rows


def floor_report(a):
    cols, rows = bottom_profile(a)
    # The support line is the most common bottom row (body/belly/tail plateau); paws are the columns below it.
    # The outer 5 % of columns at each end are the rump and cheek taper, which rises off the line by nature; they are
    # reported separately and do not fail the rule.
    values, counts = np.unique(rows, return_counts=True)
    plateau = int(values[counts.argmax()])
    n = len(cols)
    core = slice(int(n * 0.05), int(n * 0.95))
    below = rows[core] > plateau + 6
    above = rows[core] < plateau - 6
    ends = np.concatenate([rows[:int(n * 0.05)], rows[int(n * 0.95):]])
    return {'plateauRow': plateau, 'columns': int(n), 'coreColumns': int(rows[core].size),
            'coreColumnsWithin6px': int((~below & ~above).sum()), 'coreColumnsBelow6px(paws)': int(below.sum()),
            'coreColumnsAbove6px': int(above.sum()), 'coreMaxBelowPx': int((rows[core] - plateau).max()),
            'coreMaxAbovePx': int((plateau - rows[core]).max()), 'endTaperMaxRisePx': int((plateau - ends).max())}


def main():
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    src = Image.open(SRC).convert('RGBA')
    parked = Image.open(PARKED).convert('RGBA')
    sa, pa = alpha(src), alpha(parked)
    sb, pb = bbox(sa), bbox(pa)
    parked_scale = PARKED_WIDTH_AT_SMALL / parked.width
    parked_screen_w = (pb[2] - pb[0]) * parked_scale
    standard_scale = STANDARD_WIDTH_AT_SMALL / CANVAS
    # native px per source px so that the cat's screen width equals the parked one at the Small size
    scale = parked_screen_w / standard_scale / (sb[2] - sb[0])
    size = (round(src.width * scale), round(src.height * scale))
    scaled = src.convert('RGBa').resize(size, Image.LANCZOS).convert('RGBA')
    a2 = alpha(scaled)
    fr = floor_report(a2)
    b2 = bbox(a2)
    ox = ROOT_X - (b2[0] + b2[2]) // 2
    oy = SUPPORT_Y - fr['plateauRow'] - 1
    canvas = Image.new('RGBA', (CANVAS, CANVAS), (0, 0, 0, 0))
    canvas.alpha_composite(scaled, (ox, oy)) if ox >= 0 and oy >= 0 else canvas.paste(scaled, (ox, oy), scaled)
    ca = alpha(canvas)
    cb = bbox(ca, 9)
    assert cb[0] >= MARGIN and cb[1] >= MARGIN and cb[2] <= CANVAS - MARGIN and cb[3] <= CANVAS - MARGIN, 'fur touches canvas edge'
    out = OUT_DIR / 'tucked.png'
    canvas.save(out, optimize=True)
    final_floor = floor_report(ca)
    record = {
        'version': 12, 'date': '2026-11-08', 'status': 'PARKED (plan A5); review only, not in the DEV pack, not owner-approved',
        'source': SRC.relative_to(ROOT).as_posix(), 'sourceSHA256': sha(SRC), 'sourceSize': list(src.size),
        'parkedReference': PARKED.relative_to(ROOT).as_posix(), 'parkedSHA256': sha(PARKED),
        'sizeRule': 'cat screen width at Small equals the parked tucked (v11.3 width 318.3 on a 1774 canvas)',
        'parkedCatScreenWidthAtSmall': round(parked_screen_w, 2), 'scale': round(scale, 6), 'offset': [ox, oy],
        'canvas': CANVAS, 'anchorX': ROOT_X, 'supportY': SUPPORT_Y, 'renderWidthAtSmall': STANDARD_WIDTH_AT_SMALL,
        'alpha32Bounds': list(cb), 'floorRule': final_floor,
        'floorRulePassed': final_floor['coreColumnsAbove6px'] == 0 and final_floor['plateauRow'] == SUPPORT_Y - 1,
        'output': out.relative_to(ROOT).as_posix(), 'outputSHA256': sha(out),
        'resample': 'uniform LANCZOS on premultiplied alpha; no repaint, no retexture',
    }
    (OUT_DIR / 'registration.json').write_text(json.dumps(record, indent=2) + '\n')

    # Review sheet: parked (at its runtime size) | new (at runtime size) | lay/000 | loaf/000, all at the Small-size ratio x2.
    k = 2.0
    cells = [('11 tucked PARKED (v11.3 size)', parked, parked_scale * k), ('11 tucked v12 Codex redraw', canvas, standard_scale * k),
             ('3 lay/000 (fur target)', Image.open(LAY).convert('RGBA'), (444.43 / 1024) * k),
             ('2 loaf/000 (fur target)', Image.open(LOAF).convert('RGBA'), (372.43 / 1024) * k)]
    cw, ch = 720, 560
    sheet = Image.new('RGB', (cw * len(cells), ch + 40), (120, 124, 130))
    d = ImageDraw.Draw(sheet)
    for i, (label, im, s) in enumerate(cells):
        f = im.convert('RGBa').resize((max(1, round(im.width * s)), max(1, round(im.height * s))), Image.LANCZOS).convert('RGBA')
        fb = bbox(alpha(f), 32)
        card = Image.new('RGBA', (cw, ch), (120, 124, 130, 255))
        floor_y = ch - 60
        # put the cat's bottom plateau on the card's floor line
        cols, rows = bottom_profile(alpha(f))
        vals, cnts = np.unique(rows, return_counts=True)
        plateau = int(vals[cnts.argmax()])
        x = (cw - (fb[2] - fb[0])) // 2 - fb[0]
        card.alpha_composite(f, (x, floor_y - plateau)) if x >= 0 and floor_y - plateau >= 0 else card.paste(f, (x, floor_y - plateau), f)
        ImageDraw.Draw(card).line([(20, floor_y), (cw - 20, floor_y)], fill=(0, 150, 150, 255), width=2)
        sheet.paste(card.convert('RGB'), (i * cw, 0))
        d.text((i * cw + 8, ch + 10), label, fill=(255, 255, 255))
    SHEET.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(SHEET)
    print(json.dumps({'scale': record['scale'], 'offset': record['offset'], 'floor': final_floor,
                      'floorRulePassed': record['floorRulePassed'], 'sheet': SHEET.relative_to(ROOT).as_posix()}, indent=1))


if __name__ == '__main__':
    main()
