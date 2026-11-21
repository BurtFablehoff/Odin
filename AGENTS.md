RELEASE 3.3.1 - 2026-11-20: owner explicitly requested "publish it".
Packages the passed V5 source/artwork with a newer version for installed 3.3.0.
Twelve production checks and exact approved-actions comparison pass; startup
registry check skipped. Installer payload SHA256 matches tested production EXE.
Build/check files: build/release-3.3.1/source-worktree/build/release-3.3.1.
The old 3.3.0 draft remains historical and must not be published as the V5 update.

# Odin local workflow

V5 PRODUCTION SOURCE INTEGRATION - 2026-11-20: A35.
Owner requested GitHub push and merge. art/latest now contains the exact A34
V5 manifest/payloads; the source runtime matches the frozen reviewed runtime.
Production verification: all 216 resources identical, 84 approved action frames
match in 504 size/background comparisons, all 84 render through the production
path, and 300 complete grooming actions remain 2–4 seconds. Twelve production
checks pass, with the startup registry test explicitly skipped. Independent
worktree build requires no local-data or parent framework checkout.
planning/github-v5-checks.json contains the verification receipt. The source
change is being committed on a separate branch for main; the owner's original
working branch/index and all frozen artifacts are preserved. Existing draft
release binaries predate V5; this GitHub source push does not publish that draft.

OWNER EXE ACTION PASS V5 - 2026-11-20: A34. Hatt: "pass".
Approved combined candidate: corrected stretch ending/held return, retained
2–4 second grooming and sit/loaf right-border cleanup. Exact reviewed EXE/pack,
timing/registration, source snapshot and seven reports are frozen in
draft-assets/approved-action-review-v5/approval.json (74 SHA256-verified files).
planning/current-action-review.json records the pass; stretch/groom receipts
point to the same approved combined result. Preserve this sequence and use a
new revision for any requested correction. Unrelated tail approval is not inferred.
Recording the pass did not change production or the remote release.

HISTORICAL V5 HANDOFF - 2026-11-19: A33; passed under A34 above.
Owner saw a stray seated flash at the end of stretch. V5 omits the redundant
original seated variants 025/026/027 from playback and holds the exact sitting
endpoint for their time. Original PNG payloads remain intact; the other selected
stretch frames and total 4.6s are unchanged. Held loaf/lay now return via the proper
sit-to-rest transition after the seated endpoint, rather than using stale lastRest.
test-exes/Odin-Dev-ActionReview-v5.exe retains V4's short grooming and border
cleanup unchanged. Seven checks pass, including actual held-menu return rendering
and absence of the three seated variants. Owner live verdict pending; quit v4
manually then test Animations > Stretch. Receipt: planning/current-action-review.json.
Rebuild: tools/local/Build-ActionReviewV5.ps1. Production/release draft unchanged.

CURRENT COMBINED ACTION REVIEW V4 - 2026-11-19: A31/A32.
Owner passed V3 grooming ("thats good") and requested a complete 2–4 second action.
Exact V3 motion/evidence is frozen in draft-assets/approved-groom-motion-v3/.
test-exes/Odin-Dev-ActionReview-v4.exe preserves the .21875-second lick-pose pace
and uses complete lick pairs. 300 episodes from sit/loaf/lay lasted 2.275–3.71s
including preparation, paw entry and return. Mid-lick retarget still completes
the pair. Retained stretch is unchanged.
Owner also flagged sit/loaf's right image border. Conservative visibleSourceRect
metadata excludes foreign sheet content beyond the cat plus a 32-native-pixel
margin. Source PNGs, registration and timing are unchanged. Native DrawScene
checks cover 42 cases/three sizes/both directions, preserve opaque cat pixels
and remove the stray right-edge samples. Seven headless checks pass; registry
test explicitly skipped. Live verdict pending: quit v3 manually, test Grooming
and Sitting forward/Loaf both ways. planning/current-action-review.json holds
hashes/reports. Build: tools/local/Build-ActionReviewV4.ps1. Production and draft
release are unchanged; do not publish this candidate without its motion review.

CURRENT GROOMING STABLE LOOP V3 - 2026-11-19: A30.
V2 was rejected: "dude it's still growing or puffing up or something...". Do not
repeat the isolated scale tweak. test-exes/Odin-Dev-GroomReturn-v3.exe uses two
matching original bowed-head lick drawings in sixteen slots over 3.5 seconds,
with one head-lowering entry and one head-raising return. The upright/variable
torso redraws are removed from the repeating loop. No PNG edits, warps, crossfades
or pasted-head animation; no V2 scale reduction. Original source payloads,
retained stretch and passed rest metadata are unchanged. Four checks pass;
owner motion verdict pending. Quit v2 manually, then test Animations > Grooming.
Build: tools/local/Build-GroomStableReview.ps1. Current receipt:
planning/current-groom-review.json. V2 evidence: groom-review-history/v2-rejected.json.
Production and the release draft remain unchanged.

REJECTED GROOMING SCALE REVIEW V2 - 2026-11-19: A29/A30.
Owner reports a slightly larger later lick drawing. Candidate
test-exes/Odin-Dev-GroomReturn-v2.exe reduces drawing 13 (index 12) by 5% while
preserving its rear pin and support line. This is a conservative live-review
correction, not a visual pass; sparse skull texture fits were excluded. Only
that frame's width/anchor metadata changes from v1; all PNG bytes, timing, paw
entry/return, retained stretch and passed rest clips remain unchanged. Five
headless checks pass. Receipt: planning/current-groom-review.json. Rebuild:
tools/local/Build-GroomScaleReview.ps1. Owner quits the running v1 manually before
opening v2; review Animations > Grooming. Production/release draft unchanged.

PREVIOUS GROOMING RETURN REVIEW V1 - 2026-11-19: A28.
Owner says stretch is "better" and requests the same correction for grooming's
entry/exit slide. test-exes/Odin-Dev-GroomReturn-v1.exe combines the unchanged
stretch candidate with the original 16 lick PNGs, V3 placement corrections and
drawn paw-raising/lowering clips. All four entry/exit boundaries use identical
source/placement on both sides. Behavior completes a lick cycle before returning.
Four checks pass: grooming (78 rendered frames from three entry states, including
mid-lick pose request), retained stretch (120 frames/four entry states), artwork,
menus. Owner motion review pending through Animations > Grooming; let it finish.
planning/current-groom-review.json records exact hashes. Build with
tools/local/Build-GroomReturnReview.ps1. Production/release draft unchanged.

CURRENT STRETCH RETURN REVIEW - 2026-11-19: A27.
Owner flagged omitted stretch/sit continuity. The original stretch's final drawing
was about 74 px horizontally from held sitting at default size. Candidate
test-exes/Odin-Dev-StretchReturn-v1.exe retains all 28 original stretch PNGs,
registers forepaw placement and uses the exact held sitting PNG at both boundaries.
Original seated final drawing's head gap is .35 px; final held-state handoff is
pixel/registration identical. Total action remains 4.6 seconds. Three headless
checks pass, including 120 actual rendered frames across four entry states.
Owner motion review pending: Animations > Stretch. Build-StretchReturnReview.ps1
reproduces it. planning/current-stretch-review.json holds exact hashes and reports.
All A24-passed rest clips and production art remain unchanged. 3.3.0 is still a
GitHub draft; this stretch candidate has not been promoted or uploaded.

