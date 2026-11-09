# Job: clock-rear-b — four clock-hour head frames for Odin, pose #6 sitting away (rear view) (hours 4, 5, 6, 7)

Attached, in this order: the EDIT BASE (`draft-assets/codex-v12/head-turns-rear/rear-centre.png`: this pose's centre frame, 1254 × 1254, transparent background), the ORIGINAL
`sit/000` frame that defines Odin's look, and three reference photos of the real Odin.

## What to make
Four PNGs. Each is the EDIT BASE with ONLY the head, the neck and the top edge of the ruff redrawn so that the head points at a clock
hour. Everything from the shoulders down (chest ruff below the neck, body, legs, paws, tail, outline) is IDENTICAL to the base: the same
pixels in the same place at the same size. Do not redraw, restyle, move or resize the body. The head keeps the base's size (same
ear-to-ear distance, same skull size); it turns and tilts on a visible neck; it never grows, shrinks or slides on the body.

The clock is the VIEWER'S screen: 12 o'clock = he looks straight up, 3 = to the viewer's right, 6 = straight down, 9 = to the viewer's
left; the hours between are between.

| File name | Hour | Head |
| `rear-h04.png` | 4 o'clock | to the viewer's right and a little down |
| `rear-h05.png` | 5 o'clock | down and a little to the viewer's right |
| `rear-h06.png` | 6 o'clock | looks straight DOWN at the floor just in front of him: we see the top of the skull between the ears |
| `rear-h07.png` | 7 o'clock | down and a little to the viewer's left |

Seen from BEHIND; the base shows the back of his head and his ears. Hours 2-4 turn the head so the viewer sees his profile on the RIGHT side of the head (nose pointing right: one ear forward, the cheek, the whisker pad, long white whiskers, and the eye to the viewer's left of the nose, which is the WORKING eye). Hours 8-10 show the profile on the LEFT side (nose pointing left: the eye lies to the viewer's right of the nose, so it is the dark closed SOCKET). 12 tips the head back so the ears and crown rise and we see the underside of the ears; 6 drops the head so we see the top of the skull and the ears from above, with the neck showing between the head and the shoulders. 1, 5, 7 and 11 are between their neighbours. The body never turns: only the head and the neck move.

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
- Your image tool saves each result in its own output folder (`generated_images/<thread>/<call>.png`). That is where they belong:
  do NOT try to copy, rename, resize or check files, and do not run shell commands; the file system is read-only for you and
  that is expected, not a problem. Koa collects the files afterwards.
- Your final message must list every image you made as a Markdown link, one per line, in this exact form so the files can be
  named: `- [<file name from this brief>.png](<full path the tool reported>)`. If a generation failed, say which name failed.
- Do not run git, do not build anything, do not read or edit any file.

## Floor rule
Body, belly, ruff and tail rest on ONE support plane. Only the paws may hang below it. (Exception for the sitting-away pose: the
tail may overhang the support line.)

## Same cat, same lighting
Soft neutral light from the upper left, the same as the attached originals. No rim light, no colour cast.
