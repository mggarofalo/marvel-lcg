"""The replay suite has to be runnable, and it has to be able to say no.

`python main.py -test` looked like it ran the replay suite and did not: it
expanded to a bare `-device`, which set the device *name* to the string "True",
landed on the interactive keyboard device, and then blocked in
`WaitUntilGameStart()` until something killed the process. Nothing replayed,
nothing failed, and no exit code said so. That is MARVEL-28.

`-verify_replays` replaces it. These tests pin the three things that made the
old entry point useless rather than merely broken:

1. **It reaches the scenes.** A corpus folder holds run artefacts beside the
   scenes -- `bot-manifest-*.json`, `bot-coverage-*.json` -- and every field of
   `Scene` has a default, so loading a manifest as a scene succeeds and yields
   an empty game instead of an error.
2. **It cannot hang.** When a recording runs out mid-game the engine asks the
   device for a real decision. The verify device ends the game there instead of
   waiting for a client that does not exist.
3. **It can fail.** A divergence, a truncated recording and an empty folder each
   produce a non-`ok` report, and "the gate found nothing wrong" never looks
   like "the gate never ran".

End-to-end -- generate, verify, corrupt a digest, verify again -- is
`python -m tools.replay.probe_verify`.
"""

import json
import os
import tempfile
import unittest
from unittest import mock

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from core.errors import EngineIntegrityError
from engine.config import ConfigVariables
from engine.lib import Ver
from engine.log import Log
from game.test.verify import ReplayVerifier


def setUpModule():
    # The report stamps the engine version. `Engine.Initialize` is what normally
    # sets it and these tests do not boot the engine.
    Ver.Initialize()


SCENE = {
    "version": "0.5.9.205",
    "campaign": {"name": "rhino", "encounter_sets": []},
    "players": [{"name": "spider_man"}],
    "inputs": [{"digest": "abc"}],
}

MANIFEST = {
    "generator": "bot",
    "engine_version": "0.5.9.205",
    "scenario": "rhino",
    "heroes": ["spider_man"],
    "games": [{"seed": 1, "steps": 47}],
}

COVERAGE = {
    "generator": "bot",
    "totals": {"factories": {"fired": 33, "universe": 303}},
    "never_fired_factories": [],
}


def WriteJson(folder, name, document):
    path = os.path.join(folder, name)
    with open(path, "w", encoding="utf-8") as handle:
        json.dump(document, handle)
    return path


class TempFolderTestCase(unittest.TestCase):

    def setUp(self):
        self.folder = tempfile.mkdtemp(prefix="verify-test-")
        self.addCleanup(self.RemoveFolder)

    def RemoveFolder(self):
        import shutil
        shutil.rmtree(self.folder, ignore_errors=True)


class TestASceneIsToldApartFromARunArtefact(TempFolderTestCase):
    """The filter has to happen before the load, not through it."""

    def test_a_saved_scene_is_a_scene(self):
        path = WriteJson(self.folder, "scene.json", SCENE)

        self.assertTrue(ReplayVerifier.IsSceneDocument(path))

    def test_a_bot_manifest_is_not(self):
        path = WriteJson(self.folder, "bot-manifest-rhino.json", MANIFEST)

        self.assertFalse(ReplayVerifier.IsSceneDocument(path))

    def test_a_coverage_report_is_not(self):
        path = WriteJson(self.folder, "bot-coverage-rhino.json", COVERAGE)

        self.assertFalse(ReplayVerifier.IsSceneDocument(path))

    def test_a_scene_with_no_players_is_not(self):
        # This is what a manifest degenerates to once `Scene`'s defaults have
        # been applied to it: a game nobody played.
        path = WriteJson(self.folder, "empty.json", dict(SCENE, players=[]))

        self.assertFalse(ReplayVerifier.IsSceneDocument(path))

    def test_a_json_list_is_not(self):
        path = WriteJson(self.folder, "list.json", [SCENE])

        self.assertFalse(ReplayVerifier.IsSceneDocument(path))

    def test_an_unreadable_file_is_not(self):
        path = os.path.join(self.folder, "broken.json")
        with open(path, "w", encoding="utf-8") as handle:
            handle.write("{not json")

        self.assertFalse(ReplayVerifier.IsSceneDocument(path))

    def test_a_missing_file_is_not(self):
        self.assertFalse(ReplayVerifier.IsSceneDocument(
            os.path.join(self.folder, "absent.json")))


