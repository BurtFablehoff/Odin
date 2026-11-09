"""Assemble the v12 Codex art briefs: planning/art-briefs-v12/<job>.md = job text + COMMON.md.
Also writes the attachment list per job (planning/art-briefs-v12/<job>.refs.txt) so the launcher and the record agree.
Run: local-data/toolchain/python/python.exe tools/local/Build-ArtBriefsV12.py
"""
import json
import os

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
BRIEFS = os.path.join(ROOT, 'planning', 'art-briefs-v12')
INDEX = os.path.join(ROOT, 'local-data/reference-review/fur-source-selection-v1/reference-index.json')

ORIGINALS = ['art/frames/sit/000.png', 'art/frames/lay/000.png', 'art/frames/loaf/000.png']
photos = {p['number']: p['source'] for p in json.load(open(INDEX))['photos']}

DIRECTIONS = [
    ('far-left', 'head turned far to the viewer\'s left (about 60°), eye line toward the far left'),
    ('left', 'head turned a little to the viewer\'s left (about 30°)'),
    ('centre', 'head exactly as in the attached base image (this is the reference frame; redraw it whole in the new set\'s style so the five match)'),
    ('right', 'head turned a little to the viewer\'s right (about 30°)'),
    ('far-right', 'head turned far to the viewer\'s right (about 60°), eye line toward the far right'),
]

FAMILIES = {
    'head-turns-rear': dict(
        id=6, name='sitting away (rear view)', base='draft-assets/approved-sitting-v1/rear.png',
        note='Seen from behind. "Turned left/right" means the head rotates so the viewer sees more of that side of his face: at far left '
             'and far right about a third of the face is visible in profile (one ear forward, the cheek, the whisker pad and the whiskers). '
             'The missing eye is on the viewer\'s right when he faces us, so when he turns to show his RIGHT profile from behind the dark socket '
             'is what the viewer sees; turning to show his LEFT profile shows the working eye. The tail lies on the floor and may cross the support line.',
        refs=[3, 4, 2, 8, 10]),
    'head-turns-lookback': dict(
        id=7, name='sitting, looking back over the shoulder', base='draft-assets/approved-sitting-v1/lookback.png',
        note='The body keeps facing away with the head turned back over the shoulder toward the viewer. "Far left" to "far right" sweeps the '
             'head from looking back-left to looking back-right across the viewer; the centre frame is the base pose. Keep the working eye on the '
             'viewer\'s left and the dark socket on the viewer\'s right whenever both sides of the face show.',
        refs=[3, 5, 8, 10, 1]),
    'head-turns-side': dict(
        id=8, name='sitting side-on, body facing the viewer\'s right', base='draft-assets/approved-sitting-v1/side.png',
        note='The body stays in profile facing the viewer\'s right. "Far left" means the head turns toward the viewer (we see most of the face, '
             'both eye positions, the working eye on the viewer\'s left and the socket on the viewer\'s right); "left" is a three-quarter turn toward '
             'us; "centre" is the base profile; "right" and "far right" turn the head further away toward where the body faces, showing the back of '
             'the head and the far cheek. Whiskers must be long, thin, white and separate from the cheek fur in every frame.',
        refs=[1, 3, 5, 8, 10]),
}

