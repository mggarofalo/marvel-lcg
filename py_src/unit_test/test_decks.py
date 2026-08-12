"""Tests for the deckbuilding rules (MARVEL-80, MARVEL-85).

The rule this module exists to enforce is that **an illegal deck must fail
loudly**. A deck that breaks the rules still plays: the engine shuffles it and
produces a scene, and that scene describes a game the rules do not allow while
looking exactly like a valid one. So the tests that matter are the ones that
prove the checker can reject, not the ones that prove it can accept.

That applies twice over to the printed deck-building exceptions. Each one lets
*some* cards in from an aspect the deck did not choose, and an allowance that is
too wide is invisible: the decks it wrongly admits all pass. So every exception
here is tested with a negative control -- a card that looks like the allowance
from a distance and must still be rejected.
"""

import json
import os
import unittest

from tools.decks.rules import (
    ASPECTS, DECKBUILDING, DECKBUILDING_LINES, AspectAllowance, Catalogue,
    Check, DeckError, ReadDeck, Rule, Validate)

CARD_DATASET = "../datasets/cards/cards.json"


def Card(card_id, faction, *, card_set="", limit=3, card_type="event",
         traits=(), energy=0):
    """One printed record, with only the fields the rules read."""
    return {"card_id": card_id, "name": f"Card {card_id}", "faction": faction,
            "set": card_set, "deck_limit": limit, "type": card_type,
            "traits": list(traits),
            "stats": {"resource_energy": energy} if energy else {}}


def MakeCatalogue(*records, **cards):
    """A tiny dataset.

    Keyword values are `(faction, set, limit)` triples for the plain cards the
    rejection tests use; positional values are full records from `Card`.
    """
    built = {record["card_id"]: record for record in records}
    for card_id, (faction, card_set, limit) in cards.items():
        built[card_id] = Card(card_id, faction, card_set=card_set, limit=limit)
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

    def test_the_off_aspect_card_is_named(self):
        """Not just the count.

        The old message said "cards from 2 aspects" and left the reader to
        diff the deck against the dataset by hand. Every violation names its
        card, so the fix is mechanical.
        """
        deck = MakeDeck(player_deck=JUSTICE[:20] + ["aggr1"] * 3 + JUSTICE[20:36])
        aspect = [v for v in Check(deck, CATALOGUE, minimum=40)
                  if v.rule == "aspect"]
        self.assertEqual(len(aspect), 1)
        self.assertIn("aggr1", aspect[0].detail)
        self.assertIn("3 x", aspect[0].detail)

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
# The printed exceptions.
#
# One catalogue serves all seven. Each identity is `id_<set>`; `id_plain` has no
# printed line and is the control that every allowance is checked against, since
# an allowance that leaked to every hero would pass all of its own tests.

IDENTITIES = ("plain", "spider_woman", "warlock", "cyclops", "cable", "gam",
              "maria_hill", "wonder_man")

# 45 titles per aspect, which is enough for a 40-card deck at one copy each.
FILLER = [Card(f"{aspect[:3]}{i}", aspect, limit=3)
          for aspect in ASPECTS for i in range(45)]

EXCEPTIONS = MakeCatalogue(
    *[Card(f"id_{s}", "hero", card_set=s, limit=1, card_type="hero")
      for s in IDENTITIES],
    *FILLER,
    # Adam Warlock's own cards, which his copy cap spares.
    Card("warlock_own", "hero", card_set="warlock", limit=2, card_type="event"),

    # Cyclops: X-Men *allies*, and the near misses.
    Card("xmen_ally", "aggression", card_type="ally", traits=["X-Men"]),
    Card("xmen_event", "aggression", card_type="event", traits=["X-Men"]),
    Card("plain_ally", "aggression", card_type="ally"),

    # Cable: player side schemes.
    Card("side_scheme", "aggression", card_type="player_side_scheme"),

    # Gamora: attack and/or thwart *events*, at most 6.
    Card("attack_event", "aggression", card_type="event", traits=["Attack"]),
    Card("thwart_event", "justice", card_type="event", traits=["Thwart"]),
    Card("thwart_event2", "protection", card_type="event", traits=["Thwart"]),
    Card("attack_ally", "aggression", card_type="ally", traits=["Attack"]),

    # Maria Hill: 3 S.H.I.E.L.D. *supports*, each at its full copy count.
    *[Card(f"shield{i}", aspect, card_type="support", traits=["S.H.I.E.L.D."])
      for i, aspect in enumerate(("aggression", "justice", "protection",
                                  "aggression"))],
    Card("shield_ally", "aggression", card_type="ally",
         traits=["S.H.I.E.L.D."]),

    # Wonder Man: events with a printed [energy] resource icon.
    Card("energy_event", "aggression", card_type="event", energy=1),
    Card("energy_upgrade", "aggression", card_type="upgrade", energy=1),
    Card("plain_event", "aggression", card_type="event"),
)


