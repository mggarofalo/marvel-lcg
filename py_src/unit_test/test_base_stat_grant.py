"""Granting a base statistic sets it, and taking the grant away puts it back.

Three cards grant a whole base statistic line: Ultron Drones (`01140`, reprinted
as `26031`) and Controlled Innocents (`50032`). Their text is a *set*, not an
increment -- "has a base SCH of 1, a base ATK of 1, and a base hit points of 1"
-- and the three setters behind it agree: `SetBaseScheme`, `SetBaseAttack` and
`SetBaseHealth` each take the value the base becomes, compute their own
`value - base`, and move the keyword by that.

`ModelGain.Gain` called them as though they took a delta, multiplying the granted
value by the `+1`/`-1` that says whether an ability is being applied or removed:

    unit.SetBaseHealth(base_health * diff, effect)   # <-- MARVEL-103

On application `diff` is `+1` and the multiplication is invisible. On removal it
passes the *negation* of the value as the value to set, so tearing down a grant
of 1 asks for a base of -1 and the setter dutifully moves the statistic by -2 --
twice the grant, in the wrong direction, leaving the base negative.

Nothing in a shipped game noticed, for two accidents rather than one reason. The
only character that ever loses one of these grants is a facedown Drone Minion,
whose stat line is granted in full: the hit-point half of the teardown takes it
from 1 to -1, which kills it, and MARVEL-77's `IsInPlay()` guard then stops the
rest landing on the reverted card. Both accidents are about the grant being
exactly 1 and the unit not surviving. A unit that *survives* the teardown sees
the wrong value directly, which is what these tests drive.

Two shapes, because they fail differently:

- `Gain` called on a character with printed statistics, which is the call site
  under test with nothing between it and the assertion. Rhino prints 14 hit
  points, 2 ATK and 1 SCH, so every direction of the round trip is a real move.
- The shipped card, where the teardown is reached the way a game reaches it: a
  Drone Minion loses Ultron Drones while holding a second hit point from
  somewhere else, and has to walk away at 1 hit point rather than die.

The second hit point is granted by calling `GainHealthAndMaxHealth`, the same
operation Upgraded Drones (`01142`) performs, rather than by playing that card.
Upgraded Drones attaches *to Ultron Drones*, so it leaves play in the same breath
as its host and takes its hit point with it -- the drone then correctly has 0 hit
points and dies whichever way the arithmetic goes, which is exactly the hiding
place this test has to stay out of.

The engine has no card that grants a base statistic larger than 1, so the
`base_sch=3` case below is authored rather than shipped. It is here because the
defect scales with the grant: at 3 the teardown moved the base by -6.

See MARVEL-103. The board is built the way `unit_test/test_base_health_grant.py`
builds one.
"""

import unittest

# `engine` first, and not for its side effects: `game.*` modules import each
# other in a cycle that only resolves if `engine/__init__.py` has already walked
# it.
import engine  # noqa: F401  pylint: disable=unused-import

from game.puzzle.puzzle import RunPuzzle
from tools.spec.case import SpecCase, ThenStep
from tools.spec.harness import EnsureEngine, NewGameForCase
from tools.spec.policy import TranscriptPolicy


ULTRON_DRONES = "01140"          # the environment that grants the stat line
ANDROID_EFFICIENCY = "01144a"    # the treachery that makes a drone
AUNT_MAY = "01006"               # the filler under it: a support, no health
DRONE = "ultron_facedown_drone"

APPLY = +1                       # what `Gain` is passed when an ability starts
REMOVE = -1                      # ...and when it stops


def NewWorld():
    """A solo Rhino board: villain, main scheme, identity and nothing else."""
    EnsureEngine()
    case = SpecCase(
        name="a granted base statistic is set and put back",
        scenario="rhino",
        heroes=("spider_man",),
        beats=(ThenStep("Rhino", "health", 14),),
    )
    game = NewGameForCase(case, TranscriptPolicy())
    assert game.GameSetup()
    return game.world


