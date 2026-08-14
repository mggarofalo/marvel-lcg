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
        # Distinct per card unless a test deliberately makes two cards alike.
        # Left identical, every fixture card would print the same text, run the
        # same default script path and carry the same stats -- which is exactly
        # the condition `Equivalents` credits on, so the whole fixture set would
        # silently collapse into one covered card.
        "text_plain": f"printed text of {card_id}",
    }
    card.update(extra)
    return card


def Script(**overrides):
    script = {"path": f"cards/pack/core/{overrides.pop('cid', '01001')}.py",
              "lines": 20, "has_imperative_handler": False,
              "player_choice_calls": [], "player_choice_helpers": [],
              "ability_factories": []}
    script.update(overrides)
    return script


################################################################################
#

class TestTierRule(unittest.TestCase):

    def test_a_card_that_asks_the_player_something_is_interactive(self):
        card = MakeCard(script=Script(has_imperative_handler=True,
                                      player_choice_calls=["ChooseAbilities"]))
        self.assertEqual(Tier(card), "interactive")

    def test_a_card_that_asks_only_through_a_helper_is_interactive(self):
        # MARVEL-114. The script names no prompt of its own; the question is
        # asked inside `game/operate/`, and the card suspends for it just the
        # same. Reading only `player_choice_calls` filed fourteen of these
        # under "a handler that does something, but never asks".
        card = MakeCard(script=Script(
            has_imperative_handler=True,
            player_choice_helpers=["Search.Collection"]))
        self.assertEqual(Tier(card), "interactive")

    def test_a_handler_that_never_asks_is_imperative(self):
        card = MakeCard(script=Script(has_imperative_handler=True))
        self.assertEqual(Tier(card), "imperative")

    def test_a_declarative_script_that_reaches_a_prompt_is_still_interactive(self):
        # No nested handler, so the shape rule alone would say `declarative`.
        # The tier is about whether a scenario has a decision to transcribe.
        card = MakeCard(script=Script(
            player_choice_helpers=["Utility.PlaceThreatOnOneScheme"]))
        self.assertEqual(Tier(card), "interactive")

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


################################################################################
#

