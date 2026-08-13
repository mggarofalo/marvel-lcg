"""A card removed from the game stays in the world, and `card_dict` is append-only.

Marvel Champions has no "destroy". A card is discarded, removed from the game,
or defeated, and every one of those leaves it in a zone with its object id
intact. `Faces.RemoveAllFromGame` is the strongest of them and the one on live
paths: it moves the card to `world.area_removed`, from where
`game/operate/search.py` can still find it and a card like `16174` can bring it
back.

`Card.Destroy` modelled a fourth outcome, ceasing to exist, and it was the only
code anywhere that took a card out of `object_manager.card_dict`. It had one
caller, `Deck2.Destroy`, which had none. MARVEL-70 deleted both, along with
`ObjectManager.RemoveCard`, and this file replaces `test_card_destroy.py`.

**What the deletion buys is the invariant these tests pin.** With no removal
path, `card_dict` holds every card the game ever created, so the digest's
contract -- "every card, no exclusions" (`game/world/digest.py`) -- is true
without a caveat, and `game/world/invariants.py` can treat a card that is in
`card_dict` but in no zone's list as a violation rather than a legal state. It
is also the shape a C# port should reproduce: one dictionary that only grows,
and "removed from the game" as a zone rather than a deletion.

Worth saying plainly, because it is the opposite of what the phrase suggests:
`RemoveAllFromGame` does **not** take a card out of `card_dict`, and must not.
Doing so would recreate MARVEL-50 -- an entry missing from the registry while
the area it names still holds the card -- this time with a live caller.

The board is built the way `unit_test/test_puzzle.py` builds one, and the
removal is driven through `Puzzle.Remove`, which is a thin wrapper over
`Faces.RemoveAllFromGame`. Calling the operation the game calls, rather than
reaching into the areas, is what makes these tests evidence about the engine.

See MARVEL-70, MARVEL-50 and `docs/state-digest-v2.md`.
"""

import ast
import unittest
from pathlib import Path

# `engine` first, and not for its side effects: `game.*` modules import each
# other in a cycle that only resolves if `engine/__init__.py` has already walked
# it.
import engine  # noqa: F401  pylint: disable=unused-import

from game.card import Card
from game.deck import Deck2
from game.object.manager import ObjectManager
from game.puzzle.puzzle import RunPuzzle
from game.world import digest
from tools.spec.case import SpecCase, ThenStep
from tools.spec.harness import EnsureEngine, NewGameForCase
from tools.spec.policy import TranscriptPolicy


PY_SRC = Path(__file__).resolve().parent.parent

# Where the append-only rule has to hold. `core/` and `tools/` know nothing
# about a world.
GUARDED_PACKAGES = ("engine", "game")

# A player card, set aside rather than in play, so removing it disturbs nothing
# else on the board and the assertions are about the removal alone.
SWINGING_WEB_KICK = "01005"


def NewWorld():
    """A solo Rhino board: villain, main scheme, identity and nothing else."""
    EnsureEngine()
    case = SpecCase(
        name="removed from the game",
        scenario="rhino",
        heroes=("spider_man",),
        beats=(ThenStep("Rhino", "health", 14),),
    )
    game = NewGameForCase(case, TranscriptPolicy())
    assert game.GameSetup()
    return game.world


class RemovalTestCase(unittest.TestCase):
    """Sets a card aside and removes it from the game."""

    def setUp(self):
        self.world = NewWorld()
        self.manager = self.world.object_manager
        puzzle = RunPuzzle(self.world)
        puzzle.CreatePlayerAdditionalDeck(SWINGING_WEB_KICK)
        self.face = puzzle.FindOrCreateFace(SWINGING_WEB_KICK)
        self.card = self.face.card
        self.object_id = self.card.object_id
        self.before = dict(self.manager.card_dict)
        self.puzzle = puzzle

    def Remove(self):
        self.puzzle.Remove(SWINGING_WEB_KICK)

    def Zones(self):
        return {record["id"]: record["zone"]
                for record in digest.BuildDocument(self.world)["cards"]}


class TestARemovedCardIsStillInTheWorld(RemovalTestCase):

    def test_the_card_starts_somewhere_else(self):
        # Everything below is vacuous if the removal is a no-op.
        self.assertEqual(self.card.area.deck_type.name, "AdditionalDiscardPile")
        self.assertEqual(self.Zones()[self.object_id], "AdditionalDiscardPile")

    def test_removing_it_moves_it_to_the_removed_area(self):
        self.Remove()

        self.assertIs(self.card.area, self.world.area_removed)

    def test_it_keeps_its_entry_in_card_dict(self):
        self.Remove()

        self.assertIn(self.object_id, self.manager.card_dict)
        self.assertIs(self.manager.card_dict[self.object_id], self.card)

    def test_no_other_card_moves_or_disappears(self):
        self.Remove()

        self.assertEqual(self.manager.card_dict, self.before)

    def test_it_sits_in_the_area_it_claims(self):
        # The MARVEL-50 failure mode, checked on the live path rather than on
        # the dead one it was found on: an entry in `card_dict` whose `area` no
        # longer holds the card. `game/world/invariants.py` calls this
        # `zone/absent` and aborts a self-play run over it.
        self.Remove()

        self.assertIn(self.card, self.card.area.cards)

    def test_the_digest_still_describes_it(self):
        self.Remove()

        self.assertIn(self.object_id, self.Zones())

    def test_the_digest_reports_the_zone_it_is_actually_in(self):
        self.Remove()

        self.assertEqual(self.Zones()[self.object_id], "RemovedArea")

    def test_nothing_is_recorded_as_absent(self):
        # `/absent` is the digest's fallback for a card no zone holds, which is
        # what a removal that updated one structure and not the other produces.
        self.Remove()

        absent = [object_id for object_id, zone in self.Zones().items()
                  if zone.endswith(digest.SUFFIX_ABSENT)]

        self.assertEqual(absent, [])


