"""V12 2b: one optical-flow in-between per neighbouring pair of running frames, loop seam included
(run_left 18 -> 36, run_right 16 -> 32 when every tween passes the ghost gate).

Method (never a plain crossfade: two overlapping heads read as a ghost):
  1. Both frames are placed exactly as the DEV runtime draws them (head registration, planning/run-registration-v12.json,
     integer px) on a padded canvas, so the flow only has to carry the body and legs, not the whole-sprite shift.
  2. OpenCV Farnebaeck dense flow a->b and b->a on the alpha-premultiplied luminance (cat on black), with each of a few
     PRESETS; the passing result with the fewest ghost pixels is kept.
  3. Midpoint flows F(t->a) = -1/4 F(a->b) + 1/4 F(b->a) and F(t->b) = 1/4 F(a->b) - 1/4 F(b->a); each frame is warped
     halfway toward the other (premultiplied RGBA, bilinear) and the two warped frames are blended 50/50.
  4. The tween is stored on a 1024 canvas shifted back by the rounded mean of the pair's offsets ("seedOffset"); the
     head measurement (Measure-RunHeadsV12.py) then registers it midway between its neighbours.

Ghost gate (a tween that fails is dropped; the kept frames still play, unevenly spaced where one is missing):
  semi   the visible semi-transparent pixel count (alpha 9..247; the art never reaches 255 and its solid body sits at
         248-253, so 0 < alpha < 255 would count the whole body) lies within [min, max] of its two source frames;
  edge   the tween's alpha edge is within EDGE_PX native px of the flow-predicted edge everywhere: each source warped
         halfway predicts a silhouette (alpha >= 128); where one is solid and the other empty the blend is a half-alpha
         ghost, so that disagreement, eroded by a disk of radius EDGE_PX, must be empty (edges and hairs thinner than
         that erode away; a ghost limb does not). A p95 edge distance was tried first and let a ghost paw through.
         Edge-to-edge distances (p95, max) are still recorded for information.
Writes draft-assets/run-tweens-v12/<clip>/<a>-<b>.png (kept only), <clip>/order.json (play order of the kept frames) and
draft-assets/run-tweens-v12/ghost-gate.json. Never writes under art/."""
import hashlib
import json
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
REGISTRATION = ROOT / 'planning/run-registration-v12.json'
OUT = ROOT / 'draft-assets/run-tweens-v12'
CLIPS = ('run_left', 'run_right')
PAD = 192
SEMI_BAND = (9, 247)  # visible (runtime draws alpha > 8) and below the art's solid level (248-253)
EDGE_PX = 2
CROP_MARGIN = 96
# Farneback settings tried for every pair; the passing one with the smallest edge distance is kept (the legs move far
# between some frames, and no single window both follows a swinging leg and keeps the fur edge tight).
PRESETS = {
    'medium': dict(pyr_scale=0.5, levels=6, winsize=25, iterations=8, poly_n=7, poly_sigma=1.5),
    'fine': dict(pyr_scale=0.5, levels=5, winsize=15, iterations=10, poly_n=5, poly_sigma=1.1),
    'wide': dict(pyr_scale=0.5, levels=7, winsize=41, iterations=10, poly_n=7, poly_sigma=1.5),
    'widest': dict(pyr_scale=0.6, levels=8, winsize=61, iterations=12, poly_n=7, poly_sigma=1.5),
}
FLAGS = cv2.OPTFLOW_FARNEBACK_GAUSSIAN


def load(path):
    with Image.open(path) as image:
        return np.asarray(image.convert('RGBA')).astype(np.float32)


def place(rgba, shift):
    """Put a 1024 frame on the padded canvas, moved by the integer shift (dx, dy)."""
    h, w = rgba.shape[:2]
    canvas = np.zeros((h + 2 * PAD, w + 2 * PAD, 4), np.float32)
    x, y = PAD + shift[0], PAD + shift[1]
    canvas[y:y + h, x:x + w] = rgba
    return canvas


def premultiply(rgba):
    out = rgba.copy()
    out[..., :3] *= rgba[..., 3:4] / 255.0
    return out


def unpremultiply(prem):
    out = prem.copy()
    a = prem[..., 3:4]
    out[..., :3] = np.where(a > 0, prem[..., :3] * 255.0 / np.maximum(a, 1e-6), 0)
    return np.clip(out, 0, 255)


def flow_image(prem):
    luma = cv2.cvtColor(np.clip(prem[..., :3], 0, 255).astype(np.uint8), cv2.COLOR_RGB2GRAY).astype(np.float32)
    # Luminance on black plus a share of alpha, so the silhouette edge is strong even where fur matches its surroundings.
    return np.clip(0.6 * luma + 0.4 * prem[..., 3], 0, 255).astype(np.uint8)


def farneback(a, b, preset='medium'):
    return cv2.calcOpticalFlowFarneback(a, b, None, flags=FLAGS, **PRESETS[preset])


