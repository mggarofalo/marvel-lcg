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
from unittest import mock
from types import SimpleNamespace

from tools.spec.assertions import Evaluate, ResolveSubject
from tools.spec.case import (
    CannotStep, GivenStep, LimitStep, LoadJsonCases, MinimumStep, NoPromptStep,
    NotOfferedStep, PromptStep, SourceDigest, SpecCase, SpecCaseError,
    TargetsStep, ThenStep, WhenStep)
from tools.spec.harness import (
    OUTCOME_ASSERTION, OUTCOME_PASS, OUTCOME_UNPLAYABLE, RunCase)
from tools.spec.resolve import CardRef, CardRefError, NormaliseLabel
from tools.spec.state import (
    PHASE_GROUPS, PHASE_NAMES, CardState, PlayerState, StateView, UnknownProperty)

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


def MakeIdentity(**overrides):
    """An identity card. Seat 1's unless a test says otherwise."""
    fields = {
        "object_id": 1,
        "name": "Spider-Man",
        "card_id": "01001a",
        "card_ids": ("01001a", "01001b"),
        "names": ("spider-man", "peter parker"),
        "zone": "HeroArea",
        "health": 10,
        "max_health": 10,
        "is_identity": True,
        "is_hero_form": True,
    }
    fields.update(overrides)
    return MakeCard(**fields)


def MakePlayer(**overrides):
    fields = {
        "player_id": 0,
        "identity": "Spider-Man",
        "hand_size": 5,
        "deck_size": 10,
        "discard_size": 0,
        "eliminated": False,
        "resources": "",
        "identity_object_id": 1,
    }
    fields.update(overrides)
    return PlayerState(**fields)


def MakeState(cards=(), players=(), phase="Player Turn"):
    return StateView(
        round_id=1,
        phase=phase,
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
                MinimumStep(option="Deal 4 damage to an enemy", minimum=1,
                            card="Nick Fury"),
                ThenStep("Shocker", "damage", 4),
                NoPromptStep(),
            ),
        )
        again = SpecCase.FromJson(case.ToJson())
        self.assertEqual(again.ToDict(), case.ToDict())
        self.assertEqual([beat.kind for beat in again.beats],
                         ["when", "prompt", "when", "minimum", "then",
                          "no_prompt"])

    def test_a_legal_targets_beat_round_trips_with_its_card_binding(self):
        # The `targets` kind was write-only until MARVEL-94's follow-up; the
        # card binding it gained in MARVEL-141 has to survive the same trip.
        case = MakeCase(beats=(TargetsStep(option="Play", card="01043a",
                                           targets=("Panther Claws",)),))
        again = SpecCase.FromJson(case.ToJson())
        self.assertEqual(again.ToDict(), case.ToDict())
        beat = again.beats[0]
        self.assertEqual((beat.option, beat.card, beat.targets),
                         ("Play", "01043a", ("Panther Claws",)))

    def test_an_unbound_legal_targets_beat_omits_the_card_from_json(self):
        # An optional field written as "" would churn every stored case that
        # predates the binding.
        beat = TargetsStep(option="Futurist", targets=("Repulsor Blast",))
        self.assertNotIn("card", beat.ToDict())
        self.assertEqual(beat.Describe(),
                         "the legal targets for 'Futurist' are 'Repulsor Blast'")

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
        state = MakeState(
            cards=[
                MakeCard(),
                MakeIdentity(object_id=1),
            ],
            players=[MakePlayer(identity_object_id=1)],
        )
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


class TestPhaseAssertions(unittest.TestCase):
    """The two grains a scenario can name a phase at.

    `Phase.State` is not imported here on purpose. These are the strings the C#
    runner has to reproduce, so the test pins the literal wire and a rename on
    the engine side has to be a deliberate edit in two places rather than a
    silent follow.
    """

    def test_a_state_reads_back_as_written(self):
        self.assertEqual(MakeState(phase="Enemy Activation").Get("phase"),
                         "Enemy Activation")

    def test_each_engine_state_belongs_to_a_rulebook_phase(self):
        cases = {
            "Player Turn":               "player",
            "Player Turn End":           "player",
            "Main Scheme Place Threat":  "villain",
            "Enemy Activation":          "villain",
            "Deal Encounter Cards":      "villain",
            "Reveal Encounter Cards":    "villain",
            "End Phase":                 "end",
            "End Round":                 "end",
        }
        for state, group in cases.items():
            with self.subTest(state=state):
                self.assertEqual(MakeState(phase=state).Get("phase_group"), group)

    def test_every_engine_phase_state_is_classified(self):
        """A `Phase.State` nobody grouped would answer "no" to every phase.

        This is the check that catches a new state being added to the engine
        without anyone deciding which phase it belongs to -- at which point
        `it is the villain phase` starts failing for a reason that has nothing
        to do with the scenario.
        """
        from game.world.phase import Phase

        missing = [member.value for member in Phase.State
                   if member.value not in PHASE_GROUPS]
        self.assertEqual(missing, [], "unclassified Phase.State values")

    def test_an_unclassified_phase_raises_rather_than_answering_no(self):
        with self.assertRaises(UnknownProperty) as caught:
            MakeState(phase="Interdimensional Tea Break").Get("phase_group")
        self.assertIn("PHASE_GROUPS", str(caught.exception))

    def test_the_grouping_only_names_phases_the_vocabulary_offers(self):
        self.assertEqual(set(PHASE_NAMES), {"player", "villain", "end"})
        # "setup" and "start" are real groups but no step spells them: a
        # scenario runs inside GameLoop and never observes them. They stay in
        # the mapping so `phase_group` can answer for them rather than raise.
        self.assertLessEqual(set(PHASE_NAMES), set(PHASE_GROUPS.values()))


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

    def test_not_offered_needs_a_decision_to_observe(self):
        """A game ending first cannot prove that an option was absent."""
        from tools.spec.policy import TranscriptPolicy
        policy = TranscriptPolicy(
            beats=(NotOfferedStep(option="Action", card="Vision"),))

        policy.Finish(None)

        self.assertEqual(len(policy.results), 1)
        self.assertFalse(policy.results[0].passed)
        self.assertTrue(policy.results[0].unresolvable)

    def test_an_option_assertion_needs_a_board_to_read(self):
        """Every option assertion reads the world, so a decision without one
        resolves nothing -- not even the label match it could have made."""
        from tools.spec.policy import TranscriptPolicy
        for beat in (TargetsStep(option="Play", targets=("Rhino",)),
                     MinimumStep(option="Play", minimum=1),
                     LimitStep(option="Play", maximum=1)):
            with self.subTest(beat=beat.kind):
                policy = TranscriptPolicy(beats=(beat,))

                policy.Choose(self.Decide())

                self.assertEqual(len(policy.results), 1)
                self.assertFalse(policy.results[0].passed)
                self.assertTrue(policy.results[0].unresolvable)
                self.assertIn("no board", policy.results[0].message)


################################################################################
# End to end -- these boot the engine.

