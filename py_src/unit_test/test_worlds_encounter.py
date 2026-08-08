"""Tests for the encounter deck and discard pile accessors (MARVEL-60).

`Worlds.GetEncounterDiscardPileCards` looped over every villain and *reassigned*
rather than accumulated, so every villain but the last was dropped. That is
invisible on a single-villain board, which is why it survived: a scenario where
villains share one deck cannot tell the two behaviours apart.

The mirror-image mistake is just as easy. Villains normally *do* share a deck --
`Villain.PutIntoPlay` points each one at `scenario.encounter_deck` -- so a plain
`+=` reports that deck's cards once per villain. Both are covered here.

These boot the engine, so they must be run from `py_src/` like everything else in
this repo.
"""

import unittest

# `engine` first, and not for its side effects: `game.*` modules import each
# other in a cycle that only resolves if `engine/__init__.py` has already walked
# it. Importing `game.operate.worlds` cold raises ImportError.
import engine  # noqa: F401

from game.card.card_finder import CardFinder
from game.card.face.card_type import Minion
from game.card.factory import CardFactory
from game.deck import DeckType
from game.deck.deck_aside import SetAsideDeck
from game.effect.rule import DebugRule
from game.operate.worlds import Worlds
from tools.spec.case import SpecCase, ThenStep
from tools.spec.harness import EnsureEngine, NewGameForCase
from tools.spec.policy import TranscriptPolicy

HYDRA_MERCENARY = "01101"       # a non-unique minion, so copies can be told apart
SANDMAN = "01102"               # a second minion, distinctly named


def NewWorld():
    """A solo Rhino board: villain, main scheme, identity and nothing else."""
    EnsureEngine()
    case = SpecCase(
        name="encounter pile accumulation",
        scenario="rhino",
        heroes=("spider_man",),
        beats=(ThenStep("Rhino", "health", 14),),
    )
    game = NewGameForCase(case, TranscriptPolicy())
    assert game.GameSetup()
    return game.world


def SecondVillainInPlay(world, *, own_deck: bool):
    """The stage 2 Rhino, moved into the villain area beside stage 1.

    The Wrecking Crew is the scenario this is standing in for -- four villains in
    play at once, three of them holding their own deck. Setting it up for real
    needs the scenario's set-aside cards, which the puzzle scene builder does not
    create, so the board is assembled the way `twc/07001a.py` assembles its own:
    put the villain in the villain area, then `SetEncounterDeck`.

    `own_deck=False` points the villain at `scenario.encounter_deck`, which is
    what `Villain.OnPutIntoPlay` does and therefore the ordinary case.
    """
    scenario = world.GetScenario()
    stage_1 = scenario.area_villain.Get()[0]
    stage_2 = scenario.villain_deck.Get()[0]

    scenario.villain_deck.Remove(stage_2.card)
    scenario.area_villain.Append(stage_2.card)

    if own_deck:
        aside = SetAsideDeck()
        aside.Create(DebugRule(stage_1), stage_2, [], type=DeckType.EncounterDeck)
        stage_2.SetEncounterDeck(aside.deck)
    else:
        stage_2.SetEncounterDeck(scenario.encounter_deck)

    return stage_1, stage_2


def Names(faces):
    return sorted(face.name for face in faces)


class TestSeparateDecks(unittest.TestCase):
    """Villains holding their own decks are all reported, not just the last."""

    def test_every_villains_discard_pile_is_returned(self):
        # The bug itself: this returned only Sandman.
        world = NewWorld()
        first, second = SecondVillainInPlay(world, own_deck=True)
        CardFactory.GenerateCard(HYDRA_MERCENARY, first.encounter_discard_pile, world)
        CardFactory.GenerateCard(SANDMAN, second.encounter_discard_pile, world)

        faces = Worlds.GetEncounterDiscardPileCards(world)

        self.assertEqual(Names(faces), ["Hydra Mercenary", "Sandman"])

    def test_every_villains_encounter_deck_is_returned(self):
        world = NewWorld()
        first, second = SecondVillainInPlay(world, own_deck=True)
        CardFactory.GenerateCard(HYDRA_MERCENARY, first.encounter_deck, world)
        CardFactory.GenerateCard(SANDMAN, second.encounter_deck, world)

        faces = Worlds.GetEncounterDeckCards(world)

        self.assertEqual(Names(faces), ["Hydra Mercenary", "Sandman"])

    def test_a_finder_still_filters_the_accumulated_piles(self):
        # The finder must see every pile, not the last one it was handed.
        world = NewWorld()
        first, second = SecondVillainInPlay(world, own_deck=True)
        minion = CardFactory.GenerateCard(
            HYDRA_MERCENARY, first.encounter_discard_pile, world)
        CardFactory.GenerateCard("01008", second.encounter_discard_pile, world)

        faces = Worlds.GetEncounterDiscardPileCards(world, CardFinder(card_type=Minion))

        self.assertEqual([face.card.object_id for face in faces],
                         [minion.object_id])


class TestSharedDecks(unittest.TestCase):
    """The ordinary case: several villains, one deck between them.

    Accumulating without checking deck identity reports that deck's cards once
    per villain, which is the same defect pointed the other way -- a caller
    offering the player a choice would list the same card twice.
    """

    def test_a_shared_discard_pile_is_read_once(self):
        world = NewWorld()
        first, second = SecondVillainInPlay(world, own_deck=False)
        self.assertIs(first.encounter_discard_pile, second.encounter_discard_pile)
        CardFactory.GenerateCard(HYDRA_MERCENARY, first.encounter_discard_pile, world)

        faces = Worlds.GetEncounterDiscardPileCards(world)

        self.assertEqual(Names(faces), ["Hydra Mercenary"])

    def test_a_shared_encounter_deck_is_read_once(self):
        world = NewWorld()
        first, second = SecondVillainInPlay(world, own_deck=False)
        self.assertIs(first.encounter_deck, second.encounter_deck)
        CardFactory.GenerateCard(HYDRA_MERCENARY, first.encounter_deck, world)

        faces = Worlds.GetEncounterDeckCards(world)

        self.assertEqual(Names(faces), ["Hydra Mercenary"])


class TestNoVillain(unittest.TestCase):
    """With no villain in play the scenario's own decks answer.

    Worth pinning: the fallback is the branch a puzzle board hits before setup
    has put a villain out, and it is easy to lose when the loop above it moves.
    """

    def test_the_scenario_decks_answer_when_the_villain_area_is_empty(self):
        world = NewWorld()
        scenario = world.GetScenario()
        for villain in list(scenario.area_villain.Get()):
            scenario.area_villain.Remove(villain.card)
        CardFactory.GenerateCard(HYDRA_MERCENARY, scenario.encounter_deck, world)
        CardFactory.GenerateCard(SANDMAN, scenario.encounter_discard_pile, world)

        self.assertEqual(Names(Worlds.GetEncounterDeckCards(world)),
                         ["Hydra Mercenary"])
        self.assertEqual(Names(Worlds.GetEncounterDiscardPileCards(world)),
                         ["Sandman"])


if __name__ == "__main__":
    unittest.main()