def Deck(hero_set, *off, aspect="leadership", size=40):
    """A `size`-card deck in one aspect for `hero_set`, plus `off`.

    The filler is one copy of each of `size - len(off)` distinct titles, so
    nothing in a test trips the copy limit by accident.
    """
    filler = [f"{aspect[:3]}{i}" for i in range(size - len(off))]
    return {"hero": [f"id_{hero_set}"], "hero_deck": [],
            "player_deck": filler + list(off)}


def Broken(deck):
    return sorted({v.rule for v in Check(deck, EXCEPTIONS, minimum=40)})


class TestAspectCountExceptions(unittest.TestCase):
    """Spider-Woman and Adam Warlock: more than one aspect, in equal parts."""

    def test_the_default_is_one_aspect(self):
        self.assertEqual(AspectAllowance("spider_man"), 1)

    def test_the_printed_aspect_counts_are_honoured(self):
        self.assertEqual(AspectAllowance("spider_woman"), 2)
        self.assertEqual(AspectAllowance("warlock"), 4)

    def SplitDeck(self, hero_set, counts):
        """A deck holding `counts[aspect]` cards of each named aspect."""
        cards = []
        for aspect, count in counts.items():
            cards.extend(f"{aspect[:3]}{i}" for i in range(count))
        return {"hero": [f"id_{hero_set}"], "hero_deck": [],
                "player_deck": cards}

    def test_spider_woman_may_take_two_aspects(self):
        deck = self.SplitDeck("spider_woman",
                              {"aggression": 20, "justice": 20})
        self.assertEqual(Check(deck, EXCEPTIONS, minimum=40), [])

    def test_spider_womans_two_aspects_must_be_equal(self):
        deck = self.SplitDeck("spider_woman",
                              {"aggression": 22, "justice": 18})
        self.assertEqual(Broken(deck), ["balance"])

    def test_spider_woman_may_not_take_a_third_aspect(self):
        deck = self.SplitDeck("spider_woman",
                              {"aggression": 19, "justice": 19,
                               "leadership": 2})
        self.assertIn("aspect", Broken(deck))

    def test_the_two_aspect_allowance_does_not_leak(self):
        deck = self.SplitDeck("plain", {"aggression": 20, "justice": 20})
        self.assertEqual(Broken(deck), ["aspect"])

    def test_adam_warlock_takes_four_equal_aspects(self):
        deck = self.SplitDeck("warlock", {a: 10 for a in ASPECTS})
        self.assertEqual(Check(deck, EXCEPTIONS, minimum=40), [])

    def test_adam_warlocks_four_aspects_must_be_equal(self):
        deck = self.SplitDeck("warlock", dict(zip(ASPECTS, (13, 9, 9, 9))))
        self.assertEqual(Broken(deck), ["balance"])

    def test_adam_warlock_caps_every_other_card_at_one_copy(self):
        deck = self.SplitDeck("warlock", {a: 10 for a in ASPECTS})
        deck["player_deck"][0] = deck["player_deck"][1]   # a second copy
        violations = [v for v in Check(deck, EXCEPTIONS, minimum=40)
                      if v.rule == "copies"]
        self.assertEqual(len(violations), 1)
        self.assertIn("caps every card that is not warlock's",
                      violations[0].detail)

    def test_adam_warlocks_own_cards_keep_their_printed_limit(self):
        deck = self.SplitDeck("warlock", {a: 10 for a in ASPECTS})
        deck["hero_deck"] = ["warlock_own", "warlock_own"]
        self.assertEqual(Check(deck, EXCEPTIONS, minimum=40), [])

    def test_the_copy_cap_does_not_leak(self):
        """Nobody else is held to one copy of everything."""
        deck = Deck("plain")
        deck["player_deck"][0] = deck["player_deck"][1]
        self.assertNotIn("copies", Broken(deck))


