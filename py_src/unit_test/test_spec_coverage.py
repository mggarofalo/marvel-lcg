"""Tests for the behavioral-spec coverage report (MARVEL-68).

The thing under test is mostly the *denominator*. A coverage number is only
worth reading if the population it divides by is right, and the failure mode is
silent: a card wrongly excluded does not look like a bug, it looks like a
smaller universe. That is exactly how MARVEL-16 shipped 71% when the truth was
91%, so the tier rule gets a negative control rather than only a positive one.
"""

import json
import os
import unittest

from tools.spec.coverage import (
    SPECIFIABLE, TIERS, TIER_ORDER, Coverage, Tier)

CARD_DATASET = "../datasets/cards/cards.json"


def MakeCard(card_id="01001", name="A card", pack="core", in_engine=True,
             attributes=None, script=None, **extra):
    engine = {}
    if in_engine:
        engine["pack"] = pack
        engine["attributes"] = attributes if attributes is not None else {"HP": "3"}
        if script is not None:
            engine["script"] = script
    card = {
        "card_id": card_id,
        "name": name,
        "pack": pack,
        "type_name": "Minion",
        "in_engine": in_engine,
        "engine": engine,
    }
    card.update(extra)
    return card


def Script(**overrides):
    script = {"path": f"cards/pack/core/{overrides.pop('cid', '01001')}.py",
              "lines": 20, "has_imperative_handler": False,
              "player_choice_calls": [], "ability_factories": []}
    script.update(overrides)
    return script


################################################################################
#

class TestTierRule(unittest.TestCase):

    def test_a_card_that_asks_the_player_something_is_interactive(self):
        card = MakeCard(script=Script(has_imperative_handler=True,
                                      player_choice_calls=["ChooseAbilities"]))
        self.assertEqual(Tier(card), "interactive")

    def test_a_handler_that_never_asks_is_imperative(self):
        card = MakeCard(script=Script(has_imperative_handler=True))
        self.assertEqual(Tier(card), "imperative")

    def test_a_script_with_no_handler_is_declarative(self):
        self.assertEqual(Tier(MakeCard(script=Script())), "declarative")

    def test_a_card_the_engine_has_but_does_not_script_is_still_specifiable(self):
        """The bug this rule shipped with, as a test.

        Hydra Mercenary and Sandman have no card script -- their Guard and
        Toughness come from `game/card/face/attribute/`, which the engine
        applies to every card that prints them. The first version of this rule
        read "no script" as "nothing to specify" and dropped 563 cards out of
        the denominator, including two the suite already had specs for.
        """
        card = MakeCard(script=None, attributes={"HP": "3", "ATK": "1", "Guard": "1"})
        self.assertEqual(Tier(card), "stats_only")
        self.assertIn("stats_only", SPECIFIABLE)

    def test_a_card_the_engine_does_not_have_is_absent(self):
        # A scenario cannot name a card the engine has never heard of, so this
        # is the one population genuinely outside the campaign.
        self.assertEqual(Tier(MakeCard(in_engine=False, script=None)), "absent")
        self.assertNotIn("absent", SPECIFIABLE)

    def test_every_tier_has_a_planned_depth_and_a_reason(self):
        self.assertEqual(sorted(TIERS), sorted(TIER_ORDER))
        for tier in TIER_ORDER:
            budget, why = TIERS[tier]
            with self.subTest(tier=tier):
                self.assertGreaterEqual(budget, 0)
                self.assertTrue(why.strip(), "a tier needs a stated reason")
        # `absent` is the only tier that plans no scenarios; if another one ever
        # does, the campaign has quietly stopped covering something.
        zero = [t for t in TIER_ORDER if TIERS[t][0] == 0]
        self.assertEqual(zero, ["absent"])


################################################################################
#

