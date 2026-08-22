"""What an option costs can depend on which target it is given (MARVEL-140).

`EffectChecker.UpdatePayResources` normally calculates **one** cost for an
option and lets it stand for every target. One printed card makes that wrong:

    * Iron Man (09039) -- "Reduce the cost to play each upgrade **on Iron Man**
      by 1."

So playing Telekinetic Shield costs 1 on Iron Man and 2 anywhere else, and with
a single resource in hand exactly one of the three friendly characters on the
board is a target the player can actually pay for.

Two things were wrong with how the engine handled that.

**It knew the card by its id.** `UpdatePayResources` tested
`if "09039" in [x.paper.card_id for x in all_legal_targets]`, so a second card
with the same shape would have been priced with one cost for every target and
would have been wrong in a way no test would notice. The card now *declares*
the dependency -- `Ability.SetCostDependsOnTarget`, spelled at the card as
`when_this_is_the_target=True` -- and the checker asks the legal targets.

**It offered targets nobody could pay for.** `CanPayAnyTarget` keeps an option
alive when *some* target is payable and says nothing about the rest, so the
player could pick Black Widow, be charged 2, and be refused -- the same
choose-then-refuse MARVEL-130 removed one level up, for whole actions.
`DropUnpayableTargets` removes them.

**Where it deliberately stops.** MARVEL-130 left the card-play menu alone: an
unaffordable card is still offered and still refuses. So when *no* target is
payable the list is left exactly as it was and the option stays on the menu --
emptying it here would redefine that boundary from inside the target selector,
which is a decision for whoever owns the boundary, not a side effect of this
one.

Constructed boards rather than self-play. `python -m tools.coverage.reach`
puts 09039 in no shipped deck the sampler draws from, and the combination that
matters -- this ally in play, an upgrade in hand, and exactly enough resource
to pay the reduced cost and not the full one -- is not something a corpus will
stumble into.

Related but untouched: MARVEL-133. Targets are chosen before payment exists, so
nothing here may read `paid_this_resources`. It does not: every cost above is
calculated by `UpdatePayResources` before any target is selected.
"""

import pathlib
import re
import unittest

# `engine` first: the `game.*` packages import each other in a cycle that only
# resolves once `engine/__init__.py` has walked it.
import engine  # noqa: F401

from game.effect.effect_checker import EffectChecker
from game.event.manager import EventManager
from game.message import Message
from game.puzzle.puzzle import RunPuzzle
from tools.spec.case import SpecCase, ThenStep
from tools.spec.harness import EnsureEngine, NewGameForCase
from tools.spec.policy import TranscriptPolicy

IRON_MAN = "09039"              # the ally whose text moves the cost
BLACK_WIDOW = "01075"           # a second ally, so there is something to drop
SHIELD = "34007"                # Telekinetic Shield: cost 2, attach to a friend
INSPIRED = "01074"              # one resource in hand, and nothing else

PY_SRC = pathlib.Path(__file__).resolve().parent.parent


def NewWorld():
    """A solo She-Hulk board against Rhino: villain, scheme, identity, nothing else.

    She-Hulk rather than Spider-Man on purpose. Peter Parker's "Scientist"
    generates a resource from the identity card itself, so an empty hand is
    still one resource and the none-payable case cannot be built at all.
    """
    EnsureEngine()
    case = SpecCase(
        name="target-dependent affordability",
        scenario="rhino",
        heroes=("she_hulk",),
        beats=(ThenStep("Rhino", "health", 14),),
    )
    game = NewGameForCase(case, TranscriptPolicy())
    assert game.GameSetup()
    return game.world


def Board(*hand, allies=(IRON_MAN, BLACK_WIDOW)):
    """Put `allies` into play and `hand` in hand, then offer the Shield.

    Returns `(available, effect)` -- what the player would be offered, and the
    play effect whose target list and costs the tests read.
    """
    world = NewWorld()
    puzzle = RunPuzzle(world)
    player = world.players[0]

    for ally in allies:
        puzzle.PutIntoPlay(ally)
    puzzle.CreateHandCards(*hand)

    shield = puzzle.FindFaceByName("Telekinetic Shield")
    assert shield, "the puzzle did not build the upgrade this file is about"
    effects = shield.GetTurnPlayEffects()
    assert len(effects) == 1, f"{effects=}"

    message = Message.WhenPlayerInTurn(player, -1)
    available = EventManager.FilterAvailableEffects(
        message, effects, player, world, None)
    return available, effects[0]


