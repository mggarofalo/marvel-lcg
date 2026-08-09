"""Drives whole games with the bot device and writes the resulting scene files.

`Game.GameRun` waits for a browser to ask for a new game. Headless there is
nobody to ask, so the runner takes the place of that outer loop: it builds a
`NewGameDescriptor` exactly like the `/new` route does, runs the same
setup/loop/game-over sequence `GameRun` runs, and saves the scene.

Everything below the `NewGameDescriptor` — scene construction, the phase loop,
`Controller.ChoiceOne`, digest recording, `replay.Push` — is untouched engine code.
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
from engine.device.manager.bot.crash import (
    CrashCollector, Failure, Occurrence, FAILURE_CLASS)
from engine.device.manager.bot import crash
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

# Turn every failure into a replayable artefact instead of a log line, and write
# a distinct-signature report per run. See `crash.py` and MARVEL-12.
BOT_CAPTURE_CRASHES     = ConfigVariables.Bool('bot_capture_crashes', True)
BOT_CRASH_FOLDER        = ConfigVariables.Folder('bot_crash_folder', "./crashes/")

# Backstop on the number of *distinct* failures kept. Occurrences past it are
# still counted and the report says so; they just get no artefact of their own.
BOT_MAX_CRASH_SIGNATURES = ConfigVariables.Int('bot_max_crash_signatures', 200)

# Off by default. Most of what self-play finds is a pre-existing bug in the
# Python engine, and those are to be logged rather than allowed to block corpus
# generation (MARVEL-12). Turn it on to gate a run on a clean play-through.
BOT_FAIL_ON_CRASH       = ConfigVariables.Bool('bot_fail_on_crash', False)

# Guard against `SetGameOver()` looping (it returns False to re-run after undo).
MAX_GAME_OVER_RETRIES = 8

class BotRunner:

    # Directory the last scene was written to. The run manifest goes beside the
    # scenes it describes, and `GameSession.SaveScene` is the thing that knows
    # where that is when `bot_save_folder` is unset.
    save_folder: str = ""

    # Set for the duration of `Run`. Static methods all the way down, so the
    # failure paths reach it the same way they reach `save_folder`.
    collector: 'CrashCollector|None' = None

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

        collector = BotRunner.StartCapture(game, device_manager)
        try:
            for index in range(total):
                record = BotRunner.RunOne(game, device_manager, index, total)
                if record is None:
                    all_ok = False
                    if not BOT_CONTINUE_ON_ERROR.value:
                        break
                else:
                    played.append(record)
        finally:
            BotRunner.StopCapture()

        BotRunner.WriteManifest(game, device_manager, played, collector)
        # The scenes and the manifest are already on disk. A report that cannot
        # be written is worth a warning, not the run.
        BotRunner.Guarded("write the crash report",
                          lambda: BotRunner.ReportCrashes(collector, device_manager))

        if collector != None and collector.has_failures and BOT_FAIL_ON_CRASH.value:
            all_ok = False

        return all_ok

    ################################################################################
    # Crash capture (MARVEL-12)
    #
    @staticmethod
    def StartCapture(game: 'Game', device_manager: 'BotDeviceManager') -> 'CrashCollector|None':
        """Collect failures for this run, including the ones the engine absorbs.

        `EffectInvoker`, `Message2.Send`, the cost and target checkers and
        `Engine.EngineRun` all catch broadly and report through `Log.OnCrash`,
        which swallows on a release build -- so most of what self-play trips
        never reaches the `except` blocks below. The observer is how those are
        seen at all.
        """
        BotRunner.collector = None
        if not BOT_CAPTURE_CRASHES.value:
            return None

        def SaveScene(name: str) -> 'str|None':
            folder = BOT_CRASH_FOLDER.value
            return game.session.SaveScene(FileManager.JoinPath(folder, name),
                                          delete_old=False,
                                          deterministic=BOT_DETERMINISTIC_SAVE.value)

        collector = CrashCollector(save_scene=SaveScene,
                                   max_signatures=max(1, BOT_MAX_CRASH_SIGNATURES.value))

        def OnEngineCrash(category: 'Log.CATEGORY', exc: Exception) -> None:
            collector.CaptureException(
                exc, BotRunner.GetOccurrence(game, device_manager, collector.seed))

        BotRunner.collector = collector
        Log.crash_observer = OnEngineCrash
        return collector

    @staticmethod
    def StopCapture() -> None:
        # A hook on a module-level singleton outlives the run that installed it,
        # and the closure holds a game. Take it back down whatever happened.
        Log.crash_observer = None
        BotRunner.collector = None

    @staticmethod
    def GetOccurrence(game: 'Game', device_manager: 'BotDeviceManager', seed: int) -> 'Occurrence':
        """Where in the game this failure happened.

        `calculated_digest` is set by `Controller.ChoiceOne` before every
        decision, so it is the state as of the last step the engine got to --
        which is what makes the failure comparable against a replay of it.
        """
        replay = game.controller_manager.replay
        return Occurrence(
            seed        = seed,
            step        = replay.current_step_id,
            decisions   = device_manager.decision_count,
            digest      = replay.calculated_digest,
        )

    @staticmethod
    def Guarded(what: str, action: 'Callable[[], Any]') -> None:
        """Run a reporting step, absorbing anything it raises.

        Every caller below is already on a failure path, and `Finish` runs
        *outside* `RunOne`'s try block -- so an exception raised while
        describing one failed game would end the whole run and lose the games
        that succeeded. Reporting is never worth that.
        """
        try:
            action()
        except Exception as exc:
            Log.Warn(CATEGORY_NAME,
                f"Could not {what}: {type(exc).__name__}: {exc}")

    @staticmethod
    def CaptureException(game: 'Game', device_manager: 'BotDeviceManager',
                         seed: int, exc: BaseException) -> None:
        collector = BotRunner.collector
        if collector == None:
            return
        BotRunner.Guarded("capture this crash", lambda: collector.CaptureException(
            exc, BotRunner.GetOccurrence(game, device_manager, seed)))

    @staticmethod
    def CaptureReason(game: 'Game', device_manager: 'BotDeviceManager', seed: int,
                      kind: 'FAILURE_CLASS', reason_key: str, detail: str) -> None:
        """Record a failure the runner detected itself, with no exception to hash."""
        collector = BotRunner.collector
        if collector == None:
            return
        BotRunner.Guarded(f"capture {reason_key}", lambda: collector.Capture(
            Failure.FromReason(kind, reason_key, detail),
            BotRunner.GetOccurrence(game, device_manager, seed)))

    ################################################################################
    #
    @staticmethod
    def RunOne(game: 'Game', device_manager: 'BotDeviceManager', index: int, total: int) -> 'Dict[str, Any]|None':
        """Play one game. Returns its manifest record, or None if it failed."""
        seed = BotRunner.GetSeed(index)

        Log.Info(CATEGORY_NAME,
            f"Game {index + 1}/{total}: scenario={BOT_SCENARIO.value} "
            f"heroes={BOT_HEROES.value} seed={seed} policy={device_manager.policy.name}")

        if BotRunner.collector != None:
            BotRunner.collector.BeginGame(seed)

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
                BotRunner.CaptureReason(game, device_manager, seed, "timeout-stall",
                    "game_over_retries_exhausted",
                    f"Game did not finish after {MAX_GAME_OVER_RETRIES} restarts")
                return None

        except FabricatedInputError as exc:
            Log.Assert(CATEGORY_NAME, f"Refusing to record a fabricated input: {exc}")
            BotRunner.CaptureException(game, device_manager, seed, exc)
            return None
        except BotStuck as exc:
            Log.Assert(CATEGORY_NAME, f"Policy is stuck: {exc}")
            BotRunner.CaptureException(game, device_manager, seed, exc)
            return None
        except Exception as exc:
            Log.FailedTrace(CATEGORY_NAME, exc)
            BotRunner.CaptureException(game, device_manager, seed, exc)
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
            BotRunner.CaptureReason(game, device_manager, seed, "timeout-stall",
                "bot_max_steps",
                f"Game was cut short by bot_max_steps ({BOT_MAX_STEPS.value})")
            return None

        # The timeout that was in force while the inputs were being recorded is
        # the one that matters, so check it again before anything is written.
        if not BotRunner.CheckNoTimeout(game, device_manager, "before saving"):
            BotRunner.CaptureReason(game, device_manager, seed, "invariant-violation",
                "input_timeout_not_zero",
                "The resolved input timeout was not 0 while inputs were being recorded")
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
            BotRunner.CaptureReason(game, device_manager, seed, "invariant-violation",
                "fabricated_inputs_recorded",
                "Input(s) in this game came from a timed-out wait, not from the policy")
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
            BotRunner.CaptureReason(game, device_manager, seed, "invariant-violation",
                "scene_save_failed",
                "The game finished but its scene could not be written")
            return None
        record["file"] = FileManager.GetBaseName(path)

        if BOT_VERIFY.value and not BotRunner.Verify(game, path):
            BotRunner.CaptureReason(game, device_manager, seed, "invariant-violation",
                "replay_verification_failed",
                f"Replaying {FileManager.GetBaseName(path)} did not reproduce its "
                "recorded per-step digests")
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
        input is re-executed and `World.CalculateDigest()` is compared against
        the digest stored with that step, printing a card-by-card, field-by-field
        diff on mismatch.
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
                      played: List[Dict[str, Any]],
                      collector: 'CrashCollector|None'=None) -> Dict[str, Any]:
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
        # How much went wrong while these files were being made. A corpus where
        # two games in five tripped an assertion is a different artefact from
        # one where none did, and the scene files cannot say which they are.
        # The findings themselves are in the crash report; this is the pointer.
        crashes = collector.Summary() if collector != None else {
            "captured": 0, "signatures": 0, "by_class": {}, "truncated": False,
            "dropped_signatures": 0, "dropped_occurrences": 0,
        }
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
            "crashes": crashes,
            "games": played,
        }

    @staticmethod
    def RunStem(prefix: str) -> str:
        """Filename stem for a per-run artefact.

        `SanitizeFilename` strips dots, so build the stem and add the extension
        afterwards. The name comes from what the run was *asked* for, never from
        how much of it succeeded -- otherwise a run where one game failed lands
        beside the run where none did instead of replacing it.
        """
        return FileManager.SanitizeFilename(
            f"{prefix}-{BOT_SCENARIO.value}-{'+'.join(BOT_HEROES.value)}"
            f"-{BotRunner.GetSeed(0)}-{max(1, BOT_GAMES.value)}".lower())

    @staticmethod
    def WriteManifest(game: 'Game', device_manager: 'BotDeviceManager',
                      played: List[Dict[str, Any]],
                      collector: 'CrashCollector|None'=None) -> str|None:
        """Write the run manifest beside the scenes this run saved."""
        from engine.lib import Json

        saved = [record["file"] for record in played if record.get("file")]
        if not saved:
            # Nothing was written, so there is no corpus to describe.
            return None

        folder = BOT_SAVE_FOLDER.value or BotRunner.save_folder

        # Which seeds actually made it is in `games`.
        path = FileManager.JoinPath(folder, f"{BotRunner.RunStem('bot-manifest')}.json")

        Json.Save(BotRunner.BuildManifest(game, device_manager, played, collector), path)
        Log.Info(CATEGORY_NAME, f"Manifest: {path}")
        return path

    ################################################################################
    #
    @staticmethod
    def BuildCrashContext(device_manager: 'BotDeviceManager') -> Dict[str, Any]:
        """What every crash artefact needs to stand on its own."""
        from engine.lib import Ver

        return {
            "engine_version": str(Ver.version),
            "scenario": BOT_SCENARIO.value,
            "heroes": list(BOT_HEROES.value),
            "encounter_sets": list(BOT_ENCOUNTER_SETS.value),
            "rules": list(BOT_RULES.value),
            "policy": device_manager.policy.name,
        }

    @staticmethod
    def ReportCrashes(collector: 'CrashCollector|None',
                      device_manager: 'BotDeviceManager') -> str|None:
        """Write the per-run failure report and its self-contained sidecars.

        Unlike the manifest this is written even when no game succeeded -- a run
        where everything failed is exactly the one worth reading. The scenes
        were written as the failures happened; only the descriptions of them
        wait until the end, because the occurrence counts are not final until
        the run is.
        """
        from engine.lib import Json

        if collector == None or not collector.has_failures:
            return None

        context = BotRunner.BuildCrashContext(device_manager)
        folder = BOT_CRASH_FOLDER.value
        FileManager.MakeDir(folder)

        for group in collector.Groups():
            sidecar = FileManager.JoinPath(folder, crash.SidecarName(group.failure))
            Json.Save(crash.BuildSidecar(group, context), sidecar)

        path = FileManager.JoinPath(folder, f"{BotRunner.RunStem('bot-crashes')}.json")
        Json.Save(crash.BuildReport(collector, context), path)

        for line in crash.FormatSummary(collector):
            Log.Warn(CATEGORY_NAME, line)
        Log.Warn(CATEGORY_NAME, f"Crash report: {path}")
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