class TestCoverageJoin(unittest.TestCase):

    def Build(self, tagged=None, trusted=(), quarantined=()):
        cards = [
            MakeCard("01001", "Asks", script=Script(
                has_imperative_handler=True, player_choice_calls=["ChooseAbilities"])),
            MakeCard("01002", "Plain", script=Script()),
            MakeCard("01003", "No script", script=None),
            MakeCard("01004", "Not in the engine", in_engine=False, script=None),
        ]
        return Coverage(cards, tagged or {}, trusted, quarantined)

    def test_only_a_trusted_scenario_is_coverage(self):
        """A quarantined scenario is a claim that failed.

        Counting it would make the number go up when authoring goes wrong,
        which is the one direction a coverage metric must never move.
        """
        coverage = self.Build(
            tagged={"F :: passes": ["01001"], "F :: fails": ["01002"]},
            trusted=["F :: passes"], quarantined=["F :: fails"])
        self.assertTrue(coverage.Covered("01001"))
        self.assertFalse(coverage.Covered("01002"))
        self.assertEqual(coverage.ToDict()["totals"]["covered"], 1)
        self.assertEqual(coverage.ToDict()["totals"]["quarantined"], 1)

    def test_the_denominator_excludes_only_absent_cards(self):
        coverage = self.Build()
        self.assertEqual(sorted(coverage.Specifiable()),
                         ["01001", "01002", "01003"])

    def test_a_tag_naming_no_card_is_reported_not_counted(self):
        # A typo in a `@card:` tag would otherwise be invisible: the scenario
        # passes, and the card it meant to cover stays uncovered forever.
        coverage = self.Build(tagged={"F :: typo": ["09999"]},
                              trusted=["F :: typo"])
        self.assertEqual(coverage.unknown_tags["F :: typo"], ["09999"])
        self.assertEqual(coverage.ToDict()["totals"]["covered"], 0)

    def test_uncovered_leads_with_the_deepest_cards(self):
        coverage = self.Build()
        rows = coverage.Uncovered()
        self.assertEqual(rows[0]["card_id"], "01001")
        self.assertEqual(rows[0]["tier"], "interactive")
        self.assertNotIn("01004", [row["card_id"] for row in rows])

    def test_filters_narrow_without_changing_the_totals(self):
        coverage = self.Build()
        self.assertEqual([r["card_id"] for r in coverage.Uncovered(tier="declarative")],
                         ["01002"])
        self.assertEqual(coverage.ToDict()["totals"]["specifiable"], 3)


################################################################################
#

class TestAgainstTheRealDataset(unittest.TestCase):
    """The rule has to survive the actual 4,344 cards, not just fixtures."""

    def setUp(self):
        if not os.path.exists(CARD_DATASET):
            self.skipTest("run from py_src/")
        with open(CARD_DATASET, "r", encoding="utf-8") as handle:
            self.cards = json.load(handle)["cards"]

    def test_every_card_lands_in_exactly_one_known_tier(self):
        for card in self.cards:
            with self.subTest(card=card["card_id"]):
                self.assertIn(Tier(card), TIER_ORDER)

    def test_no_card_with_a_script_is_called_absent(self):
        """A scripted card is behaviour somebody wrote; it is always in scope.

        This is the check that would have caught the tier bug from the other
        side, and it stays because `absent` is the only tier that removes cards
        from the campaign -- anything that lands there needs a reason.
        """
        wrong = [card["card_id"] for card in self.cards
                 if (card.get("engine") or {}).get("script") and Tier(card) == "absent"]
        self.assertEqual(wrong, [])

    def test_the_specifiable_population_is_larger_than_the_scripted_one(self):
        """The regression guard for the denominator.

        3,781 cards have a script and 3,996 are specifiable; the difference is
        the `stats_only` tier. If these ever come out equal, the rule has
        collapsed back to "no script means nothing to specify".
        """
        scripted = [c for c in self.cards if (c.get("engine") or {}).get("script")]
        specifiable = [c for c in self.cards if Tier(c) in SPECIFIABLE]
        self.assertGreater(len(specifiable), len(scripted))


################################################################################
#

class TestDepthIsReportedBesideCoverage(unittest.TestCase):
    """MARVEL-87: one scenario on an interactive card is not the job done.

    `Covered` answers "has anyone looked at this card", which is the right
    question for the campaign's headline. It is the wrong question for "is this
    card finished", and under delegation the difference is the whole risk: 479
    thin interactive scenarios would report that tier fully covered against a
    plan of 1,916, and no number in the report would disagree.
    """

    def Coverage(self, tier, scenarios):
        script = {"interactive": Script(player_choice_calls=["ChooseAbilities"]),
                  "imperative": Script(has_imperative_handler=True),
                  "declarative": Script()}[tier]
        cards = [MakeCard(card_id="X1", script=script)]
        tagged = {f"case {n}": ["X1"] for n in range(scenarios)}
        return Coverage(cards, tagged, trusted=list(tagged), quarantined=[])

    def test_one_scenario_covers_an_interactive_card_but_is_not_its_depth(self):
        one = self.Coverage("interactive", 1)
        self.assertTrue(one.Covered("X1"))
        self.assertFalse(one.AtDepth("X1"))

    def test_the_planned_number_reaches_depth(self):
        full = self.Coverage("interactive", 4)
        self.assertTrue(full.AtDepth("X1"))

    def test_more_than_planned_still_counts(self):
        # The plan is a target, not a gate -- spec-campaign.md is explicit that
        # a card needing five gets five.
        self.assertTrue(self.Coverage("interactive", 5).AtDepth("X1"))

    def test_an_uncovered_card_is_not_at_depth(self):
        self.assertFalse(self.Coverage("interactive", 0).AtDepth("X1"))

    def test_depth_is_carried_in_the_totals_and_per_tier(self):
        data = self.Coverage("interactive", 1).ToDict()
        self.assertEqual(data["totals"]["covered"], 1)
        self.assertEqual(data["totals"]["at_depth"], 0)
        self.assertEqual(data["by_tier"]["interactive"]["at_depth"], 0)


