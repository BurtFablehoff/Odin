"""V12 2b: register the running frames by the HEAD (nose tip), not the torso.

v11 registered run frames by the eroded torso core (app/RunningMotion.cs), so the head stepped 24-25 native px per
frame and bobbed 69 px. Here the head is tracked by template matching: the reference frame's head crop (ears, eye,
muzzle; alpha composited on grey) is matched on every frame at small rotations, and the landmark is the reference
NOSE TIP carried through that match (the muzzle tip of <clip>/000, found as its leading alpha >= 128 pixel).

Registration, native px of the 1024 canvas (the units of RunningMotion and the DEV pack):
  dx = REF_X - noseX   the nose sits at one x relative to the anchor in every frame. REF_X is the v11 average placed
                       nose x, so the run envelope stays where it was on average.
  dy = targetY - noseY targetY is the least-squares-closest height track to the drawn nose heights such that
                       max - min <= BOB_BAND, every cyclic frame-to-frame change <= STEP, and no frame's lowest paw is
                       pushed below the deepest paw of the native cycle (the floor line). Frames whose head dips in
                       the art are lifted instead (the body leaps; the head stays level).
Kept tweens (draft-assets/run-tweens-v12/<clip>/order.json, written by Tween-RunFramesV12.py) are measured the same
way and registered so their nose lands midway between the placed noses of the two frames they sit between.

Writes planning/run-registration-v12.json and app/RunningRegistration.cs (generated; never edit by hand).
--sheet also writes local-data/reference-review/v12/run-heads.png (head crops with the tracked landmark)."""
import json
import sys
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'art/manifest.json'
OUT_JSON = ROOT / 'planning/run-registration-v12.json'
OUT_CS = ROOT / 'app/RunningRegistration.cs'
TWEENS = ROOT / 'draft-assets/run-tweens-v12'
SHEET = ROOT / 'local-data/reference-review/v12/run-heads.png'
CLIPS = ('run_left', 'run_right')
# v11 torso registration (app/RunningMotion.cs before V12 2b); REF_X keeps its average placed nose x.
TORSO = {'run_left': [16, 14, 15, -7, -19, 3, 16, 19, 23, -23, -68, -15, 28, 24, 22, -9, -29, -10],
         'run_right': [44, 83, 125, 48, -54, -46, -10, -27, -60, -26, -4, -12, -21, -19, -28, 8]}
# Reference head crop [x0, y0, x1, y1] of <clip>/000: both ears, the eye and the muzzle.
HEAD_BOX = {'run_left': [232, 640, 352, 784], 'run_right': [478, 684, 584, 806]}
ANGLES = [a * 2.0 for a in range(-6, 7)]  # head template rotations tried, degrees about the crop centre
BOB_BAND = 14.0
STEP = 4.0
GREY = 128.0
LANDMARK = 'nose tip (head template match from {clip}/000)'


def load(path):
    with Image.open(path) as image:
        return np.asarray(image.convert('RGBA')).astype(np.float32)


def on_grey(rgba):
    a = rgba[..., 3:4] / 255.0
    rgb = rgba[..., :3] * a + GREY * (1 - a)
    return cv2.cvtColor(rgb.astype(np.uint8), cv2.COLOR_RGB2GRAY)


def nose_tip(alpha, box, clip):
    """Leading-most alpha >= 128 pixel in the lower 60% of the head box (below the ears): the muzzle tip."""
    x0, y0, x1, y1 = box
    top = y0 + int((y1 - y0) * 0.4)
    ys, xs = np.nonzero(alpha[top:y1, x0:x1] >= 128)
    assert len(xs), f'{clip}: empty head box {box}'
    edge = xs.min() if clip == 'run_left' else xs.max()
    return float(edge + x0), float(ys[xs == edge].mean() + top)


