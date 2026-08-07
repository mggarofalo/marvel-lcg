import sys
from typing import Literal
import unittest
from build import Build

class TestMain(unittest.TestCase):

    def RunCases(self, var_name: Literal["min_test_folder", "profile_folder", "all"],
                *,
                release: bool=True,
                profile: bool=False):
        if release:
            Build.release = True
        else:
            Build.release = False

        # import types
        # debug_module = types.ModuleType('core.utility.debug')
        # sys.modules['core.utility.debug'] = debug_module
        # with open('./core/utility/debug.py') as f:
        #     exec(f.read(), debug_module.__dict__)
        # IsVSDebug = debug_module.Debug.IsVSDebug
        from core.utility.debug import Debug
        IsVSDebug = Debug.IsVSDebug

        sys.argv.append("-config_files")
        sys.argv.append("launch-debug.json")

        sys.argv.append('-test')

        sys.argv.append('-test_result_file')
        sys.argv.append(f"test_{var_name}{'_release' if release else '_debug'}{'_profile' if profile else ''}{'_trace' if IsVSDebug() else '_notrace'}.log")

        sys.argv.append('-enable_profile_category')
        sys.argv.append('Test')

        from engine import Engine
        from unit_test.entry import TestEntry
        from unit_test.runner import TestRunner
        from engine.config import ConfigVariables

        ConfigVariables.Folder('min_test_folder', "./replays/min_test/")
        ConfigVariables.Folder('profile_folder', "./replays/profiles/")

        if var_name == "all":
            folder = None
        else:
            var = ConfigVariables.Find(var_name)
            assert var and isinstance(var.value, str)
            folder = var.value

        if Engine.Initialize():
            Engine.in_unit_test = True
            TestRunner.Execute(TestEntry.Test, folder, profile)
            Engine.Shutdown()

    def test_min(self):
        self.RunCases("min_test_folder")

    def test_all(self):
        self.RunCases("all")

    def test_profile_profile(self):
        self.RunCases("profile_folder", release=False, profile=True)

    def test_profile_debug(self):
        self.RunCases("profile_folder", release=False, profile=False)

    def test_profile_release(self):
        self.RunCases("profile_folder")


