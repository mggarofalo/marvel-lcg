"""Tests for `RunPuzzle` card resolution (MARVEL-51).

`FindOrCreateFace` is the front door for every `Puzzle.*` command that takes a
name, and it used to search everywhere except the board. A command naming a card
in play therefore built a *second* copy in the aside deck and acted on that, so
the visible board never moved and a stray card was left behind. The cheat console
and the web puzzle loader both go through this path.

These boot the engine, so they must be run from `py_src/` like everything else in
this repo. Boards are built with the `Puzzle.Create*` helpers, which take card ids
and never reach the resolver -- a test that used the resolver to build its own
board would be asserting against itself.
"""

import unittest

# `engine` first, and not for its side effects: `game.*` modules import each
# other in a cycle that only resolves if `engine/__init__.py` has already walked
# it. Importing `game.operate.worlds` cold raises ImportError.
import engine  # noqa: F401

from game.operate.worlds import Worlds
from game.puzzle.puzzle import PuzzleCardError, RunPuzzle
from tools.spec.case import SpecCase, ThenStep
from tools.spec.harness import EnsureEngine, NewGameForCase
from tools.spec.policy import TranscriptPolicy

# Core set, chosen for what each one is rather than for its text.
RHINO = "01094"                 # the villain, stage 1, in play from setup
RHINO_STAGE_2 = "01095"         # same printed name, sitting in the villain deck
SWINGING_WEB_KICK = "01005"     # a player card, so it can go to hand or deck
HYDRA_MERCENARY = "01101"       # a *non-unique* minion, so two can be in play


def NewWorld():
    """A solo Rhino board: villain, main scheme, identity and nothing else."""
    EnsureEngine()
    case = SpecCase(
        name="puzzle card resolution",
        scenario="rhino",
        heroes=("spider_man",),
        beats=(ThenStep("Rhino", "health", 14),),
    )
    game = NewGameForCase(case, TranscriptPolicy())
    assert game.GameSetup()
    return game.world


def CardCount(world):
    return len(world.object_manager.card_dict)


def CardById(world, object_id):
    return world.object_manager.card_dict[object_id]


def PutMinionsIntoPlay(puzzle, world, count):
    """`count` Hydra Mercenaries engaged with the player, newest id last."""
    puzzle.CreateEncounterDeck(*[HYDRA_MERCENARY] * count)
    faces = list(Worlds.GetEncounterDeckCards(world))
    assert len(faces) == count
    for face in faces:
        puzzle.PutIntoPlay(face)
    return sorted(face.card.object_id for face in faces)


################################################################################
# The bug itself.

