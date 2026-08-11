"""A self-play failure has to survive as something you can replay.

The engine absorbs exceptions during play by design: `EffectInvoker`,
`Message2.Send`, the cost and target checkers and `Engine.EngineRun` all catch
broadly so one broken card cannot end the game, and every one of them reports
through `Log.OnCrash`, which re-raises only when `Build.release` is false --
and `build.py` hardcodes it true. So an assertion tripped by the bot leaves a
traceback on stdout and nothing else: no seed, no step, no digest, no scene.

These tests cover the collection rules that turn one into an artefact:
grouping by traceback signature, classifying it, keeping exactly one scene per
distinct bug, and choosing the shortest occurrence as the repro. None of them
boot the engine or touch the disk -- the one side effect, saving the scene, is
injected.

See MARVEL-12.
"""

import unittest
from unittest import mock

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from engine.device.manager.base import FabricatedInputError
from engine.device.manager.bot import crash
from engine.device.manager.bot.crash import (
    CrashCollector, Failure, Occurrence)
from engine.device.manager.bot.policy import BotStuck


def Raise(exc):
    """Return `exc` with a real traceback attached, as `Capture` will see it."""
    try:
        raise exc
    except BaseException as raised:
        return raised


def RaiseDeep(exc):
    """Same raise statement, caught one frame further up.

    A traceback holds the frames an exception *propagated through*, not the
    frames that led to it -- so this is what a longer path actually looks like.
    """
    def Inner():
        raise exc
    try:
        Inner()
    except BaseException as raised:
        return raised


def RaiseElsewhere(exc):
    """A different raise site, for signatures that must not collide."""
    try:
        raise exc
    except BaseException as raised:
        return raised


class RecordingSaver:
    """Stands in for `GameSession.SaveScene`."""

    def __init__(self, result="written"):
        self.result = result
        self.calls = []

    def __call__(self, name):
        self.calls.append(name)
        if self.result == "written":
            return f"./crashes/{name}"
        return self.result


def MakeCollector(saver=None, max_signatures=200):
    return CrashCollector(save_scene=saver if saver != None else RecordingSaver(),
                          max_signatures=max_signatures)


CONTEXT = {
    "engine_version": "0.5.9.205",
    "scenario": "rhino",
    "heroes": ["spider_man"],
    "encounter_sets": [],
    "rules": [],
    "policy": "first",
}


class TestSignatures(unittest.TestCase):

    def test_the_same_raise_site_groups_together(self):
        # The frame that raised is the same; only the call path above it differs
        # in length, which is part of the signature -- but the *site* is what
        # decides whether two reports are the same bug, so build both the same
        # way and vary only the message.
        one = Failure.FromException(Raise(ValueError("card 01001")))
        other = Failure.FromException(Raise(ValueError("card 02052")))

        self.assertEqual(one.signature, other.signature)

    def test_the_message_is_kept_even_though_it_is_not_hashed(self):
        # Excluded from the signature so one bug is one group; still recorded,
        # because it is usually the first thing you read.
        failure = Failure.FromException(Raise(ValueError("card 01001")))

        self.assertEqual(failure.message, "card 01001")

    def test_a_different_raise_site_gets_a_different_signature(self):
        one = Failure.FromException(Raise(ValueError("x")))
        other = Failure.FromException(RaiseElsewhere(ValueError("x")))

        self.assertNotEqual(one.signature, other.signature)

    def test_the_path_the_exception_travelled_is_part_of_the_signature(self):
        # Deliberate. The engine's handlers sit several frames above the card
        # that raised, and which handler absorbed it is part of what went
        # wrong -- the same `assert` swallowed by `EffectInvoker` and by
        # `Message2.Send` are two findings, not one.
        one = Failure.FromException(Raise(ValueError("x")))
        other = Failure.FromException(RaiseDeep(ValueError("x")))

        self.assertNotEqual(one.signature, other.signature)

    def test_only_the_frames_it_travelled_through_count(self):
        # A traceback does not record the callers above the handler, so two
        # routes into the same try/except are one signature.
        def Outer():
            return Raise(ValueError("x"))

        self.assertEqual(Failure.FromException(Raise(ValueError("x"))).signature,
                         Failure.FromException(Outer()).signature)

    def test_a_different_exception_type_gets_a_different_signature(self):
        one = Failure.FromException(Raise(ValueError("x")))
        other = Failure.FromException(Raise(TypeError("x")))

        self.assertNotEqual(one.signature, other.signature)

    def test_an_exception_with_no_traceback_still_gets_one(self):
        # Constructed but never raised. It must not blow up the reporter.
        failure = Failure.FromException(ValueError("never raised"))

        self.assertTrue(failure.signature)
        self.assertIn("no traceback", failure.title)

    def test_a_reason_signature_ignores_the_prose(self):
        # The detail carries per-game numbers; the check that refused does not.
        one = Failure.FromReason("timeout-stall", "bot_max_steps", "cut short at 20000")
        other = Failure.FromReason("timeout-stall", "bot_max_steps", "cut short at 500")

        self.assertEqual(one.signature, other.signature)
        self.assertEqual(one.reason_key, "bot_max_steps")

    def test_different_reasons_do_not_collide(self):
        one = Failure.FromReason("timeout-stall", "bot_max_steps", "")
        other = Failure.FromReason("timeout-stall", "game_over_retries_exhausted", "")

        self.assertNotEqual(one.signature, other.signature)


