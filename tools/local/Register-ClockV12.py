"""V12 3bR-1: register the Codex 12-hour clock frames for states 6 (rear), 7 (look-back) and 8 (side) onto ONE FIXED BODY.

Owner (G3 FAIL, 2026-11-08): the 3R head turns were each scaled whole to fit their lower body, so the cat "keeps growing
and shrinking"; he wants a 12-position clock of head directions. Option A for 6/7/8 only: the family's 3R centre frame
(draft-assets/head-turns-v12/<f>/centre.png, already placed in the 1024 runtime canvas) is the ONE body, and only the
head and neck change per frame.

Inputs (read only): the raw 1254 edit base draft-assets/codex-v12/head-turns-<f>/<f>-centre.png and the Codex returns
draft-assets/codex-v12/clock-<f>-<a|b|c>/<f>-hNN.png (12 = head up, 3 = viewer's right, 6 = down, 9 = viewer's left).
Codex does not keep the body pixel-exact: it redraws the whole cat (here at 0.99-1.05x the base) and shifts it.

Per family and hour:
 (a) ALIGNMENT ONLY: one uniform scale and an integer translation that maximise the IoU of the return's body mask
     (alpha > 64) with the fixed body's inside the alignment window: the rows from the seam down to the stability band
     at full width, then the band rows right of the band's x0 (the tail is left out). Scale grid 0.92-1.32 x the centre's
     in steps of 0.01, then 0.001; translation by correlation. The return's body pixels are then DISCARDED, so the
     alignment sizes nothing the user sees except through the head it carries.
 (b) The alignment is solved directly in the 1024 canvas, i.e. composed with the recorded centre -> 1024 transform
     (planning/head-turns-registration-v12.json families.<f>.centre scale/offset, which produced the 3R centre.png); the
     record carries both the return -> raw-centre and the return -> canvas transforms. One uniform LANCZOS resample on
     premultiplied alpha (float box), no warp, no repaint.
 (c) Composite: the fixed centre below the family's NECK SEAM row, the return above it, a premultiplied smoothstep blend
     of RGB and alpha over FEATHER rows centred on the seam. Every row >= seam + FEATHER/2 is the centre's bytes
     (checked per frame: 0 differing pixels), so nothing below the seam can move.
 (d) Head size, yaw-aware, from recorded landmarks (LANDMARKS below, native px of each image): hours 11, 12, 1 and 5, 6, 7
     compare ear-apex spacing and skull width (outer ear-base span); hours 2, 3, 4 and 8, 9, 10 compare pinna length
     (apex to the midpoint of the ear's base) and crown-to-chin height. Ratio = return measure x alignment scale /
     centre measure x centre scale, both in canvas px; the frame ratio is the mean of the available measures. Default
     scale = the alignment scale; only when the ratio is off by more than 2 % is the region above the seam rescaled
     by 1/ratio about the neck base (the seam row at the neck's centre) and the record says so.
     STATUS 2026-11-08: no landmarks are recorded, so every frame is headUnmeasured and keeps the alignment scale; see
     UNMEASURED_WHY. The per-family head sheet (clock-<f>-heads.png: every hour's head with the centre's head outline
     in red, same scale) is the size check until landmarks exist.
 (e) Flags: clipped (head, ears or whiskers touch the return's own 1254 border, or fall outside / onto the edge of the
     1024 canvas) and shelf (across the feather rows the return's outline steps outside, or inside, the fixed body's by
     more than 12 px).

Writes draft-assets/clock-v12/<f>/h01..h12.png + centre.png (byte copy of the 3R centre), planning/clock-registration-
v12.json and the clock-face sheets local-data/reference-review/v12/clock-<f>.png (git-ignored). Families with missing
hours are processed for what exists and their gaps listed; rerun this script when the rest land. Frames with no
recorded landmarks are composited at the alignment scale and listed as headUnmeasured. Debug: `--crops <family>` writes
gridded native head crops (clock-debug/) to read landmarks from; `--seam-crops <family> <h,h,...>` writes 3x close-ups
of both outline ends at the seam. Nothing here is owner-approved.

Run: local-data/toolchain/python/python.exe tools/local/Register-ClockV12.py"""
import hashlib
import json
import math
import shutil
import sys
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
HEADTURNS = ROOT / 'planning/head-turns-registration-v12.json'
RECORD = ROOT / 'planning/clock-registration-v12.json'
OUT = ROOT / 'draft-assets/clock-v12'
SHEETS = ROOT / 'local-data/reference-review/v12'
DEBUG = SHEETS / 'clock-debug'
CANVAS = 1024
HOURS = [12, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11]          # frame index = app/ClockGaze.cs ClockAngles index (0 = 12 o'clock)
JOB = {12: 'a', 1: 'a', 2: 'a', 3: 'a', 4: 'b', 5: 'b', 6: 'b', 7: 'b', 8: 'c', 9: 'c', 10: 'c', 11: 'c'}
FRONTAL_HOURS = {11, 12, 1, 5, 6, 7}                     # ear-apex spacing + skull width
TURNED_HOURS = {2, 3, 4, 8, 9, 10}                       # pinna length + crown-to-chin height
FEATHER = 40
HEAD_TOLERANCE = 0.02
SHELF_LIMIT_PX = 12
HAZE_CORNER_ALPHA, HAZE_FLOOR = 8, 14
GREY = (118, 124, 134, 255)

