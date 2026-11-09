"""V12 3R: register the Codex head-turn frames for states 6 (rear), 7 (look-back) and 8 (side) into the 1024 runtime canvas.

Inputs (read only): draft-assets/codex-v12/head-turns-{rear,lookback,side}/<family>-<direction>.png, 1254 x 1254 RGBA,
five directions per family, each a whole cat drawn fresh by an image model (bodies are similar, not pixel-identical).

1. The CENTRE frame is the family's new body. It is uniformly resampled (LANCZOS on premultiplied alpha, as
   Register-ApprovedSittingV11.py; no repaint, no retexture) into the 1024 canvas by the state's size rule:
     rear (6), side (8): seated height (top alpha >= 64 row to supportY, Apply-OwnerSizesV12.py's measure) equals
       today's DEV still's, so the unchanged renderWidthAtSmall keeps them matched to sit/000 (owner exception);
     look-back (7): ear-apex and eye spacing (manual landmarks, +/-3 px) match today's still on screen at its
       unchanged width 241.06 (so the size Hatt accepted stays).
   Support line: rear 930 with the butt on it (tail may overhang), side 944 (25%-row rule, as v11), look-back 944
   with the 25% row 4 px above it (today's still has its support 5 of its native px = 4 new px below that row).
   Horizontal: the lower-body centroid sits where today's still's does, relative to the anchor (x 512).
2. The other four frames are registered to the centre by the lower body (D2): one uniform scale (searched for the
   best lower-body mask IoU) and a translation that matches the lower-body centroid, over the family's stability
   band (the same band app/DevArtwork.cs Band() uses for --test-body-stability).
3. Every frame is written whole (head, body, paws, tail together). No head is cut out or pasted.

Writes draft-assets/head-turns-v12/<family>/<direction>.png, planning/head-turns-registration-v12.json and the
family sheets local-data/reference-review/v12/<family>.png. A family whose frames drift more than 3 native px from
the centre is marked out of the pack (the threshold is never loosened)."""
import hashlib
import json
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'draft-assets/head-turns-v12'
SHEETS = ROOT / 'local-data/reference-review/v12'
RECORD = ROOT / 'planning/head-turns-registration-v12.json'
CANVAS, ROOT_X, MARGIN = 1024, 512, 4
DIRECTIONS = ['far-left', 'left', 'centre', 'right', 'far-right']
THRESHOLD_PX = 3.0
SIT_SEATED_SCREEN = 422 * 480.0 / 1024   # sit/000: ear tip 522 to support 944 at width 480 (sizes-owner-v12.json)

FAMILIES = {
    'rear': {
        'id': 6, 'label': '6. Sitting away (Codex head-turn centre, DEV)', 'folder': 'draft-assets/codex-v12/head-turns-rear',
        'old': {'source': 'draft-assets/restore-v11/rear.png', 'anchorX': 512, 'supportY': 930, 'width': 423.76},
        'width': 423.76, 'supportY': 930, 'sizeRule': 'seatedHeight', 'supportRule': 'butt',
        # Manual landmarks on rear-centre.png (+/-3 px): ear apexes; head box for the tracker's head point.
        'earApexes': [[690, 110], [925, 108]], 'headBox': [640, 95, 975, 330],
        # Stability band (canvas rows y0..y1, columns x >= x0): haunches below the shoulders, above the tail.
        'band': [800, 905, 470],
    },
    'lookback': {
        'id': 7, 'label': '7. Sitting look-back (Codex head-turn centre, DEV)', 'folder': 'draft-assets/codex-v12/head-turns-lookback',
        'old': {'source': 'draft-assets/numbered-sizing-v2/sit-lookback.png', 'anchorX': 3 + 1191 / 2.0, 'supportY': 1091, 'width': 241.05705275229357},
        'width': 241.05705275229357, 'supportY': 944, 'sizeRule': 'headLandmarks', 'supportRule': 'row25', 'supportBelowRow25': 4,
        # Manual landmarks (+/-3 px): solid pinna apexes and eye centres, on today's still and on lookback-centre.png.
        'oldLandmarks': {'earApexLeft': [797.5, 132.5], 'earApexRight': [1102.5, 121], 'eyeLeft': [917.5, 302.5], 'eyeRight': [1030, 295]},
        'landmarks': {'earApexLeft': [777.5, 150], 'earApexRight': [1100, 117.5], 'eyeLeft': [911, 320], 'eyeRight': [1032.5, 312.5]},
        'headBox': [745, 105, 1175, 470],
        'band': [700, 900, 470],
    },
    'side': {
        'id': 8, 'label': '8. Sitting side (Codex head-turn centre, DEV)', 'folder': 'draft-assets/codex-v12/head-turns-side',
        'old': {'source': 'draft-assets/restore-v11/side.png', 'anchorX': 512, 'supportY': 944, 'width': 410.87},
        'width': 410.87, 'supportY': 944, 'sizeRule': 'seatedHeight', 'supportRule': 'row25',
        'headBox': [790, 60, 1130, 400],
        'band': [720, 935, 420],
    },
}


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def rel(path):
    return Path(path).relative_to(ROOT).as_posix()


