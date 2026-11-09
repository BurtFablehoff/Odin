"""Exclude retired poses from legacy review inputs while retaining source archives."""
from pathlib import Path
import argparse,json,zipfile

parser=argparse.ArgumentParser()
parser.add_argument('source',type=Path)
parser.add_argument('destination',type=Path)
args=parser.parse_args()
with zipfile.ZipFile(args.source) as source:
    manifest=json.loads(source.read('manifest.json'))
    manifest['poses']=[p for p in manifest['poses']
                       if not any(term in (p.get('key','')+' '+p.get('label','')).lower()
                                  for term in ('curled','belly','on back','on his back','side sleep','side-sleep','side_sleep','sleep-side','sleeping on side'))]
    if len(manifest['poses'])<2:
        raise ValueError('This review contains only retired content; use ApprovedContact.')
    needed={p['key'] for p in manifest['poses']}
    for pose in manifest['poses']:
        needed.update(pose.get('frames') or [])
        if pose.get('referenceKey'):needed.add(pose['referenceKey'])
    args.destination.parent.mkdir(parents=True,exist_ok=True)
    with zipfile.ZipFile(args.destination,'w',zipfile.ZIP_DEFLATED) as target:
        target.writestr('manifest.json',json.dumps(manifest))
        for key in sorted(needed):target.writestr(key,source.read(key))
print('Prepared active review: retired curled sleep, side sleep and belly-up excluded.')
