"""`AbilityFactory.UnitCannotDefend` has to accept every `CardType` it is given.

"Cannot defend" is two restrictions, not one. The first stops the unit being
declared as a defender and is checked against a *card*. The second stops the
unit's controller reaching a defense-labeled ability -- `Backflip`, `Defensive
Stance` -- and `PlayersCannotTriggerAbility` checks that against a *player*. So
the factory has to project its `which_unit` argument from a `CardType` onto a
`PlayerType`, and it only knew how to do that for a `CardFinder`:

    elif which_unit:
        assert isinstance(which_unit, CardFinder)     # <-- MARVEL-96
        check_player = PlayerFinder(which_unit)

`CardType` is a much wider union than that -- the `CARD_TYPE_EX` strings
("AttachedIdentity", "AttachedAlly", "Character") and the face classes (`Ally`,
`Unit2`) are all legal, and six shipped card scripts pass one. The branch above
it special-cases `which_unit == "Attached"`, which is not a `CardType` spelling
at all, so the string case had never actually worked.

The assert fires while the ability list is *built*, inside `GetAbilities`, so it
does not need a game state to reach: any game holding one of those six cards
died at step 0 during `World.Initialize`. That is why one bug accounted for 8 of
12 failures in the corpus run that found it.

The fix makes the projection total, and the tests below pin that it is a
projection rather than a widened assert: `which_unit` naming a subset of a
player's characters must produce *no* player-level restriction, because that
player's identity can still defend.
"""

import ast
import importlib
import os
import unittest
from unittest import mock

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from game.ability.factory import AbilityFactory
from game.card.card_finder import CardFinder
from game.card.face.card_type import Ally
from game.card.face.base import Unit2
from game.player.player_finder import PlayerFinder


CARDS = os.path.join("cards", "pack")


def CardScriptsCalling(name: str):
    """Every `cards/pack` module whose source calls `AbilityFactory.<name>`.

    Read out of the source rather than hard-coded so a card added later is
    covered without anyone remembering to list it here.
    """
    found = []
    for folder, _, files in os.walk(CARDS):
        for file in files:
            if not file.endswith(".py"):
                continue
            path = os.path.join(folder, file)
            with open(path, encoding="utf-8") as handle:
                tree = ast.parse(handle.read(), path)
            for node in ast.walk(tree):
                if not isinstance(node, ast.Call):
                    continue
                if not isinstance(node.func, ast.Attribute):
                    continue
                if node.func.attr != name:
                    continue
                if not isinstance(node.func.value, ast.Name):
                    continue
                if node.func.value.id != "AbilityFactory":
                    continue
                found.append(os.path.splitext(path)[0].replace(os.sep, "."))
                break
    return sorted(found)


################################################################################
#

class TestTheCardsBuild(unittest.TestCase):
    """The regression itself: the ability list is built at import, not at play.

    Without the fix six of these raise `AssertionError`, and four of them are
    sitting in `crashes/` as separate signatures for what is one bug.
    """

    def test_the_scan_finds_the_cards_that_use_it(self):
        # Two-sided: a scan that silently found nothing would pass every other
        # test in this class.
        modules = CardScriptsCalling("UnitCannotDefend")
        self.assertGreaterEqual(len(modules), 9)
        self.assertIn("cards.pack.mut_gen.magneto.32150", modules)
        self.assertIn("cards.pack.tt.trickster_magic.55061", modules)

    def test_every_card_that_cannot_defend_builds_its_abilities(self):
        for module in CardScriptsCalling("UnitCannotDefend"):
            with self.subTest(module=module):
                abilities = importlib.import_module(module).GetAbilities()
                self.assertTrue(abilities)


################################################################################
#

