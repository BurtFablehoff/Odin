"""Give Codex's returned images the names the brief asked for.
Usage: Name-CodexReturnsV12.py <job>
Reads draft-assets/codex-v12/<job>/last-message.md, where Codex lists "[name.png](...generated_images/<thread>/<exec>.png)",
and copies draft-assets/codex-v12/<job>/from-generated-images/<thread>__<exec>.png to <job>/<name.png>.
Writes <job>/name-map.json. Never deletes anything.
"""
import json
import os
import re
import shutil
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def main():
    job = sys.argv[1]
    folder = os.path.join(ROOT, 'draft-assets', 'codex-v12', job)
    text = open(os.path.join(folder, 'last-message.md'), encoding='utf-8', errors='replace').read()
    pairs = re.findall(r'\[([\w.-]+\.png)\]\([^)]*generated_images/([0-9a-f-]+)/([\w-]+)\.png\)', text)
    # Explicit mapping when Codex's last message names nothing: name.png=<exec-id-prefix> ... (matched against the copies).
    if len(sys.argv) > 2:
        copies = os.listdir(os.path.join(folder, 'from-generated-images'))
        pairs = []
        for arg in sys.argv[2:]:
            name, prefix = arg.split('=', 1)
            hits = [c for c in copies if ('__' + prefix) in c]
            if len(hits) != 1:
                sys.exit('ambiguous or missing exec id %s: %s' % (prefix, hits))
            thread, call = hits[0][:-4].split('__', 1)
            pairs.append((name, thread, call))
    mapping, missing = {}, []
    for name, thread, call in pairs:
        src = os.path.join(folder, 'from-generated-images', '%s__%s.png' % (thread, call))
        if os.path.exists(src):
            shutil.copy2(src, os.path.join(folder, name))
            mapping[name] = os.path.relpath(src, folder).replace('\\', '/')
        else:
            missing.append((name, src))
    with open(os.path.join(folder, 'name-map.json'), 'w', encoding='utf-8') as f:
        json.dump({'named': mapping, 'missing': missing}, f, indent=1)
    print(json.dumps({'named': sorted(mapping), 'missing': [m[0] for m in missing]}))
    sys.exit(0 if mapping and not missing else 1)


if __name__ == '__main__':
    main()