def Named(effect, card_id):
    for face in effect.checker.cost_for_different_target.target_cost:
        if face != None and face.paper.card_id == card_id:
            return face
    raise AssertionError(f"{card_id} was never priced: "
                         f"{effect.checker.cost_for_different_target.target_cost=}")


class TestTheDependencyIsDeclaredRatherThanRecognised(unittest.TestCase):

    def test_iron_man_says_his_cost_reduction_depends_on_the_target(self):
        _, effect = Board(SHIELD)
        iron_man = Named(effect, IRON_MAN)

        self.assertTrue(EffectChecker.CostDependsOnTarget(iron_man))

    def test_an_ordinary_ally_does_not(self):
        # The other half of the claim: the declaration distinguishes cards
        # rather than being set on everything.
        _, effect = Board(SHIELD)
        widow = Named(effect, BLACK_WIDOW)

        self.assertFalse(EffectChecker.CostDependsOnTarget(widow))

    def test_the_checker_names_no_card(self):
        # The test that would have failed before this change. A card id in the
        # checker is a rule that reaches exactly one printed card, and the next
        # card with the same shape gets one cost for every target in silence.
        source = (PY_SRC / "game" / "effect" / "effect_checker.py").read_text(encoding="utf-8")
        code = "\n".join(line.split("#")[0] for line in source.splitlines())

        self.assertEqual([], re.findall(r'"\d{5}[a-z]?"', code),
                         "`effect_checker.py` decides something by card id "
                         "again. Declare it on the ability instead -- see "
                         "`Ability.SetCostDependsOnTarget` and MARVEL-140.")

    def test_a_card_reading_the_targets_of_a_cost_declares_it(self):
        # `message.for_targets` is how a cost-calculation condition asks which
        # target the effect has, and a card that asks is target-dependent
        # whether or not it said so. The declaration and the condition are
        # spelled together by the factory so that they cannot drift; this
        # catches a card that spells the condition by hand instead.
        offenders = []
        for path in sorted((PY_SRC / "cards").rglob("*.py")):
            text = path.read_text(encoding="utf-8")
            if "for_targets" in text and "when_this_is_the_target" not in text:
                offenders.append(str(path.relative_to(PY_SRC)))

        self.assertEqual([], offenders,
                         "these card scripts read the targets of a cost "
                         "calculation without declaring the dependency, so "
                         "`EffectChecker` will price one cost for every "
                         "target. Ask for it with "
                         "`when_this_is_the_target=True` (MARVEL-140)")


class TestPricingEveryTargetSeparately(unittest.TestCase):

    def test_a_declaring_target_makes_the_option_priced_per_target(self):
        _, effect = Board(SHIELD)
        costs = effect.checker.cost_for_different_target

        self.assertTrue(costs.IsPerTarget())
        self.assertEqual(
            1, costs.GetCost(Named(effect, IRON_MAN)).rbyga.a,
            "Iron Man's reduction did not reach the cost of playing an "
            "upgrade on him")
        self.assertEqual(
            2, costs.GetCost(Named(effect, BLACK_WIDOW)).rbyga.a,
            "the reduction reached a target it does not apply to")

    def test_a_board_without_one_is_priced_once(self):
        # The ordinary path, and the reason this is not simply always-on: with
        # nothing on the board that moves a cost by target there is one cost,
        # calculated once, and every target shares it.
        _, effect = Board(SHIELD, allies=(BLACK_WIDOW,))
        costs = effect.checker.cost_for_different_target

        self.assertFalse(costs.IsPerTarget())
        self.assertEqual([None], list(costs.target_cost))


