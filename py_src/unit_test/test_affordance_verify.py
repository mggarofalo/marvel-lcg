"""The parts of MARVEL-164 that can be wrong without the corpus noticing.

`python -m tools.affordances.verify` is the proof and it needs the frozen
corpus. What is here is the projection it rests on: if `Project` reads the
engine's rendered option wrongly, every level the corpus run reports is a level
about the wrong thing.

Two of these pin findings rather than plumbing -- the grouped-selection rule,
and the fact that a payment slot can carry more than one resource.

    python -m unittest unit_test.test_affordance_verify
"""

from __future__ import annotations

import unittest

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from game.render.descriptor.effect import EffectDescriptor

from tools.affordances import verify


def Option(**overrides):
    """A rendered option, with everything the engine always fills in."""
    fields = {
        "id": 87, "name": "Play", "bind_id": 60, "bind_player_id": 1,
        "all_legal_targets": [], "target_num_range": [0, 0],
        "target_payment": {}, "select_rule": "", "select_rule_param": (0, 0),
        "target_groups": [], "target_must_include_traits": [],
        "failure_reason": "", "is_search": False, "pay_size_is_effect": False,
    }
    fields.update(overrides)
    return EffectDescriptor(**fields)


class TestReadingARecordedReference(unittest.TestCase):
    """The recording writes an object id and then whatever was readable at the
    time, so only the id is parsed."""

    def testACardReference(self):
        self.assertEqual(verify._ObjectId("c143 01095"), 143)

    def testAnEffectReference(self):
        self.assertEqual(verify._ObjectId("e52 Discard c64 04008"), 52)

    def testADebugCommandIsNotAReference(self):
        self.assertEqual(verify._ObjectId(":give_card 01001"), -1)
        self.assertEqual(verify._ObjectId(""), -1)


class TestTheProjection(unittest.TestCase):

    def testItReadsOnlyWhatTheModelCarries(self):
        option = verify.Project(Option(all_legal_targets=[1, 2],
                                       target_num_range=[1, 1]))
        self.assertEqual(option["legal"], [1, 2])
        self.assertEqual((option["min"], option["max"]), (1, 1))
        self.assertEqual(option["anchor"], 60)

    def testAPaymentSlotCanGenerateMoreThanOneResource(self):
        """`{"82": "YY"}` is one generator producing two resources.

        Counting letters rather than slots would say a card generating two of a
        type was two ways to pay, and a client would offer the same card twice.
        """
        payment = {0: EffectDescriptor.Payment(
            cost="2", payment=[{52: "R"}, {82: "YY"}], rule=[])}
        option = verify.Project(Option(target_payment=payment))
        self.assertEqual(option["payment"], {0: [52, 82]})

    def testPaymentIsKeyedByTargetIncludingTheUnpricedZero(self):
        payment = {
            0: EffectDescriptor.Payment(cost="1", payment=[{1: "G"}], rule=[]),
            143: EffectDescriptor.Payment(cost="3", payment=[{6: "Y"}], rule=[]),
        }
        option = verify.Project(Option(target_payment=payment))
        self.assertEqual(sorted(option["payment"]), [0, 143])


class TestAGroupedSelectionIsNotACount(unittest.TestCase):
    """MARVEL-164's finding, from the tool's side.

    Explosive Arrow -- "choose a player, deal 3 damage to the villain and each
    minion engaged with that player" -- pools three cards and accepts two of
    them. The flat range says `[3, 3]`, and it is not a rule any legal
    selection satisfies.
    """

    GROUPED = dict(all_legal_targets=[143, 3, 152], target_num_range=[3, 3],
                   target_groups=[[143, 3], [143, 152]],
                   select_rule="VillainAndMinionsEngagedSamePlayer")

    def testTheLegalSelectionIsInsideAGroupAndOutsideTheRange(self):
        verdict = verify.Verdict()
        option = verify.Project(Option(**self.GROUPED))

        verify._CheckTargets(option, [143, 3], verdict, {"scene": "x", "step": 1})

        self.assertEqual(verdict.grouped_ok, 1)
        self.assertEqual(verdict.targets_ok, 1)
        # Recorded, not failed: the disagreement is the measurement.
        self.assertEqual(verdict.grouped_disagrees, 1)
        self.assertEqual(verdict.failures, [])
        # And the count is not applied at all, so it cannot be the thing that
        # passed or failed.
        self.assertEqual(verdict.counted, 0)

    def testASelectionSpanningTwoGroupsFails(self):
        verdict = verify.Verdict()
        option = verify.Project(Option(**self.GROUPED))

        verify._CheckTargets(option, [3, 152], verdict, {"scene": "x", "step": 1})

        self.assertEqual(verdict.grouped_ok, 0)
        self.assertEqual(len(verdict.failures), 1)

    def testAnUngroupedRequestStillEnforcesTheCount(self):
        verdict = verify.Verdict()
        option = verify.Project(Option(all_legal_targets=[1, 2, 3],
                                       target_num_range=[1, 2]))

        verify._CheckTargets(option, [1, 2, 3], verdict, {"scene": "x", "step": 1})

        self.assertEqual(verdict.counted, 1)
        self.assertEqual(verdict.count_ok, 0)
        self.assertEqual(len(verdict.failures), 1)


class TestPaymentIsCheckedAgainstEveryEntry(unittest.TestCase):

    def testAGeneratorFromAnotherTargetsEntryCounts(self):
        """The recording does not say which entry it paid against.

        A player picks a target and then pays, so anything in any entry was
        offerable. Reading only the entry for the chosen target would fail a
        legal payment for a reason the recording cannot settle.
        """
        verdict = verify.Verdict()
        option = verify.Project(Option(target_payment={
            0: EffectDescriptor.Payment(cost="1", payment=[{5: "R"}], rule=[]),
            143: EffectDescriptor.Payment(cost="2", payment=[{9: "Y"}], rule=[]),
        }))

        verify._CheckResources(option, [9], [143], verdict, {"scene": "x", "step": 1})

        self.assertEqual(verdict.resources_ok, 1)

    def testAGeneratorNobodyOfferedFails(self):
        verdict = verify.Verdict()
        option = verify.Project(Option(target_payment={
            0: EffectDescriptor.Payment(cost="1", payment=[{5: "R"}], rule=[]),
        }))

        verify._CheckResources(option, [77], [], verdict, {"scene": "x", "step": 1})

        self.assertEqual(verdict.resources_ok, 0)
        self.assertEqual(len(verdict.failures), 1)


if __name__ == "__main__":
    unittest.main()
