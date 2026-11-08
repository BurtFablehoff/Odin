# Odin backlog

One row per numbered state or animation: what the DEV build draws now, its approval, and what is open.
Source and sha256 are what `test-exes/Odin-Dev-Restore-v11.exe` embeds (from
`local-data/reference-review/dev-latest-art/dev-artwork.zip`, hash-checked at load).
Scale is the runtime width at the small size in px (sit = 480); support/anchor are native px of the source canvas.
Approval values: **original** (3.x art, byte-exact), **owner-approved**, **dev-candidate**, **rejected**, **retired**.
Updated 2026-11-07 (v11.3). Scale column = v11 width; v11.1 widths (head-matched by eye + ear landmarks, `planning/size-normalization-v11-1.json`) are listed in the open issue. Keep this table current; the version history lives in `AGENTS.md` and `planning/*`.

**v12-1 (2026-11-07, `Odin-Dev-v12-1.exe`, DEV). Passed G1** (Hatt, 2026-11-08: "this looks really good"; groom and jump-right median widths "look good"). Still DEV, not a release. Widths at the small size now come from `planning/sizes-owner-v12.json`:
- States: 1 sit 480 · 2 loaf 372.43 · 3 lay 444.43 · 4 sleep 480 · 6 rear 423.76 · 7 look-back 241.06 · 8 side 410.87. Rear and side match sit's seated height (197.8 screen px; owner exception for seated views).
- Animations, sized by continuity with sit (`planning/animation-continuity-v12.json`): 12 groom 483.44 · 13 stretch 496.61 · 14 run L 627.35 · 15 run R 683.63 · 16 jump L 455.58 · 17 jump R 496.45. Groom and jump R fail the 5% check (art proportion) and use the median; Hatt 2026-11-08: "look good", so the median stays.
- Follow-mouse: loaf and lay failed when reached through the stills menu. Fixed; `--test-follow-mouse` is in Verify.
- Jumps lean along the flight arc (±45°, `--test-jump-tilt`).
- Tucked #11 is parked (13 active stills).

Terms (Hatt, 2026-11-07: "stretch isn't a state.. it's an animation… a state is like sitting, laying, sleeping… something that is
not really movement"): a **state** is a held pose with no travel; a **state detail** is small motion inside a state (look frames,
blinks, tail flicks, sleep tail); an **animation** is movement from a state and back, sized by continuity with that state.

v11.3 verdict (2026-11-07):
- Sizes: "sitting side and sitting away still too big. you can easily tell just on height compared to sitting forward."
- Follow-mouse: "follow mouse only works for 2 awake states."
- Jumps: "look just like running now. it needs to be like going upward angle. like he's jumping up on a shelf or ledge."
- Zoomies: "add more stages in between frames, ensure head movement isn't shifting all over the place… make the run faster too."
- Tucked: "still fake looking.. compare to laying awake and laying loaf"; then "park tucked sleep until we can get it aligned with the approved style".
- Whiskers (v12 review, 2026-11-07): "make sure those are whiskers on the profile shots of his face. The pictures look like fur instead of whiskers".
  Side #8 has a few real whiskers crossed by long white cheek-fur wisps; run 14/15 (also drawn mid-jump, frames 002–012) have almost none.
  Needs new pixels (Codex, Part 3): distinct long thin whiskers from the whisker pad, separate from cheek fur.

## States