class TestTheProjection(unittest.TestCase):
    """What `which_unit` becomes once it has to name a player.

    `PlayersCannotTriggerAbility` is recorded rather than run: what is under
    test is which player the restriction is aimed at, and that is decided when
    the ability is built.
    """

    def Restriction(self, which_unit, who_attacker=None,
                    cannot_trigger_defense_ability=True):
        """The `check_player` `UnitCannotDefend` aims its restriction at.

        Returns `(check_player, extra_condition_count)`. `None` means it built
        no player-level restriction at all.
        """
        calls = []

        def Record(which_player, which_ability, **kwargs):
            calls.append((which_player, kwargs))
            return mock.Mock()

        with mock.patch.object(AbilityFactory, "PlayersCannotTriggerAbility", Record):
            AbilityFactory.UnitCannotDefend(
                which_unit,
                who_attacker,
                cannot_trigger_defense_ability=cannot_trigger_defense_ability,
            )
        if not calls:
            return None, 0
        self.assertEqual(len(calls), 1)
        which_player, kwargs = calls[0]
        self.assertEqual(kwargs["label"], "defense")
        return which_player, len(kwargs["conditions"])

    def test_a_card_finder_still_becomes_a_player_finder(self):
        # The one spelling that worked before. It has to keep working
        # unchanged -- `50156` MACH-IV and `26002a` Intangible ride on it, as
        # does the villain's own "other characters cannot defend".
        finder = CardFinder(name="Vision")
        check_player, conditions = self.Restriction(finder)
        self.assertIsInstance(check_player, PlayerFinder)
        self.assertIs(check_player.is_unit, finder)
        self.assertEqual(conditions, 1)

    def test_an_attached_identity_becomes_the_attached_player(self):
        # `32150` Wrapped in Metal, `32055` Permanently Phased. The identity
        # cannot defend, so its player cannot reach a defense card either.
        check_player, _ = self.Restriction("AttachedIdentity")
        self.assertEqual(check_player, "AttachedPlayer")

    def test_that_is_the_mapping_the_sibling_factories_use(self):
        # `UnitCannotAttackTarget` and `UnitCannotThwartTarget` hard-code
        # "AttachedIdentity" -> "AttachedPlayer". Defending must not disagree:
        # `32150` calls all three with the same argument.
        for factory, keyword in (
                (AbilityFactory.UnitCannotAttackTarget, "cannot_trigger_attack_ability"),
                (AbilityFactory.UnitCannotThwartTarget, "cannot_trigger_thwart_ability")):
            with self.subTest(factory=factory.__name__):
                calls = []
                with mock.patch.object(AbilityFactory, "PlayersCannotTriggerAbility",
                                       lambda player, ability, **kwargs: calls.append(player)):
                    factory("AttachedIdentity", **{keyword: True})
                self.assertEqual(calls, ["AttachedPlayer"])

    def test_no_unit_at_all_restricts_every_player(self):
        check_player, _ = self.Restriction(None)
        self.assertEqual(check_player, "AnyPlayer")

    def test_a_subset_of_a_players_characters_restricts_no_player(self):
        """The reason this is a projection and not a widened assert.

        `55061` Puppet Master stops *allies* defending and `55062` Love Triangle
        stops one attached ally. Neither touches the identity, so neither may
        take the player's own defense cards away. The restriction is still
        emitted -- `PlayersCannotTriggerAbility` cannot express "nobody" -- but
        it carries the extra predicate that keeps it from matching anyone whose
        identity is not the unit named.
        """
        for which_unit in (Ally, "AttachedAlly", "AttachedCharacter"):
            with self.subTest(which_unit=which_unit):
                check_player, conditions = self.Restriction(which_unit)
                self.assertEqual(check_player, "AnyPlayer")
                # The attacker check, plus the identity projection. A bare
                # "AnyPlayer" with only the attacker check is the silencing fix
                # this test exists to reject.
                self.assertEqual(conditions, 2)

    def test_every_character_restricts_by_identity_not_by_fiat(self):
        # `27152` Tracking Display and `28030` Rollin', Rollin' -- every
        # character, which does include each identity. Still routed through the
        # projection so the attacker condition governs when it applies.
        for which_unit in (Unit2, "Character"):
            with self.subTest(which_unit=which_unit):
                check_player, conditions = self.Restriction(which_unit)
                self.assertEqual(check_player, "AnyPlayer")
                self.assertEqual(conditions, 2)

    def test_a_basic_defense_restriction_touches_no_ability(self):
        # `cannot_trigger_defense_ability=False` -- `50022` Grant Ward, and
        # every `UnitCannotMakeBasicDefense` caller. Nothing is projected at
        # all, which is why that card never crashed despite passing a string.
        check_player, _ = self.Restriction(
            "This", cannot_trigger_defense_ability=False)
        self.assertIsNone(check_player)