class TestAReprintIsNotSecondCardOfWork(unittest.TestCase):
    """MARVEL-105: 318 cards reprint a card the campaign already counts.

    Every one prints text byte-identical to the card it reprints. 308 of them
    also run the *same* script module, so a scenario for the original is not a
    claim about a different card -- it is the same claim about the same code
    and the same text, and writing it twice buys nothing.

    The other 10 run a script file of their own. No pair is byte-identical and
    six of the ten disagree in behaviour (MARVEL-106), so those are the one
    group where the two ids provably do different things. The credit is earned
    per card by comparing script paths, never assumed from the reprint link --
    which is what these tests pin.
    """

    ORIGINAL = "05014"
    REPRINT = "38015"

    TEXT = "Hero Interrupt (defense): cancel all boost icons on that card."

    def Coverage(self, reprint_path, covered=("05014",), original_path=None,
                 reprint_attributes=None):
        # A reprint prints text byte-identical to its original -- all 318 in the
        # dataset do -- so the fixture has to as well, or it is not testing the
        # thing the rule keys on.
        original = MakeCard(card_id=self.ORIGINAL, text_plain=self.TEXT,
                            script=Script(path=original_path or "a.py"))
        reprint = MakeCard(card_id=self.REPRINT, reprint_of=self.ORIGINAL,
                           text_plain=self.TEXT,
                           attributes=reprint_attributes,
                           script=Script(path=reprint_path))
        tagged = {f"case for {c}": [c] for c in covered}
        return Coverage([original, reprint], tagged,
                        trusted=list(tagged), quarantined=[])

    def test_a_reprint_sharing_its_original_s_script_is_covered_by_it(self):
        coverage = self.Coverage(reprint_path="a.py")
        self.assertTrue(coverage.Covered(self.REPRINT))
        self.assertEqual(coverage.Scenarios(self.REPRINT),
                         ["case for 05014"])

    def test_a_reprint_running_its_own_script_is_not(self):
        # The MARVEL-106 group. Same printed text, different implementation --
        # crediting these is the one thing this must never do.
        coverage = self.Coverage(reprint_path="b.py")
        self.assertFalse(coverage.Covered(self.REPRINT))

    def test_the_credit_does_not_run_backwards(self):
        # Covering the reprint says nothing about the original. The link is
        # directional and only the reprint carries it.
        coverage = self.Coverage(reprint_path="a.py", covered=("38015",))
        self.assertTrue(coverage.Covered(self.REPRINT))
        self.assertFalse(coverage.Covered(self.ORIGINAL))

    def test_a_credited_reprint_is_not_listed_as_work_to_do(self):
        coverage = self.Coverage(reprint_path="a.py")
        self.assertNotIn(self.REPRINT,
                         [r["card_id"] for r in coverage.Uncovered()])

    def test_an_uncredited_reprint_is_still_work_to_do(self):
        coverage = self.Coverage(reprint_path="b.py")
        self.assertIn(self.REPRINT,
                      [r["card_id"] for r in coverage.Uncovered()])

    def test_depth_is_credited_too_or_the_two_columns_disagree(self):
        # Covered and at-depth have to be credited by the same rule. If only
        # one were, a credited reprint would sit in the Shallow listing for a
        # shortfall no scenario could ever close.
        coverage = self.Coverage(reprint_path="a.py")
        self.assertTrue(coverage.AtDepth(self.REPRINT))
        self.assertEqual(coverage.Shallow(), [])

    def test_the_report_says_how_many_it_credited(self):
        summary = self.Coverage(reprint_path="a.py").ToDict()["duplicates"]
        self.assertEqual(summary["credited"], 1)
        self.assertEqual(summary["credited_and_covered"], 1)
        self.assertEqual(summary["not_credited"], [])

    def test_the_report_names_the_ones_it_refused_to_credit(self):
        summary = self.Coverage(reprint_path="b.py").ToDict()["duplicates"]
        self.assertEqual(summary["credited"], 0)
        self.assertEqual(summary["not_credited"], [self.REPRINT])

    def test_a_reprint_of_a_card_the_dataset_does_not_have_is_ignored(self):
        orphan = MakeCard(card_id="99999", reprint_of="00000",
                          script=Script(path="a.py"))
        coverage = Coverage([orphan], {}, trusted=[], quarantined=[])
        self.assertEqual(coverage.credited_to, {})


