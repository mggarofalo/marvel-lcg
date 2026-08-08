"""Tests for `RunPuzzle` card resolution (MARVEL-51, MARVEL-61).

`FindOrCreateFace` is the front door for every `Puzzle.*` command that takes a
name, and it used to search everywhere except the board. A command naming a card
in play therefore built a *second* copy in the aside deck and acted on that, so
the visible board never moved and a stray card was left behind. The cheat console
and the web puzzle loader both go through this path.

MARVEL-51 fixed the board and left the rest: the aside deck, the set-aside decks,
the victory display and the removed-from-game area were all still unsearched, so
the same silent duplicate came back for any card sitting in one of them. The
resolver is now zone-complete, driven off `ZONE_GROUP_BY_DECK_TYPE`, and
`TestEveryZoneIsAccountedFor` is what keeps it that way.

These boot the engine, so they must be run from `py_src/` like everything else in
this repo. Boards are built with the `Puzzle.Create*` helpers and `CardFactory`,
neither of which reaches the resolver -- a test that used the resolver to build
its own board would be asserting against itself.
"""

import unittest

# `engine` first, and not for its side effects: `game.*` modules import each
# other in a cycle that only resolves if `engine/__init__.py` has already walked
# it. Importing `game.operate.worlds` cold raises ImportError.
import engine  # noqa: F401

from game.card.factory import CardFactory
from game.deck import DeckType
from game.operate.worlds import Worlds
from game.puzzle.puzzle import (
    ZONE_GROUP_BY_DECK_TYPE, PuzzleCardError, RunPuzzle)
from tools.spec.case import SpecCase, ThenStep
from tools.spec.harness import EnsureEngine, NewGameForCase
from tools.spec.policy import TranscriptPolicy

# Core set, chosen for what each one is rather than for its text.
RHINO = "01094"                 # the villain, stage 1, in play from setup
RHINO_STAGE_2 = "01095"         # same printed name, sitting in the villain deck
SWINGING_WEB_KICK = "01005"     # a player card, so it can go to hand or deck
HYDRA_MERCENARY = "01101"       # a *non-unique* minion, so two can be in play
SANDMAN = "01102"               # a second minion, distinctly named


