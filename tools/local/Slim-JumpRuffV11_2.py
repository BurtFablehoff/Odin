"""Slim the throat/chest ruff ("bullfrog") on ORIGINAL jump frames by a local horizontal warp.
No generated pixels: every output pixel is a resampled original pixel. Rows between top and bottom
move the front (facing-side) contour back toward the body by up to `amount` native px with a
cosine taper in y; columns compress smoothly over `depth` px behind the contour so the fur
texture survives. Head rows above `top` and everything outside the band are byte-identical.
Usage: python Slim-JumpRuffV11_2.py            (writes draft-assets/jump-ruff-v11-2/<clip>/NNN.png)
"""
import hashlib
import json
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'draft-assets/jump-ruff-v11-2'
PARAMS = ROOT / 'planning/jump-ruff-v11-2.json'


def taper(y, top, peak, bottom):
    if y <= top or y >= bottom:
        return 0.0
    if y <= peak:
        return 0.5 - 0.5 * math.cos(math.pi * (y - top) / (peak - top))
    return 0.5 + 0.5 * math.cos(math.pi * (y - peak) / (bottom - peak))


def front_x(alpha_row, facing):
    xs = np.nonzero(alpha_row > 40)[0]
    if xs.size == 0:
        return None
    return int(xs[0]) if facing == 'left' else int(xs[-1])


def slim(image, facing, top, peak, bottom, amount, depth):
    rgba = np.asarray(image.convert('RGBA')).astype(np.float32)
    # premultiply so resampling does not pull white/black fringes
    pm = rgba.copy()
    pm[..., :3] *= rgba[..., 3:4] / 255.0
    out = pm.copy()
    h, w, _ = rgba.shape
    xs = np.arange(w, dtype=np.float32)
    for y in range(max(0, top), min(h, bottom)):
        a = amount * taper(y, top, peak, bottom)
        if a < 0.05:
            continue
        f = front_x(rgba[y, :, 3], facing)
        if f is None:
            continue
        if facing == 'left':
            # output x samples source x - s(x); s = a at/before the new front, 0 at f + depth
            t = np.clip((xs - f) / depth, 0.0, 1.0)
            s = a * (0.5 + 0.5 * np.cos(np.pi * t))
            src = xs - s
        else:
            t = np.clip((f - xs) / depth, 0.0, 1.0)
            s = a * (0.5 + 0.5 * np.cos(np.pi * t))
            src = xs + s
        x0 = np.floor(src).astype(int)
        fr = (src - x0)[:, None]
        x0c = np.clip(x0, 0, w - 1)
        x1c = np.clip(x0 + 1, 0, w - 1)
        row = pm[y]
        val = row[x0c] * (1 - fr) + row[x1c] * fr
        val[(src < 0) | (src > w - 1)] = 0
        out[y] = val
    alpha = out[..., 3:4]
    rgb = np.where(alpha > 0, out[..., :3] * 255.0 / np.maximum(alpha, 1e-6), 0)
    result = np.concatenate([np.clip(rgb, 0, 255), np.clip(alpha, 0, 255)], axis=2)
    result = np.rint(result).astype(np.uint8)
    # rows outside the band stay byte-identical
    original = np.asarray(image.convert('RGBA'))
    result[:max(0, top)] = original[:max(0, top)]
    result[min(h, bottom):] = original[min(h, bottom):]
    return Image.fromarray(result, 'RGBA')


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main(only=None):
    params = json.loads(PARAMS.read_text())
    for row in params['frames']:
        key = f"{row['clip']}/{row['frame']:03d}"
        if only and key not in only:
            continue
        src = ROOT / f"art/frames/{key}.png"
        dst = OUT / f"{key}.png"
        dst.parent.mkdir(parents=True, exist_ok=True)
        with Image.open(src) as image:
            slim(image, row['facing'], row['top'], row['peak'], row['bottom'], row['amount'], row['depth']).save(dst)
        row['sourceSHA256'] = sha(src)
        row['outputSHA256'] = sha(dst)
        print(key, row['outputSHA256'][:12])
    PARAMS.write_text(json.dumps(params, indent=2) + '\n')


if __name__ == '__main__':
    main(set(sys.argv[1:]) or None)


def apply_pack(manifest, payloads):
    """Swap the slimmed jump frames into the DEV pack; anchors/supports stay (paw rows unchanged)."""
    if not PARAMS.exists():
        return manifest, payloads
    params = json.loads(PARAMS.read_text())
    table = {(r['clip'], r['frame']): r for r in params['frames']}
    applied = 0
    for entry in manifest['actions']:
        row = table.get((entry['clip'], entry['frame']))
        if not row:
            continue
        src = f"art/frames/{row['clip']}/{row['frame']:03d}.png"
        assert entry['source'] == src and entry['sha256'] == row['sourceSHA256'], src
        path = OUT / f"{row['clip']}/{row['frame']:03d}.png"
        data = path.read_bytes()
        assert hashlib.sha256(data).hexdigest() == row['outputSHA256'], path
        with Image.open(path) as image:
            box = image.getchannel('A').point(lambda a: 255 if a >= 8 else 0).getbbox()
        entry.update(source=path.relative_to(ROOT).as_posix(), sha256=row['outputSHA256'],
                     bounds=[box[0], box[1], box[2] - box[0], box[3] - box[1]],
                     originalSource=src, originalSHA256=row['sourceSHA256'], jumpRuffV11_2=True,
                     sourcePixelsEdited=True, styleOriginalRestored=False)
        payloads[entry['key']] = data
        applied += 1
    assert applied == len(table), (applied, len(table))
    manifest['jumpRuffV11_2'] = {'policy': 'planning/jump-ruff-v11-2.json', 'frames': applied, 'ownerApproved': False}
    return manifest, payloads
