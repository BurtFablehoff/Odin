"""Assemble only clone-contained production build inputs; no authoring toolchain."""
from pathlib import Path
import json,shutil
root=Path(__file__).resolve().parents[2];dest=root/'build/release-3.3.0/source-check'
assert not dest.exists(),'Preserve prior source check; choose a new destination for a rerun.'
index=json.loads((root/'art/latest/index.json').read_text())
files={'build.ps1','build-assets.ps1','art/manifest.json','art/latest/manifest.json','art/latest/index.json','tools/local/Pack-LatestArt.ps1','installer/Odin.iss','README.md'}
files.update(p.relative_to(root).as_posix() for p in (root/'app').rglob('*') if p.is_file())
files.update(p.relative_to(root).as_posix() for p in (root/'art/frames').rglob('*.png'))
files.update(e['from'] for e in index['entries'])
for name in sorted(files):
 source=root/name;target=dest/name;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,target)
(dest.parent/'source-inputs.json').write_text(json.dumps(sorted(files),indent=2)+'\n')
print(f'Isolated {len(files)} production source/art inputs. No local-data or parent workspace dependencies.')
