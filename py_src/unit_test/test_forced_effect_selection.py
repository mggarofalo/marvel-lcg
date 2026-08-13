"""`EventManager.SelectForcedEffect` decides which forced ability initiates next.

The Rules Reference: *"If two or more forced abilities would initiate at the
same moment, the first player determines the order in which the abilities
initiate, regardless of who controls the cards bearing those abilities."*

Delay abilities are excluded from that choice. They were excluded from `faces`
but not from `forced_effects`, and the chosen face was then turned back into an
effect by indexing the *unfiltered* list -- so for a `[normal, delay, normal]`
batch, choosing the second normal ability resolved the delay ability that had
just been excluded, and never resolved the one that was picked.

See MARVEL-39. The list order this indexes into became deterministic in
MARVEL-31, which turned a randomly-wrong result into a reproducibly-wrong one.

A face also cannot name *which* of a card's abilities was picked, so a batch
that was all on one card was never put to the first player at all -- the engine
took the first entry, though the rule quoted above draws no such distinction.
That is MARVEL-40, and it is why the selection is now over effects and there is
no `is_on_the_same_card` branch left.
"""

import unittest

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported. That is the whole bootstrap this needs.
import engine  # noqa: F401  pylint: disable=unused-import

from game.ability.ability_type import AbilityType
from game.event.manager import EventManager


class FakeCard:
    def __init__(self, name: str) -> None:
        self.name = name

    def __repr__(self) -> str:
        return self.name


class FakeFace:
    """`Effect.this` -- a card face, which the prompt is offered over."""

    def __init__(self, card: FakeCard) -> None:
        self.card = card

    def __repr__(self) -> str:
        return f"face of {self.card}"


class FakeFlags:
    def __init__(self, is_delay_ability: bool) -> None:
        self.is_delay_ability = is_delay_ability


class FakeAbility:
    def __init__(self, is_delay_ability: bool,
                 ability_type: AbilityType | None = None) -> None:
        self.flags = FakeFlags(is_delay_ability)
        # `IsInternalCleanupBatch` reads `ability.type`, and the real
        # `AbilityType` enum is used rather than a stand-in so a test cannot
        # keep passing after the engine renames a member. The default is a
        # printed card ability, so a fake that says nothing about its type is
        # never mistaken for the engine's own bookkeeping.
        self.type = ability_type if ability_type is not None else AbilityType.ForcedResponse


class FakeEffect:
    """The members of `Effect` the selection and the labels read."""

    def __init__(self, label: str, face: FakeFace, delay: bool = False,
                 display_name: str | None = None,
                 ability_type: AbilityType | None = None) -> None:
        self.label = label
        self.this = face
        self.ability = FakeAbility(delay, ability_type)
        self.display_name = display_name if display_name is not None else label

    def GetDisplayName(self, *, remove_space: bool = False) -> str:
        name = self.display_name
        return name.replace(" ", "_") if remove_space else name

    def __repr__(self) -> str:
        return self.label


def Decline(candidates):
    return None


class Chooser:
    """A first player who picks the candidate at `pick`, recording what was offered."""

    def __init__(self, pick: int) -> None:
        self.pick = pick
        self.offered = None

    def __call__(self, candidates):
        self.offered = list(candidates)
        return candidates[self.pick]


