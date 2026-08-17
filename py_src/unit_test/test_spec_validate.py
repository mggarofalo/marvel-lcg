"""Tests for the Gherkin front end and the spec validation runner (MARVEL-21).

The claims under test are the ones the suite's value rests on:

- a `.feature` file compiles completely or fails naming the line
- the step vocabulary the runner implements is exactly the one checked in, so
  drift between the two runners fails a build instead of rotting
- a run's verdict follows from what it observed, not from judgement
- the quarantine cannot be talked around: only PASS enters the trusted suite,
  and an edited scenario leaves it
"""

import json
import os
import tempfile
import unittest

from tools.spec.case import (
    GivenStep, NoPromptStep, NotOfferedStep, PromptStep, SourceDigest, SpecCase,
    ThenStep, WhenStep)
from tools.spec.gherkin import GherkinError, ParseFeature, Vocabulary
from tools.spec.harness import (
    CaseResult, OUTCOME_ASSERTION, OUTCOME_ERROR, OUTCOME_PASS, OUTCOME_UNPLAYABLE)
from tools.spec.validate import (
    AppendHistory, BuildManifests, CheckDrift, Judge, ReadHistory, ReadManifest,
    SelectTrusted, Summary, VERDICT_ENGINE_SUSPECTED, VERDICT_ERROR, VERDICT_PASS,
    VERDICT_SPEC_WRONG, WriteManifest, WriteTriage)

BACKGROUND = """Feature: A feature

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"
"""

CATALOGUE = "./specs/steps.catalogue.json"


def Feature(body):
    return BACKGROUND + body


def MakeCase(**overrides):
    fields = {
        "name": "a case",
        "scenario": "rhino",
        "heroes": ("spider_man",),
        "beats": (ThenStep("Rhino", "health", 14),),
        "source_path": "specs/cards/core/x.feature",
        "source_sha256": "abc123",
    }
    fields.update(overrides)
    return SpecCase(**fields)


################################################################################
#

class TestStepCatalogue(unittest.TestCase):
    """The catalogue is the contract both engines conform to."""

    def Catalogue(self):
        if not os.path.exists(CATALOGUE):
            self.skipTest("run from py_src/")
        with open(CATALOGUE, "r", encoding="utf-8") as handle:
            return json.load(handle)

    def test_the_runner_implements_exactly_the_checked_in_vocabulary(self):
        # A form added to the parser without being checked in -- or checked in
        # without being implemented -- fails here rather than drifting quietly.
        catalogue = self.Catalogue()["steps"]
        implemented = Vocabulary()

        self.assertEqual(sorted(catalogue), sorted(implemented))
        for clause in sorted(catalogue):
            self.assertEqual(
                sorted(catalogue[clause]), sorted(implemented[clause]),
                f"{clause} steps differ between the catalogue and the parser")

    def test_the_catalogue_covers_the_two_transcript_assertions(self):
        catalogue = self.Catalogue()["steps"]
        self.assertIn("I am prompted to choose one", catalogue["then"])
        self.assertIn("I am not prompted again", catalogue["then"])


################################################################################
#

