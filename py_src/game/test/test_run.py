from core import *
from core.lib.time import Time
from engine.log import Log
from engine.config import ConfigVariables
from game import *

CATEGORY_NAME = "TEST"

BREAK_BEFORE_SAVE_GAME = ConfigVariables.Bool('break_before_save_game', True)

class TestRun:

    @staticmethod
    def Run(game: Game, test_cases: List[str], *, do_save: bool=False) -> bool:
        from game.scene import SceneLoader
        from game.test import Test

        total_cases_len = len(test_cases)

        Log.Info(CATEGORY_NAME, "--- Test Start ---")

        @dataclass
        class RunTime:
            name: str
            time: float
        run_time_list: List[RunTime] = []

        start = Time.GetTime()

        past_id = 0
        case_id = 0
        for case_path in test_cases:
            case_id += 1

            Log.Print(f'Test File: {case_path}')

            # del game.session.world
            del game.session.scene

            scene = SceneLoader.Load(case_path)
            assert scene
            scene.HackTestRule()
            game.session.SetScene(scene, 'InTesting') # for update comment
            game.GameSetup()

            game.GameLoop()
            Log.Test("\r")

            if not game.state.exit_state.is_normal_exit:
                return False

            input_len = len(game.scene.inputs)
            ave_time = Log.GetGameTime() / input_len

            # game.state.SetStartState('InTesting') # For hide coverage report
            # game.SetGameOver() # We can skip this if we don't need to save the statistics

            text = f"Average Time: {ave_time:.3f}"
            run_time_list.append(RunTime(case_path, ave_time))

            if ave_time > 0.1:
                Log.Warn(CATEGORY_NAME, text)
            else:
                Log.Print(CATEGORY_NAME, text)

            if len(scene.inputs) != len(game.controller_manager.replay.history_inputs):
                # # Hack
                # if len(scene.inputs) == len(HistoryInput.history_inputs) -1:
                #     pass
                # else:
                Log.Assert(CATEGORY_NAME, f"Input len error, hope: {len(scene.inputs)}, get: {len(game.controller_manager.replay.history_inputs)}")

            if not Test.IsInTesting():
                return False

            def check_is_pass() -> bool:
                if do_save:
                    if Log.HasError(error=True):
                        if BREAK_BEFORE_SAVE_GAME.value:
                            Debug.DebugBreak()
                        game.session.SaveScene(delete_old=True)
                        return False
                    elif Log.HasError("VERSION", warn=True):
                        game.session.SaveScene(delete_old=True)
                        return True
                    return True
                else:
                    return not Log.HasError(error=True)

            if check_is_pass():
                from colorama import Fore, Style
                past_id += 1
                used_time = Time.GetTime() - start
                remaining = used_time / case_id * total_cases_len
                Log.Info(CATEGORY_NAME, f"{case_id} Ok ({past_id}/{total_cases_len}) {Fore.BLACK}({used_time:.1f}/{remaining:.1f}s){Style.RESET_ALL}")
            else:
                Log.Assert(CATEGORY_NAME, f"{case_id} Fail ({past_id}/{total_cases_len})")

            # collected = gc.collect()
            # Log.Print(CATEGORY_NAME, f"Garbage collector: collected {collected} objects.")

        text = f"--- Test End --- ({past_id}/{total_cases_len})"
        if past_id == total_cases_len:
            Log.Info(CATEGORY_NAME, text)
        else:
            Log.Assert(CATEGORY_NAME, text)

        end = Time.GetTime()
        # Coverage.Report("Uncall")

        Log.Print(f"Execution time: {end - start:.2f}")

        # Print the first 10 elements
        # sorted_run_time_list = sorted(run_time_list, key=lambda x: x.time, reverse=True)
        # for run_time in sorted_run_time_list[:10]:
        #     Log.Print(f"{run_time.time:.3f}", run_time.name)
        # Log.Print("\n\n")
        return True

    @staticmethod
    def RunEnd(game: Game, success: bool, exit_test: bool):
        from game.test import Test
        from core.lib.beep import Beep
        from engine.profile import Coverage

        size = len(Test.test_cases)

        Test.Exit()
        if success:
            Beep.Success()
            if size > 20:
                Coverage.Report("Uncall")

        if exit_test:
            game.state.SetStartState('') # Not start new game
            game.controller_manager.skip.SetIsSkipping(False)

