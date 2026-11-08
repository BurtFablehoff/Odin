# Odin backlog

One row per numbered state/action: what the DEV build draws now, its approval, and what is open.
Source and sha256 are what `test-exes/Odin-Dev-Restore-v11.exe` embeds (from
`local-data/reference-review/dev-latest-art/dev-artwork.zip`, hash-checked at load).
Scale is the runtime width at the small size in px (sit = 480); support/anchor are native px of the source canvas.
Approval values: **original** (3.x art, byte-exact), **owner-approved**, **dev-candidate**, **rejected**, **retired**.
Updated 2026-11-07 (v11). Keep this table current; the version history lives in `AGENTS.md` and `planning/*`.

## Table 1: states and actions

| ID | State | Current source | sha256 | Approval | Scale · support · anchor | Open issue | Owner quote · date |
|---|---|---|---|---|---|---|---|
| 1 | Sitting | `art/frames/sit/000.png` | `ab5c4261984e4f252172962198c2d532dffc8b8d269cfe23dd06d000808e7c43` | original | 480 · 944 · 512 | None for the still. Whole-body look frames (19) restored in v11; Hatt's pass/fail pending | "no dude.. you had the original fix it" · V6, 2026-11-06/07 (exact date not recorded) |
| 2 | Lying loaf | `art/frames/loaf/000.png` | `b9ad734d97f2e8c31897c0e3dd3a129b821cf8723042cf527d4ea9afd6d817be` | original | 432 · 948 · 512 | Whole-body look frames (12) restored in v11, pending pass/fail. Floor contact still needs visual review (V7 note) | none recorded for this state |
| 3 | Lying awake | `art/frames/lay/000.png` | `bee0c93c1f17ccd33e359dea2de62bb8d7fcaa2cb323603a1f72a2093be3ab50` | original | 480 · 927 · 512 | Whole-body look frames (14) restored in v11, pending pass/fail. Max placed drift 2.44 px is fur-shape change, not translation | none recorded for this state |
| 4 | Sleeping | `art/frames/sleep/000.png` | `6b36174f9c2b6f359c3f04288109b4490765602c72a901b157ebfa557ce5e0f6` | original | 480 · 936 · 512 | Belly gap/contact still needs visual review. sleep_tail 1–3 are drawn 3/6/3 px low in production; v11 DEV corrects this, production does not | none recorded for this state |
| 5 | Belly-up | (none: removed from reviews and builds) | n/a | retired | n/a | Keep retired; do not renumber | "get rid of laying on back... i hate it" · V8, 2026-11-07 |
| 6 | Sitting away (rear) | `draft-assets/restore-v11/rear.png` (from `draft-assets/approved-sitting-v1/rear.png`) | `d1e026232a1cefc0aa196de1fe88485dae4a9e636dbb98ebbd5b719f46e22f04` | owner-approved source; v11 registration is a dev-candidate | 480 · 944 · 512 (source scale 0.457, "owner" candidate) | Pick a scale: "owner" 0.457 vs "head-matched" 0.672 (`local-data/reference-review/restore-v11/sheet.png`) | v1: "approved. keep going"; v10: "not proportionate" · 2026-11-07 |
| 7 | Sitting look-back | `draft-assets/numbered-sizing-v2/sit-lookback.png` | `8344b81f194dd314c0dfaf4dc288edb3ee1867facd14da7de0562ee9d4ce5e9b` | dev-candidate (derived from approved-v1 lookback) | 241.1 · 1091 · bounds center (1254 canvas) | Fur consistency pending. Its scale is the pre-+20% value V8 restored, not the size Hatt last asked for | "bigger and flat bottom" · numbered review v2 (date not recorded) |
| 8 | Sitting side | `draft-assets/restore-v11/side.png` (from `draft-assets/approved-sitting-v1/side.png`) | `bcef1809cda45746173b29cad20f5517df921b098cf0595eae09e4302bc67461` | owner-approved source; v11 registration is a dev-candidate | 480 · 944 · 512 (source scale 0.438, "owner" candidate) | Pick a scale: "owner" 0.438 vs "head-matched" 0.602 | v1: "approved. keep going"; v10 face wrong, must be the short-nosed Persian · 2026-11-07 |
| 9 | Curled sleep | (none: removed) | n/a | retired | n/a | Duplicate of tucked; keep retired | "rejected duplicate of tucked sleep" (paraphrase in `state-head-review.json`) |
| 10 | Side sleep | (none: removed) | n/a | retired | n/a | Keep retired | "looks like a totally different cat. take that shit out too" · V8, 2026-11-07 |
| 11 | Tucked sleep | `draft-assets/approved-tucked-sleep-v1/tucked.png` | `5ded8823177ae40f5f02f87f45e630c85e98a44518c5853be9d22222cae87696` | owner-approved | 406.8 · 742 · bounds center (1774 canvas) | Fur consistency vs sit/000 pending; no runtime animation | "pass!" (`approval.json`, date not recorded) |
| 12 | Grooming | `draft-assets/style-consistency-v7/groom/frames/008.png` (16 frames) | `cdc8c63fffdee9ff7ee41e0595c29269022a467597534d8bff88645de924aeab` | dev-candidate (local duplicate-paw correction; original pixels outside the mask) | 480 · 947 · 512 | Owner review of the paw correction | none recorded |
| 13 | Stretching | `art/frames/stretch/014.png` | `fb2cb29fe03a1809cb0fa7a5172ea7c4c32a8700b4b4634b344a184799cd31bd` | original | 480 · 950 · bounds center | None known | none recorded |
| 14 | Running left | `art/frames/run_left/009.png` | `076b5d6c8a7cc73bda4e0600c99a2224741866b9287446f0ad9ec6867179d85b` | original | 720 · 952 · bounds center | A third style; 18 frames vs 16 for run_right. Needs new art (Step 5, image tool) | none recorded |
| 15 | Running right | `art/frames/run_right/008.png` | `dee0ab18b1d0b8589cdd03e2e308fc114f717f1a2b6549af71267cc110f77e42` | original | 902.4 · 952 · bounds center | Flat, stripeless, small-headed. Needs new art (Step 5) | none recorded |
| 16 | Jumping left | `art/frames/jump_left/008.png` (16 frames) | `3fe7d725937c285bb4537f830ddd474c43fa3e2cbd4a02f970f778e631f5b3ef` | original (V7 restoration; the v5 anatomy overrides are not active) | 480 · 620 · 512 | Mid-flight frames 004–008 are "tubular", legs merged into the torso. Needs new art (Step 5) | v5 anatomy: "approve for now" (superseded by the V7 original restore) |
| 17 | Jumping right | `art/frames/jump_right/008.png` (16 frames) | `ab5c9893b269fe9b2d64d4f7fba1ce619576cb76451b4fd3f234f6074e62708b` | original (V7 restoration) | 480 · 620 · 512 | Same as #16 | same as #16 |

Off and on hold (manifest `deferred`): walking (on hold since 2026-11-01; original 16-frame cycle in `local-data/vm-history/...`),
3D (on hold), breathing (removed), scratching (removed).

## Table 2: engineering debt

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
| E13 | `gh` CLI token expired (401) | GitHub | out of scope; Hatt's call |
