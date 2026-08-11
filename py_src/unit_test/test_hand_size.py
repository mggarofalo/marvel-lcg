"""Hand size is enforced at the discard step, and only there.

`PlayerPhase.MayDiscardHandCardsAndDrawUpToMax` computes how many cards a player
is over their limit and passes that as the *minimum* to `AskDiscardFaces`, then
draws back up. That makes "the hand is at or under the limit" a post-condition
of this one operation.

It was a rule in `game/world/invariants.py` until MARVEL-76, checked at every
decision during `Phase.State.PlaceThreat`. Thor's printed "Have at thee!" --
draw 2 cards after a minion engages you -- fired it on a perfectly legal state:
any card that draws outside the end phase puts a hand over its limit until the
next end phase discards it down, so no decision point in a round satisfies the
bound. Moving it here is what makes it true.

These tests drive the operation with a fake player, because the thing under test
is the post-condition and not the discard prompt.
"""

import unittest
from unittest import mock

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from game.player.element.player_phase import PlayerPhase


class FakePlayer:
    """Answers the handful of things the discard step asks of a player.

    `held_after` is what the hand looks like once the discard and the draw have
    run -- the whole point is that the operation is checked on its result.
    """

    def __init__(self, hand_size, held_before, held_after, *, eliminated=False):
        self.player_id = 0
        self.hand_size = hand_size
        self.is_eliminated = eliminated
        self.counts = [held_before, held_after]
        self.discard_call = None
        self.drew = False
        self.hand_cards = mock.Mock()
        self.hand_cards.Get.return_value = []

    def GetCountHandSizeFaces(self):
        return [object()] * (self.counts.pop(0) if len(self.counts) > 1
                             else self.counts[0])

    def GetIdentity(self):
        return mock.Mock()

    def AskDiscardFaces(self, faces, amount, rule):
        self.discard_call = amount

    def DrawUp(self, amount, rule):
        self.drew = True


def Run(player):
    with mock.patch("game.effect.rule.GameRule", mock.Mock()):
        PlayerPhase(player).MayDiscardHandCardsAndDrawUpToMax("End Phase", mock.Mock())


class TestTheDiscardStepIsAsked(unittest.TestCase):

    def test_an_over_limit_hand_must_discard_the_excess(self):
        player = FakePlayer(hand_size=5, held_before=8, held_after=5)

        Run(player)

        # The minimum is the excess, not zero: this is what enforces the limit.
        self.assertEqual(player.discard_call, (3, "All"))
        self.assertTrue(player.drew)

    def test_a_hand_under_the_limit_discards_nothing_by_force(self):
        player = FakePlayer(hand_size=5, held_before=2, held_after=5)

        Run(player)

        self.assertEqual(player.discard_call, (0, "All"))


class TestThePostCondition(unittest.TestCase):

    def test_a_settled_hand_at_the_limit_passes(self):
        player = FakePlayer(hand_size=5, held_before=8, held_after=5)

        Run(player)  # does not raise

    def test_a_hand_under_the_limit_passes(self):
        # `DrawUp` cannot always reach the limit -- an empty deck stops it.
        player = FakePlayer(hand_size=5, held_before=8, held_after=3)

        Run(player)

    def test_a_discard_that_did_not_take_is_caught(self):
        # The failure the rule exists for: the operation ran and the hand is
        # still over. Previously this was only observable a phase later, from a
        # checker that also fired on legal states.
        player = FakePlayer(hand_size=5, held_before=8, held_after=7)

        with self.assertRaises(AssertionError) as caught:
            Run(player)

        message = str(caught.exception)
        self.assertIn("7 cards", message)
        self.assertIn("hand size of 5", message)
        self.assertIn("End Phase", message)

    def test_an_eliminated_player_is_not_held_to_it(self):
        player = FakePlayer(hand_size=5, held_before=9, held_after=9,
                            eliminated=True)

        Run(player)


if __name__ == "__main__":
    unittest.main()