def alpha_of(image):
    return np.asarray(image.getchannel('A'), dtype=np.uint8)


def top64(alpha):
    return int(np.nonzero((alpha >= 64).any(axis=1))[0].min())


def row25(alpha):
    """Last row whose widest solid (alpha > 128) run is >= 25% of the widest run (Register-ApprovedSittingV11 rule)."""
    solid = alpha > 128
    best = np.zeros(alpha.shape[0], dtype=int)
    for y in np.nonzero(solid.any(axis=1))[0]:
        row = np.concatenate(([0], solid[y].astype(np.int8), [0]))
        edges = np.diff(row)
        starts, ends = np.nonzero(edges == 1)[0], np.nonzero(edges == -1)[0]
        best[y] = int((ends - starts).max())
    return int(np.nonzero(best >= 0.25 * best.max())[0].max())


def butt_row(alpha, spine_x):
    """Rear: median bottom (alpha >= 128) of the columns 30-60% of the way from the spine to the right flank,
    where the body (not the tail, which lies to the left) meets the floor."""
    cols = np.nonzero((alpha >= 128).any(axis=0))[0]
    right = cols.max()
    h = right - spine_x
    xs = range(int(spine_x + 0.3 * h), int(spine_x + 0.6 * h) + 1)
    bottoms = [np.nonzero(alpha[:, x] >= 128)[0].max() for x in xs]
    return float(np.median(bottoms))


def lower_body_x(alpha, support, seated):
    """x centroid (alpha > 64) of rows 5-35% of the seated height above the support line (placement only)."""
    y0, y1 = int(round(support - 0.35 * seated)), int(round(support - 0.05 * seated))
    ys, xs = np.nonzero(alpha[y0:y1 + 1] > 64)
    return float(xs.mean())


def scaled_rgba(image, scale):
    w, h = image.size
    size = (max(1, round(w * scale)), max(1, round(h * scale)))
    return image.convert('RGBa').resize(size, Image.LANCZOS).convert('RGBA'), size[0] / w


def place(scaled, ox, oy):
    canvas = Image.new('RGBA', (CANVAS, CANVAS), (0, 0, 0, 0))
    sx, sy = max(0, -ox), max(0, -oy)
    dx, dy = max(0, ox), max(0, oy)
    w = min(scaled.width - sx, CANVAS - dx)
    h = min(scaled.height - sy, CANVAS - dy)
    if w > 0 and h > 0:
        canvas.alpha_composite(scaled.crop((sx, sy, sx + w, sy + h)), (dx, dy))
    return canvas


def place_alpha(alpha_scaled, ox, oy):
    out = np.zeros((CANVAS, CANVAS), dtype=np.uint8)
    sx, sy = max(0, -ox), max(0, -oy)
    dx, dy = max(0, ox), max(0, oy)
    w = min(alpha_scaled.shape[1] - sx, CANVAS - dx)
    h = min(alpha_scaled.shape[0] - sy, CANVAS - dy)
    if w > 0 and h > 0:
        out[dy:dy + h, dx:dx + w] = alpha_scaled[sy:sy + h, sx:sx + w]
    return out


def band_mask(alpha, band):
    y0, y1, x0 = band
    mask = np.zeros(alpha.shape, dtype=bool)
    mask[y0:y1 + 1, x0:] = alpha[y0:y1 + 1, x0:] > 64
    return mask


def centroid(mask):
    ys, xs = np.nonzero(mask)
    if len(xs) == 0:
        return None
    return float(xs.mean()), float(ys.mean())


def iou(a, b):
    union = np.logical_or(a, b).sum()
    return float(np.logical_and(a, b).sum() / union) if union else 0.0


def visible_box(alpha, threshold=8):
    ys, xs = np.nonzero(alpha > threshold)
    return [int(xs.min()), int(ys.min()), int(xs.max() - xs.min() + 1), int(ys.max() - ys.min() + 1)]


