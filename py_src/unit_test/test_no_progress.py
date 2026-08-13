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


def Tight(**kwargs):
    """A guard with only the consecutive-identical counter armed."""
    kwargs.setdefault("stall_limit", 0)
    return NoProgressGuard(**kwargs)


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

    def test_alternating_digests_never_fire_the_consecutive_counter(self):
        # This is the MARVEL-99 blind spot stated as a property: a period-2
        # cycle changes the digest on every step, so "consecutive identical"
        # resets every step and never fires however long the cycle runs.
        # `TestAStaleRunIsCaught` below is the counter that does see it.
        guard = Tight(limit=2)

        step, _ = Feed(guard, ["a", "b"] * 500)

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
        guard = Tight(limit=0)

        step, _ = Feed(guard, ["same"] * 500)

        self.assertIsNone(step)

    def test_the_default_is_well_clear_of_measured_play(self):
        # 44 scenes and 4759 decisions put the longest legitimate run at 4.
        # This is not a style preference: the margin is the whole reason a
        # false positive is not expected. See docs/no-op-decisions.md.
        self.assertGreaterEqual(NoProgressGuard().limit, 16)


class TestAStaleRunIsCaught(unittest.TestCase):
    """The loose form: a cycle whose period is greater than 1.

    MARVEL-99. Fourteen corpus cases alternated between two board states for the
    whole 20,000-step budget. Every decision changed the digest, so the counter
    above never fired and only the wall-clock cap stopped them.
    """

    def test_a_period_two_cycle_raises(self):
        guard = NoProgressGuard(limit=0, stall_limit=8)

        step, error = Feed(guard, ["a", "b"] * 50)

        # "a" and "b" are novel on their first sighting; the eight after that
        # are the run.
        self.assertEqual(step, 9)
        self.assertIsNotNone(error)

    def test_a_longer_cycle_raises_too(self):
        # Nothing about the detector knows the period is 2.
        guard = NoProgressGuard(limit=0, stall_limit=8)

        step, _ = Feed(guard, ["a", "b", "c", "d", "e"] * 20)

        self.assertEqual(step, 12)

    def test_the_report_says_it_is_a_cycle_and_names_its_period(self):
        guard = NoProgressGuard(limit=0, stall_limit=6)

        _, error = Feed(guard, ["a", "b", "c"] * 20)

        message = str(error)
        self.assertIn("No new state for 6 decisions", message)
        self.assertIn("cycling between 3 board state(s)", message)
        # Without this the reader chases the wrong guard: the digest *did*
        # change, every time.
        self.assertIn("changed the digest", message)

    def test_the_report_names_more_than_one_decision(self):
        # `recent` is emptied by every digest change, and in a period-2 cycle
        # that is every decision -- so the loose report needs its own tail or it
        # prints a single line and diagnoses nothing.
        guard = NoProgressGuard(limit=0, stall_limit=8)

        _, error = Feed(guard, ["a", "b"] * 50)

        self.assertGreaterEqual(str(error).count("WhenPlayerInTurn"), 3)

    def test_a_game_that_keeps_reaching_new_states_never_fires(self):
        guard = NoProgressGuard(limit=0, stall_limit=8)

        step, _ = Feed(guard, [f"state-{n}" for n in range(500)])

        self.assertIsNone(step)

    def test_revisiting_a_state_briefly_does_not_fire(self):
        # The shape of the longest legitimate run measured: a handful of
        # decisions inside old states, then somewhere new.
        guard = NoProgressGuard(limit=0, stall_limit=8)

        digests = []
        for block in range(20):
            digests += [f"new-{block}"] + ["old"] * 6
        step, _ = Feed(guard, digests)

        self.assertIsNone(step)

    def test_a_new_game_forgets_every_state(self):
        guard = NoProgressGuard(limit=0, stall_limit=8)
        Feed(guard, ["a", "b"] * 3)

        guard.Reset()
        step, _ = Feed(guard, ["a", "b"] * 4)

        self.assertIsNone(step)

    def test_a_stall_limit_of_zero_disables_it(self):
        guard = NoProgressGuard(limit=0, stall_limit=0)

        step, _ = Feed(guard, ["a", "b"] * 500)

        self.assertIsNone(step)

    def test_the_tight_check_wins_when_both_would_fire(self):
        # It fires sooner and its report names one decision rather than a
        # cycle, so it is the better diagnosis of the same event.
        guard = NoProgressGuard(limit=4, stall_limit=4)

        _, error = Feed(guard, ["same"] * 50)

        self.assertIn("left the state digest identical", str(error))

    def test_the_default_clears_the_longest_measured_legitimate_run(self):
        # 902 completed scenes and 105,633 decisions put that run at 10, and the
        # 20,000-step wall it replaces is what a false negative costs. The
        # margin is the whole argument -- see docs/no-op-decisions.md.
        guard = NoProgressGuard()
        self.assertGreaterEqual(guard.stall_limit, 100)
        self.assertLess(guard.stall_limit, 20000)


class TestTheStateKeyIsStableAcrossProcesses(unittest.TestCase):

    def test_the_same_digest_always_gives_the_same_key(self):
        # `hash()` on a str is salted per process. If this key were, two runs of
        # one seed could disagree about whether the guard fired.
        from engine.device.manager.bot.progress import DigestKey

        self.assertEqual(DigestKey('{"v":2,"cards":[]}'),
                         "762f4078dfe1655f485e457be7a551ed")

    def test_different_digests_give_different_keys(self):
        from engine.device.manager.bot.progress import DigestKey

        self.assertNotEqual(DigestKey("a"), DigestKey("b"))


if __name__ == "__main__":
    unittest.main()
