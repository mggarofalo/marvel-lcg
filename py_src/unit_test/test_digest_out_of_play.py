"""Every `GetInfoDict` definition is total, so the digest can read any zone.

The v2 digest populated `fields` only for cards in play, in a status area or in
a boost area. That boundary was inherited from v1 rather than chosen: several
overrides *looked* unsafe out of play, and an oracle that can raise while
computing itself is worse than one with a documented edge.

MARVEL-59 audited the nine definitions and removed the boundary. These tests are
the audit, kept executable -- the interesting one is `Identity`, whose
`GetControlByPlayer` call was the stated reason the guard existed.

Boots the engine, so it belongs in the slower tier and runs from `py_src/`.
"""

import unittest

import engine  # noqa: F401  resolves the game.* import cycle

from game.world import digest
from tools.determinism.headless import run_headless


def PlayedWorld(campaign, heroes, seed, steps=60):
    run_headless(campaign, list(heroes), seed, max_steps=steps)
    from engine import Engine
    world = Engine.game.world
    assert world is not None
    return world


class TestOutOfPlayFieldsAreSafe(unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.world = PlayedWorld("klaw", ("captain_marvel", "she_hulk"), 999)

    def OutOfPlayCards(self):
        for card in self.world.object_manager.card_dict.values():
            flags = card.area.flags
            if not (flags.is_in_play or flags.is_status_area or flags.is_boost_area):
                yield card

    def test_no_card_out_of_play_raises(self):
        # The property the widened guard rests on. A raise here is not a failed
        # assertion about game state -- it is the oracle crashing while
        # computing itself, which is what the old boundary was protecting
        # against.
        count = 0
        for card in self.OutOfPlayCards():
            card.face.GetStateFields()
            count += 1
        self.assertGreater(count, 50, "too few out-of-play cards to prove anything")

    def test_the_sweep_covers_several_distinct_zones(self):
        # One zone passing says little; the overrides differ in what they read.
        zones = {card.area.deck_type.name for card in self.OutOfPlayCards()}
        self.assertGreaterEqual(len(zones), 4, sorted(zones))

    def test_an_identity_out_of_play_reports_its_limits(self):
        """`Identity.GetInfoDict` was the override the guard was written for.

        It reads `GetControlByPlayer`, which asserts `isinstance(owner, Player)`.
        The assertion holds out of play: the method consults the controller only
        `if self.IsInPlay()` and otherwise falls back to `GetOwner()`, and an
        identity card is always owned by a player.
        """
        from game.card.face.card_type import AlterEgo, Hero

        identity = None
        for card in self.world.object_manager.card_dict.values():
            if isinstance(card.face, (Hero, AlterEgo)):
                identity = card
                break
        self.assertIsNotNone(identity, "no identity on the board")
        self.assertTrue(identity.face.IsInPlay(), "identity should start in play")

        origin = identity.area
        target = next(
            card.area
            for card in self.world.object_manager.card_dict.values()
            if card.area.deck_type.name == "PlayerDeck"
        )
        origin.Remove(identity)
        target.cards.append(identity)
        identity.area = target
        try:
            self.assertFalse(identity.face.IsInPlay())
            fields = identity.face.GetStateFields()
            # The keys that come from the player, i.e. the ones the guard was
            # protecting. Present and sane rather than merely not raising.
            self.assertIn("ally_limit", fields)
            self.assertIn("restricted_limit", fields)
            self.assertGreater(fields["ally_limit"], 0)
        finally:
            target.cards.remove(identity)
            origin.cards.append(identity)
            identity.area = origin

    def test_a_minion_reports_engaged_with_zero_out_of_play(self):
        # `Minion.GetInfoDict` already guarded itself, so its key set does not
        # change with the zone -- which is what keeps the registered key set part
        # of the contract rather than a function of where a card sits.
        from game.card.face.card_type import Minion

        minions = [
            card
            for card in self.OutOfPlayCards()
            if isinstance(card.face, Minion)
        ]
        if not minions:
            self.skipTest("no minion out of play on this board")
        for card in minions:
            fields = card.face.GetStateFields()
            self.assertEqual(fields.get("engaged_with"), 0, card.face.paper.card_id)


class TestDigestReadsEveryZone(unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.world = PlayedWorld("rhino", ("spider_man",), 12345, steps=6)

    def test_cards_outside_play_now_carry_fields(self):
        document = digest.BuildDocument(self.world)
        by_id = {record["id"]: record for record in document["cards"]}

        in_play_ids = {
            card.object_id
            for card in self.world.object_manager.card_dict.values()
            if card.area.flags.is_in_play
            or card.area.flags.is_status_area
            or card.area.flags.is_boost_area
        }
        outside = [by_id[i] for i in by_id if i not in in_play_ids]
        self.assertTrue(outside, "no cards outside play to check")

        with_fields = [record for record in outside if record["fields"]]
        self.assertTrue(
            with_fields,
            "widening the guard changed nothing, so it is not actually wider",
        )

    def test_the_document_still_describes_every_card(self):
        document = digest.BuildDocument(self.world)
        self.assertEqual(
            len(document["cards"]), len(self.world.object_manager.card_dict)
        )


if __name__ == "__main__":
    unittest.main()