class TestFramePaths(unittest.TestCase):
    """A corpus is generated on one machine and read on another."""

    def test_a_frame_in_the_tree_is_relative_and_slash_separated(self):
        frames = crash.FrameKeys(Raise(ValueError("x")))

        self.assertTrue(frames)
        site = frames[-1]
        self.assertNotIn("\\", site)
        self.assertTrue(site.startswith("unit_test/test_bot_crash.py:"), site)

    def test_a_frame_outside_the_tree_keeps_only_its_filename(self):
        # An absolute path into a Python install encodes the machine, so two
        # machines would report the same bug under two signatures.
        self.assertEqual(crash.RelativeFrame("/usr/lib/python3.13/json/decoder.py"),
                         "decoder.py")

    def test_the_traceback_text_names_no_machine(self):
        # It sits beside byte-reproducible files. The standard formatter prints
        # absolute paths, which would put a home directory in every artefact
        # and make one failure look like two across two machines.
        text = Failure.FromException(Raise(ValueError("boom"))).traceback_text

        self.assertIn("unit_test/test_bot_crash.py", text)
        self.assertNotIn(":\\", text)
        self.assertIn("ValueError: boom", text)

    def test_a_chained_exception_keeps_the_one_that_caused_it(self):
        # A card script raising inside an `except` is the shape where the first
        # exception is the finding.
        try:
            try:
                raise KeyError("01001")
            except KeyError as cause:
                raise ValueError("could not resolve card") from cause
        except ValueError as raised:
            text = Failure.FromException(raised).traceback_text

        self.assertIn("KeyError", text)
        self.assertIn("direct cause", text)
        self.assertIn("ValueError: could not resolve card", text)

    def test_the_two_hosts_agree_on_the_same_frame(self):
        # `os.path.relpath` answers in the host's separator, so a Windows run
        # and a Linux run see the same file differently. Simulate each host
        # fully -- separator and all -- or this passes on one and fails on the
        # other, which is the very thing it exists to rule out.
        def AsHost(separator, relative):
            with mock.patch.object(crash.os, "sep", separator):
                with mock.patch.object(crash.os.path, "relpath",
                                       staticmethod(lambda path, start: relative)):
                    return crash.RelativeFrame("ignored")

        windows = AsHost("\\", "game\\effect\\effect.py")
        posix = AsHost("/", "game/effect/effect.py")

        self.assertEqual(windows, posix)
        self.assertEqual(windows, "game/effect/effect.py")