PRODUCTION RELEASE PREPARED 3.3.0 - 2026-11-18: A25.
Only the six A24-passed awake rest transitions and matching awake registration
are promoted to art/latest. Existing released tail motion remains in place;
the separate V7 tail correction and new groom/sleep artwork are not included.
Four sheet frames have transparent storage margins with explicit contentRect;
runtime reconstructs the original viewport. All 42 frames reproduce the passed
review exactly in 252 size/background render comparisons. All twelve production
checks pass, including the frame-edge check. Registry test explicitly skipped.
Build/verify: tools/local/Verify-Release.ps1 -SkipRegistryTest; optionally supply
-ApprovedPack with the frozen V11 candidate.zip for the exact visual comparison.
EXE and installer: build/release-3.3.0. Installer archive/payload verified without
installing; source-only independent build and motion check pass. GitHub publishing
has verified access; see planning/release-checks-3.3.0.json.

OWNER EXE MOTION PASS V11 - 2026-11-18: RULES A24. Hatt: "pass".
Approved scope: sit/loaf, sit/lay and direct loaf/lay transitions as presented in
test-exes/Odin-Dev-Motion-v11.exe. Preserve this exact binary/pack/sequence.
94 files of binary, pack, sources, original authoring evidence and test reports
are copied and SHA256-verified in draft-assets/approved-transition-motion-v11/;
approval.json is the immutable receipt. planning/current-transition-review.json
records the live pass. Earlier internal still/geometry findings are history,
superseded by the owner's live motion verdict for this candidate.
The canvas-boundary test failure stays recorded; do not silently waive it or
change passed motion while addressing it. Unrelated tail approval is not inferred.
No production promotion or release requested. Use a new revision/path for changes.

HISTORICAL OWNER MOTION REVIEW V11 HANDOFF - 2026-11-18: RULES A23.
Hatt explicitly requests the executable: "the motion is the test not still frames".
test-exes/Odin-Dev-Motion-v11.exe embeds the sheet-only V11 candidate. Seven frames
per route including original endpoints, five discrete intermediate drawings;
sit/loaf, sit/lay and direct loaf/lay all run both directions in .315 seconds.
Build/check: tools/local/Build-CoherentMotionReview.ps1. Owner reviews by choosing
Sitting forward, Loaf and Laying in the menu. No agent visible launch or app closing.
Internal image/geometry concerns below remain evidence, not a reason to withhold
the requested EXE. Status is OWNER MOTION REVIEW PENDING, not visually approved.
V7 tail and unrelated behaviors/art are retained; production is unchanged.

HISTORICAL INTERNAL ASSESSMENT V11 (SUPERSEDED BY OWNER EXE PASS) - 2026-11-18:
Single-generation pose sheets and small-edit bridges did not fix the motion.
See draft-assets/coherent-motion-v11/README.md and prompts.json. Sheet-only
sit/lay still jumps about 40 screen px between adjacent heads. Mixing old
bridges back into these sheets reintroduced changing chest volume. None passed
internal review; no V11 EXE, runtime flag, promotion or approval. All original
endpoint bytes/registration and previous failed evidence remain preserved.

REST TRANSITIONS REJECTED - 2026-11-18: RULES A22.
Owner confirms ALL THREE routes are equally bad: sit/loaf, sit/lay, direct loaf/lay.
V9 is rejected; its EXE is historical evidence, not a recommended test candidate.
The V10 internal registration/drawn-bridge experiment also fails review. No V10 EXE
has been built. See planning/transition-review-history/v10-internal-failed.json.
Diagnostics: tools/local/Audit-GeometryMotion.py and Inspect-SkullSimilarity.py.
New raw drawings and built-in imagegen prompts: draft-assets/geometry-motion-v10.
Do not promote these or describe them as smooth. Eye spacing depends on head yaw;
normalizing it enlarged bodies. Forehead feature matching is diagnostic only and
does not prove consistent chest volume, joint progression, texture or silhouette.
Alpha-left/tail-tip anchoring and a fixed contact-column range are not anatomical
registration proof when the tail/haunch/paws change between drawings. Inspect
pelvis support, planted paws and head progression separately at actual display size.
V10 still has a head-height reversal and abrupt rear-body/limb-outline changes.
Preserve production, frozen V3 evidence, all rejection records and source PNGs.
No further owner test handoff until the complete motion survives internal review.

HISTORICAL DRAWN REST FRAMES V9 (REJECTED) - 2026-11-18: RULES A21/A22.
planning/current-transition-review.json; test-exes/Odin-Dev-DrawnMotion-v9.exe.
Build/test/export: powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\local\Build-DrawnMotionReview.ps1
Owner SCRAPPED V8's weird warped motion and requested the Bruno/Judge method.
V9 plays individually drawn whole poses, five intermediate drawings per sit/loaf
and sit/lay route, three per direct loaf/lay route. Total times .40/.42/.40 seconds,
with explicit per-frame holds, in forward/reverse order. No optical-flow, procedural
morphs, silhouette blends or warped in-betweens in this replacement. New artwork,
prompts, exact registrations and guides: draft-assets/drawn-motion-v9.
V7 tail, all existing held PNGs, groom/sleep transitions and gaze remain unchanged.
The behavior clock and native frame selection both use authored durations; tests
check elapsed route time as well as frame order and floor contact. New art/motion
failed owner visual review. Mechanical pass is not aesthetic approval.
V8 artifacts remain rejection evidence in planning/transition-review-history/
v8-rejected.json. -MotionReview now rejects instead of rebuilding V8 by mistake.

REST MOTION V8 REJECTED - 2026-11-18: A21. Weird warped motion; do not reuse.
planning/current-transition-review.json; test-exes/Odin-Dev-Motion-v8.exe.
Build/test/export: powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\local\Build-SmoothMotionReview.ps1
Add -Reauthor to regenerate motion fields and in-betweens from retained source keys.
Owner rejected V3/V7's jerky five-image sit/loaf and sit/lay playback, and required
loaf/lay to change directly. V8 has 35/39/30 frames per direction for sit/loaf,
sit/lay and loaf/lay, preserving original held endpoints and V7 tail unchanged.
The sit/lay middle pose that pulled paws back is omitted (old key 2); no index
shuffle was found. Dense playback uses registered source material plus one drawn
low loaf/lay intermediate. New early/late raw sources/prompts are in draft-assets/
motion-v8; late is unused because its leading paw overshoots the endpoint.
Actual native placements are checked for iris continuity/order, body contact,
reverse consistency and all 16 routes. Low-state routes cannot contain sit.
Mechanical checks do not pass visual fur/anatomy. V8 awaits owner visual review;
the immutable V3 preview snapshot and all earlier builds stay preserved (A15/A20).
Only the six named transitions change. Sleep/paw-groom transitions, original
16-frame groom, existing gaze and V7 tail remain byte/metadata unchanged.

