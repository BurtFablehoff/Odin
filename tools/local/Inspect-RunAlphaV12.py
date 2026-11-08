"""V12 2b diagnostics: alpha histogram of the native run frames (where does the art's solid body sit?)."""
from pathlib import Path
import numpy as np
from PIL import Image
ROOT = Path(__file__).resolve().parents[2]
for clip in ('run_left', 'run_right'):
    total = np.zeros(256, np.int64)
    for p in sorted((ROOT / 'art/frames' / clip).glob('*.png')):
        a = np.asarray(Image.open(p).convert('RGBA'))[..., 3]
        total += np.bincount(a.ravel(), minlength=256)
    print(clip, 'per-frame mean counts')
    n = len(list((ROOT / 'art/frames' / clip).glob('*.png')))
    for lo, hi in ((1, 8), (9, 63), (64, 127), (128, 199), (200, 239), (240, 247), (248, 251), (252, 253), (254, 254), (255, 255)):
        print(f'  {lo:3d}..{hi:3d}: {total[lo:hi + 1].sum() / n:9.0f}')
    print('  top values:', [(v, int(total[v] / n)) for v in np.argsort(total)[::-1][:8]])