| ID | State | Current source | sha256 | Approval | Scale · support · anchor | Open issue | Owner quote · date |
|---|---|---|---|---|---|---|---|
| 1 | Sitting | `art/frames/sit/000.png` | `ab5c4261984e4f252172962198c2d532dffc8b8d269cfe23dd06d000808e7c43` | original | 480 · 944 · 512 | None for the still. Looks passed v11 ("all the looks are great"). Size reference for v11.1 | "no dude.. you had the original fix it" · V6, 2026-11-06/07 (exact date not recorded) |
| 2 | Lying loaf | `art/frames/loaf/000.png` | `b9ad734d97f2e8c31897c0e3dd3a129b821cf8723042cf527d4ea9afd6d817be` | original | 432 · 948 · 512 | Looks passed. v11.1 width 372.4 (head 1.16x sit). Floor contact still needs visual review (V7 note) | none recorded for this state |
| 3 | Lying awake | `art/frames/lay/000.png` | `bee0c93c1f17ccd33e359dea2de62bb8d7fcaa2cb323603a1f72a2093be3ab50` | original | 480 · 927 · 512 | Looks passed. v11.1 width 444.4 (head 1.08x). Max placed drift 2.44 px is fur-shape change, not translation | none recorded for this state |
| 4 | Sleeping | `art/frames/sleep/000.png` | `6b36174f9c2b6f359c3f04288109b4490765602c72a901b157ebfa557ce5e0f6` | original | 480 · 936 · 512 | Belly gap/contact still needs visual review. sleep_tail 1–3 are drawn 3/6/3 px low in production; v11 DEV corrects this, production does not | none recorded for this state |
| 5 | Belly-up | (none: removed from reviews and builds) | n/a | retired | n/a | Keep retired; do not renumber | "get rid of laying on back... i hate it" · V8, 2026-11-07 |
| 6 | Sitting away (rear) | `draft-assets/restore-v11/rear.png` (from `draft-assets/approved-sitting-v1/rear.png`) | `d1e026232a1cefc0aa196de1fe88485dae4a9e636dbb98ebbd5b719f46e22f04` | owner-approved source; v11 registration is a dev-candidate | 480 · 944 · 512 (source scale 0.457, "owner" candidate) | Size exception by owner (head is farther away): keeps v11 scale 0.457. v11.2 supportY 930 puts the butt on the line; tail overhangs (owner OK) | v1: "approved. keep going"; v10: "not proportionate" · 2026-11-07 |
| 7 | Sitting look-back | `draft-assets/numbered-sizing-v2/sit-lookback.png` | `8344b81f194dd314c0dfaf4dc288edb3ee1867facd14da7de0562ee9d4ce5e9b` | dev-candidate (derived from approved-v1 lookback) | 241.1 · 1091 · bounds center (1254 canvas) | v11.3 back to v11 width 241.06 (owner: "WAY bigger" at x1.27; small-head art). Fur consistency pending. Its scale is the pre-+20% value V8 restored, not the size Hatt last asked for | "bigger and flat bottom" · numbered review v2 (date not recorded) |
| 8 | Sitting side | `draft-assets/restore-v11/side.png` (from `draft-assets/approved-sitting-v1/side.png`) | `bcef1809cda45746173b29cad20f5517df921b098cf0595eae09e4302bc67461` | owner-approved source; v11 registration is a dev-candidate | 480 · 944 · 512 (source scale 0.438, "owner" candidate) | Owner scale kept in v11.1: ears and eye-nose both 0.96 of sit | v1: "approved. keep going"; v10 face wrong, must be the short-nosed Persian · 2026-11-07 |
| 9 | Curled sleep | (none: removed) | n/a | retired | n/a | Duplicate of tucked; keep retired | "rejected duplicate of tucked sleep" (paraphrase in `state-head-review.json`) |
| 10 | Side sleep | (none: removed) | n/a | retired | n/a | Keep retired | "looks like a totally different cat. take that shit out too" · V8, 2026-11-07 |
| 11 | Tucked sleep | `draft-assets/approved-tucked-sleep-v1/tucked.png` | `5ded8823177ae40f5f02f87f45e630c85e98a44518c5853be9d22222cae87696` | owner-approved; being parked in v12 (A5) | 406.8 · 742 · bounds center (1774 canvas) | Owner: "still looks a little big ... fur a little too flowy and pretty"; v11.3 "still fake looking.. compare to laying awake and laying loaf". v11.3 width 318.3. Parked: leaves the active states until a redraw matching original lay/loaf fur passes. Art stays on disk | "pass!" (`approval.json`, date not recorded); "park tucked sleep until we can get it aligned with the approved style" · 2026-11-07 |

## Animations

Each animation leaves from and returns to a state; its size is set by continuity with that state, not by its own factor.