def NewWorld(*heroes):
    """A solo Rhino board: villain, main scheme, identity and nothing else."""
    EnsureEngine()
    case = SpecCase(
        name="puzzle card resolution",
        scenario="rhino",
        heroes=heroes or ("spider_man",),
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
        # "Rhino" is both stage 1 in play and stage 2 in the villain deck. Both
        # are searched, but not together: the villain deck is a staging zone and
        # sits in the `set aside` group, which is only reached when the board
        # has no candidate. So there is one candidate rather than two.
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
# The zones MARVEL-51 left unsearched.

class TestEveryZoneIsAccountedFor(unittest.TestCase):
    """The guard that keeps the resolver zone-complete.

    MARVEL-51 hand-listed three zone groups and MARVEL-61 was everything they
    missed. Hand-listing is the defect, so this asserts the map covers
    `DeckType` exactly -- a zone added to the engine fails here rather than
    going quietly unsearched.
    """

    def test_the_map_covers_every_deck_type(self):
        self.assertEqual(set(ZONE_GROUP_BY_DECK_TYPE), set(DeckType))

    def test_the_map_names_no_deck_type_the_engine_does_not_have(self):
        # Covered by the equality above, stated on its own so a failure says
        # which direction drifted.
        self.assertEqual([d for d in ZONE_GROUP_BY_DECK_TYPE if d not in DeckType],
                         [])


class TestSetAsideZones(unittest.TestCase):

    def test_a_card_in_the_additional_discard_pile_resolves(self):
        # `GameServerNewGame.play_puzzle` emits `CreatePlayerAdditionalDeck` for
        # a puzzle's set_aside list, so the web puzzle editor routinely puts
        # cards exactly here and then lets the author name them.
        world = NewWorld()
        puzzle = RunPuzzle(world)
        puzzle.CreatePlayerAdditionalDeck(SWINGING_WEB_KICK)
        before = CardCount(world)

        face = puzzle.FindOrCreateFace(SWINGING_WEB_KICK)

        self.assertEqual(face.card.area.deck_type.name, "AdditionalDiscardPile")
        self.assertEqual(CardCount(world), before)

    def test_set_aside_deck_is_the_additional_discard_pile(self):
        # One object under two names (`Player.__init__`). Worth pinning: it is
        # why the zone map needs no `set_aside_deck` entry of its own, and a
        # future split would silently drop the zone otherwise.
        world = NewWorld()
        player = world.GetCurrentPlayer()

        self.assertIs(player.set_aside_deck, player.additional_discard_pile)

    def test_a_card_in_the_set_aside_nemesis_set_resolves(self):
        world = NewWorld()
        puzzle = RunPuzzle(world)
        player = world.GetCurrentPlayer()
        CardFactory.GenerateCard(SANDMAN, player.set_aside_nemesis_sets, world)
        before = CardCount(world)

        face = puzzle.FindOrCreateFace(SANDMAN)

        self.assertEqual(face.card.area.deck_type.name, "AsideDeck")
        self.assertEqual(CardCount(world), before)

    def test_a_card_in_play_still_wins_over_a_set_aside_copy(self):
        # Ordering: widening the search must not cost the board its precedence.
        world = NewWorld()
        puzzle = RunPuzzle(world)
        CardFactory.GenerateCard(HYDRA_MERCENARY, world.aside_deck, world)
        puzzle.CreateEncounterDeck(HYDRA_MERCENARY)
        in_play = Worlds.GetEncounterDeckCards(world)[0]
        puzzle.PutIntoPlay(in_play)

        face = puzzle.FindOrCreateFace(HYDRA_MERCENARY)

        self.assertIs(face, in_play)
        self.assertEqual(face.card.area.deck_type.name, "EngagedEnemiesArea")


class TestOutOfTheGameZones(unittest.TestCase):
    """A card that has left the game is named, not silently rebuilt.

    Searched last, so anything still in the game wins. Resolving to the real
    card is the point: the alternative is a fresh copy in the aside deck and a
    command that appears to have done nothing.
    """

    def test_a_card_in_the_victory_display_resolves(self):
        world = NewWorld()
        puzzle = RunPuzzle(world)
        CardFactory.GenerateCard(SANDMAN, world.victory_display, world)
        before = CardCount(world)

        face = puzzle.FindOrCreateFace(SANDMAN)

        self.assertEqual(face.card.area.deck_type.name, "VictoryDisplay")
        self.assertEqual(CardCount(world), before)

    def test_a_card_removed_from_the_game_resolves(self):
        world = NewWorld()
        puzzle = RunPuzzle(world)
        CardFactory.GenerateCard(SANDMAN, world.area_removed, world)
        before = CardCount(world)

        face = puzzle.FindOrCreateFace(SANDMAN)

        self.assertEqual(face.card.area.deck_type.name, "RemovedArea")
        self.assertEqual(CardCount(world), before)


class TestDuplicatesDoNotCompound(unittest.TestCase):

    def test_naming_a_card_the_game_does_not_hold_twice_finds_the_first_copy(self):
        # The quiet one. `CreateCard` drops what it builds in `world.aside_deck`,
        # which the resolver could not see, so every command naming the card
        # made another copy and none of them ever became findable.
        world = NewWorld()
        puzzle = RunPuzzle(world)
        before = CardCount(world)

        first = puzzle.FindOrCreateFace(SANDMAN)
        second = puzzle.FindOrCreateFace(SANDMAN)

        self.assertIs(second, first)
        self.assertEqual(CardCount(world), before + 1)

    def test_two_commands_naming_an_absent_card_leave_one_copy(self):
        # What that cost when it went wrong: the aside deck accumulated a copy
        # per command, and the second command acted on a card the first had
        # never touched.
        world = NewWorld()
        puzzle = RunPuzzle(world)

        puzzle.Damage(SANDMAN, 2)
        puzzle.Damage(SANDMAN, 3)

        made = [f for f in world.aside_deck.Get(True) if f.IsName(SANDMAN)]
        self.assertEqual(len(made), 1,
                         f"the aside deck holds {len(made)} copies of Sandman")


class TestAnotherPlayersZones(unittest.TestCase):
    """A card another player holds is a card the game holds.

    The player group is not scoped to the current player. Scoping it would be
    the same silent-duplicate hole in a two-player game, and a bare name does
    not say whose hand it meant -- so two copies is an ambiguity, not a pick.
    """

    def test_a_card_in_another_players_deck_resolves(self):
        world = NewWorld("spider_man", "captain_marvel")
        puzzle = RunPuzzle(world)
        other = [p for p in world.const_players
                 if p is not world.GetCurrentPlayer()][0]
        CardFactory.GenerateCard(SWINGING_WEB_KICK, other.player_deck, world)
        before = CardCount(world)

        face = puzzle.FindOrCreateFace(SWINGING_WEB_KICK)

        self.assertEqual(face.card.area.deck_type.name, "PlayerDeck")
        self.assertEqual(CardCount(world), before)

    def test_a_copy_in_each_players_hand_is_refused(self):
        world = NewWorld("spider_man", "captain_marvel")
        puzzle = RunPuzzle(world)
        for player in world.const_players:
            CardFactory.GenerateCard(SWINGING_WEB_KICK, player.hand_cards, world)

        with self.assertRaises(PuzzleCardError) as caught:
            puzzle.FindOrCreateFace(SWINGING_WEB_KICK)

        self.assertIn("matches 2 cards", str(caught.exception))


class TestMultipleVillains(unittest.TestCase):
    """The MARVEL-51 failure that came back for multi-villain scenarios.

    `Worlds.GetEncounterDiscardPileCards` reported only the last villain's pile
    (MARVEL-60), so a card in an earlier one was invisible to the encounter
    group and the resolver built a duplicate.
    """

    def test_a_card_in_an_earlier_villains_discard_pile_resolves(self):
        world = NewWorld()
        puzzle = RunPuzzle(world)
        scenario = world.GetScenario()

        first = scenario.area_villain.Get()[0]
        second = scenario.villain_deck.Get()[0]
        scenario.villain_deck.Remove(second.card)
        scenario.area_villain.Append(second.card)
        second.SetEncounterDeck(scenario.encounter_deck)

        CardFactory.GenerateCard(SANDMAN, first.encounter_discard_pile, world)
        before = CardCount(world)

        face = puzzle.FindOrCreateFace(SANDMAN)

        self.assertEqual(face.card.area.deck_type.name, "EncounterDiscardPile")
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
