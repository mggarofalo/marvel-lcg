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


class TwoLiveGrantsOnOneCharacter(unittest.TestCase):
    """Two sources granting one base statistic to one character at the same time.

    A base grant used to be a single stored value: applying wrote it, removing
    wrote the *printed* statistic back. With two sources live, whichever ended
    first therefore wiped the other's grant and the character reverted to printed
    with a grant still active. See MARVEL-111.

    The rules answer is that the newest grant is the one showing and the older
    one is still there underneath, so ending the newest falls back to the older
    and ending the older changes nothing visible. That is an ordered stack, and
    the identity it is keyed on is the granting `Effect`:
    `WhenFaceApplyThisInternal.when_this_in_play` binds one per granting card and
    hands the same object to both the apply and the unapply path.

    Two `DebugRule`s built on the *same* face stand in for the two sources here,
    deliberately: `GainKeyword` attributes a contribution to `by_effect.this`, so
    sharing a face means nothing about these tests can pass by telling the two
    sources apart any other way than by the `Effect` object. The shipped pair of
    distinct cards is driven separately, in
    `TestTwoCopiesOfUltronDronesOnOneDrone`.

    The grants carry different values throughout, because equal values make
    every ordering look alike and would pass whichever entry removal dropped.

    A third source is available for the ordering tests, and it is not
    decoration. With only two grants live, removing one leaves exactly one
    behind, and "the newest of what is left" and "the oldest of what is left"
    name the same entry -- three separate wrong orderings pass every two-grant
    test. `TestOrderingNeedsThreeGrants` is where they fail.
    """

    def setUp(self):
        from game.effect.rule import DebugRule

        self.world = NewWorld()
        self.puzzle = RunPuzzle(self.world)
        self.rhino = self.puzzle.FindFaceByName("Rhino")
        self.assertIsNotNone(self.rhino)

        sentinel = self.world.object_manager.card_dict[0].face
        self.older = DebugRule(sentinel, display_name="older grant")
        self.newer = DebugRule(sentinel, display_name="newer grant")
        self.newest = DebugRule(sentinel, display_name="newest grant")
        self.assertEqual(len({id(self.older), id(self.newer), id(self.newest)}), 3)

    def Apply(self, source, **grant):
        self.rhino.Gain(effect=source, diff=APPLY, **grant)

    def Remove(self, source, **grant):
        self.rhino.Gain(effect=source, diff=REMOVE, **grant)


class TestTwoLiveSchemeGrants(TwoLiveGrantsOnOneCharacter):
    """Rhino prints SCH 1. The two grants are 3 and then 5."""

    def ApplyBoth(self):
        self.Apply(self.older, base_sch=3)
        self.Apply(self.newer, base_sch=5)

    def test_the_newest_grant_is_the_one_showing(self):
        self.ApplyBoth()

        self.assertEqual(self.rhino.scheme, 5)
        self.assertEqual(self.rhino.base_scheme, 5)

    def test_ending_the_older_grant_leaves_the_newer_one_showing(self):
        # The out-of-order removal, and the case the single stored value got
        # wrong: this used to put Rhino back on his printed SCH of 1 while a
        # grant of 5 was still live.
        self.ApplyBoth()

        self.Remove(self.older, base_sch=3)

        self.assertEqual(self.rhino.scheme, 5)
        self.assertEqual(self.rhino.base_scheme, 5)

    def test_ending_the_newer_grant_falls_back_to_the_older_one(self):
        self.ApplyBoth()

        self.Remove(self.newer, base_sch=5)

        self.assertEqual(self.rhino.scheme, 3)
        self.assertEqual(self.rhino.base_scheme, 3)

    def test_ending_both_oldest_first_restores_printed(self):
        self.ApplyBoth()

        self.Remove(self.older, base_sch=3)
        self.Remove(self.newer, base_sch=5)

        self.assertEqual(self.rhino.scheme, 1)
        self.assertEqual(self.rhino.base_scheme, 1)
        self.assertEqual(self.rhino.GetKeyword('SCH'), 1)

    def test_ending_both_newest_first_restores_printed(self):
        self.ApplyBoth()

        self.Remove(self.newer, base_sch=5)
        self.Remove(self.older, base_sch=3)

        self.assertEqual(self.rhino.scheme, 1)
        self.assertEqual(self.rhino.base_scheme, 1)
        self.assertEqual(self.rhino.GetKeyword('SCH'), 1)


