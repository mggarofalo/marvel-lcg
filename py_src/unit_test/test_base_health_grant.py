"""A granted base hit point moves max health and current health by the same one.

Some characters have **no printed statistics at all**. A facedown Ultron drone
is the shipped example: `datasets/cards/cards.json` gives
`ultron_facedown_drone` `stats: {}`, and its whole stat line comes from Ultron
Drones (`cards/pack/core/ultron/01140.py`), which grants `base_sch=1`,
`base_atk=1`, `base_health=1` to every Drone Minion in play. One source granting
one hit point should produce a 1 HP minion at full health.

It produced `health = 1, max_health = 2` -- a drone that entered play already
reading 1 damage. `CanHealth.SetBaseHealth` applied the max-health half twice:

    self.UpdateMaxHealth(diff, by_effect)      # <-- MARVEL-100
    self.GainOnlyMaxHealth(diff, by_effect)    # ...which *is* UpdateMaxHealth
    self.GainOnlyHealth(diff, by_effect)

`SetBaseAttack` and `SetBaseScheme` each move their one keyword once, which is
why the drone's granted ATK and SCH were right and only its hit points were not.

**Lethality never changed** -- 0 hit points is 0 hit points, so a drone still
died to one damage and a game that only attacked drones could not see this. What
could see it is `sustained` (`max_health - health`), which is what the rules call
damage, and that is not a cosmetic difference: `CardFinder(canbe_heal=True)`
means `face.CanHeath()`, so a drone carrying a point of phantom damage was a
**legal healing target**. First Aid (01086) is the clean case --
`.SetTarget(Unit2, canbe_heal=True)` with no trait or friendliness filter, over a
default pool that includes `player.engaged_minions`. Luck Be a Lady (40041),
Tic-Tac-Toe (44057) and Regenerative Research (27026) reach it too, the last by
sweeping `GetOnFieldEnemies` unconditionally. Healing the phantom point left a
2/2 drone that took two hits to kill instead of one, which is a live gameplay
consequence rather than a display one.

What it did *not* reach was the wire, and that is measured rather than assumed:
the v2 state digest carries `health` and not `max_health`
(`HasHealth.RegisterInfoDict`), so the drone's digest record is byte-identical
either way and no frozen fixture moves.

This is also a different defect from MARVEL-77, which added the `IsInPlay()`
guard to `UpdateMaxHealth`; the duplicated call predates that fix and survived
it. The removal direction is still covered by that guard, which
`unit_test/test_max_health_guard.py` pins.

The board is built the way `unit_test/test_card_removal.py` builds one, and the
drone is made by revealing Android Efficiency (`01144a`) rather than by calling
the operation behind it, so what is under test is the card doing what it prints.
`specs/cards/core/01144-android-efficiency.feature` describes the same board.

See MARVEL-100.
"""

import unittest

# `engine` first, and not for its side effects: `game.*` modules import each
# other in a cycle that only resolves if `engine/__init__.py` has already walked
# it.
import engine  # noqa: F401  pylint: disable=unused-import

from game.card.card_finder import CardFinder
from game.card.face.base import Unit2
from game.puzzle.puzzle import RunPuzzle
from game.world import digest
from tools.spec.case import SpecCase, ThenStep
from tools.spec.harness import EnsureEngine, NewGameForCase
from tools.spec.policy import TranscriptPolicy


ULTRON_DRONES = "01140"          # the environment that grants the stat line
ANDROID_EFFICIENCY = "01144a"    # the treachery that makes a drone
AUNT_MAY = "01006"               # the filler under it: a support, no health
DRONE = "ultron_facedown_drone"


def NewWorld():
    """A solo Rhino board: villain, main scheme, identity and nothing else."""
    EnsureEngine()
    case = SpecCase(
        name="a drone arrives at full health",
        scenario="rhino",
        heroes=("spider_man",),
        beats=(ThenStep("Rhino", "health", 14),),
    )
    game = NewGameForCase(case, TranscriptPolicy())
    assert game.GameSetup()
    return game.world


