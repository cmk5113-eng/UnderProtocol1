"""Check the saved stage hover panel's actual hierarchy, references and layout.
This checks Unity YAML wiring; Unity Editor/Play Mode validation is still required.
"""
import json
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[2]
checks = 0


def check(value, message):
    global checks
    checks += 1
    if not value:
        raise AssertionError(message)


def documents(path):
    source = path.read_text(encoding="utf-8")
    entries = list(re.finditer(r"^--- !u!\d+ &(-?\d+)(?: stripped)?\n[\s\S]*?(?=^--- !u!|\Z)", source, re.M))
    check(len({m[1] for m in entries}) == len(entries), f"Duplicate fileIDs in {path.name}")
    return {m[1]: m[0] for m in entries}


def field(block, name):
    match = re.search(r"^  " + re.escape(name) + r": (.*)$", block, re.M)
    return match[1] if match else None


def ref(block, name):
    return re.search(r"fileID: (-?\d+)", field(block, name))[1]


def guid(block, name):
    return re.search(r"guid: ([0-9a-f]+)", field(block, name))[1]


def vector(block, name):
    return json.loads(re.sub(r"(\w+):", r'"\1":', field(block, name)))


def asset_guid(path):
    return re.search(r"^guid: (\w+)$", path.read_text(), re.M)[1]


scene = documents(ROOT / "Assets/SampleScene.unity")
prefabs = {}
for name in ["GameManger", "Globals/UIs(prefabs)/S_Stage"]:
    path = ROOT / "Assets/1.Datas/Original/Prefabs" / (name + ".prefab")
    prefabs[asset_guid(path.with_suffix(".prefab.meta"))] = documents(path)


def hierarchy(table, transform_id, instance=None):
    block = table[transform_id]
    if " stripped\n" in block:
        return hierarchy(prefabs[guid(block, "m_CorrespondingSourceObject")],
                         ref(block, "m_CorrespondingSourceObject"), ref(block, "m_PrefabInstance"))
    name = field(table[ref(block, "m_GameObject")], "m_Name")
    parent = ref(block, "m_Father")
    if parent != "0":
        return hierarchy(table, parent, instance) + [name]
    if instance:
        parent = re.search(r"m_TransformParent: \{fileID: (\d+)\}", scene[instance])[1]
        if parent != "0":
            return hierarchy(scene, parent) + [name]
    return [name]


# The existing prefab/scene spells the manager's GameObject name "GameManger".
expected = ["GameManger", "Canvas", "UIScreen", "S_Stage", "Top", "StageTitle (2)"]
candidates = []
for object_id, block in scene.items():
    if not block.startswith("--- !u!1 ") or field(block, "m_Name") != expected[-1]:
        continue
    components = re.findall(r"component: \{fileID: (\d+)\}", block)
    rect_id = next(id for id in components if scene[id].startswith("--- !u!224 "))
    path = hierarchy(scene, rect_id)
    if [name.lower() for name in path] == [name.lower() for name in expected]:
        candidates.append((object_id, rect_id, components, path))
check(len(candidates) == 1, "Exactly one panel exists at the requested hierarchy")
object_id, rect_id, components, path = candidates[0]
script_guid = asset_guid(ROOT / "Assets/3.Scripts/UIs/Functions/Information/UI_TargetHoverInfo.cs.meta")
hover = [scene[id] for id in components if field(scene[id], "m_Script") and guid(scene[id], "m_Script") == script_guid]
check(len(hover) == 1, "Panel has the existing UI_TargetHoverInfo component")
hover = hover[0]
check(field(hover, "followCursor") == "0", "Stage info is fixed in place")
check(field(hover, "m_Enabled") == "1" and field(scene[object_id], "m_IsActive") == "1", "Panel and controller are active")

text = scene[ref(hover, "summaryText")]
image = scene[ref(hover, "portrait")]
check("TMPro.TextMeshProUGUI" in text, "Summary references a TMP text component")
check("UnityEngine.UI.Image" in image, "Portrait references a UI Image component")
check(ref(hover, "m_GameObject") == object_id, "Controller belongs to the requested panel")
panel_image = next(scene[id] for id in components if "UnityEngine.UI.Image" in scene[id])
for graphic in [text, image, panel_image]:
    check(field(graphic, "m_RaycastTarget") == "0", "Information graphics do not block world input")
font = asset_guid(ROOT / "Assets/TextMesh Pro/Fonts/EliceDigitalBaeumOTF_Regular SDF.asset.meta")
check(guid(text, "m_fontAsset") == font and guid(text, "m_sharedMaterial") == font, "Uses the project's Korean font and its material")
check(field(text, "m_enableAutoSizing") == "1", "Long unit names can fit the summary")
check(field(image, "m_PreserveAspect") == "1", "Unit and tile images retain their proportions")

child_rects = []
for graphic in [image, text]:
    go_id = ref(graphic, "m_GameObject")
    go = scene[go_id]
    rect = next(scene[id] for id in re.findall(r"component: \{fileID: (\d+)\}", go) if scene[id].startswith("--- !u!224 "))
    child_id = re.search(r"^--- !u!224 &(\d+)", rect)[1]
    check(ref(rect, "m_Father") == rect_id, "Image and text are direct children of the requested panel")
    check(f"{{fileID: {child_id}}}" in scene[rect_id], "Panel also records the child transform")
    check(field(go, "m_IsActive") == "1", "Content child is active")
    check(vector(rect, "m_AnchorMin") == {"x": 0, "y": 0.5} and vector(rect, "m_AnchorMax") == {"x": 0, "y": 0.5}, "Content uses fixed left anchors")
    child_rects.append(rect)

size = vector(scene[rect_id], "m_SizeDelta")
regions = []
for rect in child_rects:
    position = vector(rect, "m_AnchoredPosition")
    child_size = vector(rect, "m_SizeDelta")
    pivot = vector(rect, "m_Pivot")
    left = position["x"] - child_size["x"] * pivot["x"]
    bottom = size["y"] / 2 + position["y"] - child_size["y"] * pivot["y"]
    check(left >= 0 and left + child_size["x"] <= size["x"], "Content fits panel width")
    check(bottom >= 0 and bottom + child_size["y"] <= size["y"], "Content fits panel height")
    regions.append((left, left + child_size["x"]))
check(regions[0][1] < regions[1][0], "Image and information text do not overlap")
print("PASS scene: " + "/".join(path))
print(f"PASS: {checks} scene wiring/layout assertions")
