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
    def __init__(self, is_delay_ability: bool) -> None:
        self.flags = FakeFlags(is_delay_ability)


class FakeEffect:
    """The members of `Effect` the selection and the labels read."""

    def __init__(self, label: str, face: FakeFace, delay: bool = False,
                 display_name: str | None = None) -> None:
        self.label = label
        self.this = face
        self.ability = FakeAbility(delay)
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

    def test_labels_are_always_distinct(self):
        # The property the replay format actually depends on.
        for names in (
            ["X", "X", "X"],
            ["X", "Y", "X", "Y"],
            ["A", "B", "C"],
            ["", ""],
        ):
            labels = EventManager.ForcedOrderLabels(
                [FakeEffect(n or "blank", self.face, display_name=n) for n in names]
            )
            self.assertEqual(len(set(labels)), len(labels), names)

    def test_one_label_per_candidate_in_order(self):
        candidates = [
            FakeEffect("a", self.face, display_name="First"),
            FakeEffect("b", self.face, display_name="Second"),
        ]
        self.assertEqual(len(EventManager.ForcedOrderLabels(candidates)), 2)


if __name__ == "__main__":
    unittest.main()
