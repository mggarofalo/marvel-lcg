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

EDITOR              = ConfigVariables.Bool('editor', True)

# Replay a folder of saved scenes and check every recorded step digest. A mode
# rather than a device: it picks its own device, because the only thing it needs
# from one is that it never blocks. See `game/test/verify.py` and MARVEL-28.
VERIFY_REPLAYS          = ConfigVariables.Bool('verify_replays', False)
VERIFY_FOLDERS          = ConfigVariables.Folders('verify_folders', [])
VERIFY_REPORT_FILE      = ConfigVariables.File('verify_report_file', "")
VERIFY_ALLOW_INCOMPLETE = ConfigVariables.Bool('verify_allow_incomplete', False)
VERIFY_ALLOW_CONFIG_DRIFT = ConfigVariables.Bool('verify_allow_config_drift', False)

ConfigVariables.SetGroupArgs('test', "-verify_replays -no_editor -no_statistics -hidden_log_categories CONTROLLER WEB VERSION STATISTICS")
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

            # `-verify_replays` names a job, not a device. Resolve it to one
            # here so a verification run cannot be started against a device that
            # blocks: `-test` used to expand to a bare `-device`, which landed
            # on `KeyDeviceManager` and waited for a keypress nobody was there
            # to make. See MARVEL-28.
            if Engine.IsVerifyingReplays():
                DEVICE.value = "verify"

            if Build.release or DEVICE.value in ("bot", "verify"):
                EDITOR.value = False

            # Self-play watches itself unless told otherwise: a headless run
            # with nothing checking the state is the case MARVEL-11 exists to
            # fix. It costs roughly 40% of a bot game's wall time, so corpus
            # generation turns it off with `-no_check_invariants`.
            #
            # Forced from the resolved device rather than by putting
            # `-check_invariants` in the `bot` arg group. That started as a
            # workaround -- a flag inside a group could not be turned off again
            # -- and MARVEL-64 has fixed that, so the group would work now.
            # It is kept because the two are not equivalent: `-device bot` is a
            # documented way to run the bot and expands no group, so a group
            # entry would leave that spelling unwatched. Keying off the device
            # covers every way of selecting it.
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
            elif DEVICE.value == "verify":
                from engine.device.manager.verify.manager import VerifyDeviceManager
                device = VerifyDeviceManager
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
    def IsVerifyingReplays() -> bool:
        """Whether this process was asked to replay a folder rather than play."""
        return bool(VERIFY_REPLAYS.value or [x for x in VERIFY_FOLDERS.value if x])

    @staticmethod
    def EngineRun() -> None:
        if DEVICE.value == "bot":
            from engine.device.manager.bot.runner import BotRunner
            Engine.exit_code = 0 if BotRunner.Run(Engine.game) else 1
            return

        if Engine.IsVerifyingReplays():
            from game.test.verify import ReplayVerifier
            ok = ReplayVerifier.Run(
                Engine.game, VERIFY_FOLDERS.value,
                report_path=VERIFY_REPORT_FILE.value,
                allow_incomplete=VERIFY_ALLOW_INCOMPLETE.value,
                allow_config_drift=VERIFY_ALLOW_CONFIG_DRIFT.value)
            Engine.exit_code = 0 if ok else 1
            return

        if Build.release:
            try:
                Engine.game.GameRun()
            except Exception as exc:
                Log.OnCrash(CATEGORY_NAME, exc, "", None)
                Log.SaveCrashLog()
        else:
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