def check_fits(alpha, label):
    box = visible_box(alpha)
    assert box[0] > 0 and box[1] > 0 and box[0] + box[2] < CANVAS and box[1] + box[3] < CANVAS, f'{label}: fur touches the canvas edge {box}'
    return box


def dist(a, b):
    return float(np.hypot(a[0] - b[0], a[1] - b[1]))


def register_centre(name, fam):
    src_path = ROOT / fam['folder'] / f'{name}-centre.png'
    image = Image.open(src_path).convert('RGBA')
    src_alpha = alpha_of(image)
    old = fam['old']
    old_image = Image.open(ROOT / old['source']).convert('RGBA')
    old_alpha = alpha_of(old_image)
    old_rate = old['width'] / old_image.width            # screen px (small) per old native px
    new_rate = fam['width'] / CANVAS                      # screen px (small) per new native px
    old_seated = old['supportY'] - top64(old_alpha)
    info = {'source': rel(src_path), 'sourceSHA256': sha(src_path), 'sizeRule': fam['sizeRule'], 'supportRule': fam['supportRule'],
            'today': {'source': old['source'], 'sha256': sha(ROOT / old['source']), 'nativeWidth': old_image.width,
                      'anchorX': old['anchorX'], 'supportY': old['supportY'], 'renderWidthAtSmall': old['width'],
                      'seatedNativePx': old_seated, 'seatedScreenPx': round(old_seated * old_rate, 3)}}

    if fam['supportRule'] == 'butt':
        spine = (fam['earApexes'][0][0] + fam['earApexes'][1][0]) / 2
        support_src = butt_row(src_alpha, spine)
        # The same rule on today's rear still (spine x 512) must give its owner-set butt row 930 (+/-3).
        check = butt_row(old_alpha, old['anchorX'])
        assert abs(check - old['supportY']) <= 3, f'butt rule gives {check} on today\'s rear still, not {old["supportY"]}'
        info['buttRule'] = {'spineX': spine, 'sourceRow': support_src, 'todayStillRow': check}
    else:
        support_src = row25(src_alpha)
        info['row25Source'] = support_src

    if fam['sizeRule'] == 'seatedHeight':
        target_seated = SIT_SEATED_SCREEN / new_rate
        scale = target_seated / (support_src - top64(src_alpha))
    else:
        o, n = fam['oldLandmarks'], fam['landmarks']
        ratios = {'earApexSpacing': dist(o['earApexLeft'], o['earApexRight']) / dist(n['earApexLeft'], n['earApexRight']),
                  'eyeSpacing': dist(o['eyeLeft'], o['eyeRight']) / dist(n['eyeLeft'], n['eyeRight'])}
        # Same on-screen size: new native = old native x old_rate / new_rate.
        scale = float(np.mean(list(ratios.values()))) * old_rate / new_rate
        info['landmarkRatiosOldToNewSource'] = {k: round(v, 5) for k, v in ratios.items()}
        target_seated = None

    for _ in range(4):
        scaled, eff = scaled_rgba(image, scale)
        target_row = fam['supportY'] - fam.get('supportBelowRow25', 0)
        oy = int(round(target_row - support_src * eff))
        a_scaled = alpha_of(scaled)
        trial = place_alpha(a_scaled, 0, oy)
        seated = fam['supportY'] - top64(trial)
        if target_seated is None or abs(seated - target_seated) <= 0.5:
            break
        scale *= target_seated / seated

    # Horizontal: lower-body centroid at today's offset from the anchor, converted to screen px and back.
    old_cx = lower_body_x(old_alpha, old['supportY'], old_seated)
    offset_screen = (old_cx - old['anchorX']) * old_rate
    trial = place_alpha(a_scaled, 0, oy)
    seated = fam['supportY'] - top64(trial)
    new_cx0 = lower_body_x(trial, fam['supportY'], seated)
    ox = int(round(ROOT_X + offset_screen / new_rate - new_cx0))
    canvas = place(scaled, ox, oy)
    alpha = alpha_of(canvas)
    seated = fam['supportY'] - top64(alpha)
    info.update({'scale': round(eff, 6), 'offset': [ox, oy], 'supportY': fam['supportY'], 'anchorX': ROOT_X,
                 'renderWidthAtSmall': fam['width'],
                 'seatedNativePx': seated, 'seatedScreenPx': round(seated * new_rate, 3),
                 'sitSeatedScreenPx': round(SIT_SEATED_SCREEN, 4),
                 'lowerBodyOffsetScreenPx': {'today': round(offset_screen, 2),
                                             'new': round((lower_body_x(alpha, fam['supportY'], seated) - ROOT_X) * new_rate, 2)}})
    if fam['sizeRule'] == 'seatedHeight':
        assert abs(seated * new_rate - SIT_SEATED_SCREEN) <= 1.0, (name, seated * new_rate)
    else:
        o, n = fam['oldLandmarks'], fam['landmarks']
        info['earApexSpacingScreenPx'] = {'today': round(dist(o['earApexLeft'], o['earApexRight']) * old_rate, 2),
                                          'new': round(dist(n['earApexLeft'], n['earApexRight']) * eff * new_rate, 2)}
        info['eyeSpacingScreenPx'] = {'today': round(dist(o['eyeLeft'], o['eyeRight']) * old_rate, 2),
                                      'new': round(dist(n['eyeLeft'], n['eyeRight']) * eff * new_rate, 2)}
    return scaled, eff, info