class TestClassification(unittest.TestCase):

    def test_an_assert_is_an_engine_assert(self):
        self.assertEqual(crash.Classify(AssertionError("card not found")), "engine-assert")

    def test_a_fabricated_input_is_its_own_class(self):
        # It is the only failure whose recorded inputs are untrustworthy, and
        # therefore the only one whose scene is withheld. Sharing a class with
        # the checker was MARVEL-66.
        self.assertEqual(crash.Classify(FabricatedInputError("timed out")),
                         "fabricated-input")

    def test_integrity_wins_over_the_timeout_that_caused_it(self):
        # `FabricatedInputError` *is* a timeout, but the class exists to say the
        # run has already produced something untrustworthy, and that is what
        # decides how the finding is triaged.
        self.assertNotEqual(crash.Classify(FabricatedInputError("timed out")),
                            "timeout-stall")

    def test_a_checker_violation_is_an_invariant_violation(self):
        from game.world.invariants import InvariantViolation

        self.assertEqual(crash.Classify(InvariantViolation("hand/over-limit")),
                         "invariant-violation")

    def test_a_stalled_policy_is_a_stall_despite_being_an_integrity_error(self):
        # `NoProgressError` derives from `EngineIntegrityError` so it survives
        # the broad handlers, not because anything is corrupt. Its inputs are
        # genuine and its scene is the artefact you want. See MARVEL-37.
        from engine.device.manager.bot.progress import NoProgressError

        self.assertEqual(crash.Classify(NoProgressError("no progress for 32")),
                         "timeout-stall")

    def test_a_stuck_policy_is_a_stall(self):
        self.assertEqual(crash.Classify(BotStuck("no answer left")), "timeout-stall")

    def test_anything_else_is_unhandled(self):
        self.assertEqual(crash.Classify(ValueError("boom")), "unhandled-exception")


class TestDeduplication(unittest.TestCase):

    def test_one_recurring_bug_writes_one_scene(self):
        # The whole point: ten thousand occurrences must not be ten thousand
        # files.
        saver = RecordingSaver()
        collector = MakeCollector(saver)

        for step in range(20):
            collector.CaptureException(Raise(ValueError("boom")),
                                       Occurrence(seed=1, step=100 + step))

        self.assertEqual(len(collector.groups), 1)
        self.assertEqual(len(saver.calls), 1)
        group = collector.Groups()[0]
        self.assertEqual(group.occurrences, 20)

    def test_distinct_bugs_are_kept_apart(self):
        collector = MakeCollector()

        collector.CaptureException(Raise(ValueError("a")), Occurrence(seed=1, step=1))
        collector.CaptureException(RaiseElsewhere(TypeError("b")), Occurrence(seed=1, step=2))

        self.assertEqual(len(collector.groups), 2)

    def test_the_games_a_bug_appeared_in_are_counted_not_the_sightings(self):
        collector = MakeCollector()

        for seed in (7, 7, 7, 8):
            collector.CaptureException(Raise(ValueError("boom")),
                                       Occurrence(seed=seed, step=50))

        group = collector.Groups()[0]
        self.assertEqual(group.occurrences, 4)
        self.assertEqual(group.games, 2)
        self.assertEqual(group.seeds, [7, 8])

    def test_the_same_exception_object_seen_twice_counts_once(self):
        # `Log.OnCrash` observes it on the way through and the runner catches it
        # if it keeps propagating. Both are the same failure.
        collector = MakeCollector()
        exc = Raise(ValueError("boom"))

        collector.CaptureException(exc, Occurrence(seed=1, step=10))
        collector.CaptureException(exc, Occurrence(seed=1, step=10))

        self.assertEqual(collector.captured, 1)
        self.assertEqual(collector.Groups()[0].occurrences, 1)

    def test_two_exceptions_from_one_site_still_count_twice(self):
        # The guard is per exception object, not per signature -- a bug that
        # fires twice is two occurrences.
        collector = MakeCollector()

        collector.CaptureException(Raise(ValueError("boom")), Occurrence(seed=1, step=10))
        collector.CaptureException(Raise(ValueError("boom")), Occurrence(seed=1, step=20))

        self.assertEqual(collector.captured, 2)

    def test_capture_does_not_reenter(self):
        # Saving a scene runs engine code that can raise into another handler,
        # which would report through the observer again.
        collector = MakeCollector()
        seen = []

        def Reentrant(name):
            seen.append(name)
            collector.CaptureException(Raise(TypeError("secondary")),
                                       Occurrence(seed=1, step=11))
            return f"./crashes/{name}"

        collector.save_scene = Reentrant
        collector.CaptureException(Raise(ValueError("boom")), Occurrence(seed=1, step=10))

        self.assertEqual(len(seen), 1)
        self.assertEqual(len(collector.groups), 1)


