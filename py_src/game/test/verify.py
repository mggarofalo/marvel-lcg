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

**Config drift is a fourth way to fail, and it is checked before any replaying.**
The engine is deterministic for a given configuration and not across
configurations, so a corpus generated under one set of flags and verified under
another can diverge for a reason that has nothing to do with the engine being
wrong. Every `bot-manifest-*.json` in the folders carries the resolved config of
the run that wrote it (MARVEL-34); this compares each against the running
process and refuses to verify against a moving target. `-verify_allow_config_drift`
downgrades it to a warning for the case where the difference is the thing you
are deliberately testing.
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
            report_path: str="", allow_incomplete: bool=False,
            allow_config_drift: bool=False,
            quarantine_folder: str="") -> bool:
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

        # Was this corpus made by an engine configured the way this one is? A
        # divergence found under drifted config says nothing about the engine.
        #
        # After the replays rather than before them, which is worth the wasted
        # work when it fails: a config variable only exists once the module
        # declaring it has been imported, so a check run before the first scene
        # is comparing a barely-started process against one that had played a
        # game. Everything would read as "never registered here". Both snapshots
        # are now taken with the play path loaded, which is the only way the two
        # are comparable at all.
        manifests = ReplayVerifier.CheckConfig(resolved, allow_config_drift)

        document = ReplayVerifier.BuildReport(resolved, cases, allow_incomplete,
                                              manifests, allow_config_drift)

        if quarantine_folder:
            document["quarantine"] = ReplayVerifier.Quarantine(
                cases, quarantine_folder, allow_incomplete)

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
        if not chosen and REPLAY_FOLDERS.is_initialized:
            chosen = list(REPLAY_FOLDERS.value)
        return ReplayVerifier.ExpandTree(chosen)

    @staticmethod
    def ExpandTree(folders: Sequence[str]) -> List[str]:
        """Every folder at or under `folders` that directly holds a `.json`.

        A corpus is a tree, not a flat directory: `tools/corpus/generate.py`
        gives each case its own folder so that concurrent workers never share
        one, and a corpus of any size wants chunking anyway. Verifying it has to
        mean verifying the tree, or "verify this corpus" is a loop the caller
        writes.

        A flat folder expands to itself, so pointing at `./replays/` is
        unchanged. Folders that hold no JSON at all are dropped, which keeps an
        intermediate directory out of the reported folder list without hiding
        an empty corpus -- `Run` still fails when nothing was verified.

        Sorted, and symlinks are not followed: a verification report has to be
        the same on two machines, and a link back up the tree would otherwise
        walk forever.
        """
        import os

        found: List[str] = []
        for folder in folders:
            if not FileManager.IsDir(folder):
                # Left in so `Enumerate` can warn about it by name rather than
                # silently verifying nothing.
                found.append(folder)
                continue
            for current, subfolders, names in os.walk(folder):
                subfolders.sort()
                if any(name.endswith(".json") for name in names):
                    found.append(current)
        return sorted(set(found))

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

    ################################################################################
    #
    @staticmethod
    def Quarantine(cases: Sequence[Dict[str, Any]], folder: str,
                   allow_incomplete: bool) -> Dict[str, Any]:
        """Set aside every scene that did not reproduce, and say why.

        MARVEL-17's rule is that a non-reproducing replay must not be *silently*
        dropped: a corpus entry that fails to reproduce is a finding, and a
        finding that leaves no artefact will be rediscovered as a C# port bug
        months later and cost days.

        **Copied, not moved.** The corpus is this run's input, and a verifier
        that edits its input cannot be run twice against the same folder to see
        whether a failure repeats -- which is the first thing anyone will want
        to do. Removing them from the frozen set is MARVEL-18's job, and
        `quarantine.json` is what it reads.

        Quarantining does not forgive: the run still fails. This records what
        failed, it does not excuse it.

        Incomplete cases come here too unless `-verify_allow_incomplete` said
        they were expected -- a truncated recording is a corpus entry that
        describes less than a game, which is exactly what must not be frozen.
        """
        import shutil

        wanted = ("fail",) if allow_incomplete else ("fail", "incomplete")
        held = [case for case in cases if case.get("status") in wanted]

        index: Dict[str, Any] = {
            "tool": "verify-quarantine",
            "count": len(held),
            "cases": [],
        }

        if not held:
            # Written even when empty, on purpose: "nothing was quarantined" and
            # "quarantining never ran" must not look the same to whoever freezes
            # the corpus. The acceptance is an *empty* set, not a missing one.
            Log.Info(CATEGORY_NAME, "quarantine: nothing to set aside")
        else:
            Log.Assert(CATEGORY_NAME,
                f"quarantine: setting aside {len(held)} scene(s) in {folder}")

        FileManager.MakeDir(folder)
        for case in held:
            entry = {
                "file": case.get("file"),
                "source": case.get("path"),
                "status": case.get("status"),
                "detail": case.get("detail"),
                "recorded_steps": case.get("recorded_steps"),
                "replayed_steps": case.get("replayed_steps"),
                "copied": False,
            }
            source = str(case.get("path") or "")
            target = FileManager.JoinPath(folder, str(case.get("file") or ""))
            try:
                if source and FileManager.IsFile(source):
                    shutil.copyfile(source, target)
                    entry["copied"] = True
            except OSError as exc:
                # The record is the deliverable; the copy is a convenience. A
                # full disk must not turn a reported failure into a lost one.
                entry["detail"] = f"{entry['detail']} (copy failed: {exc})"
            index["cases"].append(entry)
            Log.Assert(CATEGORY_NAME,
                f"    {entry['status']} {entry['file']}: {entry['detail']}")

        path = FileManager.JoinPath(folder, "quarantine.json")
        Json.Save(index, path)
        Log.Info(CATEGORY_NAME, f"Quarantine: {path}")
        return {"folder": folder, "path": path, "count": len(held)}

    ################################################################################
    #
    @staticmethod
    def CheckConfig(folders: Sequence[str],
                    allow_drift: bool) -> List[Dict[str, Any]]:
        """Compare every run manifest under `folders` against this process.

        One record per manifest, whether or not it drifted, because "no manifest
        described this corpus" and "a manifest described it and agreed" are
        different findings and the report has to be able to tell them apart. A
        corpus with no manifest at all is **not** an error -- scenes saved by
        hand or by the web client never had one -- but it is reported, since it
        means nothing was checked.
        """
        from engine.config_record import ConfigRecord

        records: List[Dict[str, Any]] = []
        for path in ReplayVerifier.EnumerateManifests(folders):
            document = ReplayVerifier.ReadJson(path)
            name = FileManager.GetBaseName(path)
            compared = ConfigRecord.Compare((document or {}).get("config"))

            drifts = [drift for drift in compared if drift.is_failing]
            unmatched = [drift for drift in compared if not drift.is_failing]

            records.append({
                "file": name,
                "path": path,
                "git_sha": ((document or {}).get("config") or {}).get("git_sha"),
                "drifted": bool(drifts),
                "drift": [drift.ToDict() for drift in drifts],
                "unmatched": [drift.ToDict() for drift in unmatched],
            })

            if unmatched:
                # Not a failure -- see `ConfigDrift`. Said out loud anyway,
                # because a variable one side never read is worth a glance.
                Log.Info(CATEGORY_NAME,
                    f"{len(unmatched)} variable(s) registered on only one side "
                    f"of {name}: {', '.join(drift.name for drift in unmatched)}")

            if not drifts:
                Log.Info(CATEGORY_NAME, f"config matches {name}")
                continue

            # Loud on purpose: this is the reason a divergence would be
            # unreadable, and it is cheap to fix once you know.
            report = Log.Warn if allow_drift else Log.Assert
            report(CATEGORY_NAME,
                f"config drift against {name} ({len(drifts)} variable(s)):")
            for drift in drifts:
                report(CATEGORY_NAME, f"    {drift.name}: {drift.Describe()}")
            if allow_drift:
                Log.Warn(CATEGORY_NAME,
                    "allowed by -verify_allow_config_drift")

        if not records:
            Log.Info(CATEGORY_NAME,
                "No run manifest found beside these scenes, so the config they "
                "were generated under is unknown and unchecked.")
        return records

    @staticmethod
    def EnumerateManifests(folders: Sequence[str]) -> List[str]:
        """The run manifests under `folders`, sorted so a report is stable."""
        found: List[str] = []
        for folder in folders:
            if not FileManager.IsDir(folder):
                continue
            for name in FileManager.ListDir(folder):
                if not name.startswith("bot-manifest-") or not name.endswith(".json"):
                    continue
                path = FileManager.JoinPath(folder, name)
                if FileManager.IsFile(path):
                    found.append(path)
        return sorted(found)

    @staticmethod
    def ReadJson(path: str) -> Dict[str, Any]|None:
        """Read a run artefact with the standard library.

        Same reasoning as `IsSceneDocument`: `Json.Load` checks a checksum
        against `Ver.version`, and whether a manifest can be *read* must not
        depend on which engine wrote it -- the whole point of reading it is to
        find out.
        """
        import json

        try:
            with FileManager.OpenFile(path, read=True) as file:
                document = json.loads(file.Read())
        except Exception as exc:
            Log.Warn(CATEGORY_NAME, f"Could not read {path}: {type(exc).__name__}: {exc}")
            return None
        return document if isinstance(document, dict) else None

    ################################################################################
    #
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
            if not detail:
                # The common failure has no exception behind it: the replay
                # module compared the recorded digest against the recomputed one,
                # logged a card-by-card diff and carried on, and `Log.HasError`
                # is what turned that into a verdict. So there is nothing to
                # quote, and "fail" with an empty reason is not a reason --
                # MARVEL-17 wants a quarantined scene to say why it is there.
                # Say what is known: where it stopped, and where the detail is.
                detail = (
                    f"Diverged after {replayed} of {recorded} recorded step(s). "
                    "The recomputed state digest did not match the one saved "
                    "with the step; the card-by-card diff is in the run log "
                    "(engine/controller/module/replay.py)."
                )

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
                    allow_incomplete: bool,
                    manifests: Sequence[Dict[str, Any]]=(),
                    allow_config_drift: bool=False) -> Dict[str, Any]:
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

        drifted = [record for record in manifests if record["drifted"]]

        ok = bool(cases) and counts["fail"] == 0
        if not allow_incomplete and counts["incomplete"]:
            ok = False
        if not allow_config_drift and drifted:
            # A pass under drifted config is not a pass: the corpus and the
            # engine that replayed it were not the same engine.
            ok = False

        return {
            "tool": "verify-replays",
            "engine_version": str(Ver.version),
            "folders": list(folders),
            "allow_incomplete": allow_incomplete,
            "allow_config_drift": allow_config_drift,
            "manifests": list(manifests),
            "config_drifted": len(drifted),
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
        if document.get("config_drifted"):
            suffix = " (allowed)" if document.get("allow_config_drift") else ""
            lines.append(
                f"{document['config_drifted']} manifest(s) describe a corpus "
                f"generated under different config{suffix}")
        if not document["ok"] and not document["total"]:
            lines.append("no scenes were verified")
        return lines