def fit_frame(image, base_scale, target_band, band, target_alpha):
    """Uniform scale (relative to the centre's) and translation aligning the lower-body band mask to the centre's:
    for each scale, translate until the band centroid matches the centre's; keep the scale with the best band IoU."""
    src_alpha = alpha_of(image)
    tc = centroid(target_band)
    target_bottom = int(np.nonzero((target_alpha > 128).any(axis=1))[0].max())
    h, w = src_alpha.shape
    best = None
    for rel_scale in np.round(np.arange(0.85, 1.2001, 0.005), 4):
        s = base_scale * rel_scale
        a = cv2.resize(src_alpha, (round(w * s), round(h * s)), interpolation=cv2.INTER_AREA)
        # Start: bottoms level, band x centroid level.
        oy = target_bottom - int(np.nonzero((a > 128).any(axis=1))[0].max())
        c = centroid(band_mask(place_alpha(a, 0, oy), band))
        ox = int(round(tc[0] - c[0])) if c else 0
        for _ in range(16):
            c = centroid(band_mask(place_alpha(a, ox, oy), band))
            if c is None:
                break
            dx, dy = tc[0] - c[0], tc[1] - c[1]
            if abs(dx) <= 0.5 and abs(dy) <= 0.5:
                break
            ox += int(np.sign(dx) * max(1, round(abs(dx)))) if abs(dx) > 0.5 else 0
            oy += int(np.sign(dy) * max(1, round(abs(dy)))) if abs(dy) > 0.5 else 0
        score = iou(band_mask(place_alpha(a, ox, oy), band), target_band)
        if best is None or score > best['iou']:
            best = {'relScale': float(rel_scale), 'scale': s, 'offset': [ox, oy], 'iou': score}
    return best


def unclipped_box(scaled, offset):
    ys, xs = np.nonzero(alpha_of(scaled) > 8)
    return [int(xs.min()) + offset[0], int(ys.min()) + offset[1], int(xs.max()) + offset[0], int(ys.max()) + offset[1]]