class TestWhatTheEquivalenceRuleRefusesToCredit(unittest.TestCase):
    """The two populations that look creditable and are not.

    Both were found by measuring rather than by reasoning, and both would have
    been credited by the obvious version of this rule -- "same printed text and
    the same script is the same card". They are the reason the rule reads
    statistics as well as text, and the reason a card with no script at all
    needs a `reprint_of` link rather than a structural match.
    """

    STAGE_TEXT = "Forced Interrupt: When this villain attacks, give him 1 boost."

    def test_two_villain_stages_are_not_one_card(self):
        """34 same-text same-module groups in the dataset are villain stages.

        Same ability text, same script, different HP, ATK and SCH. A scenario
        asserting hit points does not transfer from stage 1 to stage 2, so text
        and code together are not enough to credit on.
        """
        stage_one = MakeCard(card_id="01096a", text_plain=self.STAGE_TEXT,
                             attributes={"HP": "14*", "ATK": "2", "Stage": "1"},
                             script=Script(path="rhino.py"))
        stage_two = MakeCard(card_id="01096b", text_plain=self.STAGE_TEXT,
                             attributes={"HP": "16*", "ATK": "3", "Stage": "2"},
                             script=Script(path="rhino.py"))
        tagged = {"case": ["01096a"]}
        coverage = Coverage([stage_one, stage_two], tagged,
                            trusted=["case"], quarantined=[])
        self.assertEqual(coverage.credited_to, {})
        self.assertFalse(coverage.Covered("01096b"))

    def test_the_same_stat_line_and_text_is_enough_when_the_code_matches(self):
        # The positive control for the test above: identical statistics, and
        # the credit is granted. Without this, deleting the attribute
        # comparison entirely would still pass the negative case.
        one = MakeCard(card_id="07011", text_plain="Chaos In the Prison",
                       script=Script(path="chaos.py"))
        two = MakeCard(card_id="07026", text_plain="Chaos In the Prison",
                       script=Script(path="chaos.py"))
        coverage = Coverage([one, two], {"case": ["07011"]},
                            trusted=["case"], quarantined=[])
        self.assertEqual(coverage.credited_to, {"07026": "07011"})
        self.assertTrue(coverage.Covered("07026"))

    def test_two_scriptless_cards_sharing_boilerplate_are_not_one_card(self):
        """44 unrelated cards print "Max 1 per deck." with one stat block.

        With no module to compare, the structural rule has nothing left that
        distinguishes them, so a scriptless card is credited only on an
        explicit `reprint_of` link.
        """
        a = MakeCard(card_id="01100", text_plain="Max 1 per deck.", script=None)
        b = MakeCard(card_id="02100", text_plain="Max 1 per deck.", script=None)
        coverage = Coverage([a, b], {"case": ["01100"]},
                            trusted=["case"], quarantined=[])
        self.assertEqual(coverage.credited_to, {})
        self.assertFalse(coverage.Covered("02100"))

    def test_a_scriptless_reprint_is_credited_on_the_link(self):
        a = MakeCard(card_id="01100", text_plain="Max 1 per deck.", script=None)
        b = MakeCard(card_id="02100", text_plain="Max 1 per deck.", script=None,
                     reprint_of="01100")
        coverage = Coverage([a, b], {"case": ["01100"]},
                            trusted=["case"], quarantined=[])
        self.assertEqual(coverage.credited_to, {"02100": "01100"})
        self.assertTrue(coverage.Covered("02100"))

    def test_the_credit_never_chains(self):
        """`Scenarios` takes one hop, so a chain would drop coverage silently.

        Three ids on one module: the lowest is canonical and the other two both
        point at it, rather than forming a line where the last one's credit
        stops at a card that is itself uncredited.
        """
        cards = [MakeCard(card_id=cid, text_plain="Chaos In the Prison",
                          script=Script(path="chaos.py"))
                 for cid in ("07056", "07011", "07026")]
        coverage = Coverage(cards, {"case": ["07011"]},
                            trusted=["case"], quarantined=[])
        self.assertEqual(coverage.credited_to,
                         {"07026": "07011", "07056": "07011"})
        for card_id in ("07026", "07056"):
            self.assertNotIn(coverage.credited_to[card_id], coverage.credited_to)
            self.assertTrue(coverage.Covered(card_id))


################################################################################
#