class TestGherkinParsing(unittest.TestCase):

    def test_background_applies_to_every_scenario(self):
        cases = ParseFeature(Feature("""
  Scenario: one
    Then "Rhino" has 14 health

  Scenario: two
    Then "Rhino" has 14 health
"""))
        self.assertEqual(len(cases), 2)
        for case in cases:
            self.assertEqual(case.scenario, "rhino")
            self.assertEqual(case.heroes, ("spider_man",))

    def test_the_at_setup_deck_steps_compile_to_scene_fields(self):
        """MARVEL-121. They are settings, not `Given` steps, and that is the point.

        A `Given` is applied after `GameSetup()` returns; these have to be part
        of the scene the engine sets up *from*, because the abilities that read
        them fire inside `GameSetup()`. So they land beside `expert` and `seed`
        rather than in `case.given`, and a parser that filed them as ordinary
        deck steps would put them back on the wrong side of setup.
        """
        cases = ParseFeature(Feature("""
  Scenario: one
    Given my deck at setup is "Backflip", "Swinging Web Kick"
    And the encounter deck at setup is "Hydra Mercenary"
    Then "Rhino" has 14 health
"""))
        case = cases[0]
        self.assertEqual(case.setup_player_deck, ("Backflip", "Swinging Web Kick"))
        self.assertEqual(case.setup_encounter_deck, ("Hydra Mercenary",))
        self.assertEqual(case.given, ())

    def test_an_at_setup_deck_step_accumulates_like_its_given_twin(self):
        # `my deck is` accumulates rather than replaces (MARVEL-82's docs say so
        # explicitly, and a Background that stocks one card is the usual way to
        # be surprised by it). The setup-time spelling has to behave the same
        # way or the two would be traps in opposite directions.
        cases = ParseFeature(Feature("""
  Scenario: one
    Given my deck at setup is "Backflip"
    And my deck at setup is "Swinging Web Kick"
    Then "Rhino" has 14 health
"""))
        self.assertEqual(cases[0].setup_player_deck,
                         ("Backflip", "Swinging Web Kick"))

    def test_the_ordinary_deck_step_is_still_a_given(self):
        # The control. `my deck is` must not be swept up by the new pattern.
        cases = ParseFeature(Feature("""
  Scenario: one
    Given my deck is "Backflip"
    Then "Rhino" has 14 health
"""))
        self.assertEqual(cases[0].setup_player_deck, ())
        self.assertEqual([step.verb for step in cases[0].given], ["player_deck"])

    def test_case_id_carries_the_feature_name(self):
        cases = ParseFeature(Feature("""
  Scenario: one
    Then "Rhino" has 14 health
"""))
        self.assertEqual(cases[0].case_id, "A feature :: one")

    def test_a_transcript_interleaves_when_and_then_in_order(self):
        cases = ParseFeature(Feature("""
  Scenario: one
    Given I am in hero form
    When I attack "Rhino"
    Then "Rhino" has 12 damage
    When I pass
    Then I am exhausted
"""))
        self.assertEqual([beat.kind for beat in cases[0].beats],
                         ["when", "then", "when", "then"])

    def test_and_continues_the_clause_above_it(self):
        cases = ParseFeature(Feature("""
  Scenario: one
    Given I am in hero form
    And my hand is "Swinging Web Kick"
    When I attack "Rhino"
    Then "Rhino" has 12 health
    And I am exhausted
"""))
        case = cases[0]
        # The two Background steps are settings, not Givens.
        self.assertEqual([step.verb for step in case.given], ["hero_form", "hand"])
        self.assertEqual([beat.kind for beat in case.beats], ["when", "then", "then"])

    def test_the_same_sentence_means_different_things_per_clause(self):
        # `"X" has N threat` sets the board under Given and asserts under Then.
        # Without the keyword there is no way to tell them apart.
        cases = ParseFeature(Feature("""
  Scenario: one
    Given the main scheme has 5 threat
    When I thwart "The Break-In!"
    Then the main scheme has 4 threat
"""))
        case = cases[0]
        self.assertEqual([step.verb for step in case.given], ["threat"])
        self.assertEqual(case.given[0].value, 5)
        self.assertEqual(case.beats[1].prop, "threat")
        self.assertEqual(case.beats[1].value, 4)

    def test_the_first_person_deck_step_is_sugar_for_player_one(self):
        # MARVEL-101. `my deck is` and `player 1's deck is` are the *same* step
        # with the seat left at its default, not two steps that happen to agree.
        # If they ever compile to different verbs or different seats, a scenario
        # could mean one thing and read as the other.
        cases = ParseFeature(Feature("""
  Scenario: mine
    Given my deck is "Backflip"
    Then I have 1 card in my deck

  Scenario: named
    Given player 1's deck is "Backflip"
    Then I have 1 card in my deck
"""))
        mine, named = cases[0].given[0], cases[1].given[0]
        self.assertEqual(mine.verb, "player_deck")
        self.assertEqual(mine.player, 0)
        self.assertEqual(mine, named)

    def test_a_per_player_deck_step_names_the_seat_it_stocks(self):
        cases = ParseFeature(Feature("""
  Scenario: one
    Given player 2's deck is "Aunt May", "Backflip"
    Then player 2 has 2 cards in their deck
"""))
        case = cases[0]
        step = case.given[0]
        self.assertEqual(step.verb, "player_deck")
        # 0-based seat, so player 2 is index 1.
        self.assertEqual(step.player, 1)
        self.assertEqual(step.cards, ("Aunt May", "Backflip"))
        self.assertEqual(case.beats[0].subject, "player 2")
        self.assertEqual(case.beats[0].prop, "deck_size")
        self.assertEqual(case.beats[0].value, 2)

    def test_a_per_player_deck_count_is_not_the_hand_count(self):
        # The two `player <n> has <m> cards ...` forms differ only in their
        # tail, so a regex that stopped reading early would quietly answer the
        # deck question with the hand size.
        cases = ParseFeature(Feature("""
  Scenario: one
    Given my deck is "Backflip"
    Then player 1 has 3 cards in hand
    And player 1 has 1 card in their deck
"""))
        self.assertEqual([beat.prop for beat in cases[0].beats],
                         ["hand_size", "deck_size"])

    def test_a_given_after_a_when_is_rejected(self):
        # The board is built once, before the transcript starts. A Given in the
        # middle would silently not do what it looks like it does.
        with self.assertRaises(GherkinError) as caught:
            ParseFeature(Feature("""
  Scenario: one
    When I change form
    Given I am in hero form
    Then I am in hero form
"""))
        self.assertIn("cannot follow a When", str(caught.exception))

    def test_a_prompt_assertion_reads_its_option_table(self):
        cases = ParseFeature(Feature("""
  Scenario: one
    When I play "Nick Fury"
    Then I am prompted to choose one
      | Draw 3 cards              |
      | Deal 4 damage to an enemy |
    When I choose "Draw 3 cards"
    Then I have 3 cards in hand
"""))
        prompt = cases[0].beats[1]
        self.assertEqual(prompt.kind, "prompt")
        self.assertEqual(prompt.options, ("Draw 3 cards", "Deal 4 damage to an enemy"))

    def test_a_prompt_assertion_without_a_table_is_an_error(self):
        with self.assertRaises(GherkinError) as caught:
            ParseFeature(Feature("""
  Scenario: one
    When I play "Nick Fury"
    Then I am prompted to choose one
"""))
        self.assertIn("needs a table", str(caught.exception))

    def test_unknown_step_names_the_line_and_the_clause(self):
        with self.assertRaises(GherkinError) as caught:
            ParseFeature(Feature("""
  Scenario: one
    Given the hero eats a sandwich
    Then "Rhino" has 14 health
"""), path="specs/x.feature")
        message = str(caught.exception)
        self.assertIn("specs/x.feature:8", message)
        self.assertIn("Given step", message)
        self.assertIn("sandwich", message)

    def test_a_then_in_the_wrong_clause_is_rejected(self):
        # "is in the <zone>" only exists as a Then; asking for it under Given
        # must fail rather than being silently dropped.
        with self.assertRaises(GherkinError):
            ParseFeature(Feature("""
  Scenario: one
    Given "01005" is in the "HandsArea"
    Then "Rhino" has 14 health
"""))

    def test_scenario_outline_expands_one_case_per_example_row(self):
        cases = ParseFeature(Feature("""
  Scenario Outline: damage
    Given "Rhino" has <damage> damage
    Then "Rhino" has <health> health

    Examples:
      | damage | health |
      | 0      | 14     |
      | 3      | 11     |
"""))
        self.assertEqual(len(cases), 2)
        self.assertEqual(cases[0].given[0].value, 0)
        self.assertEqual(cases[1].given[0].value, 3)
        self.assertEqual(cases[1].beats[0].value, 11)
        self.assertNotEqual(cases[0].case_id, cases[1].case_id)
        self.assertIn("damage=3", cases[1].case_id)

    def test_an_outline_with_no_examples_is_an_error_not_a_silent_drop(self):
        # A scenario that looks authored but never runs never fails either,
        # which is the one thing this parser promises not to allow.
        with self.assertRaises(GherkinError) as caught:
            ParseFeature(Feature("""
  Scenario Outline: never runs
    Given "Rhino" has <damage> damage
    Then "Rhino" has 14 health

  Scenario: this one runs
    Then "Rhino" has 14 health
"""))
        self.assertIn("no Examples rows", str(caught.exception))

    def test_an_outline_with_only_a_header_row_is_an_error(self):
        with self.assertRaises(GherkinError) as caught:
            ParseFeature(Feature("""
  Scenario Outline: never runs
    Given "Rhino" has <damage> damage
    Then "Rhino" has 14 health

    Examples:
      | damage |
"""))
        self.assertIn("no Examples rows", str(caught.exception))

    def test_a_trailing_outline_with_no_examples_is_caught_at_end_of_file(self):
        with self.assertRaises(GherkinError):
            ParseFeature(Feature("""
  Scenario: fine
    Then "Rhino" has 14 health

  Scenario Outline: never runs
    Given "Rhino" has <damage> damage
    Then "Rhino" has 14 health
"""))

    def test_a_placeholder_with_no_column_is_an_error(self):
        with self.assertRaises(GherkinError) as caught:
            ParseFeature(Feature("""
  Scenario Outline: damage
    Given "Rhino" has <damage> damage
    Then "Rhino" has <health> health

    Examples:
      | damage |
      | 0      |
"""))
        self.assertIn("<health>", str(caught.exception))

    def test_a_short_examples_row_is_an_error(self):
        with self.assertRaises(GherkinError) as caught:
            ParseFeature(Feature("""
  Scenario Outline: damage
    Given "Rhino" has <damage> damage
    Then "Rhino" has 14 health

    Examples:
      | damage | health |
      | 0      |
"""))
        self.assertIn("row has 1 cell", str(caught.exception))

    def test_comments_and_tags(self):
        cases = ParseFeature(Feature("""
  # this line is ignored
  @card:01084 @self-test
  Scenario: one
    Then "Rhino" has 14 health
"""))
        self.assertEqual(cases[0].tags, ("card:01084", "self-test"))
        self.assertEqual(cases[0].card_tags, ("01084",))

    def test_tags_accumulate_across_lines(self):
        """One scenario, one `@card:` per line -- both ids must survive.

        This is how every file in `specs/cards/reprints/` credits a reprint
        pair, because a scenario about one printing is the same claim about the
        other. The parser used to assign per tag line rather than accumulate, so
        the *first* id of each pair was dropped and the card read as uncovered
        while its own spec sat in the tree -- the coverage number moving in the
        one direction it must never move on its own.
        """
        cases = ParseFeature(Feature("""
  @card:05014
  @card:38015
  Scenario: one
    Then "Rhino" has 14 health
"""))
        self.assertEqual(cases[0].card_tags, ("05014", "38015"))

    def test_tags_do_not_leak_into_the_next_scenario(self):
        # The other half of accumulating: the buffer has to be emptied when a
        # scenario consumes it, or every later scenario in the file inherits
        # every tag above it and coverage over-counts instead.
        cases = ParseFeature(Feature("""
  @card:05014
  Scenario: one
    Then "Rhino" has 14 health

  @card:38015
  Scenario: two
    Then "Rhino" has 14 health
"""))
        self.assertEqual(cases[0].card_tags, ("05014",))
        self.assertEqual(cases[1].card_tags, ("38015",))

    def test_a_scenario_that_asserts_nothing_is_rejected(self):
        with self.assertRaises(GherkinError):
            ParseFeature(Feature("""
  Scenario: one
    Given I am in hero form
    When I change form
"""))

    def test_a_feature_with_no_scenarios_is_rejected(self):
        with self.assertRaises(GherkinError):
            ParseFeature(BACKGROUND)

    def test_settings_can_be_overridden_per_scenario(self):
        cases = ParseFeature(Feature("""
  Scenario: default seed
    Then "Rhino" has 14 health

  Scenario: its own seed
    Given the seed is 42
    And the difficulty is expert
    Then "Rhino" has 14 health
"""))
        self.assertEqual(cases[0].seed, 1)
        self.assertFalse(cases[0].expert)
        self.assertEqual(cases[1].seed, 42)
        self.assertTrue(cases[1].expert)

    def test_negated_then_steps(self):
        cases = ParseFeature(Feature("""
  Scenario: one
    Then "Rhino" is not stunned
    And "Rhino" is in play
"""))
        self.assertEqual(cases[0].beats[0].value, False)
        self.assertEqual(cases[0].beats[1].value, True)

    def test_a_restriction_compiles_to_its_action_and_card(self):
        cases = ParseFeature(Feature("""
  Scenario: one
    Then I cannot attack "Rhino"
    And I cannot thwart "The Break-In!"
"""))
        attack, thwart = cases[0].beats
        self.assertEqual((attack.kind, attack.option, attack.card),
                         ("cannot", "attack", "Rhino"))
        self.assertEqual((thwart.kind, thwart.option, thwart.card),
                         ("cannot", "thwart", "The Break-In!"))

    def test_a_restriction_counts_as_an_assertion(self):
        """Otherwise a Guard scenario is rejected for asserting nothing.

        `ASSERTION_KINDS` is what the "this case proves nothing" guard reads,
        and a `cannot` is the only assertion some restriction scenarios make.
        """
        case = ParseFeature(Feature("""
  Scenario: one
    Then I cannot attack "Rhino"
"""))[0]
        self.assertEqual(len(case.Assertions()), 1)

    def test_the_general_restriction_form_reaches_the_same_assertion(self):
        """MARVEL-94. `CannotStep` was always general; the sentences were not.

        Only `attack` and `thwart` had a step form, so "remove 2 threat from a
        *different* scheme" could not be stated at all and had to be inferred
        from the board afterwards.
        """
        case = ParseFeature(Feature("""
  Scenario: one
    Then I cannot choose "Futurist" targeting "Backflip"
"""))[0]
        beat = case.beats[0]
        self.assertEqual((beat.kind, beat.option, beat.card),
                         ("cannot", "Futurist", "Backflip"))

    def test_a_not_offered_step_names_the_option_and_its_card(self):
        """Affordability is observed on the option, not recomputed from a hand."""
        case = ParseFeature(Feature("""
  Scenario: one
    Then I am not offered "Action" on "Vision"
"""))[0]
        beat = case.beats[0]
        self.assertEqual((beat.kind, beat.option, beat.card),
                         ("not_offered", "Action", "Vision"))
        self.assertEqual(len(case.Assertions()), 1)

    def test_a_not_offered_step_survives_a_json_round_trip(self):
        case = MakeCase(beats=(NotOfferedStep("Action", "Vision"),))
        again = SpecCase.FromJson(case.ToJson())
        self.assertEqual(again.beats, case.beats)

    def test_the_two_restriction_forms_describe_themselves_differently(self):
        """The failure message is the thing an author acts on.

        Echoing the general form back in the verb form's shape gives
        "I cannot Futurist 'Backflip'", which reads as a typo in the harness
        rather than as the assertion that failed.
        """
        cases = ParseFeature(Feature("""
  Scenario: one
    Then I cannot attack "Rhino"
    And I cannot choose "Futurist" targeting "Backflip"
"""))
        verb, general = cases[0].beats
        self.assertEqual(verb.Describe(), "I cannot attack 'Rhino'")
        self.assertEqual(general.Describe(),
                         "I cannot choose 'Futurist' targeting 'Backflip'")

    def test_legal_targets_compiles_with_its_table(self):
        case = ParseFeature(Feature("""
  Scenario: one
    Then the legal targets for "Futurist" are
      | Repulsor Blast |
      | Mark V Armor   |
"""))[0]
        beat = case.beats[0]
        self.assertEqual(beat.kind, "targets")
        self.assertEqual(beat.option, "Futurist")
        self.assertEqual(beat.targets, ("Repulsor Blast", "Mark V Armor"))

    def test_legal_targets_without_a_table_is_a_parse_error(self):
        """An empty target list would read as "this option accepts nothing".

        That is a real claim and a different one, so it gets its own sentence
        (`I cannot choose`) rather than being spelled as an omission.
        """
        with self.assertRaises(GherkinError) as caught:
            ParseFeature(Feature("""
  Scenario: one
    Then the legal targets for "Futurist" are
"""))
        self.assertIn("needs a table", str(caught.exception))

    def test_legal_targets_counts_as_an_assertion(self):
        case = ParseFeature(Feature("""
  Scenario: one
    Then the legal targets for "Futurist" are
      | Repulsor Blast |
"""))[0]
        self.assertEqual(len(case.Assertions()), 1)

    def test_the_target_ceiling_compiles_to_a_limit_beat(self):
        """MARVEL-120. The other half of "up to N".

        `the legal targets for` pins which cards are candidates and a `When`
        naming three of them pins that three is reachable; nothing said three
        was the maximum, and naming a fourth is refused rather than failing an
        assertion.
        """
        case = ParseFeature(Feature("""
  Scenario: one
    Then the target maximum for "Play" is 3
"""))[0]
        beat = case.beats[0]
        self.assertEqual((beat.kind, beat.option, beat.maximum),
                         ("limit", "Play", 3))
        self.assertEqual(len(case.Assertions()), 1)

    def test_a_ceiling_below_one_is_a_parse_error(self):
        """"This option takes no target" is `I cannot choose`, not a ceiling."""
        with self.assertRaises(GherkinError) as caught:
            ParseFeature(Feature("""
  Scenario: one
    Then the target maximum for "Play" is 0
"""))
        self.assertIn("I cannot choose", str(caught.exception))

    def test_resource_icons_compile_to_a_property_read(self):
        """MARVEL-120. `RES` was the one printed attribute with no step.

        It is the only thing telling 01043a/b/c/d apart -- four ids, one
        printed text, one script -- so the coverage tool counted four cards of
        work against a vocabulary that could express one claim.
        """
        case = ParseFeature(Feature("""
  Scenario: one
    Then "01043a" has 1 "energy" resource icon
    And "Vibranium" has 2 "wild" resource icons
"""))[0]
        one, two = case.beats
        self.assertEqual((one.subject, one.prop, one.value),
                         ("01043a", "resource:energy", 1))
        self.assertEqual((two.subject, two.prop, two.value),
                         ("Vibranium", "resource:wild", 2))

    def test_resource_icons_do_not_shadow_the_generic_property_step(self):
        """`"<card>" has <n> "<property>"` is the wider pattern of the two.

        Both are anchored, so the suffix keeps them apart -- but the generic
        form matching first would turn every icon assertion into a read of a
        property called "energy" that no card has.
        """
        case = ParseFeature(Feature("""
  Scenario: one
    Then "Rhino" has 3 "scheme"
"""))[0]
        self.assertEqual(case.beats[0].prop, "scheme")

    def test_the_two_phase_forms_compile_to_different_properties(self):
        """`the villain phase` and `"Enemy Activation"` are not the same claim.

        Both read as "it is the ... phase", and the quoted form is the wider
        pattern of the two, so a table ordered the other way would swallow
        `villain` as a phase name and every rulebook-grain assertion would
        quietly start asking about a `Phase.State` that does not exist.
        """
        cases = ParseFeature(Feature("""
  Scenario: one
    Then it is the villain phase
    And it is the "Enemy Activation" phase
"""))
        group, state = cases[0].beats
        self.assertEqual((group.prop, group.value), ("phase_group", "villain"))
        self.assertEqual((state.prop, state.value), ("phase", "Enemy Activation"))

    def test_every_scenario_carries_the_source_hash(self):
        text = Feature("""
  Scenario: one
    Then "Rhino" has 14 health
""")
        cases = ParseFeature(text, path="specs/x.feature")
        self.assertEqual(cases[0].source_sha256, SourceDigest(text))
        self.assertEqual(cases[0].source_path, "specs/x.feature")


