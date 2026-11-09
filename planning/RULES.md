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
