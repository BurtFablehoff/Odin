"""Render unchanged source sprites over a floor/lip for scale and anchor review."""
from pathlib import Path
import argparse
import hashlib
import json
import math
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]


def area(points):
    return abs(sum(a[0] * b[1] - b[0] * a[1]
                   for a, b in zip(points, points[1:] + points[:1]))) / 2


def build(destination):
    config = json.loads((ROOT / 'planning/sleeping-contact-review.json').read_text())
    poses = config['poses']
    reference_area = area(poses[0]['headOutline'])
    loaded = []
    for pose in poses:
        source = ROOT / pose['source']
        image = Image.open(source).convert('RGBA')
        bounds = image.getchannel('A').point(lambda a: 255 if a >= 8 else 0).getbbox()
        head_area = area(pose['headOutline'])
        if not bounds or head_area <= 0 or not 0 < pose['supportY'] < image.height:
            raise ValueError('Invalid registration: ' + pose['key'])
        factor = math.sqrt(reference_area / head_area)
        loaded.append((pose, image, bounds, factor))

    tile_width, tile_height = 820, 430
    # One common multiplier preserves equal head areas across every panel.
    common_scale = .60
    for pose, image, bounds, factor in loaded:
        common_scale = min(common_scale, 740 / ((bounds[2] - bounds[0]) * factor),
                           260 / ((pose['supportY'] - bounds[1]) * factor))

    title_font = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 25)
    label_font = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 21)
    small_font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 17)
    destination.mkdir(parents=True, exist_ok=True)
    report = []
    for guides in (False, True):
        board = Image.new('RGBA', (tile_width * 2, tile_height * 2 + 110), '#11161d')
        draw = ImageDraw.Draw(board)
        draw.text((22, 14), config['title'], font=title_font, fill='white')
        draw.text((22, 52), 'One shared head-area scale. Outlines are provisional; confirm apparent size.',
                  font=small_font, fill='#bbc8d5')
        draw.text((22, 77), 'Cyan edge = flat floor. ONLY paws may hang below it.',
                  font=small_font, fill='#6ee5df')
        for index, (pose, image, bounds, factor) in enumerate(loaded):
            tile = Image.new('RGBA', (tile_width, tile_height), '#222a34')
            td = ImageDraw.Draw(tile)
            td.text((20, 12), pose['label'], font=label_font, fill='white')
            td.text((20, 43), pose['approval'], font=small_font, fill='#bac6d5')
            floor_y = 340
            td.rectangle((0, floor_y, tile_width, tile_height), fill='#3a4656')
            td.line((0, floor_y, tile_width, floor_y), fill='#60d4d0', width=2)
            scale = common_scale * factor
            x = round(tile_width / 2 - (bounds[0] + bounds[2]) / 2 * scale)
            y = round(floor_y - pose['supportY'] * scale)
            rendered = image.resize((round(image.width * scale), round(image.height * scale)),
                                    Image.Resampling.LANCZOS)
            tile.alpha_composite(rendered, (x, y))
            if guides:
                points = [(x + px * scale, y + py * scale) for px, py in pose['headOutline']]
                ImageDraw.Draw(tile).line(points + points[:1], fill='#f5c55a', width=2)
            td = ImageDraw.Draw(tile)
            td.text((20, tile_height - 32), 'Body / tail stay above edge; paws may overhang',
                    font=small_font, fill='#d0dae4')
            board.alpha_composite(tile, ((index % 2) * tile_width, 110 + (index // 2) * tile_height))
            if not guides:
                report.append({'pose': pose['key'], 'headArea': area(pose['headOutline']),
                               'scale': scale, 'supportY': pose['supportY'],
                               'sourceSHA256': hashlib.sha256((ROOT / pose['source']).read_bytes()).hexdigest()})
        name = 'sleeping-floor-review-guides.png' if guides else 'sleeping-floor-review.png'
        board.convert('RGB').save(destination / name)
    (destination / 'review-metrics.json').write_text(json.dumps({
        'artworkModified': False, 'scaleApprovalPending': True,
        'basis': config['scaleBasis'], 'poses': report}, indent=2) + '\n')
    print('Rendered floor/scale review: ' + str(destination))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--output', type=Path,
                        default=ROOT / 'local-data/reference-review/sleeping-floor-review-v1')
    build(parser.parse_args().output)
