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
    """The two members of `Effect` the selection reads: `ability` and `this`."""

    def __init__(self, label: str, face: FakeFace, delay: bool = False) -> None:
        self.label = label
        self.this = face
        self.ability = FakeAbility(delay)

    def __repr__(self) -> str:
        return self.label


def Decline(faces):
    return None


class Chooser:
    """A first player who picks the face at `pick`, and records what was offered."""

    def __init__(self, pick: int) -> None:
        self.pick = pick
        self.offered = None

    def __call__(self, faces):
        self.offered = list(faces)
        return faces[self.pick]


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

        self.assertEqual(chooser.offered, [a.this, c.this])

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

    def test_abilities_on_one_card_take_list_order(self):
        # Current behaviour, pinned so MARVEL-40 has to change it deliberately:
        # a same-card batch is not put to the first player at all. The Rules
        # Reference draws no such distinction.
        face = FakeFace(self.rhino)
        first = FakeEffect("first", face)
        second = FakeEffect("second", face)

        def refuse(faces):
            self.fail("the first player was asked, which MARVEL-40 has not landed yet")

        self.assertIs(EventManager.SelectForcedEffect([first, second], refuse), first)


if __name__ == "__main__":
    unittest.main()
