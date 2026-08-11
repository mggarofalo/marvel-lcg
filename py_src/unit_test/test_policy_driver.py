"""The determinism harness must be able to drive the engine with a real policy.

`tools/determinism/headless.py` shipped answering every prompt with a decline,
because no bot existed when it was written. A decline-only game never plays a
card, so it never opens the response windows where two forced abilities meet on
one message -- which is why `probe_forced_selection` reported 60 batches and not
one with a second candidate, and why every digest-based argument about MARVEL-39
and MARVEL-40 was vacuous.

`PolicyDriver` is the swap the module docstring always pointed at. These tests
pin the part that is easy to get wrong and invisible when it is: the decision
handed to the policy has to be the one the real bot device would hand it,
including `attempt`, because `FirstLegalPolicy` walks down its option list on
`attempt` alone and a driver that always reports 0 would loop on a rejected
answer forever.

See MARVEL-69.
"""

import unittest
from unittest import mock

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from tools.determinism.headless import PolicyDriver, build_decide, decline_everything


class RecordingPolicy:
    """Captures the decisions it is asked, and answers with a cancel."""

    def __init__(self):
        self.seen = []

    def OnGameStart(self, seed):
        pass

    def Choose(self, decision):
        from engine.device.manager.bot.command import BotCommand

        self.seen.append(decision)
        return BotCommand.Cancel()


def Payload(event_name="WhenPlayerInTurn", ability_type="Normal"):
    from engine.device.manager.base import AskOptionPayload

    return AskOptionPayload(
        options_json="[]",
        ability_type=ability_type,
        event_name=event_name,
        prompt_text="a prompt",
        show_cancel=True,
        replay_input="{}",
    )


class DriverTestCase(unittest.TestCase):
    """Runs the driver with `Engine.game` stubbed to a controllable step id."""

    def setUp(self):
        self.policy = RecordingPolicy()
        self.driver = PolicyDriver(self.policy)
        self.engine = mock.Mock()
        self.engine.game.controller_manager.replay.current_step_id = 0

    def Ask(self, player_id=0, step_id=None, **kwargs):
        if step_id is not None:
            self.engine.game.controller_manager.replay.current_step_id = step_id
        with mock.patch("engine.Engine", self.engine):
            return self.driver(player_id, Payload(**kwargs))


class TestThePolicySeesARealDecision(DriverTestCase):

    def test_the_payload_reaches_the_policy_intact(self):
        self.Ask(event_name="AfterMinionEngagePlayer", ability_type="ForcedResponse")

        decision = self.policy.seen[0]
        self.assertEqual(decision.event_name, "AfterMinionEngagePlayer")
        self.assertEqual(decision.ability_type, "ForcedResponse")
        self.assertEqual(decision.prompt_text, "a prompt")
        self.assertTrue(decision.can_cancel)

    def test_the_step_id_is_the_replay_step(self):
        # Not a counter of its own: a policy comparing step ids against the
        # replay would be comparing against something else entirely.
        self.Ask(step_id=17)

        self.assertEqual(self.policy.seen[0].step_id, 17)

    def test_the_answer_is_json_the_engine_can_read(self):
        answer = self.Ask()

        # `DoGetInput` hands this straight to `WhenInput`, the same entry point
        # a browser POST uses.
        self.assertIsInstance(answer, str)
        self.assertTrue(answer.startswith("{"))


class TestAttemptTracking(DriverTestCase):
    """`attempt` is how a policy learns the engine rejected its last answer."""

    def test_a_fresh_decision_starts_at_zero(self):
        self.Ask(step_id=3)

        self.assertEqual(self.policy.seen[0].attempt, 0)

    def test_asking_the_same_step_again_increments(self):
        self.Ask(step_id=3)
        self.Ask(step_id=3)
        self.Ask(step_id=3)

        self.assertEqual([d.attempt for d in self.policy.seen], [0, 1, 2])

    def test_moving_on_resets(self):
        self.Ask(step_id=3)
        self.Ask(step_id=3)
        self.Ask(step_id=4)

        self.assertEqual([d.attempt for d in self.policy.seen], [0, 1, 0])

    def test_a_different_player_is_a_different_decision(self):
        # Two players can be asked at the same step id; treating that as a retry
        # would walk the second player past its first legal option for no reason.
        self.Ask(player_id=0, step_id=3)
        self.Ask(player_id=1, step_id=3)

        self.assertEqual([d.attempt for d in self.policy.seen], [0, 0])


class TestBuildDecide(unittest.TestCase):

    def test_decline_is_still_available(self):
        # The old behaviour has to stay reachable: it is the baseline the
        # MARVEL-69 measurement is against.
        self.assertIs(build_decide("decline"), decline_everything)

    def test_a_named_policy_is_wrapped(self):
        decide = build_decide("first")

        self.assertIsInstance(decide, PolicyDriver)
        self.assertEqual(decide.policy.name, "first")

    def test_a_seeded_policy_gets_its_seed(self):
        decide = build_decide("random", 4242)

        self.assertEqual(decide.policy.name, "random")
        self.assertEqual(decide.policy.seed, 4242)

    def test_two_random_drivers_on_one_seed_agree(self):
        # The policy owns a private RNG stream and must never touch the game's,
        # so the same seed has to give the same sequence.
        first = build_decide("random", 7).policy
        second = build_decide("random", 7).policy

        self.assertEqual([first.rng.random() for _ in range(5)],
                         [second.rng.random() for _ in range(5)])


if __name__ == "__main__":
    unittest.main()
