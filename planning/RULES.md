# Odin rules register

The one place the locked rules live. Hatt (the owner) approves or revokes each rule by its ID; nothing here changes
without his words, and a revoked rule stays listed as **revoked** with the date, never deleted. Every build, pack
step, review and plan must obey the **approved** rows. A row marked **pending** is Koa's working default: Hatt has
not ruled on it yet, so it binds Koa but not Hatt. When Hatt gives a new verdict, the rule is added or changed here
first, with his words and the date, then the code or art follows.

How to use it: before changing anything a rule covers, quote the rule ID in the plan step and the commit message.
If the work needs a rule changed, stop and ask Hatt; the ask names the ID.

Status values: **approved** (Hatt's words and date), **pending** (Koa's default, awaiting Hatt), **revoked** (date and words).
Started 2026-11-08 at Hatt's request ("write down these rules somewhere on a file so you can not keep changing shit
that is locked down").

## F — Floor and contact

| ID | Rule | Status | Source |
|---|---|---|---|
| F1 | The floor line is the underside of Odin's main body: his bottom when sitting, his belly when lying, his back or flattened cheek when sleeping on his side. Body, belly, chest ruff, tail and resting cheek all sit on that one horizontal plane. | approved | Hatt 2026-11-08: "the floor was based on his main body... either bottom or belly or back"; `planning/resting-contact-policy.json` (2026-11-06) |
| F2 | Only the toes and the tail hang past the floor line, and they hang **over the edge**: drawn on top of the taskbar or the window edge he sits on, never hidden under it and never cropped. Odin's window stays above the taskbar; when Windows raises the taskbar over him he moves back above it (`app/TaskbarOrder.cs`, v12-3e). | approved | Hatt 2026-11-08: "toes and tail used to hang over the edge"; policy "allowedBelowSupport: paw/toe tips only" |
| F3 | The existing sleeping pose and its tail variants keep the tail hanging over the edge too. | approved | Hatt 2026-11-06: "same as sleeping. go ahead and keep his tail going over on that one." |
| F4 | A sprite is anchored by its support plane, never by its lowest paw pixel. Fur is compressed and bunched under weight; the underside is never cropped, cut or erased to fake contact. | approved | `planning/resting-contact-policy.json`; AGENTS "MANDATORY FLOOR RULE" |
| F5 | Airborne frames have no floor. Ground frames of an action land by actual paw-sole contact; airborne frames keep their original bottom clearance. | approved | AGENTS (jump v4/v5 integration, owner-passed) |
| F7 | Every frame of a rest family (gaze, eyes, tail) shows the same underside as the family's static still: the belly sits on the line in all of them, so he never lifts off the floor while looking around. Where an original gaze frame ends higher than the still, the frame's OWN original pixels are stretched down column by column below a top row (sit: row 820; `tools/local/Apply-FloorContactV12.py`) until its body underside meets the still's; the shift per column is smoothed across columns and capped at 8 native rows. Everything above the top row is the original frame byte for byte; nothing is generated and no pixel from another drawing is pasted in. Never composite the still's lower body into a gaze frame: the two drawings' legs do not line up (v12-3f's row-900 blend drew a line across the legs). | approved | Hatt 2026-11-08: "looks like look around is different than the static image"; v12-3f blend passed "Yes, on the line", then failed on the release candidate 2026-11-08: "you broke the sit image... look at the line across the legs... i thin it's only when looking around"; method changed the same day on his sheet verdict "any except 2. thats the only one that is broken. look at the vertical leg lines" (warp = sheet panel 4; panel 3, the unfixed frame, floats, which he flagged in v12-3e); passed the v12-5g warp on screen the same day: "Looks right" |
| F6 | Sitting places its support at native row 944 of `art/frames/sit/000.png` (the body's last row; rows 945-950 are toes). Loaf 948, lay 927, sleep 936. These rows do not move without a new rule. | approved | production `art/manifest.json` 3.1.2; V11 D2 registration; unchanged through v12 |

## A — Artwork and sizes

| ID | Rule | Status | Source |
|---|---|---|---|
| A1 | Active sitting is the byte-exact original `art/frames/sit/000.png` (sha256 ab5c4261…). No redraw, no rim repair, no slimming. | approved | Hatt 2026-11-07: "no dude.. you had the original fix it" |
| A2 | Sit, loaf, lay and sleep art and their existing gaze, eye and tail frames are locked. New work touches only the states Hatt names. The floor-contact warp of a gaze frame's own original pixels (F7) is the one exception (blend passed 2026-11-08, replaced by the warp the same day). | approved | Hatt 2026-11-08 (G3 choice A): "only for these 3 new ones... the rest are locked in" |
| A3 | Never size a frame by its body. One body per state; head size is judged by ear and skull landmarks. Never normalise by whole canvas, alpha bounds, tail reach or ear-to-floor height. | approved | Hatt's G3 verdict 2026-11-08 (body "keeps growing and shrinking"); AGENTS size rule 2026-11-07 |
| A4 | A STATE is a held pose; an ANIMATION moves from a state and back. State widths are the owner-passed table `planning/sizes-owner-v12.json`. An animation is sized by continuity with the state it leaves and returns to, never by its own factor. | approved (states) / pending (animation rule) | G1 passed 2026-11-08 ("this looks really good"); animation rule is plan v12 2a, Hatt said go |
| A5 | Seated views 6 and 8 are matched to sitting by seated height, not by head landmarks. Moot while 6/7/8 are parked (S2). | approved | Hatt v11.3 2026-11-07: "sitting side and sitting away still too big... on height compared to sitting forward" |
| A6 | Gaze is whole-body frames placed by lower-body registration on the family base. Never a head pasted on a frozen body, never a head crossfade (double faces). | approved | Hatt V10 verdict: look-around "looked like a head cut-out on a frozen body"; V11 D2 |
| A7 | Odin's look: original material and colour pattern; fine, irregular, mostly straight fur with tapered tips; unstriped body, stripes on the legs; compact ear pinnae with sparse wisps; the dark missing-eye socket; flat short Persian face with furry cheeks; modest paws. No bloated redraws, no plush or etched procedural fur, no pointed generic muzzle. | approved | Hatt's style feedback 2026-11-07 (V7-V9) and the 10 approved photos |
| A8 | Jump frames 002-012 draw the original run frames; jump fur is the reduced-shagginess v2; jump anatomy is "approve for now". | approved | Hatt v11.3: jumps "more like the running ones"; jump fur "pass"; anatomy "approve for now" |
| A9 | Production art (`art/**`, `art/manifest.json`) and owner-approved drafts (`draft-assets/approved-*/**`) are never edited. Shipping means promoting the DEV pack into production as one explicit step on Hatt's word. Done once so far: 2026-11-08 for 3.2.0 (Hatt: "okay lets push this one to release"), as the tracked pack `art/latest/` drawn by both builds; `art/frames/**` and `art/manifest.json` stayed untouched. | approved | standing since V6; plan v12 Part 5 |
| A10 | Profile faces need distinct whiskers. | approved, open work | Hatt v12 |
| A11 | Add candidate transitions between the active resting states and between sitting and paw grooming in a DEV review. Existing approved endpoint PNGs, sizes and floor rules remain locked (A1/A2/F6); intermediate drawings and timings require their own visual pass. No production art promotion is authorized by this request (A9). | approved scope, artwork pending | Hatt 2026-11-15: "can we add in transitions between states to odin. also transition between the licking paw animation and the sitting state?" |
| A12 | Correct the transition candidate's apparent growth, body slide and floating rest poses. Candidate-only uniform display scales and horizontal registration may change to maintain physical continuity; candidate body-support metadata may be corrected from the actual underside, superseding F6 coordinates and A11's preserved registration only in that review. Preserve source PNG bytes and the production pack; exact corrections remain pending visual review. | approved correction scope, measurements pending | Hatt 2026-11-15: "his body is moving and also looks like it's changing sizes... the wash paw looks slightly bigger and slides him to the left... some of the still poses are still not on the floor... where as your new stages are" |
| A13 | The v1/v2 transition drawings fail the locked fur/style requirement A7. Preserve them as rejected provenance; do not promote or treat registration fixes as texture approval. Recover from the exact original sitting artwork, using a narrow paw-edit pilot before extending new drawings. The pilot is a development candidate until visually passed. | owner rejected style; repair approach pending visual pass | Hatt 2026-11-15: "looks like you're losing control of the fur and texture again" |
| A14 | The narrow texture pilot is an internal authoring check, not a required owner decision. Continue the authorized transitions and present complete rendered motion for review. Preserve A7's original fur/material and original approved sources; no approval of new artwork is inferred. | approved continuation scope; complete motion pending review | Hatt 2026-11-15: "i don't know what that exe is supposed to help me decide", then "so what an i waiting for then?" |
| A15 | Preserve the exact v3 motion preview artifacts as a scoped visual pass. The owner's response approves what he could see in the presented review; do not infer that he launched the EXE, passed live desktop checks or authorized production promotion/publication. P1's EXE gate and A9 remain unchanged. | visual preview passed; EXE review not confirmed | Hatt 2026-11-18: "from what I can see it looks fine" |
| A16 | Repair the sit/lay/sleep tail flip in a new complete DEV candidate: make the tail visibly flip and return without a fur aura or whole-body redraw, movement or resizing. Preserve the frozen v3 transition preview and production sources. New tail motion remains pending owner review. | approved correction scope; motion pending | Hatt 2026-11-18: "the tail flip is bad and broken. it doesn't really flip the tail at all and it also creates a weird fur aura thing. that needs fixing" |
| A17 | V4 tail warp is rejected for BOTH shape and movement. Preserve its evidence but do not promote, reuse or describe it as a successful repair. Revisit anatomy and motion before another complete candidate; numerical preservation checks do not establish visual quality. A16's repair scope remains authorized. | v4 rejected; replacement pending | Hatt 2026-11-18: "nope thats terrible"; asked whether shape, movement or both: "Both" |
| A18 | V5 is rejected for thinning and extending the tail. Keep the original resting tail's fullness, length, reach and attachment; only its small distal end flicks up a little and returns. No whole-tail uncurl, extension, tall hook or narrowed redraw. V6 may animate the original tip locally with a small lift; the rest of the tail/body remains exact. Preserve v4/v5 rejection evidence and v3 transitions. | approved narrowed correction scope; replacement pending visual review | Hatt 2026-11-18: "fail... that was weird... it like got skinny and extended... the end of the tail just needs to flip up a little bit" |
| A19 | V6 moves too little of the tail. Extend the moving end section farther toward its base, retaining the original fullness and reach and the small upward lift. Do not restore V5's skinny extended uncurl or V4's whole-tail distortion. | approved wider moving-section scope; candidate pending | Hatt 2026-11-18: "lol thats not enough of the tail" |
| A20 | Repair jerky sit/loaf and sit/lay candidate motion; audit actual pose progression, ordering and frame timing. Add direct loaf/lay transitions both ways; these rest changes must not route through sit. Preserve approved source images and the immutable v3 preview evidence, but its earlier limited visual response does not pass the now-rejected motion. Carry forward V7 tail unchanged without inferring its approval. New motion remains a DEV candidate. | approved repair/direct-route scope; motion pending | Hatt 2026-11-18: "whoa those transitions are bad and jerky between sit and loaf sit and lay down. i think there's a frame out of order too? also laydown and loaf shoul dnot go through sit." |
| A21 | Scrap V8's warped/morphed transition motion. Use the Bruno/Judge workflow: individually drawn, anatomically ordered poses between the held states with short intentional frame timings. No optical-flow, silhouette blending, procedural morphing or warped in-betweens in the replacement. Direct loaf/lay remains required. Preserve original art, V7 tail and prior rejection evidence. | V8 rejected; drawn-frame replacement authorized, pending visual pass | Hatt 2026-11-18: "scrap it... thats terrible. weird ass animation... why not jsut do frames between states like you did for bruno and the judge. this look awful" |
| A22 | V9 is rejected for jerky motion, frame movement and apparent growth. Audit actual anatomical scale, body registration and between-pose displacement before another handoff. Correct or replace inconsistent drawings while retaining A21's drawn-frame method and direct loaf/lay path. No timing-only fix or mechanical test constitutes a visual pass. | V9 rejected; correction authorized, replacement pending | Hatt 2026-11-18: "nah not fluid or smooth. jerky and it looks like some frames still jerk around or grow" |

A22 clarification, 2026-11-18: Hatt answered "all equally bad" for sit/loaf,
sit/lay and direct loaf/lay. Reject the entire V9 rest-transition set. There is no
preferred route to retain as visually passed. New internal measurements or drawing
experiments do not supersede that rejection or establish approval.

### A23 — Live motion review requested, 2026-11-18

Hatt: "i need to see the exe.... the motion is the test not still frames".
Package the latest V11 discrete drawn sequence into a separate DEV test EXE for
owner motion review, including direct loaf/lay. Internal still/geometry findings
remain diagnostic concerns, not a reason to withhold this requested executable.
This supersedes the local no-handoff condition for V11. Preserve all original
art and prior evidence. No visual approval, promotion, installation or release is
implied; P2's headless-only agent execution and P5 still apply.

### A24 — V11 live transition motion passed, 2026-11-18

Hatt: "pass", responding directly to the handoff of
`test-exes/Odin-Dev-Motion-v11.exe`. Record an owner visual EXE pass for V11's
sit↔loaf, sit↔lay and direct loaf↔lay transitions, in both directions as presented
for review. Preserve the exact handed-off binary, embedded candidate pack,
timing/registration, authoring evidence and test reports in
`draft-assets/approved-transition-motion-v11/`. This live verdict supersedes
earlier internal still/geometry assessments of this same candidate. It does not
approve unrelated tail work, waive the recorded canvas-boundary check, or imply
production promotion, installation, publishing or release (A9/P2).

### A25 — Prepare production release, 2026-11-18

After the release-preparation list, Hatt: "so gooooo". Authorizes production
integration of the A24-passed rest transitions and their matching awake-state
registration, resolution of the canvas-edge check without changing reviewed
pixels or playback, and building/verifying the production EXE and installer.
Use transparent storage margins with an explicit original-content rectangle;
the renderer must reproduce the reviewed frames exactly. Promote only the six
approved awake transitions and necessary registration. Keep existing released
tail artwork/behavior; the separate tail correction remains pending. Preserve
the A24 snapshot and original art/frames and art/manifest.json. P2/P5 apply.

### A26 — Publish the prepared release, 2026-11-18

Hatt asked "ready for publish?", then "so gooooo" after the release-preparation
list, and completed the requested GitHub sign-in ("done"). In this sequence the
instruction authorizes publishing the verified 3.3.0 release and the source commit
needed to build it. Use a separate release branch, preserve existing local work,
and publish only the scope listed under A25. No installation or desktop takeover.

### A27 — Correct stretch/sit continuity, 2026-11-19

Hatt: "wasn't the stretch supposed to flow better with sit so it doesn't teleport
at the end of stretch?" Correct the omitted handoff in a new test candidate.
Preserve original sitting and stretch PNGs and all A24-passed rest motion. Inspect
the actual full stretch, including entry and return, in the companion EXE. Source
registration and timing may be corrected; no new approval or publication is
inferred. Keep the prepared release draft unchanged while this is reviewed.

### A28 — Grooming entry/return placement, 2026-11-19

Hatt on the stretch candidate: "better... same needs to happen for grooming too...
it slides a little and the begining and ending of the animation". Retain the
stretch candidate and correct grooming's placement at entry and return in a
combined test EXE. Preserve original groom PNGs, held sitting and A24 rest motion.
Existing drawn paw-entry/return artwork may be reused with consistent endpoint
registration. Finish each lick cycle before lowering the paw; no runtime morphs
or image crossfades. Full live motion review remains the decision point. No
production promotion or draft-release update is inferred from this correction.

### A29 — Grooming late-frame size consistency, 2026-11-19

Hatt: "i think one of the later frames of grooming is slightly bigger than the
others..." Audit the actual grooming playback for inconsistent anatomical size.
Compare matching head poses, skull and pinna landmarks rather than body bounds
or projected eye spacing. Correct confirmed outliers in a separate candidate;
preserve source PNGs, cycle order/timing, the A28 entry/return and retained stretch
unless a boundary must share the corrected frame's exact registration. Review
the result in a complete EXE. No promotion/publication or new visual pass implied.

### A30 — V2 scale tweak rejected; repair grooming loop, 2026-11-19

Hatt: "dude it's still growing or puffing up or something..." V2's 5% adjustment
did not resolve the reported defect; preserve it as rejected evidence. Do not
keep applying isolated scale guesses. Correct the repeating pose sequence and
inconsistent torso/ruff appearance. A candidate may reuse the closely matched
original lick poses, lower the head once on entry and raise it once on exit,
instead of repeating the upright redraws within each lick cycle. Preserve source
art and held sitting, keep the 3.5-second cycle and cycle-complete return behavior,
retain stretch and the A24-passed rest motion. No morphing/crossfading/head-cutout
substitute. Full EXE review required; no production or release promotion implied.

### A31 — Stable grooming passed; shorten complete action, 2026-11-19

Hatt on V3: "thats good. lets reduce that action to 2-4 seconds". Preserve the
exact V3 grooming motion as a scoped live EXE pass. In the next candidate, the
complete grooming action (paw entry through return to sitting) lasts 2–4 seconds.
Keep its two matched lick drawings and existing per-drawing cadence; choose a
small number of complete lick pairs and preserve the entry/return motion. Do not
truncate a lick or speed up the reviewed motion. Retain stretch and passed rest
poses except the separately requested border correction. No publishing implied.

### A32 — Sit/loaf right-edge artifact, 2026-11-19

Hatt: "the transition between sitting and loaf introduces artifacts on the right
border of the image box". Remove stray sheet-border content from rendering in a
new combined candidate. Preserve original PNGs, cat pixels, frame registration
and timing; use a conservative source viewport beyond the actual cat's silhouette
if inspection confirms foreign edge content. Validate through native rendering
and include both directions for live EXE review. Keep the release draft unchanged.

### A33 — Stretch's stray seated return frame, 2026-11-19

Hatt: "the end of the stretch... there a stray frame. looks like it's sitting
and it flashes left of where he ends up sitting". Remove the redundant seated
variants from the end of the stretch playback and settle directly on the exact
held sitting image. Keep the source PNGs, earlier stretch motion and total 4.6s;
transfer omitted holds to the final sitting image. Verify the real held-state
menu return as well as ordinary playback; a stretch finishes in sitting before
any transition back to a held loaf/lay state. Preserve V4 grooming timing and
border cleanup. New combined EXE review; no promotion or release update implied.

### A34 — V5 combined action EXE passed, 2026-11-20

Hatt: "pass", directly after the V5 EXE handoff. Record the live motion pass
for the corrected stretch ending, with the retained short grooming and sit/loaf
border cleanup as presented in that combined candidate. Preserve the exact
`test-exes/Odin-Dev-ActionReview-v5.exe`, embedded artwork pack, timing/registration,
source snapshot and check reports in `draft-assets/approved-action-review-v5/`.
No further retuning of these passed motions without a new requested correction.
This verdict does not expand to unrelated tail work or itself authorize a new
production promotion/publication. Earlier release authorization/blocks remain
separate; recording this pass does not alter production or the remote release.

### A35 — Push and merge passed V5, 2026-11-20

Hatt: "ready for push to github them", followed by "merge it or whatever too".
Integrate the A34-passed V5 native pack/runtime into production source, verify
the independent build and approved motion, commit/push a separate branch and
merge it into main. Preserve the working checkout/index, frozen approvals,
original art/frames and art/manifest.json. Carry only the approved action fixes;
unrelated pending tail work stays out. This request does not require installing
the app or making the GitHub draft release public.

## S — States and IDs

| ID | Rule | Status | Source |
|---|---|---|---|
| S1 | State IDs are never renumbered. Retired for good: 5 belly-up, 9 curled, 10 side sleep. Parked (kept, out of builds): 11 tucked (no verdict). | approved | Hatt V8 corrections 2026-11-07 |
| S2 | States 6 sitting away, 7 sitting look-back and 8 sitting side are out of this version entirely (no still, menu item, hold or test). They do not return to a build without Hatt's word. | approved | Hatt 2026-11-08: "let take those 3 new sits out for this version" |
| S3 | No 12-o'clock head-direction looks for the three new states in this version. The 36 frames and code stay on disk, off. | approved | Hatt 2026-11-08: "i said I didn't want any of the new 12 oclock looks for the 3 new states... they're broken" |
| S4 | Breathing is removed entirely and never comes back. Scratching is removed. Walking and 3D are on hold. | approved | Hatt (AGENTS, 2026-11-06/07) |

## B — Behaviour

| ID | Rule | Status | Source |
|---|---|---|---|
| B1 | Perch and zoomies are animations for any state that is not sleeping, never from sleep. From loaf, lay or belly he sits up first. | approved | Hatt 2026-11-08: "perch and zoomies are animations for any state that isn't sleeping" |
| B2 | Running is registered by the nose tip, one frame per 16 ms tick, with in-between frames only where the ghost gate passes. Zoomies dash at 1000 px/s (production too since 3.2.0, promoted 2026-11-08). | approved | G2 passed 2026-11-08; promoted on "okay lets push this one to release" |
| B3 | Jumps tilt along the flight arc, nose up rising and down falling, within ±45°. | approved | G2 passed 2026-11-08 |
| B4 | Perch landings keep the body and tail on the window top (visible-margin clamp in DEV). | approved | AGENTS V4 DEV perch margins, owner-passed |

## P — Process and approvals

| ID | Rule | Status | Source |
|---|---|---|---|
| P1 | Nothing is owner-approved until Hatt passes it on an exe at a gate. Koa's defaults are labelled pending in the records and here. | approved | standing |
| P2 | No install, release, push, publish or remote change without Hatt. Test exes live under `test-exes/`. Koa never closes Hatt's apps, opens visible windows, takes over the desktop or runs windowed tests; checks are headless. | approved | standing (AGENTS owner interaction rule) |
| P3 | A change to anything a rule here covers starts with the rule: Hatt's words and date go into this file first, then the code or art follows. A revoked rule is marked revoked, not removed. | approved | Hatt 2026-11-08: "I want to make sure it gets documented and approved or i can revoke them" |
| P4 | Before a ship cut Koa lists the version's changes; Hatt flags each one to keep or take out; unflagged means kept. | approved | Hatt 2026-11-08: "list the changes for that version... i'll flag if it's good or to take out for now" |
| P5 | Hatt runs Verify's registry test himself; Koa runs Verify with `-SkipRegistryTest` and says so. | approved | standing |
