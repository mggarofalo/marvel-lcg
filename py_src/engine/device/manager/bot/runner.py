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
from engine.device.manager.base import FabricatedInputError
from engine.device.manager.bot.manager import BOT_MAX_STEPS, BotDeviceManager
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

# Omit the wall-clock and machine metadata (`sign`, `time`, `playtime`) so the
# same seed produces a byte-identical file on any machine on any day. On by
# default: everything the bot writes is a corpus artefact. See MARVEL-27.
BOT_DETERMINISTIC_SAVE  = ConfigVariables.Bool('bot_deterministic_save', True)

# Replay each saved scene straight back through the engine's own test path and
# assert the per-step state digest matches. Doubles the run time.
BOT_VERIFY              = ConfigVariables.Bool('bot_verify', False)

# Guard against `SetGameOver()` looping (it returns False to re-run after undo).
MAX_GAME_OVER_RETRIES = 8

class BotRunner:

    # Directory the last scene was written to. The run manifest goes beside the
    # scenes it describes, and `GameSession.SaveScene` is the thing that knows
    # where that is when `bot_save_folder` is unset.
    save_folder: str = ""

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
        # Cleared per run, so a second `Run` in one process cannot write its
        # manifest into the folder the first one happened to use.
        BotRunner.save_folder = ""
        if BOT_SEED.value < 0:
            Log.Warn(CATEGORY_NAME, "bot_seed is negative: the engine will pick a random seed and the run will not be reproducible")
        if not BOT_DETERMINISTIC_SAVE.value:
            Log.Warn(CATEGORY_NAME, "bot_deterministic_save is off: saved scenes will carry wall-clock and machine metadata and will not be byte-reproducible")

        all_ok = True
        played: List[Dict[str, Any]] = []
        for index in range(total):
            record = BotRunner.RunOne(game, device_manager, index, total)
            if record is None:
                all_ok = False
                if not BOT_CONTINUE_ON_ERROR.value:
                    break
            else:
                played.append(record)

        BotRunner.WriteManifest(game, device_manager, played)

        return all_ok

    ################################################################################
    #
    @staticmethod
    def RunOne(game: 'Game', device_manager: 'BotDeviceManager', index: int, total: int) -> 'Dict[str, Any]|None':
        """Play one game. Returns its manifest record, or None if it failed."""
        seed = BotRunner.GetSeed(index)

        Log.Info(CATEGORY_NAME,
            f"Game {index + 1}/{total}: scenario={BOT_SCENARIO.value} "
            f"heroes={BOT_HEROES.value} seed={seed} policy={device_manager.policy.name}")

        try:
            device_manager.BeginGame(seed)
            game.NewGame(BotRunner.BuildDescriptor(seed))
            if not BotRunner.CheckNoTimeout(game, device_manager, "after NewGame"):
                return None

            for _ in range(MAX_GAME_OVER_RETRIES):
                if game.GameSetup():
                    if not BotRunner.CheckNoTimeout(game, device_manager, "after GameSetup"):
                        return None
                    game.GameLoop()
                if game.SetGameOver():
                    break
            else:
                Log.Assert(CATEGORY_NAME, "Game did not finish after repeated restarts")
                return None

        except FabricatedInputError as exc:
            Log.Assert(CATEGORY_NAME, f"Refusing to record a fabricated input: {exc}")
            return None
        except BotStuck as exc:
            Log.Assert(CATEGORY_NAME, f"Policy is stuck: {exc}")
            return None
        except Exception as exc:
            Log.FailedTrace(CATEGORY_NAME, exc)
            return None

        return BotRunner.Finish(game, device_manager, seed)

    @staticmethod
    def Finish(game: 'Game', device_manager: 'BotDeviceManager', seed: int) -> 'Dict[str, Any]|None':
        world = game.world
        steps = game.controller_manager.replay.current_step_id

        if world and world.game_over.reason:
            outcome = world.game_over.reason
        else:
            outcome = "Unknown"

        Log.Info(CATEGORY_NAME, f"Finished after {steps} steps ({device_manager.decision_count} decisions): {outcome}")

        if device_manager.stopped_on_max_steps:
            Log.Assert(CATEGORY_NAME, "Game was cut short by bot_max_steps")
            return None

        # The timeout that was in force while the inputs were being recorded is
        # the one that matters, so check it again before anything is written.
        if not BotRunner.CheckNoTimeout(game, device_manager, "before saving"):
            return None

        # `CheckNoTimeout` samples the timer at three moments. A timeout that
        # appeared and cleared again between them would leave every sample at
        # zero and the fabricated decline still in the replay, so trust the
        # counter over the sample: it records that it happened, not that it is
        # happening. See MARVEL-32.
        if device_manager.fabricated_inputs_since_game:
            Log.Assert(CATEGORY_NAME,
                f"{device_manager.fabricated_inputs_since_game} input(s) in this game were "
                "recorded from a timed-out wait rather than from the policy. "
                "The replay is corrupt and will not be saved.")
            return None

        record: Dict[str, Any] = {
            "seed": seed,
            "steps": steps,
            "decisions": device_manager.decision_count,
            "outcome": outcome,
            "file": "",
        }

        if not BOT_SAVE.value:
            return record

        path = BotRunner.SaveScene(game)
        if not path:
            Log.Assert(CATEGORY_NAME, "Failed to save the scene")
            return None
        record["file"] = FileManager.GetBaseName(path)

        if BOT_VERIFY.value and not BotRunner.Verify(game, path):
            return None

        return record

    ################################################################################
    #
    @staticmethod
    def CheckNoTimeout(game: 'Game', device_manager: 'BotDeviceManager', when: str) -> bool:
        """Refuse to generate under a wall-clock input timeout.

        `BuildDescriptor` sets `timeout = 0`, but the value that actually
        reaches `DoGetInput` is `device_manager.timer.max_timeout`, resolved
        through `GameSession.GameSetup` and `ControllerManager.Setup`. Check the
        resolved value rather than trusting the descriptor, the config file, or
        the layering between them. See MARVEL-32.
        """
        requested, resolved = BotRunner.GetTimeouts(game, device_manager)
        if requested == 0 and resolved == 0:
            return True

        Log.Assert(CATEGORY_NAME,
            f"Input timeout is not 0 ({when}): requested={requested} resolved={resolved}. "
            "A timeout returns an untouched input that the replay records as a "
            "decline nobody made, so generation is refused.")
        return False

    @staticmethod
    def GetTimeouts(game: 'Game', device_manager: 'BotDeviceManager') -> Tuple[float, float]:
        """(what the session was asked for, what the input wait will use)."""
        return float(game.session.timeout), float(device_manager.timer.max_timeout)

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
    def BuildManifest(game: 'Game', device_manager: 'BotDeviceManager',
                      played: List[Dict[str, Any]]) -> Dict[str, Any]:
        """What a corpus file was generated under, so it can be audited later.

        Deliberately small. The resolved input timeout is the load-bearing
        field (MARVEL-32): a corpus generated under a non-zero timeout can
        contain fabricated declines and would not reproduce elsewhere, and
        after the fact the scene file alone cannot tell you. Recording the full
        resolved config is MARVEL-34.

        Nothing here reads the clock or the host, so the manifest is as
        reproducible as the scenes it describes.
        """
        from engine.lib import Ver

        requested, resolved = BotRunner.GetTimeouts(game, device_manager)
        return {
            "generator": "bot",
            "engine_version": str(Ver.version),
            "scenario": BOT_SCENARIO.value,
            "heroes": list(BOT_HEROES.value),
            "encounter_sets": list(BOT_ENCOUNTER_SETS.value),
            "rules": list(BOT_RULES.value),
            "policy": device_manager.policy.name,
            "timeout": {"requested": requested, "resolved": resolved},
            "deterministic_save": BOT_DETERMINISTIC_SAVE.value,
            "max_steps": BOT_MAX_STEPS.value,
            # Across the whole run, including games that were discarded for it.
            # A non-zero value here means the timeout guard was bypassed and
            # every file in this run deserves suspicion, not just the dropped
            # ones -- the resolved timeout alone cannot say that, because it
            # only reports the value at the moment it was read.
            "fabricated_inputs": device_manager.fabricated_inputs_total,
            "games": played,
        }

    @staticmethod
    def WriteManifest(game: 'Game', device_manager: 'BotDeviceManager',
                      played: List[Dict[str, Any]]) -> str|None:
        """Write the run manifest beside the scenes this run saved."""
        from engine.lib import Json

        saved = [record["file"] for record in played if record.get("file")]
        if not saved:
            # Nothing was written, so there is no corpus to describe.
            return None

        folder = BOT_SAVE_FOLDER.value or BotRunner.save_folder

        # `SanitizeFilename` strips dots, so build the stem and add the
        # extension afterwards. The name comes from what the run was *asked*
        # for, never from how much of it succeeded -- otherwise a run where one
        # game failed lands beside the run where none did instead of replacing
        # it. Which seeds actually made it is in `games`.
        stem = FileManager.SanitizeFilename(
            f"bot-manifest-{BOT_SCENARIO.value}-{'+'.join(BOT_HEROES.value)}"
            f"-{BotRunner.GetSeed(0)}-{max(1, BOT_GAMES.value)}".lower())
        path = FileManager.JoinPath(folder, f"{stem}.json")

        Json.Save(BotRunner.BuildManifest(game, device_manager, played), path)
        Log.Info(CATEGORY_NAME, f"Manifest: {path}")
        return path

    @staticmethod
    def SaveScene(game: 'Game') -> str|None:
        name = None
        folder = BOT_SAVE_FOLDER.value
        if folder:
            name = FileManager.JoinPath(folder, f'{game.scene.GetSaveFileName()}.json')
        path = game.session.SaveScene(name, delete_old=False,
                                      deterministic=BOT_DETERMINISTIC_SAVE.value)
        if path:
            BotRunner.save_folder = FileManager.GetDirName(path)
        return path

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
