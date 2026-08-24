"""The pieces MARVEL-163 added, on inputs small enough to state a rule about.

`python -m tools.events.verify` is the real proof and it needs the frozen
corpus, so it is a tool rather than a test -- the same split as
`tools/events/census.py`. What is here is the part of it that can be wrong
without the corpus noticing, which is `tools/events/state.py`: if the snapshot
is built wrong, every round trip in the corpus run is a round trip over the
wrong board.

The two rules being pinned are the two that were got wrong while writing it.

    python -m unittest unit_test.test_event_verify
"""

from __future__ import annotations

import unittest

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from tools.events import state
from tools.replay import observe


class FakeOwner:
    def __init__(self, player_id: int, is_scenario: bool = False) -> None:
        self.player_id = player_id
        self.is_scenario = is_scenario


SCENARIO = FakeOwner(-1, is_scenario=True)


class FakeArea:
    """Enough of `Deck2` for `Snapshot` to read.

    `play_area` is the deliberate part: `player.engaged_minions` is
    `Deck2(world.GetScenario(), ..., related_player=self)`, so the minions
    engaged with a player are owned by the scenario and sit in front of the
    player. An area owner read from `GetOwner()` alone answers -1 for every
    player's engagement area at once.
    """

    def __init__(self, object_id, zone, owner, *, play_area=None, bind_card=None):
        self.object_id = object_id
        self.deck_type = type("DeckType", (), {"name": zone})()
        self.cards = []
        self.removed_cards = []
        self.play_area = play_area
        self.bind_card = bind_card
        self._owner = owner

    def GetOwner(self):
        return self._owner


class FakeCard:
    def __init__(self, object_id, card_id, area, *, controller=None, on_field=True,
                 face_up=True, fields=None):
        self.object_id = object_id
        self.area = area
        self.state = type("State", (), {"is_face_up": face_up})()
        self._controller = controller
        self._on_field = on_field
        paper = type("Paper", (), {"card_id": card_id})()
        self.face = type("Face", (), {
            "paper": paper,
            "GetStateFields": lambda self, f=dict(fields or {}): dict(f),
        })()

    def IsOnField(self):
        return self._on_field

    def GetController(self):
        return self._controller

    def GetOwner(self):
        return self._controller or SCENARIO


class FakeWorld:
    def __init__(self, cards):
        self.object_manager = type("Manager", (), {
            "card_dict": {card.object_id: card for card in cards}})()


class TestTheAreaOwnerIsNotTheCardsController(unittest.TestCase):

    def testEngagedMinionsBelongToThePlayerTheyFace(self):
        """The area's owner comes from `play_area`, the card's from the digest."""
        player = FakeOwner(1)
        area = FakeArea(27, "EngagedEnemiesArea", SCENARIO, play_area=player)
        minion = FakeCard(5, "01097", area, controller=None, on_field=True)
        area.cards.append(minion)

        record = state.Snapshot(FakeWorld([minion]))[5]
        self.assertEqual(record["area"]["owner"], 1)
        # The card itself is the scenario's. Both are true, and they are
        # different questions about different objects.
        self.assertEqual(record["owner"], -1)

    def testAScenarioAreaWithAControlledCardInIt(self):
        """A side scheme controlled by a player, in the scenario's area."""
        area = FakeArea(18, "SideSchemesArea", SCENARIO)
        scheme = FakeCard(9, "01096", area, controller=FakeOwner(3))
        area.cards.append(scheme)

        record = state.Snapshot(FakeWorld([scheme]))[9]
        self.assertEqual(record["owner"], 3)
        self.assertEqual(record["area"]["owner"], -1)


class TestAnAreaIsIdentifiedNotDescribed(unittest.TestCase):

    def testTwoAreasSharingATripleKeepSeparateIdentities(self):
        """Two set-aside decks: same zone, same owner, same host, both from zero."""
        first = FakeArea(20, "AsideDeck", SCENARIO)
        second = FakeArea(35, "AsideDeck", SCENARIO)
        cards = [FakeCard(1, "01097", first, controller=None, on_field=False),
                 FakeCard(2, "01098", second, controller=None, on_field=False)]
        first.cards.append(cards[0])
        second.cards.append(cards[1])

        board = state.Snapshot(FakeWorld(cards))
        self.assertEqual(board[1]["index"], 0)
        self.assertEqual(board[2]["index"], 0)
        self.assertNotEqual(board[1]["area"]["id"], board[2]["area"]["id"])
        # And the triple genuinely does not tell them apart, which is the point.
        triple = lambda r: (r["area"]["zone"], r["area"]["owner"], r["area"]["host"])
        self.assertEqual(triple(board[1]), triple(board[2]))

    def testRemovedCardsAreTheirOwnOrderedList(self):
        """`removed_cards` indexes from zero independently of `cards`."""
        area = FakeArea(7, "UpgradesArea", FakeOwner(0), bind_card=FakeCard(
            99, "01001", FakeArea(1, "HeroArea", FakeOwner(0))))
        held = FakeCard(3, "01002", area, controller=FakeOwner(0))
        detached = FakeCard(4, "01003", area, controller=FakeOwner(0))
        area.cards.append(held)
        area.removed_cards.append(detached)

        board = state.Snapshot(FakeWorld([held, detached]))
        self.assertEqual(board[3]["index"], 0)
        self.assertEqual(board[4]["index"], 0)
        self.assertNotEqual(board[3]["area"]["id"], board[4]["area"]["id"])
        self.assertTrue(board[4]["zone"].endswith("/removed"))
        self.assertEqual(board[4]["host"], 99)


class TestASnapshotIsADigest(unittest.TestCase):

    def testTheAreaKeyIsNotOnTheWire(self):
        """`Serialize` must produce a v2 document, not a v2 document plus extras."""
        from game.world import digest

        area = FakeArea(22, "HandsArea", FakeOwner(0), play_area=FakeOwner(0))
        card = FakeCard(1, "01001", area, controller=FakeOwner(0), fields={"cost": 3})
        area.cards.append(card)

        board = state.Snapshot(FakeWorld([card]))
        self.assertIn("area", board[1])

        document = digest.Parse(state.Serialize(board))
        self.assertEqual(list(document["cards"][0]), list(digest.CARD_KEYS))


class TestTheLabelsAStepCarries(unittest.TestCase):
    """`trigger` and `verb` are the half a digest cannot show, and they are
    parsed out of strings the recording already holds."""

    def testTheTriggerDropsTheMessageId(self):
        self.assertEqual(observe._Trigger("m217 WhenPlayerChooseAbility"),
                         "WhenPlayerChooseAbility")

    def testATriggerWithNoIdSurvives(self):
        self.assertEqual(observe._Trigger("GameSetup"), "GameSetup")

    def testTheVerbIsTheWordBetweenTheIds(self):
        self.assertEqual(observe._Verb("e1 Choose c1 32001b"), "Choose")

    def testAnEffectWithNoVerbHasNone(self):
        self.assertEqual(observe._Verb("e1 c1 32001b"), "")

    def testADeclineAndADebugCommandNameNoVerb(self):
        # Neither is a thing the player did to a card, so inventing a verb for
        # either would put a word in an event that nothing produced.
        self.assertEqual(observe._Verb(""), "")
        self.assertEqual(observe._Verb(":give_card 01001"), "")
        self.assertEqual(observe._Verb("Puzzle.Setup"), "")


if __name__ == "__main__":
    unittest.main()