class TestEnumeration(TempFolderTestCase):

    def test_only_the_scenes_come_back(self):
        WriteJson(self.folder, "[0.5.9.205]-a.json", SCENE)
        WriteJson(self.folder, "bot-manifest-rhino.json", MANIFEST)
        WriteJson(self.folder, "bot-coverage-rhino.json", COVERAGE)

        found = ReplayVerifier.Enumerate([self.folder])

        self.assertEqual([os.path.basename(p) for p in found], ["[0.5.9.205]-a.json"])

    def test_an_underscore_prefixed_file_is_excluded(self):
        # The replay suite's own convention for "parked, do not run".
        WriteJson(self.folder, "_parked.json", SCENE)
        WriteJson(self.folder, "[0.5.9.205]-a.json", SCENE)

        found = ReplayVerifier.Enumerate([self.folder])

        self.assertEqual([os.path.basename(p) for p in found], ["[0.5.9.205]-a.json"])

    def test_scenes_come_back_in_version_order(self):
        # Not alphabetical: "[0.5.9.9]" sorts after "[0.5.9.205]" as text and
        # before it as a version.
        WriteJson(self.folder, "[0.5.9.205]-b.json", SCENE)
        WriteJson(self.folder, "[0.5.9.9]-a.json", SCENE)

        found = [os.path.basename(p) for p in ReplayVerifier.Enumerate([self.folder])]

        self.assertEqual(found, ["[0.5.9.9]-a.json", "[0.5.9.205]-b.json"])

    def test_a_folder_that_is_not_there_yields_nothing(self):
        self.assertEqual(ReplayVerifier.Enumerate([os.path.join(self.folder, "absent")]), [])

    def test_folders_are_read_in_the_order_given(self):
        other = tempfile.mkdtemp(prefix="verify-test-")
        self.addCleanup(lambda: __import__("shutil").rmtree(other, ignore_errors=True))
        WriteJson(self.folder, "[0.5.9.205]-a.json", SCENE)
        WriteJson(other, "[0.5.9.205]-a.json", SCENE)

        found = ReplayVerifier.Enumerate([self.folder, other])

        self.assertEqual(len(found), 2)


class FakeReplay:
    def __init__(self, steps):
        self.current_step_id = steps


class FakeGame:
    """Just enough of `Game` for the parts of `RunOne` that are not `TestRun`."""

    def __init__(self, recorded, replayed):
        self.session = mock.Mock()
        self.session.scene = mock.Mock()
        self.session.scene.inputs = [None] * recorded
        self.controller_manager = mock.Mock()
        self.controller_manager.replay = FakeReplay(replayed)


class FakeVerifyDeviceManager:
    def __init__(self, unanswered=0):
        self.unanswered_decisions = unanswered
        self.began = 0

    def BeginCase(self):
        self.began += 1
        self.unanswered_decisions = 0


class TestACaseGetsTheRightVerdict(unittest.TestCase):
    """`TestRun.Run` reports a failed case by *logging* it, so a verdict is
    "it completed AND logged no error" -- never the return value alone."""

    def setUp(self):
        self.addCleanup(setattr, Log, "log_statistics", Log.log_statistics)
        Log.log_statistics = {}

    @staticmethod
    def RunOne(*, run_result=True, log_an_error=False, raises=None,
               unanswered=0, recorded=47, replayed=47):
        device_manager = FakeVerifyDeviceManager()

        def Run(game, cases, do_save=False):
            if raises != None:
                raise raises
            if log_an_error:
                Log.Assert("REPLAY", "Digest mismatch (#12 / 47)")
            device_manager.unanswered_decisions = unanswered
            return run_result

        test_run = mock.Mock()
        test_run.Run.side_effect = Run

        with mock.patch.dict("sys.modules", {
            "game.test": mock.Mock(Test=mock.Mock()),
            "game.test.test_run": mock.Mock(TestRun=test_run),
        }):
            record = ReplayVerifier.RunOne(
                FakeGame(recorded, replayed), device_manager, "scene.json")
        record["began"] = device_manager.began
        return record

    def test_a_clean_replay_passes(self):
        self.assertEqual(self.RunOne()["status"], "pass")

    def test_a_replay_that_logged_an_error_fails(self):
        # `TestRun.Run` said True; the log said otherwise. See MARVEL-65.
        self.assertEqual(self.RunOne(log_an_error=True)["status"], "fail")

    def test_a_replay_that_did_not_complete_fails(self):
        self.assertEqual(self.RunOne(run_result=False)["status"], "fail")

    def test_a_recording_that_ran_out_is_incomplete_rather_than_failed(self):
        # Every step it held reproduced. The file describes less than a game,
        # which is a different finding from a divergence.
        record = self.RunOne(run_result=False, unanswered=3, recorded=20, replayed=20)

        self.assertEqual(record["status"], "incomplete")
        self.assertEqual(record["unanswered_decisions"], 3)

    def test_a_divergence_outranks_an_incomplete_recording(self):
        # A replay that diverges stops answering and then runs out, so both
        # signals are present at once. The divergence is the one that matters.
        record = self.RunOne(run_result=False, log_an_error=True, unanswered=27,
                             recorded=47, replayed=20)

        self.assertEqual(record["status"], "fail")

    def test_an_integrity_error_fails_the_case_rather_than_the_run(self):
        # The invariant checker aborts rather than returning. Catching it here
        # is what keeps one bad scene from ending the whole folder. MARVEL-11.
        record = self.RunOne(raises=EngineIntegrityError("threat is negative"))

        self.assertEqual(record["status"], "fail")
        self.assertIn("threat is negative", record["detail"])

    def test_any_other_exception_fails_the_case_rather_than_the_run(self):
        record = self.RunOne(raises=ValueError("a card script blew up"))

        self.assertEqual(record["status"], "fail")
        self.assertIn("a card script blew up", record["detail"])

    def test_the_case_records_what_it_replayed(self):
        record = self.RunOne(recorded=47, replayed=47)

        self.assertEqual(record["file"], "scene.json")
        self.assertEqual(record["recorded_steps"], 47)
        self.assertEqual(record["replayed_steps"], 47)

    def test_each_case_starts_from_a_clean_device(self):
        # Otherwise one truncated scene would mark every later scene incomplete.
        self.assertEqual(self.RunOne()["began"], 1)


