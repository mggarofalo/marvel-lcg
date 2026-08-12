"""The deck builder (MARVEL-80).

The rule this module exists to enforce is that **a built deck is legal or it is
not built**. A deck that breaks the rules does not fail loudly on its own: the
engine shuffles it and plays it, and the corpus entry that comes out describes a
game the rules do not allow while looking exactly like a valid one. So the tests
that matter are the ones proving the builder refuses, not the ones proving it
produces something.

The second thing worth pinning is what the builder *cannot* do. 164 of the 336
unreachable cards are encounter, campaign or factionless -- no player deck can
ever hold them. A builder that silently dropped them would report full coverage
of a target set it had only partly covered, which is the failure mode this repo
has hit three times by other routes.
"""

import json
import os
import unittest

from tools.decks import build, rules

STARTERS = "deck/starter"


def Skip(test):
    if not os.path.isdir(STARTERS):
        test.skipTest("run from py_src/")


class BuilderTestCase(unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        if not os.path.isdir(STARTERS):
            raise unittest.SkipTest("run from py_src/")
        cls.catalogue = rules.Catalogue.Load()


################################################################################
#

class TestRefusal(BuilderTestCase):
    """The guarantee. An illegal deck never leaves the builder."""

    def test_every_built_deck_passes_the_checker(self):
        targets = self.catalogue.AspectCards("justice")[:30]
        for deck in build.BuildDecks(targets, self.catalogue):
            with self.subTest(deck=deck.name):
                self.assertEqual(rules.Check(deck.deck, self.catalogue), [])

    def test_a_deck_that_would_be_illegal_is_raised_not_returned(self):
        """The discriminating test.

        Mixing two aspects is the rule a builder is most likely to break, since
        it fills to 40 from whatever is available. Forcing that mix has to
        raise -- if it merely returned a deck, the corpus would play it.
        """
        starters = build.ReadStarters(STARTERS)
        base_name = build.BaseForAspect(self.catalogue, starters)["justice"]
        base = starters[base_name]

        mixed = (self.catalogue.AspectCards("justice")[:12]
                 + self.catalogue.AspectCards("aggression")[:13])

        with self.assertRaises(build.BuildError) as caught:
            build._BuildOne(self.catalogue, base_name, base, "justice",
                            mixed, 1, rules.MINIMUM_DECK)
        self.assertIn("illegal", str(caught.exception))

    def test_more_targets_than_slots_is_refused(self):
        starters = build.ReadStarters(STARTERS)
        base_name = build.BaseForAspect(self.catalogue, starters)["justice"]
        base = starters[base_name]
        too_many = self.catalogue.AspectCards("justice")[:40]

        with self.assertRaises(build.BuildError):
            build._BuildOne(self.catalogue, base_name, base, "justice",
                            too_many, 1, rules.MINIMUM_DECK)


################################################################################
#

class TestCoverage(BuilderTestCase):
    """What the builder covers, and what it says it cannot."""

    def test_every_buildable_target_lands_in_some_deck(self):
        targets = (self.catalogue.AspectCards("protection")[:30]
                   + self.catalogue.BasicCards()[:10])
        built = build.BuildDecks(targets, self.catalogue)

        carried = {card for deck in built for card in deck.targets}
        self.assertEqual(carried, set(targets))

    def test_a_card_no_deck_can_hold_is_reported_not_dropped(self):
        """Silence here would overstate coverage.

        An encounter card cannot go in a player deck. The builder must not put
        it in one, and must not pretend the target set was covered either.
        """
        encounter = [card_id for card_id, card in self.catalogue.cards.items()
                     if str(card.get("faction") or "").lower() == "encounter"][:3]
        self.assertTrue(encounter, "no encounter cards in the dataset")

        targets = self.catalogue.AspectCards("justice")[:5] + encounter
        skipped = build.Unbuildable(targets, self.catalogue)

        self.assertEqual(sorted(skipped), sorted(encounter))
        built = build.BuildDecks(targets, self.catalogue)
        carried = {card for deck in built for card in deck.targets}
        self.assertEqual(carried & set(encounter), set())

    def test_targets_spill_into_a_second_deck_rather_than_being_lost(self):
        # One deck holds 25 player-deck cards. 30 targets need two.
        targets = self.catalogue.AspectCards("leadership")[:30]
        built = build.BuildDecks(targets, self.catalogue)

        self.assertGreater(len(built), 1)
        carried = [card for deck in built for card in deck.targets]
        self.assertEqual(sorted(carried), sorted(targets))
        self.assertEqual(len(carried), len(set(carried)),
                         "a target was carried twice, wasting a slot")


################################################################################
#

class TestShape(BuilderTestCase):
    """A built deck has to look like a deck the engine can load."""

    def Build(self):
        return build.BuildDecks(self.catalogue.AspectCards("justice")[:10],
                                self.catalogue)[0]

    def test_it_reaches_the_minimum_deck_size(self):
        self.assertEqual(self.Build().size, rules.MINIMUM_DECK)

    def test_the_identity_and_nemesis_set_come_from_the_base_deck(self):
        """The parts a player does not choose are inherited, not synthesised.

        Getting the nemesis set or obligations wrong produces a deck that loads
        and plays while describing a game the rules do not allow -- and unlike
        an aspect violation, nothing checks it.
        """
        built = self.Build()
        base = build.ReadStarters(STARTERS)[built.base]
        for key in ("hero", "obligations", "nemesis_set", "set_aside",
                    "hero_deck"):
            with self.subTest(key=key):
                self.assertEqual(built.deck.get(key), base.get(key))

    def test_only_the_player_deck_is_generated(self):
        built = self.Build()
        base = build.ReadStarters(STARTERS)[built.base]
        self.assertNotEqual(built.deck["player_deck"], base["player_deck"])

    def test_it_records_where_it_came_from(self):
        # A generated deck in a corpus is evidence. It has to say what produced
        # it and what it was produced for, or the corpus cannot be explained
        # later.
        generated = self.Build().deck["metadata"]["generated"]
        self.assertEqual(generated["tool"], "tools.decks.build")
        self.assertEqual(generated["issue"], "MARVEL-80")
        self.assertTrue(generated["targets"])

    def test_two_builds_of_the_same_targets_agree(self):
        targets = self.catalogue.AspectCards("aggression")[:8]
        first = build.BuildDecks(targets, self.catalogue)
        again = build.BuildDecks(targets, self.catalogue)
        self.assertEqual([d.deck for d in first], [d.deck for d in again])


################################################################################
#

class TestBaseSelection(BuilderTestCase):

    def test_a_hero_with_a_printed_exception_is_not_used_as_a_base(self):
        """Building on Adam Warlock or Spider-Woman is legal but not simple.

        Warlock caps every non-Warlock card at 1 copy and Spider-Woman needs two
        aspects at equal size, so a deck built on either has to satisfy that as
        well. Those are fine decks; they are not the ones a builder should
        produce by accident.
        """
        starters = build.ReadStarters(STARTERS)
        for aspect, name in build.BaseForAspect(self.catalogue, starters).items():
            with self.subTest(aspect=aspect):
                hero_set = rules.HeroSet(self.catalogue, starters[name])
                self.assertNotIn(hero_set, rules.DECKBUILDING)

    def test_a_base_is_found_for_every_buildable_aspect(self):
        starters = build.ReadStarters(STARTERS)
        bases = build.BaseForAspect(self.catalogue, starters)
        for aspect in build.BUILDABLE_ASPECTS:
            with self.subTest(aspect=aspect):
                self.assertIn(aspect, bases)


################################################################################
#

class TestTheGeneratedDecksOnDisk(BuilderTestCase):
    """The decks actually checked in, if any."""

    FOLDER = build.DEFAULT_OUTPUT

    def setUp(self):
        if not os.path.isdir(self.FOLDER):
            self.skipTest(f"{self.FOLDER} not present")

    def test_every_generated_deck_on_disk_is_legal(self):
        rows = rules.CheckFolder(self.FOLDER, self.catalogue)
        self.assertTrue(rows, "no generated decks found")
        illegal = {path: violations for path, violations in rows if violations}
        self.assertEqual(illegal, {})

    def test_they_carry_targets_no_starter_deck_names(self):
        """Otherwise they are 40 cards of nothing new.

        The point of a generated deck is the cards it adds. If every target were
        already in a starter deck, the builder would have run and changed
        nothing while reporting success.
        """
        starter_cards = set()
        for deck in build.ReadStarters(STARTERS).values():
            for key in rules.DECK_FIELDS:
                starter_cards |= {str(x) for x in (deck.get(key) or [])}

        targets = set()
        for _path, _violations in rules.CheckFolder(self.FOLDER, self.catalogue):
            pass
        for name in sorted(os.listdir(self.FOLDER)):
            if not name.endswith(".json"):
                continue
            deck = rules.ReadDeck(os.path.join(self.FOLDER, name))
            generated = (deck.get("metadata") or {}).get("generated") or {}
            targets |= set(generated.get("targets") or [])

        self.assertTrue(targets, "generated decks record no targets")
        self.assertEqual(targets & starter_cards, set(),
                         "a generated deck targets a card a starter already has")
