"""Tests for the puzzle-based spec harness (MARVEL-20).

Two layers, because they cost very different amounts:

- pure logic -- case loading, card-reference parsing, assertion evaluation and
  its wording -- which needs no engine at all
- a handful of end-to-end cases that boot the engine, build a puzzle board,
  drive an action through the bot device and assert on the result

The end-to-end tests are the ones that matter: they are the claim that a card
behavior can be expressed as a test case and run. They must be run from
`py_src/`, like everything else in this repo.
"""

import unittest

from tools.spec.assertions import Evaluate, ResolveSubject
from tools.spec.case import (
    GivenStep, SpecCase, SpecCaseError, SourceDigest, ThenStep, WhenStep, LoadJsonCases)
from tools.spec.harness import (
    OUTCOME_ASSERTION, OUTCOME_PASS, OUTCOME_UNPLAYABLE, RunCase)
from tools.spec.resolve import CardRef, CardRefError
from tools.spec.state import CardState, PlayerState, StateView, UnknownProperty


def MakeCase(**overrides):
    fields = {
        "name": "a case",
        "scenario": "rhino",
        "heroes": ("spider_man",),
        "then": (ThenStep("Rhino", "health", 14),),
    }
    fields.update(overrides)
    return SpecCase(**fields)


def MakeCard(**overrides):
    fields = {
        "object_id": 3,
        "name": "Rhino",
        "card_id": "01094",
        "card_ids": ("01094",),
        "names": ("rhino",),
        "zone": "VillainArea",
        "in_play": True,
        "exhausted": False,
        "face_up": True,
        "health": 12,
        "max_health": 14,
    }
    fields.update(overrides)
    return CardState(**fields)


def MakeState(cards=(), players=()):
    return StateView(
        round_id=1,
        phase="Player 1 Turn",
        game_over=False,
        game_over_reason="",
        players_won=None,
        cards=tuple(cards),
        players=tuple(players),
    )


################################################################################
#

class TestCaseFormat(unittest.TestCase):

    def test_unknown_given_verb_is_rejected_at_load_time(self):
        with self.assertRaises(SpecCaseError) as caught:
            GivenStep(verb="teleport", cards=("01094",))
        self.assertIn("teleport", str(caught.exception))

    def test_given_arity_is_checked(self):
        with self.assertRaises(SpecCaseError):
            # `damage` names exactly one card.
            GivenStep(verb="damage", cards=("01094", "01095"), value=2)

    def test_case_without_assertions_is_rejected(self):
        # An assertion-free case reports PASS while proving nothing.
        with self.assertRaises(SpecCaseError):
            MakeCase(then=())

    def test_json_round_trip_preserves_every_clause(self):
        case = MakeCase(
            given=(GivenStep("hand", ("01005", "01007")),
                   GivenStep("damage", ("01094",), value=3)),
            when=(WhenStep(option="Attack", targets=("Rhino",)),),
            then=(ThenStep("Rhino", "health", 11, op="<="),),
        )
        again = SpecCase.FromJson(case.ToJson())
        self.assertEqual(again.ToDict(), case.ToDict())

    def test_load_json_stamps_provenance_on_every_case(self):
        text = ('[{"name": "one", "scenario": "rhino", "heroes": ["spider_man"], '
                '"then": [{"subject": "Rhino", "prop": "health", "value": 14}]}]')
        cases = LoadJsonCases(text, source_path="specs/x.json")
        self.assertEqual(cases[0].source_path, "specs/x.json")
        self.assertEqual(cases[0].source_sha256, SourceDigest(text))

    def test_source_digest_ignores_line_endings(self):
        # Otherwise every scenario would fall out of the trusted suite on a
        # checkout with different line endings.
        self.assertEqual(SourceDigest("a\r\nb"), SourceDigest("a\nb"))

    def test_pass_step_takes_no_option(self):
        with self.assertRaises(SpecCaseError):
            WhenStep(option="Attack", pass_priority=True)


################################################################################
#

