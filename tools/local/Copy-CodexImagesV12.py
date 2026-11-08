"""Copy images Codex left in ~/.codex/generated_images (newer than a marker time) into a job folder.
Usage: Copy-CodexImagesV12.py <job> <marker-file>
Never deletes or moves the originals. Writes <job>/copied-from-generated-images.json.
"""
import json
import os
import shutil
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
GEN = os.path.join(os.path.expanduser('~'), '.codex', 'generated_images')


def main():
    job, marker = sys.argv[1], sys.argv[2]
    since = os.path.getmtime(marker)
    folder = os.path.join(ROOT, 'draft-assets', 'codex-v12', job)
    copied = []
    if os.path.isdir(GEN):
        for name in sorted(os.listdir(GEN)):
            src = os.path.join(GEN, name)
            if os.path.isfile(src) and os.path.getmtime(src) >= since:
                dst = os.path.join(folder, 'from-generated-images', name)
                os.makedirs(os.path.dirname(dst), exist_ok=True)
                shutil.copy2(src, dst)
                copied.append(name)
    with open(os.path.join(folder, 'copied-from-generated-images.json'), 'w', encoding='utf-8') as f:
        json.dump({'source': GEN, 'sinceMarker': marker, 'copied': copied}, f, indent=1)
    print(json.dumps({'copied': copied}))


if __name__ == '__main__':
    main()
