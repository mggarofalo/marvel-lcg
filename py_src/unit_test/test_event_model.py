"""The event vocabulary explains a state change, and position is not an event.

`tools/events/model.py` proposes the set of events the fold returns (MARVEL-160)
and a reducer that applies them. The claim being tested is:

    Apply(before, Derive(before, after)) == after

Measured over the frozen corpus -- 27,895 steps drawn from all 58 shards -- that
holds for **100% of steps** on everything the digest records except position,
with no residue. The corpus run is a tool invocation rather than a unit test,
because it needs the corpus; what is here is the same property on boards small
enough to state a rule about, plus the two rules that shaped the design.

## The two rules

**Position is a consequence, not an event.** Taking a card out of the middle of a
deck shifts every card above it down by one, and a digest records that as a
position change for each of them -- 20% of all observed change. Emitting those
would make an animation play a deck rippling every time a card is drawn. So
`Derive` models the compaction and emits `AreaReordered` only for what is left,
which is a real shuffle. Doing that removed 85% of apparent reorderings.

**An area is not a zone name.** `HandsArea` names one area per player. Worse, the
digest's `owner` is the *card's controller*, not the area's owner, so grouping
records by `(zone, owner, host)` splits one area in two whenever a controlled
card sits in a scenario area -- which happens with side schemes. This is why the
corpus round trip is 100% on state and only 61% on position, and why `AreaRef`
carries an owner the engine supplies rather than one recovered from a digest.

    python -m unittest unit_test.test_event_model
"""

from __future__ import annotations

import unittest

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from tools.events import model


def Card(object_id, zone, index, *, card="01001", owner=0, host=-1,
         face_up=True, **fields):
    return {
        "id": object_id, "card": card, "zone": zone, "owner": owner,
        "index": index, "host": host, "face_up": face_up, "fields": dict(fields),
    }


def Board(*cards):
    return {record["id"]: record for record in cards}


def Kinds(events):
    return [event["kind"] for event in events]


class TestTheVocabularyIsClosed(unittest.TestCase):

    def testEveryKindDeriveEmitsIsDeclared(self):
        """`VOCABULARY` is a contract, so it must not drift from `Derive`."""
        before = Board(
            Card(1, "PlayerDeck", 0), Card(2, "PlayerDeck", 1),
            Card(3, "HandsArea", 0, health=5))
        after = Board(
            Card(2, "PlayerDeck", 0),
            Card(1, "HandsArea", 1),
            Card(3, "UpgradesArea", 0, host=9, owner=1, card="01002",
                 face_up=False, attack=2),
            Card(4, "DiscardPile", 0))

        events = model.Derive(before, after, "WhenPlayerInTurn", "Play")
        self.assertTrue(events)
        for event in events:
            with self.subTest(kind=event["kind"]):
                self.assertIn(event["kind"], model.VOCABULARY)
                declared = set(model.VOCABULARY[event["kind"]]) | set(model.UNIVERSAL)
                self.assertEqual(set(event) - declared, set())

    def testEveryEventCarriesTheCause(self):
        """`trigger` and `verb` are the half a digest cannot show."""
        before = Board(Card(1, "HandsArea", 0))
        after = Board(Card(1, "DiscardPile", 0))
        for event in model.Derive(before, after, "WhenPlayerInTurn", "Play"):
            self.assertEqual(event["trigger"], "WhenPlayerInTurn")
            self.assertEqual(event["verb"], "Play")


class TestPositionIsNotAnEvent(unittest.TestCase):

    def testDrawingFromTheMiddleEmitsNoReorder(self):
        """The cards above a drawn card shift down. That is not four events."""
        before = Board(*[Card(i, "PlayerDeck", i) for i in range(5)])
        after = Board(
            Card(0, "PlayerDeck", 0),
            Card(1, "HandsArea", 0),
            Card(2, "PlayerDeck", 1),
            Card(3, "PlayerDeck", 2),
            Card(4, "PlayerDeck", 3))

        events = model.Derive(before, after)
        self.assertEqual(Kinds(events), ["CardsMoved"])
        self.assertEqual(model.Apply(before, events), after)

    def testAGenuineShuffleIsOneEventForTheArea(self):
        before = Board(*[Card(i, "PlayerDeck", i) for i in range(4)])
        order = [2, 0, 3, 1]
        after = Board(*[Card(object_id, "PlayerDeck", position)
                        for position, object_id in enumerate(order)])

        events = model.Derive(before, after)
        self.assertEqual(Kinds(events), ["AreaReordered"])
        self.assertEqual(events[0]["order"], order)
        self.assertEqual(model.Apply(before, events), after)

    def testABatchOfDrawsIsOneEvent(self):
        """Drawing five cards is one thing that happened."""
        before = Board(*[Card(i, "PlayerDeck", i) for i in range(6)])
        after = Board(
            Card(5, "PlayerDeck", 0),
            *[Card(i, "HandsArea", i) for i in range(5)])

        events = model.Derive(before, after)
        self.assertEqual(Kinds(events), ["CardsMoved"])
        self.assertEqual(len(events[0]["cards"]), 5)
        self.assertEqual(model.Apply(before, events), after)


