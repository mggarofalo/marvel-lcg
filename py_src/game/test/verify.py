"""Replay a folder of saved scenes and report whether each one reproduced.

This is the headless "replay this folder, report pass/fail" command MARVEL-17
assumed existed and MARVEL-28 found did not. It is deliberately independent of
the bot: `-bot_verify` can only check the scenes the run it belongs to just
generated, and the corpus phase needs to re-check a folder that was generated
somewhere else, months ago, by something else.

    python main.py -verify_replays
    python main.py -verify_replays -verify_folders ./corpus/
    python main.py -verify_replays -verify_report_file ./verify.json

The oracle is unchanged: `TestRun` re-executes a scene's recorded inputs and
`engine/controller/module/replay.py` compares `World.CalculateDigest()` against
the digest stored with each step, printing a card-by-card diff on mismatch. What
is new is that a process can run it, say what happened in JSON, and exit
non-zero -- none of which the engine could do before.

Three outcomes rather than two:

- **pass**      every recorded step reproduced its digest.
- **fail**      a step diverged, or the replay raised.
- **incomplete** the recording ran out while the game was still going. Every
  step it did hold reproduced, but the file describes less than a game. That is
  a truncated corpus entry, not a passing one, so it fails the run unless
  `-verify_allow_incomplete` says otherwise.

Verifying nothing is also a failure. A folder with no scenes in it exits
non-zero, because "the gate found no divergence" and "the gate never ran" must
not look the same to CI.
"""

from typing import TypeAlias

from core import *
from core.errors import EngineIntegrityError
from engine.file import FileManager
from engine.lib import Json
from engine.log import Log

CATEGORY_NAME = "TEST"

CASE_STATUS: TypeAlias = Literal["pass", "fail", "incomplete"]

