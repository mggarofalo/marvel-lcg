"""A logged error has to stay detectable, or the corpus gate cannot fail.

`Log.HasError` is not a debug convenience. `BotRunner.Verify` uses it to decide
whether a generated scene may enter the corpus, and `tools/spec/harness.py` uses
it to catch a spec case that "passed" over an exception the engine swallowed.
Both are oracles the C# port will be held to.

It reads `Log.log_statistics`, which `LogHelper.StatLog` writes -- and `StatLog`
used to return immediately when `Build.release`, which `build.py` hardcodes
true. So in every real build the counts stayed empty, `HasError` always returned
False, and both gates always passed. `LogHelper.PrintLog` had the same shape a
second time: it returned early for a hidden category *before* recording, and
`-bot` expands to `-hidden_log_categories CONTROLLER WEB VERSION STATISTICS`.

Measured before the fix, by generating a game, corrupting one recorded step
digest and re-verifying: the corrupted scene was accepted.

These tests pin the whole chain -- an error is counted, it survives both
display filters, and `BotRunner.Verify` acts on it. See MARVEL-65.
"""

import unittest
from unittest import mock

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from build import Build
from engine.log import Log
from engine.log.log import HIDDEN_LOG_CATEGORIES


class LogErrorTestCase(unittest.TestCase):
    """Leaves `Log.log_statistics` as it found it.

    The counts are global and `Log.Setup()` clears them per game, so a test that
    leaked into the next one would be indistinguishable from the bug.
    """

    def setUp(self):
        self.addCleanup(setattr, Log, "log_statistics", Log.log_statistics)
        Log.log_statistics = {}


class TestAnErrorIsCountedOnARealBuild(LogErrorTestCase):

    def test_this_is_a_release_build(self):
        # Everything below is only meaningful because of this. `build.py` sets
        # it unconditionally, and it is what silenced the counts.
        self.assertTrue(Build.release)

    def test_an_asserted_error_is_visible_to_has_error(self):
        Log.Assert("BOT", "a real error was logged")

        self.assertTrue(Log.HasError(error=True))

    def test_a_logged_traceback_is_visible_to_has_error(self):
        try:
            raise ValueError("a card script blew up")
        except ValueError as exc:
            Log.FailedTrace("BOT", exc)

        self.assertTrue(Log.HasError(error=True))

    def test_a_warning_is_a_warning_and_not_an_error(self):
        # `BotRunner.Verify` asks for errors only; a warning must not fail a
        # verification, and `game.py` asks for warnings only.
        Log.Warn("BOT", "something looked odd")

        self.assertTrue(Log.HasError(warn=True))
        self.assertFalse(Log.HasError(error=True))

    def test_ordinary_logging_counts_as_neither(self):
        Log.Info("BOT", "playing a card")
        Log.Debug("BOT", "some detail")

        self.assertFalse(Log.HasError(error=True))
        self.assertFalse(Log.HasError(warn=True))

    def test_a_quiet_run_has_no_error(self):
        self.assertFalse(Log.HasError(error=True))


class TestScoping(LogErrorTestCase):

    def test_an_error_is_findable_under_its_own_category(self):
        Log.Assert("REPLAY", "digest mismatch")

        self.assertTrue(Log.HasError("REPLAY", error=True))

    def test_another_category_is_unaffected(self):
        Log.Assert("REPLAY", "digest mismatch")

        self.assertFalse(Log.HasError("BOT", error=True))

    def test_no_category_means_every_category(self):
        # `HasError()` resolves to the "ALL" aggregate, which is what
        # `BotRunner.Verify` relies on -- it does not know which subsystem will
        # report the divergence.
        Log.Assert("REPLAY", "digest mismatch")

        self.assertTrue(Log.HasError(error=True))

    def test_setup_clears_the_counts(self):
        # This is what scopes a verdict to one game: `Game.GameSetup` calls
        # `Log.Setup()`, so errors logged while *generating* a scene do not
        # decide whether *replaying* it passed.
        Log.Assert("BOT", "an error from the previous game")
        Log.Setup()

        self.assertFalse(Log.HasError(error=True))


class TestAHiddenCategoryIsStillCounted(LogErrorTestCase):
    """`-bot` hides four categories. Hiding is about display, not detection."""

    def test_an_error_in_a_hidden_category_is_still_an_error(self):
        with mock.patch.object(HIDDEN_LOG_CATEGORIES, "value", ["CONTROLLER"]):
            Log.Assert("CONTROLLER", "an error nobody wanted printed")

        self.assertTrue(Log.HasError(error=True))
        self.assertTrue(Log.HasError("CONTROLLER", error=True))

    def test_hiding_it_still_suppresses_the_output(self):
        # The point of the flag is unchanged -- only the counting moved.
        with mock.patch.object(HIDDEN_LOG_CATEGORIES, "value", ["CONTROLLER"]):
            with mock.patch.object(Log, "log_statistics", {}):
                with mock.patch("engine.log.log.LogHelper.PrintInternal") as printed:
                    Log.Assert("CONTROLLER", "an error nobody wanted printed")

        printed.assert_not_called()


class TestTheCorpusGateCanFail(LogErrorTestCase):
    """`BotRunner.Verify` is the only thing standing between a bad replay and
    the corpus, and `Log.HasError` is the only half of it that can say no.

    `TestRun.Run` returns True unconditionally (`game/test/test_run.py`); it
    reports a failed case by logging "Fail". So a verification that ignores the
    log cannot reject anything -- which is what this issue was.
    """

    @staticmethod
    def Verify(run_result, log_an_error):
        from engine.device.manager.bot.runner import BotRunner

        def Run(game, cases, do_save=False):
            if log_an_error:
                Log.Assert("REPLAY", "Digest mismatch (#12 / 47)")
            return run_result

        test_run = mock.Mock()
        test_run.Run.side_effect = Run
        test = mock.Mock()

        with mock.patch.dict("sys.modules", {
            "game.test": mock.Mock(Test=test),
            "game.test.test_run": mock.Mock(TestRun=test_run),
        }):
            return BotRunner.Verify(mock.Mock(), "scene.json")

    def test_a_clean_replay_verifies(self):
        self.assertTrue(self.Verify(run_result=True, log_an_error=False))

    def test_a_replay_that_logged_an_error_is_refused(self):
        # The regression. `TestRun.Run` said True; the log said otherwise.
        self.assertFalse(self.Verify(run_result=True, log_an_error=True))

    def test_a_replay_that_did_not_complete_is_refused(self):
        self.assertFalse(self.Verify(run_result=False, log_an_error=False))


if __name__ == "__main__":
    unittest.main()