# Neck seam rows (canvas rows of the 3R centre frame) and why; plan bounds rear < 780, look-back < 680, side < 700,
# each >= 20 px above its stability band top (rear 800, look-back 700, side 720), so the band stays byte-exact.
SEAMS = {
    'rear': (660, 'Back view: the head and neck are a column ~200 px wide down to row ~650 (solid width 195-213 px from '
                  'row 593 to 653); from ~660 the shoulders widen (240 px at 673, 256 at 693) into the ruff/shoulder mass. '
                  'The seam sits at the top of that mass; the lowest returned chin (h07, head down) is ~620, above the '
                  'feather top 640. 120 px above the band top 800.'),
    'lookback': (660, 'Look-back: the head-down hours (5-7) put the chin and whiskers at ~430-470 and every return redraws '
                      'the chest ruff with the head, so the ruff (front edge x 940-950 at rows 330-530) must come from the '
                      'return; a seam inside it leaves a shelf on the chest edge (up to 53 px at row 480, 12 px at 600). '
                      'The ruff narrows into the straight front leg at ~660 (right edge x 858-860 from row 666 to 746): '
                      'there the chest/leg edge agrees within 3 px on every hour and the leg stripes run on across the '
                      'seam; the left back edge sits 10-15 px inside the fixed body\'s (soft fur; flagged on the hours '
                      'over 12 px). 40 px above the band top 700.'),
    'side': (680, 'Side: head rows 455-600 (chin ~590), the chest ruff in front (right edge x 690-698, rows 593-693) and '
                  'the back from row ~610 on the left; the front leg begins below ~720. The head-down hours (5-7) drop '
                  'the chin to ~665 (just inside the feather top, where the return still carries > 90 %), so the seam '
                  'is at the foot of the ruff, the lowest the plan allows with the '
                  'feather clear of the band (feather 660-700, band top 720). Codex reshapes the chest with the head, '
                  'so the turned-down hours step inside the fixed chest edge (flagged).'),
}

# Head landmarks in each image's own native px, keyed 'centre' and by hour (int). Per frame: earApexes [[x, y], [x, y]]
# (left, right), skull [[x, y], [x, y]] (outer ear bases), pinna {'apex': [x, y], 'base': [[x, y], [x, y]]}, crown [x, y]
# (top of the skull), chin [x, y] (lowest point of the jaw). Absent keys = not visible. Empty: see UNMEASURED_WHY.
LANDMARKS = {
    'rear': {},
    'lookback': {},
    'side': {},
}
UNMEASURED_WHY = (
    'Not measured to 2 %: the clock turns the head in yaw AND pitch away from the centre pose (only 12/6 are pure pitch '
    'and 3/9 pure yaw, and even those start from a centre that is itself turned), so every 2D landmark distance is '
    'foreshortened by 10-50 % on most hours (rear h12 ear-apex spacing ~1.6x and h01 ~0.64x the centre\'s while the '
    'ear-base span stays ~1.0x; Codex also re-poses the mobile ears). Silhouette-only landmarks miss ears turned off the '
    'outline, the iris size varies with the drawn eyelids (look-back 0.55-1.06x the centre\'s), and hand-read landmarks '
    'on these fluffy heads carry about +/-5 native px, more than the 2 % gate on a 70-270 px measure. No frame was '
    'rescaled: the head keeps the body-alignment scale. Check size on clock-<f>-heads.png (centre head outline in red).')


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def rel(path):
    return Path(path).relative_to(ROOT).as_posix()


def alpha_of(image):
    return np.asarray(image.getchannel('A'), dtype=np.uint8)


def font(size):
    for name in ('segoeui.ttf', 'arial.ttf'):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            pass
    return ImageFont.load_default()


def return_path(name, hour):
    return ROOT / f'draft-assets/codex-v12/clock-{name}-{JOB[hour]}/{name}-h{hour:02d}.png'


def load_return(path):
    """RGBA; a return with a haze (any corner alpha > 8) has every pixel with alpha <= 14 zeroed first."""
    arr = np.asarray(Image.open(path).convert('RGBA')).copy()
    corners = [int(arr[0, 0, 3]), int(arr[0, -1, 3]), int(arr[-1, 0, 3]), int(arr[-1, -1, 3])]
    info = {'cornerAlpha': corners, 'hazeFloored': False}
    if max(corners) > HAZE_CORNER_ALPHA:
        low = arr[..., 3] <= HAZE_FLOOR
        info.update({'hazeFloored': True, 'hazeFloorAlpha': HAZE_FLOOR, 'hazePixelsZeroed': int((low & (arr[..., 3] > 0)).sum())})
        arr[low] = 0
    return Image.fromarray(arr, 'RGBA'), info


def warp(image, scale, offset, size=CANVAS, pad_out=0):
    """Uniform resample of `image` into a size x size canvas: canvas = scale * source + offset (float), LANCZOS on
    premultiplied alpha. pad_out > 0 renders a larger canvas (offset shifted by pad_out) to see what falls outside."""
    w, h = image.size
    ox, oy = offset[0] + pad_out, offset[1] + pad_out
    n = size + 2 * pad_out
    box = [-ox / scale, -oy / scale, (n - ox) / scale, (n - oy) / scale]
    pad = int(math.ceil(max(0, -box[0], -box[1], box[2] - w, box[3] - h))) + 8
    src = Image.new('RGBa', (w + 2 * pad, h + 2 * pad), (0, 0, 0, 0))
    src.paste(image.convert('RGBa'), (pad, pad))
    out = src.resize((n, n), Image.LANCZOS, box=(box[0] + pad, box[1] + pad, box[2] + pad, box[3] + pad))
    return out.convert('RGBA')