class TestCardDictIsAppendOnly(RemovalTestCase):

    def test_it_holds_every_id_the_game_ever_allocated(self):
        # Ids come from a counter that only increments, starting at 0, so an
        # append-only registry is exactly `range(0, highest + 1)`. A gap means
        # something took a card out.
        self.Remove()
        highest = self.manager.index_dict["card"]

        self.assertEqual(set(self.manager.card_dict), set(range(0, highest + 1)))

    def test_removal_allocates_no_new_card(self):
        highest = self.manager.index_dict["card"]

        self.Remove()

        self.assertEqual(self.manager.index_dict["card"], highest)

    def test_every_card_in_the_digest_is_in_card_dict(self):
        self.Remove()

        self.assertEqual(set(self.Zones()), set(self.manager.card_dict))


class TestNothingDeletesFromCardDict(unittest.TestCase):
    """The rule itself, read off the source rather than off one game.

    A played game can only show that no card left `card_dict` on the path it
    happened to take. This scan is what fails the day someone reintroduces a
    removal for a case no test plays -- which is the whole history of
    `Card.Destroy`: three defects in eight lines, none of which could fire.
    """

    def Deletions(self):
        """[(file, line, what)] for every statement that drops a card entry."""
        found = []
        for package in GUARDED_PACKAGES:
            for path in sorted((PY_SRC / package).rglob("*.py")):
                tree = ast.parse(path.read_text(encoding="utf-8"))
                where = path.relative_to(PY_SRC).as_posix()
                for node in ast.walk(tree):
                    for description in Drops(node):
                        found.append((where, node.lineno, description))
        return found

    def test_no_module_removes_an_entry_from_card_dict(self):
        self.assertEqual(self.Deletions(), [])

    def test_the_scan_reads_the_packages(self):
        # Guards against a scan that walks no files and passes vacuously.
        for package in GUARDED_PACKAGES:
            self.assertTrue(list((PY_SRC / package).rglob("*.py")), package)

    def test_the_scan_recognises_a_deletion(self):
        self.assertEqual(Found("del self.card_dict[object_id]"),
                         ["del card_dict[...]"])

    def test_the_scan_recognises_a_pop(self):
        self.assertEqual(Found("self.card_dict.pop(object_id)"),
                         ["card_dict.pop(...)"])

    def test_the_scan_recognises_a_clear(self):
        self.assertEqual(Found("self.card_dict.clear()"),
                         ["card_dict.clear(...)"])

    def test_a_different_dictionary_is_not_reported(self):
        # `effect_dict` and `message_dict` are not under this rule. Only the
        # one the digest walks is.
        self.assertEqual(Found("del self.effect_dict[object_id]"), [])

    def test_rebinding_the_whole_dictionary_is_not_reported(self):
        # `ObjectManager.ResetObjects` does this to start a new game, which is
        # not a card leaving one.
        self.assertEqual(Found("self.card_dict = {}"), [])


def Found(source):
    """What `Drops` reports for a snippet. Used to test the scan itself."""
    return [description
            for node in ast.walk(ast.parse(source))
            for description in Drops(node)]


def Drops(node):
    """What `node` does to a `card_dict` entry, if anything."""
    if isinstance(node, ast.Delete):
        for target in node.targets:
            if isinstance(target, ast.Subscript) and IsCardDict(target.value):
                return ["del card_dict[...]"]
        return []

    if isinstance(node, ast.Call) and isinstance(node.func, ast.Attribute):
        if node.func.attr in ("pop", "popitem", "clear"):
            if IsCardDict(node.func.value):
                return [f"card_dict.{node.func.attr}(...)"]
    return []


def IsCardDict(node):
    if isinstance(node, ast.Attribute):
        return node.attr == "card_dict"
    if isinstance(node, ast.Name):
        return node.id == "card_dict"
    return False


class TestDestructionIsGone(unittest.TestCase):
    """The deletion was the decision; leaving it half done is the risk.

    A `Destroy` that comes back on one of these classes without the others is
    the state MARVEL-70 was raised about: a path nothing takes, carrying
    defects nothing can trigger.
    """

    def test_a_card_cannot_be_destroyed(self):
        self.assertFalse(hasattr(Card, "Destroy"))

    def test_a_deck_cannot_be_destroyed(self):
        self.assertFalse(hasattr(Deck2, "Destroy"))

    def test_the_object_manager_has_no_card_removal(self):
        self.assertFalse(hasattr(ObjectManager, "RemoveCard"))


if __name__ == "__main__":
    unittest.main()
