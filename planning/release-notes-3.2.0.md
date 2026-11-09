# Odin 3.2.0

Odin chooses his own behavior, as before: four resting states (Sitting, Loaf, Laying down awake, Sleeping), each holding its pose while gaze, blinks and tail flicks happen on their own. This release carries the artwork and motion work reviewed since 3.1.2 as it was passed on the desktop.

| Feature | Frames |
| --- | ---: |
| Sitting gaze (now on the floor line like the still) | 19 |
| Loaf gaze | 12 |
| Awake laying gaze | 14 |
| Eye expressions for each awake resting state | 3 |
| Running left (18 originals + 3 in-betweens) | 21 |
| Running right (16 originals + 1 in-between) | 17 |
| Jumping left | 16 |
| Jumping right | 16 |
| Grooming | 16 |

- The floor is Odin's body: his underside sits on the taskbar's top edge, and his toes hang over it, drawn on top of the taskbar. When Windows raises the taskbar over him he moves back above it.
- Looking around no longer lifts him: every sitting gaze frame carries the still's own underside and paws, from the original pixels.
- The resting states and animations are sized by the owner-passed table, so grooming, stretching, running and jumping match the sitting cat.
- Running is registered by the nose, one frame per 16 ms tick, with in-between frames where they pass a ghosting check; zoomies dash at 1000 px/s.
- Jumps draw the running frames in flight and tilt along the arc, nose up rising and down falling.
- Perch and zoomies can start from any awake state; never from sleep.
- Nothing new in the state list: the sitting-away, look-back and side views and the clock looks are held back for a later version. Breathing stays removed.

Download size grows to about 82 MB: the release embeds both the original atlas and the promoted artwork pack.
