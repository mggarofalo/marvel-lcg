"""The order Change Form abilities are registered in is allocation-visible.

`PlayerSetup.register_change_form` gathers a player's identity faces into a
`set`, which iterates by memory address, and registers one ability per entry.
That decides the order `Effect` object ids are allocated, and effect ids are
written into saved scenes -- so two runs of the same seed could record different
ids for the same abilities.

`docs/determinism-audit.md` (F4) measured exactly that while landing MARVEL-38:
two of three bot scenes differed, and the entire diff was `e6`/`e7` swapping
between `01001a` and `01001b`.

See MARVEL-33. The subtlety these tests pin is that the audit's stated
remedy -- `sorted(identities)` -- is not sufficient on its own, because
`CardFace.__lt__` orders by *card*, and an identity's two faces share one card.
"""

import unittest

# `game.*` modules import each other circularly and only resolve once `engine`
# has been imported. That is the entire bootstrap needed here: no config, no
# card database, no world.
import engine  # noqa: F401  pylint: disable=unused-import

from game.player.element.player_setup import IdentityOrder


class FakeCard:
    """The one member of `Card` that the ordering key reads."""

    def __init__(self, object_id: int) -> None:
        self.object_id = object_id

    def __lt__(self, other: "FakeCard") -> bool:
        return self.object_id < other.object_id


class FakePaper:
    def __init__(self, card_id: str) -> None:
        self.card_id = card_id


class FakeFace:
    """An identity face: a printed card id, and the card both faces sit on.

    `__lt__` mirrors `CardFace.__lt__` exactly -- it delegates to the card -- so
    a test that sorts these reproduces what the engine's own comparison does.
    """

    def __init__(self, card_id: str, card: FakeCard) -> None:
        self.paper = FakePaper(card_id)
        self.card = card

    def __lt__(self, other: "FakeFace") -> bool:
        return self.card.__lt__(other.card)

    def __repr__(self) -> str:
        return self.paper.card_id


def Identity(card_id_prefix: str, object_id: int):
    """The `a` and `b` faces of one identity card, as a pair."""
    card = FakeCard(object_id)
    return FakeFace(card_id_prefix + "a", card), FakeFace(card_id_prefix + "b", card)


class TestIdentityOrder(unittest.TestCase):

    def test_faces_of_one_card_are_tied_under_the_default_comparison(self):
        # This is the reason `IdentityOrder` exists rather than a bare
        # `sorted()`. If this ever stops holding, the key can be simplified --
        # so assert it rather than leave it as a claim in a docstring.
        alter_ego, hero = Identity("01010", 1)
        self.assertFalse(alter_ego < hero)
        self.assertFalse(hero < alter_ego)

    def test_plain_sorted_preserves_arrival_order_of_tied_faces(self):
        # Python's sort is stable, so a tie keeps the input order -- and the
        # input order is the set's, which is memory address order.
        alter_ego, hero = Identity("01010", 1)
        self.assertEqual(sorted([hero, alter_ego]), [hero, alter_ego])
        self.assertEqual(sorted([alter_ego, hero]), [alter_ego, hero])

    def test_the_key_orders_tied_faces_by_printed_card_id(self):
        alter_ego, hero = Identity("01010", 1)
        self.assertEqual(
            sorted([hero, alter_ego], key=IdentityOrder), [alter_ego, hero]
        )

    def test_arrival_order_does_not_change_the_result(self):
        # The property that actually matters: every order the set could produce
        # collapses to one registration order, and so to one id allocation.
        carol_a, carol_b = Identity("01010", 1)
        jen_a, jen_b = Identity("01019", 48)
        expected = [carol_a, carol_b, jen_a, jen_b]

        for arrival in (
            [carol_b, carol_a, jen_b, jen_a],
            [jen_b, jen_a, carol_b, carol_a],
            [jen_a, carol_b, jen_b, carol_a],
        ):
            self.assertEqual(sorted(arrival, key=IdentityOrder), expected)

    def test_cards_are_ordered_by_allocation_before_printed_id(self):
        # Card object id leads the key, so a card allocated earlier sorts first
        # even when its printed id is higher. Face id only ever breaks a tie
        # within one card.
        late_a, _ = Identity("01019", 3)
        early_a, _ = Identity("01099", 1)
        self.assertEqual(
            sorted([late_a, early_a], key=IdentityOrder), [early_a, late_a]
        )

    def test_the_key_is_a_total_order_over_a_realistic_identity_set(self):
        # No two faces may share a key: a duplicate is a tie, and a tie is where
        # address order gets back in.
        faces = [*Identity("01010", 1), *Identity("01019", 48)]
        keys = [IdentityOrder(face) for face in faces]
        self.assertEqual(len(set(keys)), len(keys))


if __name__ == "__main__":
    unittest.main()
