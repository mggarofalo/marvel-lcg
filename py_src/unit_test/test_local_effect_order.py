"""`EventManager.FindLocalEffects` decides forced-ability resolution order.

`Message2.related_faces` is a `Set[CardFace]` and `CardFace` defines no
`__hash__`, so it iterates by memory address. The list built from it reaches
`ProcessForcedEffect`, which resolves forced abilities in list order and offers
the first player the tie-break in list order. Sorting by `Effect.object_id`
makes that a property of the game state instead of the allocator.

See MARVEL-31 and `docs/determinism-audit.md` (F2).
"""

import unittest

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported. That is the whole bootstrap this needs.
import engine  # noqa: F401  pylint: disable=unused-import

from game.event.manager import EventManager
from game.message import Message


class TriggeringMessage:
    """The message type the abilities under test listen for."""


class UnrelatedMessage:
    """A message type they do not listen for."""


class FakeAbility:
    def __init__(self, when) -> None:
        self.when = when


class FakeEffect:
    """The two members of `Effect` that `FindLocalEffects` reads."""

    def __init__(self, object_id: int, when=TriggeringMessage) -> None:
        self.object_id = object_id
        self.ability = FakeAbility(when)

    def __repr__(self) -> str:
        return f"effect#{self.object_id}"


class FakeFace:
    """A card face carrying local effects, as `face.effect.local_effects`."""

    def __init__(self, *effects: FakeEffect) -> None:
        self.effect = type("FaceEffect", (), {"local_effects": list(effects)})()


class FakeMessage(TriggeringMessage):
    def __init__(self, *faces: FakeFace) -> None:
        self.related_faces = set(faces)


class TestFindLocalEffectsOrdering(unittest.TestCase):

    def test_effects_come_back_in_object_id_order(self):
        late = FakeEffect(30)
        early = FakeEffect(7)
        message = FakeMessage(FakeFace(late), FakeFace(early))

        self.assertEqual(EventManager.FindLocalEffects(message), [early, late])

    def test_effects_on_one_face_are_reordered_too(self):
        # `face.effect.local_effects` is a list, so this order is already
        # stable -- but it is registration order within the face, not creation
        # order across the message. `ProcessForcedEffect` documents the latter.
        second = FakeEffect(9)
        first = FakeEffect(2)
        message = FakeMessage(FakeFace(second, first))

        self.assertEqual(EventManager.FindLocalEffects(message), [first, second])

    def test_face_arrival_order_does_not_change_the_result(self):
        # Enough faces that identity-hash iteration order is very unlikely to
        # coincide with sorted order by chance.
        effects = [FakeEffect(object_id) for object_id in (91, 4, 57, 12, 38, 6, 73, 25, 60, 1)]
        faces = [FakeFace(effect) for effect in effects]

        forward = EventManager.FindLocalEffects(FakeMessage(*faces))
        reverse = EventManager.FindLocalEffects(FakeMessage(*reversed(faces)))

        self.assertEqual(forward, sorted(effects, key=lambda e: e.object_id))
        self.assertEqual(forward, reverse)

    def test_abilities_listening_for_another_message_are_skipped(self):
        listening = FakeEffect(5)
        deaf = FakeEffect(1, when=UnrelatedMessage)
        message = FakeMessage(FakeFace(deaf, listening))

        self.assertEqual(EventManager.FindLocalEffects(message), [listening])

    def test_no_matching_effects_gives_an_empty_list(self):
        message = FakeMessage(FakeFace(FakeEffect(3, when=UnrelatedMessage)))

        self.assertEqual(EventManager.FindLocalEffects(message), [])

    def test_paying_resources_short_circuits_to_the_paying_effect(self):
        # This branch bypasses `related_faces` entirely; sorting must not have
        # changed it. `Message2.__init__` wants a live world, and nothing here
        # needs one.
        paying = object.__new__(Message.WhenPlayerPayingResources)
        paying.by_effect = FakeEffect(404)
        paying.related_faces = {FakeFace(FakeEffect(1))}

        self.assertEqual(EventManager.FindLocalEffects(paying), [paying.by_effect])


if __name__ == "__main__":
    unittest.main()
