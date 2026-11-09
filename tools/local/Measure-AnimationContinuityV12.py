"""V12 step 2a: size each animation by CONTINUITY with the state it leaves and returns to.

Groom, stretch, run (zoomies) and jump leave from and return to sitting, so their FIRST and LAST
drawn frames, as placed at runtime, are compared with sit/000 at its runtime size (480 at small).
A clip width comes from the median of the per-metric width estimates (every metric of the first
and last frame); every metric must then land within 5% of the reference at both ends.

Metrics (native px on the frame exactly as the runtime draws it):
  earSpacing   apex to apex (front views only)
  seatedHeight topmost alpha >= 64 row (ear tip) down to the runtime support line (seated frames only)
  headWidth    alpha >= 64 silhouette width on the eye row (front views only)
  pinnaHeight  near-ear pinna: perpendicular distance from the SOLID pinna tip (wisps excluded) to the base line
               (turned / profile views; Hatt 2026-11-07 "ear size is an indicator too, especially
               for angles"). Reference: side #8's near ear (a turned seated view matched to sit's
               seated height in v12); sit's front-ear pinna is recorded as a cross-check.
Ear apexes are detected automatically (topmost alpha >= 64 pixel near a guess). Eye centres and
pinna base points are manual and stored in the output JSON and drawn on the sheet.

Writes planning/animation-continuity-v12.json and local-data/reference-review/v12/continuity.png.
--strict exits 1 when any animation misses the 5% tolerance."""
import io
import json
import statistics
import sys
import zipfile
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
PACK = ROOT / 'local-data/reference-review/dev-latest-art/dev-artwork.zip'
ART = ROOT / 'art/manifest.json'
POLICY = ROOT / 'planning/sizes-owner-v12.json'
OUT_JSON = ROOT / 'planning/animation-continuity-v12.json'
OUT_SHEET = ROOT / 'local-data/reference-review/v12/continuity.png'
ALPHA = 64
TOLERANCE = 0.05
SHEET_ZOOM = 2  # uniform zoom of the small size so landmarks are legible; relative sizes unchanged

# Manual landmarks, native px of each frame as drawn. Ear apex guesses are refined automatically.
LANDMARKS = {
    'sit/000': {'ears': {'left': [449, 522], 'right': [617, 532]},
                'eyes': [[496, 616], [551, 620]],
                'pinna': {'left': {'base': [[451, 566], [500, 566]]},
                          'right': {'base': [[570, 566], [610, 570]]}}},
    'side8': {'pinna': {'near': {'apex': [574, 451], 'base': [[561, 517], [614, 500]]}}},
    'groom/000': {'ears': {'left': [380, 528], 'right': [540, 530]}, 'eyes': [[426, 630], [487, 633]]},
    'groom/015': {'ears': {'left': [382, 525], 'right': [539, 555]}, 'eyes': [[413, 642], [473, 655]]},
    'stretch/000': {'ears': {'left': [365, 537], 'right': [527, 547]}, 'eyes': [[412, 629], [466, 631]]},
    'stretch/027': {'ears': {'left': [299, 543], 'right': [454, 547]}, 'eyes': [[345, 634], [398, 636]]},
    'jump_left/000': {'pinna': {'near': {'apex': [562, 226], 'base': [[517, 260], [558, 280]]}}},
    'jump_left/015': {'pinna': {'near': {'apex': [531, 218], 'base': [[490, 257], [535, 272]]}}},
    'jump_right/000': {'pinna': {'near': {'apex': [451, 242], 'base': [[446, 292], [488, 277]]}}},
    'jump_right/015': {'pinna': {'near': {'apex': [452, 240], 'base': [[447, 293], [486, 277]]}}},
    'run_left/000': {'pinna': {'near': {'apex': [319, 654], 'base': [[299, 687], [330, 700]]}}},
    'run_left/017': {'pinna': {'near': {'apex': [318, 655], 'base': [[299, 686], [329, 701]]}}},
    'run_right/000': {'pinna': {'near': {'apex': [525, 693], 'base': [[505, 733], [539, 724]]}}},
    'run_right/015': {'pinna': {'near': {'apex': [568, 694], 'base': [[550, 730], [585, 722]]}}},
}

