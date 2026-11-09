"""V12 3bJ: make a Codex image-job folder from its brief. Usage: Prepare-CodexJobV12.py <job>
Creates draft-assets/codex-v12/<job>/ with prompt.md (copy of planning/art-briefs-v12/<job>.md), refs.txt (copy of <job>.refs.txt),
start.marker (its mtime is the copy cut-off for Copy-CodexImagesV12.py) and launch.txt holding the exact codex command to run
(printed too). Checks every attachment exists. Never deletes anything; an existing folder is left as is apart from these files.
"""
import os
import shutil
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CODEX = 'C:/Users/hattd/AppData/Local/OpenAI/Codex/bin/f71a721e0e6193a9/codex.exe'


def main():
    job = sys.argv[1]
    briefs = os.path.join(ROOT, 'planning', 'art-briefs-v12')
    folder = os.path.join(ROOT, 'draft-assets', 'codex-v12', job)
    os.makedirs(folder, exist_ok=True)
    shutil.copyfile(os.path.join(briefs, job + '.md'), os.path.join(folder, 'prompt.md'))
    shutil.copyfile(os.path.join(briefs, job + '.refs.txt'), os.path.join(folder, 'refs.txt'))
    refs = [r.strip() for r in open(os.path.join(folder, 'refs.txt'), encoding='utf-8') if r.strip()]
    for r in refs:
        assert os.path.exists(os.path.join(ROOT, r)), 'missing attachment ' + r
    assert len(refs) <= 5, 'imagegen takes at most 5 reference images'
    with open(os.path.join(folder, 'start.marker'), 'w') as f:
        f.write('started\n')
    rel = 'draft-assets/codex-v12/' + job
    cmd = ('%s exec --ignore-user-config -m gpt-6.1-sol -c model_reasoning_effort="high" -s workspace-write -C C:/_repo/pet/Odin/%s --json '
           '-o %s/last-message.md %s < planning/art-briefs-v12/%s.md > %s/codex-log.jsonl 2> %s/codex-stderr.txt'
           % (CODEX, rel, rel, ' '.join('-i ' + r for r in refs), job, rel, rel))
    with open(os.path.join(folder, 'launch.txt'), 'w', encoding='utf-8', newline='\n') as f:
        f.write(cmd + '\n')
    print(cmd)


if __name__ == '__main__':
    main()