class DroneTestCase(unittest.TestCase):
    """Puts one drone into play the way Android Efficiency does."""

    def setUp(self):
        self.world = NewWorld()
        self.puzzle = RunPuzzle(self.world)
        # The card that becomes the drone. A Support with no health of its own,
        # so every hit point the drone has was granted rather than inherited --
        # `Enemies.PutYouDeckTopCardAsFacedownMinion` replaces the face outright.
        self.puzzle.CreatePlayerDeck(AUNT_MAY)
        self.puzzle.PutIntoPlay(ULTRON_DRONES)
        self.puzzle.Reveal(ANDROID_EFFICIENCY)

        self.card = self.FindDrone()
        self.assertIsNotNone(self.card, "no drone was made; the rest is vacuous")

    def FindDrone(self):
        for card in self.world.object_manager.card_dict.values():
            if card.face.paper.card_id == DRONE:
                return card
        return None

    @property
    def face(self):
        return self.card.face


class TestTheGrantedStatLine(DroneTestCase):

    def test_the_drone_is_in_play_engaged_with_the_player(self):
        # Everything below is about a card on the board.
        self.assertEqual(self.card.area.deck_type.name, "EngagedEnemiesArea")

    def test_it_has_the_one_hit_point_it_was_granted(self):
        self.assertEqual(self.face.max_health, 1)

    def test_it_arrives_at_full_health(self):
        self.assertEqual(self.face.health, 1)

    def test_it_arrives_undamaged(self):
        # `sustained` is what a rules answer calls damage, and it is the field
        # the defect actually moved.
        self.assertEqual(self.face.sustained, 0)
        self.assertTrue(self.face.is_full_heath)

    def test_it_has_nothing_to_heal(self):
        # A drone with a point of phantom healing available is the live-gameplay
        # half of the defect: healing it would produce a drone that takes two
        # damage to defeat.
        self.assertFalse(self.face.CanHeath())
        self.assertEqual(self.face.GetLostHealth(), 0)

    def test_the_other_two_granted_statistics_moved_once_each(self):
        # `SetBaseAttack` and `SetBaseScheme` were never wrong. Pinning them
        # here is what stops a "fix" that moves the asymmetry to another
        # keyword instead of removing it.
        self.assertEqual(self.face.attack, 1)
        self.assertEqual(self.face.scheme, 1)

    def test_one_damage_still_defeats_it(self):
        # Lethality was never the symptom, and must not become one: a fix that
        # gave the drone its missing hit point instead of taking away its
        # phantom damage would pass every assertion above and fail this.
        self.puzzle.Damage(self.card, 1)

        self.assertNotEqual(self.card.face.paper.card_id, DRONE)
        self.assertEqual(self.card.area.deck_type.name, "DiscardPile")


class TestAFullHealthDroneIsNotAHealingTarget(DroneTestCase):
    """The live-gameplay half, driven through the filter the cards actually use.

    `CardFinder(canbe_heal=True)` resolves to `face.CanHeath()`
    (`game/card/card_finder/checker.py:180`), which is `max_health - health > 0`.
    A drone that arrives with a phantom point of damage therefore passes it, and
    First Aid's target set is `Unit2` with no further restriction over a pool
    that includes the player's engaged minions -- so the healer never had to be
    written with drones in mind to reach one.

    Asserting the finder rather than playing First Aid is deliberate: the finder
    is what four separate cards share, so pinning it covers all of them, and it
    does not depend on a hand, a resource or a target prompt.
    """

    def Finder(self):
        # Exactly what `cards/pack/core/spider_man/01086.py` asks for:
        # `.SetTarget(Unit2, canbe_heal=True)`.
        return CardFinder(card_type=Unit2, canbe_heal=True)

    def test_the_drone_is_in_the_pool_a_healer_draws_from(self):
        # Without this the rest is vacuous for the wrong reason -- a drone the
        # finder rejects because it was never offered proves nothing.
        engaged = [card.face for card in self.world.GetCurrentPlayer().engaged_minions.cards]
        self.assertIn(self.face, engaged)

    def test_it_is_not_a_legal_target(self):
        self.assertFalse(self.Finder().Check(self.face))

    def test_healing_it_does_nothing(self):
        self.face.HealHealth(2, self.puzzle.debug_rule)

        self.assertEqual(self.face.health, 1)
        self.assertEqual(self.face.max_health, 1)

    def test_it_still_dies_to_one_damage_after_someone_tries(self):
        # The consequence in the form a player would notice: a healed drone that
        # survives its first hit. With the phantom damage present this healed to
        # 2/2 and walked away from one damage at 1/2.
        self.face.HealHealth(2, self.puzzle.debug_rule)

        self.puzzle.Damage(self.card, 1)

        self.assertIsNone(self.FindDrone())

    def test_a_damaged_drone_is_still_a_legal_target(self):
        # The filter has to keep working. A drone cannot take a point and live,
        # so the reachable case is a drone whose max health another source
        # raised -- Upgraded Drones (01142) grants a second hit point through
        # `GainHealthAndMaxHealth`, which is a different code path and stays
        # correct.
        self.puzzle.PutIntoPlay("01142")
        self.assertEqual(self.face.max_health, 2)
        self.puzzle.Damage(self.card, 1)

        self.assertTrue(self.Finder().Check(self.face))
        self.assertEqual(self.face.sustained, 1)