class TestSelectForcedEffect(unittest.TestCase):

    def setUp(self):
        self.rhino = FakeCard("Rhino")
        self.klaw = FakeCard("Klaw")
        self.shocker = FakeCard("Shocker")

    def test_mixed_normal_delay_normal_resolves_the_chosen_ability(self):
        # The regression. `faces` is [A.this, C.this]; picking C gives index 1,
        # and `forced_effects[1]` was B -- the delay ability excluded from the
        # choice. The chosen ability never resolved at all.
        a = FakeEffect("A", FakeFace(self.rhino))
        b = FakeEffect("B-delay", FakeFace(self.klaw), delay=True)
        c = FakeEffect("C", FakeFace(self.shocker))

        chooser = Chooser(pick=1)
        chosen = EventManager.SelectForcedEffect([a, b, c], chooser)

        self.assertIs(chosen, c)

    def test_a_delay_ability_is_never_offered(self):
        a = FakeEffect("A", FakeFace(self.rhino))
        b = FakeEffect("B-delay", FakeFace(self.klaw), delay=True)
        c = FakeEffect("C", FakeFace(self.shocker))

        chooser = Chooser(pick=0)
        EventManager.SelectForcedEffect([a, b, c], chooser)

        self.assertEqual(chooser.offered, [a, c])

    def test_a_delay_ability_is_never_the_result(self):
        # Every reachable choice, so the delay ability cannot slip through on a
        # particular pick.
        a = FakeEffect("A", FakeFace(self.rhino))
        b = FakeEffect("B-delay", FakeFace(self.klaw), delay=True)
        c = FakeEffect("C", FakeFace(self.shocker))

        for pick in (0, 1):
            chosen = EventManager.SelectForcedEffect([a, b, c], Chooser(pick))
            self.assertIsNot(chosen, b)

    def test_two_leading_delay_abilities_still_align(self):
        # Several excluded entries shift the index by more than one, so the old
        # code could run off the end of the list rather than merely pick wrong.
        d1 = FakeEffect("D1-delay", FakeFace(self.rhino), delay=True)
        d2 = FakeEffect("D2-delay", FakeFace(self.rhino), delay=True)
        a = FakeEffect("A", FakeFace(self.klaw))
        c = FakeEffect("C", FakeFace(self.shocker))

        chosen = EventManager.SelectForcedEffect([a, d1, d2, c], Chooser(pick=1))

        self.assertIs(chosen, c)

    def test_the_first_player_chooses_across_different_cards(self):
        a = FakeEffect("A", FakeFace(self.rhino))
        b = FakeEffect("B", FakeFace(self.klaw))

        self.assertIs(EventManager.SelectForcedEffect([a, b], Chooser(pick=1)), b)
        self.assertIs(EventManager.SelectForcedEffect([a, b], Chooser(pick=0)), a)

    def test_declining_the_choice_falls_back_to_list_order(self):
        # `AskChooseFace` returns None if the player declines. List order is
        # `Effect.object_id` order after MARVEL-31, so the fallback is creation
        # order rather than whatever the allocator produced.
        a = FakeEffect("A", FakeFace(self.rhino))
        b = FakeEffect("B", FakeFace(self.klaw))

        self.assertIs(EventManager.SelectForcedEffect([a, b], Decline), a)

    def test_the_fallback_skips_a_leading_delay_ability(self):
        # Declining must not resolve an excluded ability either.
        a = FakeEffect("A", FakeFace(self.rhino))
        d = FakeEffect("D-delay", FakeFace(self.klaw), delay=True)

        self.assertIs(EventManager.SelectForcedEffect([a, d], Decline), a)

    def test_a_single_ability_is_not_put_to_a_choice(self):
        a = FakeEffect("A", FakeFace(self.rhino))

        def refuse(faces):
            self.fail("the first player was asked to break a one-way tie")

        self.assertIs(EventManager.SelectForcedEffect([a], refuse), a)

    def test_abilities_on_one_card_are_put_to_the_first_player(self):
        # MARVEL-40. The engine used to take the first entry without asking when
        # every candidate sat on one card. The rule quoted in the module
        # docstring says "regardless of who controls the cards bearing those
        # abilities" -- it draws no same-card distinction.
        face = FakeFace(self.rhino)
        first = FakeEffect("first", face)
        second = FakeEffect("second", face)

        chooser = Chooser(pick=1)
        chosen = EventManager.SelectForcedEffect([first, second], chooser)

        self.assertEqual(chooser.offered, [first, second])
        self.assertIs(chosen, second)

    def test_the_second_ability_on_one_card_is_reachable(self):
        # The residual MARVEL-39 left behind: selecting over faces, both entries
        # had the same `this`, so `index` always returned the first and the
        # second ability could not be chosen at all.
        face = FakeFace(self.rhino)
        first = FakeEffect("first", face)
        second = FakeEffect("second", face)
        third = FakeEffect("third", face)

        for pick, expected in ((0, first), (1, second), (2, third)):
            chosen = EventManager.SelectForcedEffect([first, second, third], Chooser(pick))
            self.assertIs(chosen, expected)

    def test_a_mixed_same_and_different_card_batch_offers_every_ability(self):
        rhino_face = FakeFace(self.rhino)
        on_rhino_1 = FakeEffect("r1", rhino_face)
        on_rhino_2 = FakeEffect("r2", rhino_face)
        on_klaw = FakeEffect("k", FakeFace(self.klaw))

        chooser = Chooser(pick=1)
        chosen = EventManager.SelectForcedEffect(
            [on_rhino_1, on_rhino_2, on_klaw], chooser
        )

        self.assertEqual(chooser.offered, [on_rhino_1, on_rhino_2, on_klaw])
        self.assertIs(chosen, on_rhino_2)

    def test_a_single_ability_on_one_card_is_still_not_a_choice(self):
        face = FakeFace(self.rhino)
        only = FakeEffect("only", face)

        def refuse(candidates):
            self.fail("the first player was asked to break a one-way tie")

        self.assertIs(EventManager.SelectForcedEffect([only], refuse), only)