################################################################################
#

def MakeResult(outcome, message="", assertions=()):
    return CaseResult(case=MakeCase(), outcome=outcome, message=message,
                      assertions=list(assertions))


class TestVerdicts(unittest.TestCase):

    def test_a_clean_run_passes(self):
        self.assertEqual(Judge(MakeResult(OUTCOME_PASS)).verdict, VERDICT_PASS)

    def test_a_scenario_the_engine_would_not_play_blames_the_spec(self):
        judgement = Judge(MakeResult(OUTCOME_UNPLAYABLE, message="never offered"))
        self.assertEqual(judgement.verdict, VERDICT_SPEC_WRONG)
        self.assertIn("never offered", judgement.reason)

    def test_a_clean_run_with_a_wrong_value_suspects_the_engine(self):
        self.assertEqual(Judge(MakeResult(OUTCOME_ASSERTION)).verdict,
                         VERDICT_ENGINE_SUSPECTED)

    def test_an_engine_exception_is_its_own_verdict(self):
        self.assertEqual(Judge(MakeResult(OUTCOME_ERROR)).verdict, VERDICT_ERROR)

    def test_only_a_pass_is_trusted(self):
        for outcome in (OUTCOME_UNPLAYABLE, OUTCOME_ASSERTION, OUTCOME_ERROR):
            self.assertFalse(Judge(MakeResult(outcome)).trusted, outcome)
        self.assertTrue(Judge(MakeResult(OUTCOME_PASS)).trusted)