class TestAnAreaIsNotAZoneName(unittest.TestCase):

    def testTwoPlayersHandsAreTwoAreas(self):
        """Both hands are `HandsArea` and both index from zero."""
        before = Board(
            Card(1, "HandsArea", 0, owner=0), Card(2, "HandsArea", 1, owner=0),
            Card(3, "HandsArea", 0, owner=1), Card(4, "HandsArea", 1, owner=1))
        after = Board(
            Card(1, "DiscardPile", 0, owner=0), Card(2, "HandsArea", 0, owner=0),
            Card(3, "HandsArea", 0, owner=1), Card(4, "HandsArea", 1, owner=1))

        events = model.Derive(before, after)
        self.assertEqual(Kinds(events), ["CardsMoved"])
        # Player 1's hand is untouched. A model that merged the two would
        # renumber across the join and move cards 3 and 4.
        self.assertEqual(model.Apply(before, events), after)

    def testTheMoveNamesBothAreasNotJustTheZones(self):
        before = Board(Card(1, "HandsArea", 0, owner=1))
        after = Board(Card(1, "UpgradesArea", 0, owner=1, host=9))

        events = model.Derive(before, after)
        moved = events[0]
        self.assertEqual(moved["from"],
                         {"zone": "HandsArea", "owner": 1, "host": -1, "id": ""})
        self.assertEqual(moved["to"],
                         {"zone": "UpgradesArea", "owner": 1, "host": 9, "id": ""})

    def testTheIdentitySlotIsEmptyOnADigestBoard(self):
        """A digest cannot name an area, so the slot travels empty rather than absent.

        Present-and-empty rather than missing so that a reader never has to
        decide whether a descriptor came from a digest or an engine: the key is
        always there, and `""` says "this area was described, not identified".
        """
        before = Board(Card(1, "HandsArea", 0))
        after = Board(Card(1, "DiscardPile", 0))
        moved = model.Derive(before, after)[0]
        self.assertEqual(moved["from"]["id"], "")


class TestALandingIndexDescribesTheFinalArea(unittest.TestCase):
    """MARVEL-163. Found by replaying the corpus against engine state.

    The index a card carries is read off the state the step *ends* in, where
    every one of the step's arrivals is already present. Splicing arrivals into
    an area one source at a time therefore puts the early ones where the late
    ones are going to be.

    This never showed up while the round trip was derived from digest diffs,
    because `Derive` predicted the positions correctly and so emitted no
    `AreaReordered` to disagree with -- the prediction and the placement were
    two pieces of code, and only `Apply` was wrong.
    """

    def testTwoSourcesLandingInOneArea(self):
        before = Board(
            Card(1, "EncounterDiscardPile", 0),
            Card(2, "EncounterDeck", 0),
            Card(3, "ObligationsArea", 0))
        # Card 3 lands *under* card 2, even though its source sorts later.
        after = Board(
            Card(1, "EncounterDiscardPile", 0),
            Card(3, "EncounterDiscardPile", 1),
            Card(2, "EncounterDiscardPile", 2))

        events = model.Derive(before, after)
        self.assertEqual(Kinds(events).count("CardsMoved"), 2)
        self.assertNotIn("AreaReordered", Kinds(events))
        self.assertEqual(model.Apply(before, events), after)

    def testAnArrivalDoesNotDisplaceAnEarlierOne(self):
        """Four arrivals from three sources, interleaved with what was there."""
        before = Board(
            Card(1, "EncounterDiscardPile", 0),
            Card(2, "EncounterDeck", 0), Card(3, "EncounterDeck", 1),
            Card(4, "DealtEncounterCardsDeck", 0),
            Card(5, "RevealingArea", 0))
        after = Board(
            Card(1, "EncounterDiscardPile", 0),
            Card(5, "EncounterDiscardPile", 1),
            Card(2, "EncounterDiscardPile", 2),
            Card(4, "EncounterDiscardPile", 3),
            Card(3, "EncounterDiscardPile", 4))

        events = model.Derive(before, after)
        self.assertEqual(model.Apply(before, events), after)