FRONT = ['earSpacing', 'seatedHeight', 'headWidth']
TURNED_SEATED = ['seatedHeight', 'pinnaHeight']
RUNNING = ['pinnaHeight']
ANIMATIONS = [
    {'name': 'groom', 'kind': 'native', 'first': 0, 'last': 15, 'metrics': FRONT,
     'view': 'front seated (head slightly tilted, paw raised)'},
    {'name': 'stretch', 'kind': 'legacy', 'first': 0, 'last': 27, 'metrics': FRONT, 'view': 'front seated'},
    {'name': 'run_left', 'kind': 'legacy', 'first': 0, 'last': 17, 'metrics': RUNNING,
     'view': 'running, head in profile facing left (not seated: no seated height; ear spacing / front head width not comparable)'},
    {'name': 'run_right', 'kind': 'legacy', 'first': 0, 'last': 15, 'metrics': RUNNING,
     'view': 'running, head in profile facing right (not seated: no seated height; ear spacing / front head width not comparable)'},
    {'name': 'jump_left', 'kind': 'native', 'first': 0, 'last': 15, 'metrics': TURNED_SEATED,
     'view': 'seated, head turned ~3/4 left and pitched up (ear spacing / front head width not comparable)'},
    {'name': 'jump_right', 'kind': 'native', 'first': 0, 'last': 15, 'metrics': TURNED_SEATED,
     'view': 'seated, head turned ~3/4 right and pitched up (ear spacing / front head width not comparable)'},
]


def rgba(data):
    with Image.open(io.BytesIO(data)) as image:
        return image.convert('RGBA')


class Frame:
    def __init__(self, label, image, support, anchor_x, width, native_width, source):
        self.label, self.image, self.support, self.anchor_x = label, image, float(support), float(anchor_x)
        self.width, self.native_width, self.source = float(width), native_width, source
        self.alpha = np.asarray(image)[..., 3] >= ALPHA

    def apex(self, guess, window=10):
        x0, y0 = guess
        cols = self.alpha[:, max(0, x0 - window):x0 + window + 1]
        rows = np.nonzero(cols.any(axis=1))[0]
        rows = rows[rows >= y0 - 20]
        top = int(rows.min())
        xs = np.nonzero(cols[top])[0] + max(0, x0 - window)
        return [round(float(xs.mean()), 1), top]

    def solid_apex(self, guess, window=12, threshold=200, run=3):
        """Solid pinna tip: topmost row near the guess holding >= `run` contiguous px of alpha >= 200,
        so sparse wispy ear hairs above the pinna are excluded (AGENTS.md: size by solid ear pinna)."""
        x0, y0 = guess
        left = max(0, x0 - window)
        solid = np.asarray(self.image)[..., 3][:, left:x0 + window + 1] >= threshold
        for y in range(max(0, y0 - 30), y0 + 20):
            xs = np.nonzero(solid[y])[0]
            if len(xs) < run:
                continue
            groups = np.split(xs, np.nonzero(np.diff(xs) > 1)[0] + 1)
            wide = [g for g in groups if len(g) >= run]
            if wide:
                return [round(float(wide[0].mean()) + left, 1), y]
        raise AssertionError(f'{self.label}: no solid pinna tip near {guess}')

    def top(self):
        return int(np.nonzero(self.alpha.any(axis=1))[0].min())

    def row_run(self, y, x):
        row = self.alpha[int(round(y))]
        x = int(round(x))
        assert row[x], (self.label, y, x)
        left = x
        while left > 0 and row[left - 1]:
            left -= 1
        right = x
        while right < len(row) - 1 and row[right + 1]:
            right += 1
        return left, right


def perpendicular(apex, base):
    (ax, ay), ((bx0, by0), (bx1, by1)) = apex, base
    dx, dy = bx1 - bx0, by1 - by0
    return abs(dx * (ay - by0) - dy * (ax - bx0)) / (dx * dx + dy * dy) ** 0.5


