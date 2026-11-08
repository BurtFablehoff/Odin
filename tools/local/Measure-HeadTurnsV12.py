"""V12 3R helper: print what the head-turn registration needs from the DEV pack and the Codex frames (read only)."""
import io
import json
import sys
import zipfile
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
PACK = ROOT / 'local-data/reference-review/dev-latest-art/dev-artwork.zip'
CODEX = ROOT / 'draft-assets/codex-v12'
FAMILIES = {'rear': 'head-turns-rear', 'lookback': 'head-turns-lookback', 'side': 'head-turns-side'}
DIRS = ['far-left', 'left', 'centre', 'right', 'far-right']


def describe(alpha):
    h, w = alpha.shape
    rows64 = np.nonzero((alpha >= 64).any(axis=1))[0]
    ys, xs = np.nonzero(alpha > 8)
    solid = alpha > 128
    runs = []
    for y in range(h):
        row = solid[y]
        best = cur = 0
        for v in row:
            cur = cur + 1 if v else 0
            best = max(best, cur)
        runs.append(best)
    runs = np.array(runs)
    support = int(np.nonzero(runs >= 0.25 * runs.max())[0].max())
    return {'size': [w, h], 'top64': int(rows64.min()), 'bottom64': int(rows64.max()), 'bbox8': [int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())],
            'supportRule25': support, 'maxRun': int(runs.max())}


def column_bottoms(alpha, step=16):
    h, w = alpha.shape
    out = []
    for x in range(0, w, step):
        col = np.nonzero(alpha[:, x] >= 128)[0]
        out.append((x, int(col.max()) if len(col) else None))
    return out


def main():
    pack = zipfile.ZipFile(PACK)
    manifest = json.loads(pack.read('manifest.json'))
    for p in manifest['poses']:
        if p['id'] in (1, 6, 7, 8):
            print('POSE', json.dumps(p))
            alpha = np.asarray(Image.open(io.BytesIO(pack.read(p['key']))).convert('RGBA'))[..., 3]
            print('  ', describe(alpha))
            if '--bottoms' in sys.argv:
                print('   bottoms', column_bottoms(alpha))
    look = [a for a in manifest['actions'] if a['clip'] == 'look_loaf']
    print('LOOK_LOAF0', json.dumps(look[0]))
    print('CLIPS', [c for c in manifest['actionClips'] if c['clip'].startswith('look_')])
    print('TOP KEYS', [k for k in manifest if k not in ('poses', 'actions', 'actionClips', 'blinks', 'motions')])
    for fam, folder in FAMILIES.items():
        for d in DIRS:
            path = CODEX / folder / f'{fam}-{d}.png'
            im = Image.open(path)
            alpha = np.asarray(im.convert('RGBA'))[..., 3]
            print(fam, d, im.mode, describe(alpha))
            if '--bottoms' in sys.argv and d == 'centre':
                print('   bottoms', column_bottoms(alpha))


if __name__ == "__main__" and "--crops" not in sys.argv:
    main()


def crops():
    """Head crops with a 20 px grid (labels every 100) for manual ear/eye landmarks, into local-data/reference-review/v12/landmarks/."""
    from PIL import ImageDraw
    out = ROOT / 'local-data/reference-review/v12/landmarks'
    out.mkdir(parents=True, exist_ok=True)
    jobs = [('old-lookback', ROOT / 'draft-assets/numbered-sizing-v2/sit-lookback.png', (720, 60, 1180, 460))]
    for fam, folder in FAMILIES.items():
        for d in DIRS:
            box = {'rear': (600, 40, 1060, 440), 'lookback': (680, 40, 1240, 480), 'side': (680, 20, 1240, 480)}[fam]
            jobs.append((f'{fam}-{d}', CODEX / folder / f'{fam}-{d}.png', box))
    for name, path, box in jobs:
        im = Image.open(path).convert('RGBA').crop(box)
        bg = Image.new('RGBA', im.size, (110, 120, 130, 255))
        bg.alpha_composite(im)
        bg = bg.resize((im.width * 2, im.height * 2), Image.NEAREST)
        draw = ImageDraw.Draw(bg)
        for gx in range((box[0] // 20 + 1) * 20, box[2], 20):
            x = (gx - box[0]) * 2
            draw.line([(x, 0), (x, bg.height)], fill=(255, 255, 0, 255) if gx % 100 == 0 else (60, 60, 60, 255), width=1)
            if gx % 100 == 0:
                draw.text((x + 2, 2), str(gx), fill=(255, 255, 0, 255))
        for gy in range((box[1] // 20 + 1) * 20, box[3], 20):
            y = (gy - box[1]) * 2
            draw.line([(0, y), (bg.width, y)], fill=(255, 255, 0, 255) if gy % 100 == 0 else (60, 60, 60, 255), width=1)
            if gy % 100 == 0:
                draw.text((2, y + 2), str(gy), fill=(255, 255, 0, 255))
        bg.convert('RGB').save(out / f'{name}.png')
    print('crops in', out)


if __name__ == '__main__' and '--crops' in sys.argv:
    crops()