class TestRecordedUnreachablePaths(unittest.TestCase):
    """MARVEL-121: a gap the vocabulary cannot reach has to be a record.

    `spec-campaign.md` calls a shard done when every card in the pack has a
    scenario, and that was checkable only against the uncovered list. A card
    whose *reachable* paths are all written drops out of that list, so a
    decision path nothing can reach was invisible to the tool -- three core
    spec files carried one as prose in a header and `--pack core` reported no
    problem. That is the MARVEL-16 failure shape: a missed population does not
    look like a bug, it looks like a smaller universe.

    The rule under test is that an entry is a **debt, not a discount**. It must
    not move a single coverage number, and it must not go away on its own.
    """

    def Build(self, unreachable=()):
        cards = [
            MakeCard("01001", "Asks", script=Script(
                has_imperative_handler=True, player_choice_calls=["ChooseAbilities"])),
            MakeCard("01002", "Plain", script=Script()),
            MakeCard("02001", "Another pack", pack="gob", script=Script()),
        ]
        # Every card is at depth, so the uncovered and shallow lists are both
        # empty and only a recorded entry can make a pack not-done. 01001 is
        # `interactive` and so is planned for four.
        tagged = {f"F :: a{n}": ["01001"] for n in range(4)}
        tagged["F :: b"] = ["01002"]
        tagged["F :: c"] = ["02001"]
        return Coverage(cards, tagged, trusted=list(tagged), quarantined=(),
                        unreachable=unreachable)

    ENTRY = {"card": "01002", "feature": "specs/cards/core/x.feature",
             "path": "the loop in 'each hero'", "why": "no per-seat form step",
             "blocked_by": "no `player <n> is in hero form` step",
             "issue": "MARVEL-121"}

    def test_a_recorded_path_does_not_change_any_coverage_number(self):
        # The whole point. If recording a gap moved `covered` in either
        # direction the record would be a lever on the metric, and the first
        # thing anybody would do with it is pull it.
        without = self.Build().ToDict()["totals"]
        with_entry = self.Build([self.ENTRY]).ToDict()["totals"]
        self.assertEqual(without["covered"], with_entry["covered"])
        self.assertEqual(without["at_depth"], with_entry["at_depth"])
        self.assertEqual(without["specifiable"], with_entry["specifiable"])
        self.assertEqual(with_entry["unreachable"], 1)

    def test_a_recorded_card_stays_covered(self):
        coverage = self.Build([self.ENTRY])
        self.assertTrue(coverage.Covered("01002"))
        self.assertEqual(coverage.UnreachableRows()[0]["covered"], True)

    def test_recording_a_path_never_covers_a_card(self):
        """The load-bearing direction, and the one the fixture above misses.

        Both cards in `Build` already have scenarios, so an entry against one
        of them cannot move `covered` whatever the rule is. This builds the
        board where it could: a card with no scenario at all, carrying a
        recorded unreachable path. It must still read as uncovered, and it must
        still be on the work list -- "nobody can reach one of its branches" is
        the opposite of "somebody has written it down".

        Found by mutation: making `Covered` return true for a recorded card
        passed the whole file before this case existed.
        """
        cards = [MakeCard("03001", "Nothing written", script=Script())]
        coverage = Coverage(cards, {}, trusted=(), quarantined=(),
                            unreachable=[dict(self.ENTRY, card="03001")])
        self.assertFalse(coverage.Covered("03001"))
        self.assertFalse(coverage.AtDepth("03001"))
        self.assertEqual(coverage.ToDict()["totals"]["covered"], 0)
        self.assertEqual([r["card_id"] for r in coverage.Uncovered()], ["03001"])

    def test_a_pack_with_an_open_entry_is_not_done(self):
        """The claim the record exists to make.

        Every card here is covered and at depth, so the uncovered and shallow
        lists are both empty and the old definition of done is satisfied. The
        entry is the only thing standing in the way, and it has to be enough.
        """
        clean = self.Build()
        self.assertEqual(clean.Uncovered(pack="core"), [])
        self.assertEqual(clean.Shallow(pack="core"), [])
        done, reasons = clean.Done("core")
        self.assertTrue(done, reasons)

        recorded = self.Build([self.ENTRY])
        done, reasons = recorded.Done("core")
        self.assertFalse(done)
        self.assertIn("unreachable", " ".join(reasons))

    def test_an_entry_only_blocks_its_own_pack(self):
        # A shard is the unit of "done", so a core gap must not hold up gob.
        coverage = self.Build([self.ENTRY])
        self.assertTrue(coverage.Done("gob")[0])
        self.assertFalse(coverage.Done("core")[0])

    def test_an_entry_naming_no_card_is_kept_and_flagged(self):
        """A stale entry is reported, never dropped.

        Dropping it would make the record quietly forget its own rot, which is
        the failure it exists to prevent one level up.
        """
        stale = dict(self.ENTRY, card="99999")
        rows = self.Build([stale]).UnreachableRows()
        self.assertEqual(len(rows), 1)
        self.assertFalse(rows[0]["known_card"])

    def test_one_card_can_carry_more_than_one_unreachable_path(self):
        second = dict(self.ENTRY, path="a different clause")
        coverage = self.Build([self.ENTRY, second])
        self.assertEqual(coverage.ToDict()["totals"]["unreachable"], 2)
        self.assertEqual(len(coverage.UnreachableRows(pack="core")), 2)