class TheGrantOnACharacterWithPrintedStatistics(unittest.TestCase):
    """`Gain` driven directly, on a villain who is still standing afterwards."""

    def setUp(self):
        self.world = NewWorld()
        self.puzzle = RunPuzzle(self.world)
        self.rhino = self.puzzle.FindFaceByName("Rhino")
        self.assertIsNotNone(self.rhino)
        self.effect = self.puzzle.debug_rule

    def Apply(self, **grant):
        self.rhino.Gain(effect=self.effect, diff=APPLY, **grant)

    def Remove(self, **grant):
        self.rhino.Gain(effect=self.effect, diff=REMOVE, **grant)


class TestTheGrantSetsTheBase(TheGrantOnACharacterWithPrintedStatistics):
    """The application direction, stated as a property rather than an accident.

    `base_health * (+1)` is `base_health`, so this half reads the same before and
    after the fix for every shipped grant. It is pinned because it is the half
    that says which contract the call site is written to: a delta reading would
    have to take Rhino to 15 hit points here, and nothing in the assertions below
    would have to change to notice.
    """

    def test_it_replaces_printed_hit_points(self):
        self.assertEqual(self.rhino.health, 14)

        self.Apply(base_health=1)

        self.assertEqual(self.rhino.health, 1)
        self.assertEqual(self.rhino.max_health, 1)

    def test_replacing_hit_points_is_not_damage(self):
        self.Apply(base_health=1)

        self.assertEqual(self.rhino.sustained, 0)

    def test_it_replaces_printed_attack(self):
        self.assertEqual(self.rhino.attack, 2)

        self.Apply(base_atk=1)

        self.assertEqual(self.rhino.attack, 1)

    def test_it_replaces_printed_scheme(self):
        self.assertEqual(self.rhino.scheme, 1)

        self.Apply(base_sch=3)

        self.assertEqual(self.rhino.scheme, 3)


class TestRemovingTheGrantRestoresThePrintedStatistic(
        TheGrantOnACharacterWithPrintedStatistics):
    """The round trip. This is the half MARVEL-103 got wrong, on all three."""

    def test_hit_points_come_back(self):
        self.Apply(base_health=1)
        self.Remove(base_health=1)

        self.assertEqual(self.rhino.health, 14)
        self.assertEqual(self.rhino.max_health, 14)
        self.assertEqual(self.rhino.base_health, 14)

    def test_the_character_survives_losing_the_grant(self):
        # The consequence in the form a player would notice. Asking for a base
        # of -1 took Rhino's hit points below zero, `LimitHealth` called `Death`,
        # and a full-health villain was defeated and advanced a stage by an
        # ability *ending*.
        self.Apply(base_health=1)
        self.Remove(base_health=1)

        self.assertFalse(self.rhino.IsDefeated())
        self.assertEqual(self.puzzle.FindFaceByName("Rhino"), self.rhino)

    def test_the_round_trip_leaves_no_damage(self):
        self.Apply(base_health=1)
        self.Remove(base_health=1)

        self.assertEqual(self.rhino.sustained, 0)

    def test_attack_comes_back(self):
        self.Apply(base_atk=1)
        self.Remove(base_atk=1)

        self.assertEqual(self.rhino.attack, 2)
        self.assertEqual(self.rhino.base_attack, 2)

    def test_scheme_comes_back(self):
        self.Apply(base_sch=3)
        self.Remove(base_sch=3)

        self.assertEqual(self.rhino.scheme, 1)
        self.assertEqual(self.rhino.base_scheme, 1)

    def test_a_grant_equal_to_the_printed_value_still_comes_back(self):
        # Rhino already schemes for 1, so applying `base_sch=1` moves nothing and
        # `SetBaseScheme` returns early. The teardown was wrong anyway, because
        # it never read the current base -- it was handed -1 as the value to set.
        self.Remove(base_sch=1)

        self.assertEqual(self.rhino.scheme, 1)
        self.assertEqual(self.rhino.base_scheme, 1)

    def test_it_leaves_no_negative_residue_on_the_keyword(self):
        # `attack` and `scheme` are `max(0, ...)`, so a keyword left at -1 reads
        # as 0 and hides until something else adds to it. The raw keyword is what
        # a second source would be added to, and what the state digest's
        # `attack`/`scheme` fields are computed from.
        self.Apply(base_atk=1, base_sch=1)
        self.Remove(base_atk=1, base_sch=1)

        self.assertEqual(self.rhino.GetKeyword('ATK'), 2)
        self.assertEqual(self.rhino.GetKeyword('SCH'), 1)

    def test_the_whole_line_round_trips_at_once(self):
        # The three keywords are torn down in one `Gain` call in a real game, so
        # drive them that way too: hit points are last, and a defect in either of
        # the first two would previously be resolved on a corpse.
        self.Apply(base_sch=1, base_atk=1, base_health=1)
        self.Remove(base_sch=1, base_atk=1, base_health=1)

        self.assertEqual(self.rhino.health, 14)
        self.assertEqual(self.rhino.attack, 2)
        self.assertEqual(self.rhino.scheme, 1)


