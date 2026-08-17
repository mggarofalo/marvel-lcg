"""The engine marks both shapes of a ranged non-resource cost for the bot."""

import unittest
from types import SimpleNamespace
from unittest.mock import patch

import engine  # noqa: F401  -- registers the game types

from game.ability.cost_func import CostFunc


class _Selector:
    def __init__(self):
        self.selector_end = SimpleNamespace(not_move=False)
        self.selector_rule = SimpleNamespace(random=False)

    def GetAllLegalTargets(self, effect, just_check=False):
        return [object(), object()]

    def GetTargetRange(self, effect, targets):
        return (0, 2)

    def AfterSelectTargets(self, effect, targets, target_range):
        return True


class _Player:
    def __init__(self):
        self.pay_size_is_effect = None

    def AskChooseFaces(self, targets, target_range, effect, **kwargs):
        self.pay_size_is_effect = kwargs.get("pay_size_is_effect")
        return targets


class _CounterFace:
    def GetCounters(self, name):
        return 3

    def CastTo(self, card_type):
        return self

    def RemoveCountersInternal(self, size, name, effect, forced=False):
        return size


class _CounterPlayer:
    def __init__(self):
        self.pay_size_is_effect = None

    def DeclareNumber(self, minimum, maximum, **kwargs):
        self.pay_size_is_effect = kwargs.get("pay_size_is_effect")
        return maximum


class TestVariableCardCost(unittest.TestCase):

    def test_ranged_card_selection_is_marked_as_the_cost_effect(self):
        player = _Player()
        cost = CostFunc.Base(_Selector(), None)
        effect = SimpleNamespace(
            ability=SimpleNamespace(flags=SimpleNamespace(is_check_pay=False)),
        )

        with patch("game.player.Player", _Player):
            self.assertTrue(cost.PayCost(effect, player))

        self.assertTrue(player.pay_size_is_effect)

    def test_ranged_counter_number_is_marked_as_the_cost_effect(self):
        player = _CounterPlayer()
        cost = CostFunc.Counter("This", (1, 3), "charge")
        effect = SimpleNamespace()

        self.assertTrue(cost.call_fn([_CounterFace()], effect, player))
        self.assertTrue(player.pay_size_is_effect)


if __name__ == "__main__":
    unittest.main()