class TestForcedOrderLabels(unittest.TestCase):
    """The prompt labels are replay-visible and must be distinct.

    `CommandDescriptor.FindNewEffectIdInternal` narrows a recorded effect by the
    card it sits on and then, only if that left more than one, by display name.
    Every option built by `AskForcedOrder` sits on the first player's identity,
    so they share a card and the name is all that separates them -- and two
    forced abilities on one card sharing a name is exactly what MARVEL-40 makes
    selectable.
    """

    def setUp(self):
        self.rhino = FakeCard("Rhino")
        self.face = FakeFace(self.rhino)

    def test_distinct_names_are_left_alone(self):
        labels = EventManager.ForcedOrderLabels([
            FakeEffect("a", self.face, display_name="Deal 1 damage"),
            FakeEffect("b", self.face, display_name="Draw a card"),
        ])
        self.assertEqual(labels, ["Deal 1 damage", "Draw a card"])

    def test_duplicate_names_get_an_ordinal(self):
        labels = EventManager.ForcedOrderLabels([
            FakeEffect("a", self.face, display_name="Deal 1 damage"),
            FakeEffect("b", self.face, display_name="Deal 1 damage"),
        ])
        self.assertEqual(labels, ["Deal 1 damage #1", "Deal 1 damage #2"])

    def test_only_the_duplicated_name_is_numbered(self):
        # Prompts and recordings must not churn where there was no ambiguity.
        labels = EventManager.ForcedOrderLabels([
            FakeEffect("a", self.face, display_name="Surge"),
            FakeEffect("b", self.face, display_name="Draw a card"),
            FakeEffect("c", self.face, display_name="Surge"),
        ])
        self.assertEqual(labels, ["Surge #1", "Draw a card", "Surge #2"])

    def test_an_ordinal_does_not_collide_with_a_genuine_name(self):
        # The naive rule numbers the two "Attack"s 1 and 2 and leaves the third
        # name alone, emitting "Attack #2" twice -- once synthesised, once
        # genuine. That hands replay the exact ambiguity this function removes.
        labels = EventManager.ForcedOrderLabels([
            FakeEffect("a", self.face, display_name="Attack"),
            FakeEffect("b", self.face, display_name="Attack"),
            FakeEffect("c", self.face, display_name="Attack #2"),
        ])
        self.assertEqual(labels, ["Attack #1", "Attack #3", "Attack #2"])

    def test_an_ordinal_does_not_collide_with_an_earlier_ordinal(self):
        # Two duplicated groups whose ordinals could land on each other.
        labels = EventManager.ForcedOrderLabels([
            FakeEffect("a", self.face, display_name="A"),
            FakeEffect("b", self.face, display_name="A"),
            FakeEffect("c", self.face, display_name="A #1"),
            FakeEffect("d", self.face, display_name="A #1"),
        ])
        self.assertEqual(len(set(labels)), len(labels), labels)

    def test_labels_are_always_distinct(self):
        # The property the replay format actually depends on.
        for names in (
            ["X", "X", "X"],
            ["X", "Y", "X", "Y"],
            ["A", "B", "C"],
            ["", ""],
            # Adversarial: genuine names shaped like the ordinals we synthesise.
            ["Attack", "Attack", "Attack #2"],
            ["Attack", "Attack", "Attack #1", "Attack #2"],
            ["A #1", "A", "A", "A #2", "A #3"],
            ["Z #1", "Z #1", "Z"],
        ):
            labels = EventManager.ForcedOrderLabels(
                [FakeEffect(n or "blank", self.face, display_name=n) for n in names]
            )
            self.assertEqual(len(set(labels)), len(labels), names)
            self.assertEqual(len(labels), len(names), names)

    def test_one_label_per_candidate_in_order(self):
        candidates = [
            FakeEffect("a", self.face, display_name="First"),
            FakeEffect("b", self.face, display_name="Second"),
        ]
        self.assertEqual(len(EventManager.ForcedOrderLabels(candidates)), 2)


if __name__ == "__main__":
    unittest.main()