def warp(image, flow):
    h, w = flow.shape[:2]
    gx, gy = np.meshgrid(np.arange(w, dtype=np.float32), np.arange(h, dtype=np.float32))
    return cv2.remap(image, gx + flow[..., 0], gy + flow[..., 1], cv2.INTER_LINEAR, borderMode=cv2.BORDER_CONSTANT, borderValue=0)


def semi(alpha):
    """Visible semi-transparent pixels. The run art never reaches alpha 255 (its solid body sits at 248-253) and carries
    an invisible alpha 1-8 haze, so 0 < alpha < 255 would count the whole body; ghosts live in the visible fur band."""
    q = np.rint(alpha)
    return int(((q >= SEMI_BAND[0]) & (q <= SEMI_BAND[1])).sum())


def edge(alpha):
    mask = (alpha >= 128).astype(np.uint8)
    return mask & (1 - cv2.erode(mask, np.ones((3, 3), np.uint8)))


def edge_distance(from_edge, to_edge):
    """Distances from each pixel of from_edge to the nearest pixel of to_edge."""
    field = cv2.distanceTransform((1 - to_edge).astype(np.uint8), cv2.DIST_L2, 5)
    return field[from_edge.astype(bool)]


def tween(a_rgba, b_rgba, sa, sb, preset='medium'):
    """Midpoint of the placed pair on the padded canvas: (tween premultiplied RGBA, warped-a alpha, warped-b alpha,
    placed-a alpha, placed-b alpha). The flow runs on the union box of both cats plus CROP_MARGIN."""
    pa, pb = premultiply(place(a_rgba, sa)), premultiply(place(b_rgba, sb))
    ys, xs = np.nonzero((pa[..., 3] > 0) | (pb[..., 3] > 0))
    y0, y1 = max(0, ys.min() - CROP_MARGIN), min(pa.shape[0], ys.max() + CROP_MARGIN + 1)
    x0, x1 = max(0, xs.min() - CROP_MARGIN), min(pa.shape[1], xs.max() + CROP_MARGIN + 1)
    ca, cb = pa[y0:y1, x0:x1], pb[y0:y1, x0:x1]
    ia, ib = flow_image(ca), flow_image(cb)
    fab, fba = farneback(ia, ib, preset), farneback(ib, ia, preset)
    fta = -0.25 * fab + 0.25 * fba
    ftb = 0.25 * fab - 0.25 * fba
    wa, wb = warp(ca, fta), warp(cb, ftb)
    mid = np.zeros_like(pa)
    mid[y0:y1, x0:x1] = 0.5 * wa + 0.5 * wb
    full_a, full_b = np.zeros(pa.shape[:2], np.float32), np.zeros(pa.shape[:2], np.float32)
    full_a[y0:y1, x0:x1], full_b[y0:y1, x0:x1] = wa[..., 3], wb[..., 3]
    return mid, full_a, full_b, pa[..., 3], pb[..., 3]


def best_tween(a_rgba, b_rgba, sa, sb):
    """Try every preset; return (preset, tween, gate result, all attempts). Passing beats failing, then fewer
    ghost pixels, then less semi-transparent excess."""
    attempts, best = [], None
    for preset in PRESETS:
        mid, wa, wb, aa, ab = tween(a_rgba, b_rgba, sa, sb, preset)
        result = gate(mid[..., 3], wa, wb, aa, ab)
        attempts.append({'preset': preset, 'passed': result['passed'], 'semiTransparent': result['semiTransparent'],
                         'ghostPixels': result['ghostPixels']})
        excess = max(0, result['semiTransparent'] - result['semiSourceRange'][1])
        key = (not result['passed'], result['ghostPixels'], excess)
        if best is None or key < best[0]:
            best = (key, preset, mid, result)
    return best[1], best[2], best[3], attempts


def gate(mid_alpha, wa_alpha, wb_alpha, a_alpha, b_alpha):
    sa, sb, st = semi(a_alpha), semi(b_alpha), semi(mid_alpha)
    lo, hi = min(sa, sb), max(sa, sb)
    # Where one warped source is solid and the other empty, their 50/50 blend is a half-alpha ghost and the tween's
    # alpha >= 128 edge is ambiguous. Disagreement thinner than 2 px either side (an edge or a hair) erodes away.
    disagree = (np.abs(wa_alpha - wb_alpha) >= 128).astype(np.uint8)
    ghost = cv2.erode(disagree, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (2 * EDGE_PX + 1, 2 * EDGE_PX + 1)))
    ghost_pixels = int(ghost.sum())
    et, ea, eb = edge(mid_alpha), edge(wa_alpha), edge(wb_alpha)
    distances = np.concatenate([edge_distance(et, ea), edge_distance(et, eb), edge_distance(ea, et), edge_distance(eb, et)])
    reasons = []
    if not lo <= st <= hi:
        reasons.append(f'semi-transparent {st} px outside source range [{lo}, {hi}]')
    if ghost_pixels > 0:
        ys, xs = np.nonzero(ghost)
        reasons.append(f'flow-predicted edges disagree by more than {EDGE_PX:g} px over {ghost_pixels} px '
                       f'(around x {int(xs.min())}..{int(xs.max())}, y {int(ys.min())}..{int(ys.max())} on the placed canvas)')
    return {'semiTransparent': st, 'semiSourceRange': [lo, hi], 'semiSources': [sa, sb], 'ghostPixels': ghost_pixels,
            'edgeDistanceP95': round(float(np.quantile(distances, 0.95)), 3), 'edgeDistanceMax': round(float(distances.max()), 3),
            'passed': not reasons, 'reasons': reasons}


