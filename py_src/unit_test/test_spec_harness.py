"""Tests for the puzzle-based spec harness (MARVEL-20).

Three layers, because they cost very different amounts:

- pure logic -- case loading, card-reference parsing, assertion evaluation and
  its wording -- which needs no engine at all
- the policy on its own, where the backstops are reachable
- end-to-end cases that boot the engine, build a puzzle board, drive a
  transcript through the bot device and assert on the result

The end-to-end tests are the ones that matter: they are the claim that a card
behavior can be expressed as a scenario and run. They must be run from
`py_src/`, like everything else in this repo.
"""

import unittest

from tools.spec.assertions import Evaluate, ResolveSubject
from tools.spec.case import (
    GivenStep, LoadJsonCases, NoPromptStep, PromptStep, SourceDigest, SpecCase,
    SpecCaseError, ThenStep, WhenStep)
from tools.spec.harness import (
    OUTCOME_ASSERTION, OUTCOME_PASS, OUTCOME_UNPLAYABLE, RunCase)
from tools.spec.resolve import CardRef, CardRefError, NormaliseLabel
from tools.spec.state import CardState, PlayerState, StateView, UnknownProperty

HERO_FORM = GivenStep("hero_form", ("me",))


def MakeCase(**overrides):
    fields = {
        "name": "a case",
        "scenario": "rhino",
        "heroes": ("spider_man",),
        "beats": (ThenStep("Rhino", "health", 14),),
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
            MakeCase(beats=(WhenStep(option="attack", targets=("Rhino",)),))

    def test_a_prompt_beat_needs_options(self):
        with self.assertRaises(SpecCaseError):
            PromptStep(options=())

    def test_json_round_trip_preserves_beat_order(self):
        case = MakeCase(
            given=(GivenStep("hand", ("01005", "01007")),),
            beats=(
                WhenStep(option="play", card="Nick Fury"),
                PromptStep(options=("Draw 3 cards", "Deal 4 damage to an enemy")),
                WhenStep(option="Deal 4 damage to an enemy", targets=("Shocker",)),
                ThenStep("Shocker", "damage", 4),
                NoPromptStep(),
            ),
        )
        again = SpecCase.FromJson(case.ToJson())
        self.assertEqual(again.ToDict(), case.ToDict())
        self.assertEqual([beat.kind for beat in again.beats],
                         ["when", "prompt", "when", "then", "no_prompt"])

    def test_load_json_stamps_provenance_on_every_case(self):
        text = ('[{"name": "one", "scenario": "rhino", "heroes": ["spider_man"], '
                '"beats": [{"kind": "then", "subject": "Rhino", "prop": "health", '
                '"value": 14}]}]')
        cases = LoadJsonCases(text, source_path="specs/x.json")
        self.assertEqual(cases[0].source_path, "specs/x.json")
        self.assertEqual(cases[0].source_sha256, SourceDigest(text))

    def test_source_digest_ignores_line_endings(self):
        # Otherwise every scenario would fall out of the trusted suite on a
        # checkout with different line endings.
        self.assertEqual(SourceDigest("a\r\nb"), SourceDigest("a\nb"))

    def test_card_tags_are_extracted(self):
        case = MakeCase(tags=("card:01084", "self-test"))
        self.assertEqual(case.card_tags, ("01084",))

    def test_pass_step_takes_no_option(self):
        with self.assertRaises(SpecCaseError):
            WhenStep(option="attack", pass_priority=True)


################################################################################
#

class TestLabelNormalisation(unittest.TestCase):
    """Option names are engine identifiers; a scenario reads English."""

    def test_underscores_and_case_are_ignored(self):
        self.assertEqual(NormaliseLabel("Deal_4_damage_to_an_enemy"),
                         NormaliseLabel("Deal 4 damage to an enemy"))
        self.assertEqual(NormaliseLabel("Change_Form"), NormaliseLabel("change form"))

    def test_surrounding_and_repeated_whitespace_is_ignored(self):
        self.assertEqual(NormaliseLabel("  Draw  3   cards "),
                         NormaliseLabel("Draw 3 cards"))


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
        self.assertTrue(Evaluate(state, ThenStep("Rhino", "health", 12)).passed)

    def test_failure_names_the_claim_and_the_board(self):
        state = MakeState(cards=[MakeCard()])
        result = Evaluate(state, ThenStep("Rhino", "health", 14))
        self.assertFalse(result.passed)
        self.assertFalse(result.unresolvable)
        self.assertIn("expected 14, got 12", result.message)
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
        self.assertTrue(result.unresolvable)
        self.assertIn("no card matches", result.message)

    def test_unknown_property_is_unresolvable(self):
        state = MakeState(cards=[MakeCard()])
        result = Evaluate(state, ThenStep("Rhino", "morale", 3))
        self.assertTrue(result.unresolvable)
        self.assertIn("morale", result.message)

    def test_a_name_matching_one_card_in_play_means_that_card(self):
        # Rhino stage 1 is on the board, stage 2 is still in the villain deck.
        # "Rhino" means the one being fought.
        state = MakeState(cards=[
            MakeCard(object_id=3),
            MakeCard(object_id=4, card_id="01095", card_ids=("01095",),
                     zone="VillainDeck", in_play=False, health=0, max_health=0),
        ])
        self.assertTrue(Evaluate(state, ThenStep("Rhino", "health", 12)).passed)

    def test_two_cards_in_play_is_still_ambiguous(self):
        state = MakeState(cards=[
            MakeCard(object_id=5, zone="HandsArea", name="Swinging Web Kick",
                     card_id="01005", card_ids=("01005",), names=("swinging web kick",),
                     health=None, max_health=None, in_play=True),
            MakeCard(object_id=6, zone="AlliesArea", name="Swinging Web Kick",
                     card_id="01005", card_ids=("01005",), names=("swinging web kick",),
                     health=None, max_health=None, in_play=True),
        ])
        result = Evaluate(state, ThenStep("01005", "zone", "HandsArea"))
        self.assertTrue(result.unresolvable)
        self.assertIn("matches 2 cards", result.message)

    def test_zone_qualifier_disambiguates(self):
        state = MakeState(cards=[
            MakeCard(object_id=5, zone="HandsArea", name="Swinging Web Kick",
                     card_id="01005", card_ids=("01005",), names=("swinging web kick",),
                     health=None, max_health=None, in_play=False),
            MakeCard(object_id=6, zone="DiscardPile", name="Swinging Web Kick",
                     card_id="01005", card_ids=("01005",), names=("swinging web kick",),
                     health=None, max_health=None, in_play=False),
        ])
        self.assertTrue(
            Evaluate(state, ThenStep("01005 in DiscardPile", "zone", "DiscardPile")).passed)

    def test_missing_card_in_a_zone_says_where_it_actually_is(self):
        state = MakeState(cards=[MakeCard()])
        result = Evaluate(state, ThenStep("Rhino in HandsArea", "health", 12))
        self.assertIn("it is in VillainArea", result.message)

    def test_me_resolves_to_the_identity(self):
        state = MakeState(cards=[
            MakeCard(),
            MakeCard(object_id=1, name="Spider-Man", card_id="01001a",
                     card_ids=("01001a", "01001b"), names=("spider-man", "peter parker"),
                     zone="HeroArea", health=10, max_health=10,
                     is_identity=True, is_hero_form=True),
        ])
        self.assertTrue(Evaluate(state, ThenStep("me", "hero_form", True)).passed)
        self.assertTrue(Evaluate(state, ThenStep("me", "damage", 0)).passed)

    def test_the_main_scheme_resolves_by_role(self):
        state = MakeState(cards=[
            MakeCard(object_id=2, name="The Break-In!", card_id="01097b",
                     card_ids=("01097b",), names=("the break-in!",),
                     zone="MainSchemesArea", health=None, max_health=None,
                     threat=5, is_main_scheme=True),
        ])
        self.assertTrue(Evaluate(state, ThenStep("the main scheme", "threat", 5)).passed)

    def test_form_on_a_card_that_is_not_an_identity(self):
        state = MakeState(cards=[MakeCard()])
        result = Evaluate(state, ThenStep("Rhino", "hero_form", True))
        self.assertTrue(result.unresolvable)
        self.assertIn("not an identity", result.message)

    def test_player_subject_is_one_based_as_written(self):
        state = MakeState(players=[
            PlayerState(0, "Spider-Man", 5, 10, 0, False, ""),
            PlayerState(1, "She-Hulk", 6, 12, 1, False, ""),
        ])
        self.assertTrue(Evaluate(state, ThenStep("player 1", "hand_size", 5)).passed)
        self.assertTrue(Evaluate(state, ThenStep("player 2", "hand_size", 6)).passed)
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
# The policy on its own -- no engine, so the backstops are reachable.

class TestDecisionBudget(unittest.TestCase):
    """The runaway backstop, which the engine cannot be made to exercise.

    An exception raised inside `Choose` does not propagate: the engine catches
    it while broadcasting a message, logs it and keeps playing. So a backstop
    that raises is a backstop that does nothing, and it has to be tested where
    the failure is visible.
    """

    def Decide(self, *, can_cancel=True, event="WhenPlayerInTurn"):
        from engine.device.manager.bot.policy import BotDecision
        return BotDecision(
            player_id=0, step_id=7, attempt=0, event_name=event,
            ability_type="Normal", prompt_text="", can_cancel=can_cancel,
            options=[], replay_input="{}", world=None,
        )

    def test_the_budget_trips_cleanly_after_the_transcript_has_finished(self):
        from tools.spec.policy import TranscriptPolicy
        policy = TranscriptPolicy(beats=(), max_decisions=0)

        policy.Choose(self.Decide())

        self.assertTrue(policy.halted)
        self.assertIn("gave up after 0 decisions", policy.failure)
        self.assertIn("unwinding", policy.failure)

    def test_the_budget_counts_the_beats_left_over(self):
        from tools.spec.policy import TranscriptPolicy
        policy = TranscriptPolicy(beats=(WhenStep(option="attack"),), max_decisions=0)

        policy.Choose(self.Decide())

        self.assertTrue(policy.halted)
        self.assertIn("1 beat(s) unplayed", policy.failure)

    def test_a_halted_policy_declines_whatever_the_engine_still_asks(self):
        from engine.device.manager.bot.command import BotCommand
        from tools.spec.policy import TranscriptPolicy
        policy = TranscriptPolicy(beats=(), max_decisions=0)

        policy.Choose(self.Decide())
        after = policy.Choose(self.Decide(can_cancel=False))

        self.assertTrue(BotCommand.IsCancel(after))
        # The failure is recorded once, not re-reported per unwinding decision.
        self.assertEqual(policy.failure.count("gave up"), 1)


################################################################################
# End to end -- these boot the engine.

class TestAgainstTheEngine(unittest.TestCase):
    """A card behavior, expressed as a transcript and run against the engine."""

    def test_basic_attack_deals_the_heros_attack_value(self):
        # Spider-Man's hero side has ATK 2; Rhino stage 1 solo has 14 hit
        # points. Attacking once should leave 12.
        case = MakeCase(
            name="basic attack",
            given=(HERO_FORM,),
            beats=(
                WhenStep(option="attack", targets=("Rhino",)),
                ThenStep("Rhino", "health", 12),
                ThenStep("Rhino", "damage", 2),
                ThenStep("me", "exhausted", True),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_given_steps_put_cards_where_the_scenario_says(self):
        case = MakeCase(
            name="given builds the board",
            given=(
                GivenStep("hand", ("Swinging Web Kick", "Spider-Tracer")),
                GivenStep("player_deck", ("Backflip",)),
                GivenStep("damage", ("Rhino",), value=4),
                GivenStep("threat", ("the main scheme",), value=5),
            ),
            beats=(
                ThenStep("Swinging Web Kick", "zone", "HandsArea"),
                ThenStep("Backflip", "zone", "PlayerDeck"),
                ThenStep("Rhino", "health", 10),
                ThenStep("the main scheme", "threat", 5),
                ThenStep("player", "hand_size", 2),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_names_come_from_the_printed_dataset_not_the_engine(self):
        # The engine's `data/cards.json` has 21141 and 21142 holding each
        # other's names. Resolving a correctly spelled scenario against it would
        # silently produce the wrong card -- the failure MARVEL-19 exists to
        # prevent, and why AGENTS.md forbids authoring from that file.
        from cards.database import CardsDB
        from tools.spec.harness import EnsureEngine, ResolveCardId

        EnsureEngine()
        self.assertEqual(ResolveCardId("Hall of Nastrond"), "21141")
        self.assertEqual(ResolveCardId("Gjallerbru"), "21142")
        # The engine disagrees, which is the point of not asking it.
        self.assertEqual(str(CardsDB.papers["21141"].name), "Gjallerbru")

    def test_a_name_only_the_engine_uses_does_not_resolve(self):
        from tools.spec.harness import EnsureEngine, ResolveCardId, SetupError

        EnsureEngine()
        with self.assertRaises(SetupError):
            # The engine's spelling of 27100b; the card says "Synchronization".
            ResolveCardId("Sinister Synchonization")

    def test_printed_names_resolve_to_card_ids(self):
        case = MakeCase(
            name="named cards",
            given=(GivenStep("hand", ("Swinging Web Kick",)),),
            beats=(ThenStep("01005", "zone", "HandsArea"),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_a_name_that_means_several_cards_asks_for_the_id(self):
        case = MakeCase(
            name="ambiguous printed name",
            given=(GivenStep("hand", ("Rhino",)),),
            beats=(ThenStep("Rhino", "zone", "HandsArea"),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("use the card id", result.Describe())

    def test_given_status_steps_are_declarative_not_toggles(self):
        # `RunPuzzle.Stun` flips the status. Applying it twice must still leave
        # the card stunned, or Given would not mean what it says.
        case = MakeCase(
            name="stunned twice is still stunned",
            given=(
                GivenStep("stunned", ("Rhino",)),
                GivenStep("stunned", ("Rhino",)),
            ),
            beats=(ThenStep("Rhino", "stunned", True),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_a_wrong_expectation_fails_with_the_actual_value(self):
        case = MakeCase(
            name="wrong on purpose",
            given=(HERO_FORM,),
            beats=(
                WhenStep(option="attack", targets=("Rhino",)),
                ThenStep("Rhino", "health", 14),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_ASSERTION, result.Describe())
        self.assertIn("expected 14, got 12", result.Describe())

    def test_an_action_the_engine_never_offers_says_what_it_did_offer(self):
        case = MakeCase(
            name="attack from alter-ego",
            given=(),
            beats=(
                WhenStep(option="attack", targets=("Rhino",)),
                ThenStep("Rhino", "health", 12),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("Change_Form", result.Describe())

    def test_an_ambiguous_target_is_refused_rather_than_guessed(self):
        case = MakeCase(
            name="ambiguous attack target",
            given=(
                HERO_FORM,
                GivenStep("in_play", ("Hydra Mercenary",)),
                GivenStep("in_play", ("Sandman",)),
            ),
            beats=(
                WhenStep(option="attack"),
                ThenStep("Rhino", "health", 12),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("say which", result.Describe())

    def test_an_ambiguous_given_never_manufactures_another_copy(self):
        case = MakeCase(
            name="ambiguous is-in-play",
            given=(
                GivenStep("hand", ("01005", "01005")),
                GivenStep("in_play", ("01005",)),
            ),
            beats=(ThenStep("01005 #1", "zone", "HandsArea"),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("matches 2 cards", result.Describe())

    def test_a_misspelled_card_in_a_given_is_an_error_not_a_new_card(self):
        case = MakeCase(
            name="typo in a Given",
            given=(GivenStep("damage", ("Rhinoo",), value=2),),
            beats=(ThenStep("Rhino", "health", 12),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("Rhinoo", result.Describe())

    def test_cases_do_not_leak_state_into_each_other(self):
        damaged = MakeCase(
            name="first case damages the villain",
            given=(GivenStep("damage", ("Rhino",), value=6),),
            beats=(ThenStep("Rhino", "health", 8),),
        )
        fresh = MakeCase(
            name="second case starts clean",
            beats=(ThenStep("Rhino", "health", 14),),
        )
        self.assertEqual(RunCase(damaged).outcome, OUTCOME_PASS)
        self.assertEqual(RunCase(fresh).outcome, OUTCOME_PASS)

    def test_the_engines_own_play_by_play_is_kept_for_triage(self):
        case = MakeCase(
            name="engine log is captured",
            given=(HERO_FORM,),
            beats=(
                WhenStep(option="attack", targets=("Rhino",)),
                ThenStep("Rhino", "health", 12),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())
        self.assertIn("will attack", result.engine_log)


################################################################################
# Duplicate card names (MARVEL-42).

class TestDuplicateNames(unittest.TestCase):
    """What `#N` counts, and what it refuses to count.

    The contract is "the Nth copy the scenario created". These tests pin the
    two halves of that: ordinals work over cards a Given made, and are refused
    over cards the engine made, where the order is allocation order and nothing
    in the scenario decides it.
    """

    def test_two_copies_in_one_zone_are_addressable_by_ordinal(self):
        case = MakeCase(
            name="two copies in hand",
            given=(GivenStep("hand", ("Backflip", "Backflip")),),
            beats=(
                ThenStep("Backflip #1", "zone", "HandsArea"),
                ThenStep("Backflip #2", "zone", "HandsArea"),
                ThenStep("player", "hand_size", 2),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_an_ordinal_past_the_last_copy_is_unplayable(self):
        case = MakeCase(
            name="ordinal overruns",
            given=(GivenStep("hand", ("Backflip", "Backflip")),),
            beats=(ThenStep("Backflip #3", "zone", "HandsArea"),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("wanted copy #3", result.Describe())

    def test_an_ordinal_over_engine_made_cards_is_refused(self):
        # Rhino stage 1 is in play and stage 2 sits in the villain deck. Both
        # were allocated during setup, so "#1" would mean whichever the
        # allocator reached first -- not something the scenario says.
        case = MakeCase(
            name="ordinal over the villain stages",
            beats=(ThenStep("Rhino #2", "zone", "VillainDeck"),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("the scenario did not create", result.Describe())

    def test_a_redundant_ordinal_on_a_narrowed_ref_is_allowed(self):
        # Once the zone has narrowed the match to one card the ordinal has
        # nothing to choose between, so it is redundant rather than unsafe.
        case = MakeCase(
            name="ordinal after a zone qualifier",
            beats=(ThenStep("Rhino #1 in VillainArea", "health", 14),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_a_shuffle_does_not_change_which_card_an_ordinal_names(self):
        # Position within a zone moves under a shuffle; creation order does not.
        # This is why the ordinal counts creation order and not deck position --
        # shuffles are RNG-driven and the engines do not share an RNG yet.
        from tools.spec.harness import ApplyGiven, EnsureEngine, NewGameForCase
        from tools.spec.policy import TranscriptPolicy
        from tools.spec.resolve import ResolveCard

        EnsureEngine()
        case = MakeCase(
            name="shuffle stability",
            given=(GivenStep("player_deck",
                             ("Backflip", "Enhanced Spider-Sense", "Backflip")),),
        )
        game = NewGameForCase(case, TranscriptPolicy())
        self.assertTrue(game.GameSetup())
        world = game.world
        ApplyGiven(world, case)

        before = [ResolveCard(world, f"Backflip #{n}").object_id for n in (1, 2)]

        from game.effect.rule import DebugRule
        player = world.GetFirstPlayer()
        player.player_deck.Shuffle(DebugRule(player.GetIdentity()))

        after = [ResolveCard(world, f"Backflip #{n}").object_id for n in (1, 2)]
        self.assertEqual(before, after)

    def test_saying_a_card_is_in_play_twice_is_refused(self):
        # Given is declarative, so the second step resolves to the card the
        # first created and does nothing. The scenario would run with one
        # minion while reading as though it had two.
        case = MakeCase(
            name="repeated is in play",
            given=(GivenStep("in_play", ("Hydra Mercenary",)),
                   GivenStep("in_play", ("Hydra Mercenary",))),
            beats=(ThenStep("Hydra Mercenary #2", "health", 3),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("already in play", result.Describe())

    def test_two_copies_can_both_be_put_into_play_by_ordinal(self):
        # The supported way to get two of the same minion onto the board. The
        # ordinals stay valid after the first copy moves zones, because they
        # track which cards the scenario created rather than where they are.
        case = MakeCase(
            name="two minions in play",
            given=(
                GivenStep("encounter_deck", ("Hydra Mercenary", "Hydra Mercenary")),
                GivenStep("in_play", ("Hydra Mercenary #1",)),
                GivenStep("in_play", ("Hydra Mercenary #2",)),
            ),
            beats=(
                ThenStep("Hydra Mercenary #1", "zone", "EngagedEnemiesArea"),
                ThenStep("Hydra Mercenary #2", "zone", "EngagedEnemiesArea"),
                ThenStep("Hydra Mercenary #1", "health", 3),
                ThenStep("Hydra Mercenary #2", "health", 3),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_a_bare_name_with_two_copies_in_play_is_still_ambiguous(self):
        case = MakeCase(
            name="bare name, two copies",
            given=(
                GivenStep("encounter_deck", ("Hydra Mercenary", "Hydra Mercenary")),
                GivenStep("in_play", ("Hydra Mercenary #1",)),
                GivenStep("in_play", ("Hydra Mercenary #2",)),
            ),
            beats=(ThenStep("Hydra Mercenary", "health", 3),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("matches 2 cards", result.Describe())


################################################################################
# The transcript shape itself.

class TestTranscripts(unittest.TestCase):
    """Assertions between decisions, and refusing to answer for the author."""

    NICK_FURY = (
        HERO_FORM,
        GivenStep("hand", ("Nick Fury", "Backflip", "Backflip", "Webbed Up",
                           "Enhanced Spider-Sense")),
        GivenStep("in_play", ("Shocker",)),
    )

    def test_a_mid_resolution_choice_is_the_scenarios_to_make(self):
        case = MakeCase(
            name="nick fury deals damage",
            tags=("card:01084",),
            given=self.NICK_FURY,
            beats=(
                WhenStep(option="play", card="Nick Fury"),
                PromptStep(options=("Draw 3 cards", "Deal 4 damage to an enemy")),
                WhenStep(option="Deal 4 damage to an enemy", targets=("Shocker",)),
                ThenStep("Shocker", "damage", 4),
                NoPromptStep(),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_the_harness_never_answers_a_choice_the_transcript_omits(self):
        # The whole reason a scenario is a transcript. Batching the actions and
        # asserting at the end would let the harness pick an option and report
        # a pass for a scenario that specified nothing.
        case = MakeCase(
            name="nick fury, choice omitted",
            tags=("card:01084",),
            given=self.NICK_FURY,
            beats=(
                WhenStep(option="play", card="Nick Fury"),
                ThenStep("Shocker", "damage", 4),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("still asking", result.Describe())

    def test_a_wrong_option_table_fails_and_names_the_difference(self):
        case = MakeCase(
            name="nick fury, wrong options",
            tags=("card:01084",),
            given=self.NICK_FURY,
            beats=(
                WhenStep(option="play", card="Nick Fury"),
                PromptStep(options=("Draw 3 cards", "Remove 2 threat from a scheme")),
                WhenStep(option="Draw 3 cards"),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_ASSERTION, result.Describe())
        self.assertIn("missing 'remove 2 threat from a scheme'", result.Describe())

    def test_not_prompted_again_fails_when_the_engine_asks_again(self):
        case = MakeCase(
            name="nick fury still asks",
            tags=("card:01084",),
            given=self.NICK_FURY,
            beats=(
                WhenStep(option="play", card="Nick Fury"),
                NoPromptStep(),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_ASSERTION, result.Describe())
        self.assertIn("expected no further prompt", result.Describe())

    def test_not_prompted_again_passes_when_the_turn_menu_returns(self):
        # The turn menu coming back is not the card asking another question.
        case = MakeCase(
            name="attack finishes cleanly",
            given=(HERO_FORM,),
            beats=(
                WhenStep(option="attack", targets=("Rhino",)),
                NoPromptStep(),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_assertions_between_decisions_see_the_intermediate_board(self):
        # After the play but before the choice resolves, Shocker is undamaged.
        # Only an assertion evaluated at that decision can see it.
        case = MakeCase(
            name="board between beats",
            tags=("card:01084",),
            given=self.NICK_FURY,
            beats=(
                WhenStep(option="play", card="Nick Fury"),
                ThenStep("Shocker", "damage", 0),
                PromptStep(options=("Draw 3 cards", "Deal 4 damage to an enemy")),
                WhenStep(option="Deal 4 damage to an enemy", targets=("Shocker",)),
                ThenStep("Shocker", "damage", 4),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())


if __name__ == "__main__":
    unittest.main()
