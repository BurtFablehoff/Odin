# Odin local workflow

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
CURRENT V12-2 - 2026-11-08 (branch koa/v12-2-run, main tree): test-exes/Odin-Dev-v12-2.exe, awaiting G2. Running is registered by
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