def measure(frame, key):
    marks = LANDMARKS[key]
    out = {'landmarks': {}, 'native': {}}
    if 'ears' in marks:
        left, right = (frame.apex(marks['ears'][side]) for side in ('left', 'right'))
        out['landmarks']['earApexes'] = {'left': left, 'right': right, 'detected': 'auto (topmost alpha>=64 near guess)'}
        out['native']['earSpacing'] = round(float(np.hypot(right[0] - left[0], right[1] - left[1])), 2)
    top = frame.top()
    out['landmarks']['earTipRow'] = top
    out['landmarks']['supportY'] = frame.support
    out['native']['seatedHeight'] = round(frame.support - top, 2)
    if 'eyes' in marks:
        (ex0, ey0), (ex1, ey1) = marks['eyes']
        row, centre = (ey0 + ey1) / 2, (ex0 + ex1) / 2
        left, right = frame.row_run(row, centre)
        out['landmarks']['eyes'] = {'points': marks['eyes'], 'detected': 'manual'}
        out['landmarks']['headWidthRow'] = {'y': round(row), 'left': left, 'right': right, 'detected': 'auto edges on manual eye row'}
        out['native']['headWidth'] = right - left + 1
    if 'pinna' in marks:
        heights, pinnae = [], {}
        for side, ear in marks['pinna'].items():
            guess = ear.get('apex') or marks['ears'][side]
            apex = frame.solid_apex(guess)
            height = perpendicular(apex, ear['base'])
            heights.append(height)
            pinnae[side] = {'apex': apex, 'base': ear['base'], 'height': round(height, 2),
                            'detected': 'apex auto (solid pinna tip: >=3 contiguous px alpha>=200), base manual'}
        out['landmarks']['pinna'] = pinnae
        out['native']['pinnaHeight'] = round(sum(heights) / len(heights), 2)
    return out


def load_frames():
    pack = zipfile.ZipFile(PACK)
    manifest = json.loads(pack.read('manifest.json'))
    art = json.loads(ART.read_text())
    poses = {p['id']: p for p in manifest['poses']}
    actions = {(a['clip'], a['frame']): a for a in manifest['actions']}
    widths = manifest['legacyActionWidths']

    def pose(pid):
        p = poses[pid]
        return Frame(f'pose {pid}', rgba(pack.read(p['key'])), p['supportY'], p.get('anchorX', 512),
                     p['renderWidthAtSmall'], p['nativeWidth'], p['source'])

    def frame(kind, clip, index):
        if kind == 'native':
            a = actions[(clip, index)]
            return Frame(f'{clip}/{index:03d}', rgba(pack.read(a['key'])), a['supportY'], a['anchorX'],
                         a['renderWidthAtSmall'], a['nativeWidth'], a['source'])
        source = f'art/frames/{clip}/{index:03d}.png'
        anchor = art['clips'][clip]['images'][index]['anchor_y']
        return Frame(f'{clip}/{index:03d}', rgba((ROOT / source).read_bytes()), anchor, art['foot_anchor'][0],
                     widths[clip], art['width'], source)

    return manifest, pose, frame


def screen(native, width, native_width):
    return native * width / native_width


