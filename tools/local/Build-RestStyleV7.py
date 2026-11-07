"""Restore original resting sprite families in a DEV pack without editing pixels.

The caller owns pack output. apply_rest mutates the supplied manifest and a
key->PNG-bytes payload dictionary, and returns both for convenient composition.
"""
from pathlib import Path
import hashlib
import io
import json
import math

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
FAMILIES = {"sit": 1, "loaf": 2, "lay": 3, "sleep": 4}
REST_CLIPS = (
    "sit", "loaf", "lay", "sleep", "look_sit", "look_loaf", "look_lay",
    "sit_eyes", "loaf_eyes", "lay_eyes", "sit_tail", "lay_tail", "sleep_tail",
)


def family(clip):
    if clip.startswith("look_"):
        return clip[5:]
    if clip.endswith("_eyes") or clip.endswith("_tail"):
        return clip[:-5]
    return clip


def visible_bounds(data):
    """Return alpha>=8 visible bounds as x/y/width/height; inspect only."""
    with Image.open(io.BytesIO(data)) as image:
        assert image.mode == "RGBA", "Original resting source lacks alpha."
        assert image.size == (1024, 1024), "Unexpected original native canvas."
        box = image.getchannel("A").point(lambda a: 255 if a >= 8 else 0).getbbox()
    assert box is not None, "Empty original resting source."
    assert box[0] > 0 and box[1] > 0 and box[2] < 1024 and box[3] < 1024, "Clipped source margin."
    return [box[0], box[1], box[2]-box[0], box[3]-box[1]]


def apply_rest(manifest, payloads):
    original = json.loads((ROOT / "art/manifest.json").read_text())
    original_clips = original["clips"]
    poses = {pose["id"]: pose for pose in manifest["poses"]}
    assert all(pose_id in poses for pose_id in FAMILIES.values()), "Missing resting still."
    widths = {}
    for name, pose_id in FAMILIES.items():
        pose = poses[pose_id]
        # Preserve the current physical display scale, explicitly converting the
        # draft canvas to the original1024 canvas. All family frames share it.
        native = pose.get("nativeWidth")
        if not native:
            with Image.open(io.BytesIO(payloads[pose["key"]])) as image:
                native = image.width
        width = float(pose["renderWidthAtSmall"]) * 1024.0 / native
        assert math.isfinite(width) and width > 0, "Invalid resting family scale."
        widths[name] = width
    v8_path=ROOT/'planning/owner-corrections-v8.json'
    v8=json.loads(v8_path.read_text()) if v8_path.exists() else {}
    fixed=bool(v8.get('enabled') and v8.get('fixedOriginalRestBodies'))
    if fixed:
        widths={name:float(v8['originalSpriteWidthsAtSmall'][str(pose_id)]) for name,pose_id in FAMILIES.items()}
    else:
        assert abs(widths["sit"] - .50625*1024) < .001, "Original sitting display scale drifted."

    entries = []
    clips = []
    grouped_bounds = {name: [] for name in FAMILIES}
    neutral_data = {}
    for clip in REST_CLIPS:
        source_clip = original_clips[clip]
        name = family(clip)
        pose_id = FAMILIES[name]
        frame_count = source_clip["frames"]
        assert len(source_clip["images"]) == frame_count, "Original per-frame metadata incomplete."
        for frame in range(frame_count):
            source = f"art/frames/{clip}/{frame:03d}.png"
            data = (ROOT/source).read_bytes()
            digest = hashlib.sha256(data).hexdigest()
            bounds = visible_bounds(data)
            support = source_clip["images"][frame].get("anchor_y", source_clip.get("anchor_y", original["foot_anchor"][1]))
            if fixed:
                neutral=original_clips[name]
                support=neutral['images'][0].get('anchor_y',neutral.get('anchor_y',original['foot_anchor'][1]))
            key = f"action-{clip}-{frame:03d}.png"
            entry = {
                "id": pose_id, "key": key, "label": f"Original {clip} frame {frame}",
                "clip": clip, "frame": frame, "source": source, "sha256": digest,
                "bounds": bounds, "nativeWidth": 1024, "nativeHeight": 1024,
                "customAnchor": True, "anchorX": original["foot_anchor"][0],
                "supportY": support, "renderWidthAtSmall": widths[name],
                "airborne": False, "grounded": True,
                "originalRestArtwork": True, "sourcePixelsUnmodified": True,
                "floorPolicyReviewPending": True,
                "notes": "Original sprite byte-exact; native frame root/support and coherent family scale. Original sleep belly/contact remains for visual review; sleep-tail overhang is allowed.",
            }
            payloads[key] = data
            entries.append(entry)
            grouped_bounds[name].append(bounds)
            if clip == name:
                assert frame == 0, "Unexpected multi-frame neutral resting clip."
                neutral_data[name] = (entry, data)
        clips.append({"clip": clip, "frames": frame_count, "seconds": source_clip["seconds"], "loop": source_clip.get("loop", False)})

    assert len(entries) == 73, "Incomplete original resting-family restoration."
    for name, pose_id in FAMILIES.items():
        pose = poses[pose_id]
        old_native = pose.get("nativeWidth", 1024)
        old_head_crop = pose.get("headCrop")
        entry, data = neutral_data[name]
        boxes = grouped_bounds[name]
        left = min(b[0] for b in boxes)
        top = min(b[1] for b in boxes)
        right = max(b[0]+b[2] for b in boxes)
        bottom = max(b[1]+b[3] for b in boxes)
        rect = original_clips[name]["head_rect"]
        pose.update({key: entry[key] for key in ("source", "sha256", "bounds", "nativeWidth", "nativeHeight", "customAnchor", "anchorX", "supportY", "renderWidthAtSmall", "airborne", "grounded", "originalRestArtwork", "sourcePixelsUnmodified", "floorPolicyReviewPending", "notes")})
        pose["motionBounds"] = [left, top, right-left, bottom-top]
        # Original app native head rectangle supplies the tracking point. It is
        # not an owner-approved anatomical measurement or a head-size estimate.
        pose["headCrop"] = ([rect[0], rect[1], rect[0]+rect[2], rect[1]+rect[3]] if rect else
                            [round(value*1024/old_native) for value in old_head_crop] if old_head_crop else None)
        payloads[pose["key"]] = data

    for kind in ("blinks", "motions"):
        for item in manifest.get(kind, []):
            payloads.pop(item["key"], None)
        manifest[kind] = []
    manifest["actions"] = [entry for entry in manifest.get("actions", []) if entry["clip"] not in REST_CLIPS] + entries
    manifest["actionClips"] = [clip for clip in manifest.get("actionClips", []) if clip["clip"] not in REST_CLIPS] + clips
    manifest["originalRestorationV7"] = {
        "frameCount": 73, "neutralStillCount": 4, "sourcePixelsUnmodified": True,
        "generatedRestLayersRemoved": True, "physicalFamilyWidthsAtSmall": widths,
        "sourceManifest": "art/manifest.json", "floorPolicyReviewPending": True,
        "contactCaveat": "Original sleep belly gap and all family floor contacts require visual review. Native support metadata retained; sleep-tail overhang remains allowed. No source pixels erased or deformed.",
    }
    manifest['fixedOriginalRestBodies']=fixed
    return manifest, payloads