class TestMinimalRepro(unittest.TestCase):

    def test_a_shorter_occurrence_replaces_the_repro(self):
        saver = RecordingSaver()
        collector = MakeCollector(saver)

        collector.CaptureException(Raise(ValueError("boom")), Occurrence(seed=1, step=900))
        collector.CaptureException(Raise(ValueError("boom")), Occurrence(seed=2, step=40))

        group = collector.Groups()[0]
        self.assertEqual(group.minimal.seed, 2)
        self.assertEqual(group.minimal.step, 40)
        # Re-saved to the same name, so the shorter repro replaces the longer
        # one rather than adding a second file.
        self.assertEqual(len(saver.calls), 2)
        self.assertEqual(saver.calls[0], saver.calls[1])

    def test_a_longer_occurrence_does_not(self):
        saver = RecordingSaver()
        collector = MakeCollector(saver)

        collector.CaptureException(Raise(ValueError("boom")), Occurrence(seed=1, step=40))
        collector.CaptureException(Raise(ValueError("boom")), Occurrence(seed=2, step=900))

        group = collector.Groups()[0]
        self.assertEqual(group.minimal.seed, 1)
        self.assertEqual(len(saver.calls), 1)

    def test_the_repro_names_the_seed_and_the_step(self):
        collector = MakeCollector()
        collector.CaptureException(
            Raise(ValueError("boom")),
            Occurrence(seed=4242, step=87, decisions=86, digest='{"v":2}'))

        repro = crash.BuildRepro(collector.Groups()[0], CONTEXT)

        self.assertEqual(repro["seed"], 4242)
        self.assertEqual(repro["step"], 87)
        self.assertEqual(repro["decisions"], 86)

    def test_the_repro_command_regenerates_that_one_game(self):
        command = crash.ReproCommand(CONTEXT, 4242)

        self.assertIn("-bot_scenario rhino", command)
        self.assertIn("-bot_heroes spider_man", command)
        self.assertIn("-bot_seed 4242", command)
        self.assertIn("-bot_games 1", command)
        self.assertIn("-bot_policy first", command)

    def test_the_repro_command_carries_the_modular_sets_and_rules(self):
        context = dict(CONTEXT, encounter_sets=["bomb_scare"], rules=["expert"])

        command = crash.ReproCommand(context, 1)

        self.assertIn("-bot_encounter_sets bomb_scare", command)
        self.assertIn("-bot_rules expert", command)


class TestTheStateDigest(unittest.TestCase):
    """The v2 digest is a whole board, not a hash. Where it goes matters."""

    BOARD = '{"v":2,"cards":[{"id":0,"card":"rule_a"}]}'

    def Collect(self):
        collector = MakeCollector()
        collector.CaptureException(Raise(ValueError("boom")),
                                   Occurrence(seed=1, step=10, digest=self.BOARD))
        return collector.Groups()[0]

    def test_the_run_report_carries_only_a_fingerprint(self):
        # One board dump per signature would bury what the report exists to show.
        repro = crash.BuildRepro(self.Collect(), CONTEXT)

        self.assertNotIn("digest", repro)
        self.assertEqual(repro["digest_hash"], crash.DigestHash(self.BOARD))

    def test_the_sidecar_carries_the_whole_digest(self):
        # It is what `digest.Diff` compares a re-run against, so the artefact
        # that has to stand on its own keeps it.
        sidecar = crash.BuildSidecar(self.Collect(), CONTEXT)

        self.assertEqual(sidecar["repro"]["digest"], self.BOARD)

    def test_the_fingerprint_distinguishes_states(self):
        self.assertNotEqual(crash.DigestHash(self.BOARD),
                            crash.DigestHash(self.BOARD.replace("rule_a", "rule_b")))

    def test_no_digest_means_no_fingerprint(self):
        # A failure before the first decision has no state to fingerprint.
        self.assertEqual(crash.DigestHash(""), "")


