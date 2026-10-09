"""Compile the actual SkillListEditor and simulate grid clicks with minimal IMGUI doubles.
This is not a Unity import or rendered Inspector test.
"""
import json, os, pathlib, re, shutil, subprocess, tempfile

root=pathlib.Path(__file__).resolve().parents[2]
dotnet=os.environ.get('PASSIVE_DOTNET') or shutil.which('dotnet')
if not dotnet: raise SystemExit('A .NET SDK or PASSIVE_DOTNET + PASSIVE_CSC is required.')
env=dict(os.environ,DOTNET_SYSTEM_GLOBALIZATION_INVARIANT='1',DOTNET_ROLL_FORWARD='Major')
csc=os.environ.get('PASSIVE_CSC')
if not csc:
    sdk=subprocess.check_output([dotnet,'--list-sdks'],env=env,text=True).strip().splitlines()[-1]
    sdk_version,sdk_location=re.match(r'(\S+) \[(.*)\]',sdk).groups()
    csc=str(pathlib.Path(sdk_location)/sdk_version/'Roslyn/bincore/csc.dll')
runtime=subprocess.check_output([dotnet,'--list-runtimes'],env=env,text=True).splitlines()
version,location=[re.match(r'Microsoft.NETCore.App (\S+) \[(.*)\]',s).groups() for s in runtime if s.startswith('Microsoft.NETCore.App ')][-1]
framework=pathlib.Path(location)/version
sources=[
    'Assets/3.Scripts/ScriptableObjects/Skill/SkillList.cs',
    'Assets/3.Scripts/ScriptableObjects/Skill/Passive.cs',
    'Assets/3.Scripts/Systems/PassiveGeometry.cs',
    'Assets/Editor/SkillListEditor.cs',
    'Tools/PassiveTests/UnityStubs.cs',
    'Tools/SkillEditorTests/EditorApiStubs.cs',
    'Tools/SkillEditorTests/Scenarios.cs',
]
with tempfile.TemporaryDirectory(prefix='skill-editor-tests-') as directory:
    out=pathlib.Path(directory)
    args=['/nologo','/noconfig','/nostdlib+','/langversion:9.0','/define:UNITY_EDITOR','/target:exe','/out:'+str(out/'Tests.dll')]
    args+=['/reference:'+str(p) for p in framework.glob('*.dll')]
    args+=[str(root/s) for s in sources]
    compiled=subprocess.run([dotnet,csc,*args],env=env)
    if compiled.returncode: raise SystemExit(compiled.returncode)
    (out/'Tests.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'netcoreapp'+'.'.join(version.split('.')[:2]),'framework':{'name':'Microsoft.NETCore.App','version':version}}}))
    raise SystemExit(subprocess.run([dotnet,str(out/'Tests.dll')],env=env,cwd=root).returncode)
