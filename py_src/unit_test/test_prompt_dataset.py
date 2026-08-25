"""The prompt dataset: the fold's other return value, recorded.

`datasets/digest/vectors.json` records the board at every step and nothing about
what the player was being asked. `datasets/digest/prompts.json` (MARVEL-173)
records the question. A port that reproduces every recorded board while offering
the wrong options passes the first fixture and fails this one, which is the
whole reason it exists -- declining a prompt that should never have been offered
leaves exactly the same board as declining the right one.

Three claims are tested here and they are different in kind.

**The two fixtures line up.** Step *n* of a case here is the prompt that was
open at step *n* of the same case there. `emit_prompts` imports `CASES` from
`emit_vectors` rather than repeating it, so the campaign, heroes and seed agree
by construction; the step *counts* are an independent consequence of replaying
the same game with the same declining policy, and they are checked.

**The projection is total and lossless where it claims to be.** Twelve of the
payload's fourteen option fields survive; the two that do not are the two
MARVEL-161 dropped after the census. These tests pin the mapping against
hand-built payloads, so a field that silently stops being carried fails here
rather than in C#.

**A target request is absent, not empty, when there is nothing to choose.**
`Change_Form` is the case: no candidates, no groups, a `[0, 0]` range. The C#
type is nullable and a client that rendered "select between 0 and 0 things"
would be wrong in a way an absent request cannot be.

    python -m unittest unit_test.test_prompt_dataset
"""

from __future__ import annotations

import json
import os
import unittest

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from tools.digest import emit_prompts

PROMPT_DATASET = os.path.join("..", "datasets", "digest", "prompts.json")
DIGEST_VECTORS = os.path.join("..", "datasets", "digest", "vectors.json")


def _Load(path: str, what: str):
    if not os.path.exists(path):
        raise unittest.SkipTest(f"run from py_src/ -- {what} missing")
    with open(path, encoding="utf-8") as handle:
        return json.load(handle)


class _Payload:
    """The fields of an `AskOptionPayload` this projection reads."""

    def __init__(self, options, *, event_name="WhenPlayerInTurn",
                 ability_type="Normal", prompt_text="", show_cancel=True):
        self.options_json = json.dumps(options)
        self.event_name = event_name
        self.ability_type = ability_type
        self.prompt_text = prompt_text
        self.show_cancel = show_cancel


def _Option(**overrides):
    """An option descriptor with every field the engine renders."""
    option = {
        "id": 1, "name": "Play", "bind_id": 37, "bind_player_id": 0,
        "all_legal_targets": [], "target_num_range": [0, 0], "target_payment": {},
        "select_rule": "", "select_rule_param": [0, 0], "target_groups": [],
        "target_must_include_traits": [], "failure_reason": "",
        "is_search": False, "pay_size_is_effect": False,
    }
    option.update(overrides)
    return option


