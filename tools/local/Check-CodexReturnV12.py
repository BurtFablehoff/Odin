"""Acceptance check for a Codex image job: tools/local/Check-CodexReturnV12.py <job> <expected.png> [...]
Checks every expected PNG in draft-assets/codex-v12/<job>/ (1024x1024 RGBA, transparent corners, some opaque pixels),
scans codex-log.jsonl for computer-use events, and writes <job>/return-check.json. Exit 1 on any failure.
Copies from ~/.codex/generated_images are NOT made here; see Copy-CodexImagesV12.py.
"""
import json
import os
import sys

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def check_png(path):
    r = {'file': os.path.relpath(path, ROOT).replace('\\', '/'), 'exists': os.path.exists(path)}
    if not r['exists']:
        r['passed'] = False
        return r
    im = Image.open(path)
    r['size'] = list(im.size)
    r['mode'] = im.mode
    if im.mode != 'RGBA':
        r['passed'] = False
        return r
    a = im.getchannel('A')
    w, h = im.size
    corners = [a.getpixel((0, 0)), a.getpixel((w - 1, 0)), a.getpixel((0, h - 1)), a.getpixel((w - 1, h - 1))]
    hist = a.histogram()
    r['corners'] = corners
    # Odin's art (originals and Codex output alike) tops out around alpha 248-253, so "solid" is alpha >= 240.
    r['opaquePixels'] = hist[255]
    r['solidPixels'] = sum(hist[240:])
    r['maxAlpha'] = max(i for i, n in enumerate(hist) if n)
    r['transparentPixels'] = hist[0]
    bbox = a.point(lambda v: 255 if v > 8 else 0).getbbox()
    r['visibleBounds'] = list(bbox) if bbox else None
    margin = min(bbox[0], bbox[1], w - bbox[2], h - bbox[3]) if bbox else -1
    r['minMarginPx'] = margin
    # Size: 1024 is the runtime canvas, but the imagegen tool returns the base image's size (1254 for the approved-v1 bases);
    # registration (3R) resamples into 1024, so either is accepted and the size is recorded.
    r['sizeAccepted'] = im.size in ((1024, 1024), (1254, 1254))
    # A corner at alpha 1-8 is the generator's faint haze (also present around the original art), not a backdrop.
    r['passed'] = (r['sizeAccepted'] and all(c <= 8 for c in corners) and r['solidPixels'] > 1000 and margin >= 24)
    return r


def scan_log(path):
    r = {'file': os.path.relpath(path, ROOT).replace('\\', '/'), 'exists': os.path.exists(path), 'events': 0,
         'computerUseEvents': 0, 'imageToolEvents': 0, 'errors': []}
    if not r['exists']:
        return r
    with open(path, encoding='utf-8', errors='replace') as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            r['events'] += 1
            low = line.lower()
            if 'computer-use' in low or 'computer_use' in low or '"computer"' in low or 'codex-computer-use' in low:
                r['computerUseEvents'] += 1
            if 'image' in low and ('tool' in low or 'generat' in low):
                r['imageToolEvents'] += 1
            if '"error"' in low:
                r['errors'].append(line[:300])
    return r


def main():
    job, expected = sys.argv[1], sys.argv[2:]
    folder = os.path.join(ROOT, 'draft-assets', 'codex-v12', job)
    pngs = [check_png(os.path.join(folder, name)) for name in expected]
    log = scan_log(os.path.join(folder, 'codex-log.jsonl'))
    passed = all(p['passed'] for p in pngs) and log['exists'] and log['computerUseEvents'] == 0
    report = {'job': job, 'passed': passed, 'images': pngs, 'log': log}
    with open(os.path.join(folder, 'return-check.json'), 'w', encoding='utf-8') as f:
        json.dump(report, f, indent=1)
    print(json.dumps({'job': job, 'passed': passed, 'images': [(p['file'], p['passed']) for p in pngs],
                      'computerUseEvents': log['computerUseEvents'], 'events': log['events']}, indent=1))
    sys.exit(0 if passed else 1)


if __name__ == '__main__':
    main()
