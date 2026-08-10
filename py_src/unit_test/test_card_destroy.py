"""Destroying a card takes it out of `card_dict`, and so out of the digest.

`Card.Destroy` removed the card from its area and unregistered its effects, but
left it in `object_manager.card_dict` with `self.area` still pointing at the area
it had just been taken out of. The digest walks `card_dict` and reports each card
from its `area`, so a destroyed card went on being described in a zone it was no
longer in.

Nothing calls it -- `Deck2.Destroy` is the only caller of `Card.Destroy`, and
nothing calls `Deck2.Destroy` -- so this never fired. It was worth settling
anyway: "destroyed" modelled as "still present with a stale pointer" is
something the C# port would either reproduce faithfully, inheriting the bug, or
quietly fix, diverging on the digest.

See MARVEL-50 and `docs/state-digest-v2.md` finding D10.
"""

import unittest

import engine  # noqa: F401  resolves the game.* import cycle

from game.world import digest
from tools.determinism.headless import run_headless


def PlayedWorld():
    run_headless("rhino", ["spider_man"], 12345, max_steps=6)
    from engine import Engine
    world = Engine.game.world
    assert world is not None
    return world


class TestDestroyRemovesFromCardDict(unittest.TestCase):

    def setUp(self):
        self.world = PlayedWorld()
        self.manager = self.world.object_manager

    def ADestroyableCard(self):
        """A card sitting in an ordinary pile, so removing it disturbs nothing."""
        for card in self.manager.card_dict.values():
            if card.area.deck_type.name == "PlayerDeck":
                return card
        raise AssertionError("no card in the player deck")

    def test_a_destroyed_card_leaves_card_dict(self):
        card = self.ADestroyableCard()
        object_id = card.object_id
        self.assertIn(object_id, self.manager.card_dict)

        card.Destroy()

        self.assertNotIn(object_id, self.manager.card_dict)

    def test_a_destroyed_card_leaves_the_digest(self):
        # The reason the first test matters: the digest walks `card_dict`.
        card = self.ADestroyableCard()
        object_id = card.object_id
        before = {record["id"] for record in digest.BuildDocument(self.world)["cards"]}
        self.assertIn(object_id, before)

        card.Destroy()

        after = {record["id"] for record in digest.BuildDocument(self.world)["cards"]}
        self.assertNotIn(object_id, after)
        self.assertEqual(before - after, {object_id}, "only that card should go")

    def test_the_id_is_not_reused(self):
        # Ids are allocated from a counter that only ever increments, which is
        # part of the object-id contract a port reproduces. Destroying a card
        # must not hand its number to the next one.
        card = self.ADestroyableCard()
        destroyed_id = card.object_id
        card.Destroy()

        next_id = self.manager.AddObject("card", object())

        self.assertNotEqual(next_id, destroyed_id)
        self.assertGreater(next_id, destroyed_id)

    def test_destroying_twice_is_refused(self):
        # A double destroy means a caller has lost track of the world, which is
        # worth surfacing rather than absorbing as a no-op.
        card = self.ADestroyableCard()
        card.Destroy()

        with self.assertRaises(AssertionError):
            self.manager.RemoveCard(card.object_id)

    def test_every_other_card_is_untouched(self):
        card = self.ADestroyableCard()
        survivors = {
            object_id
            for object_id in self.manager.card_dict
            if object_id != card.object_id
        }
        card.Destroy()
        self.assertEqual(set(self.manager.card_dict), survivors)


class TestDeckDestroy(unittest.TestCase):
    """`Deck2.Destroy` walked the list it was removing from."""

    def setUp(self):
        self.world = PlayedWorld()

    def test_it_destroys_every_card_not_every_second_one(self):
        # `Card.Destroy` calls `area.Remove(self)`, which mutates `deck.cards`.
        # Iterating that list directly skipped alternate entries, and the
        # `self.cards = []` afterwards hid it -- the deck was empty either way,
        # but the skipped cards kept their effects and their `card_dict` entry.
        deck = None
        for card in self.world.object_manager.card_dict.values():
            if card.area.deck_type.name == "PlayerDeck" and len(card.area.cards) >= 4:
                deck = card.area
                break
        self.assertIsNotNone(deck, "no pile big enough to show the skip")

        expected = {card.object_id for card in deck.cards}
        deck.Destroy()

        self.assertEqual(deck.cards, [])
        still_present = expected & set(self.world.object_manager.card_dict)
        self.assertEqual(still_present, set(), "some cards were skipped")


if __name__ == "__main__":
    unittest.main()