class Projection(unittest.TestCase):
    """What one rendered option becomes, field by field."""

    def test_the_prompt_carries_the_seat_it_was_put_to(self):
        # The payload does not know; `DeviceManager.DoGetInput` supplies it.
        prompt = emit_prompts.PromptOf(2, _Payload([]))
        self.assertEqual(prompt["player"], 2)

    def test_cancellable_is_show_cancel_not_its_negation(self):
        # The engine asks `is_forced`; `show_cancel` is already the negation,
        # and negating twice is the easy mistake here.
        for show_cancel in (True, False):
            prompt = emit_prompts.PromptOf(0, _Payload([], show_cancel=show_cancel))
            self.assertEqual(prompt["cancellable"], show_cancel)

    def test_the_label_keeps_its_whitespace(self):
        # "\n--- Spider-Man's Turn (1) ---" is the engine's console line, and
        # two implementations would strip it differently.
        label = "\n--- Spider-Man's Turn (1) ---"
        self.assertEqual(emit_prompts.PromptOf(0, _Payload([], prompt_text=label))["label"],
                         label)

    def test_no_targets_is_absent_rather_than_empty(self):
        self.assertIsNone(emit_prompts.Targets(_Option(name="Change_Form")))

    def test_a_rule_alone_is_enough_to_make_a_request(self):
        # A selection rule with no flat candidates still constrains the answer,
        # so dropping the request would drop the constraint.
        self.assertIsNotNone(emit_prompts.Targets(_Option(select_rule="Villain")))
        self.assertIsNotNone(
            emit_prompts.Targets(_Option(target_must_include_traits=["t_AVENGER"])))
        self.assertIsNotNone(emit_prompts.Targets(_Option(target_groups=[[1, 2]])))

    def test_a_target_request_carries_every_field(self):
        targets = emit_prompts.Targets(_Option(
            all_legal_targets=[42, 9], target_num_range=[1, 2],
            target_groups=[[42], [9]], target_must_include_traits=["t_AVENGER"],
            select_rule="VillainAndMinionsEngagedWithYou", is_search=True))
        self.assertEqual(targets, {
            "legal": [42, 9], "min": 1, "max": 2,
            "groups": [[42], [9]], "must_include_traits": ["t_AVENGER"],
            "rule": "VillainAndMinionsEngagedWithYou", "is_search": True,
        })

    def test_the_legal_list_keeps_the_engine_order(self):
        # The opening hand is offered as [42, 45, 37, 9, 47, 46], which is not
        # sorted. Sorting it here would make the fixture disagree with the
        # engine about which card a client highlights first.
        targets = emit_prompts.Targets(
            _Option(all_legal_targets=[42, 45, 37, 9, 47, 46], target_num_range=[0, 6]))
        self.assertEqual(targets["legal"], [42, 45, 37, 9, 47, 46])

    def test_costs_are_ordered_by_target_as_a_number(self):
        # `target_payment` arrives keyed by string, so a plain sort puts 10
        # before 2 and the fixture would depend on how many objects existed.
        costs = emit_prompts.Costs(_Option(target_payment={
            "10": {"cost": "1", "payment": []},
            "2": {"cost": "2", "payment": []},
        }))
        self.assertEqual([cost["target"] for cost in costs], [2, 10])

    def test_a_cost_carries_its_generators_in_offer_order(self):
        costs = emit_prompts.Costs(_Option(target_payment={"0": {
            "cost": "3", "rule": [], "or_cost": "", "or_rule": [],
            "payment": [{"38": "YY"}, {"1": "B"}, {"3": "R"}],
        }}))
        self.assertEqual(costs[0]["sources"], [
            {"effect": 38, "generates": "YY"},
            {"effect": 1, "generates": "B"},
            {"effect": 3, "generates": "R"},
        ])

    def test_an_alternative_cost_survives(self):
        # Flattening an alternative reading to a bare number corrupted a corpus
        # during MARVEL-158. Both readings travel.
        costs = emit_prompts.Costs(_Option(target_payment={"0": {
            "cost": "1", "rule": ["M"], "or_cost": "2", "or_rule": [],
            "payment": [],
        }}))
        self.assertEqual((costs[0]["cost"], costs[0]["rule"],
                          costs[0]["or_cost"], costs[0]["or_rule"]),
                         ("1", ["M"], "2", []))

    def test_an_illegal_option_is_kept_and_carries_its_reason(self):
        # The engine offers options it knows cannot be taken so a client can
        # grey one out and say why. Dropping them loses the "why".
        affordance = emit_prompts.Affordance(
            _Option(failure_reason="pay cost, need 3, but only have 2"))
        self.assertEqual(affordance["illegal"], "pay cost, need 3, but only have 2")

    def test_a_legal_option_says_null_rather_than_empty_string(self):
        self.assertIsNone(emit_prompts.Affordance(_Option())["illegal"])

    def test_the_two_vestigial_fields_do_not_survive(self):
        # `select_rule_param` and `pay_size_is_effect`, dropped by MARVEL-161
        # after the census. Named here so re-adding one is a decision.
        rendered = json.dumps(emit_prompts.Affordance(_Option()))
        self.assertNotIn("select_rule_param", rendered)
        self.assertNotIn("pay_size_is_effect", rendered)