def register_family(name, fam):
    out_dir = OUT / name
    out_dir.mkdir(parents=True, exist_ok=True)
    centre_scaled, centre_scale, centre_info = register_centre(name, fam)
    band = list(fam['band'])
    centre_alpha = alpha_of(place(centre_scaled, *centre_info['offset']))
    centre_band = band_mask(centre_alpha, band)
    fits = {}
    for d in DIRECTIONS:
        src_path = ROOT / fam['folder'] / f'{name}-{d}.png'
        if d == 'centre':
            fits[d] = (src_path, centre_scaled, centre_scale, list(centre_info['offset']))
        else:
            image = Image.open(src_path).convert('RGBA')
            fit = fit_frame(image, centre_scale, centre_band, band, centre_alpha)
            scaled, scale = scaled_rgba(image, fit['scale'])
            fits[d] = (src_path, scaled, scale, list(fit['offset']))
    # One shift for the whole family when any frame would touch the canvas edge (as v11's horizontalShiftToFit):
    # every frame, the band, the anchor and the support line move together, so registration is unchanged.
    boxes = [unclipped_box(f[1], f[3]) for f in fits.values()]
    union = [min(b[0] for b in boxes), min(b[1] for b in boxes), max(b[2] for b in boxes), max(b[3] for b in boxes)]
    assert union[2] - union[0] <= CANVAS - 2 * MARGIN and union[3] - union[1] <= CANVAS - 2 * MARGIN, (name, 'family does not fit', union)
    shift = [0, 0]
    for axis in (0, 1):
        if union[axis] < MARGIN:
            shift[axis] = MARGIN - union[axis]
        elif union[axis + 2] > CANVAS - 1 - MARGIN:
            shift[axis] = CANVAS - 1 - MARGIN - union[axis + 2]
    band = [band[0] + shift[1], band[1] + shift[1], band[2] + shift[0]]
    anchor_x, support_y = ROOT_X + shift[0], fam['supportY'] + shift[1]
    centre_info['offset'] = [centre_info['offset'][0] + shift[0], centre_info['offset'][1] + shift[1]]
    centre_info.update({'familyShiftToFit': shift, 'anchorX': anchor_x, 'supportY': support_y})
    hb, eff, (ox, oy) = fam['headBox'], centre_scale, centre_info['offset']
    centre_info['headCrop'] = [round(hb[0] * eff + ox), round(hb[1] * eff + oy), round(hb[2] * eff + ox), round(hb[3] * eff + oy)]
    canvases = {d: place(f[1], f[3][0] + shift[0], f[3][1] + shift[1]) for d, f in fits.items()}
    centre_alpha = alpha_of(canvases['centre'])
    centre_band = band_mask(centre_alpha, band)
    centre_sil = centre_alpha > 64
    tc = centroid(centre_band)
    seated = support_y - top64(centre_alpha)
    assert seated == centre_info['seatedNativePx'], (name, seated, centre_info['seatedNativePx'])
    frames = {}
    for d in DIRECTIONS:
        src_path, _, scale, offset = fits[d]
        offset = [offset[0] + shift[0], offset[1] + shift[1]]
        canvas = canvases[d]
        alpha = alpha_of(canvas)
        box = check_fits(alpha, f'{name}/{d}')
        m = band_mask(alpha, band)
        c = centroid(m)
        drift = float(np.hypot(c[0] - tc[0], c[1] - tc[1]))
        path = out_dir / f'{d}.png'
        canvas.save(path, optimize=True)
        frames[d] = {'file': rel(path), 'sha256': sha(path), 'source': rel(src_path), 'sourceSHA256': sha(src_path),
                     'scale': round(scale, 6), 'scaleVsCentre': round(scale / centre_scale, 5), 'offset': [int(offset[0]), int(offset[1])],
                     'lowerBodyCentroid': [round(c[0], 3), round(c[1], 3)], 'driftNativePx': round(drift, 3),
                     'lowerBodyIoU': round(iou(m, centre_band), 4), 'silhouetteIoU': round(iou(alpha > 64, centre_sil), 4),
                     'alpha8Bounds': box, 'topRow64': top64(alpha)}
    centre_info['alpha8Bounds'] = frames['centre']['alpha8Bounds']
    worst = max(frames.values(), key=lambda f: f['driftNativePx'])
    passed = worst['driftNativePx'] <= THRESHOLD_PX
    return {'id': fam['id'], 'label': fam['label'], 'clip': f'look_{name}', 'band': band, 'bandMeaning': '[y0, y1, x0]: canvas rows y0..y1, columns x >= x0, alpha > 64',
            'anchorX': anchor_x, 'supportY': support_y, 'renderWidthAtSmall': fam['width'], 'headCrop': centre_info['headCrop'],
            'centre': centre_info, 'frames': frames, 'maxDriftNativePx': worst['driftNativePx'], 'thresholdNativePx': THRESHOLD_PX,
            'stabilityPass': passed, 'inPack': passed}, canvases


def font(size):
    for name in ('segoeui.ttf', 'arial.ttf'):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            pass
    return ImageFont.load_default()