class TestTwoLiveAttackGrants(TwoLiveGrantsOnOneCharacter):
    """Rhino prints ATK 2. The two grants are 3 and then 5."""

    def ApplyBoth(self):
        self.Apply(self.older, base_atk=3)
        self.Apply(self.newer, base_atk=5)

    def test_the_newest_grant_is_the_one_showing(self):
        self.ApplyBoth()

        self.assertEqual(self.rhino.attack, 5)
        self.assertEqual(self.rhino.base_attack, 5)

    def test_ending_the_older_grant_leaves_the_newer_one_showing(self):
        self.ApplyBoth()

        self.Remove(self.older, base_atk=3)

        self.assertEqual(self.rhino.attack, 5)
        self.assertEqual(self.rhino.base_attack, 5)

    def test_ending_the_newer_grant_falls_back_to_the_older_one(self):
        self.ApplyBoth()

        self.Remove(self.newer, base_atk=5)

        self.assertEqual(self.rhino.attack, 3)
        self.assertEqual(self.rhino.base_attack, 3)

    def test_ending_both_oldest_first_restores_printed(self):
        self.ApplyBoth()

        self.Remove(self.older, base_atk=3)
        self.Remove(self.newer, base_atk=5)

        self.assertEqual(self.rhino.attack, 2)
        self.assertEqual(self.rhino.base_attack, 2)
        self.assertEqual(self.rhino.GetKeyword('ATK'), 2)

    def test_ending_both_newest_first_restores_printed(self):
        self.ApplyBoth()

        self.Remove(self.newer, base_atk=5)
        self.Remove(self.older, base_atk=3)

        self.assertEqual(self.rhino.attack, 2)
        self.assertEqual(self.rhino.base_attack, 2)
        self.assertEqual(self.rhino.GetKeyword('ATK'), 2)


class TestTwoLiveHitPointGrants(TwoLiveGrantsOnOneCharacter):
    """Rhino prints 14 hit points. The two grants are 10 and then 6.

    Hit points are where the ordering stops being bookkeeping: `SetBaseHealth`
    moves current health along with max, so falling back to the wrong entry is a
    character gaining or losing hit points, not a stale field.
    """

    def ApplyBoth(self):
        self.Apply(self.older, base_health=10)
        self.Apply(self.newer, base_health=6)

    def test_the_newest_grant_is_the_one_showing(self):
        self.ApplyBoth()

        self.assertEqual(self.rhino.health, 6)
        self.assertEqual(self.rhino.max_health, 6)
        self.assertEqual(self.rhino.base_health, 6)

    def test_ending_the_older_grant_leaves_the_newer_one_showing(self):
        self.ApplyBoth()

        self.Remove(self.older, base_health=10)

        self.assertEqual(self.rhino.health, 6)
        self.assertEqual(self.rhino.max_health, 6)
        self.assertEqual(self.rhino.base_health, 6)

    def test_ending_the_older_grant_is_not_a_heal(self):
        # Reading the same no-op through what a player would see: nothing about
        # the character's hit points moved, so it took no damage and healed none.
        self.ApplyBoth()

        self.Remove(self.older, base_health=10)

        self.assertEqual(self.rhino.sustained, 0)

    def test_ending_the_newer_grant_falls_back_to_the_older_one(self):
        self.ApplyBoth()

        self.Remove(self.newer, base_health=6)

        self.assertEqual(self.rhino.health, 10)
        self.assertEqual(self.rhino.max_health, 10)
        self.assertEqual(self.rhino.base_health, 10)

    def test_ending_both_oldest_first_restores_printed(self):
        self.ApplyBoth()

        self.Remove(self.older, base_health=10)
        self.Remove(self.newer, base_health=6)

        self.assertEqual(self.rhino.health, 14)
        self.assertEqual(self.rhino.max_health, 14)
        self.assertEqual(self.rhino.base_health, 14)

    def test_ending_both_newest_first_restores_printed(self):
        self.ApplyBoth()

        self.Remove(self.newer, base_health=6)
        self.Remove(self.older, base_health=10)

        self.assertEqual(self.rhino.health, 14)
        self.assertEqual(self.rhino.max_health, 14)
        self.assertEqual(self.rhino.base_health, 14)

    def test_the_character_survives_both_removals_in_either_order(self):
        self.ApplyBoth()

        self.Remove(self.older, base_health=10)
        self.Remove(self.newer, base_health=6)

        self.assertFalse(self.rhino.IsDefeated())
        self.assertEqual(self.rhino.sustained, 0)


