"""Independent final source/pack/pixel/registration audit. Never opens an app."""
from __future__ import annotations

import argparse
from collections import Counter
import hashlib
import json
import math
import mmap
from pathlib import Path
import traceback
import zipfile

import numpy as np
from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
EXPECTED_IDS = [1, 2, 3, 4, 6, 7, 8, 12, 13, 14, 15, 16, 17]
REVIEWED_ASSETS = {
    1: "63a101e99163ffab8c99b9a093611bf9607a5834645cb3f15a5625c72bb069d2",
    6: "83be1425f413398847060637fc5a6ef9b50e67832b00cc847865f49d1eb6e2c3",
    8: "055b3850a7eef9c1220badf95bee15781e5369652d4fd6f46c5f89c34e6a2183",
}
ORIGINAL_SIT_SHA = "ab5c4261984e4f252172962198c2d532dffc8b8d269cfe23dd06d000808e7c43"


def require(condition, message):
    if not condition:
        raise AssertionError(message)


def path_for(relative):
    path = (ROOT / relative).resolve()
    require(path.is_relative_to(ROOT), f"Path escaped Odin: {relative}")
    return path


def read_json(relative):
    return json.loads(path_for(relative).read_text(encoding="utf-8-sig"))


def digest(data):
    return hashlib.sha256(data).hexdigest()


def file_digest(path):
    result = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            result.update(block)
    return result.hexdigest()


def rgba(relative):
    with Image.open(path_for(relative)) as image:
        require(image.mode == "RGBA", f"Expected native RGBA source: {relative}")
        return np.array(image)


def same_number(actual, expected, message):
    require(math.isclose(actual, expected, rel_tol=1e-11, abs_tol=1e-9),
            f"{message}: {actual!r} != {expected!r}")


