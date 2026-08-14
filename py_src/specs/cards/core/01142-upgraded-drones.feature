# Printed: "Attach to the Ultron Drones environment.
# Each facedown [[Drone]] minion gets +1 ATK and +1 hit point.
# Hero Action: Spend [energy] [mental] [physical] resources -> discard this card."
#
# The layer above 01140. Ultron Drones grants a facedown drone a *base* line of
# 1/1/1; this card adds +1 to two of the three, so the two together are what
# make a 2/2 drone. Keeping them apart is the point of this file: a scenario
# that only asserted "2 hit points" could not tell a +1 on top of a base 1 from
# a base grant of 2, and the engine has had a bug in each of those shapes
# (MARVEL-108, MARVEL-111).
#
# Two things are asserted that the card does *not* do, because both are printed
# restrictions rather than omissions:
#
#   scheme     the bonus is "+1 ATK and +1 hit point", so a drone still schemes
#              for the base 1 it got from Ultron Drones
#   facedown   Advanced Ultron Drone is a DRONE minion with printed statistics
#              and is not facedown, so it is untouched -- which is exactly where
#              this card differs from Ultron (III), whose otherwise identical
#              "+1 ATK and +1 hit point" reaches every DRONE minion
#
# One Genius is [mental][mental] and one Energy is [energy][energy], so a hand of
# Energy, Genius and Strength pays [energy][mental][physical] with three cards.

Feature: Upgraded Drones

  Background:
    Given the scenario is "ultron"
    And the hero is "spider_man"
    And I am in hero form
    And "Ultron Drones" is in play

  @card:01142
  Scenario: it attaches to the environment and gives every facedown drone +1 ATK and +1 hit point
    Given my deck is "Aunt May", "Backflip", "Backflip", "Backflip"
    And "Upgraded Drones" is in play
    And "Advanced Ultron Drone" is in play
    And "01144a" is revealed

    # The attachment found the environment named on it rather than sitting loose
    # or attaching to the first character it could reach.
    Then "Upgraded Drones" is in the "UpgradesArea"
    # 2, not 1 and not the 4 a card with printed hit points would have: a base 1
    # from Ultron Drones with this card's +1 on top.
    And "Drone Minion" has 2 health
    And "Drone Minion" has 2 "attack"
    # Untouched, because this card says nothing about scheming.
    And "Drone Minion" has 1 "scheme"
    # "Each *facedown* [[Drone]] minion". Advanced Ultron Drone is a DRONE minion
    # engaged with me under the same attachment and keeps its printed line.
    And "Advanced Ultron Drone" has 4 health
    And "Advanced Ultron Drone" has 1 "attack"
    And I am not prompted again

  @card:01142
  Scenario: the hero action spends three resources and takes the bonus away with the card
    # The other half of the card, and the assertion that the +1s are this card's
    # rather than the environment's: the drone is 2/2 while this is attached and
    # 1/1 the moment it is discarded, with Ultron Drones still in play.
    Given my deck is "Aunt May", "Backflip", "Backflip", "Backflip"
    And my hand is "Energy", "Genius", "Strength"
    And "Upgraded Drones" is in play
    And "01144a" is revealed
    Then "Drone Minion" has 2 health

    When I choose "Hero Action" on "Upgraded Drones"
    Then "Upgraded Drones" is not in play
    And "Drone Minion" has 1 health
    And "Drone Minion" has 1 "attack"
    # Three resources spent, so the hand is empty rather than one card lighter.
    And I have 0 cards in hand
    And I am not prompted again
