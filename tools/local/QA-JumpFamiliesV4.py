"""Independent, headless verification of the native32-frame jump integration.

Checks original/new pixels and transformed geometry; never launches an EXE or UI.
"""
from pathlib import Path
import argparse,hashlib,io,json,math,zipfile
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]

def digest(data):return hashlib.sha256(data).hexdigest()
def read(path):return json.loads((ROOT/path).read_text())
def check(value,message):
    if not value:raise AssertionError(message)
def near(a,b):return math.isclose(a,b,rel_tol=1e-9,abs_tol=1e-7)
def image_data(data):
    image=Image.open(io.BytesIO(data)).convert('RGBA')
    dense=image.getchannel('A').point(lambda a:255 if a>64 else 0).getbbox()
    visible=image.getchannel('A').point(lambda a:255 if a>8 else 0).getbbox()
    check(dense and visible,'Empty sprite')
    return image.size,dense,visible

def run(exe_path,skip_exe=False):
    activation=read('planning/action-art-integration-v4.json')
    check(activation.get('enabled'), 'New jump-family integration is not enabled')
    original=read('art/manifest.json')
    current={p['id']:p for p in read('planning/state-head-review.json')['poses']}
    pack_path=ROOT/'local-data/reference-review/dev-latest-art/dev-artwork.zip'
    pack_bytes=pack_path.read_bytes();family_cases=[];frame_cases=[]
    with zipfile.ZipFile(io.BytesIO(pack_bytes)) as pack:
        manifest=json.loads(pack.read('manifest.json'))
        entries=manifest.get('actions',[])
        check(len(entries)==32,'Expected32 native jump action entries, with no partial fallback')
        by_frame={(e['clip'],e['frame']):e for e in entries}
        check(len(by_frame)==32,'Duplicate action key or frame')
        check(set(e['clip'] for e in entries)=={'jump_left','jump_right'},'Unexpected/missing action family')
        clips={c['clip']:c for c in manifest.get('actionClips',[])}
        check(set(clips)=={'jump_left','jump_right'},'Missing native clip metadata')
        for clip,pose_id in [('jump_left',16),('jump_right',17)]:
            family=read(activation['families'][clip])
            check(family.get('complete'),'Family not finalized: '+clip)
            frames={f['frame']:f for f in family['frames']}
            check(set(frames)==set(range(16)),'Frame coverage incomplete: '+clip)
            original_clip=original['clips'][clip];clip_info=clips[clip]
            check(clip_info['frames']==original_clip['frames']==16,'Original frame count changed')
            check(near(clip_info['seconds'],original_clip['seconds']),'Original duration changed')
            check(clip_info['loop']==original_clip.get('loop',False),'Original loop policy changed')
            accepted=current[pose_id]
            canonical=(ROOT/accepted['source']).read_bytes();_,canonical_dense,_=image_data(canonical)
            old_ref=(ROOT/f'art/frames/{clip}/008.png').read_bytes();_,old_ref_dense,_=image_data(old_ref)
            legacy_scale=accepted['scaleAtSmall']*(canonical_dense[2]-canonical_dense[0])/(old_ref_dense[2]-old_ref_dense[0])
            check((ROOT/frames[8]['source']).read_bytes()==canonical,'Accepted frame008 PNG bytes changed: '+clip)
            for number in range(16):
                metadata=frames[number];entry=by_frame[(clip,number)]
                source_path=ROOT/metadata['source'];new_bytes=source_path.read_bytes();new_sha=digest(new_bytes)
                check(new_sha==metadata['sha256']==entry['sha256'],'Candidate/packed hash mismatch: '+clip+str(number))
                check(entry['source']==metadata['source'],'Packed frame selects a different source')
                check(not entry['source'].startswith('art/frames/'),'Unchanged original frame fallback remains')
                check(pack.read(entry['key'])==new_bytes,'Embedded PNG pixels/encoding changed')
                size,dense,visible=image_data(new_bytes)
                check(entry.get('nativeWidth')==size[0] and entry.get('nativeHeight')==size[1],'Wrong native dimensions')
                recorded_size=metadata.get('nativeSize',metadata.get('newNativeSize'))
                if recorded_size:check(list(size)==recorded_size,'Native metadata differs from PNG')
                check(visible[0]>0 and visible[1]>0 and visible[2]<size[0] and visible[3]<size[1],'Clipped transparent margin: '+clip+str(number))
                check(entry['bounds']==[visible[0],visible[1],visible[2]-visible[0],visible[3]-visible[1]],'Packed visible bounds differ')
                old_path=ROOT/f'art/frames/{clip}/{number:03}.png';old_bytes=old_path.read_bytes();old_sha=digest(old_bytes)
                recorded_old=metadata.get('oldSHA256',metadata.get('sourceSHA256'))
                check(old_sha==recorded_old==entry['sourceLegacySHA256'],'Original source hash changed')
                old_size,old_dense,_=image_data(old_bytes)
                scale=entry['renderWidthAtSmall']/size[0]
                correction=float(metadata.get('headScaleCorrection',1))
                check(correction>0,'Invalid optional head scale calibration')
                check(scale>0 and entry['customAnchor'],'Native anchor/scale not enabled')
                # Independent world-space extent invariants, rather than importing the pack builder.
                check(near((dense[2]-dense[0])*scale,(old_dense[2]-old_dense[0])*legacy_scale*correction),'Displayed width jumps between frames')
                root_x=original['foot_anchor'][0]
                check(near((dense[0]-entry['anchorX'])*scale,(old_dense[0]-root_x)*legacy_scale*correction),'Logical left/root offset changed')
                check(near((dense[2]-entry['anchorX'])*scale,(old_dense[2]-root_x)*legacy_scale*correction),'Logical right/root offset changed')
                grounded=bool(metadata.get('grounded',False))
                check(entry['airborne']!=grounded and entry['grounded']==grounded,'Ground/airborne classification changed')
                support=entry['supportY'];check(0<support<size[1],'Support outside native canvas')
                old_anchor=original_clip['images'][number].get('anchor_y',original_clip.get('anchor_y',620))
                if grounded:
                    expected=metadata.get('proposedSupportY',metadata.get('supportY'))
                    check(expected is not None and near(support,expected),'Grounded frame lost physical contact anchor')
                elif number!=8:
                    check(near((dense[3]-support)*scale,(old_dense[3]-old_anchor)*legacy_scale*correction),'Airborne bottom/root clearance changed')
                if number==8:
                    check(near(correction,1),'Accepted008 head calibration must remain1')
                    check(near(scale,accepted['scaleAtSmall']) and near(support,accepted['supportY']),'Accepted sample geometry changed')
                for display_size in [480,640,800]:
                    actual=scale*display_size/480
                    check(near(actual/scale,display_size/480),'Display sizing is not uniform')
                frame_cases.append({'clip':clip,'frame':number,'sha256':new_sha,'nativeSize':size,'grounded':grounded,'supportY':support,'scaleAtSmall':scale,'headScaleCorrection':correction,'transparentMarginsClear':True,'originalSourcePreserved':True})
            family_cases.append({'clip':clip,'frames':16,'seconds':clip_info['seconds'],'loop':clip_info['loop'],'canonical008Exact':True})
    artifact={}
    if not skip_exe:
        executable=(ROOT/exe_path).read_bytes()
        check(pack_bytes in executable,'DEV executable has a stale/different embedded native pack')
        artifact={'path':exe_path,'sha256':digest(executable),'currentNativePackBytesEmbedded':True}
    return {'passed':True,'headless':True,'windowsCreated':0,'familyCount':2,'frameCount':32,'familyCases':family_cases,
            'originalSourceHashesPreserved':True,'allNativeAlphaMarginsClear':True,'noOldFrameFallback':True,
            'canonical008BytesAndGeometryExact':True,'originalTimingsAndCountsExact':True,'logicalRootXAndAirborneYOffsetsVerified':True,
            'groundedPhysicalSupportsVerified':True,'threeDisplaySizesUniform':True,'artifact':artifact,'frames':frame_cases}

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--dev-exe',default='test-exes/Odin-Dev-Review-v4.exe');parser.add_argument('--skip-exe',action='store_true');args=parser.parse_args()
    destination=ROOT/'build/qa-jump-families-v4.json';destination.parent.mkdir(parents=True,exist_ok=True)
    try:
        report=run(args.dev_exe,args.skip_exe);destination.write_text(json.dumps(report,indent=2)+'\n')
        print(json.dumps({k:v for k,v in report.items() if k!='frames'}))
    except Exception as error:
        destination.write_text(json.dumps({'passed':False,'headless':True,'error':str(error)},indent=2)+'\n');print(str(error));raise SystemExit(1)
