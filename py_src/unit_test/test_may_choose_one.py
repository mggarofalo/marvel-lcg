"""`MayChooseOneAbility` returns None rather than indexing an empty list.

MARVEL-126. The method's own guard concedes the list can be empty --

    effects = self.ChooseAbilities(by_effect, *abilities, cancel_ability)
    if effects and effects[0].ability == cancel_ability:   # <-- "can be empty"
        return None
    return effects[0]                                      # <-- indexed anyway

-- and the declared return type is already `Effect|None`, so returning `None`
is the contract the callers were written against rather than a new one.

## Why this is a unit test and not a scenario

**No board reaches it**, and the search for one is the finding. `ChooseAbilities`
returns `[]` only when `ChooseAbilitiesHelper` returns `None`, which requires
`ChooseEffects` to get an empty list back from
`EventManager.FilterAvailableEffects`. Two things empty that wholesale:

  * the `isinstance(message, CanBeInstead) and message.is_be_instead` early
    return -- `Message.WhenPlayerChooseAbility`, which is the only message this
    path ever sends, is `(TriggerPlayerMessage, NoSendResolve)` and not
    `CanBeInstead`, so it cannot take that branch;
  * the `CACHE_BASED_FAST_UNDO` narrowing in `SimpleCheckEffects`, which needs
    an `undo_handle` -- and `ChooseEffects` passes `None`.

Everything else is per-effect `CheckCondition`, which would have to reject
`cancel_ability` too: a bare `ForChoiceAbility` with no condition, no cost and
no selector to fail on.

So the guard is currently unreachable, in the MARVEL-113 sense: said out loud
and watched, rather than implied. **The two tests below are the watch.** If
someone gives this path a `CanBeInstead` message or threads an undo handle into
`ChooseEffects`, the defect becomes live and `TestTheEmptyListIsStillUnreachable`
is what says so -- a docstring cannot.

The reason it was reported rather than fixed green during MARVEL-119 was exactly
this: the finder had no reproduction, and a fix with no failing guard breaks the
round's own rule. This file is the guard that was missing.
"""

import unittest

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from game.event.manager import EventManager
from game.message import Message
from game.message.sender.sender import CanBeInstead
from game.player.action.player_action import PlayerAction


class TestMayChooseOneAbilityOnAnEmptyChoice(unittest.TestCase):
    """The behaviour, driven directly.

    `MayChooseOneAbility` reads nothing off `self` but `ChooseAbilities`, so a
    stand-in that answers that one call exercises the real method body -- the
    same reasoning `test_select_rules.py` and `test_invariants.py` use. Calling
    it unbound is what keeps this a test of the shipped code rather than of a
    copy of it.
    """

    class FakeAction:
        def __init__(self, result):
            self.result = result
            self.seen = None

        def ChooseAbilities(self, by_effect, *abilities, **kwargs):
            # The abilities the real method built, including the Cancel it
            # appends -- captured so a test can assert it was still offered.
            self.seen = abilities
            return self.result

    def Call(self, result):
        action = self.FakeAction(result)
        return action, PlayerAction.MayChooseOneAbility(action, None)

    def test_an_empty_choice_returns_none_rather_than_raising(self):
        # Without the fix this is an IndexError, not a failed assertion.
        action, chosen = self.Call([])
        self.assertIsNone(chosen)
        self.assertIsNotNone(action.seen)

    def test_a_cancel_is_always_among_the_offered_abilities(self):
        # The premise of the analysis above: the list can only come back empty
        # if something rejected the Cancel too. If this stops being true, the
        # reasoning in the module docstring stops applying.
        action, _ = self.Call([])
        self.assertTrue(action.seen)
        self.assertIn("Cancel", action.seen[-1].GetName(False))

    def test_choosing_the_cancel_returns_none(self):
        # The pre-existing behaviour, pinned so the new `if effects else None`
        # cannot be mistaken for the only way this returns None.
        action = self.FakeAction([])

        class FakeEffect:
            def __init__(self, ability):
                self.ability = ability

        def choose(by_effect, *abilities, **kwargs):
            action.seen = abilities
            return [FakeEffect(abilities[-1])]   # the Cancel

        action.ChooseAbilities = choose
        self.assertIsNone(PlayerAction.MayChooseOneAbility(action, None))

    def test_a_real_choice_is_returned(self):
        # The other direction, so a fix that returned None unconditionally
        # would fail rather than look correct.
        action = self.FakeAction([])

        class FakeEffect:
            def __init__(self, ability):
                self.ability = ability

        chosen_effect = None

        def choose(by_effect, *abilities, **kwargs):
            nonlocal chosen_effect
            action.seen = abilities
            chosen_effect = FakeEffect(object())   # not the Cancel
            return [chosen_effect]

        action.ChooseAbilities = choose
        self.assertIs(PlayerAction.MayChooseOneAbility(action, None), chosen_effect)


class TestTheEmptyListIsStillUnreachable(unittest.TestCase):
    """The two facts that make the guard unreachable, pinned as facts.

    Each of these is a load-bearing step in the docstring's argument. They are
    asserted rather than written down because the argument is what decides
    whether this defect is dormant or live, and an argument nothing checks goes
    stale silently -- which is how MARVEL-126 came to be reported without a
    reproduction in the first place.

    **A failure here is not a regression in this file.** It means the analysis
    expired and `MayChooseOneAbility` now has a reachable empty path, so the
    guard above is doing real work and deserves a scenario.
    """

    def test_the_choose_ability_prompt_cannot_be_instead(self):
        # `FilterAvailableEffects` returns [] outright for a `CanBeInstead`
        # message that is being replaced -- Cancel included.
        self.assertFalse(issubclass(Message.WhenPlayerChooseAbility, CanBeInstead))

    def test_choose_effects_passes_no_undo_handle(self):
        # The other wholesale emptier: `SimpleCheckEffects` narrows `effects`
        # from `undo_handle.GetAvailableEffects`, and an empty cached list would
        # empty the lot. `ChooseEffects` hands it `None`.
        import inspect
        source = inspect.getsource(PlayerAction.ChooseEffects)
        self.assertIn("FilterAvailableEffects(message, effects, player, player.world, None)",
                      " ".join(source.split()))

    def test_the_wholesale_early_return_is_where_this_says_it_is(self):
        # Guards the first test against the branch being moved or renamed: if
        # `FilterAvailableEffects` stops keying on `is_be_instead`, the subclass
        # check above stops meaning anything.
        import inspect
        source = " ".join(inspect.getsource(EventManager.FilterAvailableEffects).split())
        self.assertIn("isinstance(message, CanBeInstead) and message.is_be_instead", source)


if __name__ == "__main__":
    unittest.main()
