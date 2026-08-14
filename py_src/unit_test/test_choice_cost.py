"""The seam between a cost and a spend that is itself the effect (MARVEL-109).

`ForChoiceAbilityWithCost` and `ForChoiceAbilityToSpend` build the same kind of
option and differ in one flag. That flag decides whether a forced choice nobody
can fulfil may resolve the option at a **reduced** cost:

    "either spend [energy][mental][physical] or exhaust each character"
        -- the spending *is* the effect, so it resolves as far as it goes

    "spend a [energy] -> deal 1 damage to each enemy"
        -- the spending is a *cost*, so it is paid in full or not taken

Kid Omega (49013) is the card that makes the difference matter: partially
paying and still dealing its damage would be flatly wrong.

**These are mechanism tests, not behavioural ones, and that is deliberate.**
The behavioural difference needs a forced choice whose every option is
unfulfillable *and* a cost-bearing option that is a genuine cost, and no core-set
board can build one -- every card that would is in a later pack. Setting the
default the wrong way round passes all thirteen Sonic Boom and Android
Efficiency scenarios, so without something here the distinction that justifies
24 card scripts moving to a new factory is pinned by nothing at all.
"""

import unittest

import engine  # noqa: F401  -- registers the card/ability machinery

from game.ability.factory.ability_factory import AbilityFactory
from game.card.face.attribute.has_cost import Cost


def Spend():
    return AbilityFactory.ForChoiceAbilityToSpend(Cost("YBR"))


def PayFor():
    return AbilityFactory.ForChoiceAbilityWithCost(Cost("Y"))


class TestWhichOptionsMayResolveAtAReducedCost(unittest.TestCase):

    def test_a_spend_that_is_the_effect_says_so(self):
        self.assertTrue(Spend().spend_is_the_effect)

    def test_a_cost_does_not(self):
        # The default, and the one that matters: a factory that opted *in* to
        # partial payment by accident would silently let every "spend X -> do Y"
        # card do Y after underpaying for it.
        self.assertFalse(PayFor().spend_is_the_effect)

    def test_the_two_are_otherwise_the_same_option(self):
        # The flag is the whole difference. If these drift apart, the reduced
        # -cost path stops being the only thing this distinction controls and
        # these tests stop covering it.
        spend, cost = Spend(), PayFor()
        self.assertEqual(spend.flags.is_choose_ability, cost.flags.is_choose_ability)
        self.assertTrue(spend.NeedCost())
        self.assertTrue(cost.NeedCost())

    def test_the_flag_is_refused_on_an_option_with_no_cost(self):
        # `SetSpendIsTheEffect` asserts it is on a cost-bearing choice ability.
        # An option with nothing to reduce cannot be partially resolved, and
        # marking one would make the retry pass re-offer an option identical to
        # the one that already failed -- a loop, not a partial payment.
        plain = AbilityFactory.ForChoiceAbility("Exhaust each character",
                                                lambda targets: None)
        with self.assertRaises(AssertionError):
            plain.SetSpendIsTheEffect()

    def test_only_a_spend_is_gathered_for_the_retry_pass(self):
        """The selection `ChooseAbilitiesHelper` makes, in the small.

        It re-offers `[x for x in abilities if x.spend_is_the_effect]`. A mixed
        choice -- one cost, one spend -- must reduce only the spend, or Kid
        Omega deals its damage having underpaid.
        """
        abilities = [PayFor(), Spend(), None]
        partial = [x for x in abilities if x != None and x.spend_is_the_effect]
        self.assertEqual(len(partial), 1)
        self.assertTrue(partial[0].spend_is_the_effect)


if __name__ == "__main__":
    unittest.main()