CURRENT WIDER END FLICK V7 - 2026-11-18: RULES A19.
planning/current-tail-review.json; test-exes/Odin-Dev-Tail-v7.exe.
Build/test/export: powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\local\Build-TailReview.ps1 -Tip -TipRevision 7
V6 moved too little of the tail. V7 moves a wider distal section, maintaining
V6's small lift and original coat/fullness/reach. Sit/lay cap extends to native
x330 instead of x280; sleep spans x430..520 instead of x455..505. Tail attachment,
body, original neutral images and v3 transitions stay unchanged. Owner visual
review is pending. V6 evidence is planning/tail-review-history/v6.json; its EXE,
pack and previews remain intact. -TipRevision 6 explicitly reproduces V6.

SMALL TIP FLICK V6 SUPERSEDED - 2026-11-18: too little tail moved (A19).
planning/current-tail-review.json; test-exes/Odin-Dev-Tail-v6.exe.
Build/test/export: powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\local\Build-TailReview.ps1 -Tip
Owner rejected V5 because it got skinny and extended; wanted only the END to flip
up a little. V6 restores the exact original full resting tail and animates a small
distal cap only (native rectangles: sit x220..279/y790..969; lay x220..279/y750..969;
sleep x455..504/y865..969). Maximum lift 12/10/8 native px respectively, roughly
4-6 px at default display size. No horizontal displacement or new coat drawing;
all pixels outside those cap rectangles, placement and v3 transitions unchanged.
Tail-tip runtime is app/TailTipRenderer.cs, independent of rejected V4 renderer.
Tail fullness/width checks supplement pixel/placement tests; they are NOT a visual
approval. Owner review remains pending. V4/V5 binaries and rejection evidence stay
preserved. Old build descriptions below are history, not current recommendations.

DRAWN TAIL V5 REJECTED - 2026-11-18: A18. Thinned and extended the tail.
Historical candidate details:
planning/current-tail-review.json is the current handoff; test-exes/Odin-Dev-Tail-v5.exe.
Build/test/export: powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\local\Build-TailReview.ps1 -Drawn
V4's warp failed BOTH shape and movement. V5 replaces it with three independently
drawn tail keys per resting pose, played out and back (seven slots, original .70s).
No runtime warp or silhouette crossfade; original pixels outside bounded tail edits,
neutral endpoints, source art and all v3 transitions are preserved. Tail sources,
prompts and authoring code: draft-assets/tail-v5 and tools/local/TailDrawnFrames.cs.
This is a new visual candidate, not a passed repair. Mechanical tests verify
selection/placement/preservation, not natural anatomy or appealing motion.
V4 binary/pack/previews stay intact; rejection and source snapshot are retained in
draft-assets/tail-v4-rejected. Do not point the owner to V4 for another test.

TAIL REVIEW V4 REJECTED - 2026-11-18: RULES A17. Owner: "nope thats terrible";
shape and movement: "Both". Do not reuse the v4 warp or present it as successful.
Historical candidate details below (not a current test recommendation):
test-exes/Odin-Dev-Tail-v4.exe; planning/current-tail-review.json is the handoff.
Build/test/export headlessly with Windows PowerShell (process execution policy as needed):
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\local\Build-TailReview.ps1
The frozen v3 pack supplies all transitions and unchanged PNG payloads. Only the
sit/lay/sleep tail timelines and candidate rendering change: 17 eased local tail
deformations of each held image's own pixels; same .70-second cycle and intervals.
No whole-cat redraw or silhouette crossfade. Outside the tail regions, pixels
are exact; first/last frames equal the held pose. Seven headless checks pass,
including 51 automatic and 51 manual companion selections, plus preservation audit.
Registry test skipped (P5). Tail artwork/motion needs the owner's visual EXE pass.
Use Actions > Swish tail in each supported state. Owner closes other DEV instances
manually first; never close them or launch visible UI on the owner's behalf.
Profile whiskers (A10) remain separate open work. Parked/removed states stay out.
The v3 preview pass and immutable evidence below remain scoped and intact.

V3 PREVIEW VISUAL PASS - 2026-11-18: owner "from what I can see it looks fine".
RULES A15 records the scope. Exact reviewed binaries, pack, previews, timing/
registration and authoring evidence are frozen and hash-checked in
draft-assets/approved-transition-preview-v3/approval.json. Preserve this snapshot.
This is a preview visual pass; owner launching/testing the EXE is not confirmed.
Do not infer production promotion, publishing or a release from this response.
planning/current-transition-review.json is the current scoped handoff.

CURRENT COMPLETE MOTION REVIEW V3 - 2026-11-15: RULES A14/A7/A13.
planning/current-transition-review.json is the handoff for the complete companion:
test-exes/Odin-Dev-Transitions-v3.exe. Build and export all motion headlessly:
powershell.exe -NoProfile -File .\tools\local\Build-TransitionReview.ps1 -TextureRecovery
This includes sit/loaf, sit/lay, lay/sleep and sit/paw-wash entry/return, with the
original 16-frame grooming loop intact. The new paw bridges retain the original
head material and protected lower-body/tail pixels. New posture keys are individual
edits of approved poses, not the rejected v1/v2 strips. Candidate registration
corrections remain. All new motion/artwork awaits an owner pass on the complete EXE.
The narrow paw pilot was an internal check, NOT an owner approval gate; do not
stop authorized work for approval of an isolated technique demonstration.
Production artwork and original sources remain unchanged. V1/v2 drawings remain
rejected provenance. The V2 section below is superseded history.

CURRENT TRANSITION REVIEW V2 - 2026-11-15: planning/current-transition-review.json
is the current handoff for candidate sit/loaf/lay/sleep and sit/paw-groom transitions
(RULES A11/A12). V1 failed owner review for body growth/slide and floating stills.
V2 corrects display registration only; source PNGs are unchanged. Build explicitly:
powershell.exe -NoProfile -File .\build.ps1 -Dev -TransitionReview -DevExecutableName Odin-Dev-Transitions-v2.exe
This starts from the promoted 3.2.0 art pack and adds candidate keys; it does not
run the older DEV authoring chain or promote art. Test app:
test-exes/Odin-Dev-Transitions-v2.exe. State-menu changes animate; Animations >
Grooming includes paw lift and lowering. All new motion awaits an owner EXE pass.
Existing endpoint PNGs and production art remain locked. A12 authorizes candidate
scale/anchor/support corrections, including the held poses and their rest families.
V1 evidence is retained in planning/transitions-v1-superseded and build/transitions-v1.
Later historical CURRENT notes describe the previous release, not this candidate.

RULES FIRST: planning/RULES.md is the locked-rule register (floor F1-F6, art A1-A10, states S1-S4, behaviour B1-B4, process P1-P5).
Read it before any work; it wins over the history below. A rule changes only with Hatt's words and date written there first (P3).

Work in C:\_repo\pet\Odin, within the personal local multi-repository pet workspace.
Read ../AGENTS.md, tools/local/RESUME.txt and planning/odin-upgrades.json before work.
All files are local; no OneDrive export or old VM transfer workflow is needed.
Historical prompts, code and review packs are provenance, not current instructions.