class TestTheDepthGapIsListable(unittest.TestCase):
    """The gap between `covered` and `at depth` had no names in it.

    The tier table has printed both numbers since MARVEL-87 and `Uncovered`
    lists the cards behind one of them. The cards behind the other -- looked at
    once and reported done -- could only be counted, never read, so the one
    thing the campaign most needed to act on was the one thing it could not
    enumerate.
    """

    SCRIPTS = {"interactive": lambda: Script(player_choice_calls=["ChooseAbilities"]),
               "imperative": lambda: Script(has_imperative_handler=True),
               "declarative": Script}

    def Coverage(self, counts, tier="interactive"):
        """counts: {card_id: how many trusted scenarios tag it}."""
        cards = [MakeCard(card_id=card_id, script=self.SCRIPTS[tier]())
                 for card_id in counts]
        tagged = {f"{card_id} case {n}": [card_id]
                  for card_id, total in counts.items() for n in range(total)}
        return Coverage(cards, tagged, trusted=list(tagged), quarantined=[])

    def test_a_card_short_of_its_plan_is_listed(self):
        rows = self.Coverage({"X1": 1}).Shallow()
        self.assertEqual([r["card_id"] for r in rows], ["X1"])
        self.assertEqual(rows[0]["scenarios"], 1)
        self.assertEqual(rows[0]["planned"], 4)
        self.assertEqual(rows[0]["short"], 3)

    def test_a_card_at_depth_is_not_listed(self):
        self.assertEqual(self.Coverage({"X1": 4}).Shallow(), [])

    def test_an_uncovered_card_is_not_listed(self):
        # It belongs to `Uncovered`. A card in both lists would be counted
        # twice by anyone adding the two to size the campaign.
        shallow = self.Coverage({"X1": 0}).Shallow()
        self.assertEqual(shallow, [])

    def test_the_two_lists_partition_the_specifiable_population(self):
        coverage = self.Coverage({"X1": 0, "X2": 1, "X3": 4})
        names = lambda rows: sorted(r["card_id"] for r in rows)
        self.assertEqual(names(coverage.Uncovered()), ["X1"])
        self.assertEqual(names(coverage.Shallow()), ["X2"])
        # X3 is in neither, and that is the only card that should be.
        self.assertEqual(len(coverage.Uncovered()) + len(coverage.Shallow()) + 1,
                         len(coverage.Specifiable()))

    def test_the_biggest_shortfall_leads(self):
        rows = self.Coverage({"X1": 3, "X2": 1, "X3": 2}).Shallow()
        self.assertEqual([r["card_id"] for r in rows], ["X2", "X3", "X1"])

    def test_the_shortfall_is_measured_against_the_card_s_own_tier(self):
        # An imperative card is planned for two, so one scenario leaves it one
        # short -- not three. Reading the plan from a fixed number instead of
        # from the card's tier passes every interactive case and is wrong
        # everywhere else, which is exactly the shape that survives a test
        # suite built out of one tier.
        rows = self.Coverage({"X1": 1}, tier="imperative").Shallow()
        self.assertEqual(rows[0]["planned"], 2)
        self.assertEqual(rows[0]["short"], 1)

    def test_a_one_scenario_tier_is_never_short(self):
        # declarative plans for one, so its covered set and its at-depth set
        # are the same set and nothing can sit between them.
        self.assertEqual(self.Coverage({"X1": 1}, tier="declarative").Shallow(), [])

    def test_the_listed_count_matches_the_gap_the_tier_table_prints(self):
        coverage = self.Coverage({"X1": 0, "X2": 1, "X3": 4})
        row = coverage.ToDict()["by_tier"]["interactive"]
        self.assertEqual(len(coverage.Shallow()),
                         row["covered"] - row["at_depth"])