class HeadTracker:
    def __init__(self, clip):
        self.clip = clip
        reference = load(ROOT / 'art/frames' / clip / '000.png')
        x0, y0, x1, y1 = HEAD_BOX[clip]
        self.template = on_grey(reference)[y0:y1, x0:x1]
        self.nose = nose_tip(reference[..., 3], HEAD_BOX[clip], clip)
        self.local = np.array([self.nose[0] - x0, self.nose[1] - y0, 1.0])
        h, w = self.template.shape
        self.rotations = []
        for angle in ANGLES:
            matrix = cv2.getRotationMatrix2D((w / 2.0, h / 2.0), angle, 1.0)
            tpl = cv2.warpAffine(self.template, matrix, (w, h), flags=cv2.INTER_LINEAR, borderMode=cv2.BORDER_REPLICATE)
            self.rotations.append((angle, tpl, matrix))

    def __call__(self, rgba):
        target = on_grey(rgba)
        best = None
        for angle, tpl, matrix in self.rotations:
            _, value, _, loc = cv2.minMaxLoc(cv2.matchTemplate(target, tpl, cv2.TM_CCOEFF_NORMED))
            if best is None or value > best[0]:
                best = (value, angle, loc, matrix)
        value, angle, loc, matrix = best
        px, py = matrix @ self.local
        return float(loc[0] + px), float(loc[1] + py), float(value), angle


def lowest_paw(alpha):
    return int(np.nonzero((alpha >= 64).any(axis=1))[0].max())


def bounds(alpha, threshold=64):
    ys, xs = np.nonzero(alpha >= threshold)
    return [int(xs.min()), int(ys.min()), int(xs.max() - xs.min() + 1), int(ys.max() - ys.min() + 1)]


def band_projection(v, width):
    """Closest (least squares) vector whose max - min <= width: clamp into [c, c + width] with the best c."""
    if v.max() - v.min() <= width:
        return v.copy()
    lo, hi = float(v.min()), float(v.max() - width)
    cost = lambda c: float(((np.clip(v, c, c + width) - v) ** 2).sum())
    for _ in range(80):
        m1, m2 = lo + (hi - lo) / 3, hi - (hi - lo) / 3
        if cost(m1) <= cost(m2):
            hi = m2
        else:
            lo = m1
    c = (lo + hi) / 2
    return np.clip(v, c, c + width)


def pair_projection(v, j, step):
    """Closest vector with |v[j+1] - v[j]| <= step (cyclic pair)."""
    k = (j + 1) % len(v)
    d = v[k] - v[j]
    out = v.copy()
    if abs(d) > step:
        excess = (abs(d) - step) / 2 * np.sign(d)
        out[j] += excess
        out[k] -= excess
    return out


def vertical_targets(nose_y, paw_y, floor, band, step, iterations=4000):
    """Dykstra's projection of the drawn nose heights onto the intersection of: max - min <= band, every cyclic
    step <= step, and target <= noseY + floor - pawY (no paw below the floor). Returns the placed nose heights."""
    y = np.asarray(nose_y, float)
    upper = y + (floor - np.asarray(paw_y, float))
    projections = [lambda v: np.minimum(v, upper), lambda v: band_projection(v, band)]
    projections += [(lambda v, j=j: pair_projection(v, j, step)) for j in range(len(y))]
    x = y.copy()
    increments = [np.zeros_like(y) for _ in projections]
    for _ in range(iterations):
        for i, project in enumerate(projections):
            z = project(x + increments[i])
            increments[i] = x + increments[i] - z
            x = z
    return x


def head_metrics(xs, ys):
    """Largest cyclic frame-to-frame nose jump (euclidean) and the bob (max - min nose y), placed space."""
    xs, ys = np.asarray(xs, float), np.asarray(ys, float)
    steps = np.hypot(np.roll(xs, -1) - xs, np.roll(ys, -1) - ys)
    return float(steps.max()), float(ys.max() - ys.min())


