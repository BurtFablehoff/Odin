# Job: head-turns-lookback — five whole-body head-turn frames for Odin, pose #7 sitting, looking back over the shoulder

Attached: the BASE image for this pose (`draft-assets/approved-sitting-v1/lookback.png`), the three ORIGINAL frames that define Odin's look (`sit/000`, `lay/000`,
`loaf/000`), and reference photos of the real Odin.

## What to make
Five PNGs of the SAME cat in the SAME body pose as the base image, differing only in where the head points. Body, legs, paws, tail,
ruff and their outline must stay where they are in the base image: same silhouette, same position on the canvas, same size. Redraw
each frame WHOLE (not a head pasted on a body): the head turns on a visible neck, the ruff stays anchored to the neck and shoulders
(it does not swing with the head), and the fur direction stays consistent.

| File name | Head |
| `lookback-far-left.png` | head turned far to the viewer's left (about 60°), eye line toward the far left |
| `lookback-left.png` | head turned a little to the viewer's left (about 30°) |
| `lookback-centre.png` | head exactly as in the attached base image (this is the reference frame; redraw it whole in the new set's style so the five match) |
| `lookback-right.png` | head turned a little to the viewer's right (about 30°) |
| `lookback-far-right.png` | head turned far to the viewer's right (about 60°), eye line toward the far right |

The body keeps facing away with the head turned back over the shoulder toward the viewer. "Far left" to "far right" sweeps the head from looking back-left to looking back-right across the viewer; the centre frame is the base pose. Keep the working eye on the viewer's left and the dark socket on the viewer's right whenever both sides of the face show.

## Scale and placement
Match the base image's scale and position exactly: if the five PNGs and the base were stacked, the body below the shoulders would
coincide. The canvas is 1024 × 1024; the base image is 1254 × 1254, so first fit the base's cat into 1024 × 1024 with a uniform scale
and at least 24 px margin, and use that placement for all five frames.

## Acceptance (checked by script on return)
- Exactly these five file names, PNG, 1024 × 1024, RGBA, alpha 0 in all four corners, some alpha-255 pixels.
- The lower-body silhouette (below the shoulders) of each frame overlaps the centre frame's by at least 97 %.
- Each frame shows the identity marks in the common block below. Turned faces show distinct whiskers.
- No floor, shadow, text, border or watermark.


---


## Who Odin is
Odin is a real cat: a large, long-haired silver tabby with a Persian-type face. Every image must be the same individual as the
attached originals (`sit/000`, `lay/000`, `loaf/000`). Identity marks that must all be present and correct:
- A dark, closed, missing-eye socket on the VIEWER'S RIGHT (his left eye is gone). One working yellow-green eye on the viewer's left.
- An upward snaggle tooth: a small lower canine showing above the lip on the viewer's left of the mouth.
- Wispy ear tufts: compact, rounded pinnae with only a few sparse, separated long hairs at the tips. Not solid pointed triangles,
  not lynx tufts.
- A flat, short Persian face with full cheeks and a short, low nose. Not a pointed generic-cat muzzle.
- An unstriped silver-grey torso and ruff, with clear dark tabby stripes only on the legs and faint ones on the tail.
- Distinct whiskers on every profile or turned face: long, thin, mostly straight WHITE whiskers coming from the whisker pad,
  clearly separate from the cheek fur. They must read as whiskers at desktop size, not as fur wisps.
- A REAL NECK. The head sits on a visible neck in front of the chest; it must not merge straight into the ruff. His mane (the big
  ruff) grows from the NECK and shoulders, not from the cheeks or the back of the head: the cheek fur is full but short, then the
  neck, then the long ruff fans out below it. On turned and profile views the line of the neck shows between the jaw and the ruff.
  (Owner, 2026-11-08, on the pilot: "give him a neck... his mane comes from his neck, not his head".)

## Fur
Natural, irregular, mostly straight fine hair with tapered tips, like the attached photos. Dense pale undercoat with a silver-grey
guard-hair sheen. No rounded locks, no plush airbrushed smoothness, no etched or crosshatched texture, no painterly strokes.

## Canvas and file
- Output: PNG, exactly 1024 × 1024 pixels, RGBA, fully transparent background (alpha 0). No shadow, no floor, no backdrop.
- The whole cat inside the canvas with a margin of at least 24 px: ears, whiskers, paws and tail all complete.
- Save every final PNG in the current working directory with the exact file names this brief gives. Do not write anywhere else.
- Do not run git, do not build anything, do not edit any file outside the current working directory.

## Floor rule
Body, belly, ruff and tail rest on ONE support plane. Only the paws may hang below it. (Exception for the sitting-away pose: the
tail may overhang the support line.)

## Same cat, same lighting
Soft neutral light from the upper left, the same as the attached originals. No rim light, no colour cast.
