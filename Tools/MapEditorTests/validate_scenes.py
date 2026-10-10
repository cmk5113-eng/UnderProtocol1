"""Check saved map-editor references and spawn geometry without Unity or third-party packages."""
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[2]
MANAGER = '60469b56f5749b14f9c4c7293bd92ad9'
SETTER = '95fb41412b37ff949b4b1867b4bcbc71'
BINDING = '64372562b0d34a63bbd53a842ef95f63'
TILEMAP = '1839735485'


def docs(path):
    return {int(ident): (kind, body) for kind, ident, body in re.findall(
        r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)', path.read_text(), re.M | re.S)}


def guid_refs(text):
    return re.findall(r'guid: ([a-f0-9]{32})', text)


def field(text, name):
    return re.search(r'^    ' + name + r':[^\n]*\n(.*?)(?=^    \w|\Z)', text, re.M | re.S)[1]


assets = {}
for meta in (ROOT / 'Assets').rglob('*.meta'):
    match = re.search(r'^guid: (\w+)', meta.read_text(), re.M)
    if match:
        assets[match[1]] = pathlib.Path(str(meta)[:-5])


def cells(scene_docs, ident):
    kind, body = scene_docs[ident]
    assert kind == TILEMAP
    if '  m_Tiles:' not in body:
        source = re.search(r'm_CorrespondingSourceObject: \{fileID: (-?\d+), guid: (\w+)', body)
        body = docs(assets[source[2]])[int(source[1])][1]
    return {tuple(map(int, item)) for item in re.findall(r'  - first: \{x: (-?\d+), y: (-?\d+), z: (-?\d+)\}', body)}


assert not (ROOT / 'Assets/3.Scripts/Systems/StageMapBinding.cs').exists()
assert not (ROOT / 'Assets/3.Scripts/Systems/StageMapBinding.cs.meta').exists()
for name, count in [('SampleScene.unity', 30), ('first.unity', 30), ('SampleScene_Tutorial.unity', 5)]:
    path = ROOT / 'Assets' / name
    text = path.read_text()
    assert BINDING not in text and 'StageMapBinding' not in text, name
    assert not re.search(r'propertyPath: .*monsterDatas', text), name
    scene_docs = docs(path)
    manager = next(body for kind, body in scene_docs.values() if kind == '114' and 'guid: ' + MANAGER in body)
    assert not re.search(r'^  (stages|stage\dWaves|selectedWaves|currentWave|stageDataMigrated):', manager, re.M), name
    entries = re.findall(r'^  - tilemap: \{fileID: (-?\d+)\}\n(.*?)(?=^  - tilemap:|\Z)', manager, re.M | re.S)
    assert len(entries) == count, (name, len(entries))
    targets, buttons, clear_ids = set(), set(), set()
    total_spawns, total_waves = 0, 0
    for target, entry in entries:
        target = int(target)
        assert target not in targets, (name, target)
        targets.add(target)
        terrain = cells(scene_docs, target)
        assert terrain, (name, target, 'no tiles')
        stage_id = int(re.search(r'^    stageId: (-?\d+)', entry, re.M)[1])
        for button, clear_id in re.findall(r'^    - button: \{fileID: (-?\d+)\}\n      clearId: (-?\d+)', entry, re.M):
            button = int(button)
            assert button not in buttons and 'guid: ' + SETTER in scene_docs[button][1], (name, button)
            buttons.add(button)
            clear_id = int(clear_id)
            clear_ids.add(clear_id if clear_id >= 0 else stage_id)
            assert 'm_MethodName: ChangeCurrentCharacter' not in scene_docs[button][1], (name, button)
            assert not re.search(r'^  (index|waveStageIndex|overrideWaveStageIndex|stageId|progressController):', scene_docs[button][1], re.M)
        catalog = {}
        for guid in guid_refs(field(entry, 'monsterDatas')):
            data = assets[guid].read_text()
            ident = int(re.search(r'^  id: (\d+)', data, re.M)[1])
            assert ident not in catalog, (name, target, ident)
            assert re.search(r'^  prefab: \{fileID: [1-9]\d*, guid: \w+', data, re.M), (name, ident)
            prefab_ref = re.search(r'^  prefab: \{fileID: (\d+), guid: (\w+)', data, re.M)
            prefab = assets[prefab_ref[2]]
            assert any('Assembly-CSharp::MonsterBase' in body and f'm_GameObject: {{fileID: {prefab_ref[1]}}}' in body
                       for _, body in docs(prefab).values()), (name, ident, 'no MonsterBase on prefab root')
            size = re.search(r'^  footprintSize: \{x: (\d+), y: (\d+)\}', data, re.M)
            catalog[ident] = tuple(map(int, size.groups())) if size else (1, 1)
        waves = guid_refs(field(entry, 'waves'))
        assert waves, (name, target, 'no waves')
        if stage_id == 0:
            assert len(waves) == 7 and 5 in catalog, (name, 'Stage 1 recovery')
        for guid in waves:
            wave = assets[guid].read_text()
            total_waves += 1
            occupied = set()
            for monster, x, y, z in re.findall(r'^  - monsterID: (\d+)\n    position: \{x: (-?\d+), y: (-?\d+), z: (-?\d+)\}', wave, re.M):
                monster, x, y, z = map(int, (monster, x, y, z))
                assert monster in catalog, (name, target, assets[guid].name, monster)
                width, height = catalog[monster]
                for dx in range(width):
                    for dy in range(height):
                        cell = (x + dx, y + dy, z)
                        assert cell in terrain and cell not in occupied, (name, target, assets[guid].name, cell)
                        occupied.add(cell)
                total_spawns += 1
    assert len(buttons) == 30 and clear_ids == set(range(30)), (name, 'clear IDs')
    print(f'PASS {name}: {len(entries)} maps, 30 button identities, {total_waves} waves, {total_spawns} valid spawns')
