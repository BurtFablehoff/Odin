"""V12 2b: one-line-per-tween summary of draft-assets/run-tweens-v12/ghost-gate.json (kept / dropped and why)."""
import json
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
gate = json.loads((ROOT / 'draft-assets/run-tweens-v12/ghost-gate.json').read_text())
for clip, entry in gate['clips'].items():
    semi = sum(any(r.startswith('semi') for r in d['reasons']) for d in entry['dropped'])
    ghost = sum(any(r.startswith('flow') for r in d['reasons']) for d in entry['dropped'])
    both = sum(len(d['reasons']) == 2 for d in entry['dropped'])
    print(f"{clip}: kept {len(entry['kept'])} {[k['name'] for k in entry['kept']]}; dropped {len(entry['dropped'])}: "
          f"ghost-edge {ghost}, semi-count {semi}, both {both}; play order {entry['playOrderFrames']} frames")
    print('  ghost px of drops:', sorted(d['ghostPixels'] for d in entry['dropped']))