class TestTheWholeLineFromTwoSources(TwoLiveGrantsOnOneCharacter):
    """All three statistics granted by both sources, torn down in one call.

    This is the shape a real game reaches: the three keywords arrive and leave
    together in a single `Gain`, and hit points are last, so a defect in scheme
    or attack used to be read off a corpse.
    """

    def ApplyBoth(self):
        self.Apply(self.older, base_sch=3, base_atk=3, base_health=10)
        self.Apply(self.newer, base_sch=5, base_atk=5, base_health=6)

    def test_ending_the_older_source_leaves_the_whole_newer_line(self):
        self.ApplyBoth()

        self.Remove(self.older, base_sch=3, base_atk=3, base_health=10)

        self.assertEqual(self.rhino.scheme, 5)
        self.assertEqual(self.rhino.attack, 5)
        self.assertEqual(self.rhino.health, 6)

    def test_ending_the_newer_source_falls_back_to_the_whole_older_line(self):
        self.ApplyBoth()

        self.Remove(self.newer, base_sch=5, base_atk=5, base_health=6)

        self.assertEqual(self.rhino.scheme, 3)
        self.assertEqual(self.rhino.attack, 3)
        self.assertEqual(self.rhino.health, 10)

    def test_ending_both_restores_the_whole_printed_line(self):
        self.ApplyBoth()

        self.Remove(self.older, base_sch=3, base_atk=3, base_health=10)
        self.Remove(self.newer, base_sch=5, base_atk=5, base_health=6)

        self.assertEqual(self.rhino.scheme, 1)
        self.assertEqual(self.rhino.attack, 2)
        self.assertEqual(self.rhino.health, 14)


