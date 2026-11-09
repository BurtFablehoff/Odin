"""Part 5a (Hatt 2026-11-08: "okay lets push this one to release"; rule A9): promote the passed DEV pack into tracked production art.

Reads the DEV pack zip that the passed build embeds (Odin-Dev-v12-3f.exe; pack sha256 315ea481...) and writes art/latest/:
  manifest.json      the pack manifest, byte for byte (the loader hash-checks every entry against it)
  index.json         one row per pack entry: key, sha256, bytes, and `from`, the tracked file the packer reads
  <key>.png          only the payloads that differ from every tracked source (the v12-3f floor-fixed look_sit frames) and the stills
Payloads byte-identical to a tracked file (art/frames/** or draft-assets/**) are not duplicated: `from` names that file.
tools/local/Pack-LatestArt.ps1 rebuilds the identical entry set from index.json with PowerShell alone, so a GitHub clone builds
production without the Python toolchain. Nothing under art/frames or art/manifest.json is touched."""
import hashlib, io, json, os, sys, zipfile

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
PACK = os.path.join(ROOT, 'local-data', 'reference-review', 'dev-latest-art', 'dev-artwork.zip')
EXPECTED_PACK_SHA = '315ea4814428631dd4a01903c6e2ee7e6f87baec911c8f4fd95028630cf53913'   # the pack inside Odin-Dev-v12-3f.exe (Hatt passed it 2026-11-08)
OUT = os.path.join(ROOT, 'art', 'latest')


def sha(data):
    return hashlib.sha256(data).hexdigest()


def main():
    pack_bytes = open(PACK, 'rb').read()
    pack_sha = sha(pack_bytes)
    if pack_sha != EXPECTED_PACK_SHA:
        sys.exit('The DEV pack on disk (%s...) is not the passed v12-3f pack (%s...). Rebuild it at the ship commit or stop.' % (pack_sha[:8], EXPECTED_PACK_SHA[:8]))
    zf = zipfile.ZipFile(io.BytesIO(pack_bytes))
    manifest_bytes = zf.read('manifest.json')
    manifest = json.loads(manifest_bytes)
    sources = {}
    for group in ('poses', 'blinks', 'motions', 'actions'):
        for entry in manifest.get(group) or []:
            sources[entry['key']] = entry
    os.makedirs(OUT, exist_ok=True)
    rows, written, shared = [], [], []
    for name in zf.namelist():
        if name == 'manifest.json':
            continue
        data = zf.read(name)
        digest = sha(data)
        entry = sources.get(name)
        if entry is not None and entry.get('sha256') not in (None, digest):
            sys.exit('Pack entry %s does not match its manifest sha256.' % name)
        source = (entry or {}).get('source')
        source_path = os.path.join(ROOT, source.replace('/', os.sep)) if source else None
        still = name.startswith('state-')
        if not still and source_path and os.path.isfile(source_path) and open(source_path, 'rb').read() == data:
            frm = source
            shared.append(name)
        else:
            with open(os.path.join(OUT, name), 'wb') as f:
                f.write(data)
            frm = 'art/latest/' + name
            written.append(name)
        rows.append({'key': name, 'sha256': digest, 'bytes': len(data), 'from': frm})
    with open(os.path.join(OUT, 'manifest.json'), 'wb') as f:
        f.write(manifest_bytes)
    index = {
        'purpose': 'Odin.LatestArt pack: the owner-passed DEV artwork promoted into production (rule A9). tools/local/Pack-LatestArt.ps1 zips these entries in this order.',
        'promotedFrom': {'build': 'test-exes/Odin-Dev-v12-3f.exe', 'packSha256': pack_sha, 'date': '2026-11-08', 'ownerWords': ['Yes, on the line', 'okay lets push this one to release']},
        'manifest': 'art/latest/manifest.json', 'manifestSha256': sha(manifest_bytes),
        'entries': rows,
        'counts': {'entries': len(rows), 'storedHere': len(written), 'fromTrackedSources': len(shared)},
    }
    with open(os.path.join(OUT, 'index.json'), 'w', encoding='utf-8', newline='\n') as f:
        json.dump(index, f, indent=1)
        f.write('\n')
    # Verify: every `from` exists with the recorded sha.
    for row in rows:
        path = os.path.join(ROOT, row['from'].replace('/', os.sep))
        if not os.path.isfile(path) or sha(open(path, 'rb').read()) != row['sha256']:
            sys.exit('Index row cannot be rebuilt: ' + row['key'])
    print('Promoted %d entries: %d stored in art/latest (%s), %d read from tracked sources.' % (len(rows), len(written), ', '.join(sorted(set(n.split('-')[0] + ('-' + n.split('-')[1] if n.startswith('action') else '') for n in written))), len(shared)))


if __name__ == '__main__':
    main()