class TestCardRefParsing(unittest.TestCase):

    def test_bare_card_id(self):
        ref = CardRef.Parse("01094")
        self.assertEqual((ref.key, ref.zone, ref.ordinal), ("01094", "", 0))

    def test_zone_qualifier(self):
        ref = CardRef.Parse("Rhino in VillainArea")
        self.assertEqual((ref.key, ref.zone), ("Rhino", "VillainArea"))

    def test_ordinal(self):
        ref = CardRef.Parse("Swinging Web Kick #2")
        self.assertEqual((ref.key, ref.ordinal), ("Swinging Web Kick", 2))

    def test_ordinal_and_zone_together(self):
        ref = CardRef.Parse("01005 #3 in hand")
        self.assertEqual((ref.key, ref.ordinal, ref.zone), ("01005", 3, "hand"))

    def test_quoted_name_containing_in_is_not_split(self):
        ref = CardRef.Parse('"City in Chaos" in EncounterDeck')
        self.assertEqual((ref.key, ref.zone), ("City in Chaos", "EncounterDeck"))

    def test_empty_reference_is_an_error(self):
        with self.assertRaises(CardRefError):
            CardRef.Parse("   ")


################################################################################
#

class TestAssertions(unittest.TestCase):

    def test_passing_assertion(self):
        state = MakeState(cards=[MakeCard()])
        result = Evaluate(state, ThenStep("Rhino", "health", 12))
        self.assertTrue(result.passed)

    def test_failure_names_the_claim_and_the_board(self):
        state = MakeState(cards=[MakeCard()])
        result = Evaluate(state, ThenStep("Rhino", "health", 14))
        self.assertFalse(result.passed)
        self.assertFalse(result.unresolvable)
        self.assertIn("expected 14, got 12", result.message)
        # The board context is what makes the failure legible.
        self.assertIn("Rhino (01094) in VillainArea", result.message)
        self.assertIn("12/14 hp", result.message)

    def test_damage_is_derived_from_health(self):
        state = MakeState(cards=[MakeCard()])
        self.assertTrue(Evaluate(state, ThenStep("Rhino", "damage", 2)).passed)

    def test_comparisons_other_than_equality(self):
        state = MakeState(cards=[MakeCard()])
        self.assertTrue(Evaluate(state, ThenStep("Rhino", "health", 13, op="<")).passed)
        self.assertFalse(Evaluate(state, ThenStep("Rhino", "health", 13, op=">")).passed)

    def test_zone_comparison_ignores_case(self):
        state = MakeState(cards=[MakeCard()])
        self.assertTrue(Evaluate(state, ThenStep("Rhino", "zone", "villainarea")).passed)

    def test_missing_card_is_unresolvable_not_a_disagreement(self):
        # The difference the validation runner's verdicts turn on.
        state = MakeState(cards=[MakeCard()])
        result = Evaluate(state, ThenStep("Galactus", "health", 1))
        self.assertFalse(result.passed)
        self.assertTrue(result.unresolvable)
        self.assertIn("no card matches", result.message)

    def test_unknown_property_is_unresolvable(self):
        state = MakeState(cards=[MakeCard()])
        result = Evaluate(state, ThenStep("Rhino", "morale", 3))
        self.assertTrue(result.unresolvable)
        self.assertIn("morale", result.message)

    def test_ambiguous_card_names_every_candidate(self):
        state = MakeState(cards=[
            MakeCard(object_id=5, zone="HandsArea", name="Swinging Web Kick",
                     card_id="01005", card_ids=("01005",), names=("swinging web kick",),
                     health=None, max_health=None, in_play=False),
            MakeCard(object_id=6, zone="DiscardPile", name="Swinging Web Kick",
                     card_id="01005", card_ids=("01005",), names=("swinging web kick",),
                     health=None, max_health=None, in_play=False),
        ])
        result = Evaluate(state, ThenStep("01005", "zone", "HandsArea"))
        self.assertTrue(result.unresolvable)
        self.assertIn("matches 2 cards", result.message)
        self.assertIn("HandsArea", result.message)
        self.assertIn("DiscardPile", result.message)

    def test_zone_qualifier_disambiguates(self):
        state = MakeState(cards=[
            MakeCard(object_id=5, zone="HandsArea", name="Swinging Web Kick",
                     card_id="01005", card_ids=("01005",), names=("swinging web kick",),
                     health=None, max_health=None, in_play=False),
            MakeCard(object_id=6, zone="DiscardPile", name="Swinging Web Kick",
                     card_id="01005", card_ids=("01005",), names=("swinging web kick",),
                     health=None, max_health=None, in_play=False),
        ])
        result = Evaluate(state, ThenStep("01005 in DiscardPile", "zone", "DiscardPile"))
        self.assertTrue(result.passed)

    def test_missing_card_in_a_zone_says_where_it_actually_is(self):
        state = MakeState(cards=[MakeCard()])
        result = Evaluate(state, ThenStep("Rhino in HandsArea", "health", 12))
        self.assertIn("it is in VillainArea", result.message)

    def test_health_on_a_card_that_has_none(self):
        state = MakeState(cards=[
            MakeCard(object_id=5, name="Swinging Web Kick", card_id="01005",
                     card_ids=("01005",), names=("swinging web kick",),
                     zone="HandsArea", in_play=False, health=None, max_health=None),
        ])
        result = Evaluate(state, ThenStep("01005", "health", 1))
        self.assertTrue(result.unresolvable)
        self.assertIn("no health", result.message)

    def test_player_subject_is_one_based_as_written(self):
        state = MakeState(players=[
            PlayerState(0, "Spider-Man", 5, 10, 0, False, ""),
            PlayerState(1, "She-Hulk", 6, 12, 1, False, ""),
        ])
        self.assertTrue(Evaluate(state, ThenStep("player 1", "hand_size", 5)).passed)
        self.assertTrue(Evaluate(state, ThenStep("player 2", "hand_size", 6)).passed)
        # Bare "player" is player 1.
        self.assertTrue(Evaluate(state, ThenStep("player", "hand_size", 5)).passed)

    def test_game_subject(self):
        state = MakeState()
        self.assertTrue(Evaluate(state, ThenStep("game", "round", 1)).passed)
        self.assertTrue(Evaluate(state, ThenStep("the game", "game_over", False)).passed)

    def test_subject_resolution_reports_a_missing_player(self):
        state = MakeState(players=[PlayerState(0, "Spider-Man", 5, 10, 0, False, "")])
        _, target, error = ResolveSubject(state, "player 3")
        self.assertIsNone(target)
        self.assertIn("no player", error)


