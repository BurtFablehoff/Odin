"""V12 3R: summarise --test-body-stability and --test-follow-mouse reports for the head-turn families 6/7/8.

Usage: Summarize-HeadTurnTestsV12.py <body-stability.json> <follow-mouse.json> <out.json>
Keeps the full reports inside the output (the before-file proves the new checks fail without the head-turn frames)."""
import json
import sys
from pathlib import Path

FAMILIES = ['sit', 'loaf', 'lay', 'rear', 'lookback', 'side']


def load(path):
    text = Path(path).read_text(encoding='utf-8-sig')
    return json.loads(text)


def main():
    body, follow = load(sys.argv[1]), load(sys.argv[2])
    summary = {'bodyStability': {'passed': body.get('passed'), 'failures': body.get('failures'),
                                 'headTurnFamilies': body.get('headTurnFamilies'), 'slotCount': body.get('slotCount'),
                                 'maxDriftPx': {k: v.get('maxDriftPx') for k, v in (body.get('families') or {}).items()}},
               'followMouse': {'passed': follow.get('passed'), 'failedFamilies': follow.get('failedFamilies'), 'families': {}}}
    for fam in FAMILIES:
        sizes = (follow.get('families') or {}).get(fam)
        if sizes is None:
            summary['followMouse']['families'][fam] = 'not covered'
            continue
        summary['followMouse']['families'][fam] = {size: {'passed': v['passed'], 'distinctFrames': v['distinctFrames'],
                                                          'frames': {p: ' '.join(str(x) for x in r['frames']) for p, r in v['paths'].items()},
                                                          'reason': v['reason'][:300]} for size, v in sizes.items()}
    out = {'summary': summary, 'bodyStabilityReport': body, 'followMouseReport': follow}
    Path(sys.argv[3]).write_text(json.dumps(out, indent=1) + '\n')
    print(json.dumps(summary, indent=1)[:5000])


if __name__ == '__main__':
    main()