class TestAGrantedBaseOfZeroIsStillAGrant(
        TheGrantOnACharacterWithPrintedStatistics):
    """Zero is a value a base-statistic grant can state, not the absence of one.

    A base grant *replaces* the printed line, so "has a base ATK of 0" is how a
    card would say this character cannot attack -- and it has to be
    distinguishable from a card that grants no base attack at all. Every other
    keyword in `Gain` draws that line with `!= None`; these three tested
    truthiness, so a granted 0 was indistinguishable from an absent grant and was
    silently dropped.

    Nothing passes 0 today. The `int|None=None` declaration is repeated at all
    three layers of the call chain -- `AbilityFactory.GiveKeywordToInPlayWhenApplyThis`
    (`game/ability/factory/environment.py`), which gates building the ability at
    all on `base_sch != None or base_atk != None or base_health != None`,
    `GiveKeywordToInPlayWhenApplyThisInternal`
    (`game/ability/factory/environment_helper.py`), and `Gain` itself -- and the
    only two card scripts that reach it, Ultron Drones (`01140`) and Controlled
    Innocents (`50032`), pass the literal 1. So "absent" already means `None`
    everywhere the value is decided; `Gain` was the one place that also read 0
    that way.

    Hit points are included even though "a base hit points of 0" is not a card
    anyone has printed, because it is the same guard and the same mistake. What
    it asserts is that the grant *lands*; that landing on 0 hit points is lethal
    is the health rules doing their job, and is asserted as such.
    """

    def test_a_granted_base_attack_of_zero_replaces_printed_attack(self):
        self.assertEqual(self.rhino.attack, 2)

        self.Apply(base_atk=0)

        self.assertEqual(self.rhino.attack, 0)
        self.assertEqual(self.rhino.base_attack, 0)

    def test_a_granted_base_attack_of_zero_comes_back_off(self):
        self.Apply(base_atk=0)
        self.Remove(base_atk=0)

        self.assertEqual(self.rhino.attack, 2)
        self.assertEqual(self.rhino.base_attack, 2)

    def test_a_granted_base_scheme_of_zero_replaces_printed_scheme(self):
        self.assertEqual(self.rhino.scheme, 1)

        self.Apply(base_sch=0)

        self.assertEqual(self.rhino.scheme, 0)
        self.assertEqual(self.rhino.base_scheme, 0)

    def test_a_granted_base_scheme_of_zero_comes_back_off(self):
        self.Apply(base_sch=0)
        self.Remove(base_sch=0)

        self.assertEqual(self.rhino.scheme, 1)
        self.assertEqual(self.rhino.base_scheme, 1)

    def test_a_granted_base_of_zero_hit_points_replaces_printed_hit_points(self):
        self.assertEqual(self.rhino.health, 14)

        self.Apply(base_health=0)

        self.assertEqual(self.rhino.base_health, 0)

    def test_a_character_granted_zero_hit_points_is_defeated(self):
        # The consequence, stated separately from the grant landing: 0 hit points
        # is 0 hit points whether it was printed or granted, so the stage the
        # grant landed on is defeated and Rhino advances to stage II.
        #
        # Read through `IsThisFaceUp` on the stage-I face rather than through
        # `IsDefeated` or `health`. Both of those go to `components.health`,
        # which belongs to the *card* and now holds stage II's 15 hit points --
        # so the defeated face reports itself alive with 15, and the villain
        # standing there is a different card face than the one that was granted.
        self.assertTrue(self.rhino.IsThisFaceUp())

        self.Apply(base_health=0)

        self.assertFalse(self.rhino.IsThisFaceUp())
        self.assertIsNot(self.puzzle.FindFaceByName("Rhino"), self.rhino)

    def test_an_absent_grant_is_still_absent(self):
        # The other side of the same line, and the one that would break if the
        # guard were widened past `!= None`: passing nothing must touch nothing.
        self.Apply()
        self.Remove()

        self.assertEqual(self.rhino.health, 14)
        self.assertEqual(self.rhino.attack, 2)
        self.assertEqual(self.rhino.scheme, 1)
        self.assertEqual(self.rhino.base_health, 14)
        self.assertEqual(self.rhino.base_attack, 2)
        self.assertEqual(self.rhino.base_scheme, 1)