################################################################################
#

class TestQuarantine(unittest.TestCase):

    def Summarise(self, *outcomes):
        summary = Summary()
        for index, outcome in enumerate(outcomes):
            result = CaseResult(case=MakeCase(name=f"case {index}"), outcome=outcome)
            summary.Add(Judge(result))
        return summary

    def test_manifests_split_pass_from_everything_else(self):
        summary = self.Summarise(OUTCOME_PASS, OUTCOME_ASSERTION, OUTCOME_ERROR)
        trusted, quarantine = BuildManifests(summary)
        self.assertEqual(list(trusted), ["case 0"])
        self.assertEqual(sorted(quarantine), ["case 1", "case 2"])
        self.assertEqual(quarantine["case 1"]["verdict"], VERDICT_ENGINE_SUSPECTED)

    def test_a_trusted_entry_is_pinned_to_its_source(self):
        summary = self.Summarise(OUTCOME_PASS)
        trusted, _ = BuildManifests(summary)
        self.assertEqual(trusted["case 0"]["sha256"], "abc123")

    def test_editing_a_scenario_drops_it_out_of_the_trusted_suite(self):
        manifest = {"version": 1, "scenarios": {
            "a case": {"source": "specs/x.feature", "sha256": "abc123"}}}

        unchanged = SelectTrusted([MakeCase()], manifest)
        self.assertEqual([case.case_id for case in unchanged.cases], ["a case"])

        edited = SelectTrusted([MakeCase(source_sha256="different")], manifest)
        self.assertEqual(edited.cases, [])
        self.assertIn("source changed", edited.stale[0])

    def test_a_scenario_that_never_passed_is_not_run_by_the_gate(self):
        selection = SelectTrusted([MakeCase()], {"version": 1, "scenarios": {}})
        self.assertEqual(selection.cases, [])
        self.assertEqual(selection.untrusted, ["a case"])

    def test_a_trusted_entry_whose_file_is_gone_is_reported(self):
        manifest = {"version": 1, "scenarios": {
            "vanished": {"source": "specs/gone.feature", "sha256": "x"}}}
        selection = SelectTrusted([], manifest)
        self.assertIn("no longer on disk", selection.stale[0])

    def test_manifest_round_trip_keeps_the_do_not_edit_note(self):
        with tempfile.TemporaryDirectory() as directory:
            path = os.path.join(directory, "trusted.json")
            WriteManifest(path, {"a case": {"source": "x", "sha256": "y"}},
                          note="Do not hand-edit.")
            data = ReadManifest(path)
            self.assertEqual(data["scenarios"]["a case"]["sha256"], "y")
            self.assertEqual(data["note"], "Do not hand-edit.")

    def test_reading_a_missing_manifest_yields_an_empty_suite(self):
        with tempfile.TemporaryDirectory() as directory:
            data = ReadManifest(os.path.join(directory, "nothing.json"))
            self.assertEqual(data["scenarios"], {})

    def test_a_file_that_is_not_a_manifest_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            path = os.path.join(directory, "trusted.json")
            with open(path, "w", encoding="utf-8") as handle:
                json.dump({"something": "else"}, handle)
            with self.assertRaises(Exception):
                ReadManifest(path)


