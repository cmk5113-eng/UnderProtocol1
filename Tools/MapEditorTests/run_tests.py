"""Compile real map editor + wave scripts and run data workflow regressions with API doubles.
Requires a .NET SDK; override MAP_EDITOR_DOTNET/MAP_EDITOR_CSC for a custom installation.
This is not a Unity import, render, native serialization, or Play Mode test.
"""
import json
import os
import pathlib
import re
import shutil
import subprocess
import tempfile

root = pathlib.Path(__file__).resolve().parents[2]
dotnet = os.environ.get("MAP_EDITOR_DOTNET") or shutil.which("dotnet")
if not dotnet:
    raise SystemExit("Install a .NET SDK or set MAP_EDITOR_DOTNET.")
env = dict(os.environ, DOTNET_SYSTEM_GLOBALIZATION_INVARIANT="1", DOTNET_ROLL_FORWARD="Major")
csc = os.environ.get("MAP_EDITOR_CSC")
if not csc:
    sdks = subprocess.check_output([dotnet, "--list-sdks"], text=True, env=env).strip().splitlines()
    if not sdks:
        raise SystemExit("Set MAP_EDITOR_CSC to a Roslyn csc.dll path.")
    version, location = re.match(r"(\S+) \[(.*)\]", sdks[-1]).groups()
    csc = str(pathlib.Path(location) / version / "Roslyn/bincore/csc.dll")
runtimes = subprocess.check_output([dotnet, "--list-runtimes"], text=True, env=env).splitlines()
version, location = [re.match(r"Microsoft.NETCore.App (\S+) \[(.*)\]", line).groups()
                     for line in runtimes if line.startswith("Microsoft.NETCore.App ")][-1]
framework = pathlib.Path(location) / version
sources = [
    "Assets/3.Scripts/Managers/WaveManager.cs",
    "Assets/3.Scripts/Managers/TileMapManager.cs",
    "Assets/3.Scripts/ScriptableObjects/MonsterData.cs",
    "Assets/3.Scripts/Objects/Enemy/MonsterBase.cs",
    "Assets/3.Scripts/Systems/WaveData.cs",
    "Assets/3.Scripts/Systems/StageMapData.cs",
    "Assets/3.Scripts/Systems/StageMapBinding.cs",
    "Assets/3.Scripts/Systems/WaveLoader.cs",
    "Assets/3.Scripts/Systems/WaveSetter.cs",
    "Assets/3.Scripts/UIs/Functions/Buttons/StageButtonImageController.cs",
    "Assets/Editor/StageMapEditor.cs",
    "Assets/Editor/StageMapEditor.Monsters.cs",
    "Assets/Editor/WaveManagerEditor.cs",
    "Tools/MapEditorTests/ApiDoubles.cs",
    "Tools/MapEditorTests/Scenarios.cs",
]
with tempfile.TemporaryDirectory(prefix="map-editor-tests-") as directory:
    output = pathlib.Path(directory)
    args = ["/nologo", "/noconfig", "/nostdlib+", "/langversion:9.0", "/define:UNITY_EDITOR", "/target:exe",
            "/nowarn:0169,0414,0067,0649", "/out:" + str(output / "Tests.dll")]
    args += ["/reference:" + str(path) for path in framework.glob("*.dll")]
    args += [str(root / path) for path in sources]
    compiled = subprocess.run([dotnet, csc, *args], env=env)
    if compiled.returncode:
        raise SystemExit(compiled.returncode)
    (output / "Tests.runtimeconfig.json").write_text(json.dumps({"runtimeOptions": {
        "tfm": "netcoreapp" + ".".join(version.split(".")[:2]),
        "framework": {"name": "Microsoft.NETCore.App", "version": version},
    }}))
    raise SystemExit(subprocess.run([dotnet, str(output / "Tests.dll")], env=env, cwd=root).returncode)