class TestConstantAbilitiesAreNotOrdered(unittest.TestCase):
    """A constant effect never initiates, so it is never put to the first player.

    The Rules Reference sentence at the top of this file is about abilities that
    *initiate*. A constant applies continuously -- there is no moment at which it
    starts, and so no order to choose. `game/event/manager.py` already excluded
    Status, setup, resource and delay abilities from the prompt; it did not
    exclude Constant.

    That was MARVEL-91, and it was not one card. 86 cards carry two or more
    forced Constant abilities on `WhenCardEnterPlay` and 78 were driven into a
    live game, every one of which stopped play to ask an unanswerable question:
    the two options had no names and produced identical boards. Gatekeeper
    (32044) gets its pair from one `GiveKeywordToAttached` call, which builds
    keyword grants and hit-point grants separately because
    `asset_helper.py` drops the first when the attached unit flips and keeps the
    second -- but 56 of the 78 simply declare two constants in the card script,
    which is why the fix cannot live in the factory.

    This test pins the guard as a rule rather than as a line of source. The
    behavioural half lives in `specs/rules/timing-priority.feature`, whose
    Interrupt-before-Boost scenarios could not be authored at all until this was
    fixed -- they need Gatekeeper attached to a minion.
    """

    def test_the_guard_excludes_constant_alongside_status(self):
        import inspect

        from game.ability.ability_type import TimingPriority
        from game.event.manager import EventManager as Manager

        source = inspect.getsource(Manager)
        self.assertIn("priority != TimingPriority.Constant", source,
                      "the forced-ordering guard no longer excludes Constant; "
                      "see MARVEL-91 before removing it")
        # `TimingPriority` is a plain Enum, so its members do not order with
        # `<`; the number is the priority and that is what the engine loops
        # over. Constant resolves before Status, so a batch reaching the prompt
        # has passed both exclusions.
        self.assertLess(TimingPriority.Constant.value, TimingPriority.Status.value)


