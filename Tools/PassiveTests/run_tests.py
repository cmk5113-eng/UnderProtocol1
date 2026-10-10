"""Compile real combat/save/hover scripts against small Unity doubles, then run behavioural scenarios.
Set PASSIVE_DOTNET and PASSIVE_CSC for a runtime+Roslyn installation, or install a .NET SDK.
This checks C# and logic; it is not a Unity import, Editor or Play Mode test.
"""
import json, os, pathlib, re, shutil, subprocess, tempfile
root=pathlib.Path(__file__).resolve().parents[2]
dotnet=os.environ.get('PASSIVE_DOTNET') or shutil.which('dotnet')
if not dotnet: raise SystemExit('A .NET SDK or PASSIVE_DOTNET + PASSIVE_CSC is required.')
env=dict(os.environ, DOTNET_SYSTEM_GLOBALIZATION_INVARIANT='1', DOTNET_ROLL_FORWARD='Major')
csc=os.environ.get('PASSIVE_CSC')
if not csc:
    sdks=subprocess.check_output([dotnet,'--list-sdks'],env=env,text=True).strip().splitlines()
    if not sdks: raise SystemExit('Set PASSIVE_CSC to the Roslyn csc.dll path.')
    version,location=re.match(r'(\S+) \[(.*)\]',sdks[-1]).groups()
    csc=str(pathlib.Path(location)/version/'Roslyn/bincore/csc.dll')
runtimes=subprocess.check_output([dotnet,'--list-runtimes'],env=env,text=True).splitlines()
version,location=[re.match(r'Microsoft.NETCore.App (\S+) \[(.*)\]',s).groups() for s in runtimes if s.startswith('Microsoft.NETCore.App ')][-1]
framework=pathlib.Path(location)/version
sources=[
 'Assets/3.Scripts/ScriptableObjects/Skill/SkillList.cs',
 'Assets/3.Scripts/ScriptableObjects/Skill/Passive.cs',
 'Assets/3.Scripts/Systems/PassiveGeometry.cs',
 'Assets/3.Scripts/Systems/BattlePassiveSystem.cs',
 'Assets/3.Scripts/Systems/BattleFieldEffectSystem.cs',
 'Assets/3.Scripts/Systems/BattleTileOccupancy.cs',
 'Assets/3.Scripts/Objects/skills/ExecuteSkill.cs',
 'Assets/3.Scripts/Objects/skills/UseSkill.cs',
 'Assets/3.Scripts/Objects/characters/CharacterBase.cs',
 'Assets/3.Scripts/Objects/characters/CharacterData.cs',
 'Assets/3.Scripts/Objects/Enemy/MonsterBase.cs',
 'Assets/3.Scripts/ScriptableObjects/MonsterData.cs',
 'Assets/3.Scripts/Objects/CharacterModule/CharacterModule.cs',
 'Assets/3.Scripts/Objects/CharacterModule/MovementModule.cs',
 'Assets/3.Scripts/Objects/CharacterModule/MoveTileModule.cs',
 'Assets/3.Scripts/Managers/BattleManager.cs',
 'Assets/3.Scripts/Managers/ModeManager.cs',
 'Assets/3.Scripts/Managers/SelectionManager.cs',
 'Assets/3.Scripts/UIs/Functions/Information/StageUIController.cs',
 'Assets/3.Scripts/UIs/Functions/Information/MonsterHealthBar.cs',
 'Assets/3.Scripts/UIs/Functions/Information/UI_SkillTooltip.cs',
 'Assets/3.Scripts/UIs/Functions/Information/UI_TargetHoverInfo.cs',
 'Assets/3.Scripts/UIs/UIBase.cs',
 'Assets/3.Scripts/UIs/OpenableUIBase.cs',
 'Assets/3.Scripts/Interfaces/IOpenable.cs',
 'Assets/3.Scripts/UIs/Map/TileManager.cs',
 'Assets/3.Scripts/Objects/SaveData.cs',
 'Assets/3.Scripts/Systems/SaveCatalog.cs',
 'Assets/3.Scripts/Systems/CharacterLoadoutPersistence.cs',
 'Assets/3.Scripts/Managers/SaveManager.cs',
 'Assets/3.Scripts/Managers/ProgressManager.cs',
 'Assets/3.Scripts/UIs/Windows/UI_Hero.cs',
 'Assets/3.Scripts/UIs/Functions/Information/UI_skillSlotInfo.cs',
 'Assets/3.Scripts/Objects/Slots/SkillSlot.cs',
 'Assets/Editor/SaveCatalogBuilder.cs',
 'Tools/PassiveTests/UnityStubs.cs',
 'Tools/PassiveTests/ProjectStubs.cs',
 'Tools/PassiveTests/Scenarios.cs',
 'Tools/PassiveTests/FieldEffectScenarios.cs',
 'Tools/PassiveTests/MultiCellMonsterScenarios.cs',
 'Tools/PassiveTests/CellShieldScenarios.cs',
 'Tools/PassiveTests/HoverScenarios.cs',
 'Tools/PassiveTests/StageUIScenarios.cs',
 'Tools/SaveTests/SaveApiStubs.cs',
 'Tools/SaveTests/EditorApiStubs.cs',
 'Tools/SaveTests/SaveScenarios.cs',
]
with tempfile.TemporaryDirectory(prefix='passive-tests-') as directory:
    out=pathlib.Path(directory)
    args=['/nologo','/noconfig','/nostdlib+','/langversion:9.0','/target:exe','/nowarn:0169,0414,0067,0649','/out:'+str(out/'Tests.dll')]
    args += ['/reference:'+str(p) for p in framework.glob('*.dll')]
    args += [str(root/s) for s in sources]
    compiled=subprocess.run([dotnet,csc,*args],env=env)
    if compiled.returncode: raise SystemExit(compiled.returncode)
    (out/'Tests.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'netcoreapp'+'.'.join(version.split('.')[:2]),'framework':{'name':'Microsoft.NETCore.App','version':version}}}))
    raise SystemExit(subprocess.run([dotnet,str(out/'Tests.dll')],env=env,cwd=root).returncode)

