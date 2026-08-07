from core import *
from engine import Engine
from engine.log import Log
from engine.profile import Profile
from engine.file import FileManager
from build import Build
from game.test.test_run import TestRun
from game.test import Test

class TestEntry:

    @staticmethod
    def GetFolderTestCase(folder: str) -> List[str]:
        all_cases: List[str] = FileManager.ListFiles(folder)
        all_cases = [case for case in all_cases if case.endswith(".json")]
        return all_cases

    @staticmethod
    def Test(folder: str|None, do_profile: bool=False):
        # Log.Print(f"{folder=} {do_profile=}")

        if folder == None:
            all_cases = Test.GetTestCases(None)
        else:
            all_cases = TestEntry.GetFolderTestCase(folder)

        Test.is_in_test = True # We do need this

        if do_profile:
            assert Build.release == False

        game = Engine.game

        if do_profile:
            Profile.Run(
                TestRun.Run,
                game, all_cases,
                profile_name="Test", profile_category="Test")
            profiler = Profile.Get("Test", category="Test")
            Log.Print(profiler.Print())
        else:
            TestRun.Run(game, all_cases)

        TestRun.RunEnd(game, True, True)

        Log.Print(Tracker.PrintStats())
        world = game.world
        assert world
        Log.Print(f"{len(world.object_manager.card_dict)=}")
        Log.Print(f"{len(world.object_manager.effect_dict)=}")

