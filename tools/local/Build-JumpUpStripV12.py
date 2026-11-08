"""V12 2c strip: both jump directions' real CatMind flights (floor up to a ledge, ledge down to the floor),
as a sequence of the canvases the app itself drew (OdinWindow.DrawScene, saved by
`Odin-Dev-Local.exe --test-jump-tilt <report.json> <frames-dir>`), pasted at their placed screen positions
with the floor and ledge lines and each frame's lean (positive = nose up). No rotation is done here.

Usage: Build-JumpUpStripV12.py [frames-dir] [out.png]
Defaults: build/jump-tilt-frames -> local-data/reference-review/v12/jump-up-strip.png"""
import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
FRAMES = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / 'build/jump-tilt-frames'
OUT = Path(sys.argv[2]) if len(sys.argv) > 2 else ROOT / 'local-data/reference-review/v12/jump-up-strip.png'
SCALE = 0.3
PAD = 30
BG, WINDOW, FLOOR, LINE, INK = (238, 241, 246, 255), (92, 104, 122, 255), (214, 220, 230, 255), (0, 150, 140, 255), (20, 24, 32, 255)


def font(size):
    for name in ('segoeui.ttf', 'arial.ttf'):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            pass
    return ImageFont.load_default()


def region(flight):
    left = min(f['scene'][0] for f in flight['frames'])
    top = min(f['scene'][1] for f in flight['frames'])
    right = max(f['scene'][0] + f['scene'][2] for f in flight['frames'])
    bottom = max(f['scene'][1] + f['scene'][3] for f in flight['frames'])
    ledge = flight['ledge']
    return left - PAD, min(top, ledge[1]) - PAD, right + PAD, max(bottom, flight['floorY']) + PAD + 40


def backdrop(flight, box):
    left, top, right, bottom = box
    w, h = round((right - left) * SCALE), round((bottom - top) * SCALE)
    cell = Image.new('RGBA', (w, h), BG)
    d = ImageDraw.Draw(cell)
    lx, ly, lw, lh = flight['ledge']
    to = lambda x, y: ((x - left) * SCALE, (y - top) * SCALE)
    d.rectangle([to(lx, ly), to(lx + lw, ly + lh)], fill=WINDOW)
    d.rectangle([to(left, flight['floorY']), to(right, bottom)], fill=FLOOR)
    d.line([to(left, flight['floorY']), to(right, flight['floorY'])], fill=LINE, width=2)
    d.line([to(lx, ly), to(lx + lw, ly)], fill=LINE, width=3)
    return cell


def paste(cell, box, frame, alpha=1.0):
    image = Image.open(FRAMES / frame['file']).convert('RGBA')
    image = image.resize((max(1, round(image.width * SCALE)), max(1, round(image.height * SCALE))), Image.LANCZOS)
    if alpha < 1:
        image.putalpha(image.getchannel('A').point(lambda a: round(a * alpha)))
    layer = Image.new('RGBA', cell.size, (0, 0, 0, 0))
    layer.alpha_composite(image, (round((frame['scene'][0] - box[0]) * SCALE), round((frame['scene'][1] - box[1]) * SCALE)))
    cell.alpha_composite(layer)


def label(frame):
    if frame['kind'] != 'flight':
        name = 'takeoff crouch' if frame['kind'] == 'takeoffCrouch' else 'landing crouch'
        return [name, '0.0° upright', frame['source'].split('/')[-2] + '/' + frame['source'].split('/')[-1][:3]]
    sign = '+' if frame['tiltDeg'] > 0 else ''
    return ['t=%.2f %s' % (frame['t'], frame['motion']), '%s%.1f°' % (sign, frame['tiltDeg']),
            'slot %d: %s' % (frame['slot'], frame['source'].split('/')[-2] + '/' + frame['source'].split('/')[-1][:3])]


def main():
    data = json.loads((FRAMES / 'jump-tilt-frames.json').read_text())
    order = sorted(data['flights'], key=lambda f: (f['clip'] != 'jump_right', f['direction'] != 'up'))
    small, big = font(13), font(20)
    rows = []
    for flight in order:
        box = region(flight)
        cells = []
        overview = backdrop(flight, box)
        frames = flight['frames']
        for i, frame in enumerate(frames):
            paste(overview, box, frame, 0.25 + 0.75 * i / (len(frames) - 1))
        cells.append((overview, ['all 12 frames', 'later = darker', '']))
        for frame in frames:
            cell = backdrop(flight, box)
            paste(cell, box, frame)
            cells.append((cell, label(frame)))
        rows.append((flight, cells))
    text_h, gap, title_h = 52, 8, 34
    width = max(sum(c.width + gap for c, _ in cells) for _, cells in rows) + gap
    height = 70 + sum(title_h + max(c.height for c, _ in cells) + text_h + gap for _, cells in rows)
    sheet = Image.new('RGBA', (width, height), (255, 255, 255, 255))
    d = ImageDraw.Draw(sheet)
    d.text((gap, 8), "V12 2c DEV: jump leans along CatMind's flight arc, + = nose up, clamped to ±45°, upright crouches. "
           "Each cell is the canvas the app drew (DrawScene) at a real flight sample, size Small, at its screen position.", fill=INK, font=big)
    d.text((gap, 36), 'Teal lines: floor and ledge (window top). Not owner-approved.', fill=INK, font=big)
    y = 70
    for flight, cells in rows:
        what = 'up onto the ledge' if flight['direction'] == 'up' else 'down to the floor'
        d.text((gap, y + 6), '%s %s (CatMind %s, x %.0f -> %.0f)' % (flight['clip'], what, 'TryPerch' if flight['direction'] == 'up' else 'LeavePerch',
                                                                  flight['fromX'], flight['toX']), fill=INK, font=big)
        y += title_h
        x = gap
        for cell, lines in cells:
            sheet.alpha_composite(cell, (x, y))
            for n, line in enumerate(lines):
                d.text((x + 2, y + cell.height + 2 + n * 16), line, fill=INK, font=small)
            x += cell.width + gap
        y += max(c.height for c, _ in cells) + text_h + gap
    OUT.parent.mkdir(parents=True, exist_ok=True)
    sheet.convert('RGB').save(OUT)
    print(OUT, sheet.size)


if __name__ == '__main__':
    main()
