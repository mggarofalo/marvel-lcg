"""The order `HasTeamUp.GetTeamUpUnits` returns is part of the replay format.

`game/selector/selector_target_helper.py:get_TeamUp` feeds the result into a
select effect whose `targets` are written verbatim into the recorded command in
`engine/controller/controller.py`. The list used to be built from a `set` of
`CardFace`, and `CardFace` defines no `__hash__`, so the set iterated in memory
address order -- two identical games could record permuted target lists.

See MARVEL-30 and `docs/determinism-audit.md` (F3).
"""

import unittest
from unittest import mock

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported. That is the whole bootstrap this test
# needs: no config, no card database, no world.
import engine  # noqa: F401  pylint: disable=unused-import

from game.card.face.attribute.has_teamup import HasTeamUp
from game.operate.worlds import Worlds


class FakeCard:
    """The one member of `Card` that `GetTeamUpUnits` reads."""

    def __init__(self, object_id: int) -> None:
        self.object_id = object_id
        self.game_area = f"area-{object_id}"


class FakeUnit:
    """The three members of an on-field character that `GetTeamUpUnits` reads."""

    def __init__(self, name: str, object_id: int, sub_name: str = "") -> None:
        self.name = name
        self.sub_name = sub_name
        self.card = FakeCard(object_id)

    def IsName(self, name: str) -> bool:
        return self.name == name

    def IsSubName(self, name: str) -> bool:
        return bool(self.sub_name) and self.sub_name == name

    def __repr__(self) -> str:
        return f"{self.name}#{self.card.object_id}"


class FakeTeamUpCard:
    """Stands in for the `HasTeamUp` face the unbound method is called on.

    `HasTeamUp.__init__` wants a `Paper` and the attribute-registration
    machinery. The method under test only reads `team_up` and `card.game_area`,
    so a stub keeps the test to the ordering contract.
    """

    def __init__(self, team_up):
        self.team_up = team_up
        self.card = FakeCard(999)


def GetTeamUpUnits(team_up, on_field):
    """Call the method under test against a fixed on-field character list."""
    with mock.patch.object(
        Worlds,
        "GetOnFieldFriendlyCharacters",
        staticmethod(lambda game_area_effect, finder=None: list(on_field)),
    ):
        return HasTeamUp.GetTeamUpUnits(FakeTeamUpCard(team_up))


class TestGetTeamUpUnitsOrdering(unittest.TestCase):

    def test_result_is_ordered_by_object_id(self):
        # Spider-Man allocated after Ms. Marvel, and named second: neither the
        # field order nor the team-up order decides the result.
        ms_marvel = FakeUnit("Ms. Marvel", 12)
        spider_man = FakeUnit("Spider-Man", 40)
        result = GetTeamUpUnits(
            [["Ms. Marvel"], ["Spider-Man"]],
            [spider_man, ms_marvel],
        )
        self.assertEqual(result, [ms_marvel, spider_man])

    def test_field_order_does_not_change_the_result(self):
        # The set the method builds iterates in allocation order, which the
        # engine does not control. Every arrival order must collapse to one
        # recorded list.
        black_panther = FakeUnit("Black Panther", 7)
        shuri = FakeUnit("Shuri", 3)
        team_up = [["Black Panther"], ["Shuri"]]

        forward = GetTeamUpUnits(team_up, [black_panther, shuri])
        reverse = GetTeamUpUnits(team_up, [shuri, black_panther])

        self.assertEqual(forward, [shuri, black_panther])
        self.assertEqual(forward, reverse)

    def test_matches_by_sub_name(self):
        # A card names a character; the character in play may carry it as a
        # sub-name (an alter-ego / hero form pair).
        hero = FakeUnit("Carol Danvers", 5, sub_name="Captain Marvel")
        spider_woman = FakeUnit("Spider-Woman", 2)
        result = GetTeamUpUnits(
            [["Captain Marvel"], ["Spider-Woman"]],
            [hero, spider_woman],
        )
        self.assertEqual(result, [spider_woman, hero])

    def test_missing_named_character_returns_nothing(self):
        # Team-up cannot be played unless both named characters are in play.
        solo = FakeUnit("Ms. Marvel", 12)
        result = GetTeamUpUnits([["Ms. Marvel"], ["Spider-Man"]], [solo])
        self.assertEqual(result, [])

    def test_one_character_satisfying_both_names_appears_once(self):
        # The old `set` deduplicated; sorting must not turn that into a
        # duplicate target, which would change what the selector is offered.
        both = FakeUnit("Ms. Marvel", 12, sub_name="Kamala Khan")
        result = GetTeamUpUnits([["Ms. Marvel"], ["Kamala Khan"]], [both])
        self.assertEqual(result, [both])

    def test_alternative_names_are_tried_in_printed_order(self):
        # A slot can name several acceptable characters ("A/B"). The first one
        # found in the slot wins; the result is still ordered by object id.
        vision = FakeUnit("Vision", 20)
        wanda = FakeUnit("Scarlet Witch", 4)
        result = GetTeamUpUnits(
            [["Vision"], ["Scarlet Witch", "Quicksilver"]],
            [vision, wanda],
        )
        self.assertEqual(result, [wanda, vision])


if __name__ == "__main__":
    unittest.main()