def body_window(seam, band):
    """Canvas mask of the alignment window: rows seam..band top at full width, then the band rows right of x0."""
    y0, y1, x0 = band
    win = np.zeros((CANVAS, CANVAS), dtype=bool)
    win[seam:y0, :] = True
    win[y0:y1 + 1, x0:] = True
    return win


def align(ret_alpha, target_alpha, window, base_scale, rel_lo=0.92, rel_hi=1.32):
    """Uniform scale S (canvas px per return px) and integer offset O maximising IoU of (alpha > 64) masks inside window."""
    ys, xs = np.nonzero(window)
    wy0, wy1, wx0, wx1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    target = ((target_alpha > 64) & window)[wy0:wy1, wx0:wx1].astype(np.float32)
    win = window[wy0:wy1, wx0:wx1].astype(np.float32)
    n_target = float(target.sum())
    tys, txs = np.nonzero(target)
    t_bottom, t_cx = wy0 + tys.max(), wx0 + txs.mean()
    h, w = ret_alpha.shape

    def score_at(scale, guess, reach):
        a = cv2.resize(ret_alpha, (round(w * scale), round(h * scale)), interpolation=cv2.INTER_AREA)
        m = (a > 64).astype(np.float32)
        if guess is None:
            rows = np.nonzero(m.any(axis=1))[0]
            sub = m[max(0, rows.max() - (wy1 - wy0)):rows.max() + 1]
            _, sx = np.nonzero(sub)
            guess = (int(round(t_cx - sx.mean())), int(round(t_bottom - rows.max())))
        big = np.zeros((CANVAS + 2 * reach, CANVAS + 2 * reach), dtype=np.float32)
        gx, gy = guess[0] + reach, guess[1] + reach
        sx0, sy0 = max(0, -gx), max(0, -gy)
        dx0, dy0 = max(0, gx), max(0, gy)
        cw, ch = min(m.shape[1] - sx0, big.shape[1] - dx0), min(m.shape[0] - sy0, big.shape[0] - dy0)
        if cw > 0 and ch > 0:
            big[dy0:dy0 + ch, dx0:dx0 + cw] = m[sy0:sy0 + ch, sx0:sx0 + cw]
        image = big[wy0:wy1 + 2 * reach, wx0:wx1 + 2 * reach]
        inter = cv2.matchTemplate(image, target, cv2.TM_CCORR)
        count = cv2.matchTemplate(image, win, cv2.TM_CCORR)
        iou = inter / np.maximum(1.0, n_target + count - inter)
        i, j = np.unravel_index(int(np.argmax(iou)), iou.shape)
        return float(iou[i, j]), (guess[0] + reach - j, guess[1] + reach - i)

    best = None
    for rel_scale in np.arange(rel_lo, rel_hi + 1e-9, 0.01):
        score, off = score_at(base_scale * rel_scale, None, 120)
        if best is None or score > best[0]:
            best = (score, base_scale * rel_scale, off)
    s0, off0 = best[1], best[2]
    for d in np.arange(-0.012, 0.0121, 0.001):
        score, off = score_at(s0 + base_scale * d, off0, 12)
        if score > best[0]:
            best = (score, s0 + base_scale * d, off)
    # The mask was scored at an integer size; place with exactly that scale.
    scale = round(w * best[1]) / w
    return {'scale': float(scale), 'offset': [int(best[2][0]), int(best[2][1])], 'iou': round(best[0], 5)}


def feather_weights(seam):
    """Weight of the return per canvas row: 1 at and above seam - FEATHER/2, 0 at and below seam + FEATHER/2, smoothstep."""
    y = np.arange(CANVAS, dtype=np.float64)
    t = np.clip((seam + FEATHER / 2.0 - y) / FEATHER, 0.0, 1.0)
    return t * t * (3 - 2 * t)


def composite(centre, ret, seam):
    c = np.asarray(centre).astype(np.float64)
    r = np.asarray(ret).astype(np.float64)
    w = feather_weights(seam)[:, None]
    ca, ra = c[..., 3] / 255.0, r[..., 3] / 255.0
    a = w * ra + (1 - w) * ca
    prem = w[..., None] * r[..., :3] * ra[..., None] + (1 - w[..., None]) * c[..., :3] * ca[..., None]
    rgb = np.where(a[..., None] > 0, prem / np.maximum(a[..., None], 1e-9), 0)
    out = np.dstack([np.clip(np.round(rgb), 0, 255), np.clip(np.round(a * 255), 0, 255)]).astype(np.uint8)
    lo, hi = seam - FEATHER // 2, seam + FEATHER // 2
    out[:lo + 1] = np.asarray(ret)[:lo + 1]
    out[hi:] = np.asarray(centre)[hi:]
    return Image.fromarray(out, 'RGBA')


def outline(alpha, threshold=128):
    m = (alpha > threshold).astype(np.uint8)
    return cv2.morphologyEx(m, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5))) > 0


