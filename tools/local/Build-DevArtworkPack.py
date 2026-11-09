"""Embed native reviewed images for the DEV companion, without modifying any sprite."""
from pathlib import Path
import json,zipfile,runpy
ROOT=Path(__file__).resolve().parents[2]
source=ROOT/'local-data/reference-review/approved-contact-v1/contact-review.zip'
destination=ROOT/'local-data/reference-review/dev-latest-art/dev-artwork.zip'
destination.parent.mkdir(parents=True,exist_ok=True)
baseline=json.loads((ROOT/'planning/numbered-sizing-baseline-v1.json').read_text())
feedback=json.loads((ROOT/'planning/numbered-sizing-feedback-v2.json').read_text())
current={p['id']:p for p in json.loads((ROOT/'planning/state-head-review.json').read_text())['poses']}
keys={12:'groom',13:'stretch',14:'run_left',15:'run_right',16:'jump_left',17:'jump_right'}
widths={keys[p['id']]:p['scaleAtSmall']*1024*feedback['changes'].get(str(p['id']),{}).get('multiplier',1) for p in baseline['poses'] if p['id'] in keys}
action_builder=runpy.run_path(str(ROOT/'tools/local/Build-JumpActionEntries.py'))
v8_path=ROOT/'planning/owner-corrections-v8.json'
if v8_path.exists() and json.loads(v8_path.read_text()).get('enabled'):
 action_builder=runpy.run_path(str(ROOT/'tools/local/Build-JumpStyleV7.py'))
action_entries,action_clips=action_builder['build_actions'](current)
recovery_builder=runpy.run_path(str(ROOT/'tools/local/Build-StyleRecoveryV7.py'))
with zipfile.ZipFile(source) as archive,zipfile.ZipFile(destination,'w',zipfile.ZIP_DEFLATED) as output:
 manifest=json.loads(archive.read('manifest.json'));manifest['legacyActionWidths']=widths
 for pose in manifest['poses']:pose['headCrop']=current[pose['id']]['headCrop']
 manifest['actions']=[record for record,data in action_entries];manifest['actionClips']=action_clips
 payloads={item.filename:archive.read(item.filename) for item in archive.infolist() if item.filename not in ('manifest.json','reference.png')}
 for record,data in action_entries:payloads[record['key']]=data
 manifest,payloads=recovery_builder['apply_recovery'](manifest,payloads)
 # V12 2b: DEV run clips registered by the head, with the kept optical-flow tweens (production keeps 18 / 16 frames).
 manifest,payloads=runpy.run_path(str(ROOT/'tools/local/Apply-RunTweensV12.py'))['apply_pack'](manifest,payloads)
 # V12 3R: the Codex head-turn centre frame becomes the state's DEV still for 6/7/8 (look_* clips off since 3bR-2; the
 # centre-as-still step off since 3c, Hatt's 'revert for now': 6/7/8 keep their v12-1 stills).
 manifest,payloads=runpy.run_path(str(ROOT/'tools/local/Apply-HeadTurnsV12.py'))['apply_pack'](manifest,payloads)
 # V12 3bR-1: 12-hour clock clips clock_rear/clock_lookback/clock_side on one fixed body (complete families only; the 5-frame look_* clips are no longer emitted, V12 3bR-2).
 # V12 3c: CLOCK_IN_PACK = False there, so no clock clips are emitted and 6/7/8 are plain stills (Hatt at G3b: 'revert for now').
 manifest,payloads=runpy.run_path(str(ROOT/'tools/local/Apply-ClockV12.py'))['apply_pack'](manifest,payloads)
 # V12 3d: states 6/7/8 are parked out of this version (Hatt at G3b: 'let take those 3 new sits out for this version'). Last step.
 manifest,payloads=runpy.run_path(str(ROOT/'tools/local/Apply-ParkedStatesV12.py'))['apply_pack'](manifest,payloads)
 output.writestr('manifest.json',json.dumps(manifest))
 for key,data in payloads.items():output.writestr(key,data)
print('DEV native artwork pack:',destination)
