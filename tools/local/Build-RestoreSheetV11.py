"""V11 review sheets (git-ignored output under local-data/reference-review/restore-v11/).

sheet.png            sit reference | v11 rear/side candidates | v10 rear/side, one floor line,
                     equal physical scale (each source's runtime scaleAtSmall).
look-stability.png   per family: base vs max-drift frame placed with production anchors and with
                     V11 anchors, onion-skinned (base red, frame cyan), with drift numbers.
"""
from pathlib import Path
import json

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'local-data/reference-review/restore-v11'
SIT_SCALE = 0.46875
ZOOM = 0.62
BG = (236, 233, 226, 255)


def font(size):
    for name in ('segoeui.ttf', 'arial.ttf'):
        try:
            return ImageFont.truetype(f'C:/Windows/Fonts/{name}', size)
        except OSError:
            continue
    return ImageFont.load_default()


def physical(path, scale_at_small, support_y, anchor_x):
    """Return the sprite resized to sit-native physical units * ZOOM, plus its anchor/support in that space."""
    image = Image.open(ROOT / path).convert('RGBA')
    k = scale_at_small / SIT_SCALE * ZOOM
    size = (round(image.width * k), round(image.height * k))
    return image.convert('RGBa').resize(size, Image.LANCZOS).convert('RGBA'), anchor_x * k, support_y * k


def sheet1():
    reg = json.loads((ROOT / 'draft-assets/restore-v11/registration.json').read_text())
    v10 = {p['id']: p for p in json.loads((ROOT / 'planning/owner-corrections-v10.json').read_text())['poseOverrides']}
    panels = [('Sit reference\nart/frames/sit/000.png (original)', 'art/frames/sit/000.png', SIT_SCALE, 944, 512)]
    for name in ('rear', 'side'):
        pose = reg['poses'][name]
        for cand in ('head', 'owner'):
            c = pose['candidates'][cand]
            tag = 'IN v11 EXE' if pose['selected'] == cand else 'alternative'
            what = 'head-matched (ear spacing/skull)' if cand == 'head' else 'your last +20% sizing'
            panels.append((f"v11 {name} #{pose['id']} - {tag}\n{what}, scale {c['scale']:.3f}",
                           c['file'], SIT_SCALE, 944, c['anchorX']))
    for pid, name in ((6, 'rear'), (8, 'side')):
        o = v10[pid]
        panels.append((f'v10 {name} #{pid} (rejected)\n{o["source"].split("/")[-1]}', o['source'], o['scaleAtSmall'], o['supportY'], o['anchorX']))
    sprites = [physical(p, s, sy, ax) + (label,) for label, p, s, sy, ax in panels]
    col = 500
    heights_above = max(sy for im, ax, sy, label in sprites)
    floor = int(heights_above) + 90
    W, H = col * len(sprites) + 40, floor + 230
    sheet = Image.new('RGBA', (W, H), BG)
    d = ImageDraw.Draw(sheet)
    title, small = font(26), font(17)
    d.text((20, 14), 'Odin v11 - sitting stills at equal physical scale (runtime scaleAtSmall), shared floor line', fill=(20, 20, 20), font=title)
    for i, (im, ax, sy, label) in enumerate(sprites):
        cx = 20 + col * i + col // 2
        sheet.alpha_composite(im, (round(cx - ax), round(floor - sy)))
        d.text((20 + col * i + 8, floor + 18), label, fill=(20, 20, 20), font=small)
    d.line([(10, floor), (W - 10, floor)], fill=(60, 60, 60), width=2)
    caveat = ('Caveat: approved-v1 rear/side fur is longer/wavier than sit/000. You approved them as-is ("approved. keep going"); '
              'no retexture was attempted. Only uniform resampling and placement were applied.')
    amb = ('Landmarks disagree beyond 3 px: "head-matched" equalises ear spacing/skull width with the sit reference but makes the body '
           'much taller; "your last +20% sizing" keeps the rear/side-to-front ratio from numbered review v2. Pick one per pose.')
    d.text((20, floor + 120), caveat, fill=(120, 30, 30), font=small)
    d.text((20, floor + 150), amb, fill=(30, 30, 120), font=small)
    OUT.mkdir(parents=True, exist_ok=True)
    sheet.convert('RGB').save(OUT / 'sheet.png', optimize=True)


