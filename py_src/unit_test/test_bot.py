"""Tests for the headless bot device's pure decision logic.

These exercise the parts that turn an `AskOptionPayload` into a
`CommandDescriptor`: parsing the option payload the way the web client does,
picking targets and resources, and the policy's option walk. They need no
engine bootstrap and no game.

End-to-end coverage — a whole game, saved and replayed with per-step digest
equality — comes from `python main.py -device bot -bot_verify`.
"""

import json
import unittest

from engine.config import ConfigVariables
from engine.device.manager.bot.command import BotCommand
from engine.device.manager.bot.policies import FirstLegalPolicy, RepeatGuard, SeededRandomPolicy
from engine.device.manager.bot.policy import BotDecision, BotOptionParser, BotStuck


def MakeOptionJson(**overrides):
    """One entry shaped like `EffectDescriptor` after `Json.Dumps`."""
    option = {
        'id': 10,
        'name': 'Play',
        'bind_id': 3,
        'bind_player_id': 0,
        'all_legal_targets': [],
        'target_num_range': [0, 0],
        'target_payment': {},
        'select_rule': '',
        'select_rule_param': [0, 0],
        'target_must_include_traits': [],
        'failure_reason': '',
        'is_search': False,
        'pay_size_is_effect': False,
    }
    option.update(overrides)
    return option


def ParseOptions(*options):
    return BotOptionParser.Parse(json.dumps(list(options)))


def MakeDecision(options, *, can_cancel=True, attempt=0, event_name='WhenPlayerInTurn'):
    return BotDecision(
        player_id=0,
        step_id=1,
        attempt=attempt,
        event_name=event_name,
        ability_type='Normal',
        prompt_text='',
        can_cancel=can_cancel,
        options=options,
        replay_input='{}',
        world=None,
    )


class TestOptionParsing(unittest.TestCase):

    def test_payment_keys_come_back_as_ints(self):
        # JSON turns the `Dict[int, Payment]` keys into strings on the way out.
        options = ParseOptions(MakeOptionJson(
            all_legal_targets=[7],
            target_num_range=[1, 1],
            target_payment={'7': {'cost': '2', 'rule': [], 'payment': [{'55': 'R'}]}},
        ))
        option = options[0]
        self.assertIn(7, option.target_payment)
        self.assertEqual(option.target_payment[7].payment[0].effect_id, 55)
        self.assertEqual(option.target_payment[7].payment[0].res_text, 'R')

    def test_targets_are_dropped_when_none_can_be_selected(self):
        # Parity with `EffectDescriptor` in public/js/marvel/data.ts.
        options = ParseOptions(MakeOptionJson(all_legal_targets=[7, 8], target_num_range=[0, 0]))
        self.assertEqual(options[0].all_legal_targets, [])

    def test_payment_key_falls_back_to_zero(self):
        options = ParseOptions(MakeOptionJson(
            all_legal_targets=[7],
            target_num_range=[1, 1],
            target_payment={'0': {'cost': '1', 'rule': [], 'payment': []}},
        ))
        self.assertEqual(options[0].GetPaymentKey([7]), 0)

    def test_option_with_failure_reason_is_not_selectable(self):
        options = ParseOptions(MakeOptionJson(failure_reason='not enough resources'))
        self.assertFalse(options[0].is_selectable)