def main():
    strict = '--strict' in sys.argv
    manifest, pose, frame = load_frames()
    policy = json.loads(POLICY.read_text())
    old = policy['animations']['v11_3']['legacyActionWidths']  # history kept in the policy
    applied = {g['name']: g['renderWidthAtSmall'] for g in policy['animations'].get('groups', [])}

    sit = pose(1)
    sit_m = measure(sit, 'sit/000')
    side = pose(8)
    side_m = measure(side, 'side8')
    refs = {
        'earSpacing': ('sit/000 (#1)', screen(sit_m['native']['earSpacing'], sit.width, sit.native_width)),
        'seatedHeight': ('sit/000 (#1)', screen(sit_m['native']['seatedHeight'], sit.width, sit.native_width)),
        'headWidth': ('sit/000 (#1)', screen(sit_m['native']['headWidth'], sit.width, sit.native_width)),
        'pinnaHeight': ('side #8 near ear', screen(side_m['native']['pinnaHeight'], side.width, side.native_width)),
    }
    sit_pinna = screen(sit_m['native']['pinnaHeight'], sit.width, sit.native_width)
    reference = {
        'sit': {'source': sit.source, 'renderWidthAtSmall': sit.width, 'nativeWidth': sit.native_width,
                'measured': sit_m, 'screenPx': {k: round(screen(v, sit.width, sit.native_width), 3)
                                                for k, v in sit_m['native'].items()}},
        'side8': {'source': side.source, 'renderWidthAtSmall': side.width, 'nativeWidth': side.native_width,
                  'measured': side_m, 'screenPx': {'pinnaHeight': round(refs['pinnaHeight'][1], 3)},
                  'pinnaCrossCheck': {'side8NearEarScreenPx': round(refs['pinnaHeight'][1], 3),
                                      'sitFrontEarsMeanScreenPx': round(sit_pinna, 3),
                                      'ratio': round(refs['pinnaHeight'][1] / sit_pinna, 4)}},
        'metricReference': {k: {'reference': name, 'screenPx': round(value, 3)} for k, (name, value) in refs.items()},
    }

    results, sheet_rows = [], []
    for spec in ANIMATIONS:
        name = spec['name']
        frames = {}
        for role in ('first', 'last'):
            index = spec[role]
            f = frame(spec['kind'], name, index)
            frames[role] = (f, measure(f, f'{name}/{index:03d}'))
        estimates = []
        for role, (f, m) in frames.items():
            for metric in spec['metrics']:
                estimates.append(refs[metric][1] * f.native_width / m['native'][metric])
        width = round(statistics.median(estimates), 2)
        per_frame, worst = {}, 0.0
        for role, (f, m) in frames.items():
            rows = {}
            for metric in spec['metrics']:
                value = screen(m['native'][metric], width, f.native_width)
                ratio = value / refs[metric][1]
                worst = max(worst, abs(ratio - 1))
                rows[metric] = {'native': m['native'][metric], 'screenPxAtChosenWidth': round(value, 3),
                                'reference': refs[metric][0], 'referenceScreenPx': round(refs[metric][1], 3),
                                'ratio': round(ratio, 4), 'delta': round(ratio - 1, 4),
                                'widthThatMatchesExactly': round(refs[metric][1] * f.native_width / m['native'][metric], 2)}
            unused = {k: v for k, v in m['native'].items() if k not in spec['metrics']}
            per_frame[role] = {'frame': f.label, 'source': f.source, 'supportY': f.support, 'anchorX': f.anchor_x,
                               'nativeWidth': f.native_width, 'packWidthAtMeasure': f.width,
                               'metrics': rows, 'recordedNotUsed': unused, 'landmarks': m['landmarks']}
        # Best any single width could do (diagnostic only; the chosen width stays the median).
        grid = np.arange(min(estimates) * 0.9, max(estimates) * 1.1, 0.01)
        misses = np.max([np.abs(grid / e - 1) for e in estimates], axis=0)
        minimax = {'width': round(float(grid[misses.argmin()]), 2), 'maxAbsDelta': round(float(misses.min()), 4)}
        start = {k: v['delta'] for k, v in per_frame['first']['metrics'].items()}
        end = {k: v['delta'] for k, v in per_frame['last']['metrics'].items()}
        passed = worst <= TOLERANCE
        results.append({
            'animation': name, 'entryState': 'sit (#1, sit/000)', 'exitState': 'sit (#1, sit/000)',
            'drawnFrom': 'native DEV pack action entries' if spec['kind'] == 'native' else 'legacy art/frames at legacyActionWidths',
            'view': spec['view'], 'metricsUsed': spec['metrics'],
            'references': {k: refs[k][0] for k in spec['metrics']},
            'framesUsed': [per_frame['first']['frame'], per_frame['last']['frame']],
            'widthEstimates': [round(e, 2) for e in estimates],
            'chosenWidth': width, 'v11_3Width': old[name], 'changeVsV11_3': round(width / old[name], 4),
            'appliedWidthInPolicy': applied.get(name),
            'startDelta': start, 'endDelta': end, 'maxAbsDelta': round(worst, 4), 'bestSingleWidth': minimax,
            'tolerance': TOLERANCE, 'pass': passed, 'frames': per_frame})
        sheet_rows.append((name, width, old[name], frames, per_frame, passed, worst))
        print(f"{name:<11} frames {per_frame['first']['frame']} / {per_frame['last']['frame']}  metrics {','.join(spec['metrics'])}"
              f"  v11.3 {old[name]:.2f} -> {width:.2f}  max|delta| {worst:.3f}  {'PASS' if passed else 'FAIL'}")

    out = {'version': '12.2a', 'date': '2026-11-07',
           'rule': 'An animation is sized by continuity with the state it leaves and returns to: its first and last drawn '
                   'frames must match that state (sit/000 at 480 at the small size) within 5% on every metric used.',
           'method': __doc__.split('\n\n')[1].strip(),
           'widthRule': 'chosenWidth = median of (reference screen px x nativeWidth / frame native px) over every metric '
                        'of the first and last frame; delta = frame screen px at chosenWidth / reference - 1',
           'runStyleJump': 'Jump frames 002-012 draw ORIGINAL run frames (planning/run-style-jump-v11-3.json) at the run '
                           'clip width; jump frames 000-001 and 013-015 use the jump clip width. Both widths are set by '
                           'continuity with sit, so the run-style frames inherit the run clip size and the seated jump '
                           'frames the jump size. Apply-RunStyleJumpV11_3 places each run-style frame from the run width.',
           'reference': reference, 'animations': results, 'allPass': all(r['pass'] for r in results),
           'ownerApproved': False}
    OUT_JSON.write_text(json.dumps(out, indent=2) + '\n')
    print(OUT_JSON)
    draw_sheet(sit, sit_m, sheet_rows)
    if strict and not out['allPass']:
        sys.exit(1)


