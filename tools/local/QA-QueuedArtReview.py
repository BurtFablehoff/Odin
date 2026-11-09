"""Independent headless checks for the queued v3 still edits and embedded packs.

Reads sources and EXEs only. Writes one QA report; never launches a window.
"""
from pathlib import Path
import argparse,hashlib,io,json,math,zipfile
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
EXPECTED=[1,2,3,4,5,6,7,8,10,11,12,13,14,15,16,17]
ALLOWED={1,4,8,10,11,16,17}

def read(path):return json.loads((ROOT/path).read_text())
def digest(data):return hashlib.sha256(data).hexdigest()
def same_float(a,b):return math.isclose(a,b,rel_tol=1e-10,abs_tol=1e-8)
def check(condition,message):
    if not condition:raise AssertionError(message)
def native(data):
    im=Image.open(io.BytesIO(data)).convert('RGBA')
    b=im.getchannel('A').point(lambda a:255 if a>8 else 0).getbbox()
    check(b is not None,'Empty sprite')
    return im.size,b

def run(metadata_only=False,dev_exe='test-exes/Odin-Dev-Review-v3.exe',review_exe='test-exes/Odin-Art-Review-v3.exe'):
    baseline=read('planning/numbered-sizing-baseline-v2.json')
    feedback=read('planning/numbered-sizing-feedback-v3.json')
    current=read('planning/state-head-review.json')
    check([p['id'] for p in current['poses']]==EXPECTED,'Current IDs differ or curled sleep returned')
    old={p['id']:p for p in baseline['poses']};poses={p['id']:p for p in current['poses']}
    changes=feedback['changes'];changed=[]
    check(set(map(int,changes)).issubset(ALLOWED),'Unexpected pose in v3 feedback')
    for i in [10,11]:check(same_float(changes[str(i)]['multiplier'],.92),f'#{i} reduction is not exactly8%')
    for i,p in poses.items():
        b=old[i];change=changes.get(str(i))
        check(digest((ROOT/b['source']).read_bytes())==b['sourceSHA256'],f'Original source overwritten: #{i}')
        if change:
            multiplier=change['multiplier'];norm=change.get('sourceNormalization',1)
            check(same_float(p['scaleAtSmall'],b['scaleAtSmall']*multiplier*norm),f'Incorrect normalized scale: #{i}')
            if i not in [10,11]:check(same_float(multiplier,1),f'Unexpected proportional size edit: #{i}')
        else:
            check(same_float(p['scaleAtSmall'],b['scaleAtSmall']),f'Unexpected size edit: #{i}')
        fields=['source','sourceSHA256','scaleAtSmall','supportY','headCrop']
        if any(p.get(k)!=b.get(k) for k in fields):changed.append(i)
        if i not in ALLOWED:
            for k in fields:check(p.get(k)==b.get(k),f'Unlisted #{i} changed {k}')
        data=(ROOT/p['source']).read_bytes();check(digest(data)==p['sourceSHA256'],f'Current source hash mismatch: #{i}')
        size,bounds=native(data)
        check(bounds[0]>0 and bounds[1]>0 and bounds[2]<size[0] and bounds[3]<size[1],f'Clipped native alpha bounds: #{i}')
    result={'passed':True,'headless':True,'metadataOnly':metadata_only,'poses':16,'changedIds':changed,
            'allowedEditIds':sorted(ALLOWED),'unlistedPosesUnchanged':True,'originalSourcesPreserved':True,
            'sideAndTuckedDisplayMultiplier':.92,'normalizedScalesVerified':True,'nativeAlphaMarginsClear':True}
    if metadata_only:return result
    overrides=read('planning/queued-art-edits-v3.json').get('motionOverrides',[])
    check(any(p['key']=='gaze-1-right.png' for p in overrides),'Standing right-gaze muzzle override missing')
    packed=0;motion_checks=0
    for pack_path,exe_path in [
        ('local-data/reference-review/approved-contact-v1/contact-review.zip',review_exe),
        ('local-data/reference-review/dev-latest-art/dev-artwork.zip',dev_exe)]:
        pack_bytes=(ROOT/pack_path).read_bytes()
        with zipfile.ZipFile(io.BytesIO(pack_bytes)) as archive:
            manifest=json.loads(archive.read('manifest.json'))
            check([p['id'] for p in manifest['poses']]==EXPECTED,f'Incorrect packed IDs: {pack_path}')
            for entry in manifest['poses']:
                p=poses[entry['id']];data=archive.read(entry['key']);size,bounds=native(data)
                check(entry['source']==p['source'],f'Stale packed source: {entry["id"]}')
                check(digest(data)==p['sourceSHA256']==entry['sha256'],f'Packed source hash mismatch: {entry["id"]}')
                check(entry['bounds']==[bounds[0],bounds[1],bounds[2]-bounds[0],bounds[3]-bounds[1]],f'Packed native bounds mismatch: {entry["id"]}')
                check(same_float(entry['renderWidthAtSmall'],size[0]*p['scaleAtSmall']),f'Packed display scale mismatch: {entry["id"]}')
                check(same_float(entry['supportY'],p['supportY']),f'Packed support mismatch: {entry["id"]}')
                if 'headCrop' in entry:check(entry['headCrop']==p['headCrop'],f'Packed head crop mismatch: {entry["id"]}')
                packed+=1
            motions={p['key']:p for p in manifest['motions']}
            for override in overrides:
                entry=motions[override['key']];data=archive.read(entry['key'])
                check(entry['source']==override['source'],f'Stale motion source: {entry["key"]}')
                check(digest(data)==override['sha256']==entry['sha256'],f'Motion override hash mismatch: {entry["key"]}')
                check(data==(ROOT/override['source']).read_bytes(),f'Motion PNG bytes changed: {entry["key"]}')
                motion_checks+=1
        exe=(ROOT/exe_path).read_bytes()
        check(pack_bytes in exe,f'Executable contains stale embedded pack: {exe_path}')
    result.update(packedPoseCases=packed,packedMotionOverrideCases=motion_checks,
                  standingRightGazeOverrideVerified=True,executableEmbeddedPackBytesVerified=True,
                  noSpriteSourcesModifiedByQA=True)
    return result

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--metadata-only',action='store_true')
    parser.add_argument('--dev-exe',default='test-exes/Odin-Dev-Review-v3.exe');parser.add_argument('--review-exe',default='test-exes/Odin-Art-Review-v3.exe');args=parser.parse_args()
    path=ROOT/'build/qa-queued-art-v3.json';path.parent.mkdir(parents=True,exist_ok=True)
    try:
        result=run(args.metadata_only,args.dev_exe,args.review_exe);path.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
    except Exception as error:
        result={'passed':False,'headless':True,'error':str(error)};path.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result));raise SystemExit(1)