class TestScopedAllowances(unittest.TestCase):
    """The five identities that let particular cards in from other aspects.

    Every one of these is paired with a negative control. An allowance that
    matched on type alone, or on trait alone, would pass the positive test of
    each of the five and quietly legalise hundreds of decks.
    """

    # -- Cyclops: X-MEN allies ----------------------------------------------

    def test_cyclops_may_include_an_xmen_ally_from_another_aspect(self):
        self.assertEqual(Check(Deck("cyclops", "xmen_ally"), EXCEPTIONS,
                               minimum=40), [])

    def test_cyclops_may_include_any_number_of_them(self):
        """The printed line has no cap, so three from three aspects is fine."""
        deck = Deck("cyclops", "xmen_ally", "xmen_ally", "xmen_ally")
        self.assertEqual(Check(deck, EXCEPTIONS, minimum=40), [])

    def test_cyclops_may_not_include_an_off_aspect_xmen_event(self):
        """The negative control. The allowance is allies, not the trait."""
        self.assertEqual(Broken(Deck("cyclops", "xmen_event")), ["aspect"])

    def test_cyclops_may_not_include_an_off_aspect_non_xmen_ally(self):
        self.assertEqual(Broken(Deck("cyclops", "plain_ally")), ["aspect"])

    def test_the_xmen_allowance_does_not_leak(self):
        self.assertEqual(Broken(Deck("plain", "xmen_ally")), ["aspect"])

    # -- Cable: player side schemes -----------------------------------------

    def test_cable_may_include_a_player_side_scheme_from_another_aspect(self):
        self.assertEqual(Check(Deck("cable", "side_scheme"), EXCEPTIONS,
                               minimum=40), [])

    def test_cable_may_not_include_an_ordinary_off_aspect_card(self):
        self.assertEqual(Broken(Deck("cable", "plain_event")), ["aspect"])

    def test_the_side_scheme_allowance_does_not_leak(self):
        self.assertEqual(Broken(Deck("plain", "side_scheme")), ["aspect"])

    # -- Gamora: up to 6 attack and/or thwart events ------------------------

    def test_gamora_may_include_six_attack_or_thwart_events(self):
        deck = Deck("gam", "attack_event", "attack_event", "attack_event",
                    "thwart_event", "thwart_event", "thwart_event")
        self.assertEqual(Check(deck, EXCEPTIONS, minimum=40), [])

    def test_gamora_may_not_include_a_seventh(self):
        deck = Deck("gam", "attack_event", "attack_event", "attack_event",
                    "thwart_event", "thwart_event", "thwart_event",
                    "thwart_event2")
        self.assertEqual(Broken(deck), ["allowance"])

    def test_gamoras_cap_counts_cards_not_titles(self):
        """Three copies of one title spend three of the six."""
        deck = Deck("gam", "attack_event", "attack_event", "attack_event",
                    "thwart_event", "thwart_event", "thwart_event",
                    "thwart_event2")
        allowance = [v for v in Check(deck, EXCEPTIONS, minimum=40)
                     if v.rule == "allowance"]
        self.assertIn("7 cards", allowance[0].detail)

    def test_gamora_may_not_include_an_off_aspect_attack_ally(self):
        """The negative control. Attack is a trait on more than events."""
        self.assertEqual(Broken(Deck("gam", "attack_ally")), ["aspect"])

    def test_gamoras_own_aspect_does_not_spend_the_allowance(self):
        """"from aspects other than your chosen aspect", printed.

        Her shipped deck is 12 aggression cards, most of them attack events;
        counting those against the 6 would reject it.
        """
        deck = Deck("gam", "agg40", "agg41", aspect="aggression")
        self.assertEqual(Check(deck, EXCEPTIONS, minimum=40), [])

    def test_the_attack_event_allowance_does_not_leak(self):
        self.assertEqual(Broken(Deck("plain", "attack_event")), ["aspect"])

    # -- Maria Hill: 3 S.H.I.E.L.D. supports, at max copies -----------------

    def test_maria_hill_may_include_three_shield_supports(self):
        deck = Deck("maria_hill", "shield0", "shield1", "shield2")
        self.assertEqual(Check(deck, EXCEPTIONS, minimum=40), [])

    def test_maria_hills_cap_counts_titles_not_copies(self):
        """"the maximum number of copies of 3 S.H.I.E.L.D. supports".

        Three titles at three copies each is nine cards and legal; four titles
        at one copy each is four cards and is not.
        """
        legal = Deck("maria_hill", *(["shield0"] * 3 + ["shield1"] * 3 +
                                     ["shield2"] * 3))
        self.assertEqual(Check(legal, EXCEPTIONS, minimum=40), [])
        deck = Deck("maria_hill", "shield0", "shield1", "shield2", "shield3")
        self.assertEqual(Broken(deck), ["allowance"])

    def test_maria_hill_may_not_include_an_off_aspect_shield_ally(self):
        """The negative control. The allowance is supports, not the trait."""
        self.assertEqual(Broken(Deck("maria_hill", "shield_ally")), ["aspect"])

    def test_the_shield_support_allowance_does_not_leak(self):
        self.assertEqual(Broken(Deck("plain", "shield0")), ["aspect"])

    # -- Wonder Man: events with a printed [energy] resource icon -----------

    def test_wonder_man_may_include_an_off_aspect_energy_event(self):
        self.assertEqual(Check(Deck("wonder_man", "energy_event"), EXCEPTIONS,
                               minimum=40), [])

    def test_wonder_man_may_not_include_an_off_aspect_energy_upgrade(self):
        """The negative control. The allowance is events, not the icon."""
        self.assertEqual(Broken(Deck("wonder_man", "energy_upgrade")),
                         ["aspect"])

    def test_wonder_man_may_not_include_an_off_aspect_event_without_one(self):
        self.assertEqual(Broken(Deck("wonder_man", "plain_event")), ["aspect"])

    def test_the_energy_event_allowance_does_not_leak(self):
        self.assertEqual(Broken(Deck("plain", "energy_event")), ["aspect"])


