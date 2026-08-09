from core import *
from engine.device.manager import DeviceManager

from game import *
from game.statistics.game_statistics import GameStatistics
from build import Build
from engine.log import Log
from engine.lib import TransText, ImageCreator, Ver
from engine.user.user_info import UserInfo
from engine.task import TaskManager
from engine.job import JobManager
from engine.device.manager.base import DeviceManager
from engine.lib.check_new_version import CheckForNewVersion
from engine.config import ConfigVariables
from engine.controller.module.invariants import CHECK_INVARIANTS

CATEGORY_NAME = "ENGINE"

CHECK_FOR_NEW_VERSION_ON_STARTUP    = ConfigVariables.Bool('check_for_new_version_on_startup', False)
PRINT_MY_USER_FINGERPRINT           = ConfigVariables.Bool('print_my_user_fingerprint', False)

DEVICE              = ConfigVariables.Str('device', "web")

PROFILE_FOLDER      = ConfigVariables.Folder('profile_folder')
TEST_ALL            = ConfigVariables.Bool('test_all', False)
EDITOR              = ConfigVariables.Bool('editor', True)

ConfigVariables.SetGroupArgs('test', "-device -no_editor -no_statistics -test_result_file test_results.txt -hidden_log_categories CONTROLLER WEB VERSION STATISTICS")
ConfigVariables.SetGroupArgs('bot', "-device bot -no_editor -hidden_log_categories CONTROLLER WEB VERSION STATISTICS")

class Engine:

    device_manager: 'DeviceManager'

    game: 'Game'
    statistics: 'GameStatistics'

    has_crashed = False
    in_unit_test = False

    # Process exit code. Stays 0 for the interactive devices; the headless bot
    # sets it so CI can tell a failed run from a successful one.
    exit_code = 0

    @staticmethod
    def Initialize() -> bool:
        Ver.Initialize()
        game_name = f'Marvel LCG {Ver.ui_version_str}'
        System.SetTitle(game_name)

        # ConfigVariables.Test()

        def initialize() -> bool:
            Log.Info(CATEGORY_NAME, game_name)

            ConfigVariables.Initialize()

            JobManager.Initialize()
            TaskManager.Initialize()

            if CHECK_FOR_NEW_VERSION_ON_STARTUP.value:
                async def check_new_version() -> None:
                    CheckForNewVersion.Check()

                job = JobManager.AddJob(check_new_version, name="Check Version")

                if CHECK_FOR_NEW_VERSION_ON_STARTUP.is_from_command_line:
                    JobManager.WaitForAllJobsToComplete(job)
                    return False

            UserInfo.Initialize()
            if PRINT_MY_USER_FINGERPRINT.value:
                Log.Info(CATEGORY_NAME, f"{UserInfo.fingerprint=}")
                if PRINT_MY_USER_FINGERPRINT.is_from_command_line:
                    return False

            TransText.Initialize()
            ImageCreator.Initialize()

            from cards.database import CardsDB
            CardsDB.Initialize()

            if Build.release or DEVICE.value == "bot":
                EDITOR.value = False

            # Self-play watches itself unless told otherwise: a headless run
            # with nothing checking the state is the case MARVEL-11 exists to
            # fix. It costs roughly 40% of a bot game's wall time, so corpus
            # generation turns it off with `-no_check_invariants`.
            #
            # Forced here rather than by putting `-check_invariants` in the
            # `bot` arg group, which looks equivalent and is not: expanding a
            # group calls `ConfigVariables.InitVariable` for each of its keys
            # immediately, stamping `set_from = "CommandLine"`. The real command
            # line is applied after that loop, and `SetValue` returns early when
            # `set_from` already matches -- so `-no_check_invariants` was
            # silently discarded and the switch could not be turned off. The
            # root cause is MARVEL-64; this is the workaround.
            if DEVICE.value == "bot" and CHECK_INVARIANTS.set_from == "DefaultValue":
                CHECK_INVARIANTS.value = True

            if EDITOR.value:
                from editor.editor import Editor
                Editor.Initialize()

            # for x in range(50020, 50033):
            #     y = CardsDB.papers[str(x)]
            #     text = y.text
            #     text = text.replace("\r", "")
            #     text = text.replace("\n", "\\n")
            #     text = text.replace('"', '\\"')
            #     print(f'"{x}": "{text}",')

            Engine.statistics = GameStatistics()
            Engine.statistics.Load()

            device = DeviceManager
            if DEVICE.value == "web":
                from engine.device.manager.web.manager import WebDeviceManager
                device = WebDeviceManager
            elif DEVICE.value == "bot":
                from engine.device.manager.bot.manager import BotDeviceManager
                device = BotDeviceManager
            else:
                from engine.device.manager.key.manager import KeyDeviceManager
                device = KeyDeviceManager

            Engine.device_manager = device()
            Engine.game = Game(Engine.statistics, Engine.device_manager)
            return True

        if Build.release:
            try:
                return initialize()
            except Exception as exc:
                Log.OnCrash(CATEGORY_NAME, exc, "", None)
                return False
        else:
            return initialize()

    @staticmethod
    def EngineRun() -> None:
        if DEVICE.value == "bot":
            from engine.device.manager.bot.runner import BotRunner
            Engine.exit_code = 0 if BotRunner.Run(Engine.game) else 1
            return

        if Build.release:
            try:
                Engine.game.GameRun()
            except Exception as exc:
                Log.OnCrash(CATEGORY_NAME, exc, "", None)
                Log.SaveCrashLog()
        else:
            if TEST_ALL.value:
                from unit_test.entry import TestEntry
                from unit_test.runner import TestRunner
                TestRunner.Execute(TestEntry.Test, True)
                return
            elif PROFILE_FOLDER.is_initialized and PROFILE_FOLDER.value:
                from unit_test.entry import TestEntry
                from unit_test.runner import TestRunner
                TestRunner.Execute(TestEntry.Test, PROFILE_FOLDER.value, True)
                return

            if EDITOR.value:
                from editor.editor import Editor
                Editor.EditorRun()
            Engine.game.GameRun()

    @staticmethod
    def Shutdown():
        Log.Print("\n--- Engine Shutdown ---")
        Engine.game.Shutdown()

        if EDITOR.value:
            from editor.editor import Editor
            Editor.Shutdown()

        JobManager.Shutdown()
        TaskManager.Shutdown()

        # System.Pause()
        return

    ################################################################################
    #
    @staticmethod
    def SaveCrash():
        if not Engine.has_crashed:
            Engine.game.session.SaveScene(f'./crash.json', delete_old=False)
            Engine.has_crashed = True
        if Engine.in_unit_test:
            exit(-1)

import builtins
setattr(builtins, "DebugBreak", lambda: Debug.DebugBreak(True))
# You can call `DebugBreak()` any where without import

