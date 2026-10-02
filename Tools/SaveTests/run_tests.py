"""Run the combined real save/combat regression suite (requires .NET SDK or PASSIVE_DOTNET/PASSIVE_CSC)."""
import pathlib, runpy
runpy.run_path(str(pathlib.Path(__file__).resolve().parents[1] / 'PassiveTests' / 'run_tests.py'), run_name='__main__')