class TestWhichTargetsAPlayerIsOffered(unittest.TestCase):

    def test_a_target_that_cannot_be_paid_for_is_not_offered(self):
        """One resource in hand: Iron Man costs 1, everyone else costs 2.

        The case MARVEL-140 is about. Before the fix all three friendly
        characters were offered, and picking either of the other two reached
        `check_pay`, failed it, and refused a choice the player had been
        invited to make.
        """
        available, effect = Board(SHIELD, INSPIRED)

        self.assertEqual(1, len(available),
                         "the option itself should still be offered -- one of "
                         "its targets is payable")
        offered = [x.paper.card_id for x in effect.context.all_legal_targets]
        self.assertEqual([IRON_MAN], offered)
        self.assertEqual((1, 1), effect.context.target_range,
                         "the range must be re-derived from the targets that "
                         "survived, not kept from the unfiltered list")

    def test_no_payable_target_leaves_the_list_alone(self):
        """Nothing in hand but the upgrade: no target is payable.

        Filtering here would empty the list and take a card off the play menu,
        which is the surface MARVEL-130 deliberately did not redefine. The
        option stays, with every target on it, and refuses when taken -- the
        behaviour an unaffordable card has always had.
        """
        available, effect = Board(SHIELD)
        costs = effect.checker.cost_for_different_target

        self.assertFalse(costs.CanPayAnyTarget())
        self.assertEqual(1, len(available))
        offered = sorted(x.paper.card_id for x in effect.context.all_legal_targets)
        self.assertEqual(sorted(["01019b", IRON_MAN, BLACK_WIDOW]), offered,
                         "the identity and both allies are still legal targets")

    def test_every_target_payable_changes_nothing(self):
        # Two resources covers the unreduced cost, so nothing is dropped. The
        # filter must be inert whenever the player can pay for any of them.
        available, effect = Board(SHIELD, INSPIRED, INSPIRED)
        costs = effect.checker.cost_for_different_target

        self.assertEqual(1, len(available))
        self.assertTrue(all(costs.CanPay(x) for x in effect.context.all_legal_targets))
        self.assertEqual(3, len(effect.context.all_legal_targets))


class TestTheFilterRefusesToActOnAnUncomparableOption(unittest.TestCase):
    """`DropUnpayableTargets` returns False rather than guessing.

    Driven directly: each of these is a state `CheckCondition` can be in, and
    building a board for every one of them would take three more games to say
    what the guards already say plainly.
    """

    class FakeCosts:
        def __init__(self, per_target, payable):
            self.per_target = per_target
            self.payable = payable

        def IsPerTarget(self):
            return self.per_target

        def CanPay(self, face):
            return self.payable.get(face, True)

    class FakeChecker(EffectChecker):
        def __init__(self, costs, targets, selectors):
            class Context:
                all_legal_targets = list(targets)
                target_range = (1, 1)

            class Ability:
                pass

            self.cost_for_different_target = costs
            self.ability = Ability()
            self.ability.selectors = selectors
            self.effect = type("Effect", (), {"context": Context()})()

    def Checker(self, per_target, payable, targets, selectors=(None,)):
        costs = self.FakeCosts(per_target, payable)
        return self.FakeChecker(costs, targets, list(selectors))

    def test_one_cost_for_every_target_is_not_filtered(self):
        checker = self.Checker(False, {"a": False}, ["a", "b"])
        self.assertFalse(checker.DropUnpayableTargets())
        self.assertEqual(["a", "b"], checker.effect.context.all_legal_targets)

    def test_no_payable_target_is_not_filtered(self):
        checker = self.Checker(True, {"a": False, "b": False}, ["a", "b"])
        self.assertFalse(checker.DropUnpayableTargets())
        self.assertEqual(["a", "b"], checker.effect.context.all_legal_targets)

    def test_an_ability_with_no_selector_is_not_filtered(self):
        # `all_legal_targets` belongs to `selectors[0]`; with no selector there
        # is nothing to re-derive the range from.
        checker = self.Checker(True, {"a": False}, ["a", "b"], selectors=())
        self.assertFalse(checker.DropUnpayableTargets())
        self.assertEqual(["a", "b"], checker.effect.context.all_legal_targets)


if __name__ == "__main__":
    unittest.main()