class TestTheCheckedInUnreachableRecord(unittest.TestCase):
    """The file itself, not the mechanism.

    An entry naming a card the dataset does not have, or a feature file that
    does not exist, is a record that has rotted -- and a rotted record is worse
    than none, because it is read as evidence.
    """

    REQUIRED = ("card", "feature", "path", "why", "blocked_by", "issue")

    def setUp(self):
        from tools.spec.coverage import UNREACHABLE
        if not os.path.exists(CARD_DATASET) or not os.path.exists(UNREACHABLE):
            self.skipTest("run from py_src/")
        with open(CARD_DATASET, "r", encoding="utf-8") as handle:
            self.cards = {c["card_id"]: c for c in json.load(handle)["cards"]}
        with open(UNREACHABLE, "r", encoding="utf-8") as handle:
            self.entries = json.load(handle)["unreachable"]

    def test_every_entry_carries_every_field(self):
        for entry in self.entries:
            with self.subTest(card=entry.get("card")):
                for field in self.REQUIRED:
                    self.assertTrue(entry.get(field),
                                    f"{field!r} is missing or empty")

    def test_every_entry_names_a_specifiable_card(self):
        for entry in self.entries:
            with self.subTest(card=entry["card"]):
                card = self.cards.get(entry["card"])
                self.assertIsNotNone(card, "no such card in the dataset")
                self.assertIn(Tier(card), SPECIFIABLE,
                              "a card the engine does not have cannot have an "
                              "unreachable decision path")

    def test_every_entry_names_a_feature_file_that_exists_and_tags_the_card(self):
        """The record and the file it describes have to stay bound together.

        Without this an entry survives its own spec file being renamed,
        rewritten or deleted, and the pack stays blocked by a claim nobody can
        check.
        """
        for entry in self.entries:
            with self.subTest(card=entry["card"]):
                path = entry["feature"]
                self.assertTrue(os.path.exists(path), f"{path} does not exist")
                with open(path, "r", encoding="utf-8") as handle:
                    text = handle.read()
                self.assertIn(f"@card:{entry['card']}", text,
                              f"{path} does not tag {entry['card']}")

    def test_the_record_is_not_a_scenario_file(self):
        """`LoadCases` walks `specs/` and would try to parse it.

        It is JSON sitting beside the scenarios, exactly like `trusted.json`,
        and the first run after it was added failed on 'case is missing
        required field name' until it was reserved.
        """
        from tools.spec.run_case import RESERVED_JSON
        self.assertIn("unreachable.json", RESERVED_JSON)


class TestEveryJsonBesideTheScenariosIsAccountedFor(unittest.TestCase):
    """A `.json` under `specs/` is either a case file or reserved. Nothing else.

    `LoadCases` walks the tree and hands every `.json` it finds to
    `SpecCase.FromDict`, so a support file dropped in beside the scenarios
    aborts the whole run -- exit 2, no verdict for any file. That is not a
    hypothetical: adding `unreachable.json` did exactly that, and **every
    scoped run in the worktree stayed green** because the failure only appears
    when something walks the whole tree.

    So the guard is here rather than in the reviewer's head. `RESERVED_JSON`
    stays an explicit list rather than "anything that fails to parse" on
    purpose -- deriving it would turn a malformed scenario into a silently
    skipped one, which is the failure this suite exists to refuse -- and this
    test is what keeps the list honest as files are added.
    """

    def test_no_unreserved_json_under_specs_would_break_a_whole_tree_run(self):
        from tools.spec.case import SpecCaseError
        from tools.spec.run_case import LoadCases, RESERVED_JSON

        if not os.path.isdir("specs"):
            self.skipTest("run from py_src/")

        for root, _dirs, files in os.walk("specs"):
            for name in files:
                if not name.endswith(".json") or name in RESERVED_JSON:
                    continue
                path = os.path.join(root, name)
                with self.subTest(path=path):
                    try:
                        LoadCases(path)
                    except (SpecCaseError, ValueError) as exc:
                        self.fail(
                            f"{path} is neither a loadable case file nor in "
                            f"RESERVED_JSON, so a whole-tree run aborts on it "
                            f"({exc}). Add it to RESERVED_JSON in "
                            f"tools/spec/run_case.py.")

    def test_the_whole_tree_loads(self):
        # The end-to-end form of the same claim, and the one that would have
        # caught it: a scoped run cannot see this.
        from tools.spec.run_case import LoadCases
        if not os.path.isdir("specs"):
            self.skipTest("run from py_src/")
        self.assertGreater(len(LoadCases("specs")), 0)