class TestNamingACardInPlay(unittest.TestCase):

    def test_a_card_id_resolves_to_the_card_on_the_board(self):
        world = NewWorld()
        puzzle = RunPuzzle(world)
        rhino = world.GetScenario().area_villain.Get()[0]

        before = CardCount(world)
        face = puzzle.FindOrCreateFace(RHINO)

        self.assertIs(face, rhino)
        self.assertEqual(face.card.area.deck_type.name, "VillainArea")
        self.assertEqual(CardCount(world), before,
                         "resolving a card in play must not allocate one")

    def test_a_printed_name_resolves_the_same_way_as_an_id(self):
        world = NewWorld()
        puzzle = RunPuzzle(world)

        self.assertIs(puzzle.FindOrCreateFace("Rhino"),
                      puzzle.FindOrCreateFace(RHINO))

    def test_damaging_by_name_damages_the_villain_in_play(self):
        # The issue's reproduction: this used to leave the Rhino at 14 and put a
        # damaged duplicate in the aside deck.
        world = NewWorld()
        puzzle = RunPuzzle(world)
        before = CardCount(world)

        puzzle.Damage(RHINO, 3)

        rhino = world.GetScenario().area_villain.Get()[0]
        self.assertEqual(rhino.health, 11)
        self.assertEqual(rhino.max_health, 14)
        self.assertEqual(CardCount(world), before)

    def test_a_card_in_play_wins_over_a_copy_in_the_encounter_deck(self):
        # Both copies match the name. The one the command means is the one the
        # player can see, which is the rule `tools/spec/resolve.py` also applies.
        world = NewWorld()
        puzzle = RunPuzzle(world)
        puzzle.CreateEncounterDeck(HYDRA_MERCENARY, HYDRA_MERCENARY)
        in_play = Worlds.GetEncounterDeckCards(world)[0]
        puzzle.PutIntoPlay(in_play)

        face = puzzle.FindOrCreateFace(HYDRA_MERCENARY)

        self.assertIs(face, in_play)
        self.assertEqual(face.card.area.deck_type.name, "EngagedEnemiesArea")

    def test_a_card_in_hand_resolves_rather_than_duplicating(self):
        world = NewWorld()
        puzzle = RunPuzzle(world)
        puzzle.CreateHandCards(SWINGING_WEB_KICK)
        before = CardCount(world)

        face = puzzle.FindOrCreateFace(SWINGING_WEB_KICK)

        self.assertEqual(face.card.area.deck_type.name, "HandsArea")
        self.assertEqual(CardCount(world), before)

    def test_a_card_in_the_encounter_deck_still_resolves(self):
        # The one group that was already searched. It must keep working.
        world = NewWorld()
        puzzle = RunPuzzle(world)
        puzzle.CreateEncounterDeck(HYDRA_MERCENARY)
        before = CardCount(world)

        face = puzzle.FindOrCreateFace(HYDRA_MERCENARY)

        self.assertEqual(face.card.area.deck_type.name, "EncounterDeck")
        self.assertEqual(CardCount(world), before)


################################################################################
# Ambiguity.

class TestAmbiguousNames(unittest.TestCase):
    """A name matching several cards is reported, never picked from.

    Which copy a bare name meant is not something the author wrote down, so
    answering for them produces a board nobody authored and no sign that it
    happened. The object ids are in the message because `PuzzleHelper.Exec`
    already binds `c<N>` for every card, so the author has a way to say which.
    """

    def test_two_copies_in_play_are_refused(self):
        world = NewWorld()
        puzzle = RunPuzzle(world)
        ids = PutMinionsIntoPlay(puzzle, world, 2)

        with self.assertRaises(PuzzleCardError) as caught:
            puzzle.FindOrCreateFace(HYDRA_MERCENARY)

        message = str(caught.exception)
        self.assertIn("matches 2 cards in play", message)
        for object_id in ids:
            self.assertIn(f"c{object_id}", message)

    def test_two_copies_in_the_player_deck_are_refused(self):
        world = NewWorld()
        puzzle = RunPuzzle(world)
        puzzle.CreatePlayerDeck(SWINGING_WEB_KICK, SWINGING_WEB_KICK)

        with self.assertRaises(PuzzleCardError) as caught:
            puzzle.FindOrCreateFace(SWINGING_WEB_KICK)

        self.assertIn("matches 2 cards", str(caught.exception))
        self.assertIn("Swinging Web Kick", str(caught.exception))

    def test_a_copy_in_hand_and_a_copy_in_the_deck_are_refused(self):
        # Hand, deck and discard are searched as one group precisely so this is
        # an error: an ordering between them would be the only thing deciding
        # which copy the command meant.
        world = NewWorld()
        puzzle = RunPuzzle(world)
        puzzle.CreateHandCards(SWINGING_WEB_KICK)
        puzzle.CreatePlayerDeck(SWINGING_WEB_KICK)

        with self.assertRaises(PuzzleCardError):
            puzzle.FindOrCreateFace(SWINGING_WEB_KICK)

    def test_two_copies_in_the_encounter_deck_are_refused(self):
        world = NewWorld()
        puzzle = RunPuzzle(world)
        puzzle.CreateEncounterDeck(HYDRA_MERCENARY, HYDRA_MERCENARY)

        with self.assertRaises(PuzzleCardError) as caught:
            puzzle.FindOrCreateFace(HYDRA_MERCENARY)

        self.assertIn("encounter deck", str(caught.exception))

    def test_candidates_are_listed_in_object_id_order(self):
        # Deck order puts the newest card on top and moves under a shuffle. An
        # error message should not be the one thing a shuffle rewords.
        world = NewWorld()
        puzzle = RunPuzzle(world)
        ids = PutMinionsIntoPlay(puzzle, world, 3)

        with self.assertRaises(PuzzleCardError) as caught:
            puzzle.FindOrCreateFace(HYDRA_MERCENARY)

        # `GetDisplayName` renders each candidate as "[(05,01101) Hydra Mercenary]".
        message = str(caught.exception)
        positions = [message.index(f"({object_id:02},") for object_id in ids]
        self.assertEqual(positions, sorted(positions),
                         f"candidates are out of object-id order: {message}")

    def test_a_name_matching_one_card_in_play_and_one_in_the_villain_deck_is_fine(self):
        # "Rhino" is both stage 1 in play and stage 2 in the villain deck. The
        # villain deck is not a group this resolver searches, and the board is,
        # so there is one candidate rather than two.
        world = NewWorld()
        puzzle = RunPuzzle(world)
        stage_2 = [card for card in world.object_manager.card_dict.values()
                   if card.object_id and card.face.paper.card_id == RHINO_STAGE_2]
        self.assertEqual(len(stage_2), 1, "expected a stage 2 Rhino to exist")

        face = puzzle.FindOrCreateFace("Rhino")

        self.assertEqual(face.paper.card_id, RHINO)