################################################################################
#

class TestTriageQueue(unittest.TestCase):
    """The record an adjudicator reads to call it a spec bug or an engine bug."""

    def Judgement(self, outcome=OUTCOME_ASSERTION):
        case = MakeCase(
            name="a disagreement",
            tags=("card:01084",),
            given=(GivenStep("hand", ("Nick Fury",)),),
            beats=(
                WhenStep(option="play", card="Nick Fury"),
                PromptStep(options=("Draw 3 cards", "Deal 4 damage to an enemy")),
                WhenStep(option="Draw 3 cards"),
                ThenStep("Shocker", "damage", 4),
                NoPromptStep(),
            ),
        )
        return Judge(CaseResult(case=case, outcome=outcome, message="it disagreed",
                                engine_log="> 41: something happened"))

    def test_a_record_can_be_built_for_every_failing_verdict(self):
        # This is the whole of `--triage`; a stale field here crashes the
        # documented workflow on the first disagreement.
        for outcome in (OUTCOME_ASSERTION, OUTCOME_UNPLAYABLE, OUTCOME_ERROR):
            record = self.Judgement(outcome).TriageRecord()
            self.assertEqual(record["case"], "a disagreement")
            self.assertEqual(record["reason"], "it disagreed")

    def test_the_record_keeps_the_transcript_in_order(self):
        # Splitting it back into when/then lists would throw away the
        # interleaving, which is the thing worth reading.
        record = self.Judgement().TriageRecord()
        self.assertEqual([entry.split(":", 1)[0] for entry in record["transcript"]],
                         ["when", "prompt", "when", "then", "no_prompt"])
        self.assertEqual(record["given"], ["hand is Nick Fury"])
        self.assertEqual(record["tags"], ["card:01084"])

    def test_the_record_carries_the_engine_log(self):
        self.assertIn("something happened", self.Judgement().TriageRecord()["engine_log"])

    def test_writing_the_queue_covers_only_the_disagreements(self):
        summary = Summary()
        summary.Add(Judge(CaseResult(case=MakeCase(name="fine"), outcome=OUTCOME_PASS)))
        summary.Add(self.Judgement())

        with tempfile.TemporaryDirectory() as directory:
            path = os.path.join(directory, "triage.json")
            WriteTriage(path, summary)
            with open(path, "r", encoding="utf-8") as handle:
                data = json.load(handle)

        self.assertEqual(data["count"], 1)
        self.assertEqual(data["records"][0]["case"], "a disagreement")