class TestOrderingNeedsThreeGrants(TwoLiveGrantsOnOneCharacter):
    """Three live grants, because two cannot tell three wrong orderings apart.

    Remove one of two grants and a single entry is left, so it is simultaneously
    the newest and the oldest survivor and the stack's order never has to be
    consulted. Three wrong implementations pass every two-grant test in this
    file: falling back to the *oldest* survivor, pushing new grants onto the
    *bottom* of the stack, and letting a repeat grant keep its old position
    instead of moving to the top. Each of them needs a third grant to show.

    The values climb (3, 5, 7) so that "the newest survivor" and "the oldest
    survivor" are never the same number.
    """

    def ApplyThree(self, statistic):
        self.Apply(self.older, **{statistic: 3})
        self.Apply(self.newer, **{statistic: 5})
        self.Apply(self.newest, **{statistic: 7})

    def test_scheme_falls_back_to_the_newest_survivor(self):
        self.ApplyThree("base_sch")
        self.assertEqual(self.rhino.scheme, 7)

        self.Remove(self.newest, base_sch=7)

        self.assertEqual(self.rhino.scheme, 5)

    def test_attack_falls_back_to_the_newest_survivor(self):
        self.ApplyThree("base_atk")
        self.assertEqual(self.rhino.attack, 7)

        self.Remove(self.newest, base_atk=7)

        self.assertEqual(self.rhino.attack, 5)

    def test_hit_points_fall_back_to_the_newest_survivor(self):
        self.Apply(self.older, base_health=12)
        self.Apply(self.newer, base_health=10)
        self.Apply(self.newest, base_health=6)
        self.assertEqual(self.rhino.health, 6)

        self.Remove(self.newest, base_health=6)

        self.assertEqual(self.rhino.health, 10)
        self.assertEqual(self.rhino.max_health, 10)
        self.assertEqual(self.rhino.base_health, 10)

    def test_removing_the_middle_grant_changes_nothing_then_skips_it(self):
        # The out-of-order removal with somewhere left to fall: taking the
        # middle grant away is invisible, and the newest ending afterwards has
        # to skip past it to the oldest rather than land on a dead entry.
        self.ApplyThree("base_atk")

        self.Remove(self.newer, base_atk=5)
        self.assertEqual(self.rhino.attack, 7)

        self.Remove(self.newest, base_atk=7)
        self.assertEqual(self.rhino.attack, 3)

        self.Remove(self.older, base_atk=3)
        self.assertEqual(self.rhino.attack, 2)

    def test_a_repeat_grant_moves_to_the_top_of_the_stack(self):
        # Re-applying is a fresh application. Read it where it shows: after the
        # oldest source grants again, it outranks the two that came before, so
        # the newest of those ending must leave *its* value showing.
        self.ApplyThree("base_atk")

        self.Apply(self.older, base_atk=4)
        self.assertEqual(self.rhino.attack, 4)

        self.Remove(self.newest, base_atk=7)

        self.assertEqual(self.rhino.attack, 4)


class TestTheEdgesOfTheStack(TwoLiveGrantsOnOneCharacter):
    """Cases the stack has to answer that a single stored value never faced."""

    def test_a_source_granting_again_becomes_the_newest(self):
        # Re-applying is a fresh application, so it goes on top rather than
        # updating in place -- otherwise the older source could end up deciding
        # the base after the newer one had already claimed it.
        self.Apply(self.older, base_atk=3)
        self.Apply(self.newer, base_atk=5)

        self.Apply(self.older, base_atk=4)

        self.assertEqual(self.rhino.attack, 4)

        self.Remove(self.older, base_atk=4)

        self.assertEqual(self.rhino.attack, 5)

    def test_a_source_that_never_granted_does_not_disturb_one_that_did(self):
        # Removal drops this source's own entry. A source with no entry has
        # nothing to drop, and must not take the live grant down with it.
        self.Apply(self.older, base_atk=3)

        self.Remove(self.newer, base_atk=5)

        self.assertEqual(self.rhino.attack, 3)
        self.assertEqual(self.rhino.base_attack, 3)

    def test_resetting_keywords_forgets_every_live_grant(self):
        # `ResetKeywords` throws away every keyword contribution from every
        # source. If the base grants outlived that, a later removal would fall
        # back to a grant whose contribution no longer exists and re-apply it
        # out of nowhere.
        self.Apply(self.older, base_atk=3, base_sch=3)
        self.Apply(self.newer, base_atk=5, base_sch=5)

        self.rhino.ResetKeywords()

        self.Remove(self.newer, base_atk=5, base_sch=5)

        self.assertEqual(self.rhino.attack, 2)
        self.assertEqual(self.rhino.base_attack, 2)
        self.assertEqual(self.rhino.scheme, 1)
        self.assertEqual(self.rhino.base_scheme, 1)

    def test_resetting_hit_points_forgets_every_live_grant(self):
        # The health half of the same guard. `ResetHealth` rebuilds the whole
        # hit-point line from printed, so a grant surviving it would be handed
        # back Rhino's hit points on the next removal -- visibly, since base
        # health moves current health.
        self.Apply(self.older, base_health=10)
        self.Apply(self.newer, base_health=6)

        self.rhino.ResetHealth(self.puzzle.debug_rule)
        self.assertEqual(self.rhino.health, 14)

        self.Remove(self.newer, base_health=6)

        self.assertEqual(self.rhino.health, 14)
        self.assertEqual(self.rhino.max_health, 14)
        self.assertEqual(self.rhino.base_health, 14)

    def test_flipping_forgets_every_live_grant(self):
        # `CanHealth.OnFlip` is the third direct assignment of
        # `base_health = printed_health`, and the one `ResetHealth` does not go
        # through. Driven at the attribute hook, which is what a flip calls.
        self.Apply(self.older, base_health=10)
        self.Apply(self.newer, base_health=6)

        self.rhino.OnFlip(self.puzzle.debug_rule, None)
        self.assertEqual(self.rhino.base_health, 14)

        self.Remove(self.newer, base_health=6)

        self.assertEqual(self.rhino.base_health, 14)


