# V11 drawings — owner EXE motion passed

Owner response: "pass" (2026-11-18), directly after the V11 EXE handoff. A24 records
the approved sit/loaf, sit/lay and direct loaf/lay motion. Exact binary, pack,
authoring evidence and reports are frozen in `../approved-transition-motion-v11/`.
This supersedes the earlier internal assessment below for the reviewed motion.
The canvas-boundary check remains recorded; production promotion is not requested.

Earlier handoff (A23, 2026-11-18): Hatt explicitly requested the live EXE because motion is
the test. `test-exes/Odin-Dev-Motion-v11.exe` packages the sheet-only sequence below
for owner review. Previous internal findings remain recorded; they do not count
as the owner's verdict on playback. No visual approval or production promotion.
Rebuild/check using `tools/local/Build-CoherentMotionReview.ps1`.

The remaining text records the internal investigation before that request:

Continuation of A22 on 2026-11-18 (owner: "go"). All three V9 routes remain
rejected. None of this artwork is approved or promoted. No V11 EXE exists.

Built-in imagegen produced three six-pose sheets, one revision of the sit/loaf
sheet and two single-pose bridge attempts. Exact prompts and output provenance
are in `prompts.json`; original tool outputs are retained under `raw/`.
`references/` contains diagnostic composites of the original held endpoints.

The sheet approach improved within-sheet material consistency but did not solve
motion staging: the early drawings remain upright too long, leaving a large
drop to the next row's first low pose. Whole-sheet generation is therefore not
a demonstrated solution. The two subsequent bridge edits changed posture by
the wrong amount; neither is selected.

`Extract-CoherentSheets.cs` only crops discrete drawn cells. It locates row-specific
column separators because nominal equal grid cells cut into neighboring drawings.
The extracted cells and alpha edges still need visual scrutiny; a grid prompt is
not proof that every cell contains an intact sprite.

`Build-CoherentMotionPack.py` creates an internal sheet-only experiment with original
held endpoints preserved. It performs whole-drawing scale/placement in metadata
and never synthesizes intermediate pixels. Anatomical forehead matches are
diagnostic and do not establish consistent cheek/chest volume. The earlier mixed
sheet/old-bridge experiment is retained separately as `mixed-rejected.*` in build.

Latest displayed measurements: sit/loaf's largest head step is about 27 px,
sit/lay's about 40 px, direct loaf/lay's about 19 px at default size. The sit/lay
step is worse than V9. These are failures, not accepted thresholds or approval.

No runtime/build flags were added for this pack. Do not hand it off as a repair.
