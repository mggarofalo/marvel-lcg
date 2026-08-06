"""Drives whole games with the bot device and writes the resulting scene files.

`Game.GameRun` waits for a browser to ask for a new game. Headless there is
nobody to ask, so the runner takes the place of that outer loop: it builds a
`NewGameDescriptor` exactly like the `/new` route does, runs the same
setup/loop/game-over sequence `GameRun` runs, and saves the scene.

Everything below the `NewGameDescriptor` — scene construction, the phase loop,
`Controller.ChoiceOne`, CRC recording, `replay.Push` — is untouched engine code.
The saved file is an ordinary replay and loads through the ordinary replay path.

Usage:

    python main.py -device bot
    python main.py -device bot -bot_scenario klaw -bot_heroes she_hulk -bot_seed 7
    python main.py -device bot -bot_policy random -bot_policy_seed 3 -bot_games 20
"""

from core import *
from engine.config import ConfigVariables
from engine.file import FileManager
from engine.log import Log
from engine.device.manager.bot.manager import BotDeviceManager
from engine.device.manager.bot.policy import BotStuck
from game.game_run.game_new import NewGameDescriptor

CATEGORY_NAME = "BOT"

BOT_SCENARIO            = ConfigVariables.Str('bot_scenario', "rhino")
BOT_HEROES              = ConfigVariables.ListStr('bot_heroes', ["spider_man"])
BOT_ENCOUNTER_SETS      = ConfigVariables.ListStr('bot_encounter_sets', [])
BOT_RULES               = ConfigVariables.ListStr('bot_rules', [])

# -1 asks the engine for a random seed, which makes the run non-reproducible.
BOT_SEED                = ConfigVariables.Int('bot_seed', 1)
BOT_GAMES               = ConfigVariables.Int('bot_games', 1)

BOT_SAVE                = ConfigVariables.Bool('bot_save', True)
BOT_SAVE_FOLDER         = ConfigVariables.Folder('bot_save_folder', "")
BOT_CONTINUE_ON_ERROR   = ConfigVariables.Bool('bot_continue_on_error', False)

# Replay each saved scene straight back through the engine's own test path and
# assert the per-step state digest matches. Doubles the run time.
BOT_VERIFY              = ConfigVariables.Bool('bot_verify', False)

# Guard against `SetGameOver()` looping (it returns False to re-run after undo).
MAX_GAME_OVER_RETRIES = 8

