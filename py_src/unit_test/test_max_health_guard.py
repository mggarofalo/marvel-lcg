"""Max health is only edited while the face is in play.

`components.health` belongs to the **card**, not to the face. A face that leaves
play may have had it zeroed underneath it -- `Card.Reset` runs
`Health.OnParentReset` -- so a mutation aimed at the face it was in play as can
land on a component that no longer describes it.

`CanHealth.UpdateHealth` has always guarded against that (`if not
self.IsInPlay(): return None`). `UpdateMaxHealth` did not, and the two halves of
`GainHealthAndMaxHealth` therefore disagreed about which object they were
editing whenever the health half killed the unit:

    GainOnlyHealth(-1)      1 HP -> 0 -> Death -> card reverts, component zeroed
    GainOnlyMaxHealth(-1)   0 - 1 = -1                     <-- on a fresh component

Which is how a facedown Ultron drone made from `01043b` "Wakanda Forever!" ended
a game at `max_health = -1`: `01142` Upgraded Drones left play, the environment
teardown removed its +1 HP grant from every drone, and the one drone sitting at
1 HP died to the first half of its own removal. See MARVEL-77.

These tests drive the two mutators against a fake, because what is under test is
the guard and not the drone. That the whole sequence no longer trips the checker
is pinned by the run itself, not here -- the four-hero ultron seeds it aborted
now complete.
"""

import unittest

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from game.card.face.attribute.can_health import CanHealth


class FakeHealth:
    """The card-owned component. `Reset` is what leaving play does to it."""

    def __init__(self, health=0, max_health=0):
        self.health = health
        self.max_health = max_health

    def AddMaxHealth(self, value):
        self.max_health += value

    def GetMaxHealth(self):
        return self.max_health

    def Reset(self):
        self.health = 0
        self.max_health = 0


class FakeComponents:
    def __init__(self, health):
        self.health = health


class FakeFace:
    """Just enough of a face for `UpdateMaxHealth` to run against."""

    UpdateMaxHealth = CanHealth.UpdateMaxHealth

    def __init__(self, *, in_play=True, health=0, max_health=0):
        self.in_play = in_play
        self.components = FakeComponents(FakeHealth(health, max_health))

    def IsInPlay(self):
        return self.in_play


class TestTheGuard(unittest.TestCase):

    def test_a_face_in_play_is_edited(self):
        face = FakeFace(max_health=3)

        face.UpdateMaxHealth(-1, None)

        self.assertEqual(face.components.health.max_health, 2)

    def test_a_face_out_of_play_is_not(self):
        face = FakeFace(in_play=False, max_health=3)

        face.UpdateMaxHealth(-1, None)

        self.assertEqual(face.components.health.max_health, 3)

    def test_the_guard_is_not_a_clamp(self):
        # A negative result is still reachable for a face that *is* in play, and
        # deliberately so: the rule this protects reads the component, and a
        # clamp inside the mutator would hide the next defect of this shape
        # rather than report it. MARVEL-77 rejected that fix for the same reason
        # the determinism audit kept the `health/max-negative` rule at all.
        face = FakeFace(max_health=0)

        face.UpdateMaxHealth(-1, None)

        self.assertEqual(face.components.health.max_health, -1)


class TestTheDroneSequence(unittest.TestCase):
    """The paired mutation, with the death that used to fall between its halves.

    `GainHealthAndMaxHealth` runs the health half first when the value is
    negative, so the unit can be defeated -- and the card reverted, and the
    component reset -- before the max-health half runs.
    """

    def Sequence(self, face):
        """Remove a +1 HP grant the way environment teardown does."""
        # The health half. In the engine this is `GainOnlyHealth` ->
        # `UpdateHealth` -> `LimitHealth` -> `Death`; what matters to the second
        # half is only its effect on the card: reverted face, component reset.
        face.components.health.health -= 1
        if face.components.health.health <= 0:
            face.in_play = False
            face.components.health.Reset()

        # The max-health half.
        face.UpdateMaxHealth(-1, None)

    def test_a_drone_that_survives_loses_the_grant(self):
        face = FakeFace(health=2, max_health=3)

        self.Sequence(face)

        self.assertEqual(face.components.health.health, 1)
        self.assertEqual(face.components.health.max_health, 2)

    def test_a_drone_the_removal_kills_does_not_go_negative(self):
        face = FakeFace(health=1, max_health=3)

        self.Sequence(face)

        self.assertEqual(face.components.health.max_health, 0)


if __name__ == "__main__":
    unittest.main()
