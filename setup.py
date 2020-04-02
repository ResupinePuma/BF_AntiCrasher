import sys
from cx_Freeze import setup, Executable

# Dependencies are automatically detected, but it might need fine tuning.
build_exe_options = {"packages": ["frostbite_rcon_utils", "threading", "requests", "multiprocessing"]}

setup(  name = "BF Tool",
        version = "0.1",
        description = "My Phonebook application!",
        options = {"build_exe": build_exe_options},
        executables = [Executable("app.py")])