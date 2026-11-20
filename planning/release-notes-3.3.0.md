Odin 3.3.0 adds the owner-passed drawn transitions between sitting, loaf and awake
lying. Loaf and lying change directly without sitting up first. Each direction
uses five intermediate drawings and the reviewed .315-second timing.

Production rendering was compared with the frozen V11 review: all 42 transition
frames match pixel-for-pixel across 252 size/background cases. Transparent storage
margins preserve the reviewed image pixels and placement while satisfying the
frame-edge packaging check.

V5 action integration adds the passed stable grooming loop with a complete 2–4
second duration, matching paw-entry/return, the corrected stretch ending and
sit/loaf right-border cleanup. The production source reproduces the approved V5
pack exactly. Earlier draft release artifacts predate this source update and
must be rebuilt before a release of these fixes.

Existing released tail motion remains unchanged; the separate tail correction
is still queued. No new sleeping transition artwork is included.

Downloads: `Odin.exe` (portable) and `Odin-Setup-3.3.0.exe` (per-user installer).