def store(mid, seed):
    """Back to a 1024 canvas, shifted by -seed; every visible pixel must fit with a 1 px clear border."""
    straight = unpremultiply(mid)
    x, y = PAD + seed[0], PAD + seed[1]
    out = straight[y:y + 1024, x:x + 1024]
    visible = straight[..., 3] > 8
    assert visible.sum() == (out[..., 3] > 8).sum(), 'visible tween fur would leave the canvas'
    visible = np.nonzero(out[..., 3] > 8)
    assert visible[0].min() > 0 and visible[1].min() > 0 and visible[0].max() < 1023 and visible[1].max() < 1023
    return Image.fromarray(np.rint(out).astype(np.uint8), 'RGBA')


def main():
    registration = json.loads(REGISTRATION.read_text())
    report = {'version': 'v12-2b', 'generatedBy': 'tools/local/Tween-RunFramesV12.py', 'method': __doc__.split('Ghost gate')[0].strip(),
              'gate': {'semi': f'visible semi-transparent (alpha {SEMI_BAND[0]}..{SEMI_BAND[1]}) pixel count within [min, max] of the source pair',
                       'edge': f'the two flow-predicted silhouettes (each source warped halfway, alpha >= 128) agree within {EDGE_PX} native px '
                               f'everywhere: their disagreement eroded by a radius-{EDGE_PX} disk is empty (ghostPixels == 0)'},
              'farnebackPresets': PRESETS, 'farnebackFlags': 'OPTFLOW_FARNEBACK_GAUSSIAN',
              'presetChoice': 'every preset is tried per pair; a passing one with the fewest ghost pixels is kept',
              'clips': {}}
    for clip in CLIPS:
        native = registration['clips'][clip]['native']
        folder = OUT / clip
        folder.mkdir(parents=True, exist_ok=True)
        for old in folder.glob('*.png'):
            old.unlink()
        images = [load(ROOT / r['source']) for r in native]
        shifts = [(int(round(r['dx'])), int(round(r['dy']))) for r in native]
        order, kept, dropped = [], [], []
        for i, row in enumerate(native):
            j = (i + 1) % len(native)
            order.append({'name': row['frame'], 'source': row['source'], 'tween': False})
            preset, mid, result, attempts = best_tween(images[i], images[j], shifts[i], shifts[j])
            name = f"{row['frame']}-{native[j]['frame']}"
            seed = (int(round((shifts[i][0] + shifts[j][0]) / 2)), int(round((shifts[i][1] + shifts[j][1]) / 2)))
            record = {'name': name, 'between': [row['frame'], native[j]['frame']], 'loopSeam': j == 0, 'seedOffset': list(seed),
                      'preset': preset} | result | {'attempts': attempts}
            if result['passed']:
                path = folder / f'{name}.png'
                store(mid, seed).save(path, optimize=True)
                record['source'] = path.relative_to(ROOT).as_posix()
                record['sha256'] = hashlib.sha256(path.read_bytes()).hexdigest()
                order.append({'name': name, 'source': record['source'], 'tween': True, 'between': record['between'],
                              'seedOffset': list(seed)})
                kept.append(record)
            else:
                dropped.append(record)
            print(f"{clip} {name}: {'KEEP' if result['passed'] else 'DROP'} [{preset}] semi {result['semiTransparent']} "
                  f"range {result['semiSourceRange']} ghost {result['ghostPixels']} (edge p95 {result['edgeDistanceP95']})")
        (folder / 'order.json').write_text(json.dumps({'clip': clip, 'frames': order, 'count': len(order),
                                                       'tweens': len(kept), 'dropped': [d['name'] for d in dropped]}, indent=1) + '\n')
        report['clips'][clip] = {'pairs': len(native), 'kept': kept, 'dropped': dropped, 'playOrderFrames': len(order)}
        print(f'{clip}: kept {len(kept)} dropped {len(dropped)} -> {len(order)} frames')
    (OUT / 'ghost-gate.json').write_text(json.dumps(report, indent=1) + '\n')
    print('wrote', (OUT / 'ghost-gate.json').relative_to(ROOT).as_posix())


if __name__ == '__main__':
    main()