class TestTheTableItself(unittest.TestCase):

    def test_every_identity_names_the_card_its_rule_is_printed_on(self):
        for hero_set, rule in DECKBUILDING.items():
            self.assertTrue(rule.card_id, hero_set)
            self.assertTrue(rule.printed, hero_set)

    def test_gamoras_set_is_gam(self):
        """Not `gamora`, which is what every other hero's set looks like.

        A table keyed on the wrong string fails open -- her deck would be
        checked as an ordinary one-aspect deck -- so it is pinned here.
        """
        self.assertIn("gam", DECKBUILDING)
        self.assertNotIn("gamora", DECKBUILDING)

    def test_an_unlisted_hero_gets_the_default(self):
        rule = Rule("spider_man")
        self.assertEqual(rule.aspects, 1)
        self.assertEqual(rule.allowances, ())
        self.assertIsNone(rule.copy_cap)

    def test_every_printed_deckbuilding_line_is_in_the_table(self):
        """The guard on a hand-written table.

        Seven identities in the dataset print a deck-building line and all
        seven are listed. An eighth would silently be checked as an ordinary
        one-aspect deck, and its decks would be rejected as illegal while being
        legal -- so a new one fails here instead. The phrasings are the union of
        what the seven use; a line that uses none of them is not reachable by
        any grep, which is the limit of this guard and the reason the table is
        written out rather than parsed.
        """
        if not os.path.exists(CARD_DATASET):
            self.skipTest("run from py_src/")
        with open(CARD_DATASET, "r", encoding="utf-8") as handle:
            cards = json.load(handle)["cards"]

        printed = {str(card.get("set") or "") for card in cards
                   if card.get("type") in ("hero", "alter_ego")
                   and any(line in (card.get("text_plain") or "").lower()
                           for line in DECKBUILDING_LINES)}
        self.assertEqual(printed, set(DECKBUILDING),
                         "an identity prints a deck-building rule that "
                         "DECKBUILDING does not list")

    def test_the_dataset_still_carries_the_traits_the_table_names(self):
        """The predicates match strings, so a dataset change can empty them.

        `S.H.I.E.L.D.` is the one that has already gone missing: the trait
        splitter cut on every period and shredded it into `S`, `H`, `I`, `E`,
        `L`, `D` on 114 cards, which made Maria Hill's allowance match nothing
        at all and pass every test that only checked rejection.
        """
        if not os.path.exists(CARD_DATASET):
            self.skipTest("run from py_src/")
        with open(CARD_DATASET, "r", encoding="utf-8") as handle:
            cards = json.load(handle)["cards"]

        for trait, card_type in (("X-Men", "ally"), ("S.H.I.E.L.D.", "support"),
                                 ("Attack", "event"), ("Thwart", "event")):
            found = [c for c in cards if c.get("type") == card_type
                     and trait in (c.get("traits") or [])]
            self.assertTrue(found, f"no {card_type} carries the trait {trait}")

        energy = [c for c in cards if c.get("type") == "event"
                  and (c.get("stats") or {}).get("resource_energy")]
        self.assertTrue(energy, "no event has a printed energy resource")
        schemes = [c for c in cards if c.get("type") == "player_side_scheme"]
        self.assertTrue(schemes, "no card is a player side scheme")


