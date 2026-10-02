"""Verify shipped Unity YAML references and all authored generic loadout defaults without Unity."""
import pathlib, re
root = pathlib.Path(__file__).resolve().parents[2]
def guid(path):
    return re.search(r'^guid: ([a-f0-9]{32})', path.read_text(), re.M)[1]
catalog = (root / 'Assets/Resources/SaveCatalog.asset').read_text()
script_guid = guid(root / 'Assets/3.Scripts/Systems/SaveCatalog.cs.meta')
assert f'm_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}' in catalog
character_text, skill_text = catalog.split('  characters:\n', 1)[1].split('  skills:\n', 1)
entries = {}
for block in character_text.split('  - id: ')[1:]:
    character_id, body = block.split('\n', 1)
    assert character_id not in entries
    assert f'character: {{fileID: 11400000, guid: {character_id}, type: 2}}' in body
    entries[character_id] = body
skill_ids = re.findall(r'^  - id: ([a-f0-9]{32})\n    skill: \{fileID: 11400000, guid: \1, type: 2\}', skill_text, re.M)
assert len(skill_ids) == len(set(skill_ids))
skill_dir = root / 'Assets/1.Datas/Original/ScriptableObjects/Globals/SkillDatas'
assert set(skill_ids) == {guid(p) for p in skill_dir.rglob('*.asset.meta')}
characters = list((root / 'Assets/1.Datas/Original/ScriptableObjects/Character').glob('*.asset'))
assert set(entries) == {guid(pathlib.Path(str(p) + '.meta')) for p in characters}
slot_count = 0
for path in characters:
    source = path.read_text()
    body = entries[guid(pathlib.Path(str(path) + '.meta'))]
    for field, default_field, count in [('active', 'defaultActive', 2), ('passive', 'defaultPassive', 4)]:
        expected = re.search(r'^  ' + field + r':\n((?:  - .*\n)*)', source, re.M)[1]
        actual = re.search(r'^    ' + default_field + r':\n((?:    - .*\n)*)', body, re.M)[1]
        expected = [s.strip() for s in expected.splitlines()]
        actual = [s.strip() for s in actual.splitlines()]
        assert expected == actual and len(actual) == count, (path.name, field)
        assert all(g in skill_ids for s in actual for g in re.findall(r'guid: ([a-f0-9]{32})', s))
        slot_count += len(actual)
print(f'PASS catalog: {len(characters)} characters / {slot_count} default slots / {len(skill_ids)} skill GUID references')
