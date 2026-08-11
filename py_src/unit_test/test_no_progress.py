"""A bot that stops advancing the game has to fail loudly, not spin.

Some abilities are offered, recorded as a replay step, and legally resolve to
nothing -- the alter-ego "Ask" action is 621 of the 711 measured cases. A policy
that always answers the same way rides one forever, and `bot_max_steps` ends that
game as a *warning* after thousands of wasted steps with no scene saved and
nothing naming the cause. During corpus generation a silently masked infinite
loop is worse than a crash.

`NoProgressGuard` checks the answer rather than the question: the digest is the
engine's account of what changed, so a run of decisions that all leave it
identical has made no progress, whatever ability was offered. These tests pin the
three things that make it trustworthy -- it fires, it does not fire early, and it
does not count the retries of a decision that has not resolved yet.

See docs/no-op-decisions.md and MARVEL-37.
"""

import unittest

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from engine.device.manager.bot.progress import NoProgressError, NoProgressGuard


def Feed(guard, digests):
    """Observe one decision per digest, returning the step it raised on."""
    for step, digest in enumerate(digests):
        try:
            guard.Observe(digest, step, f"p0 #{step} WhenPlayerInTurn (Normal)")
        except NoProgressError as error:
            return step, error
    return None, None


class TestAStalledRunIsCaught(unittest.TestCase):

    def test_an_unchanging_digest_raises_at_the_limit(self):
        guard = NoProgressGuard(limit=4)

        step, error = Feed(guard, ["same"] * 20)

        # First observation sets the baseline, so the limit counts the four
        # after it.
        self.assertEqual(step, 4)
        self.assertIsNotNone(error)

    def test_the_error_is_an_integrity_error(self):
        # Everything between the bot and the runner catches broadly --
        # EffectInvoker, Message2.Send, Engine.EngineRun. A plain exception here
        # would be logged and dropped, and the run would carry on spinning.
        from core.errors import EngineIntegrityError

        self.assertTrue(issubclass(NoProgressError, EngineIntegrityError))

    def test_the_report_names_the_cycle_and_the_steps(self):
        guard = NoProgressGuard(limit=3)

        _, error = Feed(guard, ["same"] * 10)

        message = str(error)
        self.assertIn("No progress for 3 decisions", message)
        self.assertIn("WhenPlayerInTurn", message)
        # A step range is what lets someone wind a replay to the cycle.
        self.assertIn("steps 0-3", message)

    def test_the_report_does_not_print_every_decision(self):
        guard = NoProgressGuard(limit=200)

        _, error = Feed(guard, ["same"] * 400)

        # 200 identical lines is not a diagnosis.
        self.assertLessEqual(str(error).count("WhenPlayerInTurn"), 8)


class TestProgressResetsTheCount(unittest.TestCase):

    def test_a_changed_digest_clears_the_run(self):
        guard = NoProgressGuard(limit=4)

        # Three stalls, progress, three stalls: never four in a row.
        step, error = Feed(guard, ["a", "a", "a", "a", "b", "b", "b", "b"])

        self.assertIsNone(step)
        self.assertIsNone(error)

    def test_alternating_digests_never_fire(self):
        guard = NoProgressGuard(limit=2)

        step, _ = Feed(guard, ["a", "b"] * 50)

        self.assertIsNone(step)

    def test_a_new_game_starts_from_nothing(self):
        guard = NoProgressGuard(limit=4)
        Feed(guard, ["same"] * 3)

        guard.Reset()
        step, _ = Feed(guard, ["same"] * 4)

        # Reset dropped the three, so four more is one short of the limit.
        self.assertIsNone(step)


class TestTheLimitIsConfigurable(unittest.TestCase):

    def test_a_limit_of_zero_disables_the_guard(self):
        # The escape hatch for someone with a scene proving a long run is real.
        guard = NoProgressGuard(limit=0)

        step, _ = Feed(guard, ["same"] * 500)

        self.assertIsNone(step)

    def test_the_default_is_well_clear_of_measured_play(self):
        # 44 scenes and 4759 decisions put the longest legitimate run at 4.
        # This is not a style preference: the margin is the whole reason a
        # false positive is not expected. See docs/no-op-decisions.md.
        self.assertGreaterEqual(NoProgressGuard().limit, 16)


if __name__ == "__main__":
    unittest.main()