################################################################################
#

class TestAgainstTheShippedDecks(unittest.TestCase):
    """The 63 starter decks the corpus is generated from.

    All 63 are legal. Six were not before MARVEL-85: four of them -- Cable,
    Cyclops, Gamora and Maria Hill -- were legal all along and the checker was
    wrong, and two, Spider-Woman and Jubilee, were genuinely illegal and were
    fixed. This pins the set so it cannot drift in either direction unnoticed.
    """

    KNOWN_ILLEGAL: set = set()

    def Illegal(self):
        from tools.decks.rules import CheckFolder

        rows = CheckFolder("deck/starter")
        self.assertGreater(len(rows), 50, "the starter decks did not load")
        return {os.path.basename(path) for path, violations in rows
                if violations}

    def test_the_illegal_starter_decks_are_exactly_the_known_ones(self):
        if not os.path.isdir("deck/starter"):
            self.skipTest("run from py_src/")
        self.assertEqual(self.Illegal(), self.KNOWN_ILLEGAL)

    def test_a_broken_shipped_deck_is_still_caught(self):
        """`KNOWN_ILLEGAL` is empty, so the test above also passes if the
        checker has gone blind. Break a real deck and prove it has not."""
        if not os.path.isdir("deck/starter"):
            self.skipTest("run from py_src/")
        deck = ReadDeck("deck/starter/jubilee.json")
        deck["player_deck"].append("47022")     # a second Unlikely Duo, max 1
        self.assertEqual([v.rule for v in Check(deck)], ["copies"])

    def test_spider_womans_two_aspects_are_still_equal(self):
        """What the MARVEL-85 substitution was for.

        Her deck is the one shipped deck the balance rule bites on, and the
        four cards swapped in were chosen to hold it at 15 and 15.
        """
        if not os.path.isdir("deck/starter"):
            self.skipTest("run from py_src/")
        catalogue = Catalogue.Load()
        deck = ReadDeck("deck/starter/spider_woman.json")
        cards = deck["hero_deck"] + deck["player_deck"]
        self.assertEqual(len(cards), 40)
        sizes = {aspect: sum(1 for c in cards
                             if catalogue.Faction(c) == aspect)
                 for aspect in ("aggression", "justice")}
        self.assertEqual(sizes, {"aggression": 15, "justice": 15})