class TestSceneArtefacts(unittest.TestCase):

    def test_the_scene_is_named_after_the_signature(self):
        collector = MakeCollector()
        collector.CaptureException(Raise(ValueError("boom")), Occurrence(seed=1, step=10))

        group = collector.Groups()[0]
        self.assertEqual(group.scene_file,
                         f"bot-crash-unhandled-exception-{group.failure.signature}.json")

    def test_a_fabricated_input_gets_no_scene(self):
        # Writing the recorded inputs would put a replay of a run we are
        # refusing to trust on disk. The seed reproduces it instead. MARVEL-32.
        saver = RecordingSaver()
        collector = MakeCollector(saver)

        collector.Capture(
            Failure.FromReason("fabricated-input", "fabricated_inputs_recorded", "x"),
            Occurrence(seed=1, step=10))

        group = collector.Groups()[0]
        self.assertEqual(saver.calls, [])
        self.assertEqual(group.scene_file, "")
        self.assertIn("MARVEL-32", group.scene_note)

    def test_a_checker_violation_keeps_its_scene(self):
        # The inputs were all made by the policy; only the state computed from
        # them is wrong, so they replay. Withholding this scene told the reader
        # to re-run a whole game from a seed when a step-indexed repro existed.
        # See MARVEL-66.
        saver = RecordingSaver()
        collector = MakeCollector(saver)

        collector.Capture(
            Failure.FromReason("invariant-violation", "replay_verification_failed", "x"),
            Occurrence(seed=1, step=10))

        group = collector.Groups()[0]
        self.assertEqual(len(saver.calls), 1)
        self.assertNotEqual(group.scene_file, "")
        self.assertEqual(group.scene_note, "")

    def test_only_a_fabricated_input_withholds(self):
        # Pins the set itself: widening it again is the exact edit that caused
        # MARVEL-66, and it is a one-word change.
        self.assertEqual(set(crash.SCENE_WITHHELD_CLASSES), {"fabricated-input"})

    def test_a_save_that_raises_does_not_lose_the_finding(self):
        def Explode(name):
            raise RuntimeError("the world is half torn down")

        collector = MakeCollector(Explode)
        collector.CaptureException(Raise(ValueError("boom")), Occurrence(seed=1, step=10))

        group = collector.Groups()[0]
        self.assertEqual(group.occurrences, 1)
        self.assertEqual(group.scene_file, "")
        self.assertIn("RuntimeError", group.scene_note)

    def test_a_save_that_declines_says_so(self):
        collector = MakeCollector(RecordingSaver(result=None))
        collector.CaptureException(Raise(ValueError("boom")), Occurrence(seed=1, step=10))

        group = collector.Groups()[0]
        self.assertEqual(group.scene_file, "")
        self.assertTrue(group.scene_note)


class TestSignatureCap(unittest.TestCase):

    def test_past_the_cap_nothing_new_is_recorded(self):
        collector = MakeCollector(max_signatures=1)

        collector.CaptureException(Raise(ValueError("a")), Occurrence(seed=1, step=1))
        collector.CaptureException(RaiseElsewhere(TypeError("b")), Occurrence(seed=1, step=2))

        self.assertEqual(len(collector.groups), 1)

    def test_what_the_cap_dropped_is_still_counted_and_reported(self):
        # A report that silently truncates reads as a clean run.
        collector = MakeCollector(max_signatures=1)

        collector.CaptureException(Raise(ValueError("a")), Occurrence(seed=1, step=1))
        collector.CaptureException(RaiseElsewhere(TypeError("b")), Occurrence(seed=1, step=2))

        summary = collector.Summary()
        self.assertEqual(summary["captured"], 2)
        self.assertEqual(summary["signatures"], 1)
        self.assertTrue(summary["truncated"])
        self.assertEqual(summary["dropped_signatures"], 1)
        self.assertEqual(summary["dropped_occurrences"], 1)

    def test_one_dropped_bug_firing_often_is_still_one_dropped_bug(self):
        # "12 dropped" is very different news if it is 12 bugs or 12 hits on
        # one, so the two are counted separately.
        collector = MakeCollector(max_signatures=1)
        collector.CaptureException(Raise(ValueError("a")), Occurrence(seed=1, step=1))

        for step in range(9):
            collector.CaptureException(RaiseElsewhere(TypeError("b")),
                                       Occurrence(seed=1, step=step))

        summary = collector.Summary()
        self.assertEqual(summary["dropped_signatures"], 1)
        self.assertEqual(summary["dropped_occurrences"], 9)

    def test_an_exception_the_cap_dropped_is_not_offered_twice(self):
        # It was counted, so the second sighting -- the observer's and the
        # runner's are the same failure -- must not count again.
        collector = MakeCollector(max_signatures=1)
        collector.CaptureException(Raise(ValueError("a")), Occurrence(seed=1, step=1))

        capped = RaiseElsewhere(TypeError("b"))
        collector.CaptureException(capped, Occurrence(seed=1, step=2))
        collector.CaptureException(capped, Occurrence(seed=1, step=2))

        self.assertEqual(collector.captured, 2)
        self.assertEqual(collector.Summary()["dropped_signatures"], 1)

    def test_an_uncapped_run_is_not_marked_truncated(self):
        collector = MakeCollector()
        collector.CaptureException(Raise(ValueError("a")), Occurrence(seed=1, step=1))

        self.assertFalse(collector.Summary()["truncated"])