class TestTheGrantAtDiffZero(TheGrantOnACharacterWithPrintedStatistics):
    """`diff == 0` is neither an application nor a removal, and must do nothing.

    `diff` reaches `Gain` from `environment_helper.gain_keyword` as
    `StoreValueDiff.diff`, the *numeric* part of one source's contribution. That
    part is 0 whenever the source's contribution changed only in its trait list:
    `WhenFaceApplyThisInternal.try_apply_environment` enters
    `apply_environment_internal` on `diff.diff > 0 or diff.lst_add`, and
    `unapply_environment_internal` on `diff.diff < 0 or diff.lst_del`, so either
    side can arrive carrying a list and a numeric 0.

    A base statistic is not carried by that list, so nothing about it changed and
    nothing about it may move. That is the contract; the `diff > 0` / `diff < 0`
    branching implements it, but it implements it as a side effect of asking
    about the sign rather than by saying so. Collapsing the branches back into a
    single unconditional call -- which is what the code looked like before
    MARVEL-103 -- would make `diff == 0` set the base to `base_x * 0`, destroying
    the statistic outright. This pins the no-op so that collapse fails a test
    rather than waiting for the first card that combines `base_*` with `trait=`.

    No such card exists, so this is a contract test and not a reproduction: the
    two cards that grant a base line pass no `get_new_value`, so their
    `StoreValue` is the default `StoreValue(1, [])` and their `diff` is only ever
    +1 or -1. `Gain` is therefore driven directly here rather than through a
    card, which is also the only shape available -- `cards/pack/` would have to
    gain a card that does not exist to reach it any other way.
    """

    def test_it_does_not_move_a_base_statistic(self):
        self.rhino.Gain(effect=self.effect, diff=0,
                        base_sch=1, base_atk=1, base_health=1)

        self.assertEqual(self.rhino.base_health, 14)
        self.assertEqual(self.rhino.base_attack, 2)
        self.assertEqual(self.rhino.base_scheme, 1)

    def test_it_does_not_move_the_visible_statistic_either(self):
        self.rhino.Gain(effect=self.effect, diff=0,
                        base_sch=1, base_atk=1, base_health=1)

        self.assertEqual(self.rhino.health, 14)
        self.assertEqual(self.rhino.max_health, 14)
        self.assertEqual(self.rhino.attack, 2)
        self.assertEqual(self.rhino.scheme, 1)

    def test_it_does_not_defeat_the_character(self):
        # The failure mode the collapsed form had: `base_health * 0` is 0, which
        # is a request to set Rhino's hit points to nothing.
        self.rhino.Gain(effect=self.effect, diff=0, base_health=1)

        self.assertFalse(self.rhino.IsDefeated())

    def test_it_does_nothing_after_the_grant_has_been_applied(self):
        # The same no-op, read from the other end: a trait-only update arriving
        # while a base grant is live must leave the granted value alone rather
        # than reverting it to printed.
        self.Apply(base_atk=1, base_sch=1)

        self.rhino.Gain(effect=self.effect, diff=0, base_atk=1, base_sch=1)

        self.assertEqual(self.rhino.base_attack, 1)
        self.assertEqual(self.rhino.base_scheme, 1)

    def test_a_granted_base_of_zero_at_diff_zero_is_still_a_no_op(self):
        # The two findings meet here: widening the guard to `!= None` must not
        # let a granted 0 through at `diff == 0` either.
        self.rhino.Gain(effect=self.effect, diff=0,
                        base_sch=0, base_atk=0, base_health=0)

        self.assertEqual(self.rhino.base_health, 14)
        self.assertEqual(self.rhino.base_attack, 2)
        self.assertEqual(self.rhino.base_scheme, 1)


