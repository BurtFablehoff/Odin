"""V12 3bJ: watch for computer-use while a Codex image job runs. Usage: Monitor-CodexCuaV12.py <job> [max-seconds=1500]
Takes a tasklist baseline at start (draft-assets/codex-v12/<job>/baseline-tasklist.txt), then polls `tasklist` every 5 s until
<job>/last-message.md exists (the job finished) or the cap is reached, writing every poll to <job>/tasklist-monitor.txt.
Exit 1 if any NEW codex-computer-use* process appears that was not in the baseline (Hatt's own Codex desktop app may already be
running: its PID is in the baseline and does not count). Exit 0 otherwise. Reads only; kills nothing.
"""
import csv
import io
import os
import subprocess
import sys
import time

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def tasklist():
    out = subprocess.run(['tasklist', '/FO', 'CSV', '/NH'], capture_output=True, text=True).stdout
    rows = list(csv.reader(io.StringIO(out)))
    return {(r[0], r[1]) for r in rows if len(r) >= 2}


def cua(procs):
    return {p for p in procs if p[0].lower().startswith('codex-computer-use')}


def main():
    job = sys.argv[1]
    cap = float(sys.argv[2]) if len(sys.argv) > 2 else 1500
    folder = os.path.join(ROOT, 'draft-assets', 'codex-v12', job)
    os.makedirs(folder, exist_ok=True)
    base = tasklist()
    with open(os.path.join(folder, 'baseline-tasklist.txt'), 'w', encoding='utf-8') as f:
        f.write('\n'.join(sorted('%s %s' % p for p in base if 'codex' in p[0].lower() or 'odin' in p[0].lower())) + '\n')
    done = os.path.join(folder, 'last-message.md')
    start = time.time()
    new_cua = set()
    with open(os.path.join(folder, 'tasklist-monitor.txt'), 'w', encoding='utf-8') as log:
        log.write('baseline codex-computer-use: %s\n' % sorted(cua(base)))
        while time.time() - start < cap:
            now = tasklist()
            extra = cua(now) - cua(base)
            log.write('%6.0f s  codex procs: %s  NEW cua: %s\n' % (time.time() - start,
                      sorted(p for p in now if 'codex' in p[0].lower()), sorted(extra)))
            log.flush()
            new_cua |= extra
            if os.path.exists(done):
                time.sleep(5)
                break
            time.sleep(5)
        log.write('finished after %.0f s; new computer-use processes: %s\n' % (time.time() - start, sorted(new_cua)))
    print('monitor', job, 'NEW computer-use:', sorted(new_cua) or 'none')
    return 1 if new_cua else 0


if __name__ == '__main__':
    sys.exit(main())
