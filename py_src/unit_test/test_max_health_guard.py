"""Max health is only edited while the exact face is in play.

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

There is a second way for the card-owned component to change underneath a face:
villain stages reuse one card object, and `CardFace.IsInPlay()` treats another
face with the same name as equivalent unless asked for the exact face. Removing
The "Immortal" Klaw can defeat stage I, advance to stage II, and reveal the side
scheme again between the current-health and max-health halves of one teardown.
The old stage-I face then still reports itself in play and used to subtract 10
from stage II's maximum, producing 28/18 hit points solo and 46/36 with 2 heroes.
See MARVEL-123.

The small tests drive the mutator against a fake so each branch of the guard is
explicit. The final tests build the shipped Klaw board and reproduce the nested
stage transition with both one and 2 heroes.
"""

import unittest

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from game.card.face.attribute.can_health import CanHealth
from game.puzzle.puzzle import RunPuzzle
from game.world import invariants
from tools.spec.case import SpecCase, ThenStep
from tools.spec.harness import EnsureEngine, NewGameForCase
from tools.spec.policy import TranscriptPolicy


IMMORTAL_KLAW = "01127"


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

    def __init__(self, *, in_play=True, same_face=True, health=0, max_health=0):
        self.in_play = in_play
        self.same_face = same_face
        self.components = FakeComponents(FakeHealth(health, max_health))

    def IsInPlay(self, *, is_same_face=False):
        return self.in_play and (not is_same_face or self.same_face)


class TestTheGuard(unittest.TestCase):

    def test_a_face_in_play_is_edited(self):
        face = FakeFace(max_health=3)

        face.UpdateMaxHealth(-1, None)

        self.assertEqual(face.components.health.max_health, 2)

    def test_a_face_out_of_play_is_not(self):
        face = FakeFace(in_play=False, max_health=3)

        face.UpdateMaxHealth(-1, None)

        self.assertEqual(face.components.health.max_health, 3)

    def test_a_retired_face_on_an_in_play_card_is_not_edited(self):
        # Villain stages use this shape: the shared card is still on the board,
        # but another same-name face is now face up on it.
        face = FakeFace(in_play=True, same_face=False, max_health=18)

        face.UpdateMaxHealth(-10, None)

        self.assertEqual(face.components.health.max_health, 18)

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


def NewKlawWorld(*heroes):
    """Build the normal Klaw stage-I board for the named players."""
    EnsureEngine()
    case = SpecCase(
        name="a health-grant teardown advances the villain",
        scenario="klaw",
        heroes=heroes,
        beats=(ThenStep("Klaw", "health", 1),),
    )
    game = NewGameForCase(case, TranscriptPolicy())
    assert game.GameSetup()
    return game.world


class TestTheKlawStageTransition(unittest.TestCase):
    """The shipped nested transition that exposed the stale-face guard."""

    def Reproduce(self, heroes, damage, expected_health):
        world = NewKlawWorld(*heroes)
        puzzle = RunPuzzle(world)
        side_scheme = puzzle.FindOrCreateFace(IMMORTAL_KLAW)
        puzzle.PutIntoPlay(side_scheme)

        stage_one = world.GetScenario().GetVillain(None)
        puzzle.Damage(stage_one, damage)
        self.assertGreater(stage_one.health, 0,
                           "the side scheme removal, not the damage, must advance")

        # Defeating the side scheme removes its +10 hit points. That defeats
        # stage I, stage II searches the discard pile and reveals the same side
        # scheme again, and only then does the original teardown resume.
        puzzle.SetThreat(side_scheme, 0)

        stage_two = world.GetScenario().GetVillain(None)
        self.assertEqual(stage_two.paper.card_id, "01114")
        self.assertEqual(side_scheme.card.area.deck_type.name, "SideSchemesArea")
        self.assertEqual(stage_two.health, expected_health)
        self.assertEqual(stage_two.max_health, expected_health)
        self.assertEqual(stage_two.sustained, 0)
        self.assertEqual(invariants.Check(world), [])

    def test_the_reported_solo_numbers_finish_at_28_of_28(self):
        # Stage I is 12 + 10. Fifteen damage leaves 7; losing the grant defeats
        # it. Stage II is 18 + the newly revealed grant and starts undamaged.
        self.Reproduce(("captain_marvel",), damage=15, expected_health=28)

    def test_two_heroes_finish_at_46_of_46(self):
        # The per-hero lines are 24 and 36; the side scheme remains a flat +10.
        # Twenty-five damage leaves stage I at 9 before its grant is removed.
        self.Reproduce(
            ("captain_marvel", "iron_man"),
            damage=25,
            expected_health=46,
        )


if __name__ == "__main__":
    unittest.main()
