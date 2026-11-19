"""Inspect green-iris landmarks in registered transition keys (analysis only)."""
import json
import sys
from pathlib import Path
import cv2
import numpy as np

folder = Path(sys.argv[1])
config = json.loads((folder / 'keys.json').read_text(encoding='utf-8-sig'))
landmarks = {}
for seq in config['sequences']:
    points = []
    for i in range(len(seq['keys'])):
        im = cv2.imread(str(folder / f"placed/{seq['name']}-{i}.png"), cv2.IMREAD_UNCHANGED)
        hsv = cv2.cvtColor(im[..., :3], cv2.COLOR_BGR2HSV)
        mask = ((hsv[..., 0] > 20) & (hsv[..., 0] < 80) & (hsv[..., 1] > 70) & (im[..., 3] > 200)).astype(np.uint8)
        mask[:500] = 0; mask[880:] = 0; mask[:, :400] = 0; mask[:, 720:] = 0
        n, labels, stats, centers = cv2.connectedComponentsWithStats(mask)
        candidates = [j for j in range(1, n) if 15 < stats[j, 4] < 700]
        if not candidates:
            raise RuntimeError(f"No iris in {seq['name']}/{i}")
        iris = max(candidates, key=lambda j: stats[j, 4])
        points.append(centers[iris].tolist())
    landmarks[seq['name']] = points
(folder / 'iris-landmarks.json').write_text(json.dumps(landmarks, indent=2))
print(json.dumps(landmarks))