################################################################################
# The fallback the resolver must keep.

class TestCreatingWhatIsNotThere(unittest.TestCase):

    def test_a_card_the_game_does_not_hold_is_generated(self):
        # A puzzle has to be able to say "a Swinging Web Kick is in play"
        # without first naming a zone to put it in.
        world = NewWorld()
        puzzle = RunPuzzle(world)
        before = CardCount(world)

        face = puzzle.FindOrCreateFace(SWINGING_WEB_KICK)

        self.assertEqual(face.paper.card_id, SWINGING_WEB_KICK)
        self.assertEqual(face.card.area.deck_type.name, "AsideDeck")
        self.assertEqual(CardCount(world), before + 1)

    def test_an_already_resolved_face_is_passed_straight_through(self):
        # The path the spec harness uses. It must not go near the name search.
        world = NewWorld()
        puzzle = RunPuzzle(world)
        rhino = world.GetScenario().area_villain.Get()[0]
        before = CardCount(world)

        self.assertIs(puzzle.FindOrCreateFace(rhino), rhino)
        self.assertIs(puzzle.FindOrCreateFace(rhino.card), rhino)
        self.assertIs(puzzle.FindOrCreateFace(rhino.card.object_id), rhino)
        self.assertEqual(CardCount(world), before)


################################################################################
# Deduplication.

class TestUniqueFaces(unittest.TestCase):
    """A field search unions board areas with the decks hanging off them.

    The same card can be reached twice that way. Counting area memberships
    instead of cards would report an ambiguity that is not one.
    """

    def test_the_same_card_reached_twice_counts_once(self):
        world = NewWorld()
        puzzle = RunPuzzle(world)
        rhino = world.GetScenario().area_villain.Get()[0]

        self.assertEqual(puzzle.UniqueFaces([rhino, rhino, rhino]), [rhino])

    def test_distinct_cards_are_kept_in_the_order_first_seen(self):
        world = NewWorld()
        puzzle = RunPuzzle(world)
        rhino = world.GetScenario().area_villain.Get()[0]
        scheme = Worlds.GetAllMainSchemes(world)[0]

        self.assertEqual(puzzle.UniqueFaces([scheme, rhino, scheme]),
                         [scheme, rhino])


if __name__ == "__main__":
    unittest.main()