def shelf(ret_alpha, centre_alpha, seam):
    """Largest step between the return's and the fixed body's outermost silhouette edge at the
    seam: the middle half of the feather (seam +/- FEATHER/4), where each image carries at least 15 % of the blend.
    Silhouette = alpha > 64 (the alignment mask's threshold), opened 5 px so whiskers and single wisps do not count."""
    rm, cm = outline(ret_alpha, 64), outline(centre_alpha, 64)
    out_px = in_px = 0
    rows = []
    for y in range(seam - FEATHER // 4, seam + FEATHER // 4 + 1):
        rx, cx = np.nonzero(rm[y])[0], np.nonzero(cm[y])[0]
        if not len(cx) or not len(rx):
            continue
        dl, dr = int(cx.min() - rx.min()), int(rx.max() - cx.max())    # > 0: the return sticks out
        out_px = max(out_px, dl, dr)
        in_px = max(in_px, -dl, -dr)
        rows.append([y, dl, dr])
    worst = max(rows, key=lambda r: max(abs(r[1]), abs(r[2]))) if rows else None
    return {'outPx': out_px, 'inPx': in_px, 'px': max(out_px, in_px), 'worstRow': worst,
            'flag': max(out_px, in_px) > SHELF_LIMIT_PX}


def clipping(image, scale, offset, seam):
    """Clipped when the head region (above the seam) touches the return's own 1254 border or the 1024 canvas edge."""
    a = alpha_of(image)
    h, w = a.shape
    seam_src = int(math.floor((seam - offset[1]) / scale))
    top = a[:max(0, min(h, seam_src))]
    border = 0
    if top.size:
        border = int((top[0] > 8).sum() + (top[:, 0] > 8).sum() + (top[:, -1] > 8).sum())
    pad = 96
    big = alpha_of(warp(image, scale, offset, pad_out=pad))[:pad + seam]
    ys, xs = np.nonzero(big > 8)
    inside = (xs >= pad) & (xs < pad + CANVAS) & (ys >= pad)
    outside = int((~inside).sum())
    ys, xs = ys[inside] - pad, xs[inside] - pad
    margin = int(min(xs.min(), ys.min(), CANVAS - 1 - xs.max())) if len(xs) else None
    return {'sourceBorderPx': border, 'outsideCanvasPx': outside, 'minCanvasMarginPx': margin,
            'clipped': bool(border > 0 or outside > 0 or (margin is not None and margin < 1))}


def dist(p, q):
    return float(np.hypot(p[0] - q[0], p[1] - q[1]))


def measures(lm):
    """Head measures (native px) from manual landmarks."""
    out = {}
    if lm.get('earApexes') and len(lm['earApexes']) == 2:
        out['earApexSpacing'] = dist(*lm['earApexes'])
    if lm.get('skull'):
        out['skullWidth'] = dist(*lm['skull'])
    if lm.get('pinna'):
        b0, b1 = lm['pinna']['base']
        out['pinnaLength'] = dist(lm['pinna']['apex'], ((b0[0] + b1[0]) / 2.0, (b0[1] + b1[1]) / 2.0))
    if lm.get('crown') and lm.get('chin'):
        out['crownToChin'] = dist(lm['crown'], lm['chin'])
    return out


def to_canvas(lm, scale, offset):
    def p(q):
        return [round(q[0] * scale + offset[0], 1), round(q[1] * scale + offset[1], 1)]
    out = {}
    for k, v in lm.items():
        if k == 'pinna':
            out[k] = {'apex': p(v['apex']), 'base': [p(v['base'][0]), p(v['base'][1])]}
        elif k in ('earApexes', 'skull'):
            out[k] = [p(q) for q in v]
        else:
            out[k] = p(v)
    return out


def head_check(name, hour, centre_lm, centre_scale, scale, offset):
    """Yaw-aware head ratio return/centre in canvas px; None when either side lacks the hour's measures."""
    keys = ['earApexSpacing', 'skullWidth'] if hour in FRONTAL_HOURS else ['pinnaLength', 'crownToChin']
    lm = LANDMARKS[name].get(hour)
    if not lm or not centre_lm:
        return {'measures': keys, 'landmarksNative': None, 'ratio': None, 'status': 'unmeasured', 'why': UNMEASURED_WHY}
    mr, mc = measures(lm), measures(centre_lm)
    ratios, px = {}, {}
    for k in keys:
        if k in mr and k in mc:
            px[k] = {'return': round(mr[k] * scale, 2), 'centre': round(mc[k] * centre_scale, 2)}
            ratios[k] = round(mr[k] * scale / (mc[k] * centre_scale), 4)
    ratio = round(float(np.mean(list(ratios.values()))), 4) if ratios else None
    return {'measures': keys, 'landmarksNative': lm, 'landmarksCanvas': to_canvas(lm, scale, offset),
            'measuresCanvasPx': px, 'ratios': ratios, 'ratio': ratio, 'status': 'measured' if ratio is not None else 'unmeasured',
            'why': None if len(ratios) == len(keys) else f'only {sorted(ratios)} measurable on both'}


def neck_base(ret_alpha, seam):
    xs = np.nonzero(outline(ret_alpha)[seam])[0]
    return [float((xs.min() + xs.max()) / 2.0), float(seam)]


def register_family(name, fam):
    seam, seam_why = SEAMS[name]
    y0, y1, x0 = fam['band']
    assert seam + FEATHER // 2 <= y0 - 0 and seam <= y0 - 20, (name, seam, fam['band'])
    centre_path = ROOT / f'draft-assets/head-turns-v12/{name}/centre.png'
    centre = Image.open(centre_path).convert('RGBA')
    centre_alpha = alpha_of(centre)
    raw_path = ROOT / fam['centre']['source']
    e, o = fam['centre']['scale'], fam['centre']['offset']
    assert sha(raw_path) == fam['centre']['sourceSHA256'], raw_path
    assert sha(centre_path) == fam['frames']['centre']['sha256'], centre_path
    # The recorded transform reproduces the 3R centre (alpha within 1 level everywhere).
    rewarp = alpha_of(warp(Image.open(raw_path).convert('RGBA'), e, o)).astype(int)
    rewarp_diff = int(np.abs(rewarp - centre_alpha.astype(int)).max())
    assert rewarp_diff <= 2, (name, rewarp_diff)
    out_dir = OUT / name
    out_dir.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(centre_path, out_dir / 'centre.png')
    centre_lm = LANDMARKS[name].get('centre', {})
    win = body_window(seam, fam['band'])
    frames, images, missing = {}, {}, []
    for index, hour in enumerate(HOURS):
        src = return_path(name, hour)
        if not src.exists():
            missing.append(hour)
            continue
        image, haze = load_return(src)
        ra = alpha_of(image)
        fit = align(ra, centre_alpha, win, e)
        scale, offset = fit['scale'], [float(v) for v in fit['offset']]
        head = head_check(name, hour, centre_lm, e, scale, offset)
        rescale = None
        if head['ratio'] is not None and abs(head['ratio'] - 1) > HEAD_TOLERANCE:
            k = 1.0 / head['ratio']
            pivot = neck_base(alpha_of(warp(image, scale, offset)), seam)
            offset = [pivot[0] - k * (pivot[0] - offset[0]), pivot[1] - k * (pivot[1] - offset[1])]
            scale = scale * k
            rescale = {'factor': round(k, 5), 'pivotCanvas': [round(pivot[0], 1), pivot[1]],
                       'why': f'head ratio {head["ratio"]} is off by more than {HEAD_TOLERANCE:.0%}'}
        placed = warp(image, scale, offset)
        frame = composite(centre, placed, seam)
        fa = alpha_of(frame)
        below = seam + FEATHER // 2
        diff_below = int((np.asarray(frame)[below:] != np.asarray(centre)[below:]).any(axis=2).sum())
        assert diff_below == 0, (name, hour, diff_below)
        ys, xs = np.nonzero(fa > 8)
        path = out_dir / f'h{hour:02d}.png'
        frame.save(path, optimize=True)
        images[hour] = frame
        clip = clipping(image, scale, offset, seam)
        sh = shelf(alpha_of(placed), centre_alpha, seam)
        frames[f'h{hour:02d}'] = {
            'hour': hour, 'index': index, 'file': rel(path), 'sha256': sha(path), 'source': rel(src), 'sourceSHA256': sha(src),
            'sourceSize': list(image.size), **haze,
            'alignment': {'returnToCentre1254': {'scale': round(fit['scale'] / e, 5),
                                                 'offset': [round((fit['offset'][0] - o[0]) / e, 2), round((fit['offset'][1] - o[1]) / e, 2)]},
                          'returnToCanvas': {'scale': round(fit['scale'], 6), 'offset': fit['offset']},
                          'bodyMaskIoU': fit['iou']},
            'placedScale': round(scale, 6), 'placedOffset': [round(offset[0], 3), round(offset[1], 3)],
            'headRescaled': rescale is not None, 'headRescale': rescale,
            'seamRow': seam, 'featherRows': [seam - FEATHER // 2, seam + FEATHER // 2],
            'head': head, 'headRatio': head['ratio'] if rescale is None else 1.0, 'headRatioBeforeRescale': head['ratio'],
            'clipped': clip['clipped'], 'clipping': clip, 'shelfPx': sh['px'], 'shelf': sh, 'shelfFlag': sh['flag'],
            'belowSeamDiffRowsFrom': below, 'belowSeamDiffPx': diff_below,
            'alpha8Bounds': [int(xs.min()), int(ys.min()), int(xs.max() - xs.min() + 1), int(ys.max() - ys.min() + 1)]}
    present = [h for h in HOURS if f'h{h:02d}' in frames]
    unmeasured = [h for h in present if frames[f'h{h:02d}']['head']['ratio'] is None]
    entry = {
        'id': fam['id'], 'clip': f'clock_{name}', 'stillLabel': fam['label'],
        'anchorX': fam['anchorX'], 'supportY': fam['supportY'], 'renderWidthAtSmall': fam['renderWidthAtSmall'],
        'headCrop': fam['headCrop'], 'band': fam['band'],
        'centre': {'file': rel(out_dir / 'centre.png'), 'sha256': sha(out_dir / 'centre.png'), 'copiedFrom': rel(centre_path),
                   'raw1254': rel(raw_path), 'raw1254SHA256': fam['centre']['sourceSHA256'],
                   'rawToCanvas': {'scale': e, 'offset': o}, 'rewarpAlphaMaxDiff': rewarp_diff,
                   'landmarksNative': centre_lm or None, 'landmarksCanvas': to_canvas(centre_lm, e, o) if centre_lm else None,
                   'measuresCanvasPx': {k: round(v * e, 2) for k, v in measures(centre_lm).items()}},
        'seamRow': seam, 'seamWhy': seam_why, 'featherRows': FEATHER, 'blend': 'premultiplied RGB and alpha, smoothstep over the feather rows',
        'alignmentWindow': f'rows {seam}..{y0 - 1} all columns + rows {y0}..{y1} columns >= {x0}, alpha > 64, IoU',
        'hoursPresent': present, 'hoursMissing': missing, 'complete': not missing,
        'headUnmeasured': unmeasured,
        'headRescaled': [h for h in present if frames[f'h{h:02d}']['headRescaled']],
        'clippedHours': [h for h in present if frames[f'h{h:02d}']['clipped']],
        'shelfHours': [h for h in present if frames[f'h{h:02d}']['shelfFlag']],
        'frames': frames}
    return entry, images


def sheet(name, fam, entry, images):
    """Clock face: the centre (fixed body) in the middle, hour frames at their clock positions, 2x the Small size."""
    zoom = fam['renderWidthAtSmall'] / CANVAS * 2
    centre = Image.open(ROOT / entry['centre']['file']).convert('RGBA')
    boxes = [alpha_of(im).nonzero() for im in [centre] + list(images.values())]
    x0 = min(int(b[1].min()) for b in boxes) - 8
    y0 = min(int(b[0].min()) for b in boxes) - 8
    x1 = max(int(b[1].max()) for b in boxes) + 8
    y1 = max(int(b[0].max()) for b in boxes) + 8
    tw, th = round((x1 - x0) * zoom), round((y1 - y0) * zoom)
    lab = 54
    radius = 2.25 * max(tw, th + lab)
    width = int(2 * radius + tw + 80)
    height = int(2 * radius + th + lab + 200)
    card = Image.new('RGBA', (width, height), GREY)
    draw = ImageDraw.Draw(card)
    big, small = font(30), font(19)
    cx, cy = width / 2, 120 + radius + (th + lab) / 2
    draw.ellipse([cx - radius, cy - radius, cx + radius, cy + radius], outline=(150, 156, 166, 255), width=2)

    def tile(image, px, py, title, lines, colour=(255, 255, 255, 255)):
        part = image.crop((x0, y0, x1, y1)).convert('RGBa').resize((tw, th), Image.LANCZOS).convert('RGBA')
        left, top = int(px - tw / 2), int(py - (th + lab) / 2)
        card.alpha_composite(part, (left, top))
        seam_y = top + (entry['seamRow'] - y0) * zoom
        draw.line([(left - 6, seam_y), (left + 10, seam_y)], fill=(0, 220, 255, 255), width=2)
        draw.text((left, top + th + 2), title, fill=colour, font=big)
        draw.text((left + 70, top + th + 4), lines, fill=colour, font=small)

    tile(centre, cx, cy, 'centre', 'fixed body (3R centre)')
    for hour in HOURS:
        ang = math.radians(hour % 12 * 30)
        px, py = cx + radius * math.sin(ang), cy - radius * math.cos(ang)
        f = entry['frames'].get(f'h{hour:02d}')
        if f is None:
            draw.rectangle([px - tw * 0.35, py - th * 0.35, px + tw * 0.35, py + th * 0.35], outline=(200, 80, 80, 255), width=3)
            draw.text((px - tw * 0.35 + 10, py - 20), f"{hour}  MISSING", fill=(255, 200, 200, 255), font=big)
            continue
        ratio = f['headRatioBeforeRescale']
        flags = [w for w, on in (('CLIPPED', f['clipped']), (f'SHELF {f["shelfPx"]}px', f['shelfFlag']),
                                 (f'head x{f["headRescale"]["factor"]}' if f['headRescaled'] else '', f['headRescaled'])) if on]
        text = (f'align x{f["alignment"]["returnToCentre1254"]["scale"]:.3f}  head '
                f'{"n/a" if ratio is None else f"{ratio:.3f}"}  shelf {f["shelfPx"]}px' + ('  ' + ' '.join(flags) if flags else ''))
        tile(images[hour], px, py, str(hour), text, (255, 210, 120, 255) if flags else (255, 255, 255, 255))
    title = (f'{entry["stillLabel"].split(" (")[0]}: 12-hour clock on ONE fixed body (V12 3bR-1, DEV, not owner-approved). '
             f'2x the Small size; seam row {entry["seamRow"]} (cyan tick), feather {FEATHER} rows; below it every frame is the centre\'s pixels.')
    draw.text((30, 24), title, fill=(255, 255, 255, 255), font=font(26))
    gaps = entry['hoursMissing']
    draw.text((30, 64), f'Hours present {len(entry["hoursPresent"])}/12' + (f'; missing {gaps}' if gaps else '') +
              (f'; head unmeasured {entry["headUnmeasured"]}' if entry['headUnmeasured'] else ''),
              fill=(255, 255, 255, 255), font=small)
    path = SHEETS / f'clock-{name}.png'
    card.convert('RGB').save(path, optimize=True)
    return rel(path)


def head_sheet(name, entry, images):
    """Head size check: the centre and every hour cropped above the seam at one common zoom, the centre's head and neck
    outline (alpha > 64) in red on every tile, the seam in cyan. A head drawn larger or smaller than the centre's shows
    as fur crossing or falling short of the red line in the same direction all round."""
    seam = entry['seamRow']
    centre = Image.open(ROOT / entry['centre']['file']).convert('RGBA')
    frames = [('centre', centre)] + [(str(h), images[h]) for h in HOURS if h in images]
    boxes = [np.nonzero(alpha_of(im)[:seam + FEATHER // 2] > 8) for _, im in frames]
    x0 = max(0, min(int(b[1].min()) for b in boxes) - 10)
    y0 = max(0, min(int(b[0].min()) for b in boxes) - 10)
    x1 = min(CANVAS, max(int(b[1].max()) for b in boxes) + 10)
    y1 = min(CANVAS, seam + FEATHER // 2 + 10)
    zoom = min(2.0, 560.0 / (x1 - x0))
    tw, th = round((x1 - x0) * zoom), round((y1 - y0) * zoom)
    ring = outline(alpha_of(centre), 64).astype(np.uint8)
    ring[seam + FEATHER // 2:] = 0
    ring = cv2.Canny(ring * 255, 50, 150)
    ring = cv2.resize(ring[y0:y1, x0:x1], (tw, th), interpolation=cv2.INTER_NEAREST)
    ring = cv2.dilate(ring, np.ones((2, 2), np.uint8)) > 0
    cols, lab = 5, 34
    card = Image.new('RGBA', (cols * (tw + 12) + 12, ((len(frames) + cols - 1) // cols) * (th + lab + 12) + 70), GREY)
    draw = ImageDraw.Draw(card)
    draw.text((12, 10), f'{entry["stillLabel"].split(" (")[0]}: head size check, {zoom:.2f}x canvas px. Red = the centre\'s head/neck '
                        f'outline (alpha > 64), cyan = seam row {seam}. Head unmeasured by landmarks; judge here.',
              fill=(255, 255, 255, 255), font=font(22))
    for i, (label, image) in enumerate(frames):
        tx, ty = 12 + (i % cols) * (tw + 12), 60 + (i // cols) * (th + lab + 12)
        tile = Image.new('RGBA', (tw, th), GREY)
        tile.alpha_composite(image.crop((x0, y0, x1, y1)).convert('RGBa').resize((tw, th), Image.LANCZOS).convert('RGBA'))
        arr = np.asarray(tile).copy()
        arr[ring] = (255, 40, 40, 255)
        sy = round((seam - y0) * zoom)
        arr[max(0, sy - 1):sy + 1, :] = (0, 220, 255, 255)
        card.alpha_composite(Image.fromarray(arr, 'RGBA'), (tx, ty))
        f = entry['frames'].get(f'h{int(label):02d}') if label != 'centre' else None
        text = label if f is None else f'{label} o\'clock  align x{f["alignment"]["returnToCentre1254"]["scale"]:.3f}  shelf {f["shelfPx"]}px'
        draw.text((tx, ty + th + 4), text, fill=(255, 255, 255, 255), font=font(20))
    path = SHEETS / f'clock-{name}-heads.png'
    card.convert('RGB').save(path, optimize=True)
    return rel(path)


def crops(name):
    """Gridded native head crops (10 px grid, 2x) of the centre and every present hour, for manual landmarks."""
    reg = json.loads(HEADTURNS.read_text())['families'][name]
    DEBUG.mkdir(parents=True, exist_ok=True)
    jobs = [('centre', ROOT / reg['centre']['source'], False)] + [(f'h{h:02d}', return_path(name, h), True) for h in HOURS if return_path(name, h).exists()]
    half, tall = {'rear': (240, 400), 'lookback': (280, 470), 'side': (260, 430)}[name]
    made = []
    for label, path, is_return in jobs:
        image = load_return(path)[0] if is_return else Image.open(path).convert('RGBA')
        a = alpha_of(image)
        ys, xs = np.nonzero(a > 128)
        top = int(ys.min())
        cols = xs[ys < top + 260]
        cx = int((cols.min() + cols.max()) / 2)
        box = (max(0, cx - half), max(0, top - 30), min(image.width, cx + half), min(image.height, top - 30 + tall))
        card = Image.new('RGBA', image.size, (110, 120, 130, 255))
        card.alpha_composite(image)
        crop = card.crop(box).resize(((box[2] - box[0]) * 2, (box[3] - box[1]) * 2), Image.LANCZOS)
        grid = Image.new('RGBA', crop.size, (0, 0, 0, 0))
        d = ImageDraw.Draw(grid)
        for gx in range((box[0] // 10 + 1) * 10, box[2], 10):
            x = (gx - box[0]) * 2
            d.line([(x, 0), (x, crop.height)], fill=(255, 255, 0, 200) if gx % 50 == 0 else (0, 0, 0, 70), width=1)
        for gy in range((box[1] // 10 + 1) * 10, box[3], 10):
            y = (gy - box[1]) * 2
            d.line([(0, y), (crop.width, y)], fill=(255, 255, 0, 200) if gy % 50 == 0 else (0, 0, 0, 70), width=1)
        crop.alpha_composite(grid)
        d = ImageDraw.Draw(crop)
        for gx in range((box[0] // 50 + 1) * 50, box[2], 50):
            for ty in (2, crop.height // 2, crop.height - 20):
                d.text(((gx - box[0]) * 2 + 2, ty), str(gx), fill=(255, 255, 0, 255), font=font(15))
        for gy in range((box[1] // 50 + 1) * 50, box[3], 50):
            for tx in (2, crop.width // 2, crop.width - 40):
                d.text((tx, (gy - box[1]) * 2 + 2), str(gy), fill=(255, 255, 0, 255), font=font(15))
        d.text((crop.width - 160, 24), f'{name} {label}', fill=(255, 255, 255, 255), font=font(22))
        made.append((label, crop.convert('RGB')))
    # Two crops per sheet, side by side.
    for i in range(0, len(made), 2):
        pair = made[i:i + 2]
        w = sum(c.width for _, c in pair) + 10 * (len(pair) - 1)
        out = Image.new('RGB', (w, max(c.height for _, c in pair)), (0, 0, 0))
        x = 0
        for _, c in pair:
            out.paste(c, (x, 0))
            x += c.width + 10
        out.save(DEBUG / f'grid-{name}-{"-".join(l for l, _ in pair)}.png')
    print('crops in', rel(DEBUG), [l for l, _ in made])


def main():
    reg = json.loads(HEADTURNS.read_text())
    SHEETS.mkdir(parents=True, exist_ok=True)
    record = {'version': '12.3bR-1', 'date': '2026-11-08', 'canvas': CANVAS, 'ownerApproved': False,
              'rule': 'one fixed body per state (the 3R centre frame); only the head and neck change per frame (owner option A, G3 2026-11-08)',
              'hourOrder': HOURS, 'frameIndexMeaning': 'index = app/ClockGaze.cs ClockAngles index: 0 = 12 o\'clock (head up), clockwise in 30 degree steps',
              'hourMeaning': '12 = head up, 3 = viewer\'s right, 6 = head down, 9 = viewer\'s left',
              'featherRows': FEATHER, 'headTolerance': HEAD_TOLERANCE, 'shelfLimitPx': SHELF_LIMIT_PX,
              'headMeasures': {'frontalHours': sorted(FRONTAL_HOURS), 'frontal': ['earApexSpacing', 'skullWidth'],
                               'turnedHours': sorted(TURNED_HOURS), 'turned': ['pinnaLength', 'crownToChin'],
                               'landmarks': 'LANDMARKS in Register-ClockV12.py, native px of each image; none recorded yet (see families.<f>.headSizeCheck)',
                               'rescaleRule': f'only when the measured ratio is off by more than {HEAD_TOLERANCE:.0%}: the region above the seam is rescaled by 1/ratio about the neck base',
                               'ratio': 'mean over the available measures of (return measure x placed scale) / (centre measure x centre scale), canvas px'},
              'hazeRule': f'a return with any corner alpha > {HAZE_CORNER_ALPHA} has every pixel with alpha <= {HAZE_FLOOR} zeroed before alignment (hazeFloored)',
              'resample': 'uniform LANCZOS on premultiplied alpha (float box); no warp, no repaint',
              'source': rel(HEADTURNS), 'families': {}}
    gaps = {}
    for name, fam in reg['families'].items():
        entry, images = register_family(name, fam)
        entry['sheet'] = sheet(name, fam, entry, images)
        entry['headSheet'] = head_sheet(name, entry, images)
        entry['headSizeCheck'] = UNMEASURED_WHY if entry['headUnmeasured'] else 'landmarks recorded for every hour'
        record['families'][name] = entry
        if entry['hoursMissing']:
            gaps[name] = entry['hoursMissing']
        print(f'{name}: seam {entry["seamRow"]}, hours {len(entry["hoursPresent"])}/12, missing {entry["hoursMissing"]}, '
              f'clipped {entry["clippedHours"]}, shelf {entry["shelfHours"]}, rescaled {entry["headRescaled"]}, unmeasured {entry["headUnmeasured"]}')
        for key, f in entry['frames'].items():
            print(f'   {key} align x{f["alignment"]["returnToCentre1254"]["scale"]:.4f} IoU {f["alignment"]["bodyMaskIoU"]:.4f} '
                  f'head {f["headRatioBeforeRescale"]} {f["head"].get("ratios")} shelf {f["shelfPx"]} (out {f["shelf"]["outPx"]} in {f["shelf"]["inPx"]}) '
                  f'clip {f["clipping"]} haze {f["hazeFloored"]} belowSeamDiff {f["belowSeamDiffPx"]}')
    record['gaps'] = gaps
    record['complete'] = not gaps
    RECORD.write_text(json.dumps(record, indent=2) + '\n')
    print('wrote', rel(RECORD), '; gaps:', gaps or 'none')


def seam_crops(name, hours):
    """Debug: the written frames around the seam (left and right outline), 3x, the fixed body's outline in red."""
    rec = json.loads(RECORD.read_text())['families'][name]
    seam = rec['seamRow']
    centre = Image.open(ROOT / rec['centre']['file']).convert('RGBA')
    edge = cv2.Canny(outline(alpha_of(centre), 64).astype(np.uint8) * 255, 50, 150) > 0
    tiles = []
    for h in hours:
        frame = Image.open(ROOT / rec['frames'][f'h{h:02d}']['file']).convert('RGBA')
        row = np.nonzero(outline(alpha_of(centre), 64)[seam])[0]
        for x in (int(row.min()), int(row.max())):
            box = (x - 60, seam - 60, x + 60, seam + 60)
            card = Image.new('RGBA', (CANVAS, CANVAS), GREY)
            card.alpha_composite(frame)
            arr = np.asarray(card).copy()
            arr[edge] = (255, 40, 40, 255)
            t = Image.fromarray(arr).crop(box).resize((360, 360), Image.NEAREST)
            d = ImageDraw.Draw(t)
            d.line([(0, 180), (12, 180)], fill=(0, 255, 255, 255), width=2)
            d.text((4, 4), f'h{h:02d} x{x}', fill=(255, 255, 255, 255), font=font(16))
            tiles.append(t)
    sheet = Image.new('RGB', (360 * min(8, len(tiles)), 360 * ((len(tiles) + 7) // 8)))
    for i, t in enumerate(tiles):
        sheet.paste(t.convert('RGB'), ((i % 8) * 360, (i // 8) * 360))
    DEBUG.mkdir(parents=True, exist_ok=True)
    sheet.save(DEBUG / f'seams-{name}.png')
    print('wrote', rel(DEBUG / f'seams-{name}.png'))


if __name__ == '__main__':
    if '--seam-crops' in sys.argv:
        i = sys.argv.index('--seam-crops')
        seam_crops(sys.argv[i + 1], [int(v) for v in sys.argv[i + 2].split(',')])
    elif '--crops' in sys.argv:
        crops(sys.argv[sys.argv.index('--crops') + 1])
    else:
        main()