def sheet(name, fam, entry, canvases):
    """Today's still, then far-left..far-right at runtime scale (2x the small size), floor and anchor aligned.
    Each new frame has the centre frame's silhouette ghosted in red under it; the stability band is outlined."""
    zoom = 2.0
    old = fam['old']
    old_image = Image.open(ROOT / old['source']).convert('RGBA')
    panels = [('today: ' + Path(old['source']).name, old_image, old['width'] / old_image.width * zoom, old['anchorX'], old['supportY'], None)]
    rate = fam['width'] / CANVAS * zoom
    for d in DIRECTIONS:
        f = entry['frames'][d]
        panels.append((f'{d}  drift {f["driftNativePx"]:.2f} px  IoU {f["silhouetteIoU"]:.3f} (lower {f["lowerBodyIoU"]:.3f})',
                       canvases[d], rate, entry['anchorX'], entry['supportY'], f))
    pw, ph, floor = 560, 620, 560
    img = Image.new('RGBA', (pw * len(panels), ph + 60), (118, 124, 134, 255))
    draw = ImageDraw.Draw(img)
    small, big = font(15), font(20)
    centre = canvases['centre']
    ghost = np.zeros((CANVAS, CANVAS, 4), dtype=np.uint8)
    ghost[..., 0], ghost[..., 3] = 220, (alpha_of(centre) > 64) * 120
    ghost = Image.fromarray(ghost, 'RGBA')
    for i, (label, image, scale, ax, sy, f) in enumerate(panels):
        x0 = i * pw
        cx = x0 + pw / 2
        size = (round(image.width * scale), round(image.height * scale))
        ox, oy = round(cx - ax * scale), round(floor - sy * scale)
        tile = Image.new('RGBA', img.size, (0, 0, 0, 0))
        if f is not None:
            ghost_layer = Image.new('RGBA', img.size, (0, 0, 0, 0))
            ghost_layer.paste(ghost.resize(size, Image.BILINEAR), (ox, oy))
            tile.alpha_composite(ghost_layer)
        part = image.convert('RGBa').resize(size, Image.LANCZOS).convert('RGBA')
        layer = Image.new('RGBA', img.size, (0, 0, 0, 0))
        layer.paste(part, (ox, oy))
        tile.alpha_composite(layer)
        img.alpha_composite(tile)
        draw.line([(x0 + 10, floor), (x0 + pw - 10, floor)], fill=(255, 230, 0, 255), width=1)
        draw.line([(cx, floor - 6), (cx, floor + 6)], fill=(255, 230, 0, 255), width=1)
        if f is not None:
            y0, y1, bx = entry['band']
            draw.rectangle([ox + bx * scale, oy + y0 * scale, x0 + pw - 6, oy + y1 * scale], outline=(0, 200, 255, 255))
        draw.text((x0 + 10, ph - 40), label, fill=(255, 255, 255, 255), font=small)
        if i:
            draw.line([(x0, 0), (x0, ph + 60)], fill=(90, 95, 105, 255), width=2)
    verdict = 'PASS' if entry['stabilityPass'] else 'FAIL'
    draw.text((10, ph + 10), f'{entry["label"]}: Codex head turns registered by the lower body (2x small size; red = centre silhouette; '
                             f'cyan = stability band). Max drift {entry["maxDriftNativePx"]:.2f} native px, limit 3: {verdict}. '
                             f'In DEV pack: {"yes" if entry["inPack"] else "no"}. Not owner-approved.', fill=(255, 255, 255, 255), font=big)
    path = SHEETS / f'{name}.png'
    img.convert('RGB').save(path, optimize=True)
    return rel(path)


def main():
    SHEETS.mkdir(parents=True, exist_ok=True)
    record = {'version': '12.3R', 'date': '2026-11-08', 'canvas': CANVAS, 'rootX': ROOT_X, 'thresholdNativePx': THRESHOLD_PX,
              'resample': 'uniform LANCZOS on premultiplied alpha; integer offsets; no repaint, no retexture, no warp, no blend',
              'wholeFrames': True, 'headPasted': False, 'ownerApproved': False,
              'registration': 'centre frame by the state size rule; other frames by lower-body band (uniform scale for best mask IoU, translation matching the band centroid)',
              'families': {}}
    for name, fam in FAMILIES.items():
        entry, canvases = register_family(name, fam)
        entry['sheet'] = sheet(name, fam, entry, canvases)
        record['families'][name] = entry
        print(f'{name}: centre scale {entry["centre"]["scale"]} offset {entry["centre"]["offset"]} seated {entry["centre"]["seatedScreenPx"]} screen px; '
              f'max drift {entry["maxDriftNativePx"]} px -> {"in pack" if entry["inPack"] else "LEFT OUT"}')
        for d, f in entry['frames'].items():
            print(f'   {d:9} scale {f["scale"]:.4f} (x{f["scaleVsCentre"]:.4f}) offset {f["offset"]} drift {f["driftNativePx"]:.3f} '
                  f'IoU {f["silhouetteIoU"]:.3f} lower {f["lowerBodyIoU"]:.3f}')
    RECORD.write_text(json.dumps(record, indent=2) + '\n')
    print('wrote', rel(RECORD))


if __name__ == '__main__':
    main()