class ReplayVerifier:

    ################################################################################
    #
    @staticmethod
    def Run(game: 'Game', folders: Sequence[str], *,
            report_path: str="", allow_incomplete: bool=False) -> bool:
        """Verify every scene under `folders`. Returns False if anything failed."""
        from engine.device.manager.verify.manager import VerifyDeviceManager

        device_manager = game.controller_manager.device_manager
        if not isinstance(device_manager, VerifyDeviceManager):
            Log.Assert(CATEGORY_NAME,
                f"Replay verification needs the verify device, got "
                f"{type(device_manager).__name__}")
            return False

        resolved = ReplayVerifier.ResolveFolders(folders)
        paths = ReplayVerifier.Enumerate(resolved)

        cases: List[Dict[str, Any]] = []
        if paths:
            Log.Info(CATEGORY_NAME,
                f"Verifying {len(paths)} scene(s) from {resolved}")
            for index, path in enumerate(paths):
                Log.Info(CATEGORY_NAME, f"[{index + 1}/{len(paths)}] {path}")
                cases.append(ReplayVerifier.RunOne(game, device_manager, path))
        else:
            Log.Assert(CATEGORY_NAME,
                f"No saved scenes found in {resolved}. There is nothing to "
                "verify, which is not the same as nothing being wrong.")

        document = ReplayVerifier.BuildReport(resolved, cases, allow_incomplete)

        if report_path:
            ReplayVerifier.WriteReport(document, report_path)

        for line in ReplayVerifier.Summarize(document):
            Log.Info(CATEGORY_NAME, line)

        return bool(document["ok"])

    ################################################################################
    #
    @staticmethod
    def ResolveFolders(folders: Sequence[str]) -> List[str]:
        """Which folders to read. Falls back to the configured `replay_folders`."""
        from game.test.test import REPLAY_FOLDERS

        chosen = [folder for folder in folders if folder]
        if chosen:
            return chosen
        if REPLAY_FOLDERS.is_initialized:
            return list(REPLAY_FOLDERS.value)
        return []

    @staticmethod
    def Enumerate(folders: Sequence[str]) -> List[str]:
        """The scene files under `folders`, in the order the test suite uses."""
        from game.test import Test

        for folder in folders:
            if not FileManager.IsDir(folder):
                Log.Warn(CATEGORY_NAME, f"Not a folder: {folder}")

        cases = Test.GetTestCases(folders=folders)
        scenes = [path for path in cases if ReplayVerifier.IsSceneDocument(path)]

        skipped = len(cases) - len(scenes)
        if skipped:
            Log.Info(CATEGORY_NAME,
                f"Skipped {skipped} file(s) that are not saved scenes")
        return scenes

    @staticmethod
    def IsSceneDocument(path: str) -> bool:
        """Whether this JSON file is a saved scene rather than a run artefact.

        A corpus folder holds more than scenes: `BotRunner` writes its manifest
        and its coverage report beside them, and both end in `.json`. Every
        field of `Scene` has a default, so loading a manifest as one *succeeds*
        and yields an empty game rather than an error -- which would then be
        replayed and reported as a failure of the corpus. The filter therefore
        has to happen before the load, not through it.

        A scene is recognised by the two fields a game cannot be built without:
        the heroes who played it and the scenario they played.

        Read with the standard library rather than `Json.Load`, which verifies a
        checksum against `Ver.version` and would make "does this file look like a
        scene" depend on the engine being initialised. Deciding what to open is
        not the loader's job.
        """
        import json

        try:
            with FileManager.OpenFile(path, read=True) as file:
                document = json.loads(file.Read())
        except Exception as exc:
            Log.Warn(CATEGORY_NAME, f"Could not read {path}: {type(exc).__name__}: {exc}")
            return False
        if not isinstance(document, dict):
            return False
        return bool(document.get("players")) and bool(document.get("campaign"))

    ################################################################################
    #
    @staticmethod
    def RunOne(game: 'Game', device_manager: 'VerifyDeviceManager',
               path: str) -> Dict[str, Any]:
        """Replay one scene and describe what happened."""
        from game.test import Test
        from game.test.test_run import TestRun

        device_manager.BeginCase()

        Test.is_in_test = True
        Test.test_cases = [path]

        detail = ""
        try:
            # `TestRun.Run` reports a failed case by *logging* it rather than
            # through its return value, so a clean run is "it completed AND
            # logged no error". The same reading `BotRunner.Verify` uses.
            completed = TestRun.Run(game, [path], do_save=False)
            has_error = Log.HasError(error=True)
            passed = completed and not has_error
        except EngineIntegrityError as exc:
            # The invariant checker runs on a replay too when it is enabled, and
            # it aborts rather than returning. Catching it here turns "the
            # replay reached an illegal state" into a failed case instead of a
            # traceback out of `Engine.EngineRun`. See MARVEL-11.
            detail = f"{type(exc).__name__}: {exc}"
            has_error = True
            passed = False
        except Exception as exc:
            Log.FailedTrace(CATEGORY_NAME, exc)
            detail = f"{type(exc).__name__}: {exc}"
            has_error = True
            passed = False
        finally:
            TestRun.RunEnd(game, False, True)

        unanswered = device_manager.unanswered_decisions
        recorded, replayed = ReplayVerifier.GetStepCounts(game)

        if passed:
            status: 'CASE_STATUS' = "pass"
        elif unanswered and not has_error:
            # The replay reproduced everything it was given and then ran past
            # the end of the recording. Nothing diverged; the file is short.
            status = "incomplete"
            if not detail:
                detail = (f"The recording ended after {recorded} step(s) but the "
                          "game had not finished")
        else:
            status = "fail"

        record: Dict[str, Any] = {
            "file": FileManager.GetBaseName(path),
            "path": path,
            "status": status,
            "recorded_steps": recorded,
            "replayed_steps": replayed,
            "unanswered_decisions": unanswered,
            "detail": detail,
        }

        if status == "pass":
            Log.Info(CATEGORY_NAME, f"pass {record['file']} ({replayed} steps)")
        else:
            Log.Assert(CATEGORY_NAME,
                f"{status} {record['file']} ({replayed}/{recorded} steps)"
                + (f": {detail}" if detail else ""))
        return record

    @staticmethod
    def GetStepCounts(game: 'Game') -> Tuple[int, int]:
        """(inputs the scene recorded, steps the replay actually took)."""
        scene = game.session.scene if hasattr(game.session, "scene") else None
        recorded = len(scene.inputs) if scene else 0
        return recorded, game.controller_manager.replay.current_step_id

    ################################################################################
    #
    @staticmethod
    def BuildReport(folders: Sequence[str], cases: Sequence[Dict[str, Any]],
                    allow_incomplete: bool) -> Dict[str, Any]:
        """What the run found, as one machine-readable document.

        Nothing here reads the clock or the host, so two verification runs over
        the same corpus produce byte-identical reports -- the same property the
        scenes themselves have.
        """
        from engine.lib import Ver

        counts: Dict[str, int] = {"pass": 0, "fail": 0, "incomplete": 0}
        for case in cases:
            status = str(case.get("status", "fail"))
            counts[status] = counts.get(status, 0) + 1

        ok = bool(cases) and counts["fail"] == 0
        if not allow_incomplete and counts["incomplete"]:
            ok = False

        return {
            "tool": "verify-replays",
            "engine_version": str(Ver.version),
            "folders": list(folders),
            "allow_incomplete": allow_incomplete,
            "total": len(cases),
            "passed": counts["pass"],
            "failed": counts["fail"],
            "incomplete": counts["incomplete"],
            "ok": ok,
            "cases": list(cases),
        }

    @staticmethod
    def WriteReport(document: Dict[str, Any], path: str) -> None:
        FileManager.MakeDir(FileManager.GetDirName(path))
        Json.Save(document, path)
        Log.Info(CATEGORY_NAME, f"Report: {path}")

    @staticmethod
    def Summarize(document: Dict[str, Any]) -> List[str]:
        lines = [
            f"--- Verify End --- ({document['passed']}/{document['total']})",
        ]
        if document["failed"]:
            lines.append(f"{document['failed']} scene(s) did not reproduce their recorded digests")
        if document["incomplete"]:
            suffix = " (allowed)" if document["allow_incomplete"] else ""
            lines.append(f"{document['incomplete']} scene(s) ended before the game did{suffix}")
        if not document["ok"] and not document["total"]:
            lines.append("no scenes were verified")
        return lines