def register_native(clip, tracker):
    rows = []
    for i, path in enumerate(sorted((ROOT / 'art/frames' / clip).glob('*.png'))):
        rgba = load(path)
        x, y, score, angle = tracker(rgba)
        alpha = rgba[..., 3]
        rows.append({'frame': path.stem, 'source': path.relative_to(ROOT).as_posix(), 'tween': False,
                     'landmark': LANDMARK.format(clip=clip), 'x': round(x, 2), 'y': round(y, 2),
                     'matchScore': round(score, 4), 'headAngleDeg': angle, 'lowestPawY': lowest_paw(alpha),
                     'bounds': bounds(alpha), 'torsoOffsetV11': TORSO[clip][i]})
    assert len(rows) == len(TORSO[clip]), (clip, len(rows))
    ref_x = round(float(np.mean([r['x'] + r['torsoOffsetV11'] for r in rows])), 2)
    floor = max(r['lowestPawY'] for r in rows)
    targets = vertical_targets([r['y'] for r in rows], [r['lowestPawY'] for r in rows], floor, BOB_BAND, STEP)
    for r, target in zip(rows, targets):
        r['dx'] = round(ref_x - r['x'], 2)
        r['dy'] = round(float(target) - r['y'], 2)
        r['placedX'] = round(r['x'] + r['dx'], 2)
        r['placedY'] = round(r['y'] + r['dy'], 2)
        r['pawBelowFloor'] = round(r['lowestPawY'] + r['dy'] - floor, 2)
        assert r['pawBelowFloor'] <= 0.5, (clip, r['frame'], r['pawBelowFloor'])
    before = head_metrics([r['x'] + r['torsoOffsetV11'] for r in rows], [r['y'] for r in rows])
    after = head_metrics([r['placedX'] for r in rows], [r['placedY'] for r in rows])
    summary = {'refX': ref_x, 'floorY': floor, 'bobBand': BOB_BAND, 'stepLimit': STEP,
               'headStepV11Torso': round(before[0], 2), 'bobV11Torso': round(before[1], 2),
               'headStep': round(after[0], 2), 'bob': round(after[1], 2),
               'maxPawLiftAboveFloor': round(-min(r['pawBelowFloor'] for r in rows), 2)}
    return rows, summary


def register_tweens(clip, tracker, native):
    order_path = TWEENS / clip / 'order.json'
    if not order_path.exists():
        return None
    order = json.loads(order_path.read_text())
    by_name = {r['frame']: r for r in native}
    sequence = []
    for item in order['frames']:
        if not item['tween']:
            row = dict(by_name[item['name']])
            sequence.append(row)
            continue
        a, b = by_name[item['between'][0]], by_name[item['between'][1]]
        rgba = load(ROOT / item['source'])
        x, y, score, angle = tracker(rgba)
        alpha = rgba[..., 3]
        mid_x, mid_y = (a['placedX'] + b['placedX']) / 2, (a['placedY'] + b['placedY']) / 2
        row = {'frame': item['name'], 'source': item['source'], 'tween': True, 'between': item['between'],
               'landmark': LANDMARK.format(clip=clip), 'x': round(x, 2), 'y': round(y, 2),
               'matchScore': round(score, 4), 'headAngleDeg': angle, 'lowestPawY': lowest_paw(alpha),
               'bounds': bounds(alpha), 'seedOffset': item['seedOffset'],
               'dx': round(mid_x - x, 2), 'dy': round(mid_y - y, 2)}
        row['placedX'] = round(row['x'] + row['dx'], 2)
        row['placedY'] = round(row['y'] + row['dy'], 2)
        # The tween was built in placed space and stored shifted back by seedOffset; the measured head must agree.
        row['seedDisagreement'] = round(float(np.hypot(row['dx'] - item['seedOffset'][0], row['dy'] - item['seedOffset'][1])), 2)
        floor = max(r['lowestPawY'] for r in native)
        row['pawBelowFloor'] = round(row['lowestPawY'] + row['dy'] - floor, 2)
        sequence.append(row)
    step, bob = head_metrics([r['placedX'] for r in sequence], [r['placedY'] for r in sequence])
    return {'frames': sequence, 'count': len(sequence), 'tweens': sum(r['tween'] for r in sequence),
            'headStep': round(step, 2), 'bob': round(bob, 2),
            'maxSeedDisagreement': max([r.get('seedDisagreement', 0) for r in sequence])}


def cs_array(kind, values):
    if kind == 'double':
        return 'new double[]{' + ','.join(f'{v:.2f}' for v in values) + '}'
    if kind == 'bool':
        return 'new bool[]{' + ','.join('true' if v else 'false' for v in values) + '}'
    return 'new string[]{' + ','.join(f'"{v}"' for v in values) + '}'