HEAD_TURN_TEXT = """# Job: {job} — five whole-body head-turn frames for Odin, pose #{id} {name}

Attached: the BASE image for this pose (`{base}`), the three ORIGINAL frames that define Odin's look (`sit/000`, `lay/000`,
`loaf/000`), and reference photos of the real Odin.

## What to make
Five PNGs of the SAME cat in the SAME body pose as the base image, differing only in where the head points. Body, legs, paws, tail,
ruff and their outline must stay where they are in the base image: same silhouette, same position on the canvas, same size. Redraw
each frame WHOLE (not a head pasted on a body): the head turns on a visible neck, the ruff stays anchored to the neck and shoulders
(it does not swing with the head), and the fur direction stays consistent.

| File name | Head |
{rows}

{note}

## Scale and placement
Match the base image's scale and position exactly: if the five PNGs and the base were stacked, the body below the shoulders would
coincide. The canvas is 1024 × 1024; the base image is 1254 × 1254, so first fit the base's cat into 1024 × 1024 with a uniform scale
and at least 24 px margin, and use that placement for all five frames.

## Acceptance (checked by Koa's script after you finish; you do not run it)
- Five images, one per name above, PNG, square, RGBA, alpha 0 in all four corners, solid body pixels, at least 24 px margin.
- The lower-body silhouette (below the shoulders) of each frame overlaps the centre frame's by at least 97 %.
- Each frame shows the identity marks in the common block below. Turned faces show distinct whiskers.
- No floor, shadow, text, border or watermark.

"""

TUCKED_TEXT = """# Job: tucked — Odin asleep tucked up, redrawn in the original fur style

Attached: the current tucked image (`draft-assets/approved-tucked-sleep-v1/tucked.png`, owner-approved for pose and contact, but
its fur reads as too flowy and "pretty"), the ORIGINAL frames `lay/000` and `loaf/000` whose fur is the target style, `sit/000`, and
reference photos of the real Odin sleeping and lying.

## What to make
One PNG, `tucked.png`: the SAME pose as the attached tucked image (lying on the chest, legs tucked under, head down with the face
pushed into the forepaws, eyes closed, tail wrapped along the body) with the fur redrawn to match `lay/000` and `loaf/000`:
shorter-looking, denser, more irregular hair with tapered tips, and the same silver-grey tabby markings as those originals. Keep the
owner-approved contact: the body, belly and tail sit flat on one support line; only the paws may hang below it. The face stays
"smooshed" into the paws (owner correction). The ear tufts are a few sparse hairs beyond a compact pinna, not solid triangles.

## Scale and placement
Fit the cat into 1024 × 1024 with at least 24 px margin, lying along the width, support line near y = 900, body centred on x = 512.

## Acceptance (checked by script on return)
- `tucked.png`, 1024 × 1024, RGBA, transparent corners, some alpha-255 pixels.
- The bottom edge of body, belly and tail lies within 6 px of one horizontal line; only the paws may go below it.
- Fur texture statistics (edge density, highlight ratio) within the band measured on `lay/000` and `loaf/000`.
- Identity marks from the common block (the eye socket is closed in sleep, so the mark is the asymmetric socket shape; the snaggle
  tooth may be hidden by the paws).

"""

PILOT_TEXT = """# Job: pilot — one test image, Odin sitting side-on with the head turned slightly toward the viewer

Attached: the BASE image (`draft-assets/approved-sitting-v1/side.png`, body in profile facing the viewer's right), the ORIGINAL
`sit/000`, and three reference photos of the real Odin's face and ears.

## What to make
One PNG, `pilot-side-left.png`: the same cat, same body pose, same scale and placement as the base image fitted into a 1024 × 1024
canvas with at least 24 px margin, with the head turned about 30° toward the viewer so a three-quarter face shows. Redraw the whole
frame; body, legs, paws and tail stay where the base has them. Whiskers: long, thin, white, from the whisker pad, separate from the
cheek fur.

This is a tool test. The image also tells us the time one image takes.

## Acceptance (checked by script on return)
- `pilot-side-left.png`, 1024 × 1024, RGBA, alpha 0 at all four corners, some alpha-255 pixels.

"""

