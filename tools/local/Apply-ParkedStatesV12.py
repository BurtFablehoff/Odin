"""V12 3d DEV: park states 6 (sitting away), 7 (sitting look-back) and 8 (sitting side) out of the DEV pack for this version.

Hatt at G3b, 2026-11-08: "let take those 3 new sits out for this version" (after "I didn't want any of the new 12 oclock
looks for the 3 new states... they're broken"). The three poses, their payloads and any action/blink/motion entries that
belong to them leave the pack, so the DEV companion has no still, menu item, hold or test case for them; IDs are never
renumbered (AGENTS: preserve review IDs). Their art, sizes (planning/sizes-owner-v12.json), clock frames, briefs and code
stay in the tree for a later version; the static ApprovedContact review pack is not touched by this step.
manifest.parkedStatesV12 records what left and why; app/DevArtwork.cs fails if a parked id comes back, and the DEV menu
listing, perch/zoomies and follow-mouse tests skip parked ids. Runs last in Build-DevArtworkPack.py."""
import hashlib

PARKED = {6: 'sitting away (rear)', 7: 'sitting look-back', 8: 'sitting side'}
REASON = ("Hatt at G3b, 2026-11-08: 'let take those 3 new sits out for this version'; the three seated views are parked "
          "(not retired) with their IDs kept; art, sizes, clock frames and code stay in the tree for a later version")


def apply_pack(manifest, payloads):
    removed = {}
    for section in ('poses', 'blinks', 'motions', 'actions'):
        kept = []
        for entry in manifest.get(section) or []:
            if entry['id'] in PARKED:
                data = payloads.pop(entry['key'], None)
                removed.setdefault(str(entry['id']), []).append({
                    'section': section, 'key': entry['key'], 'source': entry.get('source'),
                    'sha256': hashlib.sha256(data).hexdigest() if data is not None else entry.get('sha256')})
            else:
                kept.append(entry)
        manifest[section] = kept
    # A clip whose frames all belonged to a parked state leaves with them.
    manifest['actionClips'] = [c for c in manifest.get('actionClips') or []
                               if any(a['clip'] == c['clip'] for a in manifest['actions'])]
    present = sorted(p['id'] for p in manifest['poses'])
    assert not any(pid in PARKED for pid in present), present
    manifest['parkedStatesV12'] = {'ids': sorted(PARKED), 'names': {str(k): v for k, v in PARKED.items()}, 'reason': REASON,
                                   'removed': removed, 'activePoseIds': present, 'ownerApproved': False}
    print('Parked states v12-3d:', sorted(PARKED), '; active poses', present)
    return manifest, payloads