class TestEffectiveTargetRange(unittest.TestCase):
    """The transcript contract is the range the selector presents to clients."""

    def test_a_raw_floor_above_the_clamped_ceiling_becomes_the_ceiling(self):
        """`Selector.GetTargetRange` crosses 3..2 into the effective 2..2."""
        from game.selector.selector import Selector

        selector = SimpleNamespace(selector_range=SimpleNamespace(
            GetTargetMin=lambda effect, faces: 3,
            GetTargetMax=lambda effect, faces: 2,
        ))
        self.assertEqual(
            Selector.GetTargetRange(selector, object(), [object()] * 4),
            (2, 2))


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

    def test_an_each_selector_exposes_the_candidate_count_as_its_floor(self):
        """The card binding ignores another Play option listed first."""
        case = MakeCase(
            name="each target minimum",
            heroes=("black_panther",),
            given=(
                HERO_FORM,
                GivenStep("hand", (
                    "Haymaker", "01043a", "Vibranium", "Vibranium")),
                GivenStep("in_play", ("Panther Claws",)),
                GivenStep("in_play", ("Tactical Genius",)),
            ),
            beats=(MinimumStep(option="Play", minimum=2, card="01043a"),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_a_wrong_each_selector_floor_fails_the_assertion(self):
        """Mutation control: the new assertion must not pass unconditionally."""
        case = MakeCase(
            name="wrong each target minimum",
            heroes=("black_panther",),
            given=(
                HERO_FORM,
                GivenStep("hand", (
                    "Haymaker", "01043a", "Vibranium", "Vibranium")),
                GivenStep("in_play", ("Panther Claws",)),
                GivenStep("in_play", ("Tactical Genius",)),
            ),
            beats=(MinimumStep(option="Play", minimum=1, card="01043a"),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_ASSERTION, result.Describe())
        self.assertIn("Play takes 2..2 target(s)", result.Failures()[0].message)

    def test_a_card_bound_floor_does_not_inspect_an_unrelated_play_option(self):
        """Wakanda is filtered out; Haymaker must not satisfy its assertion."""
        case = MakeCase(
            name="filtered each target minimum",
            heroes=("black_panther",),
            given=(
                HERO_FORM,
                GivenStep("hand", (
                    "01043a", "Haymaker", "Vibranium", "Vibranium")),
            ),
            beats=(MinimumStep(option="Play", minimum=1, card="01043a"),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("not offering 'Play' on '01043a'", result.Describe())

    def test_an_unbound_floor_with_two_matching_options_is_unresolvable(self):
        """Enumeration order cannot decide which card an assertion reads."""
        case = MakeCase(
            name="ambiguous target minimum",
            heroes=("black_panther",),
            given=(
                HERO_FORM,
                GivenStep("hand", (
                    "Haymaker", "01043a", "Vibranium", "Vibranium")),
                GivenStep("in_play", ("Panther Claws",)),
                GivenStep("in_play", ("Tactical Genius",)),
            ),
            beats=(MinimumStep(option="Play", minimum=2),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("matches 2 offered options", result.Describe())

    ############################################################################
    # MARVEL-141. The same four claims for `the legal targets for`, which had
    # the defect MARVEL-134 fixed for the two range steps. Haymaker and Wakanda
    # Forever! both offer `Play` and share no legal target, so a mis-bound
    # assertion cannot pass by coincidence.
    #
    def test_a_card_bound_target_list_reads_the_card_it_names(self):
        """Haymaker is listed first and answers for enemies, not upgrades."""
        case = MakeCase(
            name="bound legal targets",
            heroes=("black_panther",),
            given=(
                HERO_FORM,
                GivenStep("hand", (
                    "Haymaker", "01043a", "Vibranium", "Vibranium")),
                GivenStep("in_play", ("Panther Claws",)),
            ),
            beats=(
                TargetsStep(option="Play", card="01043a",
                            targets=("Panther Claws",)),
                TargetsStep(option="Play", card="Haymaker",
                            targets=("Rhino",)),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_a_wrong_card_bound_target_list_fails_the_assertion(self):
        """Mutation control: the binding must not pass unconditionally."""
        case = MakeCase(
            name="wrong bound legal targets",
            heroes=("black_panther",),
            given=(
                HERO_FORM,
                GivenStep("hand", (
                    "Haymaker", "01043a", "Vibranium", "Vibranium")),
                GivenStep("in_play", ("Panther Claws",)),
            ),
            beats=(TargetsStep(option="Play", card="Haymaker",
                               targets=("Panther Claws",)),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_ASSERTION, result.Describe())
        self.assertIn("missing 'panther claws'", result.Failures()[0].message)

    def test_a_card_bound_target_list_does_not_inspect_another_play_option(self):
        """Wakanda is filtered out; Haymaker must not answer in its place."""
        case = MakeCase(
            name="filtered legal targets",
            heroes=("black_panther",),
            given=(
                HERO_FORM,
                GivenStep("hand", (
                    "01043a", "Haymaker", "Vibranium", "Vibranium")),
            ),
            beats=(TargetsStep(option="Play", card="01043a",
                               targets=("Panther Claws",)),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("not offering 'Play' on '01043a'", result.Describe())

    def test_an_unbound_target_list_with_two_matching_options_is_unresolvable(self):
        """Enumeration order cannot decide which card an assertion reads."""
        case = MakeCase(
            name="ambiguous legal targets",
            heroes=("black_panther",),
            given=(
                HERO_FORM,
                GivenStep("hand", (
                    "Haymaker", "01043a", "Vibranium", "Vibranium")),
                GivenStep("in_play", ("Panther Claws",)),
            ),
            beats=(TargetsStep(option="Play", targets=("Panther Claws",)),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("matches 2 offered options", result.Describe())

    def test_an_explicit_variable_payment_controls_the_effect(self):
        case = MakeCase(
            name="explicit variable payment",
            heroes=("quicksilver",),
            given=(
                HERO_FORM,
                GivenStep("in_play", ("Hydra Mercenary",)),
                GivenStep("hand", (
                    "Speed Cyclone", "Always Be Running", "Always Be Running")),
            ),
            beats=(
                WhenStep(
                    option="Play", card="Speed Cyclone", payment=1,
                    targets=("Rhino", "Hydra Mercenary")),
                ThenStep("Rhino", "stunned", True),
                ThenStep("Hydra Mercenary", "stunned", False),
                ThenStep("player", "hand_size", 1),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_an_unaffordable_explicit_payment_is_unplayable(self):
        """The runner must not fall back to its maximal variable payment."""
        case = MakeCase(
            name="unaffordable explicit payment",
            heroes=("quicksilver",),
            given=(
                HERO_FORM,
                GivenStep("in_play", ("Hydra Mercenary",)),
                GivenStep("hand", (
                    "Speed Cyclone", "Always Be Running", "Always Be Running")),
            ),
            beats=(
                WhenStep(
                    option="Play", card="Speed Cyclone", payment=3,
                    targets=("Rhino", "Hydra Mercenary")),
                ThenStep("player", "hand_size", 0),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("cannot be paid with exactly 3 resources", result.Describe())

    def test_a_dynamic_resource_ability_must_generate_the_exact_amount(self):
        """SP//dr Suit advertises every Interface, then exhausts only those chosen."""
        case = MakeCase(
            name="dynamic exact payment",
            heroes=("sp_dr",),
            given=(
                HERO_FORM,
                GivenStep("in_play", ("Host Spider",)),
                GivenStep("in_play", ("Hydra Mercenary",)),
                GivenStep("hand", ("Speed Cyclone",)),
            ),
            beats=(
                WhenStep(
                    option="Play", card="Speed Cyclone", payment=2,
                    targets=("Rhino", "Hydra Mercenary")),
                WhenStep(
                    option="Pay cost Exhaust", card="SP//dr Suit",
                    targets=("Host Spider",)),
                ThenStep("Rhino", "stunned", True),
                ThenStep("Hydra Mercenary", "stunned", False),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn(
            "required exactly 2 resources, but the selected payment effects "
            "generated 1", result.Describe())

    def test_reusing_an_effect_keeps_each_exact_payment_separate(self):
        case = MakeCase(
            name="same effect twice",
            heroes=("captain_marvel",),
            given=(
                HERO_FORM,
                GivenStep("in_play", ("Energy Channel",)),
                GivenStep("hand", (
                    "Crisis Interdiction", "Crisis Interdiction",
                    "Crisis Interdiction")),
            ),
            beats=(
                WhenStep(option="Action", card="Energy Channel", payment=1),
                ThenStep("Energy Channel", "counter:energy", 1),
                WhenStep(option="Action", card="Energy Channel", payment=2),
                ThenStep("Energy Channel", "counter:energy", 3),
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

    def test_final_scheme_loss_has_no_world_level_rescue_path(self):
        """Mutation control: suppressing the printed rule must kill the case."""
        from game.world.game_over import GameOverReason

        case = MakeCase(
            name="final scheme completion owner",
            scenario="klaw",
            heroes=("captain_marvel",),
            given=(
                HERO_FORM,
                GivenStep("encounter_deck", (
                    "Armored Guard", "Armored Guard", "Armored Guard")),
                GivenStep("threat", ("the main scheme",), value=6),
                GivenStep("threat", ("the main scheme",), value=8),
            ),
            beats=(
                ThenStep("game", "game_over", True),
                ThenStep("game", "players_won", False),
            ),
        )
        original = GameOverReason.SetGameOver

        def SuppressPrintedLoss(reason, value, by_effect):
            if value == "The Main Scheme was Completed":
                return None
            return original(reason, value, by_effect)

        with mock.patch.object(GameOverReason, "SetGameOver", SuppressPrintedLoss):
            result = RunCase(case)

        self.assertNotEqual(result.outcome, OUTCOME_PASS, result.Describe())
        self.assertIn("final main scheme completed without a loss ability",
                      result.Describe())

    def test_a_restriction_passes_when_the_target_is_filtered_out(self):
        """Guard: the option stays, the villain leaves its legal targets."""
        case = MakeCase(
            name="guard",
            given=(
                HERO_FORM,
                GivenStep("encounter_deck", ("Hydra Mercenary", "Hydra Mercenary")),
                GivenStep("in_play", ("Hydra Mercenary #1",)),
            ),
            beats=(CannotStep(option="attack", card="Rhino"),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_a_restriction_passes_when_the_action_has_no_legal_target(self):
        """Stun, which this engine expresses the same way Guard does.

        A stunned hero is still offered `Attack`; `all_legal_targets` is empty.
        Worth its own test because the obvious reading -- that stun removes the
        option -- is wrong, and a check written for that reading would pass this
        case for the wrong reason.
        """
        case = MakeCase(
            name="stunned",
            given=(HERO_FORM, GivenStep("stunned", ("Spider-Man",))),
            beats=(CannotStep(option="attack", card="Rhino"),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_a_restriction_the_engine_does_not_impose_fails(self):
        """The control. Without this the step could pass unconditionally."""
        case = MakeCase(
            name="no restriction",
            given=(HERO_FORM,),
            beats=(CannotStep(option="attack", card="Rhino"),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_ASSERTION, result.Describe())
        self.assertIn("legal target", result.Failures()[0].message)

    def test_a_restriction_about_a_card_outside_the_game_is_unresolvable(self):
        """Not vacuously true.

        "You cannot attack a card that is not in this game" holds trivially, so
        granting it would let a misspelled card name read as a proven
        restriction. It is the one way this step could pass while saying
        nothing, so it is refused -- and refused as *unresolvable*, which is
        what routes it to FAIL-spec-wrong rather than FAIL-engine-suspected.
        """
        case = MakeCase(
            name="missing card",
            given=(HERO_FORM,),
            beats=(CannotStep(option="attack", card="Galactus"),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertTrue(result.Failures()[0].unresolvable)

    def test_a_restriction_can_follow_a_prompt_table(self):
        """Assertions after the option table describe the same decision.

        The prompt beat is consumed mid-`Choose`, so anything queued behind it
        has to be drained again before the next action is looked for. Without
        that second drain this reads as "unexpected beat at a decision".
        """
        case = MakeCase(
            name="prompt then restriction",
            given=(
                HERO_FORM,
                GivenStep("encounter_deck", ("Hydra Mercenary", "Hydra Mercenary")),
                GivenStep("in_play", ("Hydra Mercenary #1",)),
            ),
            beats=(
                PromptStep(options=("Attack", "Change Form")),
                CannotStep(option="attack", card="Rhino"),
                WhenStep(option="attack", targets=("Hydra Mercenary #1",)),
                ThenStep("Hydra Mercenary #1", "damage", 2),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_an_unaffordable_card_action_is_not_offered(self):
        """Vision needs energy; two mental cards cannot pay that cost."""
        case = MakeCase(
            name="unaffordable action",
            given=(
                HERO_FORM,
                GivenStep("in_play", ("Vision",)),
                GivenStep("hand", ("Enhanced Spider-Sense",
                                   "Enhanced Spider-Sense")),
            ),
            beats=(NotOfferedStep(option="Action", card="Vision"),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_not_offered_fails_when_the_card_action_is_affordable(self):
        """The negative assertion must not pass merely because it exists."""
        case = MakeCase(
            name="affordable action",
            given=(
                HERO_FORM,
                GivenStep("in_play", ("Vision",)),
                GivenStep("hand", ("Energy",)),
            ),
            beats=(NotOfferedStep(option="Action", card="Vision"),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_ASSERTION, result.Describe())
        self.assertIn("offered", result.Failures()[0].message)

    def test_unaffordable_event_play_remains_offered(self):
        """Event Play entries keep the established visible-menu contract."""
        case = MakeCase(
            name="unaffordable event remains visible",
            given=(
                HERO_FORM,
                GivenStep("hand", ("Swinging Web Kick",)),
            ),
            beats=(NotOfferedStep(option="Play", card="Swinging Web Kick"),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_ASSERTION, result.Describe())
        self.assertIn("offered", result.Failures()[0].message)

    def test_not_offered_requires_the_named_card_to_exist(self):
        """An absent card cannot make an absent option pass vacuously."""
        case = MakeCase(
            name="missing card option",
            given=(HERO_FORM,),
            beats=(NotOfferedStep(option="Action", card="Galactus"),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertTrue(result.Failures()[0].unresolvable)

    def test_a_deck_literal_is_written_top_first(self):
        """The first card named is the next one drawn (MARVEL-82).

        Pinned against the engine rather than against `Deck.cards`, because the
        claim is about what a scenario *sees*: the boost card of a villain
        activation is the top of the encounter deck, and Sandman's printed 2
        boost icons make it tell itself apart from Hydra Mercenary's 1.

        Rhino stage 1 is printed ATK 2, so the hero takes 4 when Sandman boosts
        and 3 when the Mercenary does. Two scenarios differing only in which
        card is written first, so nothing but the orientation can explain it.
        """
        def Round(*encounter):
            return MakeCase(
                name="deck order",
                heroes=("iron_man",),
                given=(
                    GivenStep("hero_form", ("me",)),
                    GivenStep("player_deck", ("Pepper Potts",) * 6),
                    GivenStep("encounter_deck", encounter),
                ),
                beats=(
                    WhenStep(pass_priority=True),
                    WhenStep(pass_priority=True),
                    ThenStep("me", "damage", 4 if encounter[0] == "Sandman" else 3),
                ),
            )

        top = RunCase(Round("Sandman", "Hydra Mercenary", "Hydra Mercenary"))
        self.assertEqual(top.outcome, OUTCOME_PASS, top.Describe())

        bottom = RunCase(Round("Hydra Mercenary", "Hydra Mercenary", "Sandman"))
        self.assertEqual(bottom.outcome, OUTCOME_PASS, bottom.Describe())

    def test_restacking_a_deck_leaves_the_ordinals_alone(self):
        """`#N` still counts written order after the restack (MARVEL-82).

        The two orderings run opposite ways in the engine's list, so the
        tempting fix -- reverse the list handed to `RunPuzzle` -- would correct
        the draw order and silently redefine `#1` as the *last* card written.
        Scenarios would keep passing while meaning something else, which is
        worse than the bug being fixed. This is the check that says they didn't.

        Sandman is written second, so it is second by creation order and second
        from the top. Both readings agree, and only one of them would survive
        the wrong fix.
        """
        case = MakeCase(
            name="ordinals after restack",
            heroes=("iron_man",),
            given=(
                GivenStep("hero_form", ("me",)),
                GivenStep("player_deck", ("Pepper Potts",) * 6),
                GivenStep("encounter_deck",
                          ("Hydra Mercenary", "Hydra Mercenary", "Sandman")),
            ),
            beats=(
                WhenStep(pass_priority=True),
                WhenStep(pass_priority=True),
                # `#1` is written first, so it is the top of the deck and gets
                # spent as the boost card. `#2` is the one dealt and revealed.
                # Reversing the list handed to `RunPuzzle` would swap these two
                # while every other assertion in the suite still held.
                ThenStep("Hydra Mercenary #1", "in_play", False),
                ThenStep("Hydra Mercenary #2", "in_play", True),
                # Written third and never reached.
                ThenStep("Sandman", "zone", "EncounterDeck"),
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
        self.assertIn("already used", result.Describe())

    def test_putting_a_card_the_setup_already_played_into_play_is_refused(self):
        # The other half of the guard: no earlier Given touched this card, so
        # the repeat check cannot see it. The villain is on the board because
        # scenario setup put it there, and saying so again does nothing.
        case = MakeCase(
            name="villain already in play",
            given=(GivenStep("in_play", ("Rhino in VillainArea",)),),
            beats=(ThenStep("Rhino", "health", 14),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("already in play", result.Describe())

    def test_revealing_the_same_card_twice_is_refused(self):
        # `revealed` is the other creating verb and carries the same hazard,
        # worse: `CardFace.Reveal` has no idempotency check, so a repeat re-runs
        # WhenCardWouldReveal / WhenPlayerRevealCard and double-fires triggers
        # rather than quietly doing nothing.
        case = MakeCase(
            name="repeated is revealed",
            given=(GivenStep("encounter_deck", ("Hydra Mercenary",)),
                   GivenStep("revealed", ("Hydra Mercenary",)),
                   GivenStep("revealed", ("Hydra Mercenary",))),
            beats=(ThenStep("Hydra Mercenary", "health", 3),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("already used", result.Describe())

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


################################################################################
#

class TestPerPlayerZones(unittest.TestCase):
    """Stocking and counting a seat's own deck (MARVEL-101).

    Every deck-stocking step used to be first person and route through
    `RunPuzzle`, whose four player-zone helpers all stock
    `world.GetFirstPlayer()`. So a two-player board could be *built* --
    `the heroes are` has always worked -- and nothing about the second player
    could be set up or asserted. "Each player puts the top card of their deck
    into play" is printed on 242 cards, so the hole is not one card's.

    The first-person steps are now sugar: `my deck is` is this step with the
    seat left at player 1.
    """

    def test_a_second_players_deck_can_be_stocked_and_counted(self):
        # Two seats, two different cards on top, and each deck counted on its
        # own. Before this step the second `given` could not be written at all.
        case = MakeCase(
            name="two decks",
            heroes=("spider_man", "captain_marvel"),
            given=(
                GivenStep("player_deck", ("Aunt May", "Backflip")),
                GivenStep("player_deck", ("Pepper Potts", "Energy"), player=1),
            ),
            beats=(
                ThenStep("Aunt May", "zone", "PlayerDeck"),
                ThenStep("Pepper Potts", "zone", "PlayerDeck"),
                ThenStep("player 1", "deck_size", 2),
                ThenStep("player 2", "deck_size", 2),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_the_two_seats_get_different_cards(self):
        """The seat index has to actually reach a different player.

        Stocking both lists into one deck would satisfy every count above --
        four cards over two decks and four cards in one deck both read as "2
        and 2" only if the split is real. So this asserts the split directly:
        seat 1 holds two cards and seat 2 holds one.
        """
        case = MakeCase(
            name="split decks",
            heroes=("spider_man", "captain_marvel"),
            given=(
                GivenStep("player_deck", ("Aunt May", "Backflip")),
                GivenStep("player_deck", ("Pepper Potts",), player=1),
            ),
            beats=(
                ThenStep("player 1", "deck_size", 2),
                ThenStep("player 2", "deck_size", 1),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_the_first_person_step_stocks_seat_one(self):
        # The sugar claim, against the engine rather than against the parser:
        # `my deck is` and `player 1's deck is` reach the same deck, so the
        # second player's is untouched by either.
        case = MakeCase(
            name="sugar",
            heroes=("spider_man", "captain_marvel"),
            given=(
                GivenStep("player_deck", ("Aunt May",)),
                GivenStep("player_deck", ("Backflip",), player=0),
            ),
            beats=(
                ThenStep("player 1", "deck_size", 2),
                ThenStep("player 2", "deck_size", 0),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_a_seat_this_game_does_not_have_is_refused(self):
        # A solo scenario naming player 2 is an author error, and the message
        # has to say how many seats there are or there is nothing to act on.
        case = MakeCase(
            name="no such seat",
            given=(GivenStep("player_deck", ("Backflip",), player=1),),
            beats=(ThenStep("player 1", "deck_size", 1),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("1 player(s)", result.message)
        self.assertIn("no player 2", result.message)

    def test_a_seat_below_one_is_rejected_at_load_time(self):
        # `player 0's deck is` compiles to seat -1, which would index the last
        # seat rather than failing. Caught where every other malformed Given is.
        with self.assertRaises(SpecCaseError) as caught:
            GivenStep("player_deck", ("Backflip",), player=-1)
        self.assertIn("numbered from 1", str(caught.exception))

    def test_a_seat_on_a_step_that_has_no_seat_is_rejected(self):
        # The encounter deck belongs to the table, not to a player. Silently
        # ignoring the seat would let a scenario read as something it is not.
        with self.assertRaises(SpecCaseError) as caught:
            GivenStep("encounter_deck", ("Hydra Mercenary",), player=1)
        self.assertIn("not a per-player step", str(caught.exception))


################################################################################
#

class TestSelfAfterTheTokenMoves(unittest.TestCase):
    """`"me"` as a card ref is seat 1 too, not the first player token (MARVEL-104).

    MARVEL-101 made the first-person *zone* steps sugar for seat 1 and left
    `resolve.ResolveNamed` reading `world.GetFirstPlayer()`, which is the token
    holder: `world.players` is rotated by one at the end of every round and
    loses a player outright on elimination. So in a two-player game past a round
    boundary, `"me"` named one hero while `I have <n> cards in hand` named the
    other -- one scenario talking about two players in two steps.

    It was unreachable by accident rather than by design, the same accident that
    hid MARVEL-101: a `Given` block cannot follow a `When`, so no `Given` ever
    ran after a rotation, and every trusted two-player scenario resolved inside
    one round. A `When` beat is not bound by either, which is what these pin.
    """

    def RotatedWorld(self):
        """A two-player board with the first player token moved on one seat.

        The rotation is done to `world.players` directly rather than by playing
        a round, because what is under test is the *reading* of `"me"` and not
        the engine's round machinery: the transcript below does it the long way
        and would still pass against a resolver that read the token if the
        rotation happened to be a no-op.
        """
        from core.utility.types import Types
        from tools.spec.harness import ApplyGiven, EnsureEngine, NewGameForCase
        from tools.spec.policy import TranscriptPolicy

        EnsureEngine()
        case = MakeCase(name="two seats", heroes=("spider_man", "captain_marvel"))
        game = NewGameForCase(case, TranscriptPolicy())
        self.assertTrue(game.GameSetup())
        world = game.world
        ApplyGiven(world, case)

        world.players = Types.Rotate(world.players, 1)
        return world

    def test_me_names_seat_one_and_not_the_token_holder(self):
        from tools.spec.resolve import ResolveCard

        world = self.RotatedWorld()
        seat_one = world.const_seat_order_players[0]

        # The premise: the two readings really have come apart on this board.
        self.assertIsNot(world.GetFirstPlayer(), seat_one)

        self.assertIs(ResolveCard(world, "me"), seat_one.GetIdentity().card)
        self.assertIsNot(ResolveCard(world, "me"),
                         world.GetFirstPlayer().GetIdentity().card)

    def test_every_spelling_of_the_first_person_names_the_same_card(self):
        # `SELF_NAMES` is a list of synonyms and nothing else. One reading, so
        # a scenario cannot pick a different player by spelling it differently.
        from tools.spec.resolve import SELF_NAMES, ResolveCard

        world = self.RotatedWorld()
        seat_one = world.const_seat_order_players[0]
        for name in SELF_NAMES:
            self.assertIs(ResolveCard(world, name), seat_one.GetIdentity().card, name)

    def test_a_transcript_that_crosses_a_round_boundary_stays_on_one_hero(self):
        """The reachable case, played out rather than injected.

        Two alter-egos pass through round 1, the villain schemes at each of
        them, and the round ends -- which hands the first player token to seat
        2, so round 2 opens with Carol Danvers rather than Peter Parker. She
        passes, and the transcript then addresses `"me"` on Peter Parker's turn.

        Under the token reading `"me"` is Carol Danvers, no offered option is
        bound to her card, and the `When` is unplayable while the engine is
        printing `Change_Form on Peter Parker` among the options it offered --
        the harness refusing a card it is itself naming. The `Then` is the other
        half: `I am in hero form` reads seat 1, so the two steps have to agree
        about who "I" is for the scenario to mean anything.

        Both decks are stocked and so is the encounter deck, because a puzzle
        scene has neither and a round draws from all three.
        """
        filler = ("Pepper Potts",) * 8
        case = MakeCase(
            name="me across a round boundary",
            heroes=("spider_man", "captain_marvel"),
            given=(
                GivenStep("alter_ego_form", ("me",)),
                GivenStep("player_deck", filler),
                GivenStep("player_deck", filler, player=1),
                GivenStep("encounter_deck", ("Hydra Mercenary",) * 8),
            ),
            beats=(
                WhenStep(pass_priority=True),   # seat 1 ends its turn
                WhenStep(pass_priority=True),   # seat 2 ends its turn
                ThenStep("game", "round", 2),   # the token has moved to seat 2
                WhenStep(pass_priority=True),   # seat 2 goes first now
                WhenStep(option="change form", card="me"),
                ThenStep("me", "hero_form", True),
                # Seat 2 is untouched: still the alter-ego she started as, so
                # the change of form landed on seat 1 and not merely on someone.
                ThenStep("Carol Danvers", "in_play", True),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())


################################################################################
#

class TestSelfOnTheAssertionSide(unittest.TestCase):
    """`I` in a `Then` is seat 1's identity, by seat and not by object id (MARVEL-107).

    The third reading of the first person. `assertions.ResolveSubject` picked
    the first `is_identity` card out of `StateView.cards`, which
    `resolve.AllCards` builds in **object-id order** -- stable, which is what
    that function needs, and not seat order. It named seat 1 anyway because this
    engine allocates identity cards seat by seat during setup, so the three
    readings agreed by coincidence rather than by construction.

    **These are property tests, not a reproduction.** No board this engine can
    build makes the two orders disagree: object ids ascend from 1, identity
    cards are the first things allocated, and they are allocated in
    `world.players` order, which is still seat order at setup. A card created
    later by a `Given` -- even an identity card stocked into a deck -- gets a
    *higher* id and so cannot displace seat 1. What is pinned instead is the
    property the C# port needs: relabel the same board the way another allocator
    would number it, and `I` still means seat 1.
    """

    def LiveTwoPlayerView(self):
        """A real two-player board, captured. Returns (world, view)."""
        from tools.spec.harness import ApplyGiven, EnsureEngine, NewGameForCase
        from tools.spec.policy import TranscriptPolicy
        from tools.spec.state import Capture

        EnsureEngine()
        case = MakeCase(name="two seats", heroes=("spider_man", "captain_marvel"))
        game = NewGameForCase(case, TranscriptPolicy())
        self.assertTrue(game.GameSetup())
        ApplyGiven(game.world, case)
        return game.world, Capture(game.world)

    @staticmethod
    def AllocatedTheOtherWay(view):
        """The same board, numbered by an engine that allocated the identities
        in the other order.

        Only the *numbers* move: each seat keeps the identity card it had, so
        `identity_object_id` follows its card to the card's new id. That is what
        a different allocator does -- it does not hand seat 1 another hero.
        """
        from dataclasses import replace

        identities = [card for card in view.cards if card.is_identity]
        assert len(identities) == 2, identities
        swap = {identities[0].object_id: identities[1].object_id,
                identities[1].object_id: identities[0].object_id}
        cards = tuple(sorted(
            (replace(card, object_id=swap.get(card.object_id, card.object_id))
             for card in view.cards),
            key=lambda card: card.object_id))
        players = tuple(
            replace(player, identity_object_id=swap.get(player.identity_object_id,
                                                        player.identity_object_id))
            for player in view.players)
        return replace(view, cards=cards, players=players)

    def test_the_three_readings_of_the_first_person_agree_on_a_real_board(self):
        # The baseline: on a board this engine can actually build, the `Then`
        # subject, the card ref and the seat list are one hero.
        from tools.spec.harness import SeatOf
        from tools.spec.resolve import ResolveCard

        world, view = self.LiveTwoPlayerView()
        _, card, error = ResolveSubject(view, "I")
        self.assertEqual(error, "")
        self.assertEqual(card.object_id, SeatOf(world, 0).GetIdentity().card.object_id)
        self.assertEqual(card.object_id, ResolveCard(world, "me").object_id)

    def test_todays_agreement_rests_on_allocation_order(self):
        """The premise, measured rather than asserted.

        If this ever stops holding, the old implementation was already wrong and
        the failure belongs here rather than in whichever scenario noticed.
        """
        world, view = self.LiveTwoPlayerView()
        identities = [card for card in view.cards if card.is_identity]
        seat_identities = [player.identity_object_id for player in view.players]
        self.assertEqual([card.object_id for card in identities], seat_identities)
        self.assertEqual(seat_identities, sorted(seat_identities))

    def test_i_still_means_seat_one_under_another_allocation_order(self):
        # The property the port needs. Under the object-id reading this view
        # answers with the other hero, so the assertion below is the whole
        # difference between the two implementations.
        world, view = self.LiveTwoPlayerView()
        seat_one = view.players[0].identity
        relabelled = self.AllocatedTheOtherWay(view)

        first_by_id = [card for card in relabelled.cards if card.is_identity][0]
        self.assertNotEqual(first_by_id.name, seat_one,
                            "the relabelling did not move the identities apart")

        _, card, error = ResolveSubject(relabelled, "I")
        self.assertEqual(error, "")
        self.assertEqual(card.name, seat_one)
        self.assertEqual(card.object_id, relabelled.players[0].identity_object_id)

    def test_the_zone_steps_and_the_subject_name_one_seat_under_relabelling(self):
        # Both halves of a transcript on the same view: `player 1`/`I have ...`
        # reads the seat list, `I am ...` reads the identity. They have to be
        # the same hero or a scenario means two things.
        world, view = self.LiveTwoPlayerView()
        relabelled = self.AllocatedTheOtherWay(view)

        _, card, _ = ResolveSubject(relabelled, "I")
        _, player, _ = ResolveSubject(relabelled, "player 1")
        self.assertEqual(player.identity_object_id, card.object_id)

    def test_every_spelling_of_the_first_person_reads_the_same_seat(self):
        from tools.spec.resolve import SELF_NAMES

        state = MakeState(
            cards=[MakeIdentity(object_id=2, name="Carol Danvers", card_id="01043b",
                                card_ids=("01043a", "01043b"),
                                names=("carol danvers", "captain marvel"),
                                is_hero_form=False),
                   MakeIdentity(object_id=5)],
            players=[MakePlayer(identity_object_id=5),
                     MakePlayer(player_id=1, identity="Carol Danvers",
                                identity_object_id=2)],
        )
        for name in SELF_NAMES:
            _, card, error = ResolveSubject(state, name)
            self.assertEqual(error, "", name)
            self.assertEqual(card.object_id, 5, name)

    def test_the_seat_list_is_read_by_position_not_by_player_id(self):
        """`player_id` is data, position is the seat.

        The engine numbers players by seat index today -- `World.__init__`
        passes the loop index while it fills `const_seat_order_players` -- so
        the two agreed. A view whose ids are anything else still has to answer
        by seat, or `player 2` is a fourth reading of the same idea.
        """
        state = MakeState(
            cards=[MakeIdentity(object_id=5)],
            players=[MakePlayer(player_id=41, identity_object_id=5),
                     MakePlayer(player_id=17, identity="Carol Danvers",
                                hand_size=6, identity_object_id=2)],
        )
        self.assertTrue(Evaluate(state, ThenStep("player 1", "hand_size", 5)).passed)
        self.assertTrue(Evaluate(state, ThenStep("player 2", "hand_size", 6)).passed)
        _, card, error = ResolveSubject(state, "I")
        self.assertEqual(error, "")
        self.assertEqual(card.object_id, 5)

    def test_player_zero_is_not_the_last_seat(self):
        # `player 0` compiles to seat -1, which a plain index would answer with
        # the *last* seat rather than refusing.
        state = MakeState(players=[MakePlayer(),
                                   MakePlayer(player_id=1, hand_size=6)])
        result = Evaluate(state, ThenStep("player 0", "hand_size", 6))
        self.assertFalse(result.passed)
        self.assertTrue(result.unresolvable)
        self.assertIn("no player 0", result.message)

    def test_a_missing_seat_is_reported_the_way_the_author_wrote_it(self):
        # "player 3" in a one-player game says three, not the seat index two.
        state = MakeState(players=[MakePlayer()])
        _, target, error = ResolveSubject(state, "player 3")
        self.assertIsNone(target)
        self.assertIn("no player 3", error)
        self.assertIn("1 player(s)", error)

    def test_a_seat_with_no_identity_says_so_rather_than_finding_one(self):
        # Any identity on the board used to answer for `I`. A seat that has no
        # identity card is not a seat whose identity is somebody else's.
        state = MakeState(
            cards=[MakeIdentity(object_id=2, name="Carol Danvers", card_id="01043b",
                                card_ids=("01043a", "01043b"),
                                names=("carol danvers",))],
            players=[MakePlayer(identity_object_id=None)],
        )
        result = Evaluate(state, ThenStep("I", "hero_form", True))
        self.assertTrue(result.unresolvable)
        self.assertIn("no identity", result.message)

    def test_the_first_person_in_a_game_with_no_players(self):
        state = MakeState(cards=[MakeIdentity()])
        result = Evaluate(state, ThenStep("I", "hero_form", True))
        self.assertTrue(result.unresolvable)
        self.assertIn("0 player(s)", result.message)

    def test_a_seat_pointing_at_a_card_the_snapshot_lacks_refuses_to_guess(self):
        """The error path, pinned so it cannot become a fallback.

        Seat 1 names an identity that is not in `cards`. The tempting repair is
        to fall back to "the first identity on the board" when the lookup
        misses -- which is the object-id reading this change removed, restored
        on a branch nothing tests. Left unpinned, that branch reintroduces
        MARVEL-107 silently and only on the boards where it matters.

        Unreachable from a real capture, since `CapturePlayer` reads the id off
        a card `AllCards` also walks. It is a test about the seam, not the
        engine.
        """
        state = MakeState(
            cards=[MakeIdentity(object_id=2, name="Carol Danvers",
                                card_id="01043b", card_ids=("01043a", "01043b"),
                                names=("carol danvers",))],
            players=[MakePlayer(identity_object_id=99)],
        )
        _, card, error = ResolveSubject(state, "I")
        self.assertIsNone(card)
        self.assertIn("not in this snapshot", error)
        # Emphatically not the other identity that happens to be lying around.
        result = Evaluate(state, ThenStep("I", "hero_form", True))
        self.assertTrue(result.unresolvable)

    def test_the_capture_links_each_seat_to_its_own_identity_card(self):
        # The seat -> card link, against the engine. Two seats, two different
        # cards, each one the card that seat's `GetIdentity()` sits on.
        world, view = self.LiveTwoPlayerView()
        seats = world.const_seat_order_players
        self.assertEqual(len(view.players), 2)
        for index, player in enumerate(view.players):
            self.assertEqual(player.identity_object_id,
                             seats[index].GetIdentity().card.object_id)
            # The card it names is one the view calls an identity, so `I` and
            # `"<card>" is an identity` cannot disagree about the same card.
            named = view.CardByObjectId(player.identity_object_id)
            self.assertIsNotNone(named)
            self.assertTrue(named.is_identity)
        self.assertNotEqual(view.players[0].identity_object_id,
                            view.players[1].identity_object_id)

    def test_player_ids_are_seat_indices_in_this_engine(self):
        # Recorded, not relied on: `StateView.Player` reads the position. If
        # this ever stops holding, the harness is unaffected and the *engine*
        # has changed something worth knowing about.
        world, view = self.LiveTwoPlayerView()
        self.assertEqual([player.player_id for player in view.players], [0, 1])
        self.assertEqual([player.player_id
                          for player in world.const_seat_order_players], [0, 1])

    def test_the_identity_link_survives_a_change_of_form(self):
        # Both forms are faces of one card, so the link is form-independent --
        # which is what lets `I am in hero form` be about a seat rather than
        # about whichever card is showing.
        from tools.spec.harness import ApplyGiven, EnsureEngine, NewGameForCase
        from tools.spec.policy import TranscriptPolicy
        from tools.spec.state import Capture

        EnsureEngine()
        case = MakeCase(name="forms", heroes=("spider_man", "captain_marvel"),
                        given=(GivenStep("alter_ego_form", ("me",)),))
        game = NewGameForCase(case, TranscriptPolicy())
        self.assertTrue(game.GameSetup())
        world = game.world
        before = Capture(world).players[0].identity_object_id
        ApplyGiven(world, case)
        after = Capture(world)

        self.assertEqual(after.players[0].identity_object_id, before)
        _, card, error = ResolveSubject(after, "I")
        self.assertEqual(error, "")
        self.assertFalse(card.is_hero_form)


################################################################################
#

class TestFacedownDroneNaming(unittest.TestCase):
    """Naming a card by the face it is presenting (MARVEL-102).

    `Enemies.PutYouDeckTopCardAsFacedownMinion` calls
    `card.SetAsCard(ultron_facedown_drone)` without `remove_legacy`, so the card
    keeps its printed identity while presenting a face the game displays as
    "Drone Minion". Refs matched printed faces only, so the harness refused a
    name it was itself printing in the legal-target list of the minion
    activation prompt -- the same shape as MARVEL-94.

    Ultron Drones is in play in every case here because a DRONE has no printed
    statistics of its own: without it the drone enters play with 0 hit points
    and is defeated in the same breath, and there is nothing left to name.
    """

    SOLO = (
        GivenStep("hero_form", ("me",)),
        GivenStep("in_play", ("Ultron Drones",)),
        GivenStep("player_deck", ("Aunt May", "Backflip", "Backflip")),
        GivenStep("revealed", ("01144a",)),
    )

    def test_a_drone_is_nameable_by_the_face_it_presents(self):
        case = MakeCase(name="drone by face", given=self.SOLO,
                        beats=(ThenStep("Drone Minion", "zone", "EngagedEnemiesArea"),
                               ThenStep("Drone Minion", "health", 1)))
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_a_drone_still_answers_to_its_printed_identity(self):
        # Both names are live at once, deliberately: "Aunt May" is what the
        # scenario put on top of the deck and "Drone Minion" is what is now
        # engaged with the hero. Adding the second must not cost the first.
        case = MakeCase(name="drone by print", given=self.SOLO,
                        beats=(ThenStep("Aunt May", "zone", "EngagedEnemiesArea"),))
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_a_card_that_never_changed_face_gains_no_new_name(self):
        # The current face of an ordinary card is one of its printed faces, so
        # nothing about this widening should make a second name resolvable.
        # Rhino's own name still means exactly the cards it always meant.
        case = MakeCase(name="ordinary card", given=(HERO_FORM,),
                        beats=(ThenStep("Drone Minion", "in_play", True),))
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("no card matches", result.Failures()[0].message)

    def test_an_ordinal_over_drones_counts_creation_order(self):
        """`#N` still means the Nth card *the scenario created*.

        Not the Nth card to become a drone. The two decks are stocked in written
        order, so seat 1's Aunt May is created before seat 2's Pepper Potts and
        is `Drone Minion #1` whichever of them the engine reaches first. This is
        the claim the scenario in `01144-android-efficiency.feature` cannot make
        on its own: both drones sit in a zone of the same *type*, so no `Then`
        can say which of them is which.
        """
        case = MakeCase(
            name="two drones",
            heroes=("spider_man", "captain_marvel"),
            given=(
                GivenStep("hero_form", ("me",)),
                GivenStep("in_play", ("Ultron Drones",)),
                GivenStep("player_deck", ("Aunt May", "Backflip")),
                GivenStep("player_deck", ("Pepper Potts", "Energy"), player=1),
                GivenStep("revealed", ("01144a",)),
            ),
            beats=(ThenStep("Drone Minion #1", "zone", "EngagedEnemiesArea"),
                   ThenStep("Drone Minion #2", "zone", "EngagedEnemiesArea")),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

        # Which drone is which, which the scenario cannot say.
        kind, first, error = ResolveSubject(result.state, "Drone Minion #1")
        self.assertEqual((kind, error), ("card", ""))
        kind, second, error = ResolveSubject(result.state, "Drone Minion #2")
        self.assertEqual((kind, error), ("card", ""))
        # Aunt May is 01006 and Pepper Potts is 01033. Both cards still carry
        # the printed id of the card they were made from, alongside the drone's.
        self.assertIn("01006", first.card_ids)
        self.assertIn("01033", second.card_ids)

    def test_a_bare_drone_name_with_two_of_them_is_ambiguous(self):
        # The other half of the ordinal rule: two drones on one board is a real
        # ambiguity, and answering it with the first match is what this resolver
        # exists to refuse.
        case = MakeCase(
            name="ambiguous drones",
            heroes=("spider_man", "captain_marvel"),
            given=(
                GivenStep("hero_form", ("me",)),
                GivenStep("in_play", ("Ultron Drones",)),
                GivenStep("player_deck", ("Aunt May", "Backflip")),
                GivenStep("player_deck", ("Pepper Potts", "Energy"), player=1),
                GivenStep("revealed", ("01144a",)),
            ),
            beats=(ThenStep("Drone Minion", "zone", "EngagedEnemiesArea"),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("matches 2 cards", result.Failures()[0].message)


class TestDecksThatExistAtSetup(unittest.TestCase):
    """`my deck at setup is` / `the encounter deck at setup is` (MARVEL-121).

    Every `given` step is applied after `GameSetup()` returns, which is the
    right order for a board a scenario is building and the wrong one for a
    deck a **setup ability** reads. The engine sends `Message.WhenCardSetup`
    from inside `GameSetup()` -- `world.py` step 12 for main schemes and
    villains, step 16 for identities -- so on a puzzle scene those abilities
    search decks that are still empty. 49 cards carry such an ability.

    These cases are the proof that the second spelling reaches them, and that
    the first one still cannot. The pair matters: without the negative case a
    harness that had quietly started applying `Given` before setup would pass
    the positive one and nothing would say the distinction had collapsed.
    """

    def test_a_setup_ability_searches_the_deck_that_existed_at_setup(self):
        # T'Challa: "Foresight -- Setup: Search your deck for a BLACK PANTHER
        # upgrade and add it to your hand. Shuffle your deck." Vibranium Suit
        # is the upgrade; the two Vibraniums are not, and they are what is left
        # in the deck afterwards.
        case = MakeCase(
            name="foresight",
            heroes=("black_panther",),
            setup_player_deck=("Vibranium Suit", "Vibranium", "Vibranium"),
            given=(GivenStep("alter_ego_form", ("me",)),),
            beats=(
                ThenStep("Vibranium Suit", "zone", "HandsArea"),
                ThenStep("player", "hand_size", 1),
                ThenStep("player", "deck_size", 2),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_the_same_deck_stocked_by_given_is_too_late(self):
        """The control, and the whole reason the step exists.

        Identical cards, identical hero, the ordinary `my deck is` step. The
        upgrade stays in the deck because Foresight already ran against an
        empty one, so this is `FAIL-engine-suspected`-shaped and would be a
        misreading of the card if anybody wrote it as a scenario.
        """
        case = MakeCase(
            name="foresight, too late",
            heroes=("black_panther",),
            given=(
                GivenStep("alter_ego_form", ("me",)),
                GivenStep("player_deck", ("Vibranium Suit", "Vibranium", "Vibranium")),
            ),
            beats=(
                ThenStep("Vibranium Suit", "zone", "PlayerDeck"),
                ThenStep("player", "hand_size", 0),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_a_main_scheme_setup_ability_reads_the_encounter_deck(self):
        # The second sender, and the one that is not an identity: Underground
        # Distribution 1A searches the encounter deck for the Defense Network
        # side scheme and reveals it. Three cards because 01116a shuffles
        # afterwards and `SelectorEnd.DoShuffle` asserts a non-empty deck.
        case = MakeCase(
            name="underground distribution",
            scenario="klaw",
            setup_encounter_deck=("Defense Network", "Armored Guard", "Armored Guard"),
            given=(HERO_FORM,),
            beats=(
                ThenStep("Defense Network", "zone", "SideSchemesArea"),
                ThenStep("Defense Network", "threat", 3),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_without_the_step_the_scheme_finds_nothing(self):
        # The control for the encounter-deck half. Defense Network is not in
        # the game at all, which is why 01116a's spec file recorded its search
        # as unreachable rather than specifying it.
        case = MakeCase(
            name="nothing to find",
            scenario="klaw",
            given=(HERO_FORM,),
            beats=(ThenStep("Defense Network", "in_play", True),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("no card matches", result.Failures()[0].message)

    def test_a_setup_deck_is_shuffled_and_so_pins_no_order(self):
        """Unlike `my deck is`, which is written top-first (MARVEL-82).

        `player_setup.SelectIdentity` calls `player_deck.Shuffle(rule)` at
        setup step 6, so a setup deck is a *set* of cards. That is what a real
        game does, and it is the reason these steps are for abilities that
        search rather than for pinning what is drawn next. What survives is the
        contents, which is what this asserts.
        """
        case = MakeCase(
            name="setup deck contents",
            setup_player_deck=("Backflip", "Swinging Web Kick", "Spider-Tracer"),
            given=(HERO_FORM,),
            beats=(
                ThenStep("player", "deck_size", 3),
                ThenStep("Swinging Web Kick", "zone", "PlayerDeck"),
            ),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_the_two_deck_steps_stack_rather_than_replace(self):
        # A setup deck and a Given deck are two stockings of one zone, so a
        # scenario that needs both a searchable deck and a known top card can
        # write both. Three at setup plus one after is four.
        case = MakeCase(
            name="both spellings",
            setup_player_deck=("Backflip", "Backflip", "Backflip"),
            given=(HERO_FORM, GivenStep("player_deck", ("Swinging Web Kick",))),
            beats=(ThenStep("player", "deck_size", 4),),
        )
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_a_case_with_no_setup_deck_is_the_case_it_always_was(self):
        # The additive claim, pinned. Every scenario in `specs/` predates these
        # steps and none of them may move because the fields exist.
        case = MakeCase(name="untouched", given=(HERO_FORM,))
        self.assertEqual(case.setup_player_deck, ())
        self.assertEqual(case.setup_encounter_deck, ())
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_PASS, result.Describe())

    def test_a_setup_deck_survives_a_round_trip_through_json(self):
        case = MakeCase(
            name="round trip",
            setup_player_deck=("Backflip",),
            setup_encounter_deck=("Hydra Mercenary",),
        )
        again = SpecCase.FromJson(case.ToJson())
        self.assertEqual(again.setup_player_deck, ("Backflip",))
        self.assertEqual(again.setup_encounter_deck, ("Hydra Mercenary",))

    def test_a_setup_deck_naming_no_card_is_refused(self):
        case = MakeCase(name="typo", setup_player_deck=("Backflopp",))
        result = RunCase(case)
        self.assertEqual(result.outcome, OUTCOME_UNPLAYABLE, result.Describe())
        self.assertIn("no card is named", result.message)


if __name__ == "__main__":
    unittest.main()