def tint(image, rgb):
    a = np.asarray(image.convert('RGBA')).astype(np.float32)
    out = np.zeros_like(a)
    out[:, :, :3] = rgb
    out[:, :, 3] = a[:, :, 3] * 0.55
    return Image.fromarray(out.astype(np.uint8), 'RGBA')


def sheet2():
    policy = json.loads((ROOT / 'planning/owner-corrections-v11.json').read_text())
    manifest = json.loads((ROOT / 'art/manifest.json').read_text())
    rows = []
    for fam in ('sit', 'loaf', 'lay', 'sleep'):
        own = [f for f in policy['frames'] if f['family'] == fam]
        worst_prod = max(own, key=lambda f: f['productionPlacedDriftPx'])
        worst_v11 = max(own, key=lambda f: f['placedDriftPx'])
        rows.append((fam, worst_prod, worst_v11))
    crop_h, z = 260, 2
    panel_w, panel_h = 560 * z // 2 + 40, crop_h * z // 2 + 70
    W = 40 + panel_w * 3
    H = 70 + len(rows) * (panel_h + 20)
    sheet = Image.new('RGBA', (W, H), BG)
    d = ImageDraw.Draw(sheet)
    title, small = font(24), font(16)
    d.text((20, 14), 'Odin v11 - lower-body stability while looking (base red, frame cyan; native px drift of lower-body centroid)', fill=(20, 20, 20), font=title)
    for r, (fam, wp, wv) in enumerate(rows):
        base = Image.open(ROOT / f'art/frames/{fam}/000.png').convert('RGBA')
        base_anchor = manifest['clips'][fam]['images'][0].get('anchor_y', manifest['clips'][fam]['anchor_y'])
        y0 = 70 + r * (panel_h + 20)
        cases = [
            (f'{fam}: production 3.1.2 placement  {wp["clip"]}/{wp["frame"]:03d}\nmanifest anchor {wp["manifestAnchorY"]} vs base {base_anchor}: drift {wp["productionPlacedDriftPx"]:.2f} px', wp, 0, -(wp['manifestAnchorY'] - base_anchor)),
            (f'{fam}: v11 placement  {wp["clip"]}/{wp["frame"]:03d}\nD2 shift ({wp["dx"]},{wp["dy"]}): drift {wp["placedDriftPx"]:.2f} px', wp, -wp['dx'], -wp['dy']),
            (f'{fam}: v11 worst frame  {wv["clip"]}/{wv["frame"]:03d}\nD2 shift ({wv["dx"]},{wv["dy"]}): drift {wv["placedDriftPx"]:.2f} px (limit 3)', wv, -wv['dx'], -wv['dy']),
        ]
        for c, (label, row, sx, sy) in enumerate(cases):
            frame = Image.open(ROOT / row['source']).convert('RGBA')
            layer = Image.new('RGBA', base.size, (0, 0, 0, 0))
            layer.alpha_composite(tint(base, (220, 40, 40)))
            moved = Image.new('RGBA', base.size, (0, 0, 0, 0))
            moved.paste(tint(frame, (20, 170, 200)), (sx, sy))
            layer.alpha_composite(moved)
            top = 960 - crop_h
            crop = layer.crop((200, top, 840, 960)).resize((640 * z // 2, crop_h * z // 2), Image.NEAREST)
            x0 = 20 + c * panel_w
            sheet.alpha_composite(crop, (x0, y0 + 50))
            fy = y0 + 50 + (base_anchor - top) * z // 2
            d.line([(x0, fy), (x0 + crop.width, fy)], fill=(40, 40, 40), width=1)
            d.text((x0, y0), label, fill=(20, 20, 20), font=small)
    sheet.convert('RGB').save(OUT / 'look-stability.png', optimize=True)


if __name__ == '__main__':
    sheet1()
    sheet2()
    print('Sheets:', OUT / 'sheet.png', OUT / 'look-stability.png')