class TestADroneThatOutlivesUltronDrones(unittest.TestCase):
    """The shipped teardown, on a drone that has somewhere to land.

    A drone with only its granted hit point dies when Ultron Drones leaves, and
    that is correct -- 0 hit points is 0 hit points. It is also what hid this for
    as long as it hid: the card being defeated reverts it, `Card.Reset` zeroes
    the health component, and whatever the arithmetic did to the base is thrown
    away with the face. Give the drone one more hit point and there is a survivor
    to read.
    """

    def setUp(self):
        self.world = NewWorld()
        self.puzzle = RunPuzzle(self.world)
        self.puzzle.CreatePlayerDeck(AUNT_MAY)
        self.puzzle.PutIntoPlay(ULTRON_DRONES)
        self.puzzle.Reveal(ANDROID_EFFICIENCY)
        self.assertIsNotNone(self.FindDrone(), "no drone was made; the rest is vacuous")

    def FindDrone(self):
        for card in self.world.object_manager.card_dict.values():
            if card.face.paper.card_id == DRONE:
                return card
        return None

    def GiveASecondHitPoint(self):
        drone = self.FindDrone()
        drone.face.GainHealthAndMaxHealth(1, self.puzzle.debug_rule)
        self.assertEqual(drone.face.max_health, 2)
        self.assertEqual(drone.face.health, 2)

    def test_a_drone_with_only_its_granted_hit_point_still_dies(self):
        # The case every game reaches, and the one that must not change.
        self.puzzle.Remove(ULTRON_DRONES)

        self.assertIsNone(self.FindDrone())

    def test_a_drone_with_a_second_hit_point_survives(self):
        self.GiveASecondHitPoint()

        self.puzzle.Remove(ULTRON_DRONES)

        self.assertIsNotNone(self.FindDrone())

    def test_the_survivor_keeps_exactly_the_hit_point_it_did_not_lose(self):
        self.GiveASecondHitPoint()

        self.puzzle.Remove(ULTRON_DRONES)

        drone = self.FindDrone()
        self.assertEqual(drone.face.max_health, 1)
        self.assertEqual(drone.face.health, 1)
        self.assertEqual(drone.face.sustained, 0)

    def test_the_survivor_is_back_to_its_printed_line(self):
        self.GiveASecondHitPoint()

        self.puzzle.Remove(ULTRON_DRONES)

        face = self.FindDrone().face
        self.assertEqual(face.base_health, 0)
        self.assertEqual(face.base_attack, 0)
        self.assertEqual(face.base_scheme, 0)
        self.assertEqual(face.GetKeyword('ATK'), 0)
        self.assertEqual(face.GetKeyword('SCH'), 0)

    def test_the_survivor_is_still_a_minion_on_the_board(self):
        # A drone left at a negative base is not a drone anyone can interact
        # with; assert it is still where a player would find it.
        self.GiveASecondHitPoint()

        self.puzzle.Remove(ULTRON_DRONES)

        self.assertEqual(self.FindDrone().area.deck_type.name, "EngagedEnemiesArea")


if __name__ == "__main__":
    unittest.main()
