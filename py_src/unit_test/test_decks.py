"""Tests for the deckbuilding rules (MARVEL-80).

The rule this module exists to enforce is that **an illegal deck must fail
loudly**. A deck that breaks the rules still plays: the engine shuffles it and
produces a scene, and that scene describes a game the rules do not allow while
looking exactly like a valid one. So the tests that matter are the ones that
prove the checker can reject, not the ones that prove it can accept.
"""

import json
import os
import unittest

from tools.decks.rules import (
    ASPECT_EXCEPTIONS, ASPECTS, DECKBUILDING_LINE, AspectAllowance, Catalogue,
    Check, DeckError, Validate)

CARD_DATASET = "../datasets/cards/cards.json"


def MakeCatalogue(**cards):
    """A tiny dataset. Keys are card ids, values are (faction, set, limit)."""
    built = {}
    for card_id, (faction, card_set, limit) in cards.items():
        built[card_id] = {"card_id": card_id, "name": f"Card {card_id}",
                          "faction": faction, "set": card_set,
                          "deck_limit": limit, "type": "event"}
    return Catalogue(built)


FIXTURE = {
    "ident": ("hero", "spidey", 1),
    "heroA": ("hero", "spidey", 3),
    "heroB": ("hero", "other", 3),
    "aggr1": ("aggression", "", 3),
    "basic1": ("basic", "", 3),
    "once": ("basic", "", 1),
    "enc1": ("encounter", "", None),
}
# 50 distinct justice cards, so a deck can reach the 40-card minimum without
# tripping the copy limit -- which the first version of this fixture did, and
# which is why `test_a_legal_deck_has_no_violations` is worth having.
FIXTURE.update({f"just{i}": ("justice", "", 3) for i in range(50)})

CATALOGUE = MakeCatalogue(**FIXTURE)

JUSTICE = [f"just{i}" for i in range(50)]


def MakeDeck(player_deck=(), hero_deck=("heroA",), hero="ident"):
    return {"hero": [hero], "hero_deck": list(hero_deck),
            "player_deck": list(player_deck)}


def Legal(n=40):
    """A deck long enough to pass the size rule, one aspect, all legal."""
    return MakeDeck(player_deck=JUSTICE[:n - 1], hero_deck=["heroA"])


################################################################################
#

class TestRejection(unittest.TestCase):
    """Each rule, shown failing. An accept-only test proves nothing here."""

    def Rules(self, deck):
        return sorted({v.rule for v in Check(deck, CATALOGUE, minimum=40)})

    def test_a_legal_deck_has_no_violations(self):
        self.assertEqual(Check(Legal(), CATALOGUE, minimum=40), [])

    def test_a_short_deck_is_rejected(self):
        self.assertIn("size", self.Rules(Legal(n=39)))

    def test_two_aspects_are_rejected(self):
        deck = MakeDeck(player_deck=JUSTICE[:20] + ["aggr1"] * 3 + JUSTICE[20:36])
        self.assertIn("aspect", self.Rules(deck))

    def test_another_heros_card_is_rejected(self):
        deck = MakeDeck(player_deck=JUSTICE[:38] + ["heroB"])
        self.assertIn("hero-specific", self.Rules(deck))

    def test_an_encounter_card_is_rejected(self):
        deck = MakeDeck(player_deck=JUSTICE[:38] + ["enc1"])
        self.assertIn("faction", self.Rules(deck))

    def test_exceeding_a_copy_limit_is_rejected(self):
        deck = MakeDeck(player_deck=JUSTICE[:37] + ["once", "once"])
        self.assertIn("copies", self.Rules(deck))

    def test_every_violation_is_reported_at_once(self):
        """Not the first one.

        A generator that has to be re-run per violation gets abandoned, and the
        cost of collecting them all is nothing.
        """
        deck = MakeDeck(player_deck=["just0", "aggr1", "heroB", "enc1", "once", "once"])
        self.assertEqual(self.Rules(deck),
                         ["aspect", "copies", "faction", "hero-specific", "size"])

    def test_validate_raises_and_names_every_rule(self):
        with self.assertRaises(DeckError) as caught:
            Validate(MakeDeck(player_deck=["enc1"]), CATALOGUE, where="generated")
        message = str(caught.exception)
        self.assertIn("generated", message)
        self.assertIn("faction", message)
        self.assertIn("size", message)

    def test_a_deck_with_no_cards_is_rejected(self):
        self.assertEqual(self.Rules(MakeDeck(hero_deck=())), ["empty"])

    def test_a_card_the_dataset_does_not_have_is_an_error(self):
        # Silently skipping an unknown id would let a typo produce a deck that
        # validates and then fails at the engine, which is the wrong end.
        with self.assertRaises(DeckError):
            Check(MakeDeck(player_deck=["nosuchcard"]), CATALOGUE)


################################################################################
#

class TestPrintedExceptions(unittest.TestCase):

    def test_the_default_is_one_aspect(self):
        self.assertEqual(AspectAllowance("spider_man"), 1)

    def test_the_two_printed_exceptions_are_honoured(self):
        self.assertEqual(AspectAllowance("spider_woman"), 2)
        self.assertEqual(AspectAllowance("warlock"), 4)

    def test_every_printed_deckbuilding_line_is_in_the_table(self):
        """The guard on a hand-written table.

        Two identities in the dataset print a deck-building line and both are
        listed. A third would silently be checked under the default of one
        aspect, and its decks would be rejected as illegal while being legal --
        so a new one fails here instead.
        """
        if not os.path.exists(CARD_DATASET):
            self.skipTest("run from py_src/")
        with open(CARD_DATASET, "r", encoding="utf-8") as handle:
            cards = json.load(handle)["cards"]

        printed = {str(card.get("set") or "") for card in cards
                   if card.get("type") in ("hero", "alter_ego")
                   and DECKBUILDING_LINE in (card.get("text_plain") or "").lower()}
        self.assertEqual(printed, set(ASPECT_EXCEPTIONS),
                         "an identity prints a deck-building rule that "
                         "ASPECT_EXCEPTIONS does not list")


################################################################################
#

class TestAgainstTheShippedDecks(unittest.TestCase):
    """The 63 starter decks the corpus is generated from.

    Six of them are illegal, which is a finding rather than a test failure --
    see MARVEL-85. This pins the count so the number cannot drift in either
    direction unnoticed: a deck being fixed is good news that should be
    recorded, and a new illegal deck appearing is not.
    """

    KNOWN_ILLEGAL = {
        "cable.json", "cyclops.json", "gamora.json", "jubilee.json",
        "maria_hill.json", "spider_woman.json",
    }

    def test_the_illegal_starter_decks_are_exactly_the_known_ones(self):
        if not os.path.isdir("deck/starter"):
            self.skipTest("run from py_src/")
        from tools.decks.rules import CheckFolder

        rows = CheckFolder("deck/starter")
        self.assertGreater(len(rows), 50, "the starter decks did not load")
        illegal = {os.path.basename(path)
                   for path, violations in rows if violations}
        self.assertEqual(illegal, self.KNOWN_ILLEGAL)