# V12 3b (G3 fixes): 12-hour clock of head directions for 6/7/8, option A (fixed centre body, Codex draws only head/neck/ruff top).
# Hatt 2026-11-08: "it should have the look like a clock. 1 oclock to 12 oclock" and A "only for these 3 new ones".
# Screen clock: 12 = up, 3 = viewer's right, 6 = down, 9 = viewer's left. Frame index in the pack = ClockAngles index (12,1,...,11).
HOURS = {
    12: "looks straight UP: the chin lifts, we see under the jaw, the ears tip back",
    1: "up and a little to the viewer's right",
    2: "to the viewer's right and a little up",
    3: "straight to the viewer's RIGHT",
    4: "to the viewer's right and a little down",
    5: "down and a little to the viewer's right",
    6: "looks straight DOWN at the floor just in front of him: we see the top of the skull between the ears",
    7: "down and a little to the viewer's left",
    8: "to the viewer's left and a little down",
    9: "straight to the viewer's LEFT",
    10: "to the viewer's left and a little up",
    11: "up and a little to the viewer's left",
}
CLOCK_JOBS = {'a': [12, 1, 2, 3], 'b': [4, 5, 6, 7], 'c': [8, 9, 10, 11]}
CLOCK_FAMILIES = {
    'rear': dict(
        id=6, name='sitting away (rear view)', base='draft-assets/codex-v12/head-turns-rear/rear-centre.png', photos=[2, 4, 3],
        note="Seen from BEHIND; the base shows the back of his head and his ears. Hours 2-4 turn the head so the viewer sees his profile on the "
             "RIGHT side of the head (nose pointing right: one ear forward, the cheek, the whisker pad, long white whiskers, and the eye to the "
             "viewer's left of the nose, which is the WORKING eye). Hours 8-10 show the profile on the LEFT side (nose pointing left: the eye lies "
             "to the viewer's right of the nose, so it is the dark closed SOCKET). 12 tips the head back so the ears and crown rise and we see the "
             "underside of the ears; 6 drops the head so we see the top of the skull and the ears from above, with the neck showing between the "
             "head and the shoulders. 1, 5, 7 and 11 are between their neighbours. The body never turns: only the head and the neck move."),
    'lookback': dict(
        id=7, name='sitting, looking back over the shoulder', base='draft-assets/codex-v12/head-turns-lookback/lookback-centre.png', photos=[5, 8, 3],
        note="The body faces away and the head is turned back over the shoulder toward the viewer (the base). Every hour keeps the head turned "
             "back toward the viewer and moves the gaze and the tilt: 12 looks up above the viewer, 6 looks down at the floor between him and the "
             "viewer, 3 and 9 look to the sides as far as the neck allows without the body turning. Both eyes stay visible on most hours: the "
             "working eye on the viewer's left, the dark socket on the viewer's right, the snaggle tooth on the viewer's left of the mouth. Both "
             "eyes OPEN: no wink, no half-closed eye."),
    'side': dict(
        id=8, name="sitting side-on, body facing the viewer's right", base='draft-assets/codex-v12/head-turns-side/side-centre.png', photos=[5, 3, 8],
        note="The body is in profile facing the viewer's RIGHT; the base profile looks ahead-right (about 3 o'clock). Hours 1-5 keep the near-"
             "profile and pitch it toward the hour (12 and 1 up, 5 and 6 down); the visible profile eye lies to the viewer's left of the nose, "
             "so it is the WORKING eye, fully open. Hours 7-11 turn the head toward the viewer so both eyes show (working eye on the viewer's left, "
             "dark socket on the viewer's right, both fully open, no half-closed far eye), with 9 looking back over his shoulder to the viewer's "
             "left as far as a cat can. Whiskers long, thin, white and separate from the cheek fur in every frame."),
}

