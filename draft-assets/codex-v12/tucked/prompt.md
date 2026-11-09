# Job: tucked — Odin asleep tucked up, redrawn in the original fur style

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