class TestReport(unittest.TestCase):

    def Collect(self):
        collector = MakeCollector()
        for seed in (1, 2, 3):
            collector.CaptureException(Raise(ValueError("boom")),
                                       Occurrence(seed=seed, step=100 + seed))
        collector.CaptureException(RaiseElsewhere(AssertionError("card missing")),
                                   Occurrence(seed=1, step=12))
        return collector

    def test_every_distinct_signature_appears_with_its_count(self):
        report = crash.BuildReport(self.Collect(), CONTEXT)

        self.assertEqual(report["signatures"], 2)
        self.assertEqual(report["captured"], 4)
        self.assertEqual([entry["occurrences"] for entry in report["failures"]], [3, 1])

    def test_failures_are_ordered_most_frequent_first(self):
        report = crash.BuildReport(self.Collect(), CONTEXT)

        counts = [entry["occurrences"] for entry in report["failures"]]
        self.assertEqual(counts, sorted(counts, reverse=True))

    def test_the_counts_are_broken_down_by_class(self):
        report = crash.BuildReport(self.Collect(), CONTEXT)

        self.assertEqual(report["by_class"],
                         {"engine-assert": 1, "unhandled-exception": 3})

    def test_an_entry_stands_on_its_own(self):
        # "Every failure is reproducible from its artifact alone" -- so the
        # sidecar has to carry the run it came from, not just the traceback.
        report = crash.BuildReport(self.Collect(), CONTEXT)
        entry = report["failures"][0]

        for key in ("engine_version", "scenario", "heroes", "policy"):
            self.assertIn(key, entry)
        self.assertIn("traceback", entry)
        self.assertIn("ValueError", entry["traceback"])
        self.assertIn("command", entry["repro"])

    def test_the_report_reads_no_clock_and_no_host(self):
        # It sits beside byte-reproducible scenes (MARVEL-27); it must not be
        # the thing that reintroduces a per-run difference.
        one = crash.BuildReport(self.Collect(), CONTEXT)
        other = crash.BuildReport(self.Collect(), CONTEXT)

        self.assertEqual(one, other)

    def test_the_sidecar_is_named_after_its_scene(self):
        collector = MakeCollector()
        collector.CaptureException(Raise(ValueError("boom")), Occurrence(seed=1, step=10))
        failure = collector.Groups()[0].failure

        self.assertEqual(crash.SidecarName(failure),
                         crash.SceneName(failure).replace(".json", ".crash.json"))

    def test_the_summary_lines_name_every_signature(self):
        collector = self.Collect()

        lines = crash.FormatSummary(collector)

        for group in collector.Groups():
            self.assertTrue(any(group.failure.signature in line for line in lines))

    def test_the_summary_says_when_it_is_not_showing_everything(self):
        collector = MakeCollector(max_signatures=1)
        collector.CaptureException(Raise(ValueError("a")), Occurrence(seed=1, step=1))
        collector.CaptureException(RaiseElsewhere(TypeError("b")), Occurrence(seed=1, step=2))

        lines = crash.FormatSummary(collector)

        self.assertTrue(any("not recorded" in line for line in lines))