CLOCK_TEXT = """# Job: {job} — four clock-hour head frames for Odin, pose #{id} {name} (hours {hours})

Attached, in this order: the EDIT BASE (`{base}`: this pose's centre frame, 1254 × 1254, transparent background), the ORIGINAL
`sit/000` frame that defines Odin's look, and three reference photos of the real Odin.

## What to make
Four PNGs. Each is the EDIT BASE with ONLY the head, the neck and the top edge of the ruff redrawn so that the head points at a clock
hour. Everything from the shoulders down (chest ruff below the neck, body, legs, paws, tail, outline) is IDENTICAL to the base: the same
pixels in the same place at the same size. Do not redraw, restyle, move or resize the body. The head keeps the base's size (same
ear-to-ear distance, same skull size); it turns and tilts on a visible neck; it never grows, shrinks or slides on the body.

The clock is the VIEWER'S screen: 12 o'clock = he looks straight up, 3 = to the viewer's right, 6 = straight down, 9 = to the viewer's
left; the hours between are between.

| File name | Hour | Head |
{rows}

{note}

## Eyes (every hour)
Whenever both eyes show, the working yellow-green eye is on the viewer's LEFT and the dark closed socket on the viewer's RIGHT. Whenever
only one eye shows (a profile), it is the working eye when it lies to the viewer's LEFT of the nose and the dark socket when it lies to
the viewer's RIGHT of the nose. Eyes fully open; no wink, no half-closed eye.

## Size and file
Each output is the base's size, 1254 × 1254 (this overrides the 1024 × 1024 line in the common block below). Same transparent
background, same placement as the base, at least 24 px margin. Four separate images, one per hour; no sheet, no grid.

## Acceptance (checked by Koa's script after you finish; you do not run it)
- Four images, one per name above, PNG, 1254 × 1254, RGBA, alpha 0 in all four corners, at least 24 px margin.
- Below the shoulders every image equals the base. (Koa's script keeps the base's body in any case; changes there are wasted work.)
- Head size within 2 % of the base's (ear-apex spacing, skull height).
- Identity marks from the common block; a visible neck; turned and profile faces show distinct whiskers.
- No floor, shadow, text, border or watermark.

"""


def clock_jobs():
    for family, fam in CLOCK_FAMILIES.items():
        for letter, hours in CLOCK_JOBS.items():
            job = 'clock-%s-%s' % (family, letter)
            rows = '\n'.join("| `%s-h%02d.png` | %d o'clock | %s |" % (family, h, h, HOURS[h]) for h in hours)
            text = CLOCK_TEXT.format(job=job, id=fam['id'], name=fam['name'], base=fam['base'], rows=rows, note=fam['note'],
                                     hours=', '.join(str(h) for h in hours))
            write(job, text, [fam['base'], 'art/frames/sit/000.png'] + [photos[n] for n in fam['photos']])


def write(job, text, refs):
    common = open(os.path.join(BRIEFS, 'COMMON.md'), encoding='utf-8').read()
    body = text + '\n---\n\n' + common.split('\n', 4)[4]  # drop the COMMON title and its two meta lines
    with open(os.path.join(BRIEFS, job + '.md'), 'w', encoding='utf-8', newline='\n') as f:
        f.write(body)
    with open(os.path.join(BRIEFS, job + '.refs.txt'), 'w', encoding='utf-8', newline='\n') as f:
        f.write('\n'.join(refs) + '\n')
    print(job, len(body), 'chars,', len(refs), 'attachments')


def main():
    for job, fam in FAMILIES.items():
        rows = '\n'.join('| `%s-%s.png` | %s |' % (job.split('-')[-1], key, desc) for key, desc in DIRECTIONS)
        text = HEAD_TURN_TEXT.format(job=job, id=fam['id'], name=fam['name'], base=fam['base'], rows=rows, note=fam['note'])
        write(job, text, [fam['base']] + ORIGINALS + [photos[n] for n in fam['refs']])
    write('tucked', TUCKED_TEXT, ['draft-assets/approved-tucked-sleep-v1/tucked.png', 'art/frames/lay/000.png',
                                  'art/frames/loaf/000.png', 'art/frames/sit/000.png', photos[7], photos[6], photos[2], photos[8]])
    clock_jobs()
    write('pilot', PILOT_TEXT, ['draft-assets/approved-sitting-v1/side.png', 'art/frames/sit/000.png', photos[5], photos[8], photos[10]])


if __name__ == '__main__':
    main()