CURRENT V12-1 - 2026-11-07 (branch koa/v12-1-states, built in worktree .claude/worktrees/odin-v12): test-exes/Odin-Dev-v12-1.exe.
Terms: a STATE is a held pose (sit, loaf, lay, sleep, rear 6, look-back 7, side 8); an ANIMATION moves from a state and back
(groom, stretch, run, jump) and is sized by continuity with it. Widths: planning/sizes-owner-v12.json (Apply-OwnerSizesV12.py).
Owner exception (v11.3: "sitting side and sitting away still too big... on height compared to sitting forward"): seated
views 6 and 8 are matched to sit/000 by seated height. Tucked 11 parked. Jumps tilt on the arc (DEV).
G1 PASSED 2026-11-08 (owner: "this looks really good"; groom/jump-right median widths "look good"). Still a DEV build, not a release.
Profile faces need distinct whiskers (owner, v12). Plan: ~/.claude/plans/enchanted-marinating-hopcroft.md. BACKLOG is the state table.
CURRENT V12-5G - 2026-11-08 (branch koa/v12-5g-floor-warp from koa/v12-5-ship): 3.2.0 RELEASE CANDIDATE 2. Hatt on rc1: "you broke the sit image...
look at the line across the legs" (only while looking around). The v12-3f row-900 blend pasted the still's legs under the look frames' legs; RULES F7
now WARPS each look_sit frame's own pixels down per column below row 820 onto the still's underside (Apply-FloorContactV12.py). Never composite the still's
lower body into a gaze frame. art/latest re-promoted (pack 321e32f7); test-exes/Odin-Dev-v12-5g.exe; setup + Odin.exe in test-exes/release-3.2.0
(rc1 archived). Hatt's body line = bottom of haunch/tail fur (F6 row 944), toes over it. Hatt PASSED it 2026-11-08: "Looks right". RC3 adds the 3.2.0 menus (Hatt's testing and release layouts; BACKLOG "3.2.0 menus"):
test-exes/Odin-Dev-3.2.0.exe + test-exes/release-3.2.0. Hatt PASSED RC3 (G5) 2026-11-08: "Both look right". Next: Hatt pushes release/3.2.0, PR to main, tag v3.2.0, GitHub release.
CURRENT 3.2.0 RELEASE CANDIDATE - 2026-11-08 (branch koa/v12-5-ship, main tree). Hatt passed v12-3f ("Yes, on the line") and said "okay lets push
this one to release", "GitHub release", "3.2.0". Part 5 of the plan: the passed pack is PROMOTED into tracked art/latest/ (rule A9, the one explicit
step; art/frames and art/manifest.json untouched); tools/local/Pack-LatestArt.ps1 zips it with PowerShell alone and both builds embed it as
Odin.LatestArt; the production exe (build.ps1 without -Dev) draws it by the former DEV render path (DevArtwork, JumpTilt, run registration and
tweens, perch margins, zoomies 1000 now compile in production); DEV keeps holds/stills/menus/tests on top. Verify builds production too and runs
11 headless checks on it. Authoring stays Python -> DEV pack; promotion = Promote-LatestArtV12.py (pins the passed pack sha). Version 3.2.0 in
AssemblyInfo/Program/installer/README; notes planning/release-notes-3.2.0.md. Release: Hatt installs build/Odin-Setup-3.2.0.exe (G5), then
release/3.2.0 -> PR to main (Hatt merges) -> tag v3.2.0 -> GitHub release (Odin.exe + setup, digest checked). Koa never pushes without his rule.

V12-3F - 2026-11-08 (branch koa/v12-3c-clock-off, main tree): test-exes/Odin-Dev-v12-3f.exe, Hatt's SHIP CUT, promoted into 3.2.0. Hatt: "he's
still floating... look at the black background" and "look around is different than the static image". The black band above his
taskbar is his Fit wallpaper's letterbox (62 px minus the 48 px taskbar = 14 px of black desktop), not Odin. The lift was the original
look_sit frames: belly underside 938-941 vs the static sit's 943 at the same support row 944. Apply-FloorContactV12.py (pack step after
run tweens) gives every look_sit frame the sit still's own underside and paws below row 900 (32-row feather, original pixels only,
nothing moved, 19 frames); loaf/lay gaze frames already end on or below their base. --test-body-stability fails on any difference
below the seam. RULES.md F7 approved. Pack poses unchanged (6/7/8 parked). Records: current-test-build.json floorContactV12_3f,
BACKLOG v12-3f. HATT PASSED 2026-11-08: "Yes, on the line" (sit looking around stays on the line). This is the ship cut; shipping =
promoting the DEV pack into production (Part 4, rule A9) on Hatt's word, not yet given.

V12-3E - 2026-11-08 (branch koa/v12-3c-clock-off, main tree): test-exes/Odin-Dev-v12-3e.exe, superseded by V12-3F (gaze underside). Hatt: "some how
you fucked up the floor... toes and tail used to hang over the edge.. the floor was based on his main body... either bottom or belly or
back". The geometry never moved (sit support row 944 since 3.1.2; toes rows 945-950 hang ~3 px past the line by rule F2); the Windows
taskbar had been raised above Odin's window and painted over the toes. app/TaskbarOrder.cs moves him back above the taskbar on the
quarter-second tick unless another always-on-top window above him overlaps him. --self-test taskbarOrder (8 cases). RULES: planning/
RULES.md is the locked-rule register Hatt asked for; every rule has approved/pending/revoked status and his words; change a rule there
first (P3). Pack unchanged from 3d (6/7/8 parked, 10 stills). Records: current-test-build.json taskbarOrderV12_3e, BACKLOG v12-3e.
Nothing owner-approved; Hatt checks the overhang on his screen.

V12-3D - 2026-11-08 (branch koa/v12-3c-clock-off, main tree): test-exes/Odin-Dev-v12-3d.exe, superseded by V12-3E (taskbar order). Hatt: "i said I
didn't want any of the new 12 oclock looks for the 3 new states... they're broken" then "let take those 3 new sits out for this version".
States 6 sitting away, 7 sitting look-back and 8 sitting side are PARKED out of this version's DEV pack (Apply-ParkedStatesV12.py, the
last pack step; manifest.parkedStatesV12): no still, menu item, hold or test case; 10 active stills 1-4, 12-17; IDs kept, never
renumbered. Their art, sizes, the 36 clock frames, briefs, registration and the 3b/3c code stay in the tree for a later version; the
static ApprovedContact review pack still has 13 states. Perch/zoomies from every awake state (3bP) stays. Do not bring 6/7/8 back
into a build without Hatt's word. Records: current-test-build.json parkedStatesV12_3d, BACKLOG v12-3d. Production art untouched;
shipping = promoting the DEV pack into production (Part 4 work). Nothing owner-approved.

