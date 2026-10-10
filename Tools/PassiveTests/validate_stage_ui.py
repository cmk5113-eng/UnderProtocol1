"""Validate saved wave/enemy counters and the five stage skill click events.
Checks scene YAML references, not Unity import or rendering.
"""
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[2]
PREFAB = ROOT / "Assets/1.Datas/Original/Prefabs/Globals/UIs(prefabs)/S_Stage.prefab"
GUID = re.search(r"^guid: (\w+)$", PREFAB.with_suffix(".prefab.meta").read_text(), re.M)[1]
TMP_GUID = "f4688fdb7df04437aeb418b961361dc5"
IMAGE_GUID = "fe87c0e1cc204ed48ad3b37840f39efc"
STAGE_CONTROLLER = "3658307032642652263"
STAGE_INFO = "201021953953722696"


def documents(source):
    entries = list(re.finditer(r"^--- !u!\d+ &(\d+)(?: stripped)?\n[\s\S]*?(?=^--- !u!|\Z)", source, re.M))
    assert len(entries) == len({m[1] for m in entries}), "duplicate fileID"
    return {m[1]: m[0] for m in entries}


def ref(block, field):
    return re.search(r"^  " + field + r": \{fileID: (\d+)", block, re.M)[1]


for name in ["SampleScene.unity", "first.unity", "SampleScene_Tutorial.unity"]:
    scene = documents((ROOT / "Assets" / name).read_text())
    controller_id, controller = next((id, block) for id, block in scene.items()
                                    if f"m_CorrespondingSourceObject: {{fileID: {STAGE_CONTROLLER}, guid: {GUID}" in block)
    instance = ref(controller, "m_PrefabInstance")
    info_id = next(id for id, block in scene.items()
                   if f"m_CorrespondingSourceObject: {{fileID: {STAGE_INFO}, guid: {GUID}" in block
                   and ref(block, "m_PrefabInstance") == instance)
    mods = scene[instance]

    def binding(field):
        return re.search(r"target: \{fileID: " + STAGE_CONTROLLER + r", guid: " + GUID
                         + r", type: 3\}\n      propertyPath: " + re.escape(field)
                         + r"\n      value: *\n      objectReference: \{fileID: (\d+)\}", mods)[1]

    for field, node in [("currentwave", "wave"), ("currentenemy", "enemy")]:
        label_id = binding(field)
        label = scene[label_id]
        assert TMP_GUID in label, f"{node} is not TMP text"
        go = scene[ref(label, "m_GameObject")]
        assert f"  m_Name: {node}\n" in go, f"{node} has the wrong name"
        rect = next(scene[id] for id in re.findall(r"component: \{fileID: (\d+)\}", go)
                    if scene[id].startswith("--- !u!224 "))
        assert ref(rect, "m_Father") == info_id, f"{node} is outside Top/StageInfo"
        assert "  m_IsActive: 1\n" in go

    for index, method in enumerate(["UI_StartSkill1", "UI_StartSkill2", "UI_Ultimate", "UI_StartNormalSkill", "OnClickPassiveSkill"]):
        image = scene[binding(f"'skill.Array.data[{index}]'")]
        assert IMAGE_GUID in image, f"skill {index} has no icon reference"
        go = ref(image, "m_GameObject")
        button = next(block for block in scene.values() if "m_OnClick:" in block
                      and ref(block, "m_GameObject") == go and f"m_MethodName: {method}\n" in block)
        target = re.search(r"m_Target: \{fileID: (\d+)\}", button)[1]
        if index == 4:
            assert target == controller_id, "passive tooltip targets another stage UI"
        else:
            use_skill_guid = re.search(r"^guid: (\w+)$", (ROOT / "Assets/3.Scripts/Objects/skills/UseSkill.cs.meta").read_text(), re.M)[1]
            assert use_skill_guid in scene[target], f"skill {index} targets a missing UseSkill component"
    print(f"PASS {name}: Top/StageInfo/wave + enemy, five skill click targets")