################################################################################
#

class TestHistory(unittest.TestCase):

    def test_counts_are_appended_one_line_per_run(self):
        summary = Summary()
        summary.Add(Judge(CaseResult(case=MakeCase(name="a"), outcome=OUTCOME_PASS)))
        summary.Add(Judge(CaseResult(case=MakeCase(name="b"), outcome=OUTCOME_ASSERTION)))

        with tempfile.TemporaryDirectory() as directory:
            path = os.path.join(directory, "history.jsonl")
            AppendHistory(path, summary, label="one")
            AppendHistory(path, summary, label="two")
            entries = ReadHistory(path)

        self.assertEqual(len(entries), 2)
        self.assertEqual(entries[0]["total"], 2)
        self.assertEqual(entries[0]["counts"][VERDICT_PASS], 1)
        self.assertAlmostEqual(entries[0]["disagreement_rate"], 0.5)

    def test_drift_is_reported_only_when_the_rate_rises(self):
        history = [{"disagreement_rate": 0.10}]
        self.assertEqual(CheckDrift(history, 0.10, 0.05), "")
        self.assertEqual(CheckDrift(history, 0.14, 0.05), "")
        self.assertIn("rose from", CheckDrift(history, 0.30, 0.05))
        # An improving rate is never drift.
        self.assertEqual(CheckDrift(history, 0.0, 0.05), "")

    def test_the_first_run_has_nothing_to_drift_from(self):
        self.assertEqual(CheckDrift([], 0.99, 0.01), "")