class TestCommandBuilding(unittest.TestCase):

    def test_selects_the_minimum_number_of_targets(self):
        options = ParseOptions(MakeOptionJson(all_legal_targets=[7, 8, 9], target_num_range=[2, 3]))
        command = BotCommand.Build(options[0])
        self.assertIsNotNone(command)
        self.assertEqual(command.targets, ['7', '8'])

    def test_selects_the_maximum_when_target_count_is_the_cost_effect(self):
        options = ParseOptions(MakeOptionJson(
            all_legal_targets=[7, 8, 9],
            target_num_range=[0, 3],
            pay_size_is_effect=True,
        ))
        command = BotCommand.Build(options[0])
        self.assertIsNotNone(command)
        self.assertEqual(command.targets, ['7', '8', '9'])

    def test_variable_card_cost_can_use_the_legacy_minimum(self):
        from engine.device.manager.bot.command import PAY_VARIABLE_CARD_COST

        options = ParseOptions(MakeOptionJson(
            all_legal_targets=[7, 8, 9],
            target_num_range=[0, 3],
            pay_size_is_effect=True,
        ))
        ConfigVariables.ParseString("-no_bot_pay_variable_card_cost")
        try:
            self.assertFalse(PAY_VARIABLE_CARD_COST.value)
            self.assertEqual(BotCommand.Build(options[0]).targets, [])
        finally:
            ConfigVariables.ParseString("-bot_pay_variable_card_cost")
        self.assertEqual(BotCommand.Build(options[0]).targets, ['7', '8', '9'])

    def test_tries_the_largest_counter_cost_first(self):
        options = ParseOptions(*[
            MakeOptionJson(id=value, name=str(value), pay_size_is_effect=True)
            for value in range(1, 4)
        ])
        commands = BotCommand.BuildAll(options)
        self.assertEqual([command.id for command in commands], ['3'])

    def test_first_and_random_policies_both_use_the_largest_counter_cost(self):
        options = ParseOptions(*[
            MakeOptionJson(id=value, name=str(value), pay_size_is_effect=True)
            for value in range(1, 4)
        ])
        decision = MakeDecision(options, can_cancel=False)

        self.assertEqual(FirstLegalPolicy().Choose(decision).id, '3')
        self.assertEqual(SeededRandomPolicy(7).Choose(decision).id, '3')

    def test_rejects_an_option_without_enough_legal_targets(self):
        options = ParseOptions(MakeOptionJson(all_legal_targets=[7], target_num_range=[2, 2]))
        self.assertIsNone(BotCommand.Build(options[0]))

    def test_rejects_an_option_the_engine_already_marked_as_failing(self):
        options = ParseOptions(MakeOptionJson(failure_reason='cannot pay'))
        self.assertIsNone(BotCommand.Build(options[0]))

    def test_pays_only_as_much_as_the_cost_needs(self):
        options = ParseOptions(MakeOptionJson(
            target_payment={'0': {'cost': '2', 'rule': [], 'payment': [
                {'51': 'R'}, {'52': 'B'}, {'53': 'Y'},
            ]}},
        ))
        command = BotCommand.Build(options[0])
        self.assertIsNotNone(command)
        self.assertEqual(command.resources, ['51', '52'])

    def test_rejects_an_option_it_cannot_afford(self):
        options = ParseOptions(MakeOptionJson(
            target_payment={'0': {'cost': '3', 'rule': [], 'payment': [{'51': 'R'}]}},
        ))
        self.assertIsNone(BotCommand.Build(options[0]))

    def test_respects_a_coloured_cost(self):
        # A single physical resource cannot pay a mental cost; a wild one can.
        options = ParseOptions(MakeOptionJson(
            target_payment={'0': {'cost': 'B', 'rule': [], 'payment': [{'51': 'R'}, {'52': 'G'}]}},
        ))
        command = BotCommand.Build(options[0])
        self.assertIsNotNone(command)
        self.assertEqual(command.resources, ['51', '52'])

    def test_spends_up_to_the_ceiling_of_an_up_to_cost(self):
        # This used to assert `[]`, and `[]` is legal -- an `UpTo` cost is met
        # by spending nothing. It is also the card doing nothing: "spend up to
        # 3" is a ceiling on an effect, not a price, so the minimum answer is
        # the one answer that wastes the option. MARVEL-135.
        options = ParseOptions(MakeOptionJson(
            target_payment={'0': {'cost': '3', 'rule': ['UpTo'], 'payment': [
                {'51': 'R'}, {'52': 'B'},
            ]}},
        ))
        command = BotCommand.Build(options[0])
        self.assertIsNotNone(command)
        self.assertEqual(command.resources, ['51', '52'])

    def test_an_up_to_ceiling_is_still_a_ceiling(self):
        # Stops at 3 of the 4 on offer. Without this the "maximal" reading
        # would be "everything", and an `UpTo` cost would be overpaid -- which
        # `IsMatchCost` rejects, so the option would come back unaffordable.
        options = ParseOptions(MakeOptionJson(
            target_payment={'0': {'cost': '3', 'rule': ['UpTo'], 'payment': [
                {'51': 'R'}, {'52': 'B'}, {'53': 'Y'}, {'54': 'R'},
            ]}},
        ))
        command = BotCommand.Build(options[0])
        self.assertIsNotNone(command)
        self.assertEqual(command.resources, ['51', '52', '53'])

    def test_a_resource_that_would_break_the_ceiling_is_skipped_not_final(self):
        # A greedy walk that returned at the first refusal would take nothing
        # here: the four-resource generator is offered first and does not fit
        # under a ceiling of 3.
        options = ParseOptions(MakeOptionJson(
            target_payment={'0': {'cost': '3', 'rule': ['UpTo'], 'payment': [
                {'51': 'RRRR'}, {'52': 'B'},
            ]}},
        ))
        command = BotCommand.Build(options[0])
        self.assertIsNotNone(command)
        self.assertEqual(command.resources, ['52'])

    def test_a_variable_cost_spends_the_whole_offer(self):
        # A printed X. The cost text is "0" -- `ResRBYGA.FromText` reads "X" as
        # zero and always has -- so `Variable` in the rule list is the only
        # thing separating this from a card that is genuinely free, and the
        # next test is the free card.
        options = ParseOptions(MakeOptionJson(
            target_payment={'0': {'cost': '0', 'rule': ['Variable'], 'payment': [
                {'51': 'R'}, {'52': 'B'}, {'53': 'Y'},
            ]}},
        ))
        command = BotCommand.Build(options[0])
        self.assertIsNotNone(command)
        self.assertEqual(command.resources, ['51', '52', '53'])

    def test_an_explicit_payment_takes_exactly_the_requested_amount(self):
        options = ParseOptions(MakeOptionJson(
            target_payment={'0': {'cost': '0', 'rule': ['Variable'], 'payment': [
                {'51': 'R'}, {'52': 'B'}, {'53': 'Y'},
            ]}},
        ))
        payment = BotCommand.BuildPayment(options[0], [], amount=2)
        self.assertEqual(payment, [51, 52])

    def test_an_explicit_payment_counts_icons_not_cards(self):
        options = ParseOptions(MakeOptionJson(
            target_payment={'0': {'cost': '0', 'rule': ['Variable'], 'payment': [
                {'51': 'RR'}, {'52': 'B'},
            ]}},
        ))
        self.assertEqual(
            BotCommand.BuildPayment(options[0], [], amount=2), [51])
        self.assertEqual(
            BotCommand.BuildPayment(options[0], [], amount=1), [52])

    def test_a_cost_reduction_contributes_zero_to_the_explicit_amount(self):
        options = ParseOptions(MakeOptionJson(
            target_payment={'0': {'cost': '1', 'rule': [], 'payment': [
                {'51': '-1'}, {'52': 'B'},
            ]}},
        ))
        self.assertEqual(
            BotCommand.BuildPayment(options[0], [], amount=0), [51])

    def test_an_explicit_payment_respects_the_printed_cost(self):
        # Paying one is numerically exact, but a physical cannot meet mental.
        # The later wild is the first exact legal combination.
        options = ParseOptions(MakeOptionJson(
            target_payment={'0': {'cost': 'B', 'rule': [], 'payment': [
                {'51': 'R'}, {'52': 'G'},
            ]}},
        ))
        self.assertEqual(
            BotCommand.BuildPayment(options[0], [], amount=1), [52])

    def test_an_explicit_zero_is_not_the_default_maximal_payment(self):
        options = ParseOptions(MakeOptionJson(
            target_payment={'0': {'cost': '0', 'rule': ['Variable'], 'payment': [
                {'51': 'R'}, {'52': 'B'},
            ]}},
        ))
        self.assertEqual(BotCommand.BuildPayment(options[0], [], amount=0), [])
        self.assertEqual(BotCommand.BuildPayment(options[0], []), [51, 52])

    def test_an_unavailable_explicit_amount_is_refused(self):
        options = ParseOptions(MakeOptionJson(
            target_payment={'0': {'cost': '0', 'rule': ['Variable'], 'payment': [
                {'51': 'RR'}, {'52': 'B'},
            ]}},
        ))
        self.assertIsNone(BotCommand.BuildPayment(options[0], [], amount=4))

    def test_a_free_card_still_pays_nothing_for_it(self):
        # Same cost text, no rule. Nothing about "0" changed.
        options = ParseOptions(MakeOptionJson(
            target_payment={'0': {'cost': '0', 'rule': [], 'payment': [
                {'51': 'R'}, {'52': 'B'},
            ]}},
        ))
        command = BotCommand.Build(options[0])
        self.assertIsNotNone(command)
        self.assertEqual(command.resources, [])

    def test_the_planner_can_be_put_back_the_way_it_was(self):
        # `-bot_pay_variable_cost false` is what a replay recorded before
        # MARVEL-135 was generated under, so it has to still be reachable.
        from engine.device.manager.bot.command import PAY_VARIABLE_COST

        options = ParseOptions(MakeOptionJson(
            target_payment={'0': {'cost': '0', 'rule': ['Variable'], 'payment': [
                {'51': 'R'},
            ]}},
        ))
        # `-no_<flag>` is how a `Bool` is turned off -- a bare flag name is
        # presence, so `-bot_pay_variable_cost false` would read as True with
        # a stray value. See `ConfigVariables.ParseArguments`.
        ConfigVariables.ParseString("-no_bot_pay_variable_cost")
        try:
            self.assertFalse(PAY_VARIABLE_COST.value)
            self.assertEqual(BotCommand.Build(options[0]).resources, [])
        finally:
            ConfigVariables.ParseString("-bot_pay_variable_cost")
        self.assertEqual(BotCommand.Build(options[0]).resources, ['51'])

    def test_ignored_cost_needs_no_resources(self):
        options = ParseOptions(MakeOptionJson(
            target_payment={'0': {'cost': '*', 'rule': [], 'payment': []}},
        ))
        command = BotCommand.Build(options[0])
        self.assertIsNotNone(command)
        self.assertEqual(command.resources, [])