V12-3C - 2026-11-08 (branch koa/v12-3c-clock-off from the 3b tip, main tree): test-exes/Odin-Dev-v12-3c.exe, superseded by V12-3D (6/7/8 parked).
He wants to ship soon. At G3b he flagged the v12-3b changes: the rear/look-back/side 12-hour clocks and their tracking are
"revert for now"; perch from any awake state, zoomies never from sleep, and the DEV holds/tests stay. So Apply-ClockV12.py
CLOCK_IN_PACK = False (no clock_* clips) and Apply-HeadTurnsV12.py CENTRE_AS_STILL = False: 6/7/8 are their v12-1 stills (G1
passed) with NO head turns; HasHeadTurns is false, so the clock menu, trackers and hour tests are off and follow-mouse skips them.
The 36 clock frames, registration, briefs and the 3b code are kept in the tree for a later build; two flags restore 3b.
Records: current-test-build.json shipCutV12_3c, BACKLOG v12-3c. Production art/manifest untouched; shipping = promoting the DEV
pack into production (Part 4 work). Nothing owner-approved.

V12-3B - 2026-11-08 (branch koa/v12-3b-clock, main tree): test-exes/Odin-Dev-v12-3b.exe. G3b 2026-11-08: Hatt flagged the clock (C1-C4) "revert for now" and kept perch/zoomies (C5-C8); see V12-3C. The G3 fixes:
states 6/7/8 have a 12-hour CLOCK of head directions on ONE fixed body (the 3R centre below a neck seam; only the Codex head/neck
changes; 0 pixels differ below the seams; ClockGaze 12 slots), owner's choice A "only for these 3 new ones... the rest are locked in":
sit/loaf/lay/sleep art and gaze untouched. Perch and zoomies run from every awake state, never from sleep (Behavior.TryPerch gate,
DEV holds/stills tick into them). Records: planning/clock-registration-v12.json, current-test-build.json clockV12_3b, BACKLOG v12-3b.
The 2 % head-size gate is unmeasured (turned heads have no stable landmark); head size is a visual check. Nothing owner-approved.