| ID | Animation | Current source | sha256 | Approval | Scale · support · anchor | Open issue | Owner quote · date |
|---|---|---|---|---|---|---|---|
| 12 | Grooming | `draft-assets/style-consistency-v7/groom/frames/008.png` (16 frames) | `cdc8c63fffdee9ff7ee41e0595c29269022a467597534d8bff88645de924aeab` | dev-candidate (local duplicate-paw correction; original pixels outside the mask) | 480 · 947 · 512 | v11.1 width 448.6 (head 1.07x). Owner review of the paw correction | none recorded |
| 13 | Stretching | `art/frames/stretch/014.png` | `fb2cb29fe03a1809cb0fa7a5172ea7c4c32a8700b4b4634b344a184799cd31bd` | original | 480 · 950 · bounds center | v11.1 width 640 (head 0.75x: eyes and ears agree) | none recorded |
| 14 | Running left | `art/frames/run_left/009.png` | `076b5d6c8a7cc73bda4e0600c99a2224741866b9287446f0ad9ec6867179d85b` | original | 720 · 952 · bounds center | v11.1 width 571.5 (ears 1.3x). A third style; 18 frames vs 16 for run_right. Needs new art (Step 5, image tool) | none recorded |
| 15 | Running right | `art/frames/run_right/008.png` | `dee0ab18b1d0b8589cdd03e2e308fc114f717f1a2b6549af71267cc110f77e42` | original | 902.4 · 952 · bounds center | v11.1 width 688.9 (ears 1.4x). Flat, stripeless, small-headed. Needs new art (Step 5) | none recorded |
| 16 | Jumping left | `art/frames/jump_left/008.png` (16 frames) | `3fe7d725937c285bb4537f830ddd474c43fa3e2cbd4a02f970f778e631f5b3ef` | original (V7 restoration; the v5 anatomy overrides are not active) | 480 · 620 · 512 | Owner v11: "huge ball of fur under his chin and above his chest. like a bullfrog" (original frames; v4/v5 drafts too). "jumping is very small": v11.1 width 521.8. v11.2 slims the throat ruff on 000–003/012–015 by warping original pixels (`planning/jump-ruff-v11-2.json`), pending review; 004–011 not done. Mid-flight 004–008 tubular. Needs new art (Step 5) | v5 anatomy: "approve for now" (superseded by the V7 original restore) |
| 17 | Jumping right | `art/frames/jump_right/008.png` (16 frames) | `ab5c9893b269fe9b2d64d4f7fba1ce619576cb76451b4fd3f234f6074e62708b` | original (V7 restoration) | 480 · 620 · 512 | v11.1 width 571.4 (head 0.84x). Otherwise same as #16 | same as #16 |

Off and on hold (manifest `deferred`): walking (on hold since 2026-11-01; original 16-frame cycle in `local-data/vm-history/...`),
3D (on hold), breathing (removed), scratching (removed).

## Engineering debt

| # | Item | Where | Status |
|---|---|---|---|
| E1 | `Queue()` silently drops unsupported clips (walk/belly/pounce/crab/transition), so pose changes are hard cuts | `app/Behavior.cs:83` | open (Step 3-i) |
| E2 | `IdleLook` uses frame 16 as the "forward" sentinel, which collides with real `look_sit` frame 16 | `app/IdleLook.cs:7-8` | open (Step 3-i) |
| E3 | Support-placement math is copied 3× | `app/*.cs` (SharedSpriteSupport, DevArtwork, Desktop) | open (Step 3-ii) |
| E4 | Two `VisibleBounds` with different alpha thresholds | `app/PoseVariantRenderer.cs:150`, `app/RunningScale.cs:18` | open (Step 3-ii) |
| E5 | Verify builds and tests only the DEV exe, never production `build/Odin.exe` | `tools/local/Verify-OdinLocal.ps1` | open (Step 3-iii) |
| E6 | Production manifest gives `look_sit` 000–015 anchor_y 950 vs sit 944, so production look frames float ~6 native px | `art/manifest.json` | fixed in DEV v11 via D2 placement; production unchanged on purpose |
| E7 | V8–V10 head-layer renderer (head cut-out on a frozen body) | `app/OriginalRestRenderer.cs` | bypassed in v11 (`wholeFrameRestFamilies`); file kept |
| E8 | 38 versioned one-off scripts in `tools/local` (34 match V7–V10) | `tools/local/` | open (Step 2e) |
| E9 | Project memory is two append-only logs (`AGENTS.md`, `tools/local/RESUME.txt`) | repo root, `tools/local/` | open (Step 2c) |
| E10 | No measurable "looks like Odin" spec or consistency audit | `planning/`, `tools/local/` | open (Steps 2b, 2d) |
| E11 | `C:\_repo\pet\shared` has no commits; framework extraction is blocked on Hatt | `../shared` | open (Step 4 stop) |
| E12 | Repo `.ps1` scripts need `-ExecutionPolicy Bypass` on this machine (policy Undefined = Restricted) | machine | info: allowlisted in the Bypass form 2026-11-07 |
| E14 | Zoomies ran in place at screen edges (target picked with sitting margins, run clip clamps tighter) | `app/Behavior.cs` Step | fixed v11.3; regression in `--test-screen-edges` |
| E15 | Running looks ghosted/shaky: torso-registered frames leave 24 px head steps; 45 fps clip vs render timer | `app/RunningMotion.cs`, `art/frames/run_*` | open (replan) |
| E13 | `gh` CLI token expired (401) | GitHub | out of scope; Hatt's call |