class TestCommandSerialisation(unittest.TestCase):

    def test_cancel_is_sent_as_an_empty_object(self):
        # Same bytes `Button.doPost` sends when nothing is selected.
        self.assertEqual(BotCommand.ToJson(BotCommand.Cancel()), '{}')

    def test_selection_matches_the_client_wire_shape(self):
        options = ParseOptions(MakeOptionJson(
            id=10,
            all_legal_targets=[7],
            target_num_range=[1, 1],
            target_payment={'0': {'cost': '1', 'rule': [], 'payment': [{'51': 'R'}]}},
        ))
        payload = json.loads(BotCommand.ToJson(BotCommand.Build(options[0])))
        self.assertEqual(payload, {'id': 10, 'targets': [7], 'resources': [51]})

    def test_forced_cancel_is_only_allowed_for_a_single_targetless_option(self):
        single = ParseOptions(MakeOptionJson())
        self.assertTrue(BotCommand.IsForcedCancelAllowed(single))

        needs_target = ParseOptions(MakeOptionJson(all_legal_targets=[7], target_num_range=[1, 1]))
        self.assertFalse(BotCommand.IsForcedCancelAllowed(needs_target))

        two = ParseOptions(MakeOptionJson(id=10), MakeOptionJson(id=11))
        self.assertFalse(BotCommand.IsForcedCancelAllowed(two))


