"""V12 2b diagnostics for Tween-RunFramesV12.py: frame a | warped a | tween | warped b | frame b, in placed space,
using the preset the tween script would keep for that pair.
Usage: Inspect-RunTweensV12.py <clip> <i> [<i> ...]  -> local-data/reference-review/v12/tween-inspect-<clip>-<i>.png"""
import json
import runpy
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
T = runpy.run_path(str(ROOT / 'tools/local/Tween-RunFramesV12.py'))
reg = json.loads((ROOT / 'planning/run-registration-v12.json').read_text())


def tile(rgba, box):
    im = Image.fromarray(np.rint(np.clip(rgba, 0, 255)).astype(np.uint8), 'RGBA').crop(box)
    bg = Image.new('RGBA', im.size, (210, 220, 235, 255))
    bg.alpha_composite(im)
    return bg


def main():
    clip = sys.argv[1]
    native = reg['clips'][clip]['native']
    for arg in sys.argv[2:]:
        i = int(arg)
        j = (i + 1) % len(native)
        a, b = T['load'](ROOT / native[i]['source']), T['load'](ROOT / native[j]['source'])
        sa = (int(round(native[i]['dx'])), int(round(native[i]['dy'])))
        sb = (int(round(native[j]['dx'])), int(round(native[j]['dy'])))
        preset, mid, result, _ = T['best_tween'](a, b, sa, sb)
        print(clip, i, preset, 'passed' if result['passed'] else 'FAILED', result['reasons'])
        panels = [T['place'](a, sa), T['unpremultiply'](mid), T['place'](b, sb)]
        ys, xs = np.nonzero(sum(p[..., 3] for p in panels) > 8)
        box = (int(xs.min()) - 10, int(ys.min()) - 10, int(xs.max()) + 10, int(ys.max()) + 10)
        tiles = [tile(p, box) for p in panels]
        sheet = Image.new('RGBA', (sum(t.width for t in tiles), tiles[0].height), (255, 255, 255, 255))
        x = 0
        for t in tiles:
            sheet.alpha_composite(t, (x, 0))
            x += t.width
        out = ROOT / f'local-data/reference-review/v12/tween-inspect-{clip}-{i:03d}.png'
        sheet.save(out)
        print(out)


if __name__ == '__main__':
    main()
