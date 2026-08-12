"""The no-op-aware policy, and the rotation that makes several policies useful.

`NoOpAwarePolicy` is `first` with one change: an option whose ability resolves to
nothing goes to the back of the queue. Everything cleverer than that measured
worse, and the class docstring records what was tried -- these tests pin the
behaviour that survived.

Whether a policy reaches deeper states is a question for
`tools/coverage/depth.py` over a generated corpus, not for a unit test. What a
unit test can pin is that the partition is a partition: engine order is preserved
inside each group, no-ops end up last, and nothing here consults a clock, an RNG
or the world. See MARVEL-14.
"""

import unittest
from unittest import mock

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from engine.device.manager.bot.policies import (ASK, MixedPolicy, NO_OP_VERBS,
                                                NoOpAwarePolicy)
from engine.device.manager.bot.policy import BotOption


def Option(name, option_id=1):
    return BotOption(
        id=option_id,
        name=name,
        bind_id=0,
        bind_player_id=0,
        all_legal_targets=[],
        target_num_range=(0, 0),
        target_payment={},
        select_rule="",
        target_must_include_traits=[],
        failure_reason="",
        is_search=False,
    )


def Decision(*names, attempt=0, can_cancel=True):
    options = [Option(name, option_id=100 - index)
               for index, name in enumerate(names)]
    decision = mock.Mock()
    decision.selectable_options = options
    decision.options = options          # read by `RepeatGuard.Update`
    decision.attempt = attempt
    decision.can_cancel = can_cancel
    decision.player_id = 0
    decision.event_name = "WhenPlayerInTurn"
    decision.ability_type = "Normal"
    return decision


def ChooseName(policy, decision):
    """Run `Choose` and report which option's command came back."""
    built = {}

    def Build(option):
        command = mock.Mock()
        built[id(command)] = option.name
        return command

    with mock.patch("engine.device.manager.bot.policies.BotCommand.Build",
                    side_effect=Build):
        chosen = policy.Choose(decision)
    return built[id(chosen)]


class TestNoOpsGoLast(unittest.TestCase):

    def setUp(self):
        self.policy = NoOpAwarePolicy()

    def test_a_no_op_is_not_chosen_over_something_that_acts(self):
        self.assertEqual(ChooseName(self.policy, Decision(ASK, "Attack")), "Attack")

    def test_engine_order_decides_between_two_real_options(self):
        # The engine offers an identity's basic actions before anything in hand,
        # so its order is already a reasonable policy. Overriding it with a verb
        # ranking measured worse -- 424 cards resolved against 436.
        # A fresh policy per case: both decisions carry the same option ids, so
        # one `RepeatGuard` would read the second as a recurrence of the first
        # and walk one place down the queue.
        self.assertEqual(ChooseName(NoOpAwarePolicy(), Decision("Play", "Attack")),
                         "Play")
        self.assertEqual(ChooseName(NoOpAwarePolicy(), Decision("Attack", "Play")),
                         "Attack")

    def test_a_no_op_is_still_available_when_it_is_all_there_is(self):
        # Demoted, not removed: the engine may have offered nothing else, and a
        # policy that refuses to answer is a hung game.
        self.assertEqual(ChooseName(self.policy, Decision(ASK)), ASK)

    def test_walking_past_the_real_options_reaches_the_no_op(self):
        # `attempt` moves down the queue when the engine rejects an answer.
        self.assertEqual(ChooseName(self.policy, Decision(ASK, "Attack", attempt=1)),
                         ASK)

    def test_engine_order_survives_inside_the_no_op_group(self):
        first, second = f"{ASK}_1", ASK
        self.assertEqual(ChooseName(self.policy, Decision(first, second)), first)


class TestTheNoOpSet(unittest.TestCase):

    def test_ask_is_the_only_entry(self):
        # 621 of the 711 measured no-op choices, and the only one that is both
        # inert and ubiquitous. Widening this set is a measurement, not a guess
        # -- see docs/no-op-decisions.md.
        self.assertEqual(set(NO_OP_VERBS), {ASK})

    def test_a_duplicated_ability_keeps_its_verb(self):
        # `GetDisplayName` appends `_<index>` to the second and later copies of
        # an action ability on one card.
        policy = NoOpAwarePolicy()

        self.assertTrue(policy.IsNoOp(Option("Ask_1")))
        self.assertTrue(policy.IsNoOp(Option("Ask")))

    def test_an_unrelated_name_is_not_matched_by_prefix(self):
        policy = NoOpAwarePolicy()

        self.assertFalse(policy.IsNoOp(Option("Asking_Price")))
        self.assertFalse(policy.IsNoOp(Option("Attack")))


class TestMixedRotation(unittest.TestCase):
    """The three policies reach different cards, so a run wants all of them."""

    def Policy(self, names="first,heuristic,random"):
        from engine.device.manager.bot import policies

        with mock.patch.object(policies.BOT_MIXED_POLICIES, "value", names):
            return MixedPolicy(7)

    def test_it_rotates_one_policy_per_game(self):
        policy = self.Policy()

        seen = []
        for game in range(6):
            policy.OnGameStart(1000 + game)
            seen.append(policy.current.name)

        self.assertEqual(seen, ["first", "heuristic", "random",
                                "first", "heuristic", "random"])

    def test_the_rotation_does_not_depend_on_the_game_seed(self):
        # Rotation by game index rather than by seed, so a run of N games covers
        # the policies evenly however the seeds fall.
        a, b = self.Policy(), self.Policy()

        a.OnGameStart(1)
        b.OnGameStart(99999)

        self.assertEqual(a.current.name, b.current.name)

    def test_it_refuses_to_contain_itself(self):
        policy = self.Policy("first,mixed,random")

        self.assertNotIn("mixed", [p.name for p in policy.policies])

    def test_an_empty_list_still_plays(self):
        policy = self.Policy(",")

        self.assertEqual([p.name for p in policy.policies], ["first"])

    def test_choosing_delegates_to_the_current_policy(self):
        policy = self.Policy("first")
        policy.OnGameStart(1)

        with mock.patch.object(policy.current, "Choose",
                               return_value="answer") as choose:
            self.assertEqual(policy.Choose("a decision"), "answer")
        choose.assert_called_once_with("a decision")


if __name__ == "__main__":
    unittest.main()