class TestRepeatGuard(unittest.TestCase):

    def test_counts_recurrences_of_the_same_question(self):
        guard = RepeatGuard(window=8)
        decision = MakeDecision(ParseOptions(MakeOptionJson()))
        self.assertEqual(guard.Update(decision), 0)
        self.assertEqual(guard.Update(decision), 1)
        self.assertEqual(guard.Update(decision), 2)

    def test_counts_recurrences_that_alternate_with_another_question(self):
        # The "Ask" loop bounces between two players, so the repeat is a cycle.
        guard = RepeatGuard(window=8)
        first = MakeDecision(ParseOptions(MakeOptionJson(id=10)))
        second = MakeDecision(ParseOptions(MakeOptionJson(id=11)), event_name='WhenPlayerLikeInTurn')

        self.assertEqual(guard.Update(first), 0)
        guard.Update(second)
        self.assertEqual(guard.Update(first), 1)
        guard.Update(second)
        self.assertEqual(guard.Update(first), 2)

    def test_a_different_question_is_not_a_repeat(self):
        guard = RepeatGuard(window=8)
        self.assertEqual(guard.Update(MakeDecision(ParseOptions(MakeOptionJson(id=10)))), 0)
        self.assertEqual(guard.Update(MakeDecision(ParseOptions(MakeOptionJson(id=11)))), 0)

    def test_forgets_beyond_the_window(self):
        guard = RepeatGuard(window=2)
        target = MakeDecision(ParseOptions(MakeOptionJson(id=10)))
        other = MakeDecision(ParseOptions(MakeOptionJson(id=11)))

        guard.Update(target)
        guard.Update(other)
        guard.Update(other)
        self.assertEqual(guard.Update(target), 0)


