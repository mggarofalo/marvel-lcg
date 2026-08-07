"""Tests for the Gherkin front end and the spec validation runner (MARVEL-21).

The claims under test are the ones the suite's value rests on:

- a `.feature` file compiles completely or fails naming the line
- a run's verdict follows from what it observed, not from judgement
- the quarantine cannot be talked around: only PASS enters the trusted suite,
  and an edited scenario leaves it
"""

import json
import os
import tempfile
import unittest

from tools.spec.case import SpecCase, SourceDigest, ThenStep
from tools.spec.gherkin import GherkinError, ParseFeature
from tools.spec.harness import (
    CaseResult, OUTCOME_ASSERTION, OUTCOME_ERROR, OUTCOME_PASS, OUTCOME_UNPLAYABLE)
from tools.spec.validate import (
    AppendHistory, BuildManifests, CheckDrift, Judge, ReadHistory, ReadManifest,
    SelectTrusted, Summary, VERDICT_ENGINE_SUSPECTED, VERDICT_ERROR, VERDICT_PASS,
    VERDICT_SPEC_WRONG, WriteManifest)

BACKGROUND = """Feature: A feature

  Background:
    Given the scenario "rhino"
    And the hero "spider_man"
"""


def Feature(body):
    return BACKGROUND + body


def MakeCase(**overrides):
    fields = {
        "name": "a case",
        "scenario": "rhino",
        "heroes": ("spider_man",),
        "then": (ThenStep("Rhino", "health", 14),),
        "source_path": "specs/scenarios/x.feature",
        "source_sha256": "abc123",
    }
    fields.update(overrides)
    return SpecCase(**fields)


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

    def test_case_id_carries_the_feature_name(self):
        cases = ParseFeature(Feature("""
  Scenario: one
    Then "Rhino" has 14 health
"""))
        self.assertEqual(cases[0].case_id, "A feature :: one")

    def test_and_continues_the_clause_above_it(self):
        cases = ParseFeature(Feature("""
  Scenario: one
    Given "01001a" is in hero form
    And the hand contains "01005"
    When the player attacks "Rhino"
    Then "Rhino" has 12 health
    And "01001a" is exhausted
"""))
        case = cases[0]
        # The two Background steps are settings, not Givens.
        self.assertEqual([step.verb for step in case.given], ["hero_form", "hand"])
        self.assertEqual(len(case.when), 1)
        self.assertEqual(len(case.then), 2)

    def test_the_same_sentence_means_different_things_per_clause(self):
        # `"X" has N threat` sets the board under Given and asserts under Then.
        # Without the keyword there is no way to tell them apart.
        cases = ParseFeature(Feature("""
  Scenario: one
    Given "The Break-In!" has 5 threat
    When the player thwarts "The Break-In!"
    Then "The Break-In!" has 4 threat
"""))
        case = cases[0]
        self.assertEqual([step.verb for step in case.given], ["threat"])
        self.assertEqual(case.given[0].value, 5)
        self.assertEqual(case.then[0].prop, "threat")
        self.assertEqual(case.then[0].value, 4)

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
        self.assertEqual(cases[1].then[0].value, 11)
        # Each row is a distinct case, so each gets a distinct identity.
        self.assertNotEqual(cases[0].case_id, cases[1].case_id)
        self.assertIn("damage=3", cases[1].case_id)

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
  @slow @self-test
  Scenario: one
    Then "Rhino" has 14 health
"""))
        self.assertEqual(cases[0].tags, ("slow", "self-test"))

    def test_a_scenario_without_a_then_is_rejected(self):
        # An assertion-free scenario would report PASS while proving nothing.
        with self.assertRaises(GherkinError):
            ParseFeature(Feature("""
  Scenario: one
    Given "01001a" is in hero form
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
        self.assertEqual(cases[0].then[0].value, False)
        self.assertEqual(cases[0].then[1].value, True)

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
        manifest = {"version": 1, "scenarios": {}}
        selection = SelectTrusted([MakeCase()], manifest)
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
            # The note travels with the file so nobody edits it unwarned.
            self.assertEqual(data["note"], "Do not hand-edit.")

    def test_a_file_that_is_not_a_manifest_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            path = os.path.join(directory, "trusted.json")
            with open(path, "w", encoding="utf-8") as handle:
                json.dump({"something": "else"}, handle)
            with self.assertRaises(Exception):
                ReadManifest(path)

    def test_reading_a_missing_manifest_yields_an_empty_suite(self):
        with tempfile.TemporaryDirectory() as directory:
            data = ReadManifest(os.path.join(directory, "nothing.json"))
            self.assertEqual(data["scenarios"], {})


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
        # `specs/scenarios/known_disagreements.feature` is wrong on purpose. If
        # any of it starts passing, the harness has stopped telling the truth.
        from tools.spec.gherkin import LoadFeatureFile
        from tools.spec.validate import Validate

        path = "./specs/scenarios/known_disagreements.feature"
        if not os.path.exists(path):
            self.skipTest("run from py_src/")

        summary = Validate(LoadFeatureFile(path))
        verdicts = {judgement.case.name: judgement.verdict
                    for judgement in summary.judgements}

        self.assertEqual(len(verdicts), 3)
        for name, verdict in verdicts.items():
            self.assertNotEqual(verdict, VERDICT_PASS, name)
            # Each scenario says in its own name which verdict it expects.
            self.assertIn(verdict, name)

    def test_the_trusted_manifest_only_contains_scenarios_that_pass(self):
        from tools.spec.validate import ReadManifest as Read

        path = "./specs/trusted.json"
        if not os.path.exists(path):
            self.skipTest("run from py_src/")

        trusted = Read(path)["scenarios"]
        self.assertTrue(trusted, "the trusted suite should not be empty")
        for case_id in trusted:
            self.assertNotIn("Quarantine self-test", case_id)


if __name__ == "__main__":
    unittest.main()
