"""Estimate motion fields only; C# authoring renders the candidate PNGs."""
import json
import struct
import sys
from pathlib import Path
import cv2
import numpy as np

folder = Path(sys.argv[1])
config = json.loads((folder / 'keys.json').read_text(encoding='utf-8-sig'))
landmarks = json.loads((folder / 'iris-landmarks.json').read_text())
gx, gy = np.meshgrid(np.arange(1024, dtype=np.float32), np.arange(1024, dtype=np.float32))
def smooth(x):
    x = np.clip(x, 0, 1)
    return x*x*(3-2*x)
def prior(start, end):
    weight = smooth((gx-360)/70) * (1-smooth((gy-(start[1]+70))/max(30, 944-(start[1]+70))))
    return np.stack(((end[0]-start[0])*weight, (end[1]-start[1])*weight), axis=2).astype(np.float32)
report = {}
for seq in config['sequences']:
    name = seq['name']
    distances = []
    for i in range(len(seq['keys']) - 1):
        a, b = [cv2.imread(str(folder / f'placed/{name}-{k}.png'), cv2.IMREAD_UNCHANGED) for k in (i, i + 1)]
        masks = [im[..., 3] > 128 for im in (a, b)]
        gray = [cv2.cvtColor(np.clip(im[..., :3].astype(np.float32) * im[..., 3:] / 255, 0, 255).astype(np.uint8), cv2.COLOR_BGR2GRAY) for im in (a, b)]
        flows = []
        points = landmarks[name]
        for source, target, start, end in ((gray[0], gray[1], points[i], points[i+1]), (gray[1], gray[0], points[i+1], points[i])):
            forward = prior(start, end)
            inverse = prior(end, start)
            aligned = cv2.remap(source, gx+inverse[..., 0], gy+inverse[..., 1], cv2.INTER_LINEAR, borderMode=cv2.BORDER_CONSTANT)
            dis = cv2.DISOpticalFlow_create(cv2.DISOPTICAL_FLOW_PRESET_MEDIUM)
            dis.setFinestScale(0)
            residual = dis.calc(aligned, target, None)
            carried = cv2.remap(residual, gx+forward[..., 0], gy+forward[..., 1], cv2.INTER_LINEAR, borderMode=cv2.BORDER_CONSTANT)
            flow = forward + carried
            flows.append(flow)
        for direction, flow in zip(('ab', 'ba'), flows):
            with (folder / f'{name}-{i}-{direction}.flow').open('wb') as output:
                output.write(struct.pack('<ii', 1024, 1024))
                output.write(flow.astype('<f4').tobytes())
        moving = masks[0].copy(); moving[:, :390] = False
        distances.append(max(2.0, float(np.percentile(np.linalg.norm(flows[0], axis=2)[moving], 80))))
    total = max(20, round(seq['seconds'] * 24))
    weights = np.array(distances) / sum(distances)
    edges = np.rint(np.concatenate(([0], np.cumsum(weights))) * total).astype(int)
    steps = np.maximum(1, np.diff(edges)).tolist()
    (folder / f'{name}-steps.json').write_text(json.dumps(steps))
    report[name] = dict(distances=distances, steps=steps, frames=sum(steps)+1)
(folder / 'flow-report.json').write_text(json.dumps(report, indent=2))
print(json.dumps(report))