class TestTheReportSaysWhetherTheRunPassed(unittest.TestCase):

    @staticmethod
    def Case(status, name="a.json"):
        return {"file": name, "path": name, "status": status,
                "recorded_steps": 1, "replayed_steps": 1,
                "unanswered_decisions": 0, "detail": ""}

    def Build(self, statuses, allow_incomplete=False):
        cases = [self.Case(status, f"{i}.json") for i, status in enumerate(statuses)]
        return ReplayVerifier.BuildReport(["./replays/"], cases, allow_incomplete)

    def test_all_passing_is_ok(self):
        self.assertTrue(self.Build(["pass", "pass"])["ok"])

    def test_one_failure_is_not_ok(self):
        self.assertFalse(self.Build(["pass", "fail"])["ok"])

    def test_an_incomplete_recording_is_not_ok_by_default(self):
        self.assertFalse(self.Build(["pass", "incomplete"])["ok"])

    def test_an_incomplete_recording_is_ok_when_allowed(self):
        self.assertTrue(self.Build(["pass", "incomplete"], allow_incomplete=True)["ok"])

    def test_allowing_incomplete_recordings_does_not_forgive_a_failure(self):
        self.assertFalse(self.Build(["fail", "incomplete"], allow_incomplete=True)["ok"])

    def test_verifying_nothing_is_not_ok(self):
        # "The gate found no divergence" and "the gate never ran" must not look
        # the same to CI. An empty corpus is the second one.
        document = self.Build([])

        self.assertFalse(document["ok"])
        self.assertEqual(document["total"], 0)

    def test_the_counts_add_up(self):
        document = self.Build(["pass", "pass", "fail", "incomplete"])

        self.assertEqual(document["total"], 4)
        self.assertEqual(document["passed"], 2)
        self.assertEqual(document["failed"], 1)
        self.assertEqual(document["incomplete"], 1)

    def test_the_report_reads_no_clock_and_no_host(self):
        # Same property the scenes have: two runs over one corpus produce
        # byte-identical reports, so a diff between them means something.
        first = self.Build(["pass"])
        second = self.Build(["pass"])

        self.assertEqual(json.dumps(first, sort_keys=True),
                         json.dumps(second, sort_keys=True))

    def test_the_report_can_be_written_and_read_back(self):
        folder = tempfile.mkdtemp(prefix="verify-report-")
        self.addCleanup(lambda: __import__("shutil").rmtree(folder, ignore_errors=True))
        path = os.path.join(folder, "nested", "report.json")

        ReplayVerifier.WriteReport(self.Build(["pass"]), path)

        with open(path, encoding="utf-8") as handle:
            self.assertTrue(json.load(handle)["ok"])


class TestTheVerifyDeviceEndsTheGameRatherThanWaiting(unittest.TestCase):
    """The recording running out is where the old entry point hung."""

    def Manager(self):
        from engine.device.manager.base import AskOptionPayload
        from engine.device.manager.verify.manager import VerifyDeviceManager

        manager = VerifyDeviceManager()
        manager.ask_options[0] = AskOptionPayload(
            options_json="[]", ability_type="", event_name="", prompt_text="",
            show_cancel=True, replay_input="{}")
        manager.asking_players = [0]

        device = mock.Mock()
        device.player_id = 0
        device.controller.manager.replay.current_step_id = 20
        return manager, device

    def test_it_ends_the_game_instead_of_answering(self):
        manager, device = self.Manager()

        manager.SupplyInput(device)

        device.controller.world.game_over.SetExit.assert_called_once_with()

    def test_it_hands_back_the_empty_command(self):
        # `ChoiceOne` checks `world.is_game_over` before `replay.Push`, so this
        # never becomes a step -- which is what keeps the recorded and replayed
        # step counts comparable.
        manager, device = self.Manager()

        manager.SupplyInput(device)

        self.assertEqual(manager.ask_options[0].input_json, "{}")

    def test_it_counts_what_it_refused(self):
        manager, device = self.Manager()

        manager.SupplyInput(device)

        self.assertEqual(manager.unanswered_decisions, 1)

    def test_a_new_case_starts_from_zero(self):
        manager, device = self.Manager()
        manager.SupplyInput(device)

        manager.BeginCase()

        self.assertEqual(manager.unanswered_decisions, 0)

    def test_it_survives_a_game_that_has_already_gone(self):
        manager, device = self.Manager()
        device.controller.world = None

        manager.SupplyInput(device)

        self.assertEqual(manager.unanswered_decisions, 1)