class TestCollectorIsOffByDefaultWhenUnconfigured(unittest.TestCase):

    def test_no_saver_still_collects_the_finding(self):
        # The scene is the best artefact, not the only one.
        collector = CrashCollector()
        collector.CaptureException(Raise(ValueError("boom")), Occurrence(seed=1, step=10))

        group = collector.Groups()[0]
        self.assertEqual(group.occurrences, 1)
        self.assertEqual(group.scene_file, "")
        self.assertTrue(group.scene_note)


class TestTheObserverCannotBecomeTheCrash(unittest.TestCase):
    """`Log.OnCrash` runs mid-game inside an `except`. Nothing new may escape."""

    def setUp(self):
        from engine.log import Log
        self.addCleanup(setattr, Log, "crash_observer", None)

    def test_the_observer_sees_an_absorbed_exception(self):
        from engine import Engine
        from engine.log import Log

        seen = []
        Log.crash_observer = lambda category, exc: seen.append((category, exc))

        raised = ValueError("a card script blew up")
        try:
            raise raised
        except Exception as exc:
            with mock.patch.object(Engine, "SaveCrash", staticmethod(lambda: None)):
                Log.OnCrash("BOT", exc, "step", None)

        self.assertEqual(seen, [("BOT", raised)])

    def test_an_integrity_error_is_not_observed(self):
        # It is re-raised before this point so its caller can refuse to keep
        # what the run produced; observing it here would write that state.
        from engine import Engine
        from engine.log import Log

        seen = []
        Log.crash_observer = lambda category, exc: seen.append(exc)

        try:
            raise FabricatedInputError("timed out")
        except Exception as exc:
            with mock.patch.object(Engine, "SaveCrash", staticmethod(lambda: None)):
                with self.assertRaises(FabricatedInputError):
                    Log.OnCrash("BOT", exc, "step", None)

        self.assertEqual(seen, [])

    def test_an_observer_that_raises_does_not_escape(self):
        from engine import Engine
        from engine.log import Log

        def Explode(category, exc):
            raise RuntimeError("the reporter is broken")

        Log.crash_observer = Explode

        try:
            raise ValueError("a card script blew up")
        except Exception as exc:
            with mock.patch.object(Engine, "SaveCrash", staticmethod(lambda: None)):
                info = Log.OnCrash("BOT", exc, "step", None)

        self.assertIn("ValueError", info)

    def test_a_broken_observer_is_uninstalled_rather_than_retried(self):
        from engine import Engine
        from engine.log import Log

        calls = []

        def Explode(category, exc):
            calls.append(exc)
            raise RuntimeError("the reporter is broken")

        Log.crash_observer = Explode

        for _ in range(3):
            try:
                raise ValueError("a card script blew up")
            except Exception as exc:
                with mock.patch.object(Engine, "SaveCrash", staticmethod(lambda: None)):
                    Log.OnCrash("BOT", exc, "step", None)

        self.assertEqual(len(calls), 1)
        self.assertIsNone(Log.crash_observer)


class TestReportingNeverEndsTheRun(unittest.TestCase):
    """`Finish` runs outside `RunOne`'s try, so a raising reporter escapes."""

    def test_a_failing_capture_is_absorbed(self):
        from engine.device.manager.bot.runner import BotRunner

        def Explode():
            raise RuntimeError("the collector is broken")

        BotRunner.Guarded("capture this crash", Explode)

    def test_the_guard_does_not_swallow_success(self):
        from engine.device.manager.bot.runner import BotRunner

        done = []
        BotRunner.Guarded("do the thing", lambda: done.append(True))

        self.assertEqual(done, [True])

    def test_capture_with_no_collector_installed_is_a_no_op(self):
        from engine.device.manager.bot.runner import BotRunner

        self.assertIsNone(BotRunner.collector)
        BotRunner.CaptureException(None, None, 1, Raise(ValueError("boom")))
        BotRunner.CaptureReason(None, None, 1, "timeout-stall", "bot_max_steps", "x")


if __name__ == "__main__":
    unittest.main()