class TestWhatTheDigestSees(DroneTestCase):
    """The wire format, measured rather than assumed.

    The brief for MARVEL-100 expected the fix to move `datasets/digest/vectors.json`
    on the grounds that the digest carries damage. It does not: `HasHealth`
    registers `health` and `is_infinite_health`, and nothing registers
    `max_health` or `sustained`, so the drone's record reads the same before and
    after. These tests state that as a property of the format rather than
    leaving it as a run that happened to pass.
    """

    def Record(self):
        for record in digest.BuildDocument(self.world)["cards"]:
            if record["card"] == DRONE:
                return record
        return None

    def test_the_digest_describes_the_drone(self):
        self.assertIsNotNone(self.Record())

    def test_it_carries_current_health(self):
        self.assertEqual(self.Record()["fields"]["health"], 1)

    def test_it_carries_neither_max_health_nor_damage(self):
        # Which is why this defect was invisible to the corpus oracle, and why
        # the fix moves no frozen fixture.
        fields = self.Record()["fields"]
        self.assertNotIn("max_health", fields)
        self.assertNotIn("sustained", fields)
        self.assertNotIn("damage", fields)


class TestTheMutatorOnAPrintedCharacter(unittest.TestCase):
    """The same rule, away from the drone, on a character that has printed hit points.

    A drone shows the defect at its worst because its whole stat line is granted,
    but the arithmetic is `SetBaseHealth`'s and does not depend on the printed
    value being zero. Driving it against Rhino says so without a card that has to
    exist to say it.
    """

    def setUp(self):
        self.world = NewWorld()
        self.puzzle = RunPuzzle(self.world)
        self.rhino = self.puzzle.FindFaceByName("Rhino")
        self.assertIsNotNone(self.rhino)
        self.effect = self.puzzle.debug_rule

    def test_raising_base_health_raises_both_by_the_same_amount(self):
        health = self.rhino.health
        max_health = self.rhino.max_health
        self.assertEqual(self.rhino.sustained, 0)

        self.rhino.SetBaseHealth(self.rhino.base_health + 2, self.effect)

        self.assertEqual(self.rhino.max_health, max_health + 2)
        self.assertEqual(self.rhino.health, health + 2)

    def test_it_leaves_an_undamaged_character_undamaged(self):
        self.rhino.SetBaseHealth(self.rhino.base_health + 2, self.effect)

        self.assertEqual(self.rhino.sustained, 0)

    def test_it_preserves_damage_already_taken(self):
        # Raising base hit points is not a heal, so the damage on the card has
        # to survive it. The doubled max-health half made it grow instead.
        self.puzzle.Damage(self.rhino, 3)
        self.assertEqual(self.rhino.sustained, 3)

        self.rhino.SetBaseHealth(self.rhino.base_health + 2, self.effect)

        self.assertEqual(self.rhino.sustained, 3)


if __name__ == "__main__":
    unittest.main()