class TestNoArgGroupSetsAValuedFlagWithNoValue(unittest.TestCase):
    """The shape of the original bug, pinned generally.

    `-test` expanded to `-device -no_editor ...`. `ConfigVariables.ParseString`
    saw `-device` with nothing after it, and its rule for a valueless flag is
    "this is a boolean, set it True" -- so the *string* variable `device` became
    "True", matched no device, and fell through to the interactive keyboard one.
    Any group can make that mistake; this catches the next one.
    """

    @staticmethod
    def Flags(arg_string):
        import re
        args = re.findall(r'\"(.*?)\"|(\S+)', arg_string)
        tokens = [match[0] if match[0] else match[1] for match in args]

        flags = {}
        current = ""
        for token in tokens:
            if token.startswith(("-", "/")):
                current = token[1:].lower()
                flags[current] = []
            elif current:
                flags[current].append(token)
        return flags

    def test_every_group_flag_that_needs_a_value_has_one(self):
        from engine.config import ConfigVariable

        for name, arg_string in ConfigVariables.group.items():
            for key, values in self.Flags(arg_string).items():
                variable = ConfigVariables.Find(key[3:] if key.startswith("no_") else key)
                if variable == None or isinstance(variable, ConfigVariable.Bool):
                    continue
                self.assertTrue(
                    values,
                    f"-{key} in the '{name}' group takes a value and was given none; "
                    f"it will be set to the string 'True'")

    def test_the_test_group_runs_the_replay_suite(self):
        # The literal complaint in MARVEL-28: `-test` did not run the suite.
        self.assertIn("verify_replays", ConfigVariables.group["test"])


class TestTheEngineKnowsWhenItIsVerifying(unittest.TestCase):

    @staticmethod
    def IsVerifying(replays=False, folders=()):
        from engine.engine import Engine, VERIFY_FOLDERS, VERIFY_REPLAYS

        with mock.patch.object(VERIFY_REPLAYS, "value", replays):
            with mock.patch.object(VERIFY_FOLDERS, "value", list(folders)):
                return Engine.IsVerifyingReplays()

    def test_the_switch_alone_is_enough(self):
        self.assertTrue(self.IsVerifying(replays=True))

    def test_naming_a_folder_alone_is_enough(self):
        self.assertTrue(self.IsVerifying(folders=["./corpus/"]))

    def test_an_ordinary_run_is_not_verifying(self):
        self.assertFalse(self.IsVerifying())

    def test_an_empty_folder_list_is_not_a_request_to_verify(self):
        self.assertFalse(self.IsVerifying(folders=["", ""]))


class TestFolderResolution(unittest.TestCase):

    def test_an_explicit_folder_wins(self):
        self.assertEqual(ReplayVerifier.ResolveFolders(["./corpus/"]), ["./corpus/"])

    def test_no_folder_falls_back_to_the_configured_replay_folders(self):
        from game.test.test import REPLAY_FOLDERS

        with mock.patch.object(REPLAY_FOLDERS, "value", ["./replays/"]):
            with mock.patch.object(REPLAY_FOLDERS, "set_from", "LaunchJson"):
                self.assertEqual(ReplayVerifier.ResolveFolders([]), ["./replays/"])

    def test_blank_entries_are_not_folders(self):
        with mock.patch.object(ReplayVerifier, "ResolveFolders", ReplayVerifier.ResolveFolders):
            self.assertNotIn("", ReplayVerifier.ResolveFolders(["", "./corpus/"]))


class TestTheRunRefusesTheWrongDevice(unittest.TestCase):

    def test_a_run_without_the_verify_device_is_refused(self):
        # Every other device either blocks or plays. Running the verifier under
        # one would replay until the recording ran out and then keep going.
        game = mock.Mock()
        game.controller_manager.device_manager = mock.Mock()

        self.assertFalse(ReplayVerifier.Run(game, ["./replays/"]))


if __name__ == "__main__":
    unittest.main()