def font(size):
    for name in ('segoeui.ttf', 'arial.ttf'):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            pass
    return ImageFont.load_default()


def place(frame, m, width):
    """Frame at `width` (x SHEET_ZOOM), cropped to its alpha box; returns tile, support row, landmark mapper."""
    scale = width / frame.native_width * SHEET_ZOOM
    small = frame.image.resize((round(frame.image.width * scale), round(frame.image.height * scale)), Image.LANCZOS)
    left, top, right, bottom = small.getbbox()
    tile = small.crop((left, top, right, bottom)).copy()
    draw = ImageDraw.Draw(tile)

    def p(x, y):
        return x * scale - left, y * scale - top

    marks = m['landmarks']
    tip = p(0, marks['earTipRow'])[1]
    support = p(0, frame.support)[1]
    if 'earApexes' in marks:
        a, b = p(*marks['earApexes']['left']), p(*marks['earApexes']['right'])
        draw.line((a, b), fill=(220, 30, 30, 255), width=2)
        for q in (a, b):
            draw.ellipse((q[0] - 3, q[1] - 3, q[0] + 3, q[1] + 3), outline=(220, 30, 30, 255), width=2)
    if 'headWidthRow' in marks:
        row = marks['headWidthRow']
        a, b = p(row['left'], row['y']), p(row['right'] + 1, row['y'])
        draw.line((a, b), fill=(30, 90, 220, 255), width=2)
        for x, y in marks['eyes']['points']:
            q = p(x, y)
            draw.ellipse((q[0] - 2, q[1] - 2, q[0] + 2, q[1] + 2), fill=(30, 90, 220, 255))
    for ear in marks.get('pinna', {}).values():
        a, b = p(*ear['base'][0]), p(*ear['base'][1])
        apex = p(*ear['apex'])
        draw.line((a, b), fill=(20, 160, 60, 255), width=2)
        draw.line((apex, ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)), fill=(20, 160, 60, 255), width=1)
        draw.ellipse((apex[0] - 3, apex[1] - 3, apex[0] + 3, apex[1] + 3), outline=(20, 160, 60, 255), width=2)
    return tile, support, tip