CURRENT V12-3 - 2026-11-08 (branch koa/v12-3-codex-art, main tree): test-exes/Odin-Dev-v12-3.exe, FAILED G3 (owner: body "keeps
growing and shrinking", no look up/down, wants a 12-position CLOCK of head directions, and "perch and zoomies are animations for any
state that isn't sleeping"). Cause: each head-turn frame was scaled whole so its lower body overlapped the centre's (up to x1.10), so
the rest of the cat changes size per frame. NEVER size a frame by its body; one body per state, heads sized by ear/skull landmarks.
Fix = plan Part 3b (not started; awaiting Hatt's go). Tucked has no verdict yet. New pixels came from
the Codex CLI image tool (planning/codex-pilot-v12.json: --ignore-user-config, images land in ~/.codex/generated_images, Codex cannot
run shell in its sandbox). Briefs planning/art-briefs-v12/ (COMMON.md carries the identity marks incl. the NECK rule: "his mane comes
from his neck, not his head"); jobs draft-assets/codex-v12/<job>/. States 6/7/8: five whole-body head-turn frames each, registered by
the lower body (planning/head-turns-registration-v12.json, draft-assets/head-turns-v12/), in the DEV pack as look_rear/look_lookback/
look_side; the centre frames are the DEV stills; 6/7/8 follow the mouse (app/HeadTurnGaze.cs). Tucked #11 redrawn and registered for
review (draft-assets/tucked-v12/), still parked. Nothing is owner-approved until Hatt passes it at G3. Production art untouched.

V12-2 - 2026-11-08 (branch koa/v12-2-run): test-exes/Odin-Dev-v12-2.exe, PASSED G2 2026-11-08. Running is registered by
the nose tip (planning/run-registration-v12.json -> app/RunningRegistration.cs), one frame per 16 ms tick, optical-flow tweens kept
only where the ghost gate passes (left 3/18, right 1/16; draft-assets/run-tweens-v12/ghost-gate.json). ZoomiesSpeed 1000 px/s in DEV,
720 in production. Production run frames, timing and offsets are unchanged. --test-run-motion is in Verify. More in-betweens need
drawn frames (Part 3, Codex). Hatt runs the registry test unskipped.

V11.3 - 2026-11-07: test-exes/Odin-Dev-Restore-v11-3.exe. Jump frames 002-012 draw ORIGINAL run
frames (owner: jumps "more like the running ones"; planning/run-style-jump-v11-3.json). Tucked x0.90 more;
lookback back to v11 width (head-match made it "WAY bigger"). Zoomies edge stall fixed in Behavior.cs.
Open: run ghosting/shake (E15), tucked fur too flowy (needs new art). Owner asked about replanning.

V11.2 - 2026-11-07: owner: "fix all the sizes except sitting facing away" (rear head is farther
away, so rear #6 keeps its v11 scale); rear supportY 930 = butt on the line, tail may overhang.
"go ahead an fix the jumping": jump_left 000-003/012-015 throat ruff slimmed by a local warp of
ORIGINAL pixels (Slim-JumpRuffV11_2.py, planning/jump-ruff-v11-2.json); nothing generated; not approved.
test-exes/Odin-Dev-Restore-v11-2.exe is current.

V11.1 - 2026-11-07: owner passed the v11 looks; sizes failed ("jumping is very small,
curled in a ball pretty big"), and "ear size is an indicator too, especially for angles".
test-exes/Odin-Dev-Restore-v11-1.exe applies planning/size-normalization-v11-1.json via
Apply-SizeNormalizationV11_1.py: uniform width factors so each head matches sit/000 by eye AND
ear landmarks (ears carry pitched/turned views). Not owner-approved. Small-head art (rear, lookback,
stretch, jumps) gets bigger bodies when heads match: an art-proportion issue, not a scale bug.
Jump-left "bullfrog" throat/chest ruff is in the ORIGINAL frames and in v4/v5 drafts; needs new pixels.

CURRENT V11 - 2026-11-07 (supersedes V8-V10 notes below where they conflict):
Owner verdicts on V10: rear #6 not proportionate, look-around looked like a head cut-out on
a frozen body, side #8 face wrong. V11 is a DEV test, test-exes/Odin-Dev-Restore-v11.exe;
planning/owner-corrections-v11.json and planning/current-test-build.json hold the details.
WHOLE-BODY look/eyes/tail frames are restored: the 69 original art/frames PNGs are drawn
whole, byte-exact. This SUPERSEDES the V8 line "Do not return to whole-frame gaze rendering".
OriginalRestRenderer's head layer is bypassed (wholeFrameRestFamilies), not deleted.
The "bounce" was placement, not art: look_sit 000-015 carry manifest anchor 950 vs sit 944.
V11 places every frame by lower-body mask registration against its family base (D2); 66 of
69 register at (0,0), sleep_tail 1-3 at dy 3/6/3. --test-body-stability is in Verify; max
placed drift sit 1.98, loaf 1.53, lay 2.44, sleep 1.83 native px (limit 3).
Active sitting is the byte-exact ORIGINAL art/frames/sit/000.png (sha ab5c4261...); the V10
right-flank rim repair (5128 px, never owner-approved) is dropped (D3).
Rear #6 and side #8 are owner-approved draft-assets/approved-sitting-v1/{rear,side}.png,
uniformly resampled into draft-assets/restore-v11/ (Register-ApprovedSittingV11.py). Their
landmarks disagree beyond 3 px, so two scales sit on the review sheet: "owner" (the ratio from
his last +20% request, in the exe) and "head-matched". Hatt picks; neither is final.
Production art/manifest.json is unchanged. No install, release, push or desktop UI control.

CURRENT V10 - 2026-11-07 (supersedes V9/V8 notes below):
test-exes/Odin-Dev-Consistency-v10.exe is the current DEV test, not an installation
or release. Read planning/current-test-build.json and planning/consistency-build-v10.json.
Owner requested integrated corrections, not another preview. Poses1/6/8 are active
DEV revisions; none of these new revisions is owner-approved artwork yet.

Gaze heads are ORIGINAL source pixels registered by native translation onto the
fixed neutral bodies. Sitting sources had been15-22px left of the neutral shoulders.
app/OriginalRestRenderer.cs now corrects this per frame without head scaling or
face crossfades. All45 compiled gazes and540 fixed taskbar/window destinations
passed headless QA. Bodies/paws/support and the repaired sitting rim stay fixed.

Original art/frames/sit/000.png remains byte-exact. Active sitting differs only in
a narrow right-flank hair-tip repair (5128pixels); left flank/tail/head/paws unchanged.
Rear6 uses the ORIGINAL front torso-to-haunch silhouette, same scale/root/support,
with registered back-head/coat. Do not restore the flared lower-body draft or failed
contact flap. Side8 integrates the fuller V9 Persian draft plus the moderate-flat
ORIGINAL jump_right/013 muzzle, retaining its fuller cheeks and unchanged alpha.
Policy/provenance: planning/owner-corrections-v10.json and draft-assets/consistency-v10.

Size by solid ear pinna, skull/face core and forward eye/socket/nose/mouth landmarks.
Never normalize by whole canvas, alpha bounds, tail reach or ear-to-floor height.
Manual landmarks are provisional, not owner-approved anatomy. See
planning/anatomical-sizing-v10.json. Physical side pinna matches front within~2%.
Keep14 active IDs;5/9/10 remain retired. Breathing/scratching remain removed;
walking/3D remain on hold. Original183PNGs,32jump frames and groom correction retained.
Further jump-torso anatomy and look-back/tucked fur review remain unfinished;
do not claim every pose is fully corrected or approved.
No desktop/CUA/native UI control. Owner closes old test apps before opening V10.
All checks were headless; no installation, release, commit, push or remote changes.

LATEST V9 ART FEEDBACK - 2026-11-07: Owner says v8 side is TOO NARROW, its face
must match ORIGINAL front sitting beside it: Odin has a FLAT short PERSIAN face
and FURRIER cheeks/jaw, not a pointed generic-cat muzzle or sparse cheek fur.
Do not solve fullness by widening/scaling the entire sprite or enlarging its head.
Preview only: draft-assets/style-consistency-v9/side/side-fuller-persian.png;
comparison local-data/reference-review/style-consistency-v9/side-persian-front-comparison.png.
Body fullness was edited locally, then a coherent shorter/furrier facial core was
registered using original front/groom style and actual Odin face photos. Original
ears/crown, lower body, legs/paws/tail/contact and display scale remain preserved.
No new EXE: current build remains V8, unchanged. These drafts are NOT owner-approved
or active artwork. Prompts, hashes, caveats and QA: planning/proportion-feedback-v9.json.
Preserve all originals; no desktop/native UI controls. Retired5/9/10 stay removed.

LATEST V8 OWNER CORRECTIONS - 2026-11-07 (supersedes older notes below):
REMOVE belly-up/lying on back #5 AND side sleep #10 entirely from active reviews,
menus and future builds. Curled #9 stays removed. Keep those IDs retired; do not
renumber remaining poses. Artwork/history is retained, never an active approval.
Current test: test-exes/Odin-Dev-StyleRecovery-v8.exe. V7 is superseded: its restored
whole gaze frames bounced the body. Do not return to whole-frame gaze rendering.

app/OriginalRestRenderer.cs now registers only ORIGINAL head/eye/tail layers onto
fixed ORIGINAL neutral bodies, with shared root/support/scale for each family.
All45 gazes keep body/paws/floor RGBA exact; compiled-app QA checked540 identical
taskbar/window destinations and9.4million protected body pixels, no native UI.
V8 restores original runtime widths: sit480, loaf432, lay/sleep480, stretch480,
jumps480; groom480 and original run720/902.4 retained. Previous review multipliers
must not be reapplied. Final policy: planning/owner-corrections-v8.json, applied
after historical feedback by Apply-OwnerCorrectionsV8.py. Original183PNGs unchanged.

Rear6 and side8 use new narrower ORIGINAL-style DEV candidates, NOT owner-approved.
Their whole-sprite display scales match original seated height, without deforming
source pixels or scaling heads independently. Original sitting remains byte-exact.
Side candidate/prompts: draft-assets/style-consistency-v8/side. Rear candidate:
draft-assets/style-consistency-v7/focused-rear/rear-attempt2.png; raw generation
drift was compensated by explicit uniform display registration in v8 policy.
Look-back/tucked fur and original jump torso anatomy remain unfinished; do not
claim complete style consistency. Groom duplicate-floor-paw patch remains in v8.
Current metadata/checks: planning/current-test-build.json and style-recovery-build-v8.json.
No desktop controls or visible/native UI tests. Owner closes old apps manually.

LATEST STYLE WORK - 2026-11-07: Owner wants ORIGINAL sit/groom/stretch material,
shape, color pattern, fine irregular straight fur, modest paws, and correct dark
missing-eye socket across every image. Current head turns were misregistered with
the bloated redrawn bodies. Broad style-transfer drafts made rear view FATTER;
owner objected. Those drafts are REJECTED, never approved or integrated. Do not
lock bloated draft silhouettes as invariants or promote another broad retexture.
The original sitting PNG remains byte-exact. No CUA/native UI/desktop control.

Separate recovery candidate: test-exes/Odin-Dev-StyleRecovery-v7.exe. Explicit
DEV pack policy planning/style-recovery-v7.json restores all73 ORIGINAL matching
rest/gaze/eye/tail frames (45 actual original gaze frames, zero generated neck/head
layers), plus32 original jumps. Original183 source PNGs unchanged. Grooming16frames
use only a local duplicate-grounded-paw correction; original pixels outside that
small mask remain exact. The paw correction is a visual-test candidate, not owner
approved. Original stretch/run art unchanged. V6EXE remains unchanged as fallback.
Code/source/geometry checks recorded in planning/style-recovery-build-v7.json.

THIS IS NOT THE COMPLETED STYLE CORRECTION. Six extra stills5/6/7/8/10/11 retain
their older checkpoints, explicitly labeled pending. Original midflight jumps
still have a fairly straight tubular torso; restoration removes later style drift
but is not a new waist/anatomy fix. Original sleep belly/floor contact needs visual
review. Focused rear/anatomy proofs remain separate and rejected unless explicitly
accepted. See planning/style-consistency-audit-v7.json and candidate provenance.
Do not silently treat original source restoration as completion of all requests.

CURRENT OWNER GATE: review all pose/action states and current head/facial sizing.
planning/state-head-review.json and tools/local/Build-StateHeadReview.py produce
16-state body sheets plus whole-head and facial lineups. Preserve existing per-state
scales so mismatches stay visible. Head views use one common2x viewing zoom;
selection rectangles are visual crops, not approved anatomical measurements.
Owner supplied numbered feedback: apply sizing changes #1,5,6,7,8,10,11,13,16,17;
correct flat contact #6-8 and photo-based jump fur #16-17. Development-only test
integration is now authorized and implemented; preserve the distinction between
test candidates and owner-approved artwork. No installation/release is authorized.

BREATHING IS REMOVED ENTIRELY at the owner's request, including runtime behavior,
review controls, scheduling, deformation, and animation-only legacy review mode.
Do not reintroduce it. Retired source is in local-data/retired/breathing-v3 and is
excluded from builds. Other gaze, blink, tail, grooming, running and jumping actions
remain intact. Walking and3D are on hold; scratching is removed.

Approved contact bases: draft-assets/approved-resting-contact-v1 (sit/loaf/lay/sleep).
Historical belly fur/contact: draft-assets/approved-belly-contact-v2/belly.png; pose5 is removed.
Approved tucked: draft-assets/approved-tucked-sleep-v1/tucked.png.
Curled sleep is REMOVED as a redundant tucked duplicate. Its art is historical only,
with an archive in local-data/retired/curled-sleep. Never reintroduce it in reviews
or runtime. Tucked is retained; side-sleep fur approved in approved-side-sleep-v2,
with a further size decrease requested in numbered review v2.
Earlier side checkpoints are preserved in approved-side-sleep-v1. New artwork is not yet integrated into companion runtime.
Latest older-pose ear revisions: draft-assets/legacy-ear-tufts-v1; use these in the
review, including the sitting reference. Do not silently mark their approval scope.

Artwork: use actual approved Odin photos/video references for likeness, anatomy,
coat and texture. All10 photos in local-data/reference-review/fur-source-selection-v1/
reference-index.json are owner-approved. Supply actual photo references to image_gen;
generated drafts alone are insufficient. Match natural irregular hair grouping,
mostly straight fur, realistic leg joins; avoid repeated wavy locks, etched/
crosshatched/scratchy procedural fur or plush airbrush texture. Body/torso is
unstriped; stripes are on legs, with natural facial identity retained. Ear pinnae
are compact; only sparse separated wispy hairs extend beyond them, not solid spikes.
Keep whole ears, whiskers, paws and tail inside transparent margins.

MANDATORY FLOOR RULE: planning/resting-contact-policy.json. Body, belly, ruff,
tail and resting cheek share one horizontal support plane. Only paws may overhang.
Explicit exception: the existing sleeping pose and its tail variants may keep tail
overhang. Side-sleep support follows flattened cheek. Smoosh/bunch fur under weight;
never crop/cut/erase the underside. Anchor by support, not lowest paw pixel. Verify
on visible taskbar/window-edge previews. Preserve original/approved assets.

Use odin.ps1 for Build, Verify, Review and Package. ApprovedContact is a STATIC
16-state numbered latest-art review v10 with revised sizes, surfaces and manual
Open/Half/Closed eyelid choices on states1-3; no automatic animation or breathing.
Airborne samples have no floor. Runtime, dev app
and art review are separate products; never install/publish without user instruction.
Run meaningful checks after changes. Verify -SkipRegistryTest only if registry access
is blocked, and report the skip. Portable tools are in local-data/toolchain.

Reusable generic code belongs in ../shared/framework; Odin keeps explicitly synced
source snapshots and must build independently. Sync ../shared/tools/Sync-Framework.ps1
and verify consumers. Do not share pet artwork, identity, settings or release targets.
Preserve all media/history; do not commit toolchains, private photos/videos, archives,
or generated executables. No push/release is implied by local development.

SIZE REFERENCE: owner says look-away (#6 Sitting away) is the smallest head
on the scale. Latest numbered feedback explicitly enlarges #6, superseding the
earlier instruction to keep its display scale fixed. Do not normalize head crops.
Preserve review IDs: removed curled was#9; remaining IDs stay unchanged.

Latest approved side-sleep fur: draft-assets/approved-side-sleep-v2/side.png.
Numbered sizing changes and trial multipliers are recorded in
planning/numbered-sizing-feedback-v2.json; original v1 scale baseline is preserved.
Owner approved the reduced-shagginess jump fur with "pass":
draft-assets/numbered-sizing-v2/jump-left-fur-v2.png and jump-right-fur-v2.png.
This records jump fur approval; the complete revised sizing/contact sheet remains
in review. Keep originals and the rejected shaggier variants for provenance.

Owner requested continued progress. Six new blink drafts (half/closed for sit,
loaf, lay) are in draft-assets/blink-reconciliation-v1. Review and registration
data: planning/blink-reconciliation-v1.json. Owner approved all six with "pass".
The generated full-frame sources have slight silhouette drift, so preview v6
uses Odin-specific BlinkEyeRenderer.cs eyelid-only rendering on a fixed base.
All6 rendered frames passed exact outside-eyelid pixel and alpha equality checks.
Raw source images remain unchanged. Do not swap drifting full frames into runtime.
Original73 resting-family frames remain unchanged;67 have no new candidate yet.

Six gaze and six tail KEYFRAMES in draft-assets/gaze-reconciliation-v1 and
tail-reconciliation-v1 are owner-approved and registered against fixed bodies.
Six additional up/down gaze sources in vertical-gaze-reconciliation-v1 are test
candidates, not independently approved. Twelve actual diagonal heads in
draft-assets/diagonal-gaze-v4 now provide8directions/family (24totalheads).
The45logical gaze slots select the nearest actual head;29are approximate,
maximum22.5degrees. Do not claim45distinct new artworks. Current73-slot plan:
planning/family-registration-v1.json. Older registered-families-v1/v3 exports
are historical four-direction snapshots, not the current runtime renderer.
Head crossfades caused double faces and were rejected; do not reintroduce them.
Both full16-frame jump families now use the latest fur, compact muzzles, wispy
ears and clean edges. Sources: draft-assets/jump-family-v4; metadata and activation:
planning/jump-{left,right}-family-v4.json and action-art-integration-v4.json.
Accepted008samples are byte-identical. PerframeheadScaleCorrection is uniform
whole-sprite sizing, not pixel deformation; keep008factor1.0. Leftseated13-15
face metrics agree within0.6%, so do not mistake ruff/earshape for headscale.
Groom/stretch/run artwork remains original; reviewed size corrections remain.

TEST EXES: save all review/test executable deliverables under test-exes/ inside
Odin. Current review is test-exes/Odin-ApprovedContact-Local.exe; versioned test
copies also belong here, older binaries in test-exes/archive. Do not copy test
EXEs back into chat outputs. Runtime and installer build targets are unchanged.

OWNER INTERACTION RULE: do not take over the computer, use CUA, open visible
test windows, or run native UI/activation tests during this work. Work on files,
builds and headless checks only. Message the owner when ready for manual testing.
User explicitly requested parallel sessions; subagents are authorized for current
Odin completion work. G1-G6 and T1-T6 keyframe art approved with latest pass.
Registered review uses fixed-body head/tail layers. Headless v8 checks cover
18motionkeyframes/54layercases and6eyelids. v7's204UIcombinations passed before
the no-desktop-interaction instruction; do not run those UI tests again now.

READY FOR MANUAL TESTING: test-exes/Odin-Dev-Review-v4.exe is the current test app;
Odin-Art-Review-v4.exe is static review v10. All older owner apps were left
untouched; owner must close their old test app manually before opening this one.
Current paths/checks/hashes: planning/current-test-build.json. Right-click app for
Latest artwork stills (16), held resting states, and Actions in current state.
Latest native rest bases, eyes, tails, sizes/support and head tracking are in
the DEV companion only. Headless final QA passed732actual latest edge placements,
48head-point cases, registered layers, legacy checks and productioncompileguard.

Owner GO applied queued still edits in draft-assets/queued-review-v3, IDs1,4,8,
10,11,16,17 plus sitting-right gaze. Owner approved those with goodkeepgoing;
queued-review-v3/approval.json records exact scope/hashes. Persistent
v3feedback uses numbered-sizing-baseline-v2 and numbered-sizing-feedback-v3.json;
do not compound requested.92scale twice. Muzzle motion override lives in
planning/queued-art-edits-v3.json; retain original approved source metadata.
Sleep-tail mask now protects the filled belly from x595,y1125 onward, with a
tapered seam; preserve that protection. Side sleep has hidden rear feet, neutral
mouth and shorter fur; front paw retained. Jump17farleftforepaw barely visible.
Use versioned build-name options when a test EXE is locked; never close owner apps.

V4nativeactions are lazy-loaded with an8bitmap LRU to bound memory. NewDevArt
native frame data carries customrootX/support, mapped from original atlas512/620;
do not center actionframes on their changingfur boundingbox. Groundframes use
actual pawsole contact, airborneframes retain original bottom-clearance offsets.
DEVPerchVisibleMargins delegate now clamps windowlandings/heldreststates by actual
visible fur widths (allrestbases+incomingjump), keepingbody/tail on windowtop.
Production keepsNULLdelegate/original behavior. Do not reintroduce90pxinset forDEV.
HeadlessactualCatMindmotion captures usedall16frames/direction and verifiedlanding
insidewindowbounds. FinalQA:1116actualedge/supportcases,32source/timing/hashchecks,
90motionlayercases,6eyelids,48headpoints,originalbehavior+productioncompileguard.
Newjump/diagonalart remains forowner visualtesting. No windows were shown.

LATEST OWNER REVIEW: jump frames5-9 (source004-008) looked tubular, legs merged
with torso. Ten anatomy candidates in draft-assets/jump-anatomy-v5, both directions,
await visual review. Preserve v4 originals/accepted008; runtime still v4 unchanged.
planning/jump-anatomy-feedback-v5.json records request and source hashes.
Build-JumpAnatomyReview.py makes current/revised numbered comparison sheets.
Right candidates qa.json carries uniform reviewScaleCorrection;006needs0.726136
to preserve current head size after generation drift. Do not integrate rawscale.
If approved, map native landmarks/root/support and updated action008 independently
of approved still16/17; existing pack asserts008equals acceptedstill, so do not
remove guards or overwrite approvedstills silently. Review remains candidateonly.


LATEST V5: owner provisionally approved anatomy corrections with "approve for now".
All ten jump animation overrides are integrated in test-exes/Odin-Dev-Review-v5.exe.
Metadata: planning/jump-anatomy-integration-v5.json. The pack constructs and validates
v4 first, then applies explicit approved overrides, preserving baseline008 guards.
Action008 intentionally differs from unchanged staticstills16/17; no originals lost.
Pupil midpoint registration preserves world head/root/support and uniform head size.
Headless v5 checks passed:32frames,10overrides,22exactunchangedentries,1116edge/support
placements,90landmarkplacements,actualCatMindflightsall16indiceseach and validlandings.
Static review remains Odin-Art-Review-v4.exe. Current hashes: current-test-build.json.
No UI opened or owner apps changed. Next manualtest: fulljump transitions/landing.
Remaining original actionart: grooming,stretching,running. No new artwork approved
for those actions yet. Prior v5candidatepending notes above are superseded.

OWNER CLEANUP: removed22superseded testEXEs including archive and compiledQAhelpers.
Only test-exes/Odin-Dev-Review-v5.exe remains. Staticreviewv4EXE is deleted;
historicalQA reports/source remain. Do not recreate old testbuilds without need.

LATEST OWNER FEEDBACK: screenshot reports sitting Odin became too puffy.
The sitting redraw has a wider/rounder ruff than original art/frames/sit/000.png.
A slimmer sitting candidate is draft-assets/sitting-slim-v6/sit-front.png; record:
planning/sitting-puffiness-feedback-v6.json and draft-assets/sitting-slim-v6/generation.json.
It is NOT approved/integrated. Current runtime v5 is unchanged. Preserve corrected
muzzle/ear tufts, current head size, floor contact, moderate natural fine fur.
Candidate generated1254square with alpha8bounds286,565,814,1115; proposed support1103.
It grew slightly taller despite slimmerruff; owner must assess before any runtime swap.


OWNER CONFIRMED: match RIGHTcat in puffinessscreenshot; originalruntimebody is
the reference. Additionalcomplaint: roundedfur/texture recurring. Preserve sharp
mostlystraight finehair/taperedtips and localcontrast, no soft roundedlocks/clumps.
Latest candidate draft-assets/sitting-slim-v7/sit-front.png supersedes v6candidate.
Built-inimagegen used originalruntime as edit target and actualphotos; v3puffy
redraw only secondarymuzzle/earreference. V7 remains UNAPPROVED, notinEXE.

LATEST V6 RESTORATION: owner said "no dude.. you had the original fix it".
STOP further sitting redraws. Exact ORIGINAL art/frames/sit/000.png is now static
sittingbase (native1024;shaab5c4261984e4f252172962198c2d532dffc8b8d269cfe23dd06d000808e7c43).
planning/sitting-original-restore-v6.json is a persistentfinaloverride aftersizing.
Originalfinefur/shape restored, no generatedsource edits. Rejected v6/v7/v8drafts
remain provenance only. Current soleEXE: test-exes/Odin-Dev-Review-v6.exe; v5deleted.
Sittingdraftlayers1254 mapinmemory to1024; exactbasepixels outside intendedmasks.
Existinggazemaskincludes upperruff/neck: do notclaim entirecoat unchanged whilegazing.
Other15stills/all32jumpentries andsources remainunchanged. V6headless QAallpassed.
Currentbuild paths/checks/hashes: planning/current-test-build.json. No UIwasopened.