class Dataset(unittest.TestCase):
    """The emitted file, and its relationship to the digest vectors."""

    def setUp(self) -> None:
        self.prompts = _Load(PROMPT_DATASET, "datasets/digest/prompts.json")
        self.vectors = _Load(DIGEST_VECTORS, "datasets/digest/vectors.json")

    def test_the_cases_are_the_digest_vectors_cases(self):
        self.assertEqual(
            [(case["campaign"], case["heroes"], case["seed"], case["max_steps"])
             for case in self.prompts["cases"]],
            [(case["campaign"], case["heroes"], case["seed"], case["max_steps"])
             for case in self.vectors["cases"]])

    def test_step_n_here_is_step_n_there(self):
        # The claim the file's `note` makes. Both replay the same game with the
        # same declining policy, so the counts are an independent consequence
        # rather than something either emitter copies from the other.
        for prompts, vectors in zip(self.prompts["cases"], self.vectors["cases"]):
            self.assertEqual(len(prompts["prompts"]), prompts["steps"])
            self.assertEqual(prompts["steps"], vectors["steps"], prompts["campaign"])

    def test_no_prompt_is_empty(self):
        # "A decision with no options is not put to a player." The C# `Prompt`
        # states it in prose; this is the measurement behind it.
        for case in self.prompts["cases"]:
            for step, prompt in enumerate(case["prompts"]):
                self.assertGreater(len(prompt["affordances"]), 0,
                                   f"{case['campaign']} step {step}")

    def test_every_affordance_is_anchored(self):
        # `AnchorId` is what survives a session boundary when the effect id
        # drifts, so an unanchored affordance is one a replay cannot resolve.
        for case in self.prompts["cases"]:
            for prompt in case["prompts"]:
                for affordance in prompt["affordances"]:
                    self.assertGreaterEqual(affordance["anchor_id"], 0)
                    self.assertGreaterEqual(affordance["anchor_player"], 0)

    def test_the_kind_index_is_the_kinds_that_appear(self):
        seen = {}
        for case in self.prompts["cases"]:
            for prompt in case["prompts"]:
                seen[prompt["kind"]] = seen.get(prompt["kind"], 0) + 1
        self.assertEqual(self.prompts["kinds"], dict(sorted(seen.items())))

    def test_every_kind_is_a_timing_priority(self):
        # `ability_type` is `TimingPriority.name`. That enum has twelve members
        # and the C# `PromptKind` has four, so this is the check that a kind
        # nobody has a C# member for is at least a real engine kind.
        from game.ability.ability_type import TimingPriority
        for kind in self.prompts["kinds"]:
            self.assertIn(kind, TimingPriority.__members__)

    def test_the_milestone_board_asks_the_three_expected_questions(self):
        # `rhino / spider_man / 12345` is the board the C# port is accepted
        # against. Its seven steps are three shapes -- mulligan, turn, end
        # phase -- and the turn repeats. Pinned because the C# fold reproduces
        # exactly this sequence.
        case = self.prompts["cases"][0]
        self.assertEqual(case["campaign"], "rhino")
        self.assertEqual([prompt["trigger"] for prompt in case["prompts"]], [
            "WhenPlayerChooseAbility",
            "WhenPlayerInTurn", "End Turn",
            "WhenPlayerInTurn", "End Turn",
            "WhenPlayerInTurn", "End Turn",
        ])
        self.assertEqual([prompt["cancellable"] for prompt in case["prompts"]],
                         [False, True, False, True, False, True, False])


if __name__ == "__main__":
    unittest.main()