class TestAnEngineBoardIdentifiesItsAreas(unittest.TestCase):
    """The area key `tools/events/state.py` supplies, and why it is needed.

    Measured over the corpus: `(zone, owner, host)` names more than one area
    for `AsideDeck` and `RemovedArea` in every game with more than one player.
    A board built from engine objects carries the area's own object id, and the
    triple beside it becomes description rather than address.
    """

    @staticmethod
    def Engine(object_id, zone, index, area_id, *, owner=0, host=-1):
        record = Card(object_id, zone, index, owner=owner, host=host)
        record["area"] = {"zone": zone, "owner": owner, "host": host,
                          "id": area_id}
        return record

    def testTwoAreasSharingATripleStayApart(self):
        """Two set-aside decks, both `('AsideDeck', -1, -1)`, both from zero."""
        engine = self.Engine
        before = Board(
            engine(1, "AsideDeck", 0, "20", owner=-1),
            engine(2, "AsideDeck", 1, "20", owner=-1),
            engine(3, "AsideDeck", 0, "35", owner=-1),
            engine(4, "AsideDeck", 1, "35", owner=-1))
        after = Board(
            engine(1, "AsideDeck", 0, "20", owner=-1),
            engine(2, "AsideDeck", 1, "20", owner=-1),
            engine(4, "AsideDeck", 0, "35", owner=-1),
            engine(3, "AsideDeck", 1, "35", owner=-1))

        events = model.Derive(before, after)
        # One shuffle, in one of the two decks, and it names which.
        self.assertEqual(Kinds(events), ["AreaReordered"])
        self.assertEqual(events[0]["area"]["id"], "35")
        self.assertEqual(model.Apply(before, events), after)

    def testAMoveDoesNotRewriteTheController(self):
        """On an engine board, `owner` is the card's and the area's is separate.

        A side scheme controlled by player 3 moving into the scenario's
        side-scheme area keeps its controller. On a digest board the two are
        the same field and the move implies a change of control; here it must
        not.
        """
        engine = self.Engine
        before = Board(engine(1, "HandsArea", 0, "22", owner=3))
        after = Board(engine(1, "SideSchemesArea", 0, "18", owner=3))
        after[1]["area"]["owner"] = -1

        events = model.Derive(before, after)
        self.assertEqual(Kinds(events), ["CardsMoved"])
        applied = model.Apply(before, events)
        self.assertEqual(applied[1]["owner"], 3)
        self.assertEqual(applied[1]["area"]["owner"], -1)
        self.assertEqual(applied, after)


class TestTheRestOfTheVocabulary(unittest.TestCase):

    def testAbsentAndZeroAreDifferent(self):
        """A field that is gone means the card no longer registers it."""
        before = Board(Card(1, "HeroArea", 0, attack=2, t_AVENGER=1))
        after = Board(Card(1, "HeroArea", 0, attack=0))

        events = model.Derive(before, after)
        by_field = {event["field"]: event for event in events
                    if event["kind"] == "FieldSet"}
        self.assertEqual(by_field["attack"]["to"], 0)
        self.assertIsNone(by_field["t_AVENGER"]["to"])
        self.assertEqual(model.Apply(before, events), after)

    def testFormChangeIsNotAFlip(self):
        before = Board(Card(1, "HeroArea", 0, card="01001a", face_up=True))
        after = Board(Card(1, "HeroArea", 0, card="01001b", face_up=True))
        self.assertEqual(Kinds(model.Derive(before, after)), ["CardFormChanged"])

    def testRehostingIsADetachAndAnAttach(self):
        before = Board(Card(1, "UpgradesArea", 0, host=9))
        after = Board(Card(1, "UpgradesArea", 0, host=10))

        events = model.Derive(before, after)
        self.assertEqual([k for k in Kinds(events) if k != "CardsMoved"],
                         ["CardDetached", "CardAttached"])
        self.assertEqual(model.Apply(before, events), after)

    def testNothingHappeningEmitsNothing(self):
        """35% of recorded steps change no state at all, and that is legal.

        An input that only opens a prompt produces an empty event list, which is
        different from producing no list.
        """
        board = Board(Card(1, "HandsArea", 0))
        self.assertEqual(model.Derive(board, dict(board)), [])


if __name__ == "__main__":
    unittest.main()