################################################################################
# End to end -- these boot the engine and play the shipped scenarios.

class TestShippedScenarios(unittest.TestCase):

    def test_the_self_test_scenarios_land_on_the_verdicts_they_claim(self):
        # `specs/self-test/quarantine.feature` is wrong on purpose. If any of it
        # starts passing, the harness has stopped telling the truth.
        from tools.spec.gherkin import LoadFeatureFile
        from tools.spec.validate import Validate

        path = "./specs/self-test/quarantine.feature"
        if not os.path.exists(path):
            self.skipTest("run from py_src/")

        summary = Validate(LoadFeatureFile(path))
        verdicts = {judgement.case.name: judgement.verdict
                    for judgement in summary.judgements}

        self.assertTrue(verdicts)
        for name, verdict in verdicts.items():
            self.assertNotEqual(verdict, VERDICT_PASS, name)
            # Each scenario says in its own name which verdict it expects.
            self.assertIn(verdict, name)

    def test_the_trusted_manifest_only_contains_scenarios_that_pass(self):
        path = "./specs/trusted.json"
        if not os.path.exists(path):
            self.skipTest("run from py_src/")

        trusted = ReadManifest(path)["scenarios"]
        self.assertTrue(trusted, "the trusted suite should not be empty")
        for case_id in trusted:
            self.assertNotIn("Quarantine self-test", case_id)


if __name__ == "__main__":
    unittest.main()