def write_cs(report):
    lines = ['// GENERATED by tools/local/Measure-RunHeadsV12.py from planning/run-registration-v12.json. Do not edit by hand.',
             '// V12 2b head registration of the running frames: per frame the tracked nose tip (x, y) and the placement',
             '// offset (dx, dy) that holds it steady, native px of the 1024 canvas.',
             'namespace OdinV2', '{', '    internal static class RunningRegistration', '    {']
    for clip in CLIPS:
        name = 'Left' if clip == 'run_left' else 'Right'
        rows = report['clips'][clip]['native']
        for field, key in (('NoseX', 'x'), ('NoseY', 'y'), ('Dx', 'dx'), ('Dy', 'dy')):
            lines.append(f'        public static readonly double[] {name}{field}={cs_array("double", [r[key] for r in rows])};')
    lines.append('#if ODIN_DEV')
    lines.append('        // DEV play order: original frames and kept optical-flow tweens, as the DEV pack\'s run action entries.')
    for clip in CLIPS:
        name = 'Left' if clip == 'run_left' else 'Right'
        dev = report['clips'][clip].get('dev') or {'frames': report['clips'][clip]['native']}
        rows = dev['frames']
        lines.append(f'        public static readonly string[] Dev{name}Frames={cs_array("string", [r["frame"] for r in rows])};')
        lines.append(f'        public static readonly bool[] Dev{name}Tween={cs_array("bool", [r["tween"] for r in rows])};')
        for field, key in (('NoseX', 'x'), ('NoseY', 'y'), ('Dx', 'dx'), ('Dy', 'dy')):
            lines.append(f'        public static readonly double[] Dev{name}{field}={cs_array("double", [r[key] for r in rows])};')
    lines += ['#endif', '    }', '}', '']
    OUT_CS.write_text('\n'.join(lines), encoding='utf-8')


def sheet(report):
    SHEET.parent.mkdir(parents=True, exist_ok=True)
    cells = []
    for clip in CLIPS:
        rows = (report['clips'][clip].get('dev') or {'frames': report['clips'][clip]['native']})['frames']
        for r in rows:
            image = Image.open(ROOT / r['source']).convert('RGBA')
            cx, cy = r['x'], r['y']
            left, top = int(cx) - 120, int(cy) - 120
            cell = Image.new('RGBA', (240, 240), (200, 200, 200, 255))
            cell.alpha_composite(image.crop((left, top, left + 240, top + 240)))
            d = ImageDraw.Draw(cell)
            px, py = cx - left, cy - top
            d.ellipse([px - 4, py - 4, px + 4, py + 4], outline=(255, 0, 0, 255), width=2)
            d.text((4, 4), f"{clip} {r['frame']} {r['matchScore']:.2f}", fill=(200, 0, 120, 255) if r['tween'] else (0, 0, 0, 255))
            cells.append(cell)
    cols = 12
    out = Image.new('RGBA', (cols * 240, ((len(cells) + cols - 1) // cols) * 240), (255, 255, 255, 255))
    for i, cell in enumerate(cells):
        out.alpha_composite(cell, ((i % cols) * 240, (i // cols) * 240))
    out.save(SHEET)
    print('sheet', SHEET)


def main():
    report = {'version': 'v12-2b', 'generatedBy': 'tools/local/Measure-RunHeadsV12.py', 'units': 'native px of the 1024 canvas',
              'landmark': 'nose tip, tracked by matching the head crop of <clip>/000 (rotations ' +
                          f'{ANGLES[0]:+.0f}..{ANGLES[-1]:+.0f} deg) on every frame',
              'offsets': 'dx, dy move the sprite (positive = right, down); placedX/placedY = x + dx, y + dy',
              'thresholds': {'headStepNative': 8, 'headStepWithTweens': 4, 'bob': 20}, 'clips': {}}
    for clip in CLIPS:
        tracker = HeadTracker(clip)
        native, summary = register_native(clip, tracker)
        entry = {'referenceNose': [round(v, 2) for v in tracker.nose], 'headBox': HEAD_BOX[clip], 'summary': summary,
                 'native': native}
        dev = register_tweens(clip, tracker, native)
        if dev:
            entry['dev'] = dev
        report['clips'][clip] = entry
        print(f"{clip}: v11 torso step {summary['headStepV11Torso']} bob {summary['bobV11Torso']} -> head step "
              f"{summary['headStep']} bob {summary['bob']}; refX {summary['refX']}; max paw lift {summary['maxPawLiftAboveFloor']}")
        if dev:
            print(f"   dev sequence {dev['count']} frames ({dev['tweens']} tweens): step {dev['headStep']} bob {dev['bob']} "
                  f"max seed disagreement {dev['maxSeedDisagreement']}")
    OUT_JSON.write_text(json.dumps(report, indent=1) + '\n', encoding='utf-8')
    write_cs(report)
    print('wrote', OUT_JSON.relative_to(ROOT).as_posix(), 'and', OUT_CS.relative_to(ROOT).as_posix())
    if '--sheet' in sys.argv:
        sheet(report)


if __name__ == '__main__':
    main()
