# Odin on this PC

`C:\_repo\pet\Odin` is the working checkout. Build, edit, and review here. Nothing needs
to be copied to OneDrive, exported to another machine, or repackaged for transfer.

## Everyday commands

Run in Windows PowerShell from the project folder:

```powershell
.\odin.ps1 Build
.\odin.ps1 Review -Open
.\odin.ps1 Verify
.\odin.ps1 Package
```

`Build` creates a development companion. `Review` creates the current curled-sleep
artwork review; other choices are `-Review Sitting`, `Sleeping`, `RestSizing`, and
`QuietBreathing`. Artwork approval is still pending for the curled-sleep pilot.

`Verify` builds and runs the runtime self-test, quiet-rest check, screen-edge check,
and review self-test. Reports are written to `build/local-*.json`. A restricted agent
sandbox may prohibit the self-test's temporary registry writes: use
`./odin.ps1 Verify -SkipRegistryTest` there. The JSON explicitly records that skip.
Run the normal command from your own PowerShell to verify Windows startup too.

`Package` builds the main-channel EXE and installer locally. It does not install or
publish either. A development build never registers itself for Windows startup.
Review artwork is not automatically integrated into the runtime.

## Local tools

The existing Windows .NET Framework compiler builds the C# app. Portable tools live
under `local-data/toolchain`: Python 3.13.17, Pillow, NumPy, OpenCV (headless), Inno
Setup 7.1.1, and 7-Zip 26.05. System Python and PATH are unchanged.

```powershell
.\odin.ps1 Python -PythonArgs '-c','import PIL, numpy, cv2; print(PIL.__version__)'
```

The existing walking script is historical and remains on hold. Having image tools
available does not change that decision.

To reprovision tools, run `python tools/local/Setup-Toolchain.py` with any existing
Python 3.8+ (or use the local Python). Downloads are pinned by SHA-256 in
`toolchain-lock.json`; image libraries are pinned in `requirements-art.txt`. The
setup script downloads files only from the publishers/package registry and extracts
the Inno compiler without running its installer. Tools and downloads stay in this
checkout and are ignored by Git.

## References and history

- `art/`: current production sprites, tracked in Git.
- `draft-assets/`: approved new artwork and approval records, tracked in Git.
- `local-data/reference-review/`: review packs and artwork experiments.
- `local-data/Odin-original/references` and `local-data/Odin-v3/references`: personal
  source photos, videos, and original reference material.
- `local-data/test-apps/`: original transferred review executables.
- `local-data/archives/vm-history-2026-11-06.7z`: compressed old VM workspace,
  downloads, source snapshots, prompts, and generated images.
- `local-data/maintenance/vm-history-files.csv`: original relative paths, byte sizes,
  and SHA-256 hashes. `history-compaction.json` records archive verification and savings.

The unused Blender tool installation and its downloaded ZIP are excluded from the
archive. Original photos, artwork, Blender project files, and 3D experiment source
are retained; the experiment itself remains on hold. Legacy absolute paths are
provenance and do not point to live dependencies.

If `history-compaction.json` says `expandedHistoryRemoved: false`, run
`./odin.ps1 Clean` from your own normal Windows PowerShell. The agent's automatic
approval policy blocks deletions, so it cannot complete that step. The command
first tests archive integrity, verifies every archived path and size, rechecks
the original files against the SHA-256 inventory, and then removes only the old
expanded `local-data/vm-history` tree. It leaves current references and artwork
alone. The compressed archive remains in the same checkout.
The one-time local `Finish-SetupCleanup.ps1` also removes the 128 MB disposable
audit copy created by this chat under the Codex work folder. Its exact path and
marker checks are recorded in that script; other folders are not cleaned.

To inspect or restore a selected historical folder locally:

```powershell
$zip = '.\local-data\toolchain\7zip\x64\7za.exe'
& $zip l '.\local-data\archives\vm-history-2026-11-06.7z'
& $zip x '.\local-data\archives\vm-history-2026-11-06.7z' `
  'vm-history\generated-images\*' '-o.\local-data\restored-history'
```

Keep a backup of personal references and the history archive when you arrange your
normal backups: Git does not track `local-data`. No backup/export job is configured
by this setup. `build/` is disposable and can be rebuilt. Do not delete the archive
or reference directories as if they were build caches.