class TestInternalCleanupsAreNotOrdered(unittest.TestCase):
    """An internal cleanup never initiates, so it is never put to the first player.

    MARVEL-95, and the same reasoning as MARVEL-91 one priority band over.
    `AbilityType.Temp0` and `Temp0UI` map to `TimingPriority.Rule`, and the
    ordering guard excluded `Status` and `Constant` but not these.

    **Where the pair actually comes from.** Not from one card carrying two
    constants -- `FaceEffect.RegisterTemp` files those under `global_effects`,
    and `EventManager.GetEffectCategory` sends anything with `flags.is_temp` to
    the `"Rule"` category, which `ProcessRuleEffect` resolves with no prompt at
    all. The cleanups that reach this prompt are registered *locally*, by
    `environment_helper2.apply_environment_internal`, and they go **onto the face
    being modified** rather than onto the card doing the modifying: one per
    continuous modifier, so the modifier can be taken back off when that face
    leaves play, flips, is set as another card, or the villain advances. A face
    under two such modifiers carries two, and they arrive in one batch.

    So the population is boards, not cards. `tools/determinism/probe_temp0_order.py`
    measured it: of 3999 cards, 2582 can be put into play, 61 of those put a
    cleanup onto some other face, and exactly one (Divided Loyalties, 50173)
    reaches two on one face by itself. Of the 1831 boards built from those --
    one single-card board and every pair of the 61 -- **1139 reached the prompt,
    2572 prompts in all**, every one of them entirely `Temp0` and every one of
    them traced to a single line, `environment_helper2.py:177`. Each of the 1139
    was then driven twice, with the first player forced onto candidate 0 and then
    onto candidate 1, and **all 1139 produced an identical state digest** with
    the two runs verified to have answered differently. The wide self-play matrix
    reaches one of them without any board being built for it: the Ultron facedown
    Drone Minion, whose two cleanups are the `Temp #1` / `Temp #2` labels in the
    replays.

    The exclusion is keyed on the **ability type**. The `Rule` band also carries
    `AbilityType.Rule`, `Challenge`, `Scenario`, `Campaign` and `DelayAbility`,
    every one of which is a real ability of a card or a scenario, and excluding
    the band would silence all of them. `test_the_exclusion_is_not_the_priority_band`
    is what stops that shortcut being taken later.
    """

    def setUp(self):
        self.card = FakeCard("Drone Minion")
        self.face = FakeFace(self.card)

    def Cleanup(self, label: str, ability_type: AbilityType = AbilityType.Temp0):
        return FakeEffect(label, self.face, ability_type=ability_type)

    ############################################################################
    # What the predicate says

    def test_two_cleanups_are_an_internal_batch(self):
        batch = [self.Cleanup("Temp #1"), self.Cleanup("Temp #2")]

        self.assertTrue(EventManager.IsInternalCleanupBatch(batch))

    def test_the_ui_variant_counts_as_a_cleanup(self):
        # `Temp0UI` subclasses `Temp0`'s flags but carries its own member, so an
        # identity test against `Temp0` alone would miss it.
        batch = [self.Cleanup("a", AbilityType.Temp0),
                 self.Cleanup("b", AbilityType.Temp0UI)]

        self.assertTrue(EventManager.IsInternalCleanupBatch(batch))

    def test_a_batch_of_card_abilities_is_still_ordered(self):
        batch = [FakeEffect("A", self.face), FakeEffect("B", self.face)]

        self.assertFalse(EventManager.IsInternalCleanupBatch(batch))

    def test_a_mixed_batch_is_still_ordered(self):
        # The cleanup does not initiate, but the card ability beside it does, so
        # there is a real order to choose and the prompt stays. No such batch is
        # reachable from the shipped pool as measured -- but the rule is written
        # down here rather than left to depend on that staying true.
        batch = [self.Cleanup("Temp #1"), FakeEffect("a real ability", self.face)]

        self.assertFalse(EventManager.IsInternalCleanupBatch(batch))

    def test_a_delay_ability_beside_cleanups_does_not_make_the_batch_orderable(self):
        # MARVEL-39: delay abilities are filtered out of the choice, so they are
        # not part of what would be asked and must not count as a real ability
        # here either. Reading this batch as "mixed" would put the prompt back.
        batch = [self.Cleanup("Temp #1"),
                 FakeEffect("D-delay", self.face, delay=True),
                 self.Cleanup("Temp #2")]

        self.assertTrue(EventManager.IsInternalCleanupBatch(batch))

    def test_a_batch_of_only_delay_abilities_is_not_a_cleanup_batch(self):
        # Nothing would be offered, so there is nothing to suppress. Answering
        # True here would be a claim about an empty candidate list.
        batch = [FakeEffect("D-delay", self.face, delay=True)]

        self.assertFalse(EventManager.IsInternalCleanupBatch(batch))

    def test_a_lone_cleanup_is_an_internal_batch(self):
        # `SelectForcedEffect` would not ask about one candidate anyway, but the
        # predicate is about what the batch *is*, and one cleanup is still a
        # cleanup. Keeping it honest here means the guard reads the same at
        # every size as the batch drains.
        self.assertTrue(EventManager.IsInternalCleanupBatch([self.Cleanup("Temp #1")]))

    ############################################################################
    # What the exclusion must not become

    def test_the_exclusion_is_not_the_priority_band(self):
        # Every one of these shares `TimingPriority.Rule` with Temp0. If the
        # guard is ever rewritten to exclude the band instead of the type, this
        # fails -- which is the point.
        from game.ability.ability_type import AbilityTypeFlags, TimingPriority

        band = (AbilityType.Rule, AbilityType.Challenge, AbilityType.Scenario,
                AbilityType.Campaign, AbilityType.DelayAbility)
        for ability_type in band:
            with self.subTest(ability_type=ability_type):
                self.assertEqual(
                    AbilityTypeFlags.TYPE_PRIORITY[ability_type],
                    TimingPriority.Rule,
                    "this type no longer shares the band with Temp0; the test "
                    "below is only meaningful while it does")
                batch = [FakeEffect("a", self.face, ability_type=ability_type),
                         FakeEffect("b", self.face, ability_type=ability_type)]
                self.assertFalse(EventManager.IsInternalCleanupBatch(batch))

        for ability_type in (AbilityType.Temp0, AbilityType.Temp0UI):
            with self.subTest(ability_type=ability_type):
                self.assertEqual(
                    AbilityTypeFlags.TYPE_PRIORITY[ability_type],
                    TimingPriority.Rule)

    ############################################################################
    # That the guard actually consults it

    def test_the_ordering_guard_consults_the_predicate(self):
        # The tripwire. Everything above tests the rule; this tests that
        # `ProcessForcedEffect` still applies it, which no test over the
        # predicate alone can see. Same shape as MARVEL-91's guard test below,
        # and for the same reason: the caller needs a live world and a real
        # first player, so the wiring is not reachable from a unit test.
        import inspect

        from game.event.manager import EventManager as Manager

        source = inspect.getsource(Manager.ProcessForcedEffect)
        self.assertIn("IsInternalCleanupBatch(forced_effects)", source,
                      "the forced-ordering guard no longer excludes internal "
                      "Temp0 cleanups; see MARVEL-95 before removing it")