################################################################################
#

class TestStateProperties(unittest.TestCase):

    def test_counters_and_tokens_are_addressed_by_name(self):
        card = MakeCard(counters={"web": 2}, tokens={"threat": 3})
        self.assertEqual(card.Get("counter:web"), 2)
        self.assertEqual(card.Get("token:threat"), 3)
        # An absent counter reads as zero rather than raising: "has no web
        # counters" is a claim a spec should be able to make.
        self.assertEqual(card.Get("counter:glory"), 0)

    def test_engine_render_info_is_addressable(self):
        card = MakeCard(info={"is_completed": 1, "printed_stage": 2})
        self.assertEqual(card.Get("is_completed"), 1)

    def test_unknown_property_lists_what_is_known(self):
        with self.assertRaises(UnknownProperty) as caught:
            MakeCard().Get("charisma")
        self.assertIn("health", str(caught.exception))


################################################################################
# The policy on its own -- no engine, so the backstop paths are reachable.

class TestDecisionBudget(unittest.TestCase):
    """The runaway backstop, which the engine cannot be made to exercise.

    An exception raised inside `Choose` does not propagate: the engine catches
    it while broadcasting a message, logs it and keeps playing. So a backstop
    that raises is a backstop that does nothing, and it has to be tested where
    the failure is visible.
    """

    def Decide(self, *, can_cancel=True):
        from engine.device.manager.bot.policy import BotDecision
        return BotDecision(
            player_id=0, step_id=7, attempt=0, event_name="WhenPlayerInTurn",
            ability_type="Normal", prompt_text="", can_cancel=can_cancel,
            options=[], replay_input="{}", world=None,
        )

    def test_the_budget_trips_cleanly_after_the_script_has_completed(self):
        from tools.spec.policy import ScriptedPolicy
        policy = ScriptedPolicy(steps=(), max_decisions=0)

        policy.Choose(self.Decide())

        self.assertTrue(policy.halted)
        self.assertIn("gave up after 0 decisions", policy.failure)
        self.assertIn("unwinding", policy.failure)

    def test_the_budget_names_the_next_step_when_one_is_unplayed(self):
        from tools.spec.policy import ScriptedPolicy
        policy = ScriptedPolicy(steps=(WhenStep(option="Attack"),), max_decisions=0)

        policy.Choose(self.Decide())

        self.assertTrue(policy.halted)
        self.assertIn("1 When step(s) unplayed", policy.failure)
        self.assertIn("Attack", policy.failure)

    def test_a_halted_policy_declines_whatever_the_engine_still_asks(self):
        from engine.device.manager.bot.command import BotCommand
        from tools.spec.policy import ScriptedPolicy
        policy = ScriptedPolicy(steps=(), max_decisions=0)

        policy.Choose(self.Decide())
        after = policy.Choose(self.Decide(can_cancel=False))

        self.assertTrue(BotCommand.IsCancel(after))
        # The failure is recorded once, not re-reported per unwinding decision.
        self.assertEqual(policy.failure.count("gave up"), 1)