class BotRunner:

    ################################################################################
    #
    @staticmethod
    def Run(game: 'Game') -> bool:
        """Play `bot_games` games. Returns False if any game failed."""
        device_manager = game.controller_manager.device_manager
        if not isinstance(device_manager, BotDeviceManager):
            Log.Assert(CATEGORY_NAME, f"BotRunner needs the bot device, got {type(device_manager).__name__}")
            return False

        total = max(1, BOT_GAMES.value)
        if BOT_SEED.value < 0:
            Log.Warn(CATEGORY_NAME, "bot_seed is negative: the engine will pick a random seed and the run will not be reproducible")

        all_ok = True
        for index in range(total):
            if not BotRunner.RunOne(game, device_manager, index, total):
                all_ok = False
                if not BOT_CONTINUE_ON_ERROR.value:
                    break

        return all_ok

    ################################################################################
    #
    @staticmethod
    def RunOne(game: 'Game', device_manager: 'BotDeviceManager', index: int, total: int) -> bool:
        seed = BotRunner.GetSeed(index)

        Log.Info(CATEGORY_NAME,
            f"Game {index + 1}/{total}: scenario={BOT_SCENARIO.value} "
            f"heroes={BOT_HEROES.value} seed={seed} policy={device_manager.policy.name}")

        try:
            device_manager.BeginGame(seed)
            game.NewGame(BotRunner.BuildDescriptor(seed))

            for _ in range(MAX_GAME_OVER_RETRIES):
                if game.GameSetup():
                    game.GameLoop()
                if game.SetGameOver():
                    break
            else:
                Log.Assert(CATEGORY_NAME, "Game did not finish after repeated restarts")
                return False

        except BotStuck as exc:
            Log.Assert(CATEGORY_NAME, f"Policy is stuck: {exc}")
            return False
        except Exception as exc:
            Log.FailedTrace(CATEGORY_NAME, exc)
            return False

        return BotRunner.Finish(game, device_manager)

    @staticmethod
    def Finish(game: 'Game', device_manager: 'BotDeviceManager') -> bool:
        world = game.world
        steps = game.controller_manager.replay.current_step_id

        if world and world.game_over.reason:
            outcome = world.game_over.reason
        else:
            outcome = "Unknown"

        Log.Info(CATEGORY_NAME, f"Finished after {steps} steps ({device_manager.decision_count} decisions): {outcome}")

        if device_manager.stopped_on_max_steps:
            Log.Assert(CATEGORY_NAME, "Game was cut short by bot_max_steps")
            return False

        if not BOT_SAVE.value:
            return True

        path = BotRunner.SaveScene(game)
        if not path:
            Log.Assert(CATEGORY_NAME, "Failed to save the scene")
            return False

        if BOT_VERIFY.value:
            return BotRunner.Verify(game, path)

        return True

    @staticmethod
    def Verify(game: 'Game', path: str) -> bool:
        """Replay a saved scene through the engine's own replay/oracle path.

        This is the same `TestRun` the `/T` debug command drives: every recorded
        input is re-executed and `World.CalculateCRC()` is compared against the
        digest stored with that step, printing a key-by-key diff on mismatch.
        """
        from game.test import Test
        from game.test.test_run import TestRun

        Log.Info(CATEGORY_NAME, f"Verifying {path}")

        Test.is_in_test = True
        Test.test_cases = [path]
        try:
            # `TestRun.Run` reports "Fail" through the log rather than its return
            # value, so a clean run is "it completed AND logged no error".
            completed = TestRun.Run(game, [path], do_save=False)
            passed = completed and not Log.HasError(error=True)
        finally:
            TestRun.RunEnd(game, False, True)

        if not passed:
            Log.Assert(CATEGORY_NAME, f"Replay verification failed: {path}")
        return passed

    @staticmethod
    def SaveScene(game: 'Game') -> str|None:
        name = None
        folder = BOT_SAVE_FOLDER.value
        if folder:
            name = FileManager.JoinPath(folder, f'{game.scene.GetSaveFileName()}.json')
        return game.session.SaveScene(name, delete_old=False)

    ################################################################################
    #
    @staticmethod
    def GetSeed(index: int) -> int:
        base = BOT_SEED.value
        if base < 0:
            # Let `GameSession.GameSetup` pick one.
            return -1
        return base + index

    @staticmethod
    def BuildDescriptor(seed: int) -> 'NewGameDescriptor':
        """Same shape the `/new` route builds from the browser's new-game form."""
        encounter_sets = BOT_ENCOUNTER_SETS.value[:]

        return NewGameDescriptor(
            campaign_json       = BotRunner.ReadJson('Campaign', BOT_SCENARIO.value),
            # None means "use the scenario's own modular sets".
            encounter_set_names = Cast(Any, encounter_sets if encounter_sets else None),
            hero_json           = [BotRunner.ReadJson('Hero', hero) for hero in BOT_HEROES.value],
            seed                = seed,
            timeout             = 0,
            challenges          = [],
            rules               = BOT_RULES.value[:],
            campaign_log        = {},
        )

    @staticmethod
    def ReadJson(load_type: 'FileManager.JsonType', name: str) -> str:
        file_path = FileManager.FindJsonPath(load_type, name)
        assert file_path, f"{load_type} {name!r} not found"
        with FileManager.OpenFile(file_path, read=True) as file:
            return file.Read()
