"""Targets are chosen before the cost is paid, and six cards are shaped by it.

MARVEL-133. `EffectChecker.CheckBeforeActive` calls `check_target()` and then
`check_pay()`, in that order, and `check_pay` is where
`effect.context.paid_this_resources` is first assigned. So **every card-level
hook a selector offers runs before the payment exists**:

  * a callable `range=` is evaluated while computing the range;
  * `check_again_fn(effect, targets)` is called from
    `Selector.AfterSelectTargets`, which `check_target()` drives.

Both read `paid_this_resources` as `Resources("0")` — its initial value from
`EffectContext.__init__` — and therefore `GetCostX()` as `0`, whatever the card
actually cost. Measured, not inferred: Wakanda Forever! costs 1, is genuinely
paid, and reports `paid=0` in all seven `check_again_fn` invocations its spec
file produces.

## Why this file exists rather than a comment

It is the constraint behind a card-script pattern that **looks like a bug and is
not**, and the pattern is spread over six cards nobody would connect. Each
prints a target count only knowable after the cost resolves, asks for
`range=(1, "All")`, and slices:

    cost = effect.GetCostX()
    targets = effect.targets[:cost]

MARVEL-133 was filed calling that a spelling mistake fixable per card. It is
not: over-select-and-slice is the only thing the ordering allows. The defects
are real -- the floor is 1 rather than X, so a mandatory effect can be
under-applied, and the ceiling is the board rather than X, so the selection
*order* silently decides who is dropped -- but the fix is architectural, and it
belongs to the Engine Core fold, where payment and targeting can be separate
inputs. `docs/engine_architecture.md` carries the design note.

**This test is what makes that note actionable.** If the ordering is ever
changed -- deliberately, or as a side effect of a refactor -- this fails, and
the failure is the signal to go back and give those six cards a real bound. A
docstring cannot notice that; the same reasoning as
`unit_test/test_may_choose_one.py` and MARVEL-113.

Deliberately a source-order assertion rather than a played game: the claim is
about the *pipeline*, and a scenario asserting "nothing was paid yet" would pass
just as happily on a card that costs nothing.
"""

import ast
import inspect
import textwrap
import unittest

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from game.effect.effect_checker import EffectChecker
from game.effect.effect_context import EffectContext


def CallOrder(function, names):
    """Line numbers of the first call to each of `names` inside `function`."""
    tree = ast.parse(textwrap.dedent(inspect.getsource(function)))

    found = {}
    for node in ast.walk(tree):
        if isinstance(node, ast.Call) and isinstance(node.func, ast.Name) \
                and node.func.id in names and node.func.id not in found:
            found[node.func.id] = node.lineno
    return found


class TestSelectionPrecedesPayment(unittest.TestCase):

    def test_check_target_is_called_before_check_pay(self):
        order = CallOrder(EffectChecker.CheckBeforeActive,
                          {"check_target", "check_pay"})

        self.assertIn("check_target", order,
                      "`CheckBeforeActive` no longer calls `check_target`; the "
                      "ordering this file pins has moved rather than gone")
        self.assertIn("check_pay", order,
                      "`CheckBeforeActive` no longer calls `check_pay`; see above")

        self.assertLess(
            order["check_target"], order["check_pay"],
            "payment now happens before target selection. That lifts the "
            "MARVEL-133 constraint: a selector can finally see what was paid, "
            "so the six cards that over-select and slice a target count "
            "(14006, 03006, 58018, 22010, 16006, 53017) can be given a real "
            "bound instead. See docs/engine_architecture.md and MARVEL-133.")

    def test_payment_is_assigned_in_the_later_of_the_two(self):
        # The order above only means something because `check_pay` is where the
        # payment first exists. If the assignment moves into `check_target`, the
        # constraint is gone and the ordering assertion would still pass.
        source = inspect.getsource(EffectChecker.CheckBeforeActive)
        before, _, after = source.partition("def check_pay(")

        self.assertNotIn("paid_this_resources =", before,
                         "`paid_this_resources` is now assigned before "
                         "`check_pay`, so a selector may be able to read it. "
                         "See MARVEL-133.")
        self.assertIn("paid_this_resources =", after,
                      "`check_pay` no longer assigns `paid_this_resources`; "
                      "this file can no longer tell when payment happens")

    def test_a_fresh_context_starts_out_reporting_nothing_paid(self):
        # The value a selector actually sees before `check_pay` runs. Both
        # halves matter: `GetCostX` is `paid_this_resources - paid_this_cost`,
        # so an empty pair is what makes X read as 0 during selection rather
        # than as "unknown", which is why the six cards cannot tell the
        # difference between "X is 0" and "X is not decided yet".
        #
        # Read from the source rather than by constructing one: `__init__`
        # reaches through `effect.this` for the initiator, so building a
        # context means building a game, and the initial values are the claim.
        source = inspect.getsource(EffectContext.__init__)
        self.assertIn('self.paid_this_resources: \'Resources\' = Resources("0")', source)
        self.assertIn('self.paid_this_cost: \'Cost\' = Cost("0")', source)


if __name__ == "__main__":
    unittest.main()