class TestTwoCopiesOfUltronDronesOnOneDrone(unittest.TestCase):
    """The shipped pair, on the card the grant was written for.

    `26031` is not a second script -- `data/cards.json` carries it as
    `{"card_id": "26031", "full_link": "01140"}`, the same Ultron Drones -- so
    two copies in play are two live sources of the same base line on every Drone
    Minion. A drone prints nothing, so its whole stat line comes from them.

    Removing one used to put the drone back on its printed 0 hit points and kill
    it while the other copy was still in play. Whichever copy leaves first, the
    drone has to walk away at 1 hit point and only die when the last one goes.
    """

    REPRINT = "26031"

    def setUp(self):
        self.world = NewWorld()
        self.puzzle = RunPuzzle(self.world)
        self.puzzle.CreatePlayerDeck(AUNT_MAY)
        self.puzzle.PutIntoPlay(ULTRON_DRONES)
        self.puzzle.PutIntoPlay(self.REPRINT)
        self.puzzle.Reveal(ANDROID_EFFICIENCY)
        self.assertIsNotNone(self.FindDrone(), "no drone was made; the rest is vacuous")

    def FindDrone(self):
        for card in self.world.object_manager.card_dict.values():
            if card.face.paper.card_id == DRONE:
                return card
        return None

    def AssertADroneAtOneHitPoint(self):
        drone = self.FindDrone()
        self.assertIsNotNone(drone, "the drone was defeated by a grant it still has")
        self.assertEqual(drone.face.health, 1)
        self.assertEqual(drone.face.max_health, 1)
        self.assertEqual(drone.face.base_health, 1)
        self.assertEqual(drone.face.base_attack, 1)
        self.assertEqual(drone.face.base_scheme, 1)

    def test_both_copies_in_play_grant_one_stat_line(self):
        self.AssertADroneAtOneHitPoint()

    def test_the_drone_survives_losing_the_first_copy(self):
        self.puzzle.Remove(ULTRON_DRONES)

        self.AssertADroneAtOneHitPoint()

    def test_the_drone_survives_losing_the_second_copy(self):
        self.puzzle.Remove(self.REPRINT)

        self.AssertADroneAtOneHitPoint()

    def test_the_drone_dies_when_the_last_copy_goes_first_order(self):
        self.puzzle.Remove(ULTRON_DRONES)
        self.puzzle.Remove(self.REPRINT)

        self.assertIsNone(self.FindDrone())

    def test_the_drone_dies_when_the_last_copy_goes_other_order(self):
        self.puzzle.Remove(self.REPRINT)
        self.puzzle.Remove(ULTRON_DRONES)

        self.assertIsNone(self.FindDrone())

    def test_the_surviving_drone_is_still_a_minion_on_the_board(self):
        self.puzzle.Remove(ULTRON_DRONES)

        self.assertEqual(self.FindDrone().area.deck_type.name, "EngagedEnemiesArea")


if __name__ == "__main__":
    unittest.main()