def audit(executable, report):
    policy = read_json("planning/owner-corrections-v10.json")
    require(policy.get("enabled") and policy.get("version") == 10, "V10 policy is not active.")
    require(policy.get("developmentOnly") and policy.get("ownerRequestedDevelopmentIntegration"),
            "Owner-requested DEV integration scope is missing.")
    require(not policy.get("workInProgress"), "V10 policy still marks a work-in-progress build.")
    require(policy.get("ownerArtworkApproval") is False, "DEV integration was promoted to artwork approval.")
    require(set(policy["retiredPoseIds"]) == {5, 9, 10}, "Retired pose scope changed.")
    overrides = {entry["id"]: entry for entry in policy["poseOverrides"]}
    require(set(overrides) == {1, 6, 8}, "Final sitting/rear/side override set is incomplete.")

    inventory = read_json("local-data/reference-review/style-consistency-v7/inventory.json")
    legacy = inventory["legacySources"]
    require(len(legacy) == 183, "Original inventory count changed.")
    require(len({entry["source"] for entry in legacy}) == 183, "Original inventory contains duplicates.")
    legacy_by_source = {entry["source"]: entry for entry in legacy}
    legacy_counts = Counter(Path(entry["source"]).parent.name for entry in legacy)
    for entry in legacy:
        require(file_digest(path_for(entry["source"])) == entry["sha256"],
                f"Original source changed: {entry['source']}")
    require(legacy_counts["jump_left"] == legacy_counts["jump_right"] == 16,
            "Original jump inventory is incomplete.")
    require(legacy_counts["groom"] == 16, "Original grooming inventory is incomplete.")
    require(file_digest(path_for("art/frames/sit/000.png")) == ORIGINAL_SIT_SHA,
            "Exact original sitting master changed.")
    report.update(originalSourceHashesVerified=183, originalJumpSourcesPreserved=32,
                  originalGroomSourcesPreserved=16, originalSittingSourcePreserved=True)

    pack_path = path_for("local-data/reference-review/dev-latest-art/dev-artwork.zip")
    pack_data = pack_path.read_bytes()
    exe_path = path_for(executable)
    require(exe_path.is_file(), f"Final executable missing: {executable}")
    with exe_path.open("rb") as stream, mmap.mmap(stream.fileno(), 0, access=mmap.ACCESS_READ) as binary:
        resource_offset = binary.find(pack_data)
    require(resource_offset >= 0, "Compiled executable embeds a stale/different DEV pack.")
    report.update(candidateExe=str(exe_path.relative_to(ROOT)).replace("\\", "/"),
                  executableSHA256=file_digest(exe_path), packSHA256=digest(pack_data),
                  exactPackEmbedded=True, embeddedPackByteOffset=resource_offset)

    original_manifest = read_json("art/manifest.json")
    state_config = read_json("planning/state-head-review.json")
    current_states = {entry["id"]: entry for entry in state_config["poses"]}
    with zipfile.ZipFile(pack_path) as archive:
        require(archive.testzip() is None, "DEV pack CRC check failed.")
        manifest = json.loads(archive.read("manifest.json"))
        require([p["id"] for p in manifest["poses"]] == EXPECTED_IDS, "Active/retired still IDs changed.")
        require(not manifest.get("blinks") and not manifest.get("motions"),
                "Old generated rest-head/tail source layers returned to the pack.")
        require(manifest.get("fixedOriginalRestBodies"), "Fixed original resting bodies are disabled.")
        require(manifest.get("headSourceRegistrationVersion") == 10, "V10 gaze registration is missing.")
        require(manifest.get("ownerCorrectionsV10") == policy, "Embedded V10 policy is stale.")
        require(manifest.get("completeStyleCorrection") is False, "Build claims artwork completion.")
        require(len(manifest["poses"]) == 13 and len(manifest["actions"]) == 121,
                "Final 13-still/121-action count changed.")
        entries = [entry for kind in ("poses", "actions", "blinks", "motions")
                   for entry in manifest.get(kind, [])]
        require(len(entries) == 135, "Expected all 135 final pack sources.")
        names = [entry["key"] for entry in entries]
        require(len(set(names)) == 135, "Final pack has duplicate source keys.")
        require(set(archive.namelist()) == set(names) | {"manifest.json"},
                "Pack contains unreferenced/historical assets.")
        require(not any(p["id"] in (5, 9, 10, 11) for p in entries), "A retired or parked pose returned in actions.")
        for entry in entries:
            data = archive.read(entry["key"])
            require(digest(data) == entry["sha256"], f"Packed hash mismatch: {entry['key']}")
            require(data == path_for(entry["source"]).read_bytes(),
                    f"Packed bytes differ from selected source: {entry['key']}")
        poses = {entry["id"]: entry for entry in manifest["poses"]}
        for pose_id, override in overrides.items():
            pose = poses[pose_id]
            require(override["sourceSHA256"] == REVIEWED_ASSETS[pose_id],
                    f"Policy no longer uses reviewed asset {pose_id}.")
            require(pose["source"] == override["source"] and pose["sha256"] == override["sourceSHA256"],
                    f"Pose {pose_id} does not use the final reviewed override.")
            require(pose.get("customAnchor") and pose.get("ownerArtworkApproved") is False,
                    f"Pose {pose_id} registration or approval scope changed.")
            for field in ("supportY", "anchorX"):
                same_number(pose[field], override[field], f"Pose {pose_id} {field}")
                same_number(current_states[pose_id][field], override[field], f"State config {pose_id} {field}")
            same_number(pose["renderWidthAtSmall"], pose["nativeWidth"] * override["scaleAtSmall"],
                        f"Pose {pose_id} physical scale")
            same_number(current_states[pose_id]["scaleAtSmall"], override["scaleAtSmall"],
                        f"State config {pose_id} scale")
            require(current_states[pose_id]["source"] == override["source"], f"State config {pose_id} source")
            require(pose["headCrop"] == override["headCrop"], f"Pose {pose_id} anatomical crop")
        for pose_id in (1, 6):
            same_number(poses[pose_id]["anchorX"], 512, f"Native original root {pose_id}")
            same_number(poses[pose_id]["supportY"], 944, f"Native original support {pose_id}")
            same_number(poses[pose_id]["renderWidthAtSmall"], 480, f"Native original size {pose_id}")
        original_widths = read_json("planning/owner-corrections-v8.json")["originalSpriteWidthsAtSmall"]
        for pose_id, width in original_widths.items():
            same_number(poses[int(pose_id)]["renderWidthAtSmall"], width, f"Original runtime size {pose_id}")
        same_number(manifest["legacyActionWidths"]["stretch"], 480, "Original stretch width")

        actions = manifest["actions"]
        clip_entries = {}
        for entry in actions:
            clip_entries.setdefault(entry["clip"], []).append(entry)
            if entry["id"] <= 4:
                for field in ("anchorX", "supportY", "renderWidthAtSmall"):
                    same_number(entry[field], poses[entry["id"]][field], f"Fixed rest {entry['key']} {field}")
        jump_count = groom_count = rest_count = 0
        for clip in ("jump_left", "jump_right"):
            family = clip_entries[clip]
            require(sorted(p["frame"] for p in family) == list(range(16)), f"Incomplete {clip}")
            for entry in family:
                original_source = f"art/frames/{clip}/{entry['frame']:03d}.png"
                require(entry["source"] == original_source, f"Original jump artwork was replaced: {entry['key']}")
                require(entry["sha256"] == legacy_by_source[original_source]["sha256"],
                        f"Original jump hash changed: {entry['key']}")
                same_number(entry["renderWidthAtSmall"], 480, f"Jump original scale {entry['key']}")
                jump_count += 1
        require(sorted(p["frame"] for p in clip_entries["groom"]) == list(range(16)), "Incomplete groom correction")
        for entry in clip_entries["groom"]:
            source = f"art/frames/groom/{entry['frame']:03d}.png"
            original = rgba(source)
            corrected = rgba(entry["source"])
            require(original.shape == corrected.shape == (1024, 1024, 4), "Groom canvas changed.")
            changed = np.any(original != corrected, axis=2)
            allowed = np.zeros(changed.shape, dtype=bool)
            allowed[895:958, 380:463] = True
            require(not changed[~allowed].any(), f"Groom correction escaped the local paw region: {entry['frame']}")
            require(changed.any(), f"Groom correction absent: {entry['frame']}")
            require(entry["sourceLegacySHA256"] == legacy_by_source[source]["sha256"],
                    f"Groom source provenance changed: {entry['frame']}")
            groom_count += 1
        for clip, family in clip_entries.items():
            if clip.startswith("look_") or clip in ("sit", "loaf", "lay", "sleep", "sit_eyes", "loaf_eyes", "lay_eyes", "sit_tail", "lay_tail", "sleep_tail"):
                rest_count += len(family)
        require(jump_count == 32 and groom_count == 16 and rest_count == 73,
                "Final jump/groom/rest family counts changed.")
        for clip in manifest["actionClips"]:
            source_clip = original_manifest["clips"][clip["clip"]]
            require(clip["frames"] == source_clip["frames"], f"Frame count changed: {clip['clip']}")
            same_number(clip["seconds"], source_clip["seconds"], f"Original timeline {clip['clip']}")
            require(clip["loop"] == source_clip["loop"], f"Original looping changed: {clip['clip']}")
        report.update(packSourcesChecked=len(entries), activeStillCount=14, actionFrameCount=121,
                      retiredPoseIds=[5, 9, 10], finalOverridePoseIds=[1, 6, 8],
                      originalJumpFramesPacked=jump_count, groomLocalPatchFramesPacked=groom_count,
                      fixedRestFamilyFrames=rest_count, originalActionTimelinesPreserved=True,
                      policyScalesRootsSupportsVerified=True, finalStateConfigurationMatches=True)

    original_sit = rgba("art/frames/sit/000.png")
    repaired_sit = rgba(overrides[1]["source"])
    require(original_sit.shape == repaired_sit.shape == (1024, 1024, 4), "Sitting native canvas changed.")
    changed = np.any(original_sit != repaired_sit, axis=2)
    dense = original_sit[:, :, 3] > 128
    right_edges = np.where(dense.any(axis=1), dense.shape[1] - 1 - dense[:, ::-1].argmax(axis=1), -1)
    y, x = np.indices(changed.shape)
    edge = right_edges[:, None]
    allowed = (y > 725) & (y < 936) & (x > edge - 16) & (x < edge + 11)
    require(not changed[~allowed].any(), "Sitting repair escaped the narrow curved right rim.")
    require(int(changed.sum()) == 5128, "Reviewed sitting edge changed since visual review.")
    require(np.array_equal(original_sit[936:], repaired_sit[936:]), "Sitting underside/contact changed.")

    old_side = rgba("draft-assets/style-consistency-v9/side/side-fuller-persian.png")
    new_side = rgba(overrides[8]["source"])
    require(old_side.shape == new_side.shape, "Side canvas changed.")
    require(np.array_equal(old_side[:, :, 3], new_side[:, :, 3]), "Side silhouette/alpha changed.")
    side_changes = np.any(old_side != new_side, axis=2)
    require(int(side_changes.sum()) == 4761, "Reviewed side muzzle core changed.")
    yy, xx = np.where(side_changes)
    require(xx.min() > 700 and xx.max() < 850 and yy.min() > 710 and yy.max() < 850,
            "Side refinement escaped the coherent facial core.")

    rear = rgba(overrides[6]["source"])
    require(rear.shape == original_sit.shape, "Rear native original-scene canvas changed.")
    require(np.array_equal(rear[700:, :, 3], original_sit[700:, :, 3]),
            "Rear body alpha no longer matches the exact front-body contour.")
    require(np.array_equal(rear[790:959, 220:390], original_sit[790:959, 220:390]),
            "Rear free tail changed.")
    require(not (rear[945:, 650:, 3] > 8).any(), "Rejected rear contact flap returned.")
    require(np.array_equal(rear[945:, :, 3], original_sit[945:, :, 3]),
            "Rear added body/tail overhang beyond original paw footprint.")
    neck_edges = []
    for row in rear[620:786, :, 3]:
        visible = np.flatnonzero(row > 128)
        require(visible.size > 0, "Missing rear neck row.")
        neck_edges.append(int(visible[-1]))
    require(max(abs(a-b) for a, b in zip(neck_edges, neck_edges[1:])) <= 3,
            "Rejected rear head/body ledge returned.")
    report.update(sittingRimOnlyChangedPixels=int(changed.sum()),
                  sittingOutsideRimRgbaExact=True, sideAlphaExactV9=True,
                  sideLocalMuzzleChangedPixels=int(side_changes.sum()), rearBodyAlphaExactOriginal=True,
                  rearFreeTailExactOriginal=True, rearNoShelfOrContactFlap=True,
                  rearBelowFloorOnlyOriginalPawOverhang=True,
                  reviewedAssetSHA256={str(key): value for key, value in REVIEWED_ASSETS.items()},
                  ownerArtworkApproved=False, noRuntimeOrDesktopInvoked=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--executable", default="test-exes/Odin-Dev-Consistency-v10.exe")
    parser.add_argument("--report", default="build/qa-owner-corrections-v10.json")
    args = parser.parse_args()
    report = dict(passed=False, headless=True, windowsCreated=0)
    try:
        audit(args.executable, report)
        report["passed"] = True
    except Exception as exc:
        report.update(error=str(exc), traceback=traceback.format_exc())
    output = path_for(args.report)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