class TestFirstLegalPolicy(unittest.TestCase):

    def test_answers_with_the_first_legal_option(self):
        policy = FirstLegalPolicy()
        options = ParseOptions(MakeOptionJson(id=10), MakeOptionJson(id=11))
        self.assertEqual(policy.Choose(MakeDecision(options)).id, '10')

    def test_skips_an_option_the_bot_cannot_build(self):
        policy = FirstLegalPolicy()
        options = ParseOptions(
            MakeOptionJson(id=10, all_legal_targets=[], target_num_range=[1, 1]),
            MakeOptionJson(id=11),
        )
        self.assertEqual(policy.Choose(MakeDecision(options)).id, '11')

    def test_moves_on_when_the_engine_rejected_the_last_answer(self):
        policy = FirstLegalPolicy()
        options = ParseOptions(MakeOptionJson(id=10), MakeOptionJson(id=11))
        self.assertEqual(policy.Choose(MakeDecision(options, attempt=1)).id, '11')

    def test_moves_on_when_the_same_question_comes_back(self):
        policy = FirstLegalPolicy()
        options = ParseOptions(MakeOptionJson(id=10), MakeOptionJson(id=11))

        self.assertEqual(policy.Choose(MakeDecision(options)).id, '10')
        self.assertEqual(policy.Choose(MakeDecision(options)).id, '11')
        # Nothing left to try, so decline and let the turn move on.
        self.assertTrue(BotCommand.IsCancel(policy.Choose(MakeDecision(options))))

    def test_does_not_skip_a_forced_option_that_recurs(self):
        # "End Turn" comes back every turn and is the only legal answer.
        policy = FirstLegalPolicy()
        options = ParseOptions(MakeOptionJson(id=10, name='End Phase'))

        for _ in range(5):
            command = policy.Choose(MakeDecision(options, can_cancel=False, event_name='End Turn'))
            self.assertEqual(command.id, '10')

    def test_moves_on_when_a_forced_question_with_a_way_out_recurs(self):
        # MARVEL-99. `PlayerAction.MayChooseOneAbility` appends an explicit
        # `Cancel` *ability* and then asks forced, so `can_cancel` is False while
        # two legal answers exist -- and one of them is the way out of
        # `SwapTheseCards`'s `while True`. Riding the first one cost 14 corpus
        # cases their entire 20,000-step budget.
        policy = FirstLegalPolicy()
        options = ParseOptions(
            MakeOptionJson(id=1, name='Select_2_cards_to_swap',
                           all_legal_targets=[66, 56], target_num_range=[2, 2]),
            MakeOptionJson(id=2, name='Cancel'),
        )
        decision = lambda: MakeDecision(options, can_cancel=False,
                                        event_name='WhenPlayerChooseAbility')

        self.assertEqual(policy.Choose(decision()).id, '1')
        self.assertEqual(policy.Choose(decision()).id, '2')

    def test_a_recurring_forced_question_never_runs_the_policy_out_of_answers(self):
        # A forced decision has no cancel to fall back on, so walking `index`
        # off the end would turn a stall into a `BotStuck` abort -- a new way to
        # lose a game that used to finish. The guard rides the last option
        # instead and `NoProgressGuard` is what catches it.
        policy = FirstLegalPolicy()
        options = ParseOptions(MakeOptionJson(id=1), MakeOptionJson(id=2))

        answers = [policy.Choose(MakeDecision(options, can_cancel=False)).id
                   for _ in range(40)]

        self.assertEqual(answers[:3], ['1', '2', '2'])
        self.assertNotIn('', answers)

    def test_a_rejected_answer_still_runs_the_policy_out(self):
        # `attempt` is not clamped: it counts answers the engine actually
        # refused, and running out of those is genuinely stuck.
        policy = FirstLegalPolicy()
        options = ParseOptions(MakeOptionJson(id=1), MakeOptionJson(id=2))

        with self.assertRaises(BotStuck):
            policy.Choose(MakeDecision(options, can_cancel=False, attempt=2))

    def test_raises_when_it_has_no_answer_and_cannot_decline(self):
        policy = FirstLegalPolicy()
        options = ParseOptions(
            MakeOptionJson(id=10, all_legal_targets=[], target_num_range=[1, 1]),
            MakeOptionJson(id=11, all_legal_targets=[], target_num_range=[1, 1]),
        )
        with self.assertRaises(BotStuck):
            policy.Choose(MakeDecision(options, can_cancel=False))

    def test_game_start_clears_the_guard(self):
        policy = FirstLegalPolicy()
        options = ParseOptions(MakeOptionJson(id=10), MakeOptionJson(id=11))

        self.assertEqual(policy.Choose(MakeDecision(options)).id, '10')
        policy.OnGameStart(1)
        self.assertEqual(policy.Choose(MakeDecision(options)).id, '10')


class TestSeededRandomPolicy(unittest.TestCase):

    def Play(self, seed):
        policy = SeededRandomPolicy(seed)
        policy.OnGameStart(1)
        options = ParseOptions(*[MakeOptionJson(id=x) for x in range(10, 20)])
        return [policy.Choose(MakeDecision(options)).id for _ in range(40)]

    def test_the_same_seed_gives_the_same_answers(self):
        self.assertEqual(self.Play(7), self.Play(7))

    def test_a_different_seed_gives_different_answers(self):
        self.assertNotEqual(self.Play(7), self.Play(8))

    def test_never_touches_the_engine_rng(self):
        from engine.lib import Random
        before = Random.counter
        self.Play(7)
        self.assertEqual(Random.counter, before)

    def test_answers_a_forced_decision_with_a_real_option(self):
        policy = SeededRandomPolicy(7)
        policy.OnGameStart(1)
        options = ParseOptions(MakeOptionJson(id=10), MakeOptionJson(id=11))
        command = policy.Choose(MakeDecision(options, can_cancel=False))
        self.assertIn(command.id, ('10', '11'))


if __name__ == '__main__':
    unittest.main()