def draw_sheet(sit, sit_m, rows):
    title_font, label_font, small_font = font(22), font(16), font(14)
    gap, margin, label_h = 36, 30, 96
    prepared, heights, widths = [], [], []
    sit_tile = place(sit, sit_m, sit.width)
    for name, width, old, frames, per_frame, passed, worst in rows:
        tiles = [sit_tile, place(frames['first'][0], frames['first'][1], width),
                 place(frames['last'][0], frames['last'][1], width), sit_tile]
        above = max(t[1] for t in tiles)
        below = max(t[0].height - t[1] for t in tiles)
        prepared.append((name, width, old, per_frame, passed, worst, tiles, above, below))
        heights.append(round(above + below) + label_h + 40)
        widths.append(margin * 2 + 150 + sum(t[0].width for t in tiles) + gap * 3 + 60)
    sheet = Image.new('RGBA', (max(widths + [1500]), sum(heights) + 90), (246, 244, 240, 255))
    draw = ImageDraw.Draw(sheet)
    draw.text((margin, 14), f'Odin v12 2a: animations sized by continuity with sitting - entry sit -> first frame ... last frame -> '
                            f'exit sit, runtime small size x{SHEET_ZOOM}, one floor line per row (DEV, not approved)',
              fill=(20, 20, 20, 255), font=title_font)
    draw.text((margin, 46), 'red: ear apexes / spacing   blue: eyes + head width on eye row   green: near-ear pinna apex + base   '
                            'dashed blue: sit ear-tip height (seated frames)', fill=(60, 60, 60, 255), font=small_font)
    y = 80
    for (name, width, old, per_frame, passed, worst, tiles, above, below), height in zip(prepared, heights):
        floor = round(y + 20 + above)
        draw.text((margin, floor - 60), name, fill=(20, 20, 20, 255), font=title_font)
        draw.text((margin, floor - 30), f'{old:.1f} -> {width:.1f}', fill=(20, 20, 20, 255), font=label_font)
        draw.text((margin, floor - 8), 'PASS' if passed else 'FAIL >5%: median used', font=label_font,
                  fill=(20, 140, 50, 255) if passed else (200, 30, 30, 255))
        sit_tip = floor - (tiles[0][1] - tiles[0][2])
        x = margin + 150
        captions = ['entry: sit/000 @480', f"first: {per_frame['first']['frame']} @{width:.1f}",
                    f"last: {per_frame['last']['frame']} @{width:.1f}", 'exit: sit/000 @480']
        for i, ((tile, support, tip), caption) in enumerate(zip(tiles, captions)):
            sheet.alpha_composite(tile, (x, round(floor - support)))
            draw.text((x, floor + 8), caption, fill=(20, 20, 20, 255), font=label_font)
            role = {1: 'first', 2: 'last'}.get(i)
            if role:
                for row, (metric, data) in enumerate(per_frame[role]['metrics'].items()):
                    draw.text((x, floor + 30 + row * 18), f"{metric} {data['delta'] * 100:+.1f}% vs {data['reference'].split(' (')[0]}",
                              fill=(200, 30, 30, 255) if abs(data['delta']) > TOLERANCE else (40, 40, 40, 255), font=small_font)
            x += tile.width + gap
            if i == 1:
                draw.text((x - gap + 6, floor - 40), '...', fill=(20, 20, 20, 255), font=title_font)
        for gx in range(margin + 150, x, 14):
            draw.line((gx, round(sit_tip), gx + 7, round(sit_tip)), fill=(40, 110, 200, 255), width=1)
        draw.line((margin + 140, floor, x, floor), fill=(200, 30, 30, 255), width=2)
        y += height
    OUT_SHEET.parent.mkdir(parents=True, exist_ok=True)
    sheet.convert('RGB').save(OUT_SHEET)
    print(OUT_SHEET, sheet.size)


if __name__ == '__main__':
    main()