################################################################################
# End to end -- these boot the engine.

class TestAgainstTheEngine(unittest.TestCase):
    """A card behavior, expressed as a case and run against the real engine."""

    def test_basic_attack_deals_the_heros_attack_value(self):
        # Spider-Man's hero side has ATK 2; Rhino stage 1 solo has 14 hit
        # points. Attacking once should leave 12.
        case = MakeCase(
            name="basic attack",
            given=(GivenStep("hero_form", ("01001a",)),),
            when=(WhenStep(option="Attack", targets=("Rhino in VillainArea",)),),
            then=(
                ThenStep("Rhino in VillainArea", "health", 12),
                ThenStep("Rhino in VillainArea", "damage", 2),
                ThenStep("01001a", "exhausted", True),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_given_steps_put_cards_where_the_spec_says(self):
        case = MakeCase(
            name="given builds the board",
            given=(
                GivenStep("hand", ("01005", "01007")),
                GivenStep("player_deck", ("01003",)),
                GivenStep("damage", ("Rhino in VillainArea",), value=4),
                GivenStep("threat", ("The Break-In!",), value=5),
            ),
            when=(),
            then=(
                ThenStep("01005", "zone", "HandsArea"),
                ThenStep("01003", "zone", "PlayerDeck"),
                ThenStep("Rhino in VillainArea", "health", 10),
                ThenStep("The Break-In!", "threat", 5),
                ThenStep("player", "hand_size", 2),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_given_status_steps_are_declarative_not_toggles(self):
        # `RunPuzzle.Stun` flips the status. Applying it twice must still leave
        # the card stunned, or Given would not mean what it says.
        case = MakeCase(
            name="stunned twice is still stunned",
            given=(
                GivenStep("stunned", ("Rhino in VillainArea",)),
                GivenStep("stunned", ("Rhino in VillainArea",)),
            ),
            when=(),
            then=(ThenStep("Rhino in VillainArea", "stunned", True),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_a_wrong_expectation_fails_with_the_actual_value(self):
        case = MakeCase(
            name="wrong on purpose",
            given=(GivenStep("hero_form", ("01001a",)),),
            when=(WhenStep(option="Attack", targets=("Rhino in VillainArea",)),),
            then=(ThenStep("Rhino in VillainArea", "health", 14),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_ASSERTION, result.Describe())
        self.assertIn("expected 14, got 12", result.Describe())

    def test_an_action_the_engine_never_offers_says_what_it_did_offer(self):
        # Attacking from alter-ego is not a legal action, so the step never
        # matches. The failure has to name the decisions the policy saw.
        case = MakeCase(
            name="attack from alter-ego",
            given=(),
            when=(WhenStep(option="Attack", targets=("Rhino in VillainArea",)),),
            then=(ThenStep("Rhino in VillainArea", "health", 12),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("unplayed", result.Describe())
        self.assertIn("Change_Form", result.Describe())

    def test_a_then_naming_a_card_outside_the_game_is_unplayable(self):
        case = MakeCase(
            name="asserts about a card that is not here",
            given=(GivenStep("hero_form", ("01001a",)),),
            when=(),
            then=(ThenStep("Galactus", "health", 1),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())

    def test_an_ambiguous_target_is_refused_rather_than_guessed(self):
        # Two minions engaged and a spec that names neither: the harness must
        # not pick one, or the scenario's result would depend on engine
        # ordering rather than on the card.
        case = MakeCase(
            name="ambiguous attack target",
            given=(
                GivenStep("hero_form", ("01001a",)),
                GivenStep("in_play", ("01101",)),   # Hydra Mercenary
                GivenStep("in_play", ("01102",)),   # Sandman
            ),
            when=(WhenStep(option="Attack"),),
            then=(ThenStep("Rhino in VillainArea", "health", 12),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("say which", result.Describe())

    def test_naming_the_target_resolves_the_ambiguity(self):
        case = MakeCase(
            name="named attack target",
            given=(
                GivenStep("hero_form", ("01001a",)),
                GivenStep("in_play", ("01101",)),
                GivenStep("in_play", ("01102",)),
            ),
            when=(WhenStep(option="Attack", targets=("Hydra Mercenary",)),),
            then=(
                ThenStep("Hydra Mercenary", "damage", 2),
                ThenStep("Sandman", "damage", 0),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_a_given_may_bring_a_named_card_into_play(self):
        case = MakeCase(
            name="minion enters play",
            given=(GivenStep("in_play", ("01101",)),),
            when=(),
            then=(
                ThenStep("Hydra Mercenary", "in_play", True),
                ThenStep("Hydra Mercenary", "health", 3),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_a_misspelled_card_in_a_given_is_an_error_not_a_new_card(self):
        case = MakeCase(
            name="typo in a Given",
            given=(GivenStep("damage", ("Rhinoo",), value=2),),
            when=(),
            then=(ThenStep("Rhino in VillainArea", "health", 12),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("Rhinoo", result.Describe())

    def test_cases_do_not_leak_state_into_each_other(self):
        damaged = MakeCase(
            name="first case damages the villain",
            given=(GivenStep("damage", ("Rhino in VillainArea",), value=6),),
            when=(),
            then=(ThenStep("Rhino in VillainArea", "health", 8),),
        )
        fresh = MakeCase(
            name="second case starts clean",
            given=(),
            when=(),
            then=(ThenStep("Rhino in VillainArea", "health", 14),),
        )
        self.assertEqual(RunCase(damaged).outcome, OUTCOME_PASS)
        self.assertEqual(RunCase(fresh).outcome, OUTCOME_PASS)

    def test_an_ambiguous_given_never_manufactures_another_copy(self):
        # `is in play` may bring a card into the game, but only when the id
        # means nothing yet. Two copies already in hand and a bare id is the
        # author failing to say which -- answering it by creating a third
        # would be the silent first-match this harness refuses.
        case = MakeCase(
            name="ambiguous is-in-play",
            given=(
                GivenStep("hand", ("01005", "01005")),
                GivenStep("in_play", ("01005",)),
            ),
            when=(),
            then=(ThenStep("01005 #1", "zone", "HandsArea"),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("matches 2 cards", result.Describe())

    def test_an_ambiguous_when_card_says_so_rather_than_no_match(self):
        case = MakeCase(
            name="ambiguous play target",
            given=(
                GivenStep("hero_form", ("01001a",)),
                GivenStep("hand", ("01005", "01005", "01003", "01003")),
            ),
            when=(WhenStep(option="Play", card="01005"),),
            then=(ThenStep("Rhino in VillainArea", "damage", 8),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("matches 2 cards", result.Describe())

    def test_the_engines_own_play_by_play_is_kept_for_triage(self):
        case = MakeCase(
            name="engine log is captured",
            given=(GivenStep("hero_form", ("01001a",)),),
            when=(WhenStep(option="Attack", targets=("Rhino in VillainArea",)),),
            then=(ThenStep("Rhino in VillainArea", "health", 12),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())
        self.assertIn("will attack", result.engine_log)


if __name__ == "__main__":
    unittest.main()
